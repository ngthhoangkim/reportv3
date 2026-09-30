using ReportV2.Core.Models;

namespace ReportV2.Application.Abstractions;

public interface IBackfillJobProcessor
{
    Task ProcessAsync(BackfillCandidate candidate, bool dryRun, CancellationToken cancellationToken = default);
}
