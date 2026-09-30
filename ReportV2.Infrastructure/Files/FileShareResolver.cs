using Microsoft.Extensions.Options;
using ReportV2.Application.Abstractions;
using ReportV2.Application.Options;
using System.IO.Compression;

namespace ReportV2.Infrastructure.Files;

public sealed class FileShareResolver : IFileResolver
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff"
    };

    private readonly ReportV2Options _options;

    public FileShareResolver(IOptions<ReportV2Options> options)
    {
        _options = options.Value;
    }

    public async Task<IReadOnlyList<string>> ResolveImagesAsync(
        string fileName,
        string workDir,
        CancellationToken cancellationToken = default)
    {
        var resolved = await ResolveFileAsync(fileName, cancellationToken);
        if (resolved is null)
        {
            return Array.Empty<string>();
        }

        var ext = Path.GetExtension(resolved);
        if (ImageExtensions.Contains(ext))
        {
            return new[] { resolved };
        }

        if (!string.Equals(ext, ".zip", StringComparison.OrdinalIgnoreCase))
        {
            return Array.Empty<string>();
        }

        Directory.CreateDirectory(workDir);
        var extractDir = Path.Combine(workDir, Path.GetFileNameWithoutExtension(resolved));
        Directory.CreateDirectory(extractDir);

        using var archive = ZipFile.OpenRead(resolved);
        var output = new List<string>();
        foreach (var entry in archive.Entries.Where(entry => !string.IsNullOrWhiteSpace(entry.Name)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entryExt = Path.GetExtension(entry.Name);
            if (!ImageExtensions.Contains(entryExt))
            {
                continue;
            }

            var outPath = Path.Combine(extractDir, entry.Name);
            entry.ExtractToFile(outPath, overwrite: true);
            output.Add(outPath);
        }

        return output;
    }

    public Task<string?> ResolveFileAsync(string fileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Task.FromResult<string?>(null);
        }

        var candidates = CandidatePaths(fileName.Trim());
        return Task.FromResult(candidates.FirstOrDefault(File.Exists));
    }

    private IEnumerable<string> CandidatePaths(string fileName)
    {
        if (Path.IsPathRooted(fileName))
        {
            yield return fileName;
        }

        foreach (var root in new[] { _options.SourceImageDir, _options.FallbackImageDir, _options.LocalImageDir })
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            yield return Path.Combine(root, fileName);
            yield return Path.Combine(root, Path.GetFileName(fileName));
        }
    }
}
