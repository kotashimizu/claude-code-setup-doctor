using SetupDoctor.Core.Diagnostics;
using SetupDoctor.Core.Diagnostics.Checks;
using SetupDoctor.Core.Models;
using SetupDoctor.Core.Tests.Infrastructure;

namespace SetupDoctor.Core.Tests.Diagnostics;

public sealed class ShellCheckTests
{
    private static DiagnosticContext EmptyCtx() =>
        DiagnosticContext.Create("test", DateTimeOffset.UtcNow);

    // ── WindowsPowerShellCheck ─────────────────────────────────────
    [Fact]
    public async Task PS_Pass_WhenExitZeroWithOutput()
    {
        var runner = new FakeCommandRunner();
        runner.Register(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
            new CommandResult(0, "5.1.19041.3636", "", TimeSpan.FromMilliseconds(200), false));
        var check = new WindowsPowerShellCheck(runner, new FakeClock());
        var result = await check.RunAsync(EmptyCtx(), CancellationToken.None);
        Assert.Equal(DiagnosticStatus.Pass, result.Status);
        Assert.Equal("SHELL_PS_AVAILABLE", result.SummaryKey);
    }

    [Fact]
    public async Task PS_Fallback_ToPlainPowershellExe()
    {
        var runner = new FakeCommandRunner();
        // フルパスは失敗させ、plain "powershell.exe" は成功させる
        runner.Register(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
            new CommandResult(1, "", "error", TimeSpan.FromMilliseconds(100), false));
        runner.Register("powershell.exe",
            new CommandResult(0, "5.1.17763", "", TimeSpan.FromMilliseconds(200), false));
        var check = new WindowsPowerShellCheck(runner, new FakeClock());
        var result = await check.RunAsync(EmptyCtx(), CancellationToken.None);
        Assert.Equal(DiagnosticStatus.Pass, result.Status);
    }

    [Fact]
    public async Task PS_ITAction_WhenPolicyBlocked()
    {
        var runner = new FakeCommandRunner();
        runner.Register(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
            new CommandResult(5, "", "Access denied", TimeSpan.FromMilliseconds(100), false));
        runner.Register("powershell.exe",
            new CommandResult(5, "", "Access denied", TimeSpan.FromMilliseconds(100), false));
        var check = new WindowsPowerShellCheck(runner, new FakeClock());
        var result = await check.RunAsync(EmptyCtx(), CancellationToken.None);
        Assert.Equal(DiagnosticStatus.ITAction, result.Status);
    }

    [Fact]
    public async Task PS_Unknown_WhenTimedOut()
    {
        var runner = new FakeCommandRunner();
        runner.Register(@"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
            new CommandResult(-1, "", "", TimeSpan.FromSeconds(5), true));
        runner.Register("powershell.exe",
            new CommandResult(-1, "", "", TimeSpan.FromSeconds(5), true));
        var check = new WindowsPowerShellCheck(runner, new FakeClock());
        var result = await check.RunAsync(EmptyCtx(), CancellationToken.None);
        Assert.Equal(DiagnosticStatus.Unknown, result.Status);
    }

    // ── PowerShell7Check ───────────────────────────────────────────
    [Fact]
    public async Task PS7_Pass_WhenPwshAvailable()
    {
        var runner = new FakeCommandRunner();
        runner.Register("pwsh.exe", new CommandResult(0, "PowerShell 7.4.0", "", TimeSpan.FromMilliseconds(200), false));
        var check = new PowerShell7Check(runner, new FakeClock());
        var result = await check.RunAsync(EmptyCtx(), CancellationToken.None);
        Assert.Equal(DiagnosticStatus.Pass, result.Status);
    }

    [Fact]
    public async Task PS7_Warning_WhenPwshMissing()
    {
        var runner = new FakeCommandRunner();
        // 未登録 → exit -1
        var check = new PowerShell7Check(runner, new FakeClock());
        var result = await check.RunAsync(EmptyCtx(), CancellationToken.None);
        Assert.Equal(DiagnosticStatus.Warning, result.Status);
    }
}
