using System;
using System.Net.Http;
using System.Threading.Tasks;
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

        static string FindLaunchUri(string[] args)
        {
            foreach (string a in args)
            {
                if (a.StartsWith("roblox:", StringComparison.OrdinalIgnoreCase)
                    || a.StartsWith("roblox-player:", StringComparison.OrdinalIgnoreCase))
                    return a;
            }
            return null;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var s = Settings.Prop;
            Lang.Set(s.Language);
            Theme.Apply(s.Theme);
            Motion.Enabled = s.Animations;
            Motion.Init();

            string uri = FindLaunchUri(e.Args);
            if (uri != null)
            {
                if (RobloxService.IsInstalled(s.InstallDir))
                {
                    Logger.WriteLine("WebJoin", "Launching from browser link");
                    ShutdownMode = ShutdownMode.OnExplicitShutdown;
                    Views.LoadingWindow.LaunchWithProgress(s.InstallDir, s.FlushDns, s.PowerMode,
                        m => Logger.WriteLine("WebJoin", m), uri)
                        .ContinueWith(t => Dispatcher.Invoke(() =>
                        {
                            ShutdownMode = ShutdownMode.OnLastWindowClose;
                            new Views.MainWindow().Show();
                        }));
                    return;
                }
                MessageBox.Show(Lang.T("web.notinstalled"), "VortexX", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            new Views.MainWindow().Show();
        }

        void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.WriteException("App::Unhandled", e.Exception);
            MessageBox.Show(e.Exception.ToString(), "VortexX");
            e.Handled = true;
        }
    }
}
