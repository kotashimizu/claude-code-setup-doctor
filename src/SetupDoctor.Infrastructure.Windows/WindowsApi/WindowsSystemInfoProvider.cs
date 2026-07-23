using System.Runtime.InteropServices;
using SetupDoctor.Core.Abstractions;

namespace SetupDoctor.Infrastructure.Windows.WindowsApi;

public sealed class WindowsSystemInfoProvider : ISystemInfoProvider
{
    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
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
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

    public string OsFamily => "Windows";

    public string? OsBuild
    {
        get
        {
            try
            {
                // Windows 10以降のビルド番号はEnvironment.OSVersionから取得可能
                return Environment.OSVersion.Version.Build.ToString();
            }
            catch { return null; }
        }
    }

    public string? Architecture
    {
        get
        {
            return RuntimeInformation.OSArchitecture switch
            {
                Architecture.X64 => "X64",
                Architecture.Arm64 => "ARM64",
                Architecture.X86 => "X86",
                _ => RuntimeInformation.OSArchitecture.ToString(),
            };
        }
    }

    public double? MemoryGiB
    {
        get
        {
            var status = new MemoryStatusEx();
            status.dwLength = (uint)Marshal.SizeOf(status);
            if (GlobalMemoryStatusEx(ref status))
                return status.ullTotalPhys / (1024.0 * 1024 * 1024);
            return null;
        }
    }

    public bool Is64BitOperatingSystem => Environment.Is64BitOperatingSystem;

    public string ExpandEnvironmentVariables(string path)
        => Environment.ExpandEnvironmentVariables(path);
}
