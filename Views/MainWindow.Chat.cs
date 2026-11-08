using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using VortexX.Models;
using VortexX.Services;

namespace VortexX.Views
{
    // AI Chat 頁面
    public partial class MainWindow
    {
        readonly List<ChatMsg> _chat = new List<ChatMsg>();
        bool _chatBusy;
        List<string> _modelList = new List<string>();
        DateTime _chatStart;
        DispatcherTimer _chatTimer;
        TextBox _thinkingBox;

        string ThinkingText()
        {
            return Lang.T("chat.thinking") + " " + (int)(DateTime.Now - _chatStart).TotalSeconds + "s";
        }

        void StartChatTimer()
        {
            _chatStart = DateTime.Now;
            if (_chatTimer == null)
            {
                _chatTimer = new DispatcherTimer();
                _chatTimer.Interval = TimeSpan.FromSeconds(1);
                _chatTimer.Tick += (sender2, args2) =>
                {
                    if (_chatBusy && _thinkingBox != null) _thinkingBox.Text = ThinkingText();
                };
            }
            _chatTimer.Start();
        }

        // ═════════ 設定讀寫 ═════════
        AiProvider CurProvider()
        {
            AiProvider p;
            if (!Enum.TryParse<AiProvider>(_s.AiProvider, out p)) p = AiProvider.Anthropic;
            return p;
        }

        string SavedKey(AiProvider p)
        {
            string enc;
            if (_s.AiKeys != null && _s.AiKeys.TryGetValue(p.ToString(), out enc)) return AiService.Unprotect(enc);
            return "";
        }

        string EffectiveKey(AiProvider p)
        {
            string typed = ChatKeyBox.Password.Trim();
            return typed.Length > 0 ? typed : SavedKey(p);
        }

        string SavedModel(AiProvider p)
        {
            string m;
            if (_s.AiModels != null && _s.AiModels.TryGetValue(p.ToString(), out m) && !string.IsNullOrWhiteSpace(m)) return m;
            return AiService.DefaultModel(p);
        }

        void RefreshChatConfig()
        {
            AiProvider p = CurProvider();
            ChatBaseUrlPanel.Visibility = p == AiProvider.Custom ? Visibility.Visible : Visibility.Collapsed;
            ChatBaseUrlBox.Text = _s.AiBaseUrl ?? "";
            ChatModelBox.Text = SavedModel(p);
            ChatKeyBox.Password = "";
            ChatModelStatus.Text = "";
            _modelList = new List<string>(AiService.PresetModels(p));
            BuildModelChips();
            MarkProvider();
            UpdateKeyStatus();
        }

        void MarkProvider()
        {
            ThemeColors c = Theme.Current;
            Button[] bs = { BtnProvAnthropic, BtnProvOpenAI, BtnProvGemini, BtnProvCustom };
            string cur = CurProvider().ToString();
            foreach (Button b in bs)
            {
                bool act = (b.Tag as string) == cur;
                b.Background = act ? BrA(c.Accent, 40) : Br(c.Bg1);
                b.BorderBrush = act ? Br(c.Accent) : Br(c.Border);
                b.Foreground = act ? Br(c.Accent) : Br(c.Gray);
            }
        }

        void UpdateKeyStatus()
        {
            ThemeColors c = Theme.Current;
            bool has = SavedKey(CurProvider()).Length > 0;
            ChatKeyStatus.Text = Lang.T(has ? "chat.key_saved" : "chat.key_missing");
            ChatKeyStatus.Foreground = Br(has ? c.Green : c.Yellow);
        }

        void BuildModelChips()
        {
            ThemeColors c = Theme.Current;
            string cur = ChatModelBox.Text.Trim();
            ChatModelChips.Children.Clear();
            foreach (string m in _modelList)
            {
                bool sel = m == cur;
                var b = new Button();
                b.Content = m;
                b.Tag = m;
                b.Padding = new Thickness(8, 3, 8, 3);
                b.Margin = new Thickness(0, 0, 6, 6);
                b.BorderThickness = new Thickness(1);
                b.FontFamily = new FontFamily("Segoe UI");
                b.FontSize = 10;
                b.Background = sel ? BrA(c.Accent, 40) : Br(c.Bg1);
                b.BorderBrush = sel ? Br(c.Accent) : Br(c.Border);
                b.Foreground = sel ? Br(c.Accent) : Br(c.Gray);
                b.Click += ModelChip_Click;
                ChatModelChips.Children.Add(b);
            }
        }

