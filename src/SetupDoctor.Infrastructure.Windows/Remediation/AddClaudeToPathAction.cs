using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Policies;
using SetupDoctor.Core.Remediation;

namespace SetupDoctor.Infrastructure.Windows.Remediation;

// REM-PATH-CLAUDE-LOCALBIN: %USERPROFILE%\.local\bin を User PATH に追加する
public sealed class AddClaudeToPathAction : IRemediationAction
{
    private readonly IPathEnvironmentService _path;
    private readonly ISystemInfoProvider _sys;

    public string Id => "REM-PATH-CLAUDE-LOCALBIN";

    public AddClaudeToPathAction(IPathEnvironmentService path, ISystemInfoProvider sys)
    {
        _path = path;
        _sys = sys;
    }

    public Task<RemediationPreview> PreviewAsync(RemediationContext context,
        CancellationToken cancellationToken)
    {
        var target = _sys.ExpandEnvironmentVariables(@"%USERPROFILE%\.local\bin");
        var currentPath = _path.GetUserPath() ?? string.Empty;
        var alreadyIn = PathNormalizer.Contains(currentPath, target);

        var plan = new RemediationPlanItem(
            Id,
            "ユーザー PATH に Claude のパスを追加",
            @"%USERPROFILE%\.local\bin",
            alreadyIn ? "（すでに PATH に存在します）" : "PATH に含まれていません",
            "PATH に追加します",
            RequiresElevation: false,
            RequiresNewProcess: true,  // PATH反映には新しいターミナルが必要
            SupportsRollback: true);

        return Task.FromResult(new RemediationPreview(plan, !alreadyIn, alreadyIn
            ? "エントリはすでに User PATH に存在するため、追加は不要です。"
            : null));
    }

    public Task<RemediationExecutionResult> ExecuteAsync(RemediationContext context,
        CancellationToken cancellationToken)
    {
        var target = _sys.ExpandEnvironmentVariables(@"%USERPROFILE%\.local\bin");
        var current = _path.GetUserPath() ?? string.Empty;

        if (PathNormalizer.Contains(current, target))
        {
            return Task.FromResult(new RemediationExecutionResult(
                Id, RemediationStatus.Verified,
                "PATH エントリはすでに存在します。", true));
        }

        var newPath = PathNormalizer.Append(current, target);
        _path.SetUserPath(newPath);

        var after = _path.GetUserPath() ?? string.Empty;
        if (!PathNormalizer.Contains(after, target))
        {
            return Task.FromResult(new RemediationExecutionResult(
                Id, RemediationStatus.Failed,
                "PATH の書き込み後にエントリが確認できませんでした。", false));
        }

        return Task.FromResult(new RemediationExecutionResult(
            Id, RemediationStatus.Verified, null, true));
    }

    public Task<RollbackResult> RollbackAsync(RemediationContext context,
        CancellationToken cancellationToken)
    {
        var target = _sys.ExpandEnvironmentVariables(@"%USERPROFILE%\.local\bin");
        var current = _path.GetUserPath() ?? string.Empty;

        if (!PathNormalizer.Contains(current, target))
        {
            return Task.FromResult(new RollbackResult(Id, true, null));
        }

        // エントリを除去して再構築する
        var entries = PathNormalizer.Split(current)
            .Where(e => !PathNormalizer.Normalize(e).Equals(
                PathNormalizer.Normalize(target), StringComparison.Ordinal))
            .ToArray();

        _path.SetUserPath(string.Join(";", entries));
        return Task.FromResult(new RollbackResult(Id, true, null));
    }
}
