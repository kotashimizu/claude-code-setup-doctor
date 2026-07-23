namespace SetupDoctor.Core.Abstractions;

public interface IFileBackupService
{
    Task<string> BackupAsync(string sourcePath, string backupDirectory, CancellationToken cancellationToken);
    Task RestoreAsync(string backupPath, string targetPath, CancellationToken cancellationToken);
}
