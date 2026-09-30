using Microsoft.Extensions.Logging;
using ReportV2.Application.Abstractions;
using ReportV2.Core.Models;

namespace ReportV2.Application.Services;

public sealed class NoopBackfillJobProcessor : IBackfillJobProcessor
{
    private readonly ILogger<NoopBackfillJobProcessor> _logger;

    public NoopBackfillJobProcessor(ILogger<NoopBackfillJobProcessor> logger)
    {
        _logger = logger;
    }

    public Task ProcessAsync(BackfillCandidate candidate, bool dryRun, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Backfill candidate discovered. Source={Source} FileNum={FileNum} SessionId={SessionId} ProgressId={ProgressId} DryRun={DryRun}",
            candidate.Source,
            candidate.FileNum,
            candidate.SessionId,
            candidate.ProgressId,
            dryRun);

        return Task.CompletedTask;
    }
}
