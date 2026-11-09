using System;
using System.Collections.Generic;
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
        public bool Animations { get; set; } = true;
        public bool ApplyMods { get; set; } = true;
        public Dictionary<string, string> SkyFaces { get; set; } = new Dictionary<string, string>();
        public int FpsCap { get; set; } = 0;   // 0 = 不修改
        public AppLanguage Language { get; set; } = AppLanguage.English;
        public AppTheme Theme { get; set; } = AppTheme.Cyber;

        // AI Chat（API 金鑰用 Windows DPAPI 加密後才存檔）
        public string AiProvider { get; set; } = "Anthropic";
        public Dictionary<string, string> AiModels { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> AiKeys { get; set; } = new Dictionary<string, string>();
        public string AiBaseUrl { get; set; } = "https://openrouter.ai/api/v1";
    }
}
