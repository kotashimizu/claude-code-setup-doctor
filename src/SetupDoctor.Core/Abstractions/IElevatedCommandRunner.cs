namespace SetupDoctor.Core.Abstractions;

// UAC昇格(runas)を伴うコマンド実行。UseShellExecute=trueが必須のため、
// ICommandRunner(標準出力/標準エラーをリダイレクトする通常実行)とは別経路として扱う。
// 昇格プロンプトはOS標準のものを使用し、本体アプリは非昇格のまま維持する。
public interface IElevatedCommandRunner
{
    // 戻り値は終了コードのみ(UseShellExecute=trueのためstdout/stderrはリダイレクトできない)。
    // ユーザーがUACプロンプトで拒否した場合はOperationCanceledExceptionを投げる。
    Task<int> RunElevatedAsync(
        string executablePath,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}
