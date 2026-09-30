using ReportV2.Core.Models;

namespace ReportV2.Application.Abstractions;

public interface ICdhaReportRepository
{
    Task<IReadOnlyList<CdhaRenderRecord>> GetRenderRecordsAsync(
        string fileNum,
        int? sessionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, IReadOnlyList<PathologyImageRecord>>> GetPathologyImagesAsync(
        IReadOnlyCollection<int> resultIds,
        bool printedOnly,
        CancellationToken cancellationToken = default);
}
