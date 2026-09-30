namespace ReportV2.Application.Abstractions;

public interface IUploadClient
{
    Task UploadPdfAsync(
        string pdfPath,
        string prefix,
        CancellationToken cancellationToken = default);
}
