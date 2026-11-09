using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VortexX.Services
{
    /// <summary>類似 Bloxstrap 的 Mods：Mods 資料夾裡的檔案會覆蓋到 Roblox 安裝資料夾（保留原檔備份，移除後自動還原）。</summary>
    public static class ModService
    {
        public static readonly string Root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Voretex_RB");
        public static string ModsDir { get { return Path.Combine(Root, "Mods"); } }
        static string BackupDir { get { return Path.Combine(Root, "ModBackup"); } }
        static string StatePath { get { return Path.Combine(Root, "mods-state.json"); } }

        /// <summary>FPS 上限：Roblox 已不再接受 DFIntTaskSchedulerTargetFps，改寫遊戲內「最大幀率」的設定檔。</summary>
        public static bool ApplyFpsCap(int cap, Action<string> log)
        {
            if (cap <= 0) return false;
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Roblox", "GlobalBasicSettings_13.xml");
                if (!File.Exists(path))
                {
                    if (log != null) log(Lang.T("log.fpsnofile"));
                    return false;
                }
                XDocument doc = XDocument.Load(path);
                XElement props = doc.Descendants("Item")
                    .Where(i => (string)i.Attribute("class") == "UserGameSettings")
                    .Select(i => i.Element("Properties"))
                    .FirstOrDefault(p => p != null);
                if (props == null) return false;
                XElement el = props.Elements("int").FirstOrDefault(x => (string)x.Attribute("name") == "FramerateCap");
                if (el == null)
                {
                    el = new XElement("int", new XAttribute("name", "FramerateCap"));
                    props.Add(el);
                }
                el.Value = cap.ToString();
                doc.Save(path);
                if (log != null) log(string.Format(Lang.T("log.fps"), cap));
                return true;
            }
            catch (Exception ex)
            {
                if (log != null) log("FPS: " + ex.Message);
                return false;
            }
        }

        public static void EnsureFolder()
        {
            try { Directory.CreateDirectory(ModsDir); } catch { }
        }

        public static void OpenFolder()
        {
            EnsureFolder();
            Process.Start(new ProcessStartInfo("explorer.exe", "\"" + ModsDir + "\"") { UseShellExecute = true });
        }

        public static List<string> ListFiles()
        {
            var list = new List<string>();
            try
            {
                if (!Directory.Exists(ModsDir)) return list;
                foreach (string f in Directory.GetFiles(ModsDir, "*", SearchOption.AllDirectories))
                    list.Add(f.Substring(ModsDir.Length + 1));
                list.Sort(StringComparer.OrdinalIgnoreCase);
            }
            catch { }
            return list;
        }

        static List<string> LoadState()
        {
            try
            {
                if (File.Exists(StatePath))
                {
                    var l = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(StatePath));
                    if (l != null) return l;
                }
            }
            catch { }
            return new List<string>();
        }

        static void SaveState(List<string> l)
        {
            try { File.WriteAllText(StatePath, JsonConvert.SerializeObject(l, Formatting.Indented)); } catch { }
        }

        /// <summary>套用 Mods。回傳套用的檔案數。</summary>
        public static int Apply(string installDir, Action<string> log)
        {
            if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir)) return 0;
            EnsureFolder();
            List<string> files = ListFiles();
            List<string> prev = LoadState();
            int ok = 0;

            // 1) 已不在 Mods 資料夾的檔案 → 還原原檔或刪除
            foreach (string rel in prev)
            {
                if (files.Contains(rel, StringComparer.OrdinalIgnoreCase)) continue;
                try
                {
                    string dest = Path.Combine(installDir, rel);
                    string bak = Path.Combine(BackupDir, rel);
                    if (File.Exists(bak)) { Directory.CreateDirectory(Path.GetDirectoryName(dest)); File.Copy(bak, dest, true); File.Delete(bak); }
                    else if (File.Exists(dest)) File.Delete(dest);
                    if (log != null) log("Mods: restored " + rel);
                }
                catch (Exception ex) { if (log != null) log("Mods: " + rel + " - " + ex.Message); }
            }

            // 2) 套用目前的 Mods
            foreach (string rel in files)
            {
                try
                {
                    string src = Path.Combine(ModsDir, rel);
                    string dest = Path.Combine(installDir, rel);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    string bak = Path.Combine(BackupDir, rel);
                    if (File.Exists(dest) && !File.Exists(bak) && !prev.Contains(rel, StringComparer.OrdinalIgnoreCase))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(bak));
                        File.Copy(dest, bak, true);
                    }
                    File.Copy(src, dest, true);
                    ok++;
                }
                catch (Exception ex) { if (log != null) log("Mods: " + rel + " - " + ex.Message); }
            }
            SaveState(files);
            return ok;
        }
    }

    /// <summary>引擎設定（FastFlags）：存成 Mods\ClientSettings\ClientAppSettings.json，啟動時隨 Mods 一起套用。</summary>
    public static class FlagService
    {
        public static string FilePath
        {
            get { return Path.Combine(ModService.ModsDir, "ClientSettings", "ClientAppSettings.json"); }
        }

        public static JObject Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    JObject o = JObject.Parse(File.ReadAllText(FilePath));
                    return o;
                }
            }
            catch { }
            return new JObject();
        }

        public static void Save(JObject o)
        {
            if (o.Count == 0)
            {
                try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { }
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            File.WriteAllText(FilePath, o.ToString(Formatting.Indented));
        }
    }
}
