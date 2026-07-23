using SetupDoctor.Core.Models;

namespace SetupDoctor.Core.Abstractions;

public interface ICommandRunner
{
    Task<CommandResult> RunAsync(CommandRequest request, CancellationToken cancellationToken);
}
