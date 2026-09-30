namespace ReportV2.Application.Abstractions;

public interface IFileResolver
{
    Task<IReadOnlyList<string>> ResolveImagesAsync(
        string fileName,
        string workDir,
        CancellationToken cancellationToken = default);

    Task<string?> ResolveFileAsync(
        string fileName,
        CancellationToken cancellationToken = default);
}
