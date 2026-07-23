using SetupDoctor.Core.Abstractions;

namespace SetupDoctor.Core.Tests.Infrastructure;

public sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new DateTimeOffset(2026, 7, 23, 0, 0, 0, TimeSpan.Zero);
}
