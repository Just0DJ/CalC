using System;
using System.Globalization;

namespace TitanCalc.Engine
{
    /// <summary>
    /// High-precision complex number with polar/cartesian conversions and transcendental functions.
    /// </summary>
    public struct ComplexNumber : IEquatable<ComplexNumber>
    {
        private readonly double _real;
        private readonly double _imaginary;

        public double Real { get { return _real; } }
        public double Imaginary { get { return _imaginary; } }

        public static readonly ComplexNumber Zero = new ComplexNumber(0.0, 0.0);
        public static readonly ComplexNumber One = new ComplexNumber(1.0, 0.0);
        public static readonly ComplexNumber I = new ComplexNumber(0.0, 1.0);

        public ComplexNumber(double real, double imaginary = 0.0)
        {
            _real = real;
            _imaginary = imaginary;
        }

        public double Magnitude { get { return Math.Sqrt(_real * _real + _imaginary * _imaginary); } }
        public double Phase { get { return Math.Atan2(_imaginary, _real); } }
        public ComplexNumber Conjugate { get { return new ComplexNumber(_real, -_imaginary); } }
        public bool IsReal { get { return Math.Abs(_imaginary) < 1e-15; } }

        public static ComplexNumber FromPolar(double magnitude, double phase)
        {
            return new ComplexNumber(magnitude * Math.Cos(phase), magnitude * Math.Sin(phase));
        }

        public static ComplexNumber operator +(ComplexNumber a, ComplexNumber b)
        {
            return new ComplexNumber(a._real + b._real, a._imaginary + b._imaginary);
        }

        public static ComplexNumber operator -(ComplexNumber a, ComplexNumber b)
        {
            return new ComplexNumber(a._real - b._real, a._imaginary - b._imaginary);
        }

        public static ComplexNumber operator -(ComplexNumber a)
        {
            return new ComplexNumber(-a._real, -a._imaginary);
        }

        public static ComplexNumber operator *(ComplexNumber a, ComplexNumber b)
        {
            return new ComplexNumber(
                a._real * b._real - a._imaginary * b._imaginary,
                a._real * b._imaginary + a._imaginary * b._real
            );
        }

        public static ComplexNumber operator *(ComplexNumber a, double scalar)
        {
            return new ComplexNumber(a._real * scalar, a._imaginary * scalar);
        }

        public static ComplexNumber operator *(double scalar, ComplexNumber a)
        {
            return new ComplexNumber(a._real * scalar, a._imaginary * scalar);
        }

        public static ComplexNumber operator /(ComplexNumber a, ComplexNumber b)
        {
            double denom = b._real * b._real + b._imaginary * b._imaginary;
            if (denom == 0.0)
                throw new DivideByZeroException("Division by zero complex number.");
            return new ComplexNumber(
                (a._real * b._real + a._imaginary * b._imaginary) / denom,
                (a._imaginary * b._real - a._real * b._imaginary) / denom
            );
        }

        public static ComplexNumber operator /(ComplexNumber a, double scalar)
        {
            if (scalar == 0.0)
                throw new DivideByZeroException("Division by zero scalar.");
            return new ComplexNumber(a._real / scalar, a._imaginary / scalar);
        }

        public static ComplexNumber Exp(ComplexNumber z)
        {
            double expReal = Math.Exp(z._real);
            return new ComplexNumber(expReal * Math.Cos(z._imaginary), expReal * Math.Sin(z._imaginary));
        }

        public static ComplexNumber Log(ComplexNumber z)
        {
            return new ComplexNumber(Math.Log(z.Magnitude), z.Phase);
        }

        public static ComplexNumber Pow(ComplexNumber z, ComplexNumber power)
        {
            if (z._real == 0.0 && z._imaginary == 0.0)
                return power._real == 0.0 && power._imaginary == 0.0 ? One : Zero;
            return Exp(power * Log(z));
        }

        public static ComplexNumber Pow(ComplexNumber z, double power)
        {
            return Pow(z, new ComplexNumber(power, 0.0));
        }

        public static ComplexNumber Sqrt(ComplexNumber z)
        {
            double r = Math.Sqrt(z.Magnitude);
            double theta = z.Phase / 2.0;
            return new ComplexNumber(r * Math.Cos(theta), r * Math.Sin(theta));
        }

        public static ComplexNumber Sin(ComplexNumber z)
        {
            return new ComplexNumber(
                Math.Sin(z._real) * Math.Cosh(z._imaginary),
                Math.Cos(z._real) * Math.Sinh(z._imaginary)
            );
        }

        public static ComplexNumber Cos(ComplexNumber z)
        {
            return new ComplexNumber(
                Math.Cos(z._real) * Math.Cosh(z._imaginary),
                -Math.Sin(z._real) * Math.Sinh(z._imaginary)
            );
        }

        public static ComplexNumber Tan(ComplexNumber z)
        {
            return Sin(z) / Cos(z);
        }

        public static ComplexNumber Sinh(ComplexNumber z)
        {
            return (Exp(z) - Exp(-z)) / 2.0;
        }

        public static ComplexNumber Cosh(ComplexNumber z)
        {
            return (Exp(z) + Exp(-z)) / 2.0;
        }

        public bool Equals(ComplexNumber other)
        {
            return Math.Abs(_real - other._real) < 1e-14 && Math.Abs(_imaginary - other._imaginary) < 1e-14;
        }

        public override bool Equals(object obj)
        {
            return obj is ComplexNumber && Equals((ComplexNumber)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_real.GetHashCode() * 397) ^ _imaginary.GetHashCode();
            }
        }

        public override string ToString()
        {
            if (Math.Abs(_imaginary) < 1e-12)
                return CleanNumber(_real);

            if (Math.Abs(_real) < 1e-12)
            {
                if (Math.Abs(_imaginary - 1.0) < 1e-12) return "i";
                if (Math.Abs(_imaginary + 1.0) < 1e-12) return "-i";
                return CleanNumber(_imaginary) + "i";
            }

            string rStr = CleanNumber(_real);
            if (_imaginary > 0)
            {
                string iStr = Math.Abs(_imaginary - 1.0) < 1e-12 ? "" : CleanNumber(_imaginary);
                return string.Format(CultureInfo.InvariantCulture, "{0} + {1}i", rStr, iStr);
            }
            else
            {
                string iStr = Math.Abs(_imaginary + 1.0) < 1e-12 ? "" : CleanNumber(-_imaginary);
                return string.Format(CultureInfo.InvariantCulture, "{0} - {1}i", rStr, iStr);
            }
        }

        public string ToPolarString(bool degrees = true)
        {
            double r = Magnitude;
            double theta = Phase;
            if (degrees)
                theta = theta * 180.0 / Math.PI;
            return string.Format(CultureInfo.InvariantCulture, "{0} \u2220 {1}\u00b0", CleanNumber(r), CleanNumber(theta));
        }

        private static string CleanNumber(double val)
        {
            if (Math.Abs(val) < 1e-12) return "0";
            if (Math.Abs(val - Math.Round(val)) < 1e-11)
                return Math.Round(val).ToString(CultureInfo.InvariantCulture);
            return val.ToString("G12", CultureInfo.InvariantCulture);
        }
    }
}
