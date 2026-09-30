namespace ReportV2.Application.Options;

public sealed class BackfillOptions
{
    public const string SectionName = "Backfill";

    public int ChunkDays { get; init; } = 1;
    public int MaxCandidatesPerChunk { get; init; } = 50;
    public int DelayBetweenChunksMs { get; init; } = 2000;
    public string[] Sources { get; init; } = ["cdha"];
}
