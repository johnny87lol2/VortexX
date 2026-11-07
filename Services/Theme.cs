using System.Windows.Media;
using VortexX.Models;

namespace VortexX.Services
{
    public class ThemeColors
    {
        public string Bg, Bg1, Card, Border, Accent, Text, Gray, Green, Red, Yellow;
        public ThemeColors(string bg, string bg1, string card, string border,
                           string accent, string text, string gray,
                           string green, string red, string yellow)
        {
            Bg = bg; Bg1 = bg1; Card = card; Border = border;
            Accent = accent; Text = text; Gray = gray;
            Green = green; Red = red; Yellow = yellow;
        }
    }

    public static class Theme
    {
        public static ThemeColors Current { get; private set; } = Get(AppTheme.Cyber);

        public static void Apply(AppTheme t) { Current = Get(t); }

        public static ThemeColors Get(AppTheme t)
        {
            switch (t)
            {
                case AppTheme.Midnight:
                    return new ThemeColors("#05010F", "#0A0520", "#080315", "#1E0A40", "#A855F7", "#E8D5FF", "#7A60A0", "#00FF8C", "#FF2060", "#FFB800");
                case AppTheme.Matrix:
                    return new ThemeColors("#010A01", "#021202", "#011001", "#083A08", "#00FF41", "#CCFFCC", "#3E8A48", "#00FF41", "#FF4444", "#CCFF00");
                case AppTheme.Crimson:
                    return new ThemeColors("#0F0205", "#1A0308", "#150204", "#380610", "#FF1744", "#FFD0D8", "#A06070", "#00E87A", "#FF1744", "#FFB800");
                case AppTheme.Arctic:
                    return new ThemeColors("#010D1A", "#021525", "#011018", "#083050", "#60CFFF", "#E0F4FF", "#5A8AAA", "#00E87A", "#FF2060", "#FFB800");
                default:
                    return new ThemeColors("#02050C", "#060D18", "#07101E", "#0E2040", "#00D4FF", "#D0E8FF", "#5A80A0", "#00E87A", "#FF2060", "#FFB800");
            }
        }
    }
}
