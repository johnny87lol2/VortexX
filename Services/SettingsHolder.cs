using VortexX.Models;

namespace VortexX.Services
{
    public class SettingsHolder
    {
        public AppSettings Prop;

        public SettingsHolder()
        {
            Prop = SettingsService.Load();
        }
    }
}
