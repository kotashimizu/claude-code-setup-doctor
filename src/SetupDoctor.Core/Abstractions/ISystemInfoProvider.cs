namespace SetupDoctor.Core.Abstractions;

public interface ISystemInfoProvider
{
    string OsFamily { get; }
    string? OsBuild { get; }
    string? Architecture { get; }
    double? MemoryGiB { get; }
    bool Is64BitOperatingSystem { get; }
    string ExpandEnvironmentVariables(string path);
}
