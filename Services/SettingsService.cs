using System;
using System.IO;
using Newtonsoft.Json;
using VortexX.Models;

namespace VortexX.Services
{
    public static class SettingsService
    {
        static readonly string DataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Voretex_RB");
        static readonly string SettingsPath = Path.Combine(DataDir, "settings.json");

        public static string SavePath { get { return SettingsPath; } }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var s = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(SettingsPath));
                    if (s != null) return s;
                }
            }
            catch { }
            return new AppSettings();
        }

        public static void Save(AppSettings s)
        {
            try
            {
                Directory.CreateDirectory(DataDir);
                File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(s, Formatting.Indented));
            }
            catch { }
        }
    }
}
