using ReportV2.Core.Models;

namespace ReportV2.Application.Abstractions;

public interface IPrescriptionRepository
{
    Task<IReadOnlyList<PrescriptionDocument>> GetPrescriptionDocumentsAsync(
        string fileNum,
        int? sessionId,
        int? progressId,
        CancellationToken cancellationToken = default);
}
