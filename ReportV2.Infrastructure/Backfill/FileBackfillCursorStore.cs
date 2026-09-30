using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReportV2.Application.Abstractions;
using ReportV2.Application.Options;
using ReportV2.Core.Models;

namespace ReportV2.Infrastructure.Backfill;

public sealed class FileBackfillCursorStore : IBackfillCursorStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly ReportV2Options _options;
    private readonly ILogger<FileBackfillCursorStore> _logger;

    public FileBackfillCursorStore(
        IOptions<ReportV2Options> options,
        ILogger<FileBackfillCursorStore> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<BackfillCursor?> LoadAsync(int year, CancellationToken cancellationToken = default)
    {
        var path = CursorPath(year);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<BackfillCursor>(stream, JsonOptions, cancellationToken);
    }

    public async Task SaveAsync(BackfillCursor cursor, CancellationToken cancellationToken = default)
    {
        var path = CursorPath(cursor.Year);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var tmpPath = $"{path}.tmp";
        await using (var stream = File.Create(tmpPath))
        {
            await JsonSerializer.SerializeAsync(stream, cursor, JsonOptions, cancellationToken);
        }

        File.Move(tmpPath, path, overwrite: true);
        _logger.LogDebug("Backfill cursor saved. Path={Path}", path);
    }

    private string CursorPath(int year)
    {
        return Path.Combine(_options.StatePath, $"backfill-{year}.json");
    }
}
