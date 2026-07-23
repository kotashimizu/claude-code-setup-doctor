using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Models;
using SetupDoctor.Core.Policies;

namespace SetupDoctor.Core.Diagnostics;

public sealed class DiagnosticOrchestrator : IDiagnosticOrchestrator
{
    private readonly IReadOnlyList<IDiagnosticCheck> _basicChecks;
    private readonly ReadinessAggregator _aggregator;
    private readonly IClock _clock;

    public DiagnosticOrchestrator(
        IReadOnlyList<IDiagnosticCheck> basicChecks,
        ReadinessAggregator aggregator,
        IClock clock)
    {
        _basicChecks = basicChecks;
        _aggregator = aggregator;
        _clock = clock;
    }

    public async Task<DiagnosticSession> RunBasicDiagnosticsAsync(
        IProgress<DiagnosticResult>? progress,
        CancellationToken cancellationToken)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var startedAt = _clock.UtcNow;
        var context = DiagnosticContext.Create(sessionId, startedAt);

        foreach (var check in _basicChecks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DiagnosticResult result;
            try
            {
                result = await check.RunAsync(context, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                result = new DiagnosticResult(
                    check.Id,
                    DiagnosticStatus.Unknown,
                    "CHECK_EXCEPTION",
                    ex.GetType().Name,
                    new Dictionary<string, string>(),
                    TimeSpan.Zero,
                    _clock.UtcNow);
            }

            context = context.WithResult(result);
            progress?.Report(result);
        }

        var readiness = _aggregator.Aggregate(context.PriorResults);

        return new DiagnosticSession(
            sessionId,
            startedAt,
            _clock.UtcNow,
            context.PriorResults,
            readiness);
    }
}
