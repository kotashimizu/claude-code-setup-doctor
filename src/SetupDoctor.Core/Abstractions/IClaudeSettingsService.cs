namespace SetupDoctor.Core.Abstractions;

public interface IClaudeSettingsService
{
    Task<bool> ExistsAsync(CancellationToken cancellationToken);
    Task<string?> GetGitBashPathAsync(CancellationToken cancellationToken);
    Task SetGitBashPathAsync(string bashPath, string backupDirectory, CancellationToken cancellationToken);
}
