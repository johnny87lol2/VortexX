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
                    return new ThemeColors("#0C0816", "#100B1D", "#130D1F", "#261B3D", "#A78BFA", "#EDE6FF", "#8B7FA8", "#22C55E", "#FB7185", "#FBBF24");
                case AppTheme.Matrix:
                    return new ThemeColors("#070D08", "#0A130B", "#0C160D", "#1B2E1D", "#22C55E", "#E3F5E5", "#6FA57B", "#22C55E", "#FB7185", "#FBBF24");
                case AppTheme.Crimson:
                    return new ThemeColors("#12080C", "#170B10", "#1A0E12", "#33181F", "#FB7185", "#FBE7EA", "#A87A83", "#22C55E", "#FB7185", "#FBBF24");
                case AppTheme.Arctic:
                    return new ThemeColors("#08111B", "#0B1722", "#0E1A26", "#1B3045", "#7DD3FC", "#E6F4FF", "#7A9BB5", "#22C55E", "#FB7185", "#FBBF24");
                default:
                    return new ThemeColors("#090D14", "#0C121B", "#0F1620", "#1D2A3A", "#22D3EE", "#E6EEF9", "#7B8CA3", "#22C55E", "#FB7185", "#FBBF24");
            }
        }
    }
}
