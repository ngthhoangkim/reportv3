using Dapper;
using Microsoft.Extensions.Logging;
using ReportV2.Application.Abstractions;
using ReportV2.Core.Models;
using ReportV2.Infrastructure.Sql;

namespace ReportV2.Infrastructure.Backfill;

public sealed class HisBackfillCandidateSource : IBackfillCandidateSource
{
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly ILogger<HisBackfillCandidateSource> _logger;

    public HisBackfillCandidateSource(
        SqlConnectionFactory connectionFactory,
        ILogger<HisBackfillCandidateSource> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BackfillCandidate>> GetCandidatesAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        IReadOnlySet<string> sources,
        int maxCandidates,
        CancellationToken cancellationToken = default)
    {
        var enabled = sources.Count == 0
            ? new HashSet<string>(["cdha"], StringComparer.OrdinalIgnoreCase)
            : sources;

        var candidates = new Dictionary<string, BackfillCandidate>(StringComparer.OrdinalIgnoreCase);

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        if (enabled.Contains("cdha"))
        {
            await AddCandidatesAsync(
                candidates,
                await QueryCdhaAsync(connection, from, to, maxCandidates),
                maxCandidates);
        }

        if (enabled.Contains("prescription"))
        {
            await AddCandidatesAsync(
                candidates,
                await QueryPrescriptionAsync(connection, from, to, maxCandidates),
                maxCandidates);
        }

        _logger.LogDebug(
            "HIS candidates loaded. From={From} To={To} Count={Count}",
            from,
            to,
            candidates.Count);

        return candidates.Values
            .OrderBy(candidate => candidate.LastChangedAt)
            .ThenBy(candidate => candidate.FileNum, StringComparer.OrdinalIgnoreCase)
            .Take(maxCandidates)
            .ToArray();
    }

    private static async Task AddCandidatesAsync(
        Dictionary<string, BackfillCandidate> target,
        IEnumerable<BackfillCandidate> candidates,
        int maxCandidates)
    {
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate.FileNum))
            {
                continue;
            }

            var key = CandidateKey(candidate);
            if (!target.TryGetValue(key, out var existing) ||
                string.CompareOrdinal(
                    candidate.LastChangedAt?.ToString("O"),
                    existing.LastChangedAt?.ToString("O")) > 0)
            {
                target[key] = candidate;
            }

            if (target.Count >= maxCandidates)
            {
                break;
            }
        }

        await Task.CompletedTask;
    }

    private static async Task<IEnumerable<BackfillCandidate>> QueryCdhaAsync(
        System.Data.IDbConnection connection,
        DateTimeOffset from,
        DateTimeOffset to,
        int maxCandidates)
    {
        const string sql = """
            SELECT TOP (@MaxCandidates)
              CONVERT(VARCHAR(50), v.FileNum) AS FileNum,
              v.SessionId,
              CAST(NULL AS INT) AS ProgressId,
              'cdha' AS Source,
              MAX(COALESCE(r.UpdatedDate, r.FinishDate, r.CreatedDate)) AS LastChangedAt
            FROM dbo.CN_ImagingResult r WITH (NOLOCK)
            INNER JOIN dbo.ViewImagingResult v WITH (NOLOCK) ON v.Id = r.Id
            WHERE r.DeletedDate IS NULL
              AND COALESCE(r.UpdatedDate, r.FinishDate, r.CreatedDate) >= @From
              AND COALESCE(r.UpdatedDate, r.FinishDate, r.CreatedDate) < @To
            GROUP BY v.FileNum, v.SessionId
            ORDER BY LastChangedAt ASC
            """;

        return await connection.QueryAsync<BackfillCandidate>(sql, new
        {
            From = from.LocalDateTime,
            To = to.LocalDateTime,
            MaxCandidates = maxCandidates
        });
    }

    private static async Task<IEnumerable<BackfillCandidate>> QueryPrescriptionAsync(
        System.Data.IDbConnection connection,
        DateTimeOffset from,
        DateTimeOffset to,
        int maxCandidates)
    {
        const string sql = """
            SELECT TOP (@MaxCandidates)
              CONVERT(VARCHAR(50), p.FileNum) AS FileNum,
              rx.SessionId,
              rx.ProgressID AS ProgressId,
              'prescription' AS Source,
              MAX(rx.CreatedDate) AS LastChangedAt
            FROM dbo.ViewRX rx WITH (NOLOCK)
            INNER JOIN dbo.CR_Patient p WITH (NOLOCK) ON p.ContactId = rx.PatientID
            WHERE rx.DeletedDate IS NULL
              AND rx.CreatedDate >= @From
              AND rx.CreatedDate < @To
            GROUP BY p.FileNum, rx.SessionId, rx.ProgressID
            ORDER BY LastChangedAt ASC
            """;

        return await connection.QueryAsync<BackfillCandidate>(sql, new
        {
            From = from.LocalDateTime,
            To = to.LocalDateTime,
            MaxCandidates = maxCandidates
        });
    }

    private static string CandidateKey(BackfillCandidate candidate)
    {
        return string.Equals(candidate.Source, "prescription", StringComparison.OrdinalIgnoreCase)
            ? $"{candidate.Source}::{candidate.FileNum}::{candidate.SessionId?.ToString() ?? "all"}::{candidate.ProgressId?.ToString() ?? "all"}"
            : $"{candidate.Source}::{candidate.FileNum}::{candidate.SessionId?.ToString() ?? "all"}";
    }
}
