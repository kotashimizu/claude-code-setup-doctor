using SetupDoctor.Core.Abstractions;

namespace SetupDoctor.Infrastructure.Windows.Files;

public sealed class WindowsFileBackupService : IFileBackupService
{
    public async Task<string> BackupAsync(string sourcePath, string backupDirectory,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("バックアップ元のファイルが存在しません。", sourcePath);

        Directory.CreateDirectory(backupDirectory);

        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        var fileName = Path.GetFileName(sourcePath);
        var backupPath = Path.Combine(backupDirectory,
            $"{fileName}.setup-doctor-backup-{timestamp}");

        // 一時ファイル→移動で原子的にバックアップ
        var tmp = backupPath + ".tmp";
        await using (var src = File.OpenRead(sourcePath))
        await using (var dst = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write,
            FileShare.None, 81920, useAsync: true))
        {
            await src.CopyToAsync(dst, cancellationToken);
            await dst.FlushAsync(cancellationToken);
        }

        File.Move(tmp, backupPath, overwrite: false);
        return backupPath;
    }

    public async Task RestoreAsync(string backupPath, string targetPath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(backupPath))
            throw new FileNotFoundException("バックアップファイルが存在しません。", backupPath);

        var tmp = targetPath + ".restore.tmp";
        await using (var src = File.OpenRead(backupPath))
        await using (var dst = new FileStream(tmp, FileMode.Create, FileAccess.Write,
            FileShare.None, 81920, useAsync: true))
        {
            await src.CopyToAsync(dst, cancellationToken);
            await dst.FlushAsync(cancellationToken);
        }

        File.Move(tmp, targetPath, overwrite: true);
    }
}
