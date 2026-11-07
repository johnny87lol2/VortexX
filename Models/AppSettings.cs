using System;
using System.IO;

namespace VortexX.Models
{
    public enum AppLanguage { ZhTW, English }
    public enum AppTheme { Cyber, Midnight, Matrix, Crimson, Arctic }

    public class AppSettings
    {
        // Roblox 裝在 Voretex_RB\Roblox，不跟 Bootstrapper 解壓的檔案混在一起
        public string InstallDir { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Voretex_RB", "Roblox");
        public bool FlushDns { get; set; } = true;
        public bool PowerMode { get; set; } = true;
        public bool AutoStart { get; set; } = false;
        public AppLanguage Language { get; set; } = AppLanguage.English;
        public AppTheme Theme { get; set; } = AppTheme.Cyber;
    }
}
