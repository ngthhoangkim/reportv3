using Microsoft.Extensions.Options;
using ReportV2.Application.Abstractions;
using ReportV2.Application.Options;

namespace ReportV2.Infrastructure.Upload;

public sealed class UploadClient : IUploadClient
{
    private readonly HttpClient _httpClient;
    private readonly UploadOptions _options;

    public UploadClient(HttpClient httpClient, IOptions<UploadOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task UploadPdfAsync(
        string pdfPath,
        string prefix,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            return;
        }

        var endpoint = $"{_options.BaseUrl.TrimEnd('/')}/api/v1/s3/upload-multiple";
        await using var stream = File.OpenRead(pdfPath);
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(prefix), "prefix");
        form.Add(new StreamContent(stream), "files", Path.GetFileName(pdfPath));

        using var response = await _httpClient.PostAsync(endpoint, form, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Upload failed {(int)response.StatusCode}: {body[..Math.Min(body.Length, 300)]}");
        }

        if (_options.CleanupAfterUpload)
        {
            File.Delete(pdfPath);
        }
    }
}
