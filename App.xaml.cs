using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using VortexX.Services;

namespace VortexX
{
    public partial class App : System.Windows.Application
    {
        public static readonly Logger Logger = new Logger();
        public static readonly SettingsHolder Settings = new SettingsHolder();
        public static readonly HttpClient HttpClient = CreateHttp();

        static HttpClient CreateHttp()
        {
            var h = new HttpClient();
            h.Timeout = System.TimeSpan.FromSeconds(20);
            h.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 VortexX");
            return h;
        }

        public App()
        {
            DispatcherUnhandledException += OnUnhandled;
        }

        void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.WriteException("App::Unhandled", e.Exception);
            MessageBox.Show(e.Exception.ToString(), "VortexX");
            e.Handled = true;
        }
    }
}
