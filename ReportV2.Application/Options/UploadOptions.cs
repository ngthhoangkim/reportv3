namespace ReportV2.Application.Options;

public sealed class UploadOptions
{
    public const string SectionName = "Upload";

    public string BaseUrl { get; init; } = "";
    public string Prefix { get; init; } = "khambenh/";
    public string PrescriptionPrefix { get; init; } = "khambenh/toathuoc/";
    public bool CleanupAfterUpload { get; init; }
}
