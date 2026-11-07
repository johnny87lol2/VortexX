using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VortexX.Models;

namespace VortexX.Services
{
    public class StatusComp
    {
        public string Name = "";
        public string Status = "";
    }

    public class StatusResult
    {
        public string Indicator = "unknown";   // none / minor / major / critical / unknown / error
        public string Source = "official";     // official / probe
        public string Time = DateTime.Now.ToString("HH:mm:ss");
        public List<StatusComp> Components = new List<StatusComp>();
    }

    public static class RobloxService
    {
        static readonly HttpClient Http = CreateHttp();

        static HttpClient CreateHttp()
        {
            var h = new HttpClient();
            h.Timeout = TimeSpan.FromSeconds(20);
            h.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 VortexX");
            return h;
        }

        static string LocalAppData
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData); }
        }

        // ───────── 版本 ─────────
        public static async Task<string> GetLatestVersionAsync()
        {
            try
            {
                string json = await Http.GetStringAsync("https://clientsettingscdn.roblox.com/v1/client-version/WindowsPlayer");
                var v = JsonConvert.DeserializeObject<RobloxVersion>(json);
                if (v != null && !string.IsNullOrEmpty(v.ClientVersionUpload)) return v.ClientVersionUpload;
            }
            catch { }
            return null;
        }

        public static string GetInstalledVersion(string dir)
        {
            try
            {
                string p = Path.Combine(dir, "version.txt");
                if (File.Exists(p)) return File.ReadAllText(p).Trim();
            }
            catch { }
            return null;
        }

        public static bool IsInstalled(string dir)
        {
            try { return File.Exists(Path.Combine(dir, "RobloxPlayerBeta.exe")); }
            catch { return false; }
        }

        // ───────── 安裝 ─────────
        public static async Task InstallAsync(string dir, Action<int, string> progress, Action<string> log)
        {
            string ver = await GetLatestVersionAsync();
            if (ver == null) throw new Exception(Lang.T("install.latest_fail"));
            log(string.Format(Lang.T("inst.version"), ver));

            Directory.CreateDirectory(dir);
            string tmp = Path.Combine(Path.GetTempPath(), "VortexX_RobloxPlayerInstaller.exe");

            progress(0, Lang.T("inst.download"));
            string url = "https://setup.rbxcdn.com/" + ver + "-RobloxPlayerInstaller.exe";
            using (var resp = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                resp.EnsureSuccessStatusCode();
                long total = resp.Content.Headers.ContentLength ?? 0;
                using (var src = await resp.Content.ReadAsStreamAsync())
                using (var dst = File.Create(tmp))
                {
                    var buf = new byte[81920];
                    long got = 0;
                    int n;
                    while ((n = await src.ReadAsync(buf, 0, buf.Length)) > 0)
                    {
                        await dst.WriteAsync(buf, 0, n);
                        got += n;
                        if (total > 0) progress((int)(got * 40 / total), Lang.T("inst.download"));
                    }
                }
            }

            progress(40, Lang.T("inst.run"));
            Process proc = Process.Start(new ProcessStartInfo(tmp) { UseShellExecute = true });

            string versionsDir = Path.Combine(LocalAppData, "Roblox", "Versions");
            string srcDir = Path.Combine(versionsDir, ver);
            for (int i = 0; i < 180; i++)
            {
                if (File.Exists(Path.Combine(srcDir, "RobloxPlayerBeta.exe"))) break;
                await Task.Delay(1000);
                progress(40 + Math.Min(i / 4, 10), Lang.T("inst.run"));
            }

            progress(52, Lang.T("inst.wait"));
            if (proc != null)
            {
                await Task.Run(() => { try { proc.WaitForExit(60000); } catch { } });
            }
            await Task.Delay(2000);

            if (!File.Exists(Path.Combine(srcDir, "RobloxPlayerBeta.exe")) && Directory.Exists(versionsDir))
            {
                var cand = new DirectoryInfo(versionsDir).GetDirectories("version-*")
                    .Where(d => File.Exists(Path.Combine(d.FullName, "RobloxPlayerBeta.exe")))
                    .OrderByDescending(d => d.LastWriteTime)
                    .FirstOrDefault();
                if (cand != null) srcDir = cand.FullName;
            }
            if (!File.Exists(Path.Combine(srcDir, "RobloxPlayerBeta.exe")))
                throw new Exception(Lang.T("inst.nofiles"));

            progress(55, Lang.T("inst.copy"));
            string from = srcDir;
            await Task.Run(() => CopyDir(from, dir, p => progress(55 + p * 40 / 100, Lang.T("inst.copy"))));

            File.WriteAllText(Path.Combine(dir, "version.txt"), ver);
            RegisterProtocol(dir);
            try { File.Delete(tmp); } catch { }

            string done = string.Format(Lang.T("inst.done"), ver);
            progress(100, done);
            log(done);
        }

        static void CopyDir(string src, string dst, Action<int> pct)
        {
            string[] files = Directory.GetFiles(src, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                string rel = Path.GetRelativePath(src, files[i]);
                string target = Path.Combine(dst, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(files[i], target, true);
                pct((i + 1) * 100 / files.Length);
            }
        }

        static void RegisterProtocol(string dir)
        {
            try
            {
                string exe = Path.Combine(dir, "RobloxPlayerBeta.exe");
                foreach (string proto in new string[] { "roblox-player", "roblox" })
                {
                    using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + proto))
                    {
                        if (k == null) continue;
                        k.SetValue("", "URL: " + proto + " Protocol");
                        k.SetValue("URL Protocol", "");
                        using (RegistryKey cmd = k.CreateSubKey(@"shell\open\command"))
                        {
                            if (cmd != null) cmd.SetValue("", "\"" + exe + "\" \"%1\"");
                        }
                    }
                }
            }
            catch { }
        }

        // ───────── 啟動 ─────────
        static void RunHidden(string file, string args)
        {
            try
            {
                var psi = new ProcessStartInfo(file, args);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                using (Process p = Process.Start(psi))
                {
                    if (p != null) p.WaitForExit(8000);
                }
            }
            catch { }
        }

        public static bool Launch(string dir, bool flush, bool power, Action<string> log)
        {
            if (flush)
            {
                RunHidden("ipconfig", "/flushdns");
                log(Lang.T("log.dns"));
            }
            if (power)
            {
                RunHidden("powercfg", "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
                log(Lang.T("log.power"));
            }
            string exe = Path.Combine(dir, "RobloxPlayerBeta.exe");
            if (File.Exists(exe))
            {
                var psi = new ProcessStartInfo(exe, "--app");
                psi.WorkingDirectory = dir;
                psi.UseShellExecute = false;
                Process.Start(psi);
                log(Lang.T("log.started"));
                return true;
            }
            Process.Start(new ProcessStartInfo("roblox://") { UseShellExecute = true });
            log(Lang.T("log.fallback"));
            return false;
        }

        public static bool IsRunning()
        {
            try
            {
                Process[] ps = Process.GetProcessesByName("RobloxPlayerBeta");
                bool r = ps.Length > 0;
                foreach (Process p in ps) p.Dispose();
                return r;
            }
            catch { return false; }
        }

        public static DateTime? GetRobloxStartTime()
        {
            try
            {
                DateTime? earliest = null;
                foreach (Process p in Process.GetProcessesByName("RobloxPlayerBeta"))
                {
                    try
                    {
                        DateTime t = p.StartTime;
                        if (earliest == null || t < earliest.Value) earliest = t;
                    }
                    catch { }
                    p.Dispose();
                }
                return earliest;
            }
            catch { return null; }
        }

        // ───────── 伺服器偵測 ─────────
        static readonly Regex JoinRx = new Regex(@"Joining game '([0-9a-fA-F\-]{36})' place ([0-9]+) at ([0-9\.]+)");
        static readonly Regex UdmuxRx = new Regex(@"UDMUX Address = ([0-9\.]+), Port = [0-9]+ \| RCC Server Address = ([0-9\.]+), Port = [0-9]+");
        static readonly Regex JoinedRx = new Regex(@"serverId:\s*([0-9\.]+)\|[0-9]+");

        static string ReadShared(string path)
        {
            try
            {
                // Roblox 執行中會鎖住 log，必須允許共用讀取
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var sr = new StreamReader(fs))
                {
                    return sr.ReadToEnd();
                }
            }
            catch { return null; }
        }

        public static RobloxServerInfo GetCurrentServer()
        {
            try
            {
                string logs = Path.Combine(LocalAppData, "Roblox", "logs");
                if (!Directory.Exists(logs)) return null;
                var files = new DirectoryInfo(logs).GetFiles("*.log")
                    .OrderByDescending(f => f.LastWriteTime)
                    .Take(5)
                    .ToList();
                foreach (FileInfo f in files)
                {
                    string text = ReadShared(f.FullName);
                    if (text == null) continue;
                    MatchCollection ms = JoinRx.Matches(text);
                    if (ms.Count == 0) continue;
                    Match m = ms[ms.Count - 1];
                    var info = new RobloxServerInfo();
                    info.JobId = m.Groups[1].Value;
                    info.PlaceId = m.Groups[2].Value;
                    info.ServerIp = m.Groups[3].Value;
                    string after = text.Substring(m.Index);
                    // Bloxstrap 的做法：伺服器多半有 UDMUX 代理，第一個位址才是公開 IP（RCC 位址常是 10.x 內網）
                    Match u = UdmuxRx.Match(after);
                    if (u.Success) info.ServerIp = u.Groups[1].Value;
                    else
                    {
                        Match j = JoinedRx.Match(after);
                        if (j.Success) info.ServerIp = j.Groups[1].Value;
                    }
                    return info;
                }
            }
            catch { }
            return null;
        }

        public static async Task<string> GetGameNameAsync(string placeId)
        {
            try
            {
                string u = await Http.GetStringAsync("https://apis.roblox.com/universes/v1/places/" + placeId + "/universe");
                JObject uo = JObject.Parse(u);
                string uid = uo["universeId"] == null ? null : uo["universeId"].ToString();
                if (string.IsNullOrEmpty(uid)) return null;
                string g = await Http.GetStringAsync("https://games.roblox.com/v1/games?universeIds=" + uid);
                JObject go = JObject.Parse(g);
                JArray arr = go["data"] as JArray;
                if (arr != null && arr.Count > 0 && arr[0]["name"] != null) return arr[0]["name"].ToString();
            }
            catch { }
            return null;
        }

        public static async Task<GeoInfo> GetGeoAsync(string ip)
        {
            try
            {
                string json = await Http.GetStringAsync("http://ip-api.com/json/" + Uri.EscapeDataString(ip) +
                    "?fields=status,message,country,countryCode,regionName,city,lat,lon,timezone,isp,as,query");
                var g = JsonConvert.DeserializeObject<GeoInfo>(json);
                if (g != null && g.Status == "success") return g;
            }
            catch { }
            return null;
        }

        // ───────── 平台狀態 ─────────
        public static async Task<StatusResult> GetStatusAsync()
        {
            var res = new StatusResult();

            // 1) 官方 statuspage JSON
            try
            {
                string json = await Http.GetStringAsync("https://status.roblox.com/api/v2/summary.json");
                JObject o = JObject.Parse(json);
                JToken st = o["status"];
                string ind = (st != null && st["indicator"] != null) ? st["indicator"].ToString() : null;
                if (!string.IsNullOrEmpty(ind))
                {
                    res.Indicator = ind;
                    JArray comps = o["components"] as JArray;
                    if (comps != null)
                    {
                        foreach (JToken c in comps)
                        {
                            if (c["group"] != null && c["group"].Type == JTokenType.Boolean && (bool)c["group"]) continue;
                            var sc = new StatusComp();
                            sc.Name = c["name"] != null ? c["name"].ToString() : "";
                            sc.Status = c["status"] != null ? c["status"].ToString() : "";
                            res.Components.Add(sc);
                        }
                    }
                    return await FillComponents(res);
                }
            }
            catch { }

            // 2) 官方網頁：只有明確寫著「全部正常」才信
            try
            {
                string html = await Http.GetStringAsync("https://status.roblox.com/");
                string low = html.ToLowerInvariant();
                if (low.Contains("all systems operational") || low.Contains("fully operational"))
                {
                    res.Indicator = "none";
                    return await FillComponents(res);
                }
            }
            catch { }

            // 3) 實際連線探測（絕不誤報大當機）
            return await ProbeAsync();
        }

        // 官方資料沒有服務清單時（例如網頁只有一句「全部正常」），用實際連線探測補上清單
        static async Task<StatusResult> FillComponents(StatusResult res)
        {
            if (res.Components.Count == 0)
            {
                StatusResult probe = await ProbeAsync();
                res.Components = probe.Components;
            }
            return res;
        }

        static async Task<StatusResult> ProbeAsync()
        {
            var res = new StatusResult();
            res.Source = "probe";
            var targets = new List<KeyValuePair<string, string>>();
            targets.Add(new KeyValuePair<string, string>("Website", "https://www.roblox.com/"));
            targets.Add(new KeyValuePair<string, string>("Auth", "https://auth.roblox.com/"));
            targets.Add(new KeyValuePair<string, string>("Users API", "https://users.roblox.com/v1/users/1"));
            targets.Add(new KeyValuePair<string, string>("Games API", "https://games.roblox.com/v1/games?universeIds=1"));
            targets.Add(new KeyValuePair<string, string>("Client Settings", "https://clientsettingscdn.roblox.com/v1/client-version/WindowsPlayer"));
            targets.Add(new KeyValuePair<string, string>("CDN", "https://setup.rbxcdn.com/version"));

            var tasks = new List<Task<bool>>();
            foreach (var t in targets) tasks.Add(ProbeOne(t.Value));
            bool[] ok = await Task.WhenAll(tasks);

            int good = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                var sc = new StatusComp();
                sc.Name = targets[i].Key;
                sc.Status = ok[i] ? "operational" : "major_outage";
                if (ok[i]) good++;
                res.Components.Add(sc);
            }
            if (good == targets.Count) res.Indicator = "none";
            else if (good == 0) res.Indicator = "error";
            else res.Indicator = "minor";
            return res;
        }

        static async Task<bool> ProbeOne(string url)
        {
            try
            {
                using (var req = new HttpRequestMessage(HttpMethod.Get, url))
                using (var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead))
                {
                    return (int)resp.StatusCode < 500;
                }
            }
            catch { return false; }
        }

        // ───────── Ping ─────────
        public static async Task<long> PingAsync(string host)
        {
            try
            {
                using (var p = new Ping())
                {
                    PingReply r = await p.SendPingAsync(host, 3000);
                    if (r.Status == IPStatus.Success) return r.RoundtripTime;
                }
            }
            catch { }

            System.Net.IPAddress ip;
            if (System.Net.IPAddress.TryParse(host, out ip)) return -1;

            try
            {
                var sw = Stopwatch.StartNew();
                using (var req = new HttpRequestMessage(HttpMethod.Head, "https://" + host + "/"))
                using (var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead))
                {
                }
                sw.Stop();
                return sw.ElapsedMilliseconds;
            }
            catch { }
            return -1;
        }
    }
}
