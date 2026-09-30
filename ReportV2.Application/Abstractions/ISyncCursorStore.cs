using ReportV2.Core.Models;

namespace ReportV2.Application.Abstractions;

public interface ISyncCursorStore
{
    Task<SyncCursor?> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(SyncCursor cursor, CancellationToken cancellationToken = default);
}
