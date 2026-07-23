namespace SetupDoctor.Core.Models;

public sealed record CommandRequest(
    string ExecutablePath,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    TimeSpan Timeout,
    bool CaptureOutput = true);
