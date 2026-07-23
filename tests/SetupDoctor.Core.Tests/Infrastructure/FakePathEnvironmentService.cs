using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Policies;

namespace SetupDoctor.Core.Tests.Infrastructure;

public sealed class FakePathEnvironmentService : IPathEnvironmentService
{
    public string? UserPath { get; set; }
    public string? ProcessPath { get; set; }

    public string? GetUserPath() => UserPath;
    public string? GetProcessPath() => ProcessPath;

    public void SetUserPath(string path) => UserPath = path;

    public IReadOnlyList<string> GetUserPathEntries() => PathNormalizer.Split(UserPath);

    public bool UserPathContains(string normalizedEntry)
        => PathNormalizer.Contains(UserPath, normalizedEntry);
}
