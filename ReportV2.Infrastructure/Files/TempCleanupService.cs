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

    public Task CleanupStaleAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.TempPath) || !Directory.Exists(_options.TempPath))
        {
            return Task.CompletedTask;
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

        return Task.CompletedTask;
    }
}
