using System;
using System.IO;
using System.Text;
using TitanCalc.Engine;

namespace TitanCalc.Storage
{
    public enum AppTheme
    {
        ModernMaterial3,
        RetroCasioFx991
    }

    public enum CalculatorLayout
    {
        CompactStandard,
        ExpandedScientific
    }

    /// <summary>
    /// Lightweight application settings persistence.
    /// </summary>
    public class SettingsManager
    {
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("TCSET001");
        private readonly string _filePath;

        public AppTheme Theme { get; set; }
        public CalculatorLayout Layout { get; set; }
        public RetentionPolicy Retention { get; set; }
        public AngleUnit Angle { get; set; }
        public bool AlwaysOnTop { get; set; }
        public double HudOpacity { get; set; }
        public bool PreferFraction { get; set; }

        public SettingsManager()
        {
            // Defaults
            Theme = AppTheme.ModernMaterial3;
            Layout = CalculatorLayout.ExpandedScientific;
            Retention = RetentionPolicy.Hours8;
            Angle = AngleUnit.Degrees;
            AlwaysOnTop = false;
            HudOpacity = 0.88;
            PreferFraction = false;

            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TitanCalc");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "settings.dat");

            Load();
        }

        public void Save()
        {
            try
            {
                using (FileStream fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write))
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    bw.Write(Magic);
                    bw.Write((int)Theme);
                    bw.Write((int)Layout);
                    bw.Write((int)Retention);
                    bw.Write((int)Angle);
                    bw.Write(AlwaysOnTop);
                    bw.Write(HudOpacity);
                    bw.Write(PreferFraction);
                }
            }
            catch { }
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(_filePath)) return;
                using (FileStream fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    byte[] m = br.ReadBytes(Magic.Length);
                    for (int i = 0; i < Magic.Length; i++)
                        if (m[i] != Magic[i]) return;

                    Theme = (AppTheme)br.ReadInt32();
                    Layout = (CalculatorLayout)br.ReadInt32();
                    Retention = (RetentionPolicy)br.ReadInt32();
                    Angle = (AngleUnit)br.ReadInt32();
                    AlwaysOnTop = br.ReadBoolean();
                    HudOpacity = br.ReadDouble();
                    PreferFraction = br.ReadBoolean();
                }
            }
            catch { }
        }
    }
}
