namespace SetupDoctor.Core.Abstractions;

public interface IPathEnvironmentService
{
    string? GetUserPath();
    void SetUserPath(string path);
    string? GetProcessPath();
    IReadOnlyList<string> GetUserPathEntries();
    bool UserPathContains(string normalizedEntry);
}
