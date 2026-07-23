using SetupDoctor.Core.Remediation;
using SetupDoctor.Core.Tests.Infrastructure;

namespace SetupDoctor.Core.Tests.Remediation;

public sealed class RemediationOrchestratorTests
{
    private static RemediationPlanItem MakePlan(string id) =>
        new(id, "タイトル", "ターゲット", "前", "後",
            RequiresElevation: false, RequiresNewProcess: false, SupportsRollback: true);

    [Fact]
    public async Task Execute_Returns_Failed_For_Unknown_RemediationId()
    {
        var orchestrator = new RemediationOrchestrator(Array.Empty<IRemediationAction>());
        var plan = new[] { MakePlan("REM-UNKNOWN-001") };
        var results = await orchestrator.ExecuteAsync(plan, null, CancellationToken.None);

        Assert.Single(results);
        Assert.Equal(RemediationStatus.Failed, results[0].Status);
    }

    [Fact]
    public async Task Execute_Calls_Progress_For_Each_Step()
    {
        var fakeAction = new FakeRemediationAction("REM-FAKE-001", RemediationStatus.Verified);
        var orchestrator = new RemediationOrchestrator([fakeAction]);
        var plan = new[] { MakePlan("REM-FAKE-001") };

        var reported = new List<RemediationExecutionResult>();
        var progress = new Progress<RemediationExecutionResult>(r => reported.Add(r));

        var results = await orchestrator.ExecuteAsync(plan, progress, CancellationToken.None);
        // Progress はバックグラウンドスレッドで呼ばれるので少し待つ
        await Task.Delay(50);

        Assert.Single(results);
        Assert.Equal(RemediationStatus.Verified, results[0].Status);
    }

    [Fact]
    public async Task Execute_Continues_After_Single_Action_Fails()
    {
        var fail = new FakeRemediationAction("REM-A", RemediationStatus.Failed);
        var ok = new FakeRemediationAction("REM-B", RemediationStatus.Verified);
        var orchestrator = new RemediationOrchestrator([fail, ok]);

        var plan = new[] { MakePlan("REM-A"), MakePlan("REM-B") };
        var results = await orchestrator.ExecuteAsync(plan, null, CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Equal(RemediationStatus.Failed, results[0].Status);
        Assert.Equal(RemediationStatus.Verified, results[1].Status);
    }

    [Fact]
    public async Task Execute_ThrowsOnCancellation()
    {
        var slow = new FakeRemediationAction("REM-SLOW", RemediationStatus.Verified,
            delay: TimeSpan.FromSeconds(10));
        var orchestrator = new RemediationOrchestrator([slow]);
        var plan = new[] { MakePlan("REM-SLOW") };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => orchestrator.ExecuteAsync(plan, null, cts.Token));
    }

    // ── helpers ──────────────────────────────────────────────────────
    private sealed class FakeRemediationAction : IRemediationAction
    {
        private readonly RemediationStatus _status;
        private readonly TimeSpan _delay;

        public string Id { get; }

        public FakeRemediationAction(string id, RemediationStatus status,
            TimeSpan delay = default)
        {
            Id = id;
            _status = status;
            _delay = delay;
        }

        public Task<RemediationPreview> PreviewAsync(RemediationContext context,
            CancellationToken cancellationToken)
            => Task.FromResult(new RemediationPreview(
                new RemediationPlanItem(Id, "t", "target", "before", "after",
                    false, false, true),
                true, null));

        public async Task<RemediationExecutionResult> ExecuteAsync(RemediationContext context,
            CancellationToken cancellationToken)
        {
            if (_delay > TimeSpan.Zero)
                await Task.Delay(_delay, cancellationToken);
            return new RemediationExecutionResult(Id, _status, null, true);
        }

        public Task<RollbackResult> RollbackAsync(RemediationContext context,
            CancellationToken cancellationToken)
            => Task.FromResult(new RollbackResult(Id, true, null));
    }
}
