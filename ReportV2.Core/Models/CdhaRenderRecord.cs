namespace ReportV2.Core.Models;

public sealed record CdhaRenderRecord
{
    public string FileNum { get; init; } = string.Empty;
    public int? SessionId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public DateTime? Dob { get; init; }
    public string Gender { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string Conclusion { get; init; } = string.Empty;
    public string Doctor { get; init; } = string.Empty;
    public string RequestedDoctor { get; init; } = string.Empty;
    public DateTime? NgayKham { get; init; }
    public string FileName { get; init; } = string.Empty;
    public int? RequestId { get; init; }
    public int? ImagingResultId { get; init; }
    public string SampleNumber { get; init; } = string.Empty;
    public byte[]? ResultData { get; init; }
    public byte[]? ConclusionData { get; init; }
    public byte[]? SuggestionData { get; init; }
    public string TemplateFile { get; init; } = string.Empty;
    public int PathologyType { get; init; }
}
