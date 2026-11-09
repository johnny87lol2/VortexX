using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VortexX.Models;
using VortexX.Services;

namespace VortexX.Views
{
    public partial class MainWindow
    {
        static readonly string[] PresetKeys =
        {
            "DFIntTaskSchedulerTargetFps", "FIntDebugForceMSAASamples",
            "DFFlagTextureQualityOverrideEnabled", "DFIntTextureQualityOverride",
            "FFlagDisablePostFx", "FIntFRMMinGrassDistance", "FIntFRMMaxGrassDistance",
            "FIntRenderGrassDetailStrips", "FIntRenderShadowIntensity", "DFFlagDisableDPIScale"
        };

        void ThemeMods()
        {
            ThemeColors c = Theme.Current;
            Brush bg = Br(c.Bg), bg1 = Br(c.Bg1), card = Br(c.Card), bd = Br(c.Border);
            Brush ac = Br(c.Accent), tx = Br(c.Text), gr = Br(c.Gray);

            Cards(card, bd, ModsInfoCard, ModsFilesCard, EngPresetCard, EngJsonCard);
            Cards(bg, bd, ModsPathBox);
            Fill(bd, ModsInfoSep, ModsFilesSep, EngPresetSep, EngJsonSep);
            Fore(tx, TxtModsTitle, TxtEngTitle, ModsPathVal);
            Fore(gr, ModsInfoTitle, ModsFilesTitle, EngPresetTitle, EngJsonTitle, ModsDesc, ModsFilesText,
                 EngDesc, EngJsonDesc, EngFpsLbl, EngMsaaLbl, EngTexLbl);
            ForeC(tx, ChkApplyMods, EngNoPostFx, EngNoGrass, EngNoShadow, EngNoDpi);
            foreach (TextBox t in new TextBox[] { EngFps, EngMsaa, EngTex, EngJsonBox })
            {
                t.Background = bg; t.Foreground = tx; t.BorderBrush = bd; t.CaretBrush = ac;
            }
            BtnStyle(bg1, ac, bd, BtnOpenMods, BtnApplyMods, BtnEngReset);
            BtnStyle(ac, bg, ac, BtnEngSave);

            Cards(card, bd, StgUpdCard);
            Fill(bd, StgUpdSep);
            Fore(gr, StgUpdTitle, StgUpdVer);
            BtnStyle(bg1, ac, bd, BtnCheckUpdate);
        }

        void LangMods()
        {
            StgUpdTitle.Text = Lang.T("upd.title");
            BtnCheckUpdate.Content = Lang.T("upd.check");
            ShowLocalVersion();
            BtnMods.Content = Lang.T("nav.mods");
            BtnEngine.Content = Lang.T("nav.engine");

            TxtModsTitle.Text = Lang.T("mods.title");
            ModsInfoTitle.Text = Lang.T("mods.card");
            ModsDesc.Text = Lang.T("mods.desc");
            BtnOpenMods.Content = Lang.T("mods.open");
            BtnApplyMods.Content = Lang.T("mods.apply");
            ChkApplyMods.Content = Lang.T("mods.auto");
            ModsFilesTitle.Text = Lang.T("mods.files");

            TxtEngTitle.Text = Lang.T("eng.title");
            EngPresetTitle.Text = Lang.T("eng.presets");
            EngDesc.Text = Lang.T("eng.desc");
            EngFpsLbl.Text = Lang.T("eng.fps");
            EngMsaaLbl.Text = Lang.T("eng.msaa");
            EngTexLbl.Text = Lang.T("eng.tex");
            EngNoPostFx.Content = Lang.T("eng.nopostfx");
            EngNoGrass.Content = Lang.T("eng.nograss");
            EngNoShadow.Content = Lang.T("eng.noshadow");
            EngNoDpi.Content = Lang.T("eng.nodpi");
            EngJsonTitle.Text = Lang.T("eng.json");
            EngJsonDesc.Text = Lang.T("eng.jsondesc");
            BtnEngSave.Content = Lang.T("eng.save");
            BtnEngReset.Content = Lang.T("eng.reset");
        }

