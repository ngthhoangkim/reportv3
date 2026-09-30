using ReportV2.Core.Models;

namespace ReportV2.Application.Abstractions;

public interface IBackfillCandidateSource
{
    Task<IReadOnlyList<BackfillCandidate>> GetCandidatesAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        IReadOnlySet<string> sources,
        int maxCandidates,
        CancellationToken cancellationToken = default);
}
