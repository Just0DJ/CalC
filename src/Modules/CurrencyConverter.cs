using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace TitanCalc.Modules
{
    /// <summary>
    /// Offline-first currency converter with cached rates and non-blocking sync.
    /// </summary>
    public static class CurrencyConverter
    {
        private static readonly Dictionary<string, double> _rates = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        private static DateTime _lastUpdated = DateTime.MinValue;
        private static readonly string CacheFile;

        public static DateTime LastUpdated { get { return _lastUpdated; } }
        public static IEnumerable<string> Currencies { get { return _rates.Keys; } }

        static CurrencyConverter()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dir = Path.Combine(appData, "TitanCalc");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            CacheFile = Path.Combine(dir, "rates.cache");

            LoadDefaultRates();
            LoadFromDisk();
        }

        private static void LoadDefaultRates()
        {
            // Base: 1 USD
            _rates["USD"] = 1.0;
            _rates["EUR"] = 0.92;
            _rates["GBP"] = 0.79;
            _rates["JPY"] = 154.50;
            _rates["CAD"] = 1.39;
            _rates["AUD"] = 1.53;
            _rates["CHF"] = 0.88;
            _rates["CNY"] = 7.23;
            _rates["INR"] = 84.40;
            _rates["SGD"] = 1.34;
            _rates["NZD"] = 1.69;
            _rates["BRL"] = 5.75;
            _rates["ZAR"] = 18.10;
            _rates["AED"] = 3.67;
            _rates["KRW"] = 1395.0;
            _lastUpdated = DateTime.UtcNow;
        }

        public static double Convert(string fromCurrency, string toCurrency, double amount)
        {
            if (fromCurrency.Equals(toCurrency, StringComparison.OrdinalIgnoreCase))
                return amount;

            double fromRate, toRate;
            if (!_rates.TryGetValue(fromCurrency, out fromRate) || !_rates.TryGetValue(toCurrency, out toRate))
                throw new ArgumentException("Unknown currency code: " + fromCurrency + " or " + toCurrency);

            // fromRate = units per 1 USD
            double inUSD = amount / fromRate;
            return inUSD * toRate;
        }

        public static void SyncRatesAsync(Action<bool, string> onComplete)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    // Use free open exchange rates endpoint
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // Tls12
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://open.er-api.com/v6/latest/USD");
                    request.Timeout = 5000;
                    request.UserAgent = "TitanCalc/1.0";

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    {
                        string json = reader.ReadToEnd();
                        ParseAndApplyRates(json);
                        SaveToDisk();
                        if (onComplete != null) onComplete(true, "Rates updated successfully");
                    }
                }
                catch (Exception ex)
                {
                    if (onComplete != null) onComplete(false, "Sync failed: " + ex.Message);
                }
            });
        }

        private static void ParseAndApplyRates(string json)
        {
            // Lightweight zero-dependency JSON rate parser for rates block: "EUR": 0.92, ...
            int ratesIdx = json.IndexOf("\"rates\":", StringComparison.OrdinalIgnoreCase);
            if (ratesIdx < 0) return;

            int braceStart = json.IndexOf('{', ratesIdx);
            int braceEnd = json.IndexOf('}', braceStart);
            if (braceStart < 0 || braceEnd < 0) return;

            string block = json.Substring(braceStart + 1, braceEnd - braceStart - 1);
            string[] pairs = block.Split(',');

            foreach (string pair in pairs)
            {
                string[] kv = pair.Split(':');
                if (kv.Length == 2)
                {
                    string cur = kv[0].Trim().Trim('"', ' ', '\r', '\n');
                    double r;
                    if (double.TryParse(kv[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out r) && r > 0)
                    {
                        _rates[cur] = r;
                    }
                }
            }

            _lastUpdated = DateTime.UtcNow;
        }

        private static void SaveToDisk()
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(CacheFile, false, Encoding.UTF8))
                {
                    sw.WriteLine(_lastUpdated.ToString("o"));
                    foreach (KeyValuePair<string, double> kv in _rates)
                    {
                        sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}:{1}", kv.Key, kv.Value));
                    }
                }
            }
            catch { }
        }

        private static void LoadFromDisk()
        {
            try
            {
                if (!File.Exists(CacheFile)) return;
                using (StreamReader sr = new StreamReader(CacheFile, Encoding.UTF8))
                {
                    string dateLine = sr.ReadLine();
                    DateTime dt;
                    if (DateTime.TryParse(dateLine, out dt)) _lastUpdated = dt;

                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] parts = line.Split(':');
                        if (parts.Length == 2)
                        {
                            double r;
                            if (double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out r))
                            {
                                _rates[parts[0]] = r;
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }
}
