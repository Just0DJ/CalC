using System;
using System.Collections.Generic;

namespace TitanCalc.Engine
{
    public struct Solution2D
    {
        public double X;
        public double Y;
        public Solution2D(double x, double y) { X = x; Y = y; }
    }

    public struct Solution3D
    {
        public double X;
        public double Y;
        public double Z;
        public Solution3D(double x, double y, double z) { X = x; Y = y; Z = z; }
    }

    /// <summary>
    /// Casio fx-991 style SOLVE engine:
    /// - Newton-Raphson equation solver for f(x) = 0
    /// - Analytical & Numerical polynomial root finders (degrees 1 to 4)
    /// - 2x2 and 3x3 simultaneous linear systems
    /// </summary>
    public static class EquationSolver
    {
        public struct SolveResult
        {
            public bool Success;
            public double Root;
            public int Iterations;
            public double Error;
            public string Message;
        }

        public static SolveResult SolveNewtonRaphson(Func<double, double> f, double initialGuess = 0.0, double tol = 1e-10, int maxIter = 150)
        {
            double x = initialGuess;

            for (int iter = 1; iter <= maxIter; iter++)
            {
                double fx = f(x);
                if (Math.Abs(fx) < tol)
                {
                    return new SolveResult { Success = true, Root = x, Iterations = iter, Error = Math.Abs(fx), Message = "Converged" };
                }

                double dfx = CalculusEngine.Differentiate(f, x);
                if (Math.Abs(dfx) < 1e-14)
                {
                    x += 0.05 * (iter % 2 == 0 ? 1 : -1) * Math.Max(1.0, Math.Abs(x));
                    continue;
                }

                double step = fx / dfx;
                double damping = 1.0;
                double nextX = x - damping * step;
                while (Math.Abs(f(nextX)) > Math.Abs(fx) && damping > 0.05)
                {
                    damping *= 0.5;
                    nextX = x - damping * step;
                }

                if (Math.Abs(nextX - x) < tol && Math.Abs(f(nextX)) < tol * 100)
                {
                    return new SolveResult { Success = true, Root = nextX, Iterations = iter, Error = Math.Abs(f(nextX)), Message = "Converged" };
                }

                x = nextX;
            }

            double finalError = Math.Abs(f(x));
            return new SolveResult
            {
                Success = finalError < tol * 1000,
                Root = x,
                Iterations = maxIter,
                Error = finalError,
                Message = finalError < tol * 1000 ? "Approximate root found" : "Failed to converge within max iterations"
            };
        }

        public static List<ComplexNumber> SolveQuadratic(double a, double b, double c)
        {
            var roots = new List<ComplexNumber>();
            if (Math.Abs(a) < 1e-15)
            {
                if (Math.Abs(b) > 1e-15)
                    roots.Add(new ComplexNumber(-c / b, 0.0));
                return roots;
            }

            double disc = b * b - 4 * a * c;
            if (disc >= 0)
            {
                double sqrtD = Math.Sqrt(disc);
                roots.Add(new ComplexNumber((-b + sqrtD) / (2 * a), 0.0));
                roots.Add(new ComplexNumber((-b - sqrtD) / (2 * a), 0.0));
            }
            else
            {
                double sqrtD = Math.Sqrt(-disc);
                roots.Add(new ComplexNumber(-b / (2 * a), sqrtD / (2 * a)));
                roots.Add(new ComplexNumber(-b / (2 * a), -sqrtD / (2 * a)));
            }
            return roots;
        }

