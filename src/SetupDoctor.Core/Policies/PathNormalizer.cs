namespace SetupDoctor.Core.Policies;

public static class PathNormalizer
{
    // User PATHエントリを比較用に正規化する。
    // 末尾区切り・引用符・大文字小文字の差異を吸収する。
    public static string Normalize(string entry)
    {
        var s = entry.Trim().Trim('"').TrimEnd('\\', '/').ToLowerInvariant();
        return s;
    }

    public static IReadOnlyList<string> Split(string? userPath)
    {
        if (string.IsNullOrEmpty(userPath))
            return Array.Empty<string>();

        return userPath
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim())
            .Where(e => e.Length > 0)
            .ToArray();
    }

    public static bool Contains(string? userPath, string targetExpanded)
    {
        var target = Normalize(targetExpanded);
        return Split(userPath).Any(e => Normalize(e) == target);
    }

    public static string Append(string? userPath, string newEntry)
    {
        var entries = Split(userPath).ToList();
        var norm = Normalize(newEntry);
        if (entries.Any(e => Normalize(e) == norm))
            return userPath ?? string.Empty;

        return string.IsNullOrEmpty(userPath)
            ? newEntry
            : $"{userPath.TrimEnd(';')};{newEntry}";
    }
}
