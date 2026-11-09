using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using VortexX.Models;
using VortexX.Services;
using Shapes = System.Windows.Shapes;

namespace VortexX.Views
{
    public partial class MainWindow : Window
    {
        AppSettings _s;
        string _page = "Home";

        string _installedVer;
        string _latestVer;
        bool _checking;
        bool _installing;
        bool _launching;

        StatusResult _status;
        bool _statusBusy;

        RobloxServerInfo _srv;
        GeoInfo _srvGeo;
        bool _srvBusy;
        string _srvMsgKey = "server.searching";
        string _srvMsgKind = "gray";

        GeoInfo _ipGeo;
        string _ipError;

        readonly Dictionary<string, long> _pingRes = new Dictionary<string, long>();
        bool _pingBusy;

        int _cpu, _ram;
        DispatcherTimer _timer;
        DispatcherTimer _srvTimer;

        public MainWindow()
        {
            InitializeComponent();

            _s = App.Settings.Prop;
            Lang.Set(_s.Language);
            Theme.Apply(_s.Theme);

            RbZh.IsChecked = _s.Language == AppLanguage.ZhTW;
            RbEn.IsChecked = _s.Language != AppLanguage.ZhTW;
            ChkFlushDns.IsChecked = _s.FlushDns;
            ChkPowerMode.IsChecked = _s.PowerMode;
            ChkAutoStart.IsChecked = _s.AutoStart;
            ChkAnim.IsChecked = _s.Animations;
            Motion.Enabled = _s.Animations;
            Motion.Init();

            MaxHeight = SystemParameters.WorkArea.Height;
            MaxWidth = SystemParameters.WorkArea.Width;

            RefreshChatConfig();
            ApplyTheme();
            ApplyLang();
            MarkSelectedTheme();
            ShowPage("Home");

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(2);
            _timer.Tick += (a, b) => Tick();
            _timer.Start();

            _srvTimer = new DispatcherTimer();
            _srvTimer.Interval = TimeSpan.FromSeconds(6);
            _srvTimer.Tick += async (a, b) =>
            {
                if (_page == "Server" && !_srvBusy && _srv == null) await RefreshServerAsync();
            };
            _srvTimer.Start();

            SizeChanged += (a, b) => UpdateBars();
            Loaded += async (a, b) =>
            {
                var openFade = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(350));
                if (Motion.Enabled) WinBorder.BeginAnimation(UIElement.OpacityProperty, openFade);
                Tick();
                var upd = CheckUpdateAsync(true);
                await CheckVersionAsync();
            };
        }

        static SolidColorBrush Br(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }

