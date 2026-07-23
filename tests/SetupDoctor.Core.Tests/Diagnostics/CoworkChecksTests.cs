using SetupDoctor.Core.Diagnostics;
using SetupDoctor.Core.Diagnostics.Checks;
using SetupDoctor.Core.Models;
using SetupDoctor.Core.Tests.Infrastructure;

namespace SetupDoctor.Core.Tests.Diagnostics;

public sealed class CoworkChecksTests
{
    private static DiagnosticContext EmptyCtx() =>
        DiagnosticContext.Create("test", DateTimeOffset.UtcNow);

    // ── CoworkVmServiceCheck (CHK-COWORK-001) ─────────────────────
    [Fact]
    public async Task VmService_Pass_WhenRunning()
    {
        var runner = new FakeCommandRunner();
        runner.Register("powershell.exe", new CommandResult(0, "Running", "", TimeSpan.FromMilliseconds(100), false));
        var check = new CoworkVmServiceCheck(runner, new FakeClock());
        var result = await check.RunAsync(EmptyCtx(), CancellationToken.None);
        Assert.Equal(DiagnosticStatus.Pass, result.Status);
    }

    [Fact]
    public async Task VmService_Repairable_WhenStopped()
    {
        var runner = new FakeCommandRunner();
        runner.Register("powershell.exe", new CommandResult(0, "Stopped", "", TimeSpan.FromMilliseconds(100), false));
        var check = new CoworkVmServiceCheck(runner, new FakeClock());
        var result = await check.RunAsync(EmptyCtx(), CancellationToken.None);
        Assert.Equal(DiagnosticStatus.Repairable, result.Status);
    }

    [Fact]
    public async Task VmService_Unknown_WhenNotFound()
    {
        var runner = new FakeCommandRunner();
        runner.Register("powershell.exe", new CommandResult(0, "NOTFOUND", "", TimeSpan.FromMilliseconds(100), false));
        var check = new CoworkVmServiceCheck(runner, new FakeClock());
        var result = await check.RunAsync(EmptyCtx(), CancellationToken.None);
        Assert.Equal(DiagnosticStatus.Unknown, result.Status);
    }

    [Fact]
    public async Task VmService_Unknown_WhenPowerShellUnavailable()
    {
        var runner = new FakeCommandRunner(); // powershell.exe未登録 → exit -1
        var check = new CoworkVmServiceCheck(runner, new FakeClock());
        var result = await check.RunAsync(EmptyCtx(), CancellationToken.None);
        Assert.Equal(DiagnosticStatus.Unknown, result.Status);
    }

    // ── CoworkVirtualizationCheck (CHK-COWORK-002) ────────────────
    [Fact]
    public async Task Virtualization_Pass_WhenEnabledAndNotHome()
    {
        var runner = new FakeCommandRunner();
        runner.Register("powershell.exe",
            new CommandResult(0, "Professional|Enabled", "", TimeSpan.FromMilliseconds(100), false));
        var sys = new FakeSystemInfoProvider { OsBuild = "22631" };
        var check = new CoworkVirtualizationCheck(runner, sys, new FakeClock());
        var result = await check.RunAsync(EmptyCtx(), CancellationToken.None);
        Assert.Equal(DiagnosticStatus.Pass, result.Status);
    }

    [Fact]
    public async Task Virtualization_Unknown_WhenEnabledButHomeEdition()
    {
        var runner = new FakeCommandRunner();
        runner.Register("powershell.exe",
            new CommandResult(0, "Core|Enabled", "", TimeSpan.FromMilliseconds(100), false));
        var sys = new FakeSystemInfoProvider { OsBuild = "22631" };
        var check = new CoworkVirtualizationCheck(runner, sys, new FakeClock());
        var result = await check.RunAsync(EmptyCtx(), CancellationToken.None);
        Assert.Equal(DiagnosticStatus.Unknown, result.Status);
    }

    [Fact]
    public async Task Virtualization_Repairable_WhenDisabled()
    {
        var runner = new FakeCommandRunner();
        runner.Register("powershell.exe",
            new CommandResult(0, "Professional|Disabled", "", TimeSpan.FromMilliseconds(100), false));
        var sys = new FakeSystemInfoProvider { OsBuild = "22631" };
        var check = new CoworkVirtualizationCheck(runner, sys, new FakeClock());
        var result = await check.RunAsync(EmptyCtx(), CancellationToken.None);
        Assert.Equal(DiagnosticStatus.Repairable, result.Status);
    }

    // ── CoworkHcsComponentsCheck (CHK-COWORK-003) ─────────────────
    [Fact]
    public async Task Hcs_NotApplicable_WhenVirtualizationNotReady()
    {
        var runner = new FakeCommandRunner();
        var ctx = EmptyCtx().WithResult(new DiagnosticResult(
            "CHK-COWORK-002", DiagnosticStatus.Unknown, "s", "d",
            new Dictionary<string, string>(), TimeSpan.Zero, DateTimeOffset.UtcNow));
        var check = new CoworkHcsComponentsCheck(runner, new FakeClock());
        var result = await check.RunAsync(ctx, CancellationToken.None);
        Assert.Equal(DiagnosticStatus.NotApplicable, result.Status);
    }

