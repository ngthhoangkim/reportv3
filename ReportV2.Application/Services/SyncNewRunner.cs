using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReportV2.Application.Abstractions;
using ReportV2.Application.Options;
using ReportV2.Core.Models;

namespace ReportV2.Application.Services;

public sealed class SyncNewRunner
{
    private readonly IBackfillCandidateSource _candidateSource;
    private readonly ISyncCursorStore _cursorStore;
    private readonly IBackfillJobProcessor _jobProcessor;
    private readonly SyncNewOptions _options;
    private readonly ILogger<SyncNewRunner> _logger;

    public SyncNewRunner(
        IBackfillCandidateSource candidateSource,
        ISyncCursorStore cursorStore,
        IBackfillJobProcessor jobProcessor,
        IOptions<SyncNewOptions> options,
        ILogger<SyncNewRunner> logger)
    {
        _candidateSource = candidateSource;
        _cursorStore = cursorStore;
        _jobProcessor = jobProcessor;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RunAsync(
        bool resume,
        bool dryRun,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.Now;
        var scanTo = now.AddMinutes(-Math.Max(0, _options.ScanToDelayMinutes));
        var cursor = resume
            ? await _cursorStore.LoadAsync(cancellationToken)
            : null;

        var scanFrom = cursor is null
            ? scanTo.AddHours(-Math.Max(1, _options.LookbackHours))
            : cursor.LastSuccessfulScanTo.AddHours(-Math.Max(1, _options.LookbackHours));

        var processed = cursor?.Processed ?? 0;
        var failed = cursor?.Failed ?? 0;
        var chunkHours = Math.Max(1, _options.ChunkHours);
        var sources = _options.Sources
            .Where(source => !string.IsNullOrWhiteSpace(source))
            .Select(source => source.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation(
            "Sync-new started. From={From} To={To} DryRun={DryRun} Resume={Resume}",
            scanFrom,
            scanTo,
            dryRun,
            resume);

        var current = scanFrom;
        while (current < scanTo)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var next = Min(current.AddHours(chunkHours), scanTo);
            var candidates = await _candidateSource.GetCandidatesAsync(
                current,
                next,
                sources,
                _options.MaxCandidatesPerChunk,
                cancellationToken);

            _logger.LogInformation(
                "Sync-new chunk scanned. From={From} To={To} Candidates={CandidateCount}",
                current,
                next,
                candidates.Count);

            if (candidates.Count >= _options.MaxCandidatesPerChunk)
            {
                throw new InvalidOperationException(
                    $"Sync-new chunk hit MaxCandidatesPerChunk={_options.MaxCandidatesPerChunk}. " +
                    "Reduce SyncNew:ChunkHours or increase the limit before resuming.");
            }

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
                        "Sync-new candidate failed. Source={Source} FileNum={FileNum} SessionId={SessionId} ProgressId={ProgressId}",
                        candidate.Source,
                        candidate.FileNum,
                        candidate.SessionId,
                        candidate.ProgressId);
                }
            }

            current = next;
            await _cursorStore.SaveAsync(new SyncCursor
            {
                LastSuccessfulScanTo = current,
                Processed = processed,
                Failed = failed,
                UpdatedAt = DateTimeOffset.UtcNow
            }, cancellationToken);

            if (_options.DelayBetweenChunksMs > 0 && current < scanTo)
            {
                await Task.Delay(_options.DelayBetweenChunksMs, cancellationToken);
            }
        }

        _logger.LogInformation(
            "Sync-new completed. LastSuccessfulScanTo={ScanTo} Processed={Processed} Failed={Failed}",
            scanTo,
            processed,
            failed);
    }

    private static DateTimeOffset Min(DateTimeOffset left, DateTimeOffset right) => left <= right ? left : right;
}
