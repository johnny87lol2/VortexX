using System;
using System.IO;

namespace VortexX.Services
{
    public class Logger
    {
        readonly object _lock = new object();
        readonly string _path;

        public Logger()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Voretex_RB", "logs");
            try { Directory.CreateDirectory(dir); } catch { }
            _path = Path.Combine(dir, "VortexX.log");
            try { if (File.Exists(_path) && new FileInfo(_path).Length > 1000000) File.Delete(_path); } catch { }
        }

        public void WriteLine(string ident, string message)
        {
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " [" + ident + "] " + message + Environment.NewLine;
            lock (_lock)
            {
                try { File.AppendAllText(_path, line); } catch { }
            }
        }

        public void WriteException(string ident, Exception ex)
        {
            WriteLine(ident, ex.ToString());
        }
    }
}