    [Fact]
    public async Task Hcs_Pass_WhenAllComponentsPresent()
    {
        var runner = new FakeCommandRunner();
        runner.Register("powershell.exe", new CommandResult(0, "True|True", "", TimeSpan.FromMilliseconds(100), false));
        runner.Register("sc.exe", new CommandResult(0, "", "", TimeSpan.FromMilliseconds(50), false));
        var ctx = EmptyCtx().WithResult(new DiagnosticResult(
            "CHK-COWORK-002", DiagnosticStatus.Pass, "s", "d",
            new Dictionary<string, string>(), TimeSpan.Zero, DateTimeOffset.UtcNow));
        var check = new CoworkHcsComponentsCheck(runner, new FakeClock());
        var result = await check.RunAsync(ctx, CancellationToken.None);
        Assert.Equal(DiagnosticStatus.Pass, result.Status);
    }

    [Fact]
    public async Task Hcs_Repairable_WhenComponentMissing()
    {
        var runner = new FakeCommandRunner();
        runner.Register("powershell.exe", new CommandResult(0, "False|True", "", TimeSpan.FromMilliseconds(100), false));
        // sc.exe未登録 → exit -1 → vfpext不在扱い
        var ctx = EmptyCtx().WithResult(new DiagnosticResult(
            "CHK-COWORK-002", DiagnosticStatus.Repairable, "s", "d",
            new Dictionary<string, string>(), TimeSpan.Zero, DateTimeOffset.UtcNow));
        var check = new CoworkHcsComponentsCheck(runner, new FakeClock());
        var result = await check.RunAsync(ctx, CancellationToken.None);
        Assert.Equal(DiagnosticStatus.Repairable, result.Status);
    }

    // ── CoworkVhdxCompressionCheck (CHK-COWORK-004) ───────────────
    [Fact]
    public async Task Vhdx_Unknown_WhenBundlesDirectoryNotFound()
    {
        var sys = new FakeSystemInfoProvider();
        sys.SetExpansion("LOCALAPPDATA", "/nonexistent-test-path-local");
        sys.SetExpansion("APPDATA", "/nonexistent-test-path-appdata");
        var check = new CoworkVhdxCompressionCheck(sys, new FakeClock());
        var result = await check.RunAsync(EmptyCtx(), CancellationToken.None);
        Assert.Equal(DiagnosticStatus.Unknown, result.Status);
    }

    // ── CoworkNetworkConflictCheck (CHK-COWORK-005) ───────────────
    [Fact]
    public async Task Network_CompletesWithoutThrowing()
    {
        var check = new CoworkNetworkConflictCheck(new FakeClock());
        var result = await check.RunAsync(EmptyCtx(), CancellationToken.None);
        Assert.Contains(result.Status, new[]
        {
            DiagnosticStatus.Pass, DiagnosticStatus.ITAction, DiagnosticStatus.Unknown,
        });
    }

    // ── CoworkOrgPolicyHintCheck (CHK-COWORK-006) ─────────────────
    [Fact]
    public async Task OrgHint_Unknown_WhenAllPriorCoworkChecksHealthy()
    {
        var ctx = EmptyCtx();
        foreach (var id in new[] { "CHK-COWORK-001", "CHK-COWORK-002", "CHK-COWORK-003", "CHK-COWORK-004", "CHK-COWORK-005" })
        {
            ctx = ctx.WithResult(new DiagnosticResult(id, DiagnosticStatus.Pass, "s", "d",
                new Dictionary<string, string>(), TimeSpan.Zero, DateTimeOffset.UtcNow));
        }

        var check = new CoworkOrgPolicyHintCheck(new FakeClock());
        var result = await check.RunAsync(ctx, CancellationToken.None);
        Assert.Equal(DiagnosticStatus.Unknown, result.Status);
    }

    [Fact]
    public async Task OrgHint_NotApplicable_WhenOtherCoworkIssueFound()
    {
        var ctx = EmptyCtx();
        foreach (var id in new[] { "CHK-COWORK-001", "CHK-COWORK-002", "CHK-COWORK-003", "CHK-COWORK-005" })
        {
            ctx = ctx.WithResult(new DiagnosticResult(id, DiagnosticStatus.Pass, "s", "d",
                new Dictionary<string, string>(), TimeSpan.Zero, DateTimeOffset.UtcNow));
        }
        ctx = ctx.WithResult(new DiagnosticResult("CHK-COWORK-004", DiagnosticStatus.Repairable, "s", "d",
            new Dictionary<string, string>(), TimeSpan.Zero, DateTimeOffset.UtcNow));

        var check = new CoworkOrgPolicyHintCheck(new FakeClock());
        var result = await check.RunAsync(ctx, CancellationToken.None);
        Assert.Equal(DiagnosticStatus.NotApplicable, result.Status);
    }
}
