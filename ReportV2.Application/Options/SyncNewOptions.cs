namespace ReportV2.Application.Options;

public sealed class SyncNewOptions
{
    public const string SectionName = "SyncNew";

    public int LookbackHours { get; init; } = 48;
    public int ChunkHours { get; init; } = 6;
    public int MaxCandidatesPerChunk { get; init; } = 200;
    public int DelayBetweenChunksMs { get; init; } = 1000;
    public int ScanToDelayMinutes { get; init; } = 0;
    public string[] Sources { get; init; } = ["cdha"];
}
