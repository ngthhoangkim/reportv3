namespace ReportV2.Core.Models;

public sealed record BackfillCursor
{
    public int Year { get; init; }
    public DateOnly CurrentDate { get; init; }
    public long Processed { get; init; }
    public long Failed { get; init; }
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
