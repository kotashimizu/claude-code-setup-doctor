using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Models;

namespace SetupDoctor.Core.Tests.Infrastructure;

public sealed class FakeCommandRunner : ICommandRunner
{
    private readonly Dictionary<string, CommandResult> _responses = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string executablePath, CommandResult result)
        => _responses[executablePath] = result;

    public Task<CommandResult> RunAsync(CommandRequest request, CancellationToken cancellationToken)
    {
        if (_responses.TryGetValue(request.ExecutablePath, out var result))
            return Task.FromResult(result);

        return Task.FromResult(new CommandResult(
            ExitCode: -1,
            StandardOutput: string.Empty,
            StandardError: $"FakeCommandRunner: no response registered for '{request.ExecutablePath}'",
            Duration: TimeSpan.Zero,
            TimedOut: false));
    }
}
