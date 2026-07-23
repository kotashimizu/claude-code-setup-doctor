using SetupDoctor.Core.Diagnostics;
using SetupDoctor.Core.Diagnostics.Checks;
using SetupDoctor.Core.Tests.Infrastructure;

namespace SetupDoctor.Core.Tests.Diagnostics;

public sealed class OsVersionCheckTests
{
    private static DiagnosticContext EmptyCtx() =>
        DiagnosticContext.Create("test", DateTimeOffset.UtcNow);

    [Fact]
    public async Task Pass_WhenBuildMeetsMinimum()
    {
        var sys = new FakeSystemInfoProvider { OsBuild = "19041" };
        var check = new OsVersionCheck(sys, new FakeClock());
        var result = await check.RunAsync(EmptyCtx(), CancellationToken.None);
        Assert.Equal(DiagnosticStatus.Pass, result.Status);
    }

    [Fact]
    public async Task Pass_AtMinimumBuild()
    {
        var sys = new FakeSystemInfoProvider { OsBuild = "17763" };
        var check = new OsVersionCheck(sys, new FakeClock());
        var result = await check.RunAsync(EmptyCtx(), CancellationToken.None);
        Assert.Equal(DiagnosticStatus.Pass, result.Status);
    }

    [Fact]
    public async Task Fail_WhenBuildTooOld()
    {
        var sys = new FakeSystemInfoProvider { OsBuild = "17762" };
        var check = new OsVersionCheck(sys, new FakeClock());
        var result = await check.RunAsync(EmptyCtx(), CancellationToken.None);
        Assert.Equal(DiagnosticStatus.Fail, result.Status);
    }

    [Fact]
    public async Task Fail_WhenNotWindows()
    {
        var sys = new FakeSystemInfoProvider { OsFamily = "macOS", OsBuild = "19041" };
        var check = new OsVersionCheck(sys, new FakeClock());
        var result = await check.RunAsync(EmptyCtx(), CancellationToken.None);
        Assert.Equal(DiagnosticStatus.Fail, result.Status);
        Assert.Equal("OS_NOT_WINDOWS", result.SummaryKey);
    }

    [Fact]
    public async Task Fail_WhenBuildNullOrUnparseable()
    {
        var sys = new FakeSystemInfoProvider { OsBuild = null };
        var check = new OsVersionCheck(sys, new FakeClock());
        var result = await check.RunAsync(EmptyCtx(), CancellationToken.None);
        Assert.Equal(DiagnosticStatus.Fail, result.Status);
    }
}
