using System;
using System.Collections.Generic;
using System.Globalization;

namespace TitanCalc.Modules
{
    public struct ConstantEntry
    {
        public string Symbol;
        public string Name;
        public double Value;
        public string Unit;

        public ConstantEntry(string symbol, string name, double val, string unit)
        {
            Symbol = symbol;
            Name = name;
            Value = val;
            Unit = unit;
        }
    }

    /// <summary>
    /// Engineering constants catalog and Casio ENG / SI prefix formatting engine.
    /// </summary>
    public static class EngineeringConstants
    {
        public static readonly List<ConstantEntry> PhysicalConstants = new List<ConstantEntry>();

        static EngineeringConstants()
        {
            PhysicalConstants.Add(new ConstantEntry("c", "Speed of Light", 299792458.0, "m/s"));
            PhysicalConstants.Add(new ConstantEntry("g", "Standard Gravity", 9.80665, "m/s\u00b2"));
            PhysicalConstants.Add(new ConstantEntry("G", "Gravitational Const", 6.67430e-11, "m\u00b3/(kg\u00b7s\u00b2)"));
            PhysicalConstants.Add(new ConstantEntry("h", "Planck Constant", 6.62607015e-34, "J\u00b7s"));
            PhysicalConstants.Add(new ConstantEntry("\u0127", "Reduced Planck Const", 1.054571817e-34, "J\u00b7s"));
            PhysicalConstants.Add(new ConstantEntry("k_B", "Boltzmann Constant", 1.380649e-23, "J/K"));
            PhysicalConstants.Add(new ConstantEntry("e", "Elementary Charge", 1.602176634e-19, "C"));
            PhysicalConstants.Add(new ConstantEntry("\u03b5\u2080", "Permittivity of Free Space", 8.8541878128e-12, "F/m"));
            PhysicalConstants.Add(new ConstantEntry("\u03bc\u2080", "Permeability of Free Space", 1.25663706212e-6, "N/A\u00b2"));
            PhysicalConstants.Add(new ConstantEntry("R", "Universal Gas Const", 8.314462618, "J/(mol\u00b7K)"));
            PhysicalConstants.Add(new ConstantEntry("N_A", "Avogadro Constant", 6.02214076e23, "mol\u207b\u00b9"));
            PhysicalConstants.Add(new ConstantEntry("\u03c3", "Stefan-Boltzmann", 5.670374419e-8, "W/(m\u00b2\u00b7K\u2074)"));
        }

        private static readonly string[] SIPrefixes = new string[]
        {
            "y", // 10^-24 yocto
            "z", // 10^-21 zepto
            "a", // 10^-18 atto
            "f", // 10^-15 femto
            "p", // 10^-12 pico
            "n", // 10^-9 nano
            "\u03bc", // 10^-6 micro
            "m", // 10^-3 milli
            "",  // 10^0
            "k", // 10^3 kilo
            "M", // 10^6 mega
            "G", // 10^9 giga
            "T", // 10^12 tera
            "P", // 10^15 peta
            "E", // 10^18 exa
            "Z", // 10^21 zetta
            "Y"  // 10^24 yotta
        };

        private const int ZeroIndex = 8; // Index of 10^0 in SIPrefixes

        /// <summary>
        /// Formats number into engineering notation (exponent is multiple of 3) with SI prefix.
        /// e.g. 0.0045 -> 4.5 m, 2500000 -> 2.5 M
        /// </summary>
        public static string FormatWithSIPrefix(double value, int decimals = 4)
        {
            if (value == 0.0 || double.IsNaN(value) || double.IsInfinity(value))
                return value.ToString(CultureInfo.InvariantCulture);

            double sign = value < 0 ? -1.0 : 1.0;
            double absVal = Math.Abs(value);

            int exponent = (int)Math.Floor(Math.Log10(absVal));
            int engExp = exponent - (exponent % 3);
            if (exponent < 0 && (exponent % 3) != 0)
            {
                engExp -= 3;
            }

            int prefixIndex = ZeroIndex + (engExp / 3);
            if (prefixIndex < 0 || prefixIndex >= SIPrefixes.Length)
            {
                // Out of SI prefix range, format as scientific
                return value.ToString("E" + decimals, CultureInfo.InvariantCulture);
            }

            double mantissa = sign * (absVal / Math.Pow(10, engExp));
            string prefix = SIPrefixes[prefixIndex];

            string mantissaStr = Math.Round(mantissa, decimals).ToString(CultureInfo.InvariantCulture);
            if (string.IsNullOrEmpty(prefix))
                return mantissaStr;

            return string.Format(CultureInfo.InvariantCulture, "{0} {1}", mantissaStr, prefix);
        }

        /// <summary>
        /// Formats into pure engineering notation: Mantissa x 10^(3k)
        /// </summary>
        public static string FormatEngineering(double value, int expOffset = 0, int decimals = 4)
        {
            if (value == 0.0 || double.IsNaN(value) || double.IsInfinity(value))
                return value.ToString(CultureInfo.InvariantCulture);

            double sign = value < 0 ? -1.0 : 1.0;
            double absVal = Math.Abs(value);

            int exponent = (int)Math.Floor(Math.Log10(absVal));
            int engExp = exponent - (exponent % 3);
            if (exponent < 0 && (exponent % 3) != 0)
            {
                engExp -= 3;
            }

            engExp += expOffset * 3;

            double mantissa = sign * (absVal / Math.Pow(10, engExp));
            return string.Format(CultureInfo.InvariantCulture, "{0} \u00d7 10^{1}", Math.Round(mantissa, decimals), engExp);
        }
    }
}