        void RefreshMods()
        {
            ModService.EnsureFolder();
            ModsPathVal.Text = ModService.ModsDir;
            ChkApplyMods.IsChecked = _s.ApplyMods;
            List<string> files = ModService.ListFiles();
            ModsFilesText.Text = files.Count == 0 ? Lang.T("mods.empty") : string.Join("\n", files);
        }

        void BtnOpenMods_Click(object sender, RoutedEventArgs e)
        {
            try { ModService.OpenFolder(); }
            catch (Exception ex) { ModsStatus.Text = ex.Message; }
        }

        void BtnApplyMods_Click(object sender, RoutedEventArgs e)
        {
            ThemeColors c = Theme.Current;
            if (!RobloxService.IsInstalled(_s.InstallDir))
            {
                ModsStatus.Foreground = Br(c.Yellow);
                ModsStatus.Text = Lang.T("mods.notinstalled");
                return;
            }
            int n = ModService.Apply(_s.InstallDir, null);
            ModsStatus.Foreground = Br(c.Green);
            ModsStatus.Text = string.Format(Lang.T("log.mods"), n);
            RefreshMods();
        }

        void ChkApplyMods_Click(object sender, RoutedEventArgs e)
        {
            _s.ApplyMods = ChkApplyMods.IsChecked == true;
            SettingsService.Save(_s);
        }

        static string FlagStr(JObject o, string key)
        {
            JToken t = o[key];
            return t == null ? "" : t.ToString();
        }

        void LoadEngine()
        {
            JObject o = FlagService.Load();
            EngFps.Text = _s.FpsCap > 0 ? _s.FpsCap.ToString() : "";
            EngMsaa.Text = FlagStr(o, "FIntDebugForceMSAASamples");
            EngTex.Text = FlagStr(o, "DFFlagTextureQualityOverrideEnabled").ToLowerInvariant() == "true"
                ? FlagStr(o, "DFIntTextureQualityOverride") : "";
            EngNoPostFx.IsChecked = FlagStr(o, "FFlagDisablePostFx").ToLowerInvariant() == "true";
            EngNoGrass.IsChecked = FlagStr(o, "FIntFRMMaxGrassDistance") == "0";
            EngNoShadow.IsChecked = FlagStr(o, "FIntRenderShadowIntensity") == "0";
            EngNoDpi.IsChecked = FlagStr(o, "DFFlagDisableDPIScale").ToLowerInvariant() == "true";
            EngJsonBox.Text = o.Count == 0 ? "{\n}" : o.ToString(Formatting.Indented);
        }

        static void SetFlag(JObject o, string key, string val)
        {
            if (string.IsNullOrWhiteSpace(val)) o.Remove(key);
            else o[key] = val.Trim();
        }

