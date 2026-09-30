using ReportV2.Core.Models;

namespace ReportV2.Application.Abstractions;

public interface IBackfillCursorStore
{
    Task<BackfillCursor?> LoadAsync(int year, CancellationToken cancellationToken = default);

    Task SaveAsync(BackfillCursor cursor, CancellationToken cancellationToken = default);
}
