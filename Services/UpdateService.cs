using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace VortexX.Services
{
    public static class UpdateService
    {
        public const string RemoteVersionUrl = "https://raw.githubusercontent.com/johnny87lol2/VortexX/refs/heads/main/v.txt";
        public const string UpdateZipUrl = "https://github.com/johnny87lol2/VortexX/releases/latest/download/VortexX.zip";
        public const string ReleasesPage = "https://github.com/johnny87lol2/VortexX/releases/latest";

        public static string AppDir { get { return AppContext.BaseDirectory.TrimEnd('\\', '/'); } }
        static string LocalFile { get { return Path.Combine(AppDir, "v.txt"); } }

        public static string GetLocalVersion()
        {
            try { if (File.Exists(LocalFile)) return File.ReadAllText(LocalFile).Trim(); } catch { }
            return "";
        }

        public static async Task<string> GetRemoteVersionAsync()
        {
            string url = RemoteVersionUrl + "?t=" + DateTime.UtcNow.Ticks;
            string v = await App.HttpClient.GetStringAsync(url);
            return v.Trim();
        }

        public static async Task StartUpdateAsync(string newVersion, Action<string> progress)
        {
            string work = Path.Combine(Path.GetTempPath(), "VortexX_update");
            try { if (Directory.Exists(work)) Directory.Delete(work, true); } catch { }
            Directory.CreateDirectory(work);

            string zip = Path.Combine(work, "update.zip");
            string ext = Path.Combine(work, "files");

            if (progress != null) progress("download");
            using (HttpResponseMessage resp = await App.HttpClient.GetAsync(UpdateZipUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                resp.EnsureSuccessStatusCode();
                using (Stream src = await resp.Content.ReadAsStreamAsync())
                using (FileStream dst = File.Create(zip))
                    await src.CopyToAsync(dst);
            }

            if (progress != null) progress("extract");
            ZipFile.ExtractToDirectory(zip, ext);

            string[] dirs = Directory.GetDirectories(ext);
            string[] files = Directory.GetFiles(ext);
            string srcDir = (files.Length == 0 && dirs.Length == 1) ? dirs[0] : ext;

            File.WriteAllText(Path.Combine(srcDir, "v.txt"), newVersion);

            string exe = Process.GetCurrentProcess().MainModule.FileName;
            string bat = Path.Combine(work, "update.bat");
            string script =
                "@echo off\r\n" +
                "timeout /t 2 /nobreak >nul\r\n" +
                "xcopy /e /y /q \"" + srcDir + "\\*\" \"" + AppDir + "\\\" >nul\r\n" +
                "start \"\" \"" + exe + "\"\r\n";
            File.WriteAllText(bat, script);

            if (progress != null) progress("restart");
            var psi = new ProcessStartInfo("cmd.exe", "/c \"" + bat + "\"");
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            Process.Start(psi);
        }
    }
}
