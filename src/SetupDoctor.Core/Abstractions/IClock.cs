namespace SetupDoctor.Core.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
