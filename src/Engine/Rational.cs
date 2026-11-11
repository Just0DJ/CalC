using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace TitanCalc.Engine
{
    /// <summary>
    /// Arbitrary precision rational number (Numerator / Denominator) to eliminate
    /// IEEE 754 binary floating-point rounding errors (e.g. 0.1 + 0.2 = 0.3 exactly).
    /// </summary>
    public struct BigRational : IComparable<BigRational>, IEquatable<BigRational>
    {
        private readonly BigInteger _num;
        private readonly BigInteger _den;

        public static readonly BigRational Zero = new BigRational(BigInteger.Zero, BigInteger.One);
        public static readonly BigRational One = new BigRational(BigInteger.One, BigInteger.One);
        public static readonly BigRational MinusOne = new BigRational(-BigInteger.One, BigInteger.One);

        public BigInteger Numerator { get { return _den.IsZero ? BigInteger.Zero : _num; } }
        public BigInteger Denominator { get { return _den.IsZero ? BigInteger.One : _den; } }

        public BigRational(BigInteger numerator, BigInteger denominator)
        {
            if (denominator.IsZero)
                throw new DivideByZeroException("Denominator cannot be zero in BigRational.");

            if (denominator < 0)
            {
                numerator = -numerator;
                denominator = -denominator;
            }

            if (numerator.IsZero)
            {
                _num = BigInteger.Zero;
                _den = BigInteger.One;
            }
            else
            {
                BigInteger gcd = BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator), denominator);
                _num = numerator / gcd;
                _den = denominator / gcd;
            }
        }

        public BigRational(long numerator, long denominator = 1)
            : this(new BigInteger(numerator), new BigInteger(denominator))
        {
        }

        public BigRational(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentException("Cannot create BigRational from NaN or Infinity.", "value");

            string s = value.ToString("G17", CultureInfo.InvariantCulture);
            BigRational r = FromDecimalString(s);
            _num = r._num;
            _den = r._den;
        }

        public static BigRational FromDecimalString(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return Zero;

            s = s.Trim();
            int expIndex = s.IndexOfAny(new[] { 'e', 'E' });
            int exp = 0;
            if (expIndex >= 0)
            {
                string expStr = s.Substring(expIndex + 1);
                exp = int.Parse(expStr, CultureInfo.InvariantCulture);
                s = s.Substring(0, expIndex);
            }

            int dotIndex = s.IndexOf('.');
            if (dotIndex < 0)
            {
                BigInteger n = BigInteger.Parse(s, CultureInfo.InvariantCulture);
                if (exp > 0)
                    n *= BigInteger.Pow(10, exp);
                else if (exp < 0)
                    return new BigRational(n, BigInteger.Pow(10, -exp));
                return new BigRational(n, BigInteger.One);
            }
            else
            {
                string beforeDot = s.Substring(0, dotIndex);
                string afterDot = s.Substring(dotIndex + 1);
                int decimalPlaces = afterDot.Length;
                string allDigits = beforeDot + afterDot;
                if (allDigits.Length == 0 || allDigits == "-")
                    allDigits = "0";

                BigInteger n = BigInteger.Parse(allDigits, CultureInfo.InvariantCulture);
                int netExp = exp - decimalPlaces;
                if (netExp >= 0)
                    return new BigRational(n * BigInteger.Pow(10, netExp), BigInteger.One);
                else
                    return new BigRational(n, BigInteger.Pow(10, -netExp));
            }
        }

        public bool IsZero { get { return _num.IsZero; } }
        public int Sign { get { return _num.Sign; } }

        public static BigRational operator +(BigRational a, BigRational b)
        {
            BigInteger num = a._num * b._den + b._num * a._den;
            BigInteger den = a._den * b._den;
            return new BigRational(num, den);
        }

        public static BigRational operator -(BigRational a, BigRational b)
        {
            BigInteger num = a._num * b._den - b._num * a._den;
            BigInteger den = a._den * b._den;
            return new BigRational(num, den);
        }

        public static BigRational operator -(BigRational a)
        {
            return new BigRational(-a._num, a._den);
        }

        public static BigRational operator *(BigRational a, BigRational b)
        {
            return new BigRational(a._num * b._num, a._den * b._den);
        }

        public static BigRational operator /(BigRational a, BigRational b)
        {
            if (b.IsZero)
                throw new DivideByZeroException("Division by zero in BigRational.");
            return new BigRational(a._num * b._den, a._den * b._num);
        }

        public static bool operator ==(BigRational a, BigRational b) { return a.Equals(b); }
        public static bool operator !=(BigRational a, BigRational b) { return !a.Equals(b); }
        public static bool operator <(BigRational a, BigRational b) { return a.CompareTo(b) < 0; }
        public static bool operator <=(BigRational a, BigRational b) { return a.CompareTo(b) <= 0; }
        public static bool operator >(BigRational a, BigRational b) { return a.CompareTo(b) > 0; }
        public static bool operator >=(BigRational a, BigRational b) { return a.CompareTo(b) >= 0; }

        public int CompareTo(BigRational other)
        {
            return (_num * other._den).CompareTo(other._num * _den);
        }

        public bool Equals(BigRational other)
        {
            return _num == other._num && _den == other._den;
        }

        public override bool Equals(object obj)
        {
            return obj is BigRational && Equals((BigRational)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_num.GetHashCode() * 397) ^ _den.GetHashCode();
            }
        }

        public double ToDouble()
        {
            return (double)_num / (double)_den;
        }

        public string ToDecimalString(int maxDecimals = 15)
        {
            if (_den == 1)
                return _num.ToString(CultureInfo.InvariantCulture);

            BigInteger integerPart = _num / _den;
            BigInteger remainder = BigInteger.Abs(_num % _den);

            if (remainder.IsZero)
                return integerPart.ToString(CultureInfo.InvariantCulture);

            StringBuilder sb = new StringBuilder();
            if (_num.Sign < 0 && integerPart.IsZero)
                sb.Append("-");
            sb.Append(integerPart.ToString(CultureInfo.InvariantCulture));
            sb.Append(".");

            for (int i = 0; i < maxDecimals && !remainder.IsZero; i++)
            {
                remainder *= 10;
                BigInteger digit = remainder / _den;
                sb.Append(digit.ToString(CultureInfo.InvariantCulture));
                remainder %= _den;
            }

            string result = sb.ToString();
            while (result.EndsWith("0") && result.Contains("."))
                result = result.Substring(0, result.Length - 1);
            if (result.EndsWith("."))
                result = result.Substring(0, result.Length - 1);

            return result;
        }

        public string ToFractionString()
        {
            if (_den == 1)
                return _num.ToString(CultureInfo.InvariantCulture);
            return string.Format(CultureInfo.InvariantCulture, "{0}/{1}", _num, _den);
        }

        public override string ToString()
        {
            return ToDecimalString();
        }
    }
}
