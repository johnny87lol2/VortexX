using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using VortexX.Services;

namespace VortexX.Views
{
    public partial class LoadingWindow : Window
    {
        public bool IsClosed;

        public LoadingWindow()
        {
            InitializeComponent();
            ThemeColors c = Theme.Current;
            Root.Background = Br(c.Bg1);
            Root.BorderBrush = Br(c.Border);
            LogoText.Foreground = Br(c.Accent);
            StatusText.Foreground = Br(c.Gray);
            Track.Background = Br(c.Border);
            Bar.Background = Br(c.Accent);
            BtnClose.Background = Br(c.Card);
            BtnClose.Foreground = Br(c.Gray);
            BtnClose.BorderBrush = Br(c.Border);
            BtnClose.Content = Lang.T("load.close");
            Loaded += OnLoaded;
            base.Closed += (a, b) => { IsClosed = true; };
        }

        static SolidColorBrush Br(string hex)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            double w = Track.ActualWidth;
            if (!Motion.Enabled)
            {
                Bar.Width = w * 0.4;
                return;
            }
            var anim = new DoubleAnimation(-Bar.Width, w, TimeSpan.FromMilliseconds(1300));
            anim.RepeatBehavior = RepeatBehavior.Forever;
            anim.EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut };
            BarMove.BeginAnimation(TranslateTransform.XProperty, anim);
        }

        public void SetStatus(string text)
        {
            StatusText.Text = text;
        }

        void Window_Down(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                try { DragMove(); } catch { }
            }
        }

        void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public static async Task<bool> LaunchWithProgress(string dir, bool flush, bool power, Action<string> log, string uri)
        {
            var w = new LoadingWindow();
            w.SetStatus(Lang.T("load.preparing"));
            w.Show();
            try
            {
                Action<string> both = m =>
                {
                    if (log != null) log(m);
                    w.Dispatcher.BeginInvoke(new Action(() => w.SetStatus(m)));
                };
                bool ok = await Task.Run(() => RobloxService.Launch(dir, flush, power, both, uri));
                if (ok)
                {
                    w.SetStatus(Lang.T("load.waiting"));
                    for (int i = 0; i < 40 && !w.IsClosed && !RobloxService.IsRunning(); i++)
                        await Task.Delay(500);
                    w.SetStatus(Lang.T("load.done"));
                    await Task.Delay(700);
                }
                return ok;
            }
            finally
            {
                if (!w.IsClosed) w.Close();
            }
        }
    }
}
