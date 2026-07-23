namespace SetupDoctor.Core.Remediation;

public sealed class RemediationOrchestrator : IRemediationOrchestrator
{
    private readonly IReadOnlyDictionary<string, IRemediationAction> _actions;

    public RemediationOrchestrator(IEnumerable<IRemediationAction> actions)
    {
        _actions = actions.ToDictionary(a => a.Id, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<RemediationExecutionResult>> ExecuteAsync(
        IReadOnlyList<RemediationPlanItem> plan,
        IProgress<RemediationExecutionResult>? progress,
        CancellationToken cancellationToken)
    {
        var results = new List<RemediationExecutionResult>(plan.Count);

        foreach (var item in plan)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_actions.TryGetValue(item.RemediationId, out var action))
            {
                var unknown = new RemediationExecutionResult(
                    item.RemediationId, RemediationStatus.Failed,
                    $"修復アクション '{item.RemediationId}' が見つかりません。", false);
                results.Add(unknown);
                progress?.Report(unknown);
                continue;
            }

            var backupDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".claude-setup-doctor", "backups",
                DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss"));

            var ctx = new RemediationContext(item.RemediationId, backupDir,
                new Dictionary<string, string>());

            RemediationExecutionResult result;
            try
            {
                result = await action.ExecuteAsync(ctx, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                result = new RemediationExecutionResult(
                    item.RemediationId, RemediationStatus.Failed,
                    $"予期しないエラーが発生しました: {ex.Message}", false);
            }

            results.Add(result);
            progress?.Report(result);
        }

        return results;
    }
}
