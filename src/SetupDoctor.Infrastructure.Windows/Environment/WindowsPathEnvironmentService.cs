using System.Runtime.InteropServices;
using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Policies;

namespace SetupDoctor.Infrastructure.Windows.Environment;

public sealed class WindowsPathEnvironmentService : IPathEnvironmentService
{
    // WM_SETTINGCHANGE を送り、他のアプリに環境変数変更を通知する
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd, uint Msg, UIntPtr wParam, string lParam,
        uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);

    private const uint WM_SETTINGCHANGE = 0x001A;
    private const uint SMTO_ABORTIFHUNG = 0x0002;
    private static readonly IntPtr HWND_BROADCAST = new(0xFFFF);

    public string? GetUserPath()
        => System.Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);

    public void SetUserPath(string path)
    {
        System.Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.User);
        // 変更を他のプロセスに通知
        SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, UIntPtr.Zero,
            "Environment", SMTO_ABORTIFHUNG, 5000, out _);
    }

    public string? GetProcessPath()
        => System.Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);

    public IReadOnlyList<string> GetUserPathEntries()
        => PathNormalizer.Split(GetUserPath());

    public bool UserPathContains(string normalizedEntry)
        => PathNormalizer.Contains(GetUserPath(), normalizedEntry);
}
