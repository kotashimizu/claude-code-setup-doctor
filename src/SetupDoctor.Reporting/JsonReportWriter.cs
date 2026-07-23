using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using SetupDoctor.Core.Abstractions;
using SetupDoctor.Core.Models;

namespace SetupDoctor.Reporting;

public sealed class JsonReportWriter : IReportWriter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task WriteJsonAsync(DiagnosticSession session, string filePath,
        CancellationToken cancellationToken)
    {
        var report = BuildReport(session);
        var json = JsonSerializer.Serialize(report, Options);
        var tmp = filePath + ".tmp";
        await File.WriteAllTextAsync(tmp, json, Encoding.UTF8, cancellationToken);
        File.Move(tmp, filePath, overwrite: true);
    }

    public async Task WriteTextAsync(DiagnosticSession session, string filePath,
        CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Claude Code セットアップ診断レポート ===");
        sb.AppendLine($"セッション ID : {session.SessionId}");
        sb.AppendLine($"開始日時      : {session.StartedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"完了日時      : {session.CompletedAtUtc:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"総合判定      : {session.OverallReadiness}");
        sb.AppendLine();
        sb.AppendLine("--- チェック結果 ---");

        foreach (var r in session.Results)
        {
            sb.AppendLine($"[{r.Status,-16}] {r.CheckId,-20} {r.SummaryKey}");
            if (r.SafeMetadata.Count > 0)
            {
                foreach (var (k, v) in RedactionEngine.Redact(r.SafeMetadata))
                    sb.AppendLine($"               {k} = {v}");
            }
        }

        var tmp = filePath + ".tmp";
        await File.WriteAllTextAsync(tmp, sb.ToString(), Encoding.UTF8, cancellationToken);
        File.Move(tmp, filePath, overwrite: true);
    }

    private static object BuildReport(DiagnosticSession session)
    {
        return new
        {
            sessionId = session.SessionId,
            startedAtUtc = session.StartedAtUtc,
            completedAtUtc = session.CompletedAtUtc,
            overallReadiness = session.OverallReadiness.ToString(),
            checks = session.Results.Select(r => new
            {
                checkId = r.CheckId,
                status = r.Status.ToString(),
                summaryKey = r.SummaryKey,
                detailCode = r.DetailCode,
                durationMs = (long)r.Duration.TotalMilliseconds,
                completedAtUtc = r.CompletedAtUtc,
                metadata = RedactionEngine.Redact(r.SafeMetadata),
            }),
        };
    }
}
