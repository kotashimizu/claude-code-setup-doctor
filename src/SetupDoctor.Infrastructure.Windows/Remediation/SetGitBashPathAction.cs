using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Remediation;

namespace SetupDoctor.Infrastructure.Windows.Remediation;

// REM-CLAUDE-GITBASH-SETTING: settings.json に CLAUDE_CODE_GIT_BASH_PATH を設定する
public sealed class SetGitBashPathAction : IRemediationAction
{
    private const string DefaultBashPath = @"C:\Program Files\Git\bin\bash.exe";

    private readonly IClaudeSettingsService _settings;
    private readonly IFileBackupService _backup;

    public string Id => "REM-CLAUDE-GITBASH-SETTING";

    public SetGitBashPathAction(IClaudeSettingsService settings, IFileBackupService backup)
    {
        _settings = settings;
        _backup = backup;
    }

    public async Task<RemediationPreview> PreviewAsync(RemediationContext context,
        CancellationToken cancellationToken)
    {
        var current = await _settings.GetGitBashPathAsync(cancellationToken);
        var bashPath = context.Parameters.TryGetValue("bashPath", out var p) ? p : DefaultBashPath;
        var bashExists = File.Exists(bashPath);

        var plan = new RemediationPlanItem(
            Id,
            "Git Bash パスを Claude 設定に追加",
            "~/.claude/settings.json",
            current is null ? "（未設定）" : $"現在: {current}",
            $"CLAUDE_CODE_GIT_BASH_PATH = {bashPath}",
            RequiresElevation: false,
            RequiresNewProcess: true,
            SupportsRollback: true);

        return Task.FromResult(new RemediationPreview(plan, bashExists,
            bashExists ? null
                : $"Git Bash が {bashPath} に見つかりません。インストール後に再実行してください。"))
            .GetAwaiter().GetResult();
    }

    public async Task<RemediationExecutionResult> ExecuteAsync(RemediationContext context,
        CancellationToken cancellationToken)
    {
        var bashPath = context.Parameters.TryGetValue("bashPath", out var p) ? p : DefaultBashPath;

        if (!File.Exists(bashPath))
        {
            return new RemediationExecutionResult(
                Id, RemediationStatus.Failed,
                $"Git Bash が {bashPath} に見つかりません。", false);
        }

        try
        {
            await _settings.SetGitBashPathAsync(bashPath, context.BackupDirectory, cancellationToken);
        }
        catch (Exception ex)
        {
            return new RemediationExecutionResult(
                Id, RemediationStatus.Failed, ex.Message, false);
        }

        // 事後検証
        var after = await _settings.GetGitBashPathAsync(cancellationToken);
        if (!string.Equals(after, bashPath, StringComparison.OrdinalIgnoreCase))
        {
            return new RemediationExecutionResult(
                Id, RemediationStatus.Failed,
                "設定ファイルへの書き込み後に値が確認できませんでした。", false);
        }

        return new RemediationExecutionResult(Id, RemediationStatus.Verified, null, true);
    }

    public async Task<RollbackResult> RollbackAsync(RemediationContext context,
        CancellationToken cancellationToken)
    {
        // バックアップディレクトリから settings.json.setup-doctor-backup-* を探して復元する
        var backupDir = context.BackupDirectory;
        if (!Directory.Exists(backupDir))
            return new RollbackResult(Id, true, "バックアップが存在しないため、ロールバック不要と判断しました。");

        var backups = Directory.GetFiles(backupDir, "settings.json.setup-doctor-backup-*")
            .OrderByDescending(f => f)
            .ToArray();

        if (backups.Length == 0)
            return new RollbackResult(Id, true, "バックアップファイルが見つかりませんでした。ロールバック不要の可能性があります。");

        var latestBackup = backups[0];
        var settingsPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
            ".claude", "settings.json");

        try
        {
            await _backup.RestoreAsync(latestBackup, settingsPath, cancellationToken);
            return new RollbackResult(Id, true, null);
        }
        catch (Exception ex)
        {
            return new RollbackResult(Id, false, $"ロールバック失敗: {ex.Message}");
        }
    }
}
