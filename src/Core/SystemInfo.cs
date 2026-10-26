using System;
using System.Runtime.InteropServices;

namespace Vexor.Core
{
    public static class SystemInfo
    {
        [StructLayout(LayoutKind.Sequential)]
        private sealed class MemoryStatus
        {
            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhys;
            public ulong AvailPhys;
            public ulong TotalPageFile;
            public ulong AvailPageFile;
            public ulong TotalVirtual;
            public ulong AvailVirtual;
            public ulong AvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatus buffer);

        public static int TotalRamMb
        {
            get
            {
                try
                {
                    var status = new MemoryStatus();
                    status.Length = (uint)Marshal.SizeOf(typeof(MemoryStatus));
                    if (GlobalMemoryStatusEx(status)) return (int)(status.TotalPhys / (1024UL * 1024UL));
                }
                catch
                {
                }
                return 8192;
            }
        }
    }
}
