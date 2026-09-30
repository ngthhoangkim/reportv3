namespace ReportV2.Core.Models;

public sealed record BackfillCandidate
{
    public string FileNum { get; init; } = string.Empty;
    public int? SessionId { get; init; }
    public int? ProgressId { get; init; }
    public string Source { get; init; } = string.Empty;
    public DateTimeOffset? LastChangedAt { get; init; }
}
