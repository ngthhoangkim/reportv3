using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReportV2.Application.Abstractions;
using ReportV2.Application.Options;
using ReportV2.Core.Models;

namespace ReportV2.Application.Services;

public sealed class YearBackfillRunner
{
    private readonly IBackfillCandidateSource _candidateSource;
    private readonly IBackfillCursorStore _cursorStore;
    private readonly IBackfillJobProcessor _jobProcessor;
    private readonly BackfillOptions _options;
    private readonly ILogger<YearBackfillRunner> _logger;

    public YearBackfillRunner(
        IBackfillCandidateSource candidateSource,
        IBackfillCursorStore cursorStore,
        IBackfillJobProcessor jobProcessor,
        IOptions<BackfillOptions> options,
        ILogger<YearBackfillRunner> logger)
    {
        _candidateSource = candidateSource;
        _cursorStore = cursorStore;
        _jobProcessor = jobProcessor;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RunAsync(
        int year,
        bool resume,
        bool dryRun,
        CancellationToken cancellationToken = default)
    {
        if (year < 2000 || year > DateTimeOffset.Now.Year)
        {
            throw new ArgumentOutOfRangeException(nameof(year), year, "Backfill year is out of range.");
        }

        var today = DateOnly.FromDateTime(DateTimeOffset.Now.Date);
        var yearStart = new DateOnly(year, 1, 1);
        var yearEndExclusive = year == today.Year
            ? today.AddDays(1)
            : new DateOnly(year + 1, 1, 1);

        var cursor = resume
            ? await _cursorStore.LoadAsync(year, cancellationToken)
            : null;

        var current = cursor?.CurrentDate > yearStart ? cursor.CurrentDate : yearStart;
        var processed = cursor?.Processed ?? 0;
        var failed = cursor?.Failed ?? 0;
        var sources = _options.Sources
            .Where(source => !string.IsNullOrWhiteSpace(source))
            .Select(source => source.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation(
            "Year backfill started. Year={Year} CurrentDate={CurrentDate} EndExclusive={EndExclusive} DryRun={DryRun} Resume={Resume}",
            year,
            current,
            yearEndExclusive,
            dryRun,
            resume);

        while (current < yearEndExclusive)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var next = Min(current.AddDays(Math.Max(1, _options.ChunkDays)), yearEndExclusive);
            var from = current.ToDateTime(TimeOnly.MinValue);
            var to = next.ToDateTime(TimeOnly.MinValue);

            var candidates = await _candidateSource.GetCandidatesAsync(
                new DateTimeOffset(from),
                new DateTimeOffset(to),
                sources,
                _options.MaxCandidatesPerChunk,
                cancellationToken);

            _logger.LogInformation(
                "Backfill chunk scanned. Year={Year} From={FromDate} To={ToDate} Candidates={CandidateCount}",
                year,
                current,
                next,
                candidates.Count);

            foreach (var candidate in candidates)
            {
                try
                {
                    await _jobProcessor.ProcessAsync(candidate, dryRun, cancellationToken);
                    processed++;
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(
                        ex,
                        "Backfill candidate failed. Source={Source} FileNum={FileNum} SessionId={SessionId} ProgressId={ProgressId}",
                        candidate.Source,
                        candidate.FileNum,
                        candidate.SessionId,
                        candidate.ProgressId);
                }
            }

            current = next;
            await _cursorStore.SaveAsync(new BackfillCursor
            {
                Year = year,
                CurrentDate = current,
                Processed = processed,
                Failed = failed,
                UpdatedAt = DateTimeOffset.UtcNow
            }, cancellationToken);

            if (_options.DelayBetweenChunksMs > 0 && current < yearEndExclusive)
            {
                await Task.Delay(_options.DelayBetweenChunksMs, cancellationToken);
            }
        }

        _logger.LogInformation(
            "Year backfill completed. Year={Year} Processed={Processed} Failed={Failed}",
            year,
            processed,
            failed);
    }

    private static DateOnly Min(DateOnly left, DateOnly right) => left <= right ? left : right;
}
