using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using SetupDoctor.Core.Abstractions;

namespace SetupDoctor.Infrastructure.Windows.Environment;

public sealed class WindowsClaudeSettingsService : IClaudeSettingsService
{
    private const string EnvKey = "CLAUDE_CODE_GIT_BASH_PATH";

    private static string SettingsPath =>
        Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
            ".claude", "settings.json");

    public Task<bool> ExistsAsync(CancellationToken cancellationToken)
        => Task.FromResult(File.Exists(SettingsPath));

    public async Task<string?> GetGitBashPathAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(SettingsPath)) return null;

        try
        {
            var json = await File.ReadAllTextAsync(SettingsPath, Encoding.UTF8, cancellationToken);
            var node = JsonNode.Parse(json);
            return node?["env"]?[EnvKey]?.GetValue<string>();
        }
        catch { return null; }
    }

    public async Task SetGitBashPathAsync(string bashPath, string backupDirectory, CancellationToken cancellationToken)
    {
        // 前提条件チェック
        if (!File.Exists(bashPath))
            throw new FileNotFoundException("Git Bash executable not found", bashPath);

        // コメント付きJSONは自動変更しない
        var settingsPath = SettingsPath;
        var dir = Path.GetDirectoryName(settingsPath)!;
        Directory.CreateDirectory(dir);

        // バックアップ
        if (File.Exists(settingsPath))
        {
            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
            var backupPath = Path.Combine(backupDirectory,
                $"settings.json.setup-doctor-backup-{timestamp}");
            File.Copy(settingsPath, backupPath, overwrite: false);
        }

        // 既存JSONを読み込んでマージ
        JsonObject root;
        if (File.Exists(settingsPath))
        {
            var existing = await File.ReadAllTextAsync(settingsPath, Encoding.UTF8, cancellationToken);
            try
            {
                root = JsonNode.Parse(existing)?.AsObject() ?? new JsonObject();
            }
            catch (JsonException)
            {
                throw new InvalidOperationException(
                    "settings.json のJSON形式が不正です。手動で確認してください。");
            }
        }
        else
        {
            root = new JsonObject();
        }

        // env オブジェクトを取得または作成
        if (root["env"] is not JsonObject envObj)
        {
            envObj = new JsonObject();
            root["env"] = envObj;
        }

        envObj[EnvKey] = JsonValue.Create(bashPath);

        // 一時ファイル経由で原子的保存
        var tmp = settingsPath + ".tmp";
        var options = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(tmp, root.ToJsonString(options), Encoding.UTF8, cancellationToken);

        // 再パースして値確認
        var verification = JsonNode.Parse(await File.ReadAllTextAsync(tmp, Encoding.UTF8, cancellationToken));
        if (verification?["env"]?[EnvKey]?.GetValue<string>() != bashPath)
            throw new InvalidOperationException("設定ファイルの書き込み検証に失敗しました。");

        File.Move(tmp, settingsPath, overwrite: true);
    }
}