        static SolidColorBrush BrA(string hex, byte alpha)
        {
            Color c = (Color)ColorConverter.ConvertFromString(hex);
            c.A = alpha;
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        static void Cards(Brush bg, Brush bd, params Border[] items)
        {
            foreach (Border b in items) { b.Background = bg; b.BorderBrush = bd; }
        }

        static void Fill(Brush bg, params Border[] items)
        {
            foreach (Border b in items) { b.Background = bg; }
        }

        static void Fore(Brush fg, params TextBlock[] items)
        {
            foreach (TextBlock t in items) { t.Foreground = fg; }
        }

        static void ForeC(Brush fg, params Control[] items)
        {
            foreach (Control c in items) { c.Foreground = fg; }
        }

        static void BtnStyle(Brush bg, Brush fg, Brush bd, params Button[] items)
        {
            foreach (Button b in items) { b.Background = bg; b.Foreground = fg; b.BorderBrush = bd; }
        }

        void ApplyTheme()
        {
            ThemeColors c = Theme.Current;
            Brush bg = Br(c.Bg), bg1 = Br(c.Bg1), card = Br(c.Card), bd = Br(c.Border);
            Brush ac = Br(c.Accent), tx = Br(c.Text), gr = Br(c.Gray);

            WinBorder.Background = bg;
            WinBorder.BorderBrush = bd;
            WinBorder.BorderThickness = new Thickness(1);
            SidebarBorder.Background = card;
            SidebarBorder.BorderBrush = bd;
            TitlebarBorder.Background = Brushes.Transparent;
            FooterBorder.Background = bg1;
            Resources["AccentBrush"] = Br(c.Accent);
            Resources["LineBrush"] = Br(c.Gray);
            Resources["OnAccentBrush"] = Br(c.Bg);
            Foreground = tx;

            RbChip.Background = card;
            RbChip.BorderBrush = bd;
            var heroBg = new LinearGradientBrush();
            heroBg.StartPoint = new Point(0, 0);
            heroBg.EndPoint = new Point(1, 1);
            heroBg.GradientStops.Add(new GradientStop(((SolidColorBrush)BrA(c.Accent, 70)).Color, 0));
            heroBg.GradientStops.Add(new GradientStop(((SolidColorBrush)BrA(c.Accent, 18)).Color, 0.55));
            heroBg.GradientStops.Add(new GradientStop(((SolidColorBrush)Br(c.Card)).Color, 1));
            HeroBanner.Background = heroBg;
            HeroBanner.BorderBrush = BrA(c.Accent, 60);
            Fore(tx, HeroGreeting, HmNewsTitle);
            Fore(gr, HeroSub, HmNewT1, HmNewT2, HmNewT3);
            Fore(ac, HmNewV1, HmNewV2, HmNewV3);
            Cards(card, bd, HmNewsCard);
            Fill(bd, HmNewsSep);

            Cards(card, bd,
                HmCpuCard, HmRamCard, HmStatusCard, HmInfoCard, InstStatusCard, InstActionCard,
                LaunchOptCard, LaunchLogCard, StatusOvCard, StatusSvcCard, SrvMsgCard, SrvGameCard,
                SrvGeoCard, SrvManualCard, SrvPingCard, StgInstCard, StgLangCard, StgThemeCard,
                StgGenCard, StgAboutCard);
            Cards(bg, bd, InstLogArea, LaunchLogBox, IpResultArea, StgDirBox);

            Fill(bd,
                NavSep1, NavSep2, SbSep, TitleSep, HmInfoSep, InstStatusSep, InstActionSep,
                LaunchOptSep, LaunchLogSep, StatusSvcSep, SrvGameSep, SrvGeoSep, SrvManualSep,
                SrvPingSep, StgInstSep, StgLangSep, StgThemeSep, StgGenSep,
                SbCpuTrack, SbRamTrack, HmCpuTrack, HmRamTrack, ProgressTrack);
            Fill(ac, SbCpuBar, SbRamBar, HmCpuBar, HmRamBar, ProgFill);

            Fore(ac, LogoText, SrvIpVal, StgAboutVer, ProgressPct);
            Fore(tx,
                TxtHomeTitle, TxtInstallTitle, TxtLaunchTitle, TxtStatusTitle,
                TxtServerTitle, TxtSettingsTitle, HmCpuVal, HmRamVal);

            Fore(gr,
                SbVer, SbCpuLbl, SbRamLbl, SbCpuVal, SbRamVal, TxtRbSb, TitleText,
                HmCpuLbl, HmRamLbl, HmStatusLbl, HmVerVal, HmInfoTitle, HmPathLbl, HmInstLbl,
                HmLatestLbl, HmPathVal, InstStatusTitle, InstFolderLbl, InstStatusLbl, InstCurLbl,
                InstLatLbl, InstDirVal, InstActionTitle, InstDesc, LaunchOptTitle, TxtLaunchPath,
                LaunchLogTitle, StatusSvcTitle, StatusTs, SrvGameTitle, SrvGameNameLbl,
                SrvPlaceIdLbl, SrvJobIdLbl, SrvIpLbl, SrvGeoTitle, SrvCountryLbl, SrvRegionLbl,
                SrvCityLbl, SrvTzLbl, SrvIspLbl, SrvCoordLbl, SrvAsLbl, SrvManualTitle,
                SrvPingTitle, StgInstTitle, StgDirLbl, StgLangTitle, StgThemeTitle, StgThemeDesc,
                StgGenTitle, StgAboutTitle, StgAboutDesc, StgSavePathLbl, StgSavePathVal,
                InstLogText, LaunchLogText, ProgressMsg);

            Fore(tx,
                HmInstVal, HmLatestVal, InstCurVal, InstLatVal, SrvGameNameVal, SrvPlaceIdVal,
                SrvJobIdVal, SrvCountryVal, SrvRegionVal, SrvCityVal, SrvTzVal, SrvIspVal,
                SrvCoordVal, SrvAsVal, StgDirVal);

            ForeC(tx, ChkFlushDns, ChkPowerMode, ChkAutoStart, ChkAnim, RbZh, RbEn);

            InputIp.Background = bg;
            InputIp.Foreground = tx;
            InputIp.BorderBrush = bd;
            InputIp.CaretBrush = ac;

            Cards(card, bd, ChatCfgCard, ChatLogCard);
            Fill(bd, ChatCfgSep);
            Fore(ac, TxtChatTitle);
            Fore(gr, ChatCfgTitle, ChatProviderLbl, ChatBaseUrlLbl, ChatApiKeyLbl, ChatKeyNote, ChatModelLbl, ChatModelStatus, ChatHint);
            foreach (TextBox tbx in new TextBox[] { ChatBaseUrlBox, ChatModelBox, ChatInput })
            {
                tbx.Background = bg;
                tbx.Foreground = tx;
                tbx.BorderBrush = bd;
                tbx.CaretBrush = ac;
            }
            ChatKeyBox.Background = bg;
            ChatKeyBox.Foreground = tx;
            ChatKeyBox.BorderBrush = bd;
            ChatKeyBox.CaretBrush = ac;
            BtnStyle(ac, bg, ac, BtnChatSend);
            BtnStyle(bg1, ac, bd, BtnChatSaveKey, BtnChatFetch, BtnChatClear);
            ThemeMods();
            ThemeSky();
            BuildModelChips();
            MarkProvider();
            UpdateKeyStatus();
            RenderChat();

            BtnStyle(ac, bg, ac, BtnGoLaunch, BtnDoInstall, BtnDoLaunch);
            BtnStyle(bg1, ac, bd,
                BtnGoInstall, BtnCheckVer, BtnChangeDir, BtnClearLog, BtnRefreshStatus,
                BtnRefreshServer, BtnIpLookup, BtnPing, BtnChangeDirStg);
            BtnStyle(Brushes.Transparent, gr, Brushes.Transparent, BtnMin, BtnMax, BtnClose);

            RbDot.Fill = gr;
            StatusOvDot.Fill = gr;

            HighlightNav();
            UpdateDynamic();
            RenderStatus();
            RenderSrvMsg();
            RenderServer();
            RenderIp();
            BuildPingRows();
            UpdateRunning();
        }

        static string U(string key) { return Lang.T(key); }

        void ApplyLang()
        {
            Title = "VortexX";

            BtnHome.Content = Lang.T("nav.home");
            BtnInstall.Content = Lang.T("nav.install");
            BtnLaunch.Content = Lang.T("nav.launch");
            BtnStatus.Content = Lang.T("nav.status");
            BtnServer.Content = Lang.T("nav.server");
            BtnSettings.Content = Lang.T("nav.settings");
            LangMods();
            LangSky();

            int hr = DateTime.Now.Hour;
            string gk = hr < 12 ? "home.greet_m" : (hr < 18 ? "home.greet_a" : "home.greet_e");
            HeroGreeting.Text = string.Format(Lang.T(gk), Environment.UserName);
            HeroSub.Text = Lang.T("home.hero_sub");
            HmNewsTitle.Text = Lang.T("home.news");
            HmNewV1.Text = Lang.T("home.n1v"); HmNewT1.Text = Lang.T("home.n1t");
            HmNewV2.Text = Lang.T("home.n2v"); HmNewT2.Text = Lang.T("home.n2t");
            HmNewV3.Text = Lang.T("home.n3v"); HmNewT3.Text = Lang.T("home.n3t");
            TxtHomeTitle.Text = U("home.title");
            HmStatusLbl.Text = Lang.T("home.status");
            HmInfoTitle.Text = U("home.info");
            HmPathLbl.Text = Lang.T("home.path");
            HmInstLbl.Text = Lang.T("home.installed");
            HmLatestLbl.Text = Lang.T("home.latest");
            BtnGoInstall.Content = Lang.T("home.btn_install");
            BtnGoLaunch.Content = Lang.T("home.btn_launch");

            TxtInstallTitle.Text = U("install.title");
            BtnCheckVer.Content = Lang.T("install.check");
            InstStatusTitle.Text = U("install.card_status");
            InstFolderLbl.Text = Lang.T("install.folder");
            BtnChangeDir.Content = Lang.T("install.change");
            InstStatusLbl.Text = Lang.T("install.status");
            InstCurLbl.Text = Lang.T("install.current");
            InstLatLbl.Text = Lang.T("install.latest");
            InstActionTitle.Text = U("install.card_action");
            InstDesc.Text = Lang.T("install.desc");

            TxtLaunchTitle.Text = U("launch.title");
            LaunchOptTitle.Text = U("launch.opts");
            ChkFlushDns.Content = Lang.T("launch.dns");
            ChkPowerMode.Content = Lang.T("launch.power");
            BtnDoLaunch.Content = Lang.T("launch.btn");
            LaunchLogTitle.Text = U("launch.log");
            BtnClearLog.Content = Lang.T("launch.clear");

            TxtStatusTitle.Text = U("status.title");
            BtnRefreshStatus.Content = Lang.T("status.refresh");
            StatusSvcTitle.Text = U("status.services");

            TxtServerTitle.Text = U("server.title");
            BtnRefreshServer.Content = Lang.T("server.refresh");
            SrvGameTitle.Text = U("server.game");
            SrvGameNameLbl.Text = Lang.T("server.gamename");
            SrvPlaceIdLbl.Text = Lang.T("server.placeid");
            SrvJobIdLbl.Text = Lang.T("server.jobid");
            SrvIpLbl.Text = Lang.T("server.ip");
            SrvGeoTitle.Text = U("server.geo");
            SrvCountryLbl.Text = Lang.T("server.country");
            SrvRegionLbl.Text = Lang.T("server.region");
            SrvCityLbl.Text = Lang.T("server.city");
            SrvTzLbl.Text = Lang.T("server.tz");
            SrvIspLbl.Text = Lang.T("server.isp");
            SrvCoordLbl.Text = Lang.T("server.coord");
            SrvAsLbl.Text = Lang.T("server.as");
            SrvManualTitle.Text = U("server.manual");
            BtnIpLookup.Content = Lang.T("server.lookup");
            SrvPingTitle.Text = U("server.ping");
            BtnPing.Content = Lang.T("server.pingbtn");

            TxtSettingsTitle.Text = U("settings.title");
            StgInstTitle.Text = U("settings.install");
            StgDirLbl.Text = Lang.T("settings.dir");
            BtnChangeDirStg.Content = Lang.T("settings.change");
            StgLangTitle.Text = U("settings.lang");
            RbZh.Content = Lang.T("lang.zh");
            RbEn.Content = Lang.T("lang.en");
            StgThemeTitle.Text = U("settings.theme");
            StgThemeDesc.Text = Lang.T("settings.themedesc");
            TCardCyberDesc.Text = Lang.T("theme.cyber");
            TCardMidnightDesc.Text = Lang.T("theme.midnight");
            TCardMatrixDesc.Text = Lang.T("theme.matrix");
            TCardCrimsonDesc.Text = Lang.T("theme.crimson");
            TCardArcticDesc.Text = Lang.T("theme.arctic");
            StgGenTitle.Text = U("settings.general");
            ChkAutoStart.Content = Lang.T("settings.autostart");
            ChkAnim.Content = Lang.T("settings.anim");
            StgAboutTitle.Text = U("settings.about");
            StgAboutVer.Text = "VortexX";
            StgAboutDesc.Text = Lang.T("settings.aboutdesc");
            StgSavePathLbl.Text = Lang.T("settings.savepath");
            StgSavePathVal.Text = SettingsService.SavePath;

            BtnChat.Content = Lang.T("nav.chat");
            TxtChatTitle.Text = U("chat.title");
            ChatCfgTitle.Text = U("chat.config");
            ChatProviderLbl.Text = Lang.T("chat.provider");
            BtnProvCustom.Content = Lang.T("chat.prov_custom");
            ChatBaseUrlLbl.Text = Lang.T("chat.baseurl");
            ChatApiKeyLbl.Text = Lang.T("chat.apikey");
            BtnChatSaveKey.Content = Lang.T("chat.save");
            ChatKeyNote.Text = Lang.T("chat.keynote");
            ChatModelLbl.Text = Lang.T("chat.model");
            BtnChatFetch.Content = Lang.T("chat.fetch");
            BtnChatSend.Content = Lang.T("chat.send");
            BtnChatClear.Content = Lang.T("chat.clear");
            ChatHint.Text = Lang.T("chat.hint");
            UpdateKeyStatus();
            RenderChat();

            UpdateDynamic();
            UpdateLaunchPath();
            RenderStatus();
            RenderSrvMsg();
            RenderServer();
            RenderIp();
            BuildPingRows();
            UpdateRunning();
        }

        void TitleBar_Down(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) { ToggleMax(); return; }
            if (WindowState == WindowState.Normal)
            {
                try { DragMove(); } catch { }
            }
        }