        void BtnEngSave_Click(object sender, RoutedEventArgs e)
        {
            ThemeColors c = Theme.Current;
            JObject o;
            try { o = JObject.Parse(string.IsNullOrWhiteSpace(EngJsonBox.Text) ? "{}" : EngJsonBox.Text); }
            catch (Exception ex)
            {
                EngStatus.Foreground = Br(c.Red);
                EngStatus.Text = Lang.T("eng.badjson") + " " + ex.Message;
                return;
            }

            foreach (string k in PresetKeys) o.Remove(k);

            int dummy;
            string fps = EngFps.Text.Trim();
            if (fps.Length > 0 && !int.TryParse(fps, out dummy)) { EngStatus.Foreground = Br(c.Red); EngStatus.Text = Lang.T("eng.badnum"); return; }
            string msaa = EngMsaa.Text.Trim();
            if (msaa.Length > 0 && !int.TryParse(msaa, out dummy)) { EngStatus.Foreground = Br(c.Red); EngStatus.Text = Lang.T("eng.badnum"); return; }
            string tex = EngTex.Text.Trim();
            if (tex.Length > 0 && !int.TryParse(tex, out dummy)) { EngStatus.Foreground = Br(c.Red); EngStatus.Text = Lang.T("eng.badnum"); return; }

            int capVal = 0;
            if (fps.Length > 0) capVal = Math.Max(30, Math.Min(999, int.Parse(fps)));
            _s.FpsCap = capVal;
            EngFps.Text = capVal > 0 ? capVal.ToString() : "";
            SettingsService.Save(_s);
            SetFlag(o, "FIntDebugForceMSAASamples", msaa);
            if (tex.Length > 0)
            {
                o["DFFlagTextureQualityOverrideEnabled"] = "True";
                o["DFIntTextureQualityOverride"] = tex;
            }
            if (EngNoPostFx.IsChecked == true) o["FFlagDisablePostFx"] = "True";
            if (EngNoGrass.IsChecked == true)
            {
                o["FIntFRMMinGrassDistance"] = "0";
                o["FIntFRMMaxGrassDistance"] = "0";
                o["FIntRenderGrassDetailStrips"] = "0";
            }
            if (EngNoShadow.IsChecked == true) o["FIntRenderShadowIntensity"] = "0";
            if (EngNoDpi.IsChecked == true) o["DFFlagDisableDPIScale"] = "True";

            foreach (JProperty p in new List<JProperty>(o.Properties()))
            {
                if (p.Value.Type == JTokenType.Boolean) p.Value = (bool)p.Value ? "True" : "False";
                else if (p.Value.Type != JTokenType.String) p.Value = p.Value.ToString();
            }

            try
            {
                FlagService.Save(o);
                EngJsonBox.Text = o.Count == 0 ? "{\n}" : o.ToString(Formatting.Indented);
                EngStatus.Foreground = Br(c.Green);
                EngStatus.Text = Lang.T("eng.saved");
                RefreshMods();
            }
            catch (Exception ex)
            {
                EngStatus.Foreground = Br(c.Red);
                EngStatus.Text = ex.Message;
            }
        }

        void BtnEngReset_Click(object sender, RoutedEventArgs e)
        {
            try { FlagService.Save(new JObject()); } catch { }
            LoadEngine();
            EngStatus.Foreground = Br(Theme.Current.Gray);
            EngStatus.Text = Lang.T("eng.resetdone");
            RefreshMods();
        }

        bool _updBusy;

        void ShowLocalVersion()
        {
            string v = UpdateService.GetLocalVersion();
            StgUpdVer.Text = Lang.T("upd.current") + " " + (v.Length == 0 ? "?" : v);
        }

        async void BtnCheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            await CheckUpdateAsync(false);
        }

        async Task CheckUpdateAsync(bool startup)
        {
            if (_updBusy) return;
            _updBusy = true;
            BtnCheckUpdate.IsEnabled = false;
            ThemeColors c = Theme.Current;
            StgUpdStatus.Foreground = Br(c.Gray);
            StgUpdStatus.Text = Lang.T("upd.checking");
            try
            {
                string local = UpdateService.GetLocalVersion();
                string remote = await UpdateService.GetRemoteVersionAsync();
                if (remote.Length == 0) throw new Exception("Empty version");
                if (remote == local)
                {
                    StgUpdStatus.Foreground = Br(c.Green);
                    StgUpdStatus.Text = string.Format(Lang.T("upd.latest"), local);
                }
                else
                {
                    StgUpdStatus.Foreground = Br(c.Yellow);
                    StgUpdStatus.Text = string.Format(Lang.T("upd.found"), remote);
                    MessageBoxResult r = MessageBox.Show(
                        string.Format(Lang.T("upd.ask"), local.Length == 0 ? "?" : local, remote),
                        "VortexX", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (r == MessageBoxResult.Yes)
                    {
                        try
                        {
                            await UpdateService.StartUpdateAsync(remote, step =>
                                StgUpdStatus.Text = Lang.T("upd." + step));
                            Application.Current.Shutdown();
                            return;
                        }
                        catch (Exception ex)
                        {
                            StgUpdStatus.Foreground = Br(c.Red);
                            StgUpdStatus.Text = string.Format(Lang.T("upd.failed"), ex.Message);
                            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(UpdateService.ReleasesPage) { UseShellExecute = true }); } catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StgUpdStatus.Foreground = Br(c.Red);
                StgUpdStatus.Text = string.Format(Lang.T("upd.error"), ex.Message);
            }
            finally
            {
                _updBusy = false;
                BtnCheckUpdate.IsEnabled = true;
            }
        }
    }
}
