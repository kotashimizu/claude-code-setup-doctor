using SetupDoctor.Core.Abstractions;

namespace SetupDoctor.Core.Tests.Infrastructure;

public sealed class FakeSystemInfoProvider : ISystemInfoProvider
{
    public string OsFamily { get; set; } = "Windows";
    public string? OsBuild { get; set; } = "19041";
    public string? Architecture { get; set; } = "X64";
    public double? MemoryGiB { get; set; } = 8.0;
    public bool Is64BitOperatingSystem { get; set; } = true;

    private readonly Dictionary<string, string> _expansions = new(StringComparer.OrdinalIgnoreCase);

    public void SetExpansion(string variable, string value)
        => _expansions[variable] = value;

    public string ExpandEnvironmentVariables(string path)
    {
        var result = path;
        foreach (var (k, v) in _expansions)
            result = result.Replace($"%{k}%", v, StringComparison.OrdinalIgnoreCase);
        return result;
    }
}