        public static List<ComplexNumber> SolveCubic(double a, double b, double c, double d)
        {
            var roots = new List<ComplexNumber>();
            if (Math.Abs(a) < 1e-15)
                return SolveQuadratic(b, c, d);

            double shift = b / (3.0 * a);
            double p = (3.0 * a * c - b * b) / (3.0 * a * a);
            double q = (2.0 * b * b * b - 9.0 * a * b * c + 27.0 * a * a * d) / (27.0 * a * a * a);

            double disc = (q * q / 4.0) + (p * p * p / 27.0);

            if (Math.Abs(disc) < 1e-14)
            {
                if (Math.Abs(q) < 1e-14)
                {
                    roots.Add(new ComplexNumber(-shift, 0));
                    roots.Add(new ComplexNumber(-shift, 0));
                    roots.Add(new ComplexNumber(-shift, 0));
                }
                else
                {
                    double u = Math.Sign(-q) * Math.Pow(Math.Abs(q) / 2.0, 1.0 / 3.0);
                    roots.Add(new ComplexNumber(2 * u - shift, 0));
                    roots.Add(new ComplexNumber(-u - shift, 0));
                    roots.Add(new ComplexNumber(-u - shift, 0));
                }
            }
            else if (disc > 0)
            {
                double sqrtDisc = Math.Sqrt(disc);
                double uVal = -q / 2.0 + sqrtDisc;
                double vVal = -q / 2.0 - sqrtDisc;

                double u = Math.Sign(uVal) * Math.Pow(Math.Abs(uVal), 1.0 / 3.0);
                double v = Math.Sign(vVal) * Math.Pow(Math.Abs(vVal), 1.0 / 3.0);

                double realRoot = u + v - shift;
                roots.Add(new ComplexNumber(realRoot, 0));

                double compReal = -(u + v) / 2.0 - shift;
                double compImag = (u - v) * Math.Sqrt(3.0) / 2.0;
                roots.Add(new ComplexNumber(compReal, compImag));
                roots.Add(new ComplexNumber(compReal, -compImag));
            }
            else
            {
                double r = Math.Sqrt(-p * p * p / 27.0);
                double phi = Math.Acos(Math.Max(-1.0, Math.Min(1.0, -q / (2.0 * r))));
                double m = 2.0 * Math.Pow(r, 1.0 / 3.0);

                roots.Add(new ComplexNumber(m * Math.Cos(phi / 3.0) - shift, 0));
                roots.Add(new ComplexNumber(m * Math.Cos((phi + 2.0 * Math.PI) / 3.0) - shift, 0));
                roots.Add(new ComplexNumber(m * Math.Cos((phi + 4.0 * Math.PI) / 3.0) - shift, 0));
            }

            return roots;
        }

        public static List<ComplexNumber> SolveQuartic(double a, double b, double c, double d, double e)
        {
            if (Math.Abs(a) < 1e-15)
                return SolveCubic(b, c, d, e);

            double A = b / a;
            double B = c / a;
            double C = d / a;
            double D = e / a;

            double shift = A / 4.0;
            double p = B - 3.0 * A * A / 8.0;
            double q = C - A * B / 2.0 + A * A * A / 8.0;
            double r = D - A * C / 4.0 + A * A * B / 16.0 - 3.0 * A * A * A * A / 256.0;

            var roots = new List<ComplexNumber>();

            if (Math.Abs(q) < 1e-12)
            {
                var zRoots = SolveQuadratic(1.0, p, r);
                foreach (var z in zRoots)
                {
                    var sqrtZ = ComplexNumber.Sqrt(z);
                    roots.Add(sqrtZ - new ComplexNumber(shift, 0));
                    roots.Add(-sqrtZ - new ComplexNumber(shift, 0));
                }
                return roots;
            }

            var resolventRoots = SolveCubic(8.0, 8.0 * p, 2.0 * p * p - 8.0 * r, -q * q);
            double m = 0;
            foreach (var rr in resolventRoots)
            {
                if (rr.IsReal && rr.Real + p > -1e-12)
                {
                    m = Math.Max(0.0, rr.Real);
                    break;
                }
            }

            double sqrtTerm = Math.Sqrt(Math.Max(0.0, 2.0 * m + p));
            if (sqrtTerm < 1e-12) sqrtTerm = 1e-12;

            var quad1 = SolveQuadratic(1.0, sqrtTerm, m + p / 2.0 - q / (2.0 * sqrtTerm));
            var quad2 = SolveQuadratic(1.0, -sqrtTerm, m + p / 2.0 + q / (2.0 * sqrtTerm));

            foreach (var root in quad1) roots.Add(root - new ComplexNumber(shift, 0));
            foreach (var root in quad2) roots.Add(root - new ComplexNumber(shift, 0));

            return roots;
        }

        public static Solution2D SolveSimultaneous2x2(double a1, double b1, double c1, double a2, double b2, double c2)
        {
            double det = a1 * b2 - a2 * b1;
            if (Math.Abs(det) < 1e-14)
                throw new InvalidOperationException("System has no unique solution (determinant is 0).");

            double x = (c1 * b2 - c2 * b1) / det;
            double y = (a1 * c2 - a2 * c1) / det;
            return new Solution2D(x, y);
        }

        public static Solution3D SolveSimultaneous3x3(
            double a1, double b1, double c1, double d1,
            double a2, double b2, double c2, double d2,
            double a3, double b3, double c3, double d3)
        {
            var mat = new MatrixEngine(new double[,] {
                { a1, b1, c1 },
                { a2, b2, c2 },
                { a3, b3, c3 }
            });
            var vec = new double[] { d1, d2, d3 };
            var sol = MatrixEngine.SolveLinearSystem(mat, vec);
            return new Solution3D(sol[0], sol[1], sol[2]);
        }
    }
}
