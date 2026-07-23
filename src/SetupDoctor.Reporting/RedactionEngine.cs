using System.Text.RegularExpressions;

namespace SetupDoctor.Reporting;

public static partial class RedactionEngine
{
    // メールアドレスのパターン
    [GeneratedRegex(@"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}", RegexOptions.None)]
    private static partial Regex EmailRegex();

    // トークン・APIキー様のパターン（32文字以上の英数字記号列）
    [GeneratedRegex(@"[A-Za-z0-9\-_]{32,}", RegexOptions.None)]
    private static partial Regex TokenRegex();

    // USERPROFILE の展開値をマスクする（ユーザー名漏洩防止）
    private static readonly string UserProfilePath =
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public static string Redact(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var s = input;

        // ユーザープロファイルパスをマスク
        if (!string.IsNullOrEmpty(UserProfilePath))
            s = s.Replace(UserProfilePath, "%USERPROFILE%", StringComparison.OrdinalIgnoreCase);

        // メールアドレス
        s = EmailRegex().Replace(s, "[EMAIL_REDACTED]");

        // 長いトークン様文字列
        s = TokenRegex().Replace(s, "[TOKEN_REDACTED]");

        return s;
    }

    // 辞書全体をリダクション
    public static IReadOnlyDictionary<string, string> Redact(
        IReadOnlyDictionary<string, string> metadata)
    {
        return metadata.ToDictionary(
            kv => kv.Key,
            kv => Redact(kv.Value));
    }
}
