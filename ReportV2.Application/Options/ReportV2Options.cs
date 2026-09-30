namespace ReportV2.Application.Options;

public sealed class ReportV2Options
{
    public const string SectionName = "ReportV2";

    public string TemplatesPath { get; init; } = "Templates";
    public string OutputPath { get; init; } = "output";
    public string TempPath { get; init; } = "tmp";
    public string StatePath { get; init; } = "data/state";
    public string LogsPath { get; init; } = "logs";
    public string SourceImageDir { get; init; } = "";
    public string FallbackImageDir { get; init; } = "";
    public string LocalImageDir { get; init; } = "";
    public string PrescriptionTemplateFront { get; init; } = "Templates/ToaThuocV2/TT_MAT_1.docx";
    public string PrescriptionTemplateBack { get; init; } = "Templates/ToaThuocV2/TT_MAT_2.docx";
    public string PrescriptionOutputPath { get; init; } = "output/prescriptions";
    public int TempRetentionHours { get; init; } = 24;
    public int LogRetentionDays { get; init; } = 30;
    public int MaxLogsMegabytes { get; init; } = 512;
    public int StateTempRetentionHours { get; init; } = 24;
    public int MaxHisConcurrency { get; init; } = 2;
    public int MaxRenderConcurrency { get; init; } = 4;
}
