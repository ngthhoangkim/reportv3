using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReportV2.Application.Abstractions;
using ReportV2.Application.Options;
using ReportV2.Core.Models;

namespace ReportV2.Infrastructure.Sync;

public sealed class FileSyncCursorStore : ISyncCursorStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly ReportV2Options _options;
    private readonly ILogger<FileSyncCursorStore> _logger;

    public FileSyncCursorStore(
        IOptions<ReportV2Options> options,
        ILogger<FileSyncCursorStore> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SyncCursor?> LoadAsync(CancellationToken cancellationToken = default)
    {
        var path = CursorPath();
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<SyncCursor>(stream, JsonOptions, cancellationToken);
    }

    public async Task SaveAsync(SyncCursor cursor, CancellationToken cancellationToken = default)
    {
        var path = CursorPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var tmpPath = $"{path}.tmp";
        await using (var stream = File.Create(tmpPath))
        {
            await JsonSerializer.SerializeAsync(stream, cursor, JsonOptions, cancellationToken);
        }

        File.Move(tmpPath, path, overwrite: true);
        _logger.LogDebug("Sync cursor saved. Path={Path}", path);
    }

    private string CursorPath()
    {
        return Path.Combine(_options.StatePath, "sync-new.json");
    }
}