        void ToggleMax()
        {
            WindowState = (WindowState == WindowState.Normal) ? WindowState.Maximized : WindowState.Normal;
            bool max = WindowState == WindowState.Maximized;
            BtnMax.Content = max ? "❐" : "▢";
            WinBorder.CornerRadius = new CornerRadius(max ? 0 : 8);
        }

        void BtnMin_Click(object sender, RoutedEventArgs e) { WindowState = WindowState.Minimized; }
        void BtnMax_Click(object sender, RoutedEventArgs e) { ToggleMax(); }
        void BtnClose_Click(object sender, RoutedEventArgs e) { Close(); }

        void Nav_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            if (b == null) return;
            ShowPage(b.Tag as string);
        }

        void BtnGoInstall_Click(object sender, RoutedEventArgs e) { ShowPage("Install"); }
        async void BtnGoLaunch_Click(object sender, RoutedEventArgs e)
        {
            if (_launching) return;
            _launching = true;
            BtnGoLaunch.IsEnabled = false;
            BtnDoLaunch.IsEnabled = false;
            object oldContent = BtnGoLaunch.Content;
            BtnGoLaunch.Content = Lang.T("home.launching");
            string err = null;
            try
            {
                bool fd = ChkFlushDns.IsChecked == true;
                bool pw = ChkPowerMode.IsChecked == true;
                string dir = _s.InstallDir;
                Action<string> log = m => Dispatcher.BeginInvoke(new Action(() => AppendLog(LaunchLogText, LaunchLogScroll, m)));
                await Task.Run(() => RobloxService.Launch(dir, fd, pw, log));
            }
            catch (Exception ex)
            {
                err = ex.Message;
                AppendLog(LaunchLogText, LaunchLogScroll, string.Format(Lang.T("log.error"), ex.Message));
            }
            finally
            {
                _launching = false;
                BtnGoLaunch.IsEnabled = true;
                BtnDoLaunch.IsEnabled = true;
                BtnGoLaunch.Content = oldContent;
            }
            if (err != null) MessageBox.Show(err, "VortexX", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        void ShowPage(string name)
        {
            _page = name;
            PgHome.Visibility = name == "Home" ? Visibility.Visible : Visibility.Collapsed;
            PgInstall.Visibility = name == "Install" ? Visibility.Visible : Visibility.Collapsed;
            PgLaunch.Visibility = name == "Launch" ? Visibility.Visible : Visibility.Collapsed;
            PgStatus.Visibility = name == "Status" ? Visibility.Visible : Visibility.Collapsed;
            PgServer.Visibility = name == "Server" ? Visibility.Visible : Visibility.Collapsed;
            PgChat.Visibility = name == "Chat" ? Visibility.Visible : Visibility.Collapsed;
            PgMods.Visibility = name == "Mods" ? Visibility.Visible : Visibility.Collapsed;
            PgEngine.Visibility = name == "Engine" ? Visibility.Visible : Visibility.Collapsed;
            PgSky.Visibility = name == "Sky" ? Visibility.Visible : Visibility.Collapsed;
            PgSettings.Visibility = name == "Settings" ? Visibility.Visible : Visibility.Collapsed;
            HighlightNav();
            AnimatePage(name);

            if (name == "Install") { var t1 = CheckVersionAsync(); }
            else if (name == "Launch") { UpdateLaunchPath(); }
            else if (name == "Status") { if (_status == null && !_statusBusy) { var t2 = RefreshStatusAsync(); } }
            else if (name == "Server") { if (!_srvBusy) { var t3 = RefreshServerAsync(); } }
            else if (name == "Mods") { RefreshMods(); }
            else if (name == "Engine") { LoadEngine(); }
            else if (name == "Sky") { RefreshSky(); }
        }

        void AnimatePage(string name)
        {
            FrameworkElement pg = PgHome;
            if (name == "Install") pg = PgInstall;
            else if (name == "Launch") pg = PgLaunch;
            else if (name == "Status") pg = PgStatus;
            else if (name == "Server") pg = PgServer;
            else if (name == "Chat") pg = PgChat;
            else if (name == "Mods") pg = PgMods;
            else if (name == "Engine") pg = PgEngine;
            else if (name == "Sky") pg = PgSky;
            else if (name == "Settings") pg = PgSettings;

            if (!Motion.Enabled)
            {
                pg.BeginAnimation(UIElement.OpacityProperty, null);
                pg.Opacity = 1;
                pg.RenderTransform = Transform.Identity;
                return;
            }
            var ease = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };
            var tt = new TranslateTransform(0, 18);
            pg.RenderTransform = tt;
            var fade = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(320));
            fade.EasingFunction = ease;
            var slide = new System.Windows.Media.Animation.DoubleAnimation(18, 0, TimeSpan.FromMilliseconds(360));
            slide.EasingFunction = ease;
            pg.BeginAnimation(UIElement.OpacityProperty, fade);
            tt.BeginAnimation(TranslateTransform.YProperty, slide);
        }

        void HighlightNav()
        {
            ThemeColors c = Theme.Current;
            Button[] navs = { BtnHome, BtnInstall, BtnLaunch, BtnStatus, BtnServer, BtnChat, BtnMods, BtnEngine, BtnSky, BtnSettings };
            foreach (Button b in navs)
            {
                bool act = (b.Tag as string) == _page;
                b.Background = act ? BrA(c.Accent, 38) : Brushes.Transparent;
                b.BorderBrush = Brushes.Transparent;
                b.Foreground = act ? Br(c.Accent) : Br(c.Gray);
            }
        }

        void Tick()
        {
            _cpu = SysStats.Cpu();
            _ram = SysStats.Ram();
            SbCpuVal.Text = _cpu + "%";
            SbRamVal.Text = _ram + "%";
            HmCpuVal.Text = _cpu + "%";
            HmRamVal.Text = _ram + "%";
            UpdateBars();
            UpdateRunning();
        }

        static void SetBar(Border track, Border bar, int pct)
        {
            double w = track.ActualWidth;
            double to = Math.Max(0, w * pct / 100.0);
            if (!Motion.Enabled)
            {
                bar.BeginAnimation(FrameworkElement.WidthProperty, null);
                bar.Width = to;
                return;
            }
            var anim = new System.Windows.Media.Animation.DoubleAnimation(to, TimeSpan.FromMilliseconds(500));
            anim.EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };
            bar.BeginAnimation(FrameworkElement.WidthProperty, anim);
        }

        void UpdateBars()
        {
            SetBar(SbCpuTrack, SbCpuBar, _cpu);
            SetBar(SbRamTrack, SbRamBar, _ram);
            SetBar(HmCpuTrack, HmCpuBar, _cpu);
            SetBar(HmRamTrack, HmRamBar, _ram);
        }

        void UpdateRunning()
        {
            ThemeColors c = Theme.Current;
            bool r = RobloxService.IsRunning();
            TxtRbSb.Text = Lang.T(r ? "sb.rb_on" : "sb.rb_off");
            RbDot.Fill = Br(r ? c.Green : c.Gray);
            TxtRbRunning.Text = Lang.T(r ? "launch.running" : "launch.notrunning");
            TxtRbRunning.Foreground = Br(r ? c.Green : c.Gray);
        }

        async Task CheckVersionAsync()
        {
            if (_checking) return;
            _checking = true;
            UpdateDynamic();
            try { _latestVer = await RobloxService.GetLatestVersionAsync(); }
            catch { _latestVer = null; }
            _installedVer = RobloxService.GetInstalledVersion(_s.InstallDir);
            _checking = false;
            UpdateDynamic();
        }

        void UpdateDynamic()
        {
            ThemeColors c = Theme.Current;
            bool installed = RobloxService.IsInstalled(_s.InstallDir);
            bool outdated = installed && _latestVer != null && _installedVer != _latestVer;

            string stText;
            string stColor;
            if (!installed) { stText = Lang.T("home.notinstalled"); stColor = c.Yellow; }
            else if (outdated) { stText = Lang.T("home.update"); stColor = c.Yellow; }
            else { stText = Lang.T("home.ready"); stColor = c.Green; }
            Brush stBrush = Br(stColor);

            HmStatusVal.Text = stText;
            HmStatusVal.Foreground = stBrush;
            InstStatusVal.Text = stText;
            InstStatusVal.Foreground = stBrush;

            string instTxt = installed ? (_installedVer ?? "?") : Lang.T("install.not_installed");
            string latTxt = _checking ? Lang.T("install.checking") : (_latestVer ?? Lang.T("install.latest_fail"));
            HmInstVal.Text = instTxt;
            HmLatestVal.Text = latTxt;
            InstCurVal.Text = instTxt;
            InstLatVal.Text = latTxt;
            HmVerVal.Text = installed ? (_installedVer ?? "") : "";

            HmPathVal.Text = _s.InstallDir;
            InstDirVal.Text = _s.InstallDir;
            StgDirVal.Text = _s.InstallDir;

            if (!installed) BtnDoInstall.Content = Lang.T("install.btn_install");
            else if (outdated) BtnDoInstall.Content = Lang.T("install.btn_update");
            else BtnDoInstall.Content = Lang.T("install.btn_reinstall");

            UpdateLaunchPath();
        }

        void UpdateLaunchPath()
        {
            if (RobloxService.IsInstalled(_s.InstallDir))
                TxtLaunchPath.Text = string.Format(Lang.T("launch.path"), _s.InstallDir);
            else
                TxtLaunchPath.Text = Lang.T("launch.notinstalled");
        }

        async void BtnCheckVer_Click(object sender, RoutedEventArgs e)
        {
            await CheckVersionAsync();
        }

        async void BtnChangeDir_Click(object sender, RoutedEventArgs e)
        {
            using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                dlg.Description = Lang.T("dialog.pick_dir");
                try { if (Directory.Exists(_s.InstallDir)) dlg.SelectedPath = _s.InstallDir; } catch { }
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _s.InstallDir = dlg.SelectedPath;
                    SettingsService.Save(_s);
                    _installedVer = null;
                    UpdateDynamic();
                    await CheckVersionAsync();
                }
            }
        }

        void SetProgress(int pct, string msg)
        {
            ProgressPct.Text = pct + "%";
            ProgressMsg.Text = msg;
            ProgFill.Width = Math.Max(0, ProgressTrack.ActualWidth * pct / 100.0);
        }

        void AppendLog(TextBlock tb, ScrollViewer sv, string msg)
        {
            tb.Text += "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg + "\n";
            sv.ScrollToEnd();
        }

        async void BtnDoInstall_Click(object sender, RoutedEventArgs e)
        {
            if (_installing) return;
            _installing = true;
            BtnDoInstall.IsEnabled = false;
            ProgressArea.Visibility = Visibility.Visible;
            InstLogArea.Visibility = Visibility.Visible;
            InstLogText.Text = "";
            SetProgress(0, Lang.T("inst.download"));

            Action<int, string> prog = (p, m) => Dispatcher.BeginInvoke(new Action(() => SetProgress(p, m)));
            Action<string> log = m => Dispatcher.BeginInvoke(new Action(() => AppendLog(InstLogText, InstLogScroll, m)));

            try
            {
                await RobloxService.InstallAsync(_s.InstallDir, prog, log);
            }
            catch (Exception ex)
            {
                string msg = string.Format(Lang.T("inst.fail"), ex.Message);
                AppendLog(InstLogText, InstLogScroll, msg);
                ProgressMsg.Text = msg;
            }
            finally
            {
                _installing = false;
                BtnDoInstall.IsEnabled = true;
            }
            _installedVer = RobloxService.GetInstalledVersion(_s.InstallDir);
            UpdateDynamic();
            await CheckVersionAsync();
        }

        void LaunchOpt_Click(object sender, RoutedEventArgs e)
        {
            _s.FlushDns = ChkFlushDns.IsChecked == true;
            _s.PowerMode = ChkPowerMode.IsChecked == true;
            SettingsService.Save(_s);
        }

        async void BtnDoLaunch_Click(object sender, RoutedEventArgs e)
        {
            if (_launching) return;
            _launching = true;
            BtnDoLaunch.IsEnabled = false;
            bool fd = ChkFlushDns.IsChecked == true;
            bool pw = ChkPowerMode.IsChecked == true;
            string dir = _s.InstallDir;
            Action<string> log = m => Dispatcher.BeginInvoke(new Action(() => AppendLog(LaunchLogText, LaunchLogScroll, m)));
            try
            {
                await Task.Run(() => RobloxService.Launch(dir, fd, pw, log));
            }
            catch (Exception ex)
            {
                AppendLog(LaunchLogText, LaunchLogScroll, string.Format(Lang.T("log.error"), ex.Message));
            }
            finally
            {
                _launching = false;
                BtnDoLaunch.IsEnabled = true;
            }
        }

        void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            LaunchLogText.Text = "";
        }

        async void BtnRefreshStatus_Click(object sender, RoutedEventArgs e)
        {
            await RefreshStatusAsync();
        }

        async Task RefreshStatusAsync()
        {
            if (_statusBusy) return;
            _statusBusy = true;
            StatusOvText.Text = Lang.T("status.loading");
            StatusOvText.Foreground = Br(Theme.Current.Gray);
            try { _status = await RobloxService.GetStatusAsync(); }
            catch { _status = new StatusResult(); _status.Indicator = "error"; }
            _statusBusy = false;
            RenderStatus();
        }

        void RenderStatus()
        {
            ThemeColors c = Theme.Current;
            SvcGrid.Children.Clear();
            if (_status == null)
            {
                StatusOvText.Text = "—";
                StatusOvText.Foreground = Br(c.Gray);
                StatusOvDot.Fill = Br(c.Gray);
                StatusTs.Text = "";
                return;
            }

            bool probe = _status.Source == "probe";
            string key;
            string col;
            switch (_status.Indicator)
            {
                case "none": key = probe ? "status.ok_probe" : "status.ok"; col = c.Green; break;
                case "minor": key = probe ? "status.probe_fail" : "status.degraded"; col = c.Yellow; break;
                case "major":
                case "critical": key = "status.outage"; col = c.Red; break;
                case "error": key = "status.error"; col = c.Gray; break;
                default: key = "status.unknown"; col = c.Gray; break;
            }
            StatusOvText.Text = Lang.T(key);
            StatusOvText.Foreground = Br(col);
            StatusOvDot.Fill = Br(col);
            StatusTs.Text = string.Format(Lang.T("status.updated"), _status.Time);

            foreach (StatusComp comp in _status.Components)
            {
                string cc;
                string label;
                switch (comp.Status)
                {
                    case "operational": cc = c.Green; label = Lang.T("status.op"); break;
                    case "degraded_performance": cc = c.Yellow; label = Lang.T("status.deg"); break;
                    case "partial_outage": cc = c.Yellow; label = Lang.T("status.out"); break;
                    case "under_maintenance": cc = c.Yellow; label = comp.Status.Replace('_', ' '); break;
                    case "major_outage": cc = c.Red; label = Lang.T("status.out"); break;
                    default: cc = c.Gray; label = comp.Status.Replace('_', ' '); break;
                }

                var row = new DockPanel();
                row.Margin = new Thickness(0, 4, 12, 4);

                var dot = new Shapes.Ellipse();
                dot.Width = 7;
                dot.Height = 7;
                dot.Margin = new Thickness(0, 0, 8, 0);
                dot.Fill = Br(cc);
                dot.VerticalAlignment = VerticalAlignment.Center;

                var st = new TextBlock();
                st.Text = label;
                st.FontFamily = new FontFamily("Segoe UI");
                st.FontSize = 10;
                st.Foreground = Br(cc);
                st.VerticalAlignment = VerticalAlignment.Center;
                DockPanel.SetDock(st, Dock.Right);

                var nm = new TextBlock();
                nm.Text = comp.Name;
                nm.FontFamily = new FontFamily("Segoe UI");
                nm.FontSize = 11;
                nm.Foreground = Br(c.Text);
                nm.TextTrimming = TextTrimming.CharacterEllipsis;

                row.Children.Add(dot);
                row.Children.Add(st);
                row.Children.Add(nm);
                SvcGrid.Children.Add(row);
            }
        }

        async void BtnRefreshServer_Click(object sender, RoutedEventArgs e)
        {
            await RefreshServerAsync();
        }

        void SetSrvMsg(string key, string kind)
        {
            _srvMsgKey = key;
            _srvMsgKind = kind;
            RenderSrvMsg();
        }

        void RenderSrvMsg()
        {
            ThemeColors c = Theme.Current;
            SrvMsg.Text = Lang.T(_srvMsgKey);
            string col = c.Gray;
            if (_srvMsgKind == "green") col = c.Green;
            else if (_srvMsgKind == "yellow") col = c.Yellow;
            SrvMsg.Foreground = Br(col);
        }

        async Task RefreshServerAsync()
        {
            if (_srvBusy) return;
            _srvBusy = true;
            try
            {
                if (_srv == null) SetSrvMsg("server.searching", "gray");

                RobloxServerInfo info = null;
                try { info = await Task.Run(() => RobloxService.GetCurrentServer()); }
                catch { info = null; }

                if (info == null)
                {
                    _srv = null;
                    _srvGeo = null;
                    SetSrvMsg("server.notfound", "yellow");
                    RenderServer();
                    BuildPingRows();
                    return;
                }

                bool changed = _srv == null || _srv.ServerIp != info.ServerIp || _srv.JobId != info.JobId;
                if (changed || _srvGeo == null)
                {
                    _srv = info;
                    _srvGeo = null;
                    RenderServer();
                    Task<string> nameT = RobloxService.GetGameNameAsync(info.PlaceId);
                    Task<GeoInfo> geoT = RobloxService.GetGeoAsync(info.ServerIp);
                    info.GameName = await nameT;
                    _srvGeo = await geoT;
                }

                bool running = RobloxService.IsRunning();
                SetSrvMsg(running ? "server.found" : "server.found_old", running ? "green" : "yellow");
                RenderServer();
                BuildPingRows();
            }
            finally
            {
                _srvBusy = false;
            }
        }

        void RenderServer()
        {
            if (_srv == null)
            {
                SrvGameCard.Visibility = Visibility.Collapsed;
                SrvGeoCard.Visibility = Visibility.Collapsed;
                return;
            }
            SrvGameCard.Visibility = Visibility.Visible;
            SrvGameNameVal.Text = _srv.GameName ?? Lang.T("server.unknown_game");
            SrvPlaceIdVal.Text = _srv.PlaceId ?? "";
            SrvJobIdVal.Text = _srv.JobId ?? "";
            SrvIpVal.Text = _srv.ServerIp ?? "";

            if (_srvGeo == null)
            {
                SrvGeoCard.Visibility = Visibility.Collapsed;
                return;
            }
            SrvGeoCard.Visibility = Visibility.Visible;
            SrvCountryVal.Text = _srvGeo.Country + " (" + _srvGeo.CountryCode + ")";
            SrvRegionVal.Text = _srvGeo.RegionName;
            SrvCityVal.Text = _srvGeo.City;
            SrvTzVal.Text = _srvGeo.Timezone;
            SrvIspVal.Text = _srvGeo.Isp;
            SrvCoordVal.Text = _srvGeo.Lat.ToString("0.####", CultureInfo.InvariantCulture) + ", " +
                               _srvGeo.Lon.ToString("0.####", CultureInfo.InvariantCulture);
            SrvAsVal.Text = _srvGeo.As;
        }

        async void BtnIpLookup_Click(object sender, RoutedEventArgs e)
        {
            await DoIpLookup();
        }

        async void InputIp_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) await DoIpLookup();
        }

        async Task DoIpLookup()
        {
            string q = InputIp.Text.Trim();
            if (q.Length == 0)
            {
                _ipGeo = null;
                _ipError = "server.invalid";
                RenderIp();
                return;
            }
            BtnIpLookup.IsEnabled = false;
            GeoInfo g = await RobloxService.GetGeoAsync(q);
            _ipGeo = g;
            _ipError = g == null ? "server.lookup_fail" : null;
            RenderIp();
            BtnIpLookup.IsEnabled = true;
        }

        void AddRow(Panel p, string label, string val, Brush valBrush)
        {
            var row = new DockPanel();
            row.Margin = new Thickness(0, 3, 0, 3);

            var l = new TextBlock();
            l.Text = label;
            l.Width = 100;
            l.FontFamily = new FontFamily("Segoe UI");
            l.FontSize = 11;
            l.Foreground = Br(Theme.Current.Gray);

            var v = new TextBlock();
            v.Text = val;
            v.FontFamily = new FontFamily("Segoe UI");
            v.FontSize = 11;
            v.Foreground = valBrush;
            v.TextWrapping = TextWrapping.Wrap;

            row.Children.Add(l);
            row.Children.Add(v);
            p.Children.Add(row);
        }

        void RenderIp()
        {
            ThemeColors c = Theme.Current;
            IpResultPanel.Children.Clear();
            if (_ipGeo == null && _ipError == null)
            {
                IpResultArea.Visibility = Visibility.Collapsed;
                return;
            }
            IpResultArea.Visibility = Visibility.Visible;
            if (_ipGeo == null)
            {
                var t = new TextBlock();
                t.Text = Lang.T(_ipError);
                t.FontFamily = new FontFamily("Segoe UI");
                t.FontSize = 11;
                t.Foreground = Br(c.Red);
                IpResultPanel.Children.Add(t);
                return;
            }
            Brush tx = Br(c.Text);
            AddRow(IpResultPanel, "IP", _ipGeo.Query, Br(c.Accent));
            AddRow(IpResultPanel, Lang.T("server.country"), _ipGeo.Country + " (" + _ipGeo.CountryCode + ")", tx);
            AddRow(IpResultPanel, Lang.T("server.region"), _ipGeo.RegionName, tx);
            AddRow(IpResultPanel, Lang.T("server.city"), _ipGeo.City, tx);
            AddRow(IpResultPanel, Lang.T("server.tz"), _ipGeo.Timezone, tx);
            AddRow(IpResultPanel, Lang.T("server.isp"), _ipGeo.Isp, tx);
            AddRow(IpResultPanel, Lang.T("server.coord"),
                _ipGeo.Lat.ToString("0.####", CultureInfo.InvariantCulture) + ", " +
                _ipGeo.Lon.ToString("0.####", CultureInfo.InvariantCulture), tx);
            AddRow(IpResultPanel, Lang.T("server.as"), _ipGeo.As, tx);
        }

        List<KeyValuePair<string, string>> PingTargets()
        {
            var l = new List<KeyValuePair<string, string>>();
            l.Add(new KeyValuePair<string, string>(Lang.T("ping.roblox"), "www.roblox.com"));
            l.Add(new KeyValuePair<string, string>(Lang.T("ping.api"), "apis.roblox.com"));
            l.Add(new KeyValuePair<string, string>(Lang.T("ping.cdn"), "setup.rbxcdn.com"));
            if (_srv != null && !string.IsNullOrEmpty(_srv.ServerIp))
                l.Add(new KeyValuePair<string, string>(Lang.T("ping.server") + " (" + _srv.ServerIp + ")", _srv.ServerIp));
            return l;
        }

        void BuildPingRows()
        {
            ThemeColors c = Theme.Current;
            PingPanel.Children.Clear();
            foreach (KeyValuePair<string, string> kv in PingTargets())
            {
                string txt = "—";
                string col = c.Gray;
                long r;
                if (_pingRes.TryGetValue(kv.Value, out r))
                {
                    if (r < 0) { txt = Lang.T("server.timeout"); col = c.Red; }
                    else
                    {
                        txt = r + " ms";
                        col = r < 80 ? c.Green : (r < 160 ? c.Yellow : c.Red);
                    }
                }
                else if (_pingBusy)
                {
                    txt = Lang.T("server.pinging");
                }

                var row = new DockPanel();
                row.Margin = new Thickness(0, 4, 0, 4);

                var val = new TextBlock();
                val.Text = txt;
                val.FontFamily = new FontFamily("Segoe UI");
                val.FontSize = 11;
                val.FontWeight = FontWeights.Bold;
                val.Foreground = Br(col);
                DockPanel.SetDock(val, Dock.Right);

                var nm = new TextBlock();
                nm.Text = kv.Key;
                nm.FontFamily = new FontFamily("Segoe UI");
                nm.FontSize = 11;
                nm.Foreground = Br(c.Text);

                row.Children.Add(val);
                row.Children.Add(nm);
                PingPanel.Children.Add(row);
            }
        }

        async void BtnPing_Click(object sender, RoutedEventArgs e)
        {
            if (_pingBusy) return;
            _pingBusy = true;
            BtnPing.IsEnabled = false;
            _pingRes.Clear();
            BuildPingRows();
            foreach (KeyValuePair<string, string> kv in PingTargets())
            {
                long r = await RobloxService.PingAsync(kv.Value);
                _pingRes[kv.Value] = r;
                BuildPingRows();
            }
            _pingBusy = false;
            BtnPing.IsEnabled = true;
            BuildPingRows();
        }

        void Lang_Click(object sender, RoutedEventArgs e)
        {
            _s.Language = RbZh.IsChecked == true ? AppLanguage.ZhTW : AppLanguage.English;
            Lang.Set(_s.Language);
            SettingsService.Save(_s);
            ApplyLang();
        }

        void Theme_Click(object sender, MouseButtonEventArgs e)
        {
            Border b = sender as Border;
            if (b == null) return;
            AppTheme t;
            if (Enum.TryParse<AppTheme>(b.Tag as string, out t))
            {
                _s.Theme = t;
                Theme.Apply(t);
                SettingsService.Save(_s);
                ApplyTheme();
                MarkSelectedTheme();
            }
        }

        void MarkSelectedTheme()
        {
            Border[] cards = { TCardCyber, TCardMidnight, TCardMatrix, TCardCrimson, TCardArctic };
            foreach (Border b in cards)
            {
                bool sel = (b.Tag as string) == _s.Theme.ToString();
                b.BorderThickness = new Thickness(sel ? 3 : 1);
            }
        }

        void Anim_Click(object sender, RoutedEventArgs e)
        {
            _s.Animations = ChkAnim.IsChecked == true;
            Motion.Enabled = _s.Animations;
            SettingsService.Save(_s);
            UpdateBars();
        }

        void AutoStart_Click(object sender, RoutedEventArgs e)
        {
            _s.AutoStart = ChkAutoStart.IsChecked == true;
            SettingsService.Save(_s);
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (k != null)
                    {
                        if (_s.AutoStart)
                        {
                            string exe = Environment.ProcessPath ?? "";
                            k.SetValue("VortexX", "\"" + exe + "\"");
                        }
                        else
                        {
                            k.DeleteValue("VortexX", false);
                        }
                    }
                }
            }
            catch { }
        }
    }
}
