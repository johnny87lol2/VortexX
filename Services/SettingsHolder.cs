using VortexX.Models;

namespace VortexX.Services
{
    // 讓移植自 Bloxstrap 的程式碼可以照原樣使用 App.Settings.Prop
    public class SettingsHolder
    {
        public AppSettings Prop;

        public SettingsHolder()
        {
            Prop = SettingsService.Load();
        }
    }
}
