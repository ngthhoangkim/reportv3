namespace ReportV2.Application.Options;

public sealed class ReportV2Options
{
    public const string SectionName = "ReportV2";

    public string TemplatesPath { get; init; } = "Templates";
    public string OutputPath { get; init; } = "output";
    public string TempPath { get; init; } = "tmp";
    public string StatePath { get; init; } = "data/state";
    public int MaxHisConcurrency { get; init; } = 2;
    public int MaxRenderConcurrency { get; init; } = 4;
}
