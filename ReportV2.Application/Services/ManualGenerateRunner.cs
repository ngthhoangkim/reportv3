using Microsoft.Extensions.Logging;
using ReportV2.Application.Abstractions;
using ReportV2.Core.Models;

namespace ReportV2.Application.Services;

public sealed class ManualGenerateRunner
{
    private readonly IBackfillJobProcessor _jobProcessor;
    private readonly ILogger<ManualGenerateRunner> _logger;

    public ManualGenerateRunner(
        IBackfillJobProcessor jobProcessor,
        ILogger<ManualGenerateRunner> logger)
    {
        _jobProcessor = jobProcessor;
        _logger = logger;
    }

    public async Task RunAsync(
        string fileNum,
        int? sessionId,
        int? progressId,
        string source,
        bool dryRun,
        CancellationToken cancellationToken = default)
    {
        var cleanFileNum = (fileNum ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(cleanFileNum))
        {
            throw new ArgumentException("Missing or invalid --file-num.", nameof(fileNum));
        }

        var cleanSource = string.IsNullOrWhiteSpace(source)
            ? "cdha"
            : source.Trim().ToLowerInvariant();

        if (cleanSource is not ("cdha" or "prescription"))
        {
            throw new ArgumentException("Source must be 'cdha' or 'prescription'.", nameof(source));
        }

        if (cleanSource == "prescription" && progressId is null)
        {
            _logger.LogWarning("Manual prescription generation was requested without ProgressId.");
        }

        var candidate = new BackfillCandidate
        {
            FileNum = cleanFileNum,
            SessionId = sessionId,
            ProgressId = progressId,
            Source = cleanSource,
            LastChangedAt = DateTimeOffset.Now
        };

        _logger.LogInformation(
            "Manual generation started. Source={Source} FileNum={FileNum} SessionId={SessionId} ProgressId={ProgressId} DryRun={DryRun}",
            candidate.Source,
            candidate.FileNum,
            candidate.SessionId,
            candidate.ProgressId,
            dryRun);

        await _jobProcessor.ProcessAsync(candidate, dryRun, cancellationToken);

        _logger.LogInformation(
            "Manual generation completed. Source={Source} FileNum={FileNum} SessionId={SessionId} ProgressId={ProgressId}",
            candidate.Source,
            candidate.FileNum,
            candidate.SessionId,
            candidate.ProgressId);
    }
}
