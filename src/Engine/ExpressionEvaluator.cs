using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TitanCalc.Engine
{
    public enum AngleUnit
    {
        Degrees,
        Radians,
        Gradians
    }

    public class EvalResult
    {
        private bool _isComplex;
        private ComplexNumber _complexVal;
        private BigRational? _rationalVal;

        public bool IsComplex
        {
            get { return _isComplex; }
            set { _isComplex = value; }
        }

        public ComplexNumber ComplexVal
        {
            get { return _complexVal; }
            set { _complexVal = value; }
        }

        public double RealVal
        {
            get { return _complexVal.Real; }
        }

        public BigRational? RationalVal
        {
            get { return _rationalVal; }
            set { _rationalVal = value; }
        }

        public bool HasRational
        {
            get { return _rationalVal.HasValue; }
        }

        public static EvalResult FromReal(double val, BigRational? rat = null)
        {
            EvalResult r = new EvalResult();
            r._isComplex = false;
            r._complexVal = new ComplexNumber(val, 0.0);
            r._rationalVal = rat;
            return r;
        }

        public static EvalResult FromComplex(ComplexNumber c)
        {
            EvalResult r = new EvalResult();
            if (c.IsReal)
            {
                r._isComplex = false;
                r._complexVal = c;
                r._rationalVal = null;
            }
            else
            {
                r._isComplex = true;
                r._complexVal = c;
                r._rationalVal = null;
            }
            return r;
        }

        public string Format(bool preferFraction = false, int maxDecimals = 12)
        {
            if (_isComplex)
            {
                return _complexVal.ToString();
            }

            if (preferFraction && HasRational && !RationalVal.Value.IsZero && RationalVal.Value.Denominator != 1)
            {
                return RationalVal.Value.ToFractionString();
            }

            if (HasRational && RationalVal.Value.Denominator == 1)
            {
                return RationalVal.Value.Numerator.ToString(CultureInfo.InvariantCulture);
            }

            double val = RealVal;
            if (Math.Abs(val - Math.Round(val)) < 1e-12 && Math.Abs(val) < 1e15)
                return Math.Round(val).ToString(CultureInfo.InvariantCulture);

            if (Math.Abs(val) < 1e-14)
                return "0";

            return val.ToString("G" + maxDecimals, CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Recursive descent / Pratt parser for mathematical expressions with exact rational arithmetic,
    /// complex numbers, calculus stencils, engineering constants, and Casio-style angle modes.
    /// </summary>
    public class ExpressionEvaluator
    {
        private AngleUnit _angleMode;
        private double _lastAnswer;
        private double _memoryRegister;
        private readonly Dictionary<string, double> _variables;

        public AngleUnit AngleMode
        {
            get { return _angleMode; }
            set { _angleMode = value; }
        }

        public double LastAnswer
        {
            get { return _lastAnswer; }
            set { _lastAnswer = value; }
        }

        public double MemoryRegister
        {
            get { return _memoryRegister; }
            set { _memoryRegister = value; }
        }

        public ExpressionEvaluator()
        {
            _angleMode = AngleUnit.Degrees;
            _lastAnswer = 0.0;
            _memoryRegister = 0.0;
            _variables = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            ResetVariables();
        }

        public void SetVariable(string name, double val)
        {
            _variables[name] = val;
        }

        public void ResetVariables()
        {
            _variables.Clear();
            _variables["pi"] = Math.PI;
            _variables["e"] = Math.E;
            _variables["euler"] = 0.57721566490153286;
            // Physical constants
            _variables["c"] = 299792458.0;               // Speed of light (m/s)
            _variables["g_acc"] = 9.80665;               // Standard gravity (m/s^2)
            _variables["G"] = 6.67430e-11;               // Gravitational constant
            _variables["h"] = 6.62607015e-34;            // Planck constant (J*s)
            _variables["hbar"] = 1.054571817e-34;        // Reduced Planck constant
            _variables["kB"] = 1.380649e-23;             // Boltzmann constant (J/K)
            _variables["echarge"] = 1.602176634e-19;     // Elementary charge (C)
            _variables["eps0"] = 8.8541878128e-12;       // Vacuum permittivity (F/m)
            _variables["mu0"] = 1.25663706212e-6;        // Vacuum permeability (N/A^2)
            _variables["R"] = 8.314462618;               // Gas constant (J/(mol*K))
            _variables["Na"] = 6.02214076e23;            // Avogadro constant (mol^-1)
        }

        public EvalResult Evaluate(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return EvalResult.FromReal(0.0, BigRational.Zero);

            string sanitized = PreprocessExpression(expression);

            if (CanUseRationalMath(sanitized))
            {
                try
                {
                    BigRational rat = EvaluateRational(sanitized);
                    return EvalResult.FromReal(rat.ToDouble(), rat);
                }
                catch
                {
                    // Fall back to complex parser
                }
            }

            Parser parser = new Parser(sanitized, this);
            ComplexNumber c = parser.ParseExpression();
            return EvalResult.FromComplex(c);
        }

        private bool CanUseRationalMath(string expr)
        {
            string lower = expr.ToLowerInvariant();
            string[] forbidden = new string[] { "sin", "cos", "tan", "ln", "log", "sqrt", "cbrt", "root", "diff", "integ", "sigma", "prod", "i", "^", "e", "pi" };
            foreach (var f in forbidden)
            {
                if (lower.Contains(f))
                    return false;
            }
            return true;
        }

        private string PreprocessExpression(string expr)
        {
            StringBuilder sb = new StringBuilder();
            expr = expr.Trim();

            expr = expr.Replace("\u00d7", "*")  // ×
                       .Replace("\u00f7", "/")  // ÷
                       .Replace("\u2212", "-")  // −
                       .Replace("\u03c0", "pi") // π
                       .Replace("\u221a", "sqrt"); // √

            for (int i = 0; i < expr.Length; i++)
            {
                char c = expr[i];
                sb.Append(c);

                if (i < expr.Length - 1)
                {
                    char next = expr[i + 1];

                    if ((char.IsDigit(c) || c == ')') && next == '(')
                    {
                        sb.Append('*');
                    }
                    else if (c == ')' && (char.IsDigit(next) || char.IsLetter(next)))
                    {
                        sb.Append('*');
                    }
                    else if (char.IsDigit(c) && (char.IsLetter(next) && next != 'e' && next != 'E'))
                    {
                        sb.Append('*');
                    }
                }
            }

            return sb.ToString();
        }

        #region Rational Evaluator
        private BigRational EvaluateRational(string expr)
        {
            int index = 0;
            return ParseRationalAddSub(expr, ref index);
        }

        private BigRational ParseRationalAddSub(string s, ref int idx)
        {
            BigRational left = ParseRationalMulDiv(s, ref idx);
            while (idx < s.Length)
            {
                SkipWhitespace(s, ref idx);
                if (idx >= s.Length) break;
                char op = s[idx];
                if (op != '+' && op != '-') break;
                idx++;
                BigRational right = ParseRationalMulDiv(s, ref idx);
                left = (op == '+') ? left + right : left - right;
            }
            return left;
        }

        private BigRational ParseRationalMulDiv(string s, ref int idx)
        {
            BigRational left = ParseRationalFactor(s, ref idx);
            while (idx < s.Length)
            {
                SkipWhitespace(s, ref idx);
                if (idx >= s.Length) break;
                char op = s[idx];
                if (op != '*' && op != '/') break;
                idx++;
                BigRational right = ParseRationalFactor(s, ref idx);
                left = (op == '*') ? left * right : left / right;
            }
            return left;
        }

        private BigRational ParseRationalFactor(string s, ref int idx)
        {
            SkipWhitespace(s, ref idx);
            if (idx >= s.Length) return BigRational.Zero;

            bool neg = false;
            if (s[idx] == '+') { idx++; SkipWhitespace(s, ref idx); }
            else if (s[idx] == '-') { neg = true; idx++; SkipWhitespace(s, ref idx); }

            BigRational val;
            if (s[idx] == '(')
            {
                idx++;
                val = ParseRationalAddSub(s, ref idx);
                SkipWhitespace(s, ref idx);
                if (idx < s.Length && s[idx] == ')') idx++;
            }
            else
            {
                int start = idx;
                while (idx < s.Length && (char.IsDigit(s[idx]) || s[idx] == '.'))
                    idx++;
                string numStr = s.Substring(start, idx - start);
                val = BigRational.FromDecimalString(numStr);
            }

            return neg ? -val : val;
        }

        private static void SkipWhitespace(string s, ref int idx)
        {
            while (idx < s.Length && char.IsWhiteSpace(s[idx])) idx++;
        }
        #endregion

        #region Complex & Transcendental Parser
        private class Parser
        {
            private readonly string _text;
            private readonly ExpressionEvaluator _eval;
            private int _pos;

            public Parser(string text, ExpressionEvaluator eval)
            {
                _text = text;
                _eval = eval;
                _pos = 0;
            }

            private char Peek() { return _pos < _text.Length ? _text[_pos] : '\0'; }
            private char Get() { return _pos < _text.Length ? _text[_pos++] : '\0'; }
            private void Skip() { while (_pos < _text.Length && char.IsWhiteSpace(_text[_pos])) _pos++; }

            public ComplexNumber ParseExpression()
            {
                return ParseAddSub();
            }

            private ComplexNumber ParseAddSub()
            {
                ComplexNumber left = ParseMulDiv();
                while (true)
                {
                    Skip();
                    char c = Peek();
                    if (c == '+')
                    {
                        Get();
                        left += ParseMulDiv();
                    }
                    else if (c == '-')
                    {
                        Get();
                        left -= ParseMulDiv();
                    }
                    else break;
                }
                return left;
            }

            private ComplexNumber ParseMulDiv()
            {
                ComplexNumber left = ParsePower();
                while (true)
                {
                    Skip();
                    char c = Peek();
                    if (c == '*')
                    {
                        Get();
                        left *= ParsePower();
                    }
                    else if (c == '/')
                    {
                        Get();
                        left /= ParsePower();
                    }
                    else if (c == '%')
                    {
                        Get();
                        ComplexNumber right = ParsePower();
                        left = new ComplexNumber(left.Real % right.Real, 0);
                    }
                    else break;
                }
                return left;
            }

            private ComplexNumber ParsePower()
            {
                ComplexNumber left = ParseUnary();
                Skip();
                if (Peek() == '^')
                {
                    Get();
                    ComplexNumber right = ParsePower(); // right-associative
                    return ComplexNumber.Pow(left, right);
                }
                return left;
            }

            private ComplexNumber ParseUnary()
            {
                Skip();
                char c = Peek();
                if (c == '+')
                {
                    Get();
                    return ParseUnary();
                }
                if (c == '-')
                {
                    Get();
                    return -ParseUnary();
                }
                return ParseFactor();
            }

            private ComplexNumber ParseFactor()
            {
                Skip();
                char c = Peek();

                if (c == '(')
                {
                    Get();
                    ComplexNumber res = ParseExpression();
                    Skip();
                    if (Peek() == ')') Get();
                    return CheckPostfix(res);
                }

                if (char.IsDigit(c) || c == '.')
                {
                    int start = _pos;
                    while (_pos < _text.Length && (char.IsDigit(_text[_pos]) || _text[_pos] == '.'))
                        _pos++;

                    if (_pos < _text.Length && (_text[_pos] == 'e' || _text[_pos] == 'E'))
                    {
                        int nextP = _pos + 1;
                        if (nextP < _text.Length && (_text[nextP] == '+' || _text[nextP] == '-' || char.IsDigit(_text[nextP])))
                        {
                            _pos++;
                            if (_pos < _text.Length && (_text[_pos] == '+' || _text[_pos] == '-')) _pos++;
                            while (_pos < _text.Length && char.IsDigit(_text[_pos])) _pos++;
                        }
                    }

                    string numStr = _text.Substring(start, _pos - start);
                    double val = double.Parse(numStr, CultureInfo.InvariantCulture);

                    if (Peek() == 'i')
                    {
                        Get();
                        return CheckPostfix(new ComplexNumber(0.0, val));
                    }

                    return CheckPostfix(new ComplexNumber(val, 0.0));
                }

                if (char.IsLetter(c))
                {
                    int start = _pos;
                    while (_pos < _text.Length && (char.IsLetterOrDigit(_text[_pos]) || _text[_pos] == '_'))
                        _pos++;
                    string id = _text.Substring(start, _pos - start);

                    if (id.Equals("i", StringComparison.OrdinalIgnoreCase))
                    {
                        return CheckPostfix(ComplexNumber.I);
                    }

                    if (id.Equals("ans", StringComparison.OrdinalIgnoreCase))
                    {
                        return CheckPostfix(new ComplexNumber(_eval.LastAnswer, 0.0));
                    }

                    if (id.Equals("M", StringComparison.OrdinalIgnoreCase))
                    {
                        return CheckPostfix(new ComplexNumber(_eval.MemoryRegister, 0.0));
                    }

                    Skip();
                    if (Peek() == '(')
                    {
                        Get();
                        var args = new List<ComplexNumber>();
                        var rawArgStrings = new List<string>();

                        int openCount = 1;
                        int argStart = _pos;
                        while (_pos < _text.Length && openCount > 0)
                        {
                            if (_text[_pos] == '(') openCount++;
                            else if (_text[_pos] == ')')
                            {
                                openCount--;
                                if (openCount == 0)
                                {
                                    string argStr = _text.Substring(argStart, _pos - argStart);
                                    rawArgStrings.Add(argStr);
                                    _pos++;
                                    break;
                                }
                            }
                            else if (_text[_pos] == ',' && openCount == 1)
                            {
                                string argStr = _text.Substring(argStart, _pos - argStart);
                                rawArgStrings.Add(argStr);
                                _pos++;
                                argStart = _pos;
                                continue;
                            }
                            _pos++;
                        }

                        // For calculus functions, the first argument contains variable 'x', so we pass raw expressions
                        string fn = id.ToLowerInvariant();
                        bool isCalculus = fn == "diff" || fn == "integ" || fn == "sigma";
                        for (int i = 0; i < rawArgStrings.Count; i++)
                        {
                            if (isCalculus && i == 0)
                            {
                                args.Add(ComplexNumber.Zero); // dummy placeholder for expression
                            }
                            else
                            {
                                var argParser = new Parser(rawArgStrings[i], _eval);
                                args.Add(argParser.ParseExpression());
                            }
                        }

                        ComplexNumber res = ExecuteFunction(id, args, rawArgStrings);
                        return CheckPostfix(res);
                    }

                    double varVal;
                    if (_eval._variables.TryGetValue(id, out varVal))
                    {
                        return CheckPostfix(new ComplexNumber(varVal, 0.0));
                    }

                    throw new InvalidOperationException("Unknown identifier or function: " + id);
                }

                throw new InvalidOperationException("Unexpected token in expression at character: '" + c + "'");
            }

            private ComplexNumber CheckPostfix(ComplexNumber val)
            {
                Skip();
                if (Peek() == '!')
                {
                    Get();
                    double n = val.Real;
                    return new ComplexNumber(Factorial(n), 0.0);
                }
                return val;
            }

            private ComplexNumber ExecuteFunction(string name, List<ComplexNumber> args, List<string> rawArgs)
            {
                string fn = name.ToLowerInvariant();

                if (fn == "sin") return ComplexNumber.Sin(ToAngle(args[0]));
                if (fn == "cos") return ComplexNumber.Cos(ToAngle(args[0]));
                if (fn == "tan") return ComplexNumber.Tan(ToAngle(args[0]));
                if (fn == "asin") return FromAngle(Math.Asin(args[0].Real));
                if (fn == "acos") return FromAngle(Math.Acos(args[0].Real));
                if (fn == "atan") return FromAngle(Math.Atan(args[0].Real));

                if (fn == "sinh") return ComplexNumber.Sinh(args[0]);
                if (fn == "cosh") return ComplexNumber.Cosh(args[0]);
                if (fn == "tanh") return ComplexNumber.Sinh(args[0]) / ComplexNumber.Cosh(args[0]);

                if (fn == "ln") return ComplexNumber.Log(args[0]);
                if (fn == "log" || fn == "log10") return ComplexNumber.Log(args[0]) / Math.Log(10.0);
                if (fn == "log2") return ComplexNumber.Log(args[0]) / Math.Log(2.0);
                if (fn == "logb" && args.Count >= 2) return ComplexNumber.Log(args[1]) / ComplexNumber.Log(args[0]);
                if (fn == "exp") return ComplexNumber.Exp(args[0]);

                if (fn == "sqrt") return ComplexNumber.Sqrt(args[0]);
                if (fn == "cbrt") return ComplexNumber.Pow(args[0], 1.0 / 3.0);
                if (fn == "root" && args.Count >= 2) return ComplexNumber.Pow(args[1], 1.0 / args[0].Real);

                if (fn == "fact") return new ComplexNumber(Factorial(args[0].Real), 0.0);
                if (fn == "npr" && args.Count >= 2) return new ComplexNumber(Permutations((long)args[0].Real, (long)args[1].Real), 0.0);
                if (fn == "ncr" && args.Count >= 2) return new ComplexNumber(Combinations((long)args[0].Real, (long)args[1].Real), 0.0);

                if (fn == "abs" || fn == "mod") return new ComplexNumber(args[0].Magnitude, 0.0);
                if (fn == "arg") return new ComplexNumber(args[0].Phase, 0.0);
                if (fn == "conj") return args[0].Conjugate;
                if (fn == "real") return new ComplexNumber(args[0].Real, 0.0);
                if (fn == "imag") return new ComplexNumber(args[0].Imaginary, 0.0);

                if (fn == "diff" && rawArgs.Count >= 2)
                {
                    string exprStr = rawArgs[0];
                    double xVal = args[1].Real;
                    Func<double, double> func = delegate(double v)
                    {
                        ExpressionEvaluator ev = new ExpressionEvaluator();
                        ev.AngleMode = _eval.AngleMode;
                        foreach (KeyValuePair<string, double> kv in _eval._variables)
                            ev.SetVariable(kv.Key, kv.Value);
                        ev.SetVariable("x", v);
                        return ev.Evaluate(exprStr).RealVal;
                    };
                    return new ComplexNumber(CalculusEngine.Differentiate(func, xVal), 0.0);
                }

                if (fn == "integ" && rawArgs.Count >= 3)
                {
                    string exprStr = rawArgs[0];
                    double a = args[1].Real;
                    double b = args[2].Real;
                    Func<double, double> func = delegate(double v)
                    {
                        ExpressionEvaluator ev = new ExpressionEvaluator();
                        ev.AngleMode = _eval.AngleMode;
                        foreach (KeyValuePair<string, double> kv in _eval._variables)
                            ev.SetVariable(kv.Key, kv.Value);
                        ev.SetVariable("x", v);
                        return ev.Evaluate(exprStr).RealVal;
                    };
                    return new ComplexNumber(CalculusEngine.Integrate(func, a, b), 0.0);
                }

                if (fn == "sigma" && rawArgs.Count >= 3)
                {
                    string exprStr = rawArgs[0];
                    long a = (long)args[1].Real;
                    long b = (long)args[2].Real;
                    Func<double, double> func = delegate(double v)
                    {
                        ExpressionEvaluator ev = new ExpressionEvaluator();
                        ev.AngleMode = _eval.AngleMode;
                        foreach (KeyValuePair<string, double> kv in _eval._variables)
                            ev.SetVariable(kv.Key, kv.Value);
                        ev.SetVariable("x", v);
                        return ev.Evaluate(exprStr).RealVal;
                    };
                    return new ComplexNumber(CalculusEngine.Summation(func, a, b), 0.0);
                }

                throw new InvalidOperationException("Unsupported function: " + name);
            }

            private ComplexNumber ToAngle(ComplexNumber c)
            {
                if (!c.IsReal) return c;
                double val = c.Real;
                if (_eval.AngleMode == AngleUnit.Degrees) val = val * Math.PI / 180.0;
                else if (_eval.AngleMode == AngleUnit.Gradians) val = val * Math.PI / 200.0;
                return new ComplexNumber(val, 0.0);
            }

            private ComplexNumber FromAngle(double rad)
            {
                if (_eval.AngleMode == AngleUnit.Degrees) rad = rad * 180.0 / Math.PI;
                else if (_eval.AngleMode == AngleUnit.Gradians) rad = rad * 200.0 / Math.PI;
                return new ComplexNumber(rad, 0.0);
            }

            private double Factorial(double n)
            {
                if (n < 0 || n > 170) throw new OverflowException("Factorial out of range [0, 170].");
                long k = (long)Math.Round(n);
                double res = 1.0;
                for (long i = 2; i <= k; i++) res *= i;
                return res;
            }

            private double Permutations(long n, long r)
            {
                if (n < 0 || r < 0 || r > n) return 0;
                double res = 1.0;
                for (long i = n - r + 1; i <= n; i++) res *= i;
                return res;
            }

            private double Combinations(long n, long r)
            {
                if (n < 0 || r < 0 || r > n) return 0;
                if (r > n - r) r = n - r;
                double res = 1.0;
                for (long i = 1; i <= r; i++)
                {
                    res *= (n - i + 1);
                    res /= i;
                }
                return res;
            }
        }
        #endregion
    }
}
