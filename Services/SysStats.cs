using System;
using System.Runtime.InteropServices;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace VortexX.Services
{
    public static class SysStats
    {
        [StructLayout(LayoutKind.Sequential)]
        struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetSystemTimes(out FILETIME idle, out FILETIME kernel, out FILETIME user);

        static ulong U(FILETIME f)
        {
            return (((ulong)(uint)f.dwHighDateTime) << 32) | (uint)f.dwLowDateTime;
        }

        static ulong _prevIdle, _prevTotal;
        static bool _has;

        public static int Cpu()
        {
            FILETIME i, k, u;
            if (!GetSystemTimes(out i, out k, out u)) return 0;
            ulong idle = U(i);
            ulong total = U(k) + U(u);
            int r = 0;
            if (_has)
            {
                ulong di = idle - _prevIdle;
                ulong dt = total - _prevTotal;
                if (dt > 0) r = (int)(100UL * (dt - di) / dt);
            }
            _prevIdle = idle; _prevTotal = total; _has = true;
            return Math.Max(0, Math.Min(100, r));
        }

        public static int Ram()
        {
            var m = new MEMORYSTATUSEX();
            m.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            if (!GlobalMemoryStatusEx(ref m)) return 0;
            return (int)m.dwMemoryLoad;
        }
    }
}
