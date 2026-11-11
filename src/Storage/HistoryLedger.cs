using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TitanCalc.Storage
{
    public enum RetentionPolicy
    {
        Minutes30,
        Hours8,
        Persistent
    }

    public class HistoryEntry
    {
        public DateTime Timestamp { get; set; }
        public string Expression { get; set; }
        public string Result { get; set; }
        public string ExactFraction { get; set; }
        public bool IsComplex { get; set; }

        public HistoryEntry()
        {
            Timestamp = DateTime.UtcNow;
            Expression = "";
            Result = "";
            ExactFraction = "";
            IsComplex = false;
        }

        public HistoryEntry(string expr, string result, string frac = "", bool isComplex = false)
        {
            Timestamp = DateTime.UtcNow;
            Expression = expr;
            Result = result;
            ExactFraction = frac;
            IsComplex = isComplex;
        }
    }

    /// <summary>
    /// Lightweight zero-dependency memory-managed calculation history ledger.
    /// Serializes directly to a flat binary file in %LOCALAPPDATA%\TitanCalc\history.dat.
    /// </summary>
    public class HistoryLedger
    {
        private static readonly byte[] MagicHeader = Encoding.ASCII.GetBytes("TCHIST01");
        private readonly List<HistoryEntry> _entries;
        private readonly string _filePath;
        private RetentionPolicy _retention;

        public RetentionPolicy Retention
        {
            get { return _retention; }
            set
            {
                _retention = value;
                PurgeExpired();
            }
        }

        public IReadOnlyList<HistoryEntry> Entries { get { return _entries; } }

        public HistoryLedger()
        {
            _entries = new List<HistoryEntry>();
            _retention = RetentionPolicy.Hours8;

            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TitanCalc");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "history.dat");

            LoadFromDisk();
            PurgeExpired();
        }

        public void Add(string expression, string result, string fraction = "", bool isComplex = false)
        {
            if (string.IsNullOrWhiteSpace(expression)) return;

            HistoryEntry entry = new HistoryEntry(expression, result, fraction, isComplex);
            _entries.Insert(0, entry); // Most recent first

            // Limit in-memory history to 500 items max to guarantee near-zero memory footprint
            if (_entries.Count > 500)
            {
                _entries.RemoveAt(_entries.Count - 1);
            }

            PurgeExpired();
            SaveToDisk();
        }

        public void Clear()
        {
            _entries.Clear();
            SaveToDisk();
        }

        public void PurgeExpired()
        {
            if (_retention == RetentionPolicy.Persistent)
                return;

            DateTime now = DateTime.UtcNow;
            TimeSpan maxAge = (_retention == RetentionPolicy.Minutes30) ? TimeSpan.FromMinutes(30) : TimeSpan.FromHours(8);

            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (now - _entries[i].Timestamp > maxAge)
                {
                    _entries.RemoveAt(i);
                }
            }
        }

        public void SaveToDisk()
        {
            try
            {
                using (FileStream fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (BinaryWriter bw = new BinaryWriter(fs, Encoding.UTF8))
                {
                    bw.Write(MagicHeader);
                    bw.Write((int)_retention);
                    bw.Write(_entries.Count);

                    foreach (HistoryEntry e in _entries)
                    {
                        bw.Write(e.Timestamp.ToBinary());
                        bw.Write(e.Expression ?? "");
                        bw.Write(e.Result ?? "");
                        bw.Write(e.ExactFraction ?? "");
                        bw.Write(e.IsComplex);
                    }
                }
            }
            catch { }
        }

        public void LoadFromDisk()
        {
            try
            {
                if (!File.Exists(_filePath)) return;

                using (FileStream fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (BinaryReader br = new BinaryReader(fs, Encoding.UTF8))
                {
                    byte[] header = br.ReadBytes(MagicHeader.Length);
                    for (int i = 0; i < MagicHeader.Length; i++)
                    {
                        if (header[i] != MagicHeader[i]) return;
                    }

                    _retention = (RetentionPolicy)br.ReadInt32();
                    int count = br.ReadInt32();
                    _entries.Clear();

                    for (int i = 0; i < count; i++)
                    {
                        HistoryEntry e = new HistoryEntry();
                        e.Timestamp = DateTime.FromBinary(br.ReadInt64());
                        e.Expression = br.ReadString();
                        e.Result = br.ReadString();
                        e.ExactFraction = br.ReadString();
                        e.IsComplex = br.ReadBoolean();
                        _entries.Add(e);
                    }
                }
            }
            catch { }
        }
    }
}
