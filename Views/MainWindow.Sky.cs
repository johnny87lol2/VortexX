using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using VortexX.Models;
using VortexX.Services;

namespace VortexX.Views
{
    // Skybox 更換頁面
    public partial class MainWindow
    {
        void ThemeSky()
        {
            ThemeColors c = Theme.Current;
            Brush bg = Br(c.Bg), bg1 = Br(c.Bg1), card = Br(c.Card), bd = Br(c.Border);
            Brush ac = Br(c.Accent), tx = Br(c.Text), gr = Br(c.Gray);
            Cards(card, bd, SkyCard);
            Fill(bd, SkyCardSep);
            Fore(tx, TxtSkyTitle);
            Fore(gr, SkyCardTitle, SkyDesc, SkyDropSub);
            Fore(ac, SkyDropIcon, SkyDropText);
            SkyDrop.Background = BrA(c.Accent, 14);
            SkyDrop.BorderBrush = BrA(c.Accent, 90);
            BtnStyle(bg1, ac, bd, BtnSkyReset);
        }

        void LangSky()
        {
            BtnSky.Content = Lang.T("nav.sky");
            TxtSkyTitle.Text = Lang.T("sky.title");
            SkyCardTitle.Text = Lang.T("sky.card");
            SkyDesc.Text = Lang.T("sky.desc");
            SkyDropText.Text = Lang.T("sky.drop");
            SkyDropSub.Text = Lang.T("sky.dropsub");
            BtnSkyReset.Content = Lang.T("sky.reset");
        }

        void RefreshSky() { }

        // ───────── 拖曳 .tex：檔名跟 Roblox 原本一樣就直接替換 ─────────
        void SkyDrop_Enter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                SkyDrop.BorderBrush = Br(Theme.Current.Accent);
                SkyDrop.Background = BrA(Theme.Current.Accent, 40);
            }
            e.Handled = true;
        }

        void SkyDrop_Leave(object sender, DragEventArgs e)
        {
            SkyDrop.BorderBrush = BrA(Theme.Current.Accent, 90);
            SkyDrop.Background = BrA(Theme.Current.Accent, 14);
        }

        void SkyDrop_Drop(object sender, DragEventArgs e)
        {
            SkyDrop_Leave(sender, e);
            ThemeColors c = Theme.Current;
            string[] dropped = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (dropped == null) return;
            try
            {
                if (!RobloxService.IsInstalled(_s.InstallDir))
                {
                    SkyStatus.Foreground = Br(c.Yellow);
                    SkyStatus.Text = Lang.T("mods.notinstalled");
                    return;
                }
                // 展開資料夾
                var files = new List<string>();
                foreach (string d in dropped)
                {
                    if (Directory.Exists(d)) files.AddRange(Directory.GetFiles(d, "*", SearchOption.AllDirectories));
                    else if (File.Exists(d)) files.Add(d);
                }

                // 安裝資料夾裡所有跟天空有關的 .tex（路徑含 sky 或檔名以 sky 開頭），用檔名對應
                string root = _s.InstallDir;
                var index = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (string f in Directory.GetFiles(root, "*.tex", SearchOption.AllDirectories))
                {
                    string rel = f.Substring(root.Length).TrimStart('\\', '/');
                    string name = Path.GetFileName(f);
                    if (rel.IndexOf("sky", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    List<string> l;
                    if (!index.TryGetValue(name, out l)) { l = new List<string>(); index[name] = l; }
                    l.Add(rel);
                }

                var done = new List<string>();
                foreach (string f in files)
                {
                    List<string> rels;
                    if (!index.TryGetValue(Path.GetFileName(f), out rels)) continue;
                    foreach (string rel in rels)
                    {
                        string dest = Path.Combine(ModService.ModsDir, rel);
                        Directory.CreateDirectory(Path.GetDirectoryName(dest));
                        File.Copy(f, dest, true);
                    }
                    done.Add(Path.GetFileName(f));
                }

                if (done.Count == 0)
                {
                    SkyStatus.Foreground = Br(c.Yellow);
                    SkyStatus.Text = Lang.T("sky.dropnone");
                    return;
                }
                _s.ApplyMods = true;
                SettingsService.Save(_s);
                ModService.Apply(root, null);
                SkyStatus.Foreground = Br(c.Green);
                SkyStatus.Text = string.Format(Lang.T("sky.dropdone"), done.Count, string.Join(", ", done));
            }
            catch (Exception ex)
            {
                SkyStatus.Foreground = Br(c.Red);
                SkyStatus.Text = ex.Message;
            }
        }

        void BtnSkyReset_Click(object sender, RoutedEventArgs e)
        {
            ThemeColors c = Theme.Current;
            try
            {
                foreach (string file in Directory.GetFiles(ModService.ModsDir, "sky512_*.tex", SearchOption.AllDirectories))
                    File.Delete(file);
                if (RobloxService.IsInstalled(_s.InstallDir)) ModService.Apply(_s.InstallDir, null);
                SkyStatus.Foreground = Br(c.Gray);
                SkyStatus.Text = Lang.T("sky.resetdone");
            }
            catch (Exception ex)
            {
                SkyStatus.Foreground = Br(c.Red);
                SkyStatus.Text = ex.Message;
            }
        }
    }
}
