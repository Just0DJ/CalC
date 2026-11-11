using System;
using System.Collections.Generic;

namespace TitanCalc.Modules
{
    public class UnitCategory
    {
        public string Name { get; set; }
        public Dictionary<string, double> UnitsToSI { get; set; }

        public UnitCategory(string name)
        {
            Name = name;
            UnitsToSI = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// High-speed bidirectional unit converter across 8 physical engineering dimensions.
    /// </summary>
    public static class UnitConverter
    {
        public static readonly Dictionary<string, UnitCategory> Categories = new Dictionary<string, UnitCategory>(StringComparer.OrdinalIgnoreCase);

        static UnitConverter()
        {
            // 1. Length (SI: Meter)
            var length = new UnitCategory("Length");
            length.UnitsToSI["Meter (m)"] = 1.0;
            length.UnitsToSI["Kilometer (km)"] = 1000.0;
            length.UnitsToSI["Centimeter (cm)"] = 0.01;
            length.UnitsToSI["Millimeter (mm)"] = 0.001;
            length.UnitsToSI["Micrometer (\u03bcm)"] = 1e-6;
            length.UnitsToSI["Nanometer (nm)"] = 1e-9;
            length.UnitsToSI["Inch (in)"] = 0.0254;
            length.UnitsToSI["Foot (ft)"] = 0.3048;
            length.UnitsToSI["Yard (yd)"] = 0.9144;
            length.UnitsToSI["Mile (mi)"] = 1609.344;
            length.UnitsToSI["Nautical Mile (NM)"] = 1852.0;
            Categories["Length"] = length;

            // 2. Mass (SI: Kilogram)
            var mass = new UnitCategory("Mass");
            mass.UnitsToSI["Kilogram (kg)"] = 1.0;
            mass.UnitsToSI["Gram (g)"] = 0.001;
            mass.UnitsToSI["Milligram (mg)"] = 1e-6;
            mass.UnitsToSI["Metric Ton (t)"] = 1000.0;
            mass.UnitsToSI["Pound (lb)"] = 0.45359237;
            mass.UnitsToSI["Ounce (oz)"] = 0.028349523125;
            mass.UnitsToSI["Carat (ct)"] = 0.0002;
            Categories["Mass"] = mass;

            // 3. Volume (SI: Liter)
            var vol = new UnitCategory("Volume");
            vol.UnitsToSI["Liter (L)"] = 1.0;
            vol.UnitsToSI["Milliliter (mL)"] = 0.001;
            vol.UnitsToSI["Cubic Meter (m\u00b3)"] = 1000.0;
            vol.UnitsToSI["Cubic Centimeter (cm\u00b3)"] = 0.001;
            vol.UnitsToSI["US Gallon (gal)"] = 3.785411784;
            vol.UnitsToSI["US Quart (qt)"] = 0.946352946;
            vol.UnitsToSI["US Pint (pt)"] = 0.473176473;
            vol.UnitsToSI["US Cup"] = 0.2365882365;
            vol.UnitsToSI["Fluid Ounce (fl oz)"] = 0.0295735295625;
            vol.UnitsToSI["Tablespoon (tbsp)"] = 0.01478676478125;
            vol.UnitsToSI["Teaspoon (tsp)"] = 0.00492892159375;
            Categories["Volume"] = vol;

            // 4. Temperature (Handled dynamically)
            var temp = new UnitCategory("Temperature");
            temp.UnitsToSI["Celsius (\u00b0C)"] = 1.0;
            temp.UnitsToSI["Fahrenheit (\u00b0F)"] = 1.0;
            temp.UnitsToSI["Kelvin (K)"] = 1.0;
            temp.UnitsToSI["Rankine (\u00b0R)"] = 1.0;
            Categories["Temperature"] = temp;

            // 5. Pressure (SI: Pascal)
            var pres = new UnitCategory("Pressure");
            pres.UnitsToSI["Pascal (Pa)"] = 1.0;
            pres.UnitsToSI["Kilopascal (kPa)"] = 1000.0;
            pres.UnitsToSI["Bar (bar)"] = 100000.0;
            pres.UnitsToSI["PSI (lbf/in\u00b2)"] = 6894.757293168;
            pres.UnitsToSI["Atmosphere (atm)"] = 101325.0;
            pres.UnitsToSI["Torr / mmHg"] = 133.322368421;
            Categories["Pressure"] = pres;

            // 6. Energy (SI: Joule)
            var energy = new UnitCategory("Energy");
            energy.UnitsToSI["Joule (J)"] = 1.0;
            energy.UnitsToSI["Kilojoule (kJ)"] = 1000.0;
            energy.UnitsToSI["Calorie (cal)"] = 4.184;
            energy.UnitsToSI["Kilocalorie (kcal)"] = 4184.0;
            energy.UnitsToSI["Watt-hour (Wh)"] = 3600.0;
            energy.UnitsToSI["Kilowatt-hour (kWh)"] = 3600000.0;
            energy.UnitsToSI["Electronvolt (eV)"] = 1.602176634e-19;
            energy.UnitsToSI["BTU"] = 1055.05585;
            Categories["Energy"] = energy;

            // 7. Power (SI: Watt)
            var power = new UnitCategory("Power");
            power.UnitsToSI["Watt (W)"] = 1.0;
            power.UnitsToSI["Kilowatt (kW)"] = 1000.0;
            power.UnitsToSI["Megawatt (MW)"] = 1000000.0;
            power.UnitsToSI["Mechanical Horsepower (hp)"] = 745.69987158227022;
            power.UnitsToSI["Metric Horsepower (PS)"] = 735.49875;
            power.UnitsToSI["Foot-pound/sec (ft\u00b7lb/s)"] = 1.3558179483314;
            power.UnitsToSI["BTU/hour (BTU/h)"] = 0.29307107;
            Categories["Power"] = power;

            // 8. Speed (SI: m/s)
            var speed = new UnitCategory("Speed");
            speed.UnitsToSI["Meters/second (m/s)"] = 1.0;
            speed.UnitsToSI["Kilometers/hour (km/h)"] = 1.0 / 3.6;
            speed.UnitsToSI["Miles/hour (mph)"] = 0.44704;
            speed.UnitsToSI["Knot (kn)"] = 0.514444444444;
            speed.UnitsToSI["Feet/second (ft/s)"] = 0.3048;
            Categories["Speed"] = speed;
        }

        public static double Convert(string category, string fromUnit, string toUnit, double value)
        {
            if (fromUnit == toUnit) return value;

            if (category.Equals("Temperature", StringComparison.OrdinalIgnoreCase))
            {
                return ConvertTemperature(fromUnit, toUnit, value);
            }

            UnitCategory cat;
            if (!Categories.TryGetValue(category, out cat))
                throw new ArgumentException("Unknown unit category: " + category);

            double fromFactor, toFactor;
            if (!cat.UnitsToSI.TryGetValue(fromUnit, out fromFactor) || !cat.UnitsToSI.TryGetValue(toUnit, out toFactor))
                throw new ArgumentException("Unknown unit for conversion.");

            double siValue = value * fromFactor;
            return siValue / toFactor;
        }

        private static double ConvertTemperature(string fromUnit, string toUnit, double val)
        {
            // Convert to Celsius first
            double celsius;
            if (fromUnit.StartsWith("Celsius")) celsius = val;
            else if (fromUnit.StartsWith("Fahrenheit")) celsius = (val - 32.0) * 5.0 / 9.0;
            else if (fromUnit.StartsWith("Kelvin")) celsius = val - 273.15;
            else if (fromUnit.StartsWith("Rankine")) celsius = (val - 491.67) * 5.0 / 9.0;
            else celsius = val;

            // Convert Celsius to Target
            if (toUnit.StartsWith("Celsius")) return celsius;
            if (toUnit.StartsWith("Fahrenheit")) return celsius * 9.0 / 5.0 + 32.0;
            if (toUnit.StartsWith("Kelvin")) return celsius + 273.15;
            if (toUnit.StartsWith("Rankine")) return (celsius + 273.15) * 9.0 / 5.0;
            return celsius;
        }
    }
}
