using SetupDoctor.Core.Diagnostics;
using SetupDoctor.Core.Diagnostics.Checks;
using SetupDoctor.Core.Models;
using SetupDoctor.Core.Tests.Infrastructure;

namespace SetupDoctor.Core.Tests.Diagnostics;

public sealed class GitCheckTests
{
    private static DiagnosticContext CtxWithPs(DiagnosticStatus psStatus)
    {
        var ctx = DiagnosticContext.Create("test", DateTimeOffset.UtcNow);
        var psResult = new DiagnosticResult(
            "CHK-SHELL-001", psStatus, "summary", "detail",
            new Dictionary<string, string>(), TimeSpan.Zero, DateTimeOffset.UtcNow);
        return ctx.WithResult(psResult);
    }

    [Fact]
    public async Task Pass_WhenGitAvailable()
    {
        var runner = new FakeCommandRunner();
        runner.Register("git.exe", new CommandResult(0, "git version 2.44.0.windows.1", "", TimeSpan.FromMilliseconds(100), false));
        var check = new GitCheck(runner, new FakeClock());
        var result = await check.RunAsync(CtxWithPs(DiagnosticStatus.Pass), CancellationToken.None);
        Assert.Equal(DiagnosticStatus.Pass, result.Status);
    }

    [Fact]
    public async Task Warning_WhenGitMissingButPowerShellAvailable()
    {
        var runner = new FakeCommandRunner(); // git.exe 未登録 → exit -1
        var check = new GitCheck(runner, new FakeClock());
        var result = await check.RunAsync(CtxWithPs(DiagnosticStatus.Pass), CancellationToken.None);
        Assert.Equal(DiagnosticStatus.Warning, result.Status);
    }

    [Fact]
    public async Task Fail_WhenGitMissingAndNoPowerShell()
    {
        var runner = new FakeCommandRunner(); // git.exe 未登録
        var check = new GitCheck(runner, new FakeClock());
        // PowerShell も失敗しているコンテキスト
        var result = await check.RunAsync(CtxWithPs(DiagnosticStatus.Fail), CancellationToken.None);
        Assert.Equal(DiagnosticStatus.Fail, result.Status);
    }

    [Fact]
    public async Task Fail_WhenNoPriorPsResultAndGitMissing()
    {
        var runner = new FakeCommandRunner();
        var check = new GitCheck(runner, new FakeClock());
        // コンテキストにCHK-SHELL-001なし
        var emptyCtx = DiagnosticContext.Create("test", DateTimeOffset.UtcNow);
        var result = await check.RunAsync(emptyCtx, CancellationToken.None);
        Assert.Equal(DiagnosticStatus.Fail, result.Status);
    }
}
