namespace ReportV2.Core.Models;

public sealed record SyncCursor
{
    public DateTimeOffset LastSuccessfulScanTo { get; init; }
    public long Processed { get; init; }
    public long Failed { get; init; }
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
