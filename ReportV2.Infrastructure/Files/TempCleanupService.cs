using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReportV2.Application.Options;

namespace ReportV2.Infrastructure.Files;

public sealed class TempCleanupService
{
    private readonly ReportV2Options _options;
    private readonly ILogger<TempCleanupService> _logger;

    public TempCleanupService(
        IOptions<ReportV2Options> options,
        ILogger<TempCleanupService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task CleanupStaleAsync(CancellationToken cancellationToken = default)
    {
        CleanupTemp(TimeSpan.FromHours(Math.Max(1, _options.TempRetentionHours)), cancellationToken);
        CleanupLogs(cancellationToken);
        CleanupState(cancellationToken);
        return Task.CompletedTask;
    }

    private void CleanupTemp(TimeSpan maxAge, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.TempPath) || !Directory.Exists(_options.TempPath))
        {
            return;
        }

        var cutoff = DateTimeOffset.Now.Subtract(maxAge);
        foreach (var dir in Directory.EnumerateDirectories(_options.TempPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var info = new DirectoryInfo(dir);
                if (info.LastWriteTime < cutoff)
                {
                    info.Delete(recursive: true);
                    _logger.LogInformation("Deleted stale temp directory. Path={Path}", dir);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete stale temp directory. Path={Path}", dir);
            }
        }
    }

    private void CleanupLogs(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.LogsPath) || !Directory.Exists(_options.LogsPath))
        {
            return;
        }

        var cutoff = DateTimeOffset.Now.AddDays(-Math.Max(1, _options.LogRetentionDays));
        foreach (var file in Directory.EnumerateFiles(_options.LogsPath, "*", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            TryDeleteOldFile(file, cutoff, "log");
        }

        var maxBytes = Math.Max(1, _options.MaxLogsMegabytes) * 1024L * 1024L;
        var files = Directory.EnumerateFiles(_options.LogsPath, "*", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .Where(file => file.Exists)
            .OrderBy(file => file.LastWriteTimeUtc)
            .ToArray();
        var total = files.Sum(file => file.Length);
        foreach (var file in files)
        {
            if (total <= maxBytes)
            {
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                total -= file.Length;
                file.Delete();
                _logger.LogInformation("Deleted log file to enforce size cap. Path={Path}", file.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete log file. Path={Path}", file.FullName);
            }
        }
    }

    private void CleanupState(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.StatePath) || !Directory.Exists(_options.StatePath))
        {
            return;
        }

        var cutoff = DateTimeOffset.Now.Subtract(TimeSpan.FromHours(Math.Max(1, _options.StateTempRetentionHours)));
        foreach (var file in Directory.EnumerateFiles(_options.StatePath, "*.tmp", SearchOption.TopDirectoryOnly)
                     .Concat(Directory.EnumerateFiles(_options.StatePath, "*.bak", SearchOption.TopDirectoryOnly)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            TryDeleteOldFile(file, cutoff, "state temp");
        }
    }

    private void TryDeleteOldFile(string path, DateTimeOffset cutoff, string kind)
    {
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.LastWriteTime >= cutoff)
            {
                return;
            }

            info.Delete();
            _logger.LogInformation("Deleted stale {Kind} file. Path={Path}", kind, path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete stale {Kind} file. Path={Path}", kind, path);
        }
    }
}
