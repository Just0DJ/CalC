using System;

namespace TitanCalc.Engine
{
    /// <summary>
    /// High-order calculus engine providing numerical differentiation, integration, summation, and products.
    /// </summary>
    public static class CalculusEngine
    {
        /// <summary>
        /// Computes numerical derivative d/dx f(x) at x0 using 5-point stencil (O(h^4)).
        /// </summary>
        public static double Differentiate(Func<double, double> f, double x0, double h = 0.0)
        {
            if (h <= 0.0)
            {
                // Optimal step size for 5-point formula with double precision
                h = Math.Max(1.0, Math.Abs(x0)) * 1e-5;
            }

            double f_m2 = f(x0 - 2 * h);
            double f_m1 = f(x0 - h);
            double f_p1 = f(x0 + h);
            double f_p2 = f(x0 + 2 * h);

            double deriv = (-f_p2 + 8.0 * f_p1 - 8.0 * f_m1 + f_m2) / (12.0 * h);
            return Math.Abs(deriv) < 1e-13 ? 0.0 : deriv;
        }

        /// <summary>
        /// Computes numerical integral \int_a^b f(x) dx using Adaptive Simpson's 3/8 Rule.
        /// </summary>
        public static double Integrate(Func<double, double> f, double a, double b, double eps = 1e-8, int maxDepth = 20)
        {
            if (Math.Abs(b - a) < 1e-15) return 0.0;
            if (a > b) return -Integrate(f, b, a, eps, maxDepth);

            return AdaptiveSimpsonRecursive(f, a, b, eps, Simpson(f, a, b), maxDepth);
        }

        private static double Simpson(Func<double, double> f, double a, double b)
        {
            double c = (a + b) / 2.0;
            double h = b - a;
            return (h / 6.0) * (f(a) + 4.0 * f(c) + f(b));
        }

        private static double AdaptiveSimpsonRecursive(Func<double, double> f, double a, double b, double eps, double whole, int depth)
        {
            double c = (a + b) / 2.0;
            double left = Simpson(f, a, c);
            double right = Simpson(f, c, b);
            double delta = left + right - whole;

            if (depth <= 0 || Math.Abs(delta) <= 15.0 * eps)
            {
                return left + right + delta / 15.0;
            }

            return AdaptiveSimpsonRecursive(f, a, c, eps / 2.0, left, depth - 1)
                 + AdaptiveSimpsonRecursive(f, c, b, eps / 2.0, right, depth - 1);
        }

        /// <summary>
        /// Discrete finite summation \sum_{x=a}^b f(x)
        /// </summary>
        public static double Summation(Func<double, double> f, long a, long b)
        {
            if (a > b) return 0.0;
            double sum = 0.0;
            for (long x = a; x <= b; x++)
            {
                sum += f(x);
            }
            return sum;
        }

        /// <summary>
        /// Discrete finite product \prod_{x=a}^b f(x)
        /// </summary>
        public static double Product(Func<double, double> f, long a, long b)
        {
            if (a > b) return 1.0;
            double prod = 1.0;
            for (long x = a; x <= b; x++)
            {
                prod *= f(x);
            }
            return prod;
        }
    }
}