        void SaveModel()
        {
            string m = ChatModelBox.Text.Trim();
            if (_s.AiModels == null) _s.AiModels = new Dictionary<string, string>();
            _s.AiModels[CurProvider().ToString()] = m;
            SettingsService.Save(_s);
            BuildModelChips();
        }

        // ═════════ 事件 ═════════
        void Provider_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            if (b == null) return;
            _s.AiProvider = b.Tag as string;
            SettingsService.Save(_s);
            RefreshChatConfig();
        }

        void ModelChip_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            if (b == null) return;
            ChatModelBox.Text = b.Tag as string;
            SaveModel();
        }

        void ChatModelBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SaveModel();
        }

        void ChatBaseUrlBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _s.AiBaseUrl = ChatBaseUrlBox.Text.Trim();
            SettingsService.Save(_s);
        }

        async void BtnChatSaveKey_Click(object sender, RoutedEventArgs e)
        {
            AiProvider p = CurProvider();
            string key = ChatKeyBox.Password.Trim();
            if (_s.AiKeys == null) _s.AiKeys = new Dictionary<string, string>();
            if (key.Length == 0) _s.AiKeys.Remove(p.ToString());
            else _s.AiKeys[p.ToString()] = AiService.Protect(key);
            SettingsService.Save(_s);
            ChatKeyBox.Password = "";
            UpdateKeyStatus();

            // 存好金鑰後自動載入這個帳號真正能用的模型，避免用到已下架的舊模型
            if (key.Length > 0) await FetchModels(true);
        }

        async void BtnChatFetch_Click(object sender, RoutedEventArgs e)
        {
            await FetchModels(true);
        }

        static string PickModel(List<string> list)
        {
            foreach (string m in list)
                if (m.ToLowerInvariant().Contains("flash") || m.ToLowerInvariant().Contains("mini") || m.ToLowerInvariant().Contains("sonnet")) return m;
            return list[0];
        }

        async Task FetchModels(bool replaceStaleModel)
        {
            ThemeColors c = Theme.Current;
            AiProvider p = CurProvider();
            string key = EffectiveKey(p);
            if (p != AiProvider.Custom && key.Length == 0)
            {
                ChatModelStatus.Text = Lang.T("chat.nokey");
                ChatModelStatus.Foreground = Br(c.Yellow);
                return;
            }
            ChatModelStatus.Text = Lang.T("status.loading");
            ChatModelStatus.Foreground = Br(c.Gray);
            BtnChatFetch.IsEnabled = false;
            try
            {
                List<string> list = await AiService.ListModelsAsync(p, key, _s.AiBaseUrl);
                _modelList = list;
                // 目前選的模型不在清單裡（例如已下架），就換成清單裡適合的一個（自訂服務商清單不完整，不自動換）
                if (replaceStaleModel && p != AiProvider.Custom && list.Count > 0 && !list.Contains(ChatModelBox.Text.Trim()))
                {
                    ChatModelBox.Text = PickModel(list);
                    SaveModel();
                }
                BuildModelChips();
                ChatModelStatus.Text = string.Format(Lang.T("chat.models_ok"), list.Count);
                ChatModelStatus.Foreground = Br(c.Green);
            }
            catch (Exception ex)
            {
                ChatModelStatus.Text = string.Format(Lang.T("chat.models_fail"), ex.Message);
                ChatModelStatus.Foreground = Br(c.Red);
            }
            BtnChatFetch.IsEnabled = true;
        }

        void BtnChatClear_Click(object sender, RoutedEventArgs e)
        {
            _chat.Clear();
            RenderChat();
        }

        async void BtnChatSend_Click(object sender, RoutedEventArgs e)
        {
            await SendChat();
        }

        async void ChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
            {
                e.Handled = true;
                await SendChat();
            }
        }

        // ═════════ 送出訊息 ═════════
        static bool LooksLikeApiKey(string s)
        {
            if (s.Length < 30 || s.Length > 300) return false;
            foreach (char ch in s)
            {
                bool ok = char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == '.';
                if (!ok) return false;
            }
            return true;
        }

        string BuildSystemPrompt()
        {
            var st = new JObject();
            st["theme"] = _s.Theme.ToString();
            st["language"] = _s.Language.ToString();
            st["flushDns"] = _s.FlushDns;
            st["powerMode"] = _s.PowerMode;
            st["autoStart"] = _s.AutoStart;
            st["installDir"] = _s.InstallDir;
            st["robloxInstalled"] = RobloxService.IsInstalled(_s.InstallDir);
            st["installedVersion"] = _installedVer ?? "";
            st["latestVersion"] = _latestVer ?? "";

            return "You are the built-in assistant of VortexX, a custom Roblox launcher for Windows. " +
                "Help the user with Roblox, with VortexX features (install manager, launch options, Roblox status, server info, settings) and with changing VortexX settings. " +
                "Reply in the same language the user writes in, and keep answers short and practical.\n\n" +
                "To change a VortexX setting, put one action block per change in your reply, exactly in this format:\n" +
                "```vortex-action\n{\"action\":\"set_theme\",\"value\":\"Matrix\"}\n```\n\n" +
                "Allowed actions and values:\n" +
                "- set_theme: Cyber, Midnight, Matrix, Crimson, Arctic\n" +
                "- set_language: English, ZhTW\n" +
                "- set_flush_dns: true or false (flush the DNS cache before launching Roblox)\n" +
                "- set_power_mode: true or false (switch to the high-performance power plan on launch)\n" +
                "- set_autostart: true or false (start VortexX with Windows)\n" +
                "- navigate: Home, Install, Launch, Status, Server, Chat, Settings\n\n" +
                "The app shows each action to the user with an Apply button, so nothing changes until the user confirms. " +
                "Never say a setting has already been changed. Do not invent other actions. Never ask for or repeat API keys.\n\n" +
                "Current settings: " + st.ToString(Newtonsoft.Json.Formatting.None);
        }

        async Task SendChat()
        {
            if (_chatBusy) return;
            string txt = ChatInput.Text.Trim();
            if (txt.Length == 0) return;

            // 金鑰貼錯地方：不要把它當成訊息送出去
            if (LooksLikeApiKey(txt))
            {
                ChatInput.Text = "";
                ChatMsg warn = new ChatMsg();
                warn.Role = "assistant";
                warn.IsError = true;
                warn.Text = Lang.T("chat.looks_like_key");
                _chat.Add(warn);
                RenderChat();
                return;
            }

            AiProvider p = CurProvider();
            string key = EffectiveKey(p);
            string model = ChatModelBox.Text.Trim();
            if (model.Length == 0) model = AiService.DefaultModel(p);

            if (p != AiProvider.Custom && key.Length == 0)
            {
                ChatMsg err = new ChatMsg();
                err.Role = "assistant";
                err.IsError = true;
                err.Text = Lang.T("chat.nokey");
                _chat.Add(err);
                RenderChat();
                return;
            }

            ChatMsg um = new ChatMsg();
            um.Role = "user";
            um.Text = txt;
            _chat.Add(um);
            ChatInput.Text = "";
            _chatBusy = true;
            BtnChatSend.IsEnabled = false;
            StartChatTimer();
            RenderChat();

            try
            {
                var hist = new List<ChatMsg>();
                foreach (ChatMsg m in _chat) if (!m.IsError) hist.Add(m);
                if (hist.Count > 24) hist = hist.GetRange(hist.Count - 24, 24);
                while (hist.Count > 0 && hist[0].Role != "user") hist.RemoveAt(0);

                string reply = await AiService.ChatAsync(p, model, key, _s.AiBaseUrl, BuildSystemPrompt(), hist);

                var acts = new List<JObject>();
                string clean = AiService.ExtractActions(reply, acts);

                ChatMsg am = new ChatMsg();
                am.Role = "assistant";
                am.Text = clean;
                foreach (JObject a in acts)
                {
                    if (DescribeAction(a) != null)
                    {
                        ChatAction ca = new ChatAction();
                        ca.Json = a;
                        am.Actions.Add(ca);
                    }
                }
                if (am.Text.Length == 0 && am.Actions.Count == 0) am.Text = reply;
                _chat.Add(am);
            }
            catch (Exception ex)
            {
                ChatMsg em = new ChatMsg();
                em.Role = "assistant";
                em.IsError = true;
                em.Text = string.Format(Lang.T("chat.error"), ex.Message);
                _chat.Add(em);
            }
            finally
            {
                _chatBusy = false;
                BtnChatSend.IsEnabled = true;
                if (_chatTimer != null) _chatTimer.Stop();
            }
            RenderChat();
        }

        // ═════════ 畫出對話 ═════════
        UIElement MakeBubble(string text, bool user, bool isError, ThemeColors c)
        {
            var tb = new TextBox();
            tb.Text = text;
            tb.IsReadOnly = true;
            tb.TextWrapping = TextWrapping.Wrap;
            tb.BorderThickness = new Thickness(0);
            tb.Background = Brushes.Transparent;
            tb.Foreground = Br(isError ? c.Red : c.Text);
            tb.CaretBrush = Br(c.Accent);
            tb.FontFamily = new FontFamily("Consolas");
            tb.FontSize = 12;

            var b = new Border();
            b.Child = tb;
            b.Padding = new Thickness(10);
            b.CornerRadius = new CornerRadius(4);
            b.BorderThickness = new Thickness(1);
            b.Margin = new Thickness(user ? 48 : 0, 0, user ? 0 : 48, 8);
            b.Background = user ? BrA(c.Accent, 30) : Br(c.Bg1);
            b.BorderBrush = user ? Br(c.Accent) : Br(isError ? c.Red : c.Border);
            return b;
        }

        void RenderChat()
        {
            ThemeColors c = Theme.Current;
            ChatMessages.Children.Clear();

            foreach (ChatMsg m in _chat)
            {
                bool user = m.Role == "user";
                if (m.Text.Length > 0 || m.Actions.Count == 0)
                    ChatMessages.Children.Add(MakeBubble(m.Text, user, m.IsError, c));

                foreach (ChatAction a in m.Actions)
                {
                    string desc = DescribeAction(a.Json);
                    if (desc == null) continue;

                    var row = new DockPanel();

                    var btn = new Button();
                    btn.Content = Lang.T(a.Applied ? "chat.applied" : "chat.apply");
                    btn.IsEnabled = !a.Applied;
                    btn.Padding = new Thickness(12, 5, 12, 5);
                    btn.BorderThickness = new Thickness(1);
                    btn.FontFamily = new FontFamily("Segoe UI");
                    btn.FontSize = 11;
                    btn.Background = Br(c.Accent);
                    btn.Foreground = Br(c.Bg);
                    btn.BorderBrush = Br(c.Accent);
                    DockPanel.SetDock(btn, Dock.Right);
                    ChatAction act = a;
                    btn.Click += (sender2, args2) =>
                    {
                        if (ApplyAiAction(act.Json)) act.Applied = true;
                        RenderChat();
                    };

                    var label = new TextBlock();
                    label.Text = desc;
                    label.FontFamily = new FontFamily("Segoe UI");
                    label.FontSize = 11;
                    label.Foreground = Br(c.Text);
                    label.TextWrapping = TextWrapping.Wrap;
                    label.VerticalAlignment = VerticalAlignment.Center;
                    label.Margin = new Thickness(0, 0, 10, 0);

                    row.Children.Add(btn);
                    row.Children.Add(label);

                    var card = new Border();
                    card.Child = row;
                    card.Padding = new Thickness(10, 8, 10, 8);
                    card.CornerRadius = new CornerRadius(4);
                    card.BorderThickness = new Thickness(1);
                    card.BorderBrush = Br(c.Accent);
                    card.Background = BrA(c.Accent, 18);
                    card.Margin = new Thickness(0, 0, 48, 8);
                    ChatMessages.Children.Add(card);
                }
            }

            _thinkingBox = null;
            if (_chatBusy)
            {
                UIElement bubble = MakeBubble(ThinkingText(), false, false, c);
                Border bb = bubble as Border;
                if (bb != null) _thinkingBox = bb.Child as TextBox;
                ChatMessages.Children.Add(bubble);
            }

            ChatScroll.ScrollToEnd();
        }

        // ═════════ 設定動作（只接受白名單裡的動作） ═════════
        static string OnOff(bool b) { return Lang.T(b ? "chat.on" : "chat.off"); }

        static bool ParseTheme(string v, out AppTheme t)
        {
            return Enum.TryParse<AppTheme>(v, true, out t) && Enum.IsDefined(typeof(AppTheme), t);
        }

        static bool ParseLanguage(string v, out AppLanguage l)
        {
            l = AppLanguage.English;
            string s = (v ?? "").Trim().ToLowerInvariant();
            if (s == "english" || s == "en") { l = AppLanguage.English; return true; }
            if (s == "zhtw" || s == "zh-tw" || s == "zh" || s == "chinese") { l = AppLanguage.ZhTW; return true; }
            return false;
        }

        static readonly string[] Pages = { "Home", "Install", "Launch", "Status", "Server", "Chat", "Settings" };

        static string FindPage(string v)
        {
            foreach (string p in Pages)
                if (string.Equals(p, v, StringComparison.OrdinalIgnoreCase)) return p;
            return null;
        }

        static string ActionValue(JObject a)
        {
            JToken t = a["value"];
            return t == null ? "" : t.ToString();
        }

        /// <summary>Returns a readable description, or null when the action is not valid.</summary>
        string DescribeAction(JObject a)
        {
            string act = a.Value<string>("action");
            string v = ActionValue(a);
            bool b;
            AppTheme t;
            AppLanguage l;
            switch (act)
            {
                case "set_theme":
                    if (ParseTheme(v, out t)) return string.Format(Lang.T("chat.act.theme"), t);
                    break;
                case "set_language":
                    if (ParseLanguage(v, out l)) return string.Format(Lang.T("chat.act.language"), l == AppLanguage.ZhTW ? "繁體中文" : "English");
                    break;
                case "set_flush_dns":
                    if (bool.TryParse(v, out b)) return string.Format(Lang.T("chat.act.flushdns"), OnOff(b));
                    break;
                case "set_power_mode":
                    if (bool.TryParse(v, out b)) return string.Format(Lang.T("chat.act.power"), OnOff(b));
                    break;
                case "set_autostart":
                    if (bool.TryParse(v, out b)) return string.Format(Lang.T("chat.act.autostart"), OnOff(b));
                    break;
                case "navigate":
                    string page = FindPage(v);
                    if (page != null) return string.Format(Lang.T("chat.act.navigate"), page);
                    break;
            }
            return null;
        }

        bool ApplyAiAction(JObject a)
        {
            string act = a.Value<string>("action");
            string v = ActionValue(a);
            bool b;
            AppTheme t;
            AppLanguage l;
            switch (act)
            {
                case "set_theme":
                    if (!ParseTheme(v, out t)) return false;
                    _s.Theme = t;
                    Theme.Apply(t);
                    SettingsService.Save(_s);
                    ApplyTheme();
                    MarkSelectedTheme();
                    return true;
                case "set_language":
                    if (!ParseLanguage(v, out l)) return false;
                    _s.Language = l;
                    Lang.Set(l);
                    RbZh.IsChecked = l == AppLanguage.ZhTW;
                    RbEn.IsChecked = l != AppLanguage.ZhTW;
                    SettingsService.Save(_s);
                    ApplyLang();
                    return true;
                case "set_flush_dns":
                    if (!bool.TryParse(v, out b)) return false;
                    _s.FlushDns = b;
                    ChkFlushDns.IsChecked = b;
                    SettingsService.Save(_s);
                    return true;
                case "set_power_mode":
                    if (!bool.TryParse(v, out b)) return false;
                    _s.PowerMode = b;
                    ChkPowerMode.IsChecked = b;
                    SettingsService.Save(_s);
                    return true;
                case "set_autostart":
                    if (!bool.TryParse(v, out b)) return false;
                    SetAutoStart(b);
                    return true;
                case "navigate":
                    string page = FindPage(v);
                    if (page == null) return false;
                    ShowPage(page);
                    return true;
            }
            return false;
        }

        void SetAutoStart(bool on)
        {
            _s.AutoStart = on;
            ChkAutoStart.IsChecked = on;
            SettingsService.Save(_s);
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (k != null)
                    {
                        if (on) k.SetValue("VortexX", "\"" + (Environment.ProcessPath ?? "") + "\"");
                        else k.DeleteValue("VortexX", false);
                    }
                }
            }
            catch { }
        }
    }
}
