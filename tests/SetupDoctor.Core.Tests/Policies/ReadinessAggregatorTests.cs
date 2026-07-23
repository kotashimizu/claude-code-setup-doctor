using SetupDoctor.Core.Diagnostics;
using SetupDoctor.Core.Models;
using SetupDoctor.Core.Policies;
using SetupDoctor.Core.Tests.Infrastructure;
using Xunit;

namespace SetupDoctor.Core.Tests.Policies;

public sealed class ReadinessAggregatorTests
{
    private readonly ReadinessAggregator _sut = new();

    private static List<DiagnosticResult> AllRequiredPass() =>
    [
        DiagnosticResultBuilder.Pass("CHK-OS-001"),
        DiagnosticResultBuilder.Pass("CHK-ARCH-001"),
        DiagnosticResultBuilder.Pass("CHK-MEM-001"),
        DiagnosticResultBuilder.Pass("CHK-SHELL-001"),
        DiagnosticResultBuilder.Pass("CHK-CLAUDE-001"),
        DiagnosticResultBuilder.Pass("CHK-CLAUDE-002"),
        DiagnosticResultBuilder.Pass("CHK-PATH-001"),
        DiagnosticResultBuilder.Pass("CHK-AUTH-001"),
    ];

    [Fact]
    public void EmptyResults_ReturnsUnknown()
    {
        var result = _sut.Aggregate([]);
        Assert.Equal(OverallReadiness.Unknown, result);
    }

    [Fact]
    public void OsCheckFail_ReturnsUnsupported()
    {
        var results = new List<DiagnosticResult>
        {
            DiagnosticResultBuilder.Fail("CHK-OS-001"),
            DiagnosticResultBuilder.Pass("CHK-ARCH-001"),
        };
        Assert.Equal(OverallReadiness.Unsupported, _sut.Aggregate(results));
    }

    [Fact]
    public void ArchCheckFail_ReturnsUnsupported()
    {
        var results = new List<DiagnosticResult>
        {
            DiagnosticResultBuilder.Pass("CHK-OS-001"),
            DiagnosticResultBuilder.Fail("CHK-ARCH-001"),
        };
        Assert.Equal(OverallReadiness.Unsupported, _sut.Aggregate(results));
    }

    [Fact]
    public void MemCheckFail_ReturnsUnsupported()
    {
        var results = new List<DiagnosticResult>
        {
            DiagnosticResultBuilder.Pass("CHK-OS-001"),
            DiagnosticResultBuilder.Pass("CHK-ARCH-001"),
            DiagnosticResultBuilder.Fail("CHK-MEM-001"),
        };
        Assert.Equal(OverallReadiness.Unsupported, _sut.Aggregate(results));
    }

    [Fact]
    public void RequiredCheckITAction_ReturnsITActionRequired()
    {
        var results = AllRequiredPass();
        results.Add(DiagnosticResultBuilder.ITAction("CHK-CLAUDE-002"));
        // ITActionがrequiredチェックに存在する場合
        var modified = results.Select(r =>
            r.CheckId == "CHK-CLAUDE-002"
                ? DiagnosticResultBuilder.ITAction("CHK-CLAUDE-002")
                : r).ToList();

        Assert.Equal(OverallReadiness.ITActionRequired, _sut.Aggregate(modified));
    }

    [Fact]
    public void RequiredCheckRepairable_ReturnsRepairable()
    {
        var results = AllRequiredPass();
        var modified = results
            .Select(r => r.CheckId == "CHK-PATH-001"
                ? DiagnosticResultBuilder.Repairable("CHK-PATH-001")
                : r)
            .ToList();
        Assert.Equal(OverallReadiness.Repairable, _sut.Aggregate(modified));
    }

    [Fact]
    public void RequiredCheckUserAction_ReturnsUserActionRequired()
    {
        var results = AllRequiredPass();
        var modified = results
            .Select(r => r.CheckId == "CHK-AUTH-001"
                ? DiagnosticResultBuilder.UserAction("CHK-AUTH-001")
                : r)
            .ToList();
        Assert.Equal(OverallReadiness.UserActionRequired, _sut.Aggregate(modified));
    }

    [Fact]
    public void RequiredCheckUnknown_ReturnsUnknown()
    {
        var results = AllRequiredPass();
        var modified = results
            .Select(r => r.CheckId == "CHK-CLAUDE-002"
                ? DiagnosticResultBuilder.Unknown("CHK-CLAUDE-002")
                : r)
            .ToList();
        Assert.Equal(OverallReadiness.Unknown, _sut.Aggregate(modified));
    }

    [Fact]
    public void AllRequiredPass_WithRecommendedWarning_ReturnsReadyWithRecommendations()
    {
        var results = AllRequiredPass();
        results.Add(DiagnosticResultBuilder.Warning("CHK-SHELL-002"));  // pwsh推奨
        Assert.Equal(OverallReadiness.ReadyWithRecommendations, _sut.Aggregate(results));
    }

    [Fact]
    public void AllRequiredPass_NoWarnings_ReturnsReady()
    {
        var results = AllRequiredPass();
        Assert.Equal(OverallReadiness.Ready, _sut.Aggregate(results));
    }

    [Fact]
    public void GitFail_WithPowerShellPass_ReturnsReadyWithRecommendations()
    {
        // Git未導入 + PowerShell利用可能 → 必須エラーにならない
        var results = AllRequiredPass();
        results.Add(DiagnosticResultBuilder.Warning("CHK-GIT-001"));
        Assert.Equal(OverallReadiness.ReadyWithRecommendations, _sut.Aggregate(results));
    }

    [Fact]
    public void OptionalNetworkCheckFailure_DoesNotAffectOverall()
    {
        // ネットワークチェック失敗は全体判定に影響しない
        var results = AllRequiredPass();
        results.Add(DiagnosticResultBuilder.ITAction("CHK-NET-001"));
        Assert.Equal(OverallReadiness.Ready, _sut.Aggregate(results));
    }

    [Fact]
    public void ClaudeDesktopConflict_ITAction_ReturnsITActionRequired()
    {
        var results = AllRequiredPass();
        var modified = results
            .Select(r => r.CheckId == "CHK-CLAUDE-003"
                ? DiagnosticResultBuilder.ITAction("CHK-CLAUDE-003")
                : r)
            .ToList();
        if (!modified.Any(r => r.CheckId == "CHK-CLAUDE-003"))
            modified.Add(DiagnosticResultBuilder.ITAction("CHK-CLAUDE-003"));

        Assert.Equal(OverallReadiness.ITActionRequired, _sut.Aggregate(modified));
    }
}
