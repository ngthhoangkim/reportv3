namespace ReportV2.Core.Models;

public sealed record PathologyImageRecord
{
    public int? ResultId { get; init; }
    public string Filename { get; init; } = string.Empty;
    public bool Printed { get; init; }
    public DateTime? CreatedDate { get; init; }
}
