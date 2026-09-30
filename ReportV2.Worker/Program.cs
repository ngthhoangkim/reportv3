using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReportV2.Application.Abstractions;
using ReportV2.Application.Options;
using ReportV2.Application.Services;
using ReportV2.Infrastructure;
using ReportV2.Rendering.Aspose;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<ReportV2Options>(
    builder.Configuration.GetSection(ReportV2Options.SectionName));
builder.Services.Configure<BackfillOptions>(
    builder.Configuration.GetSection(BackfillOptions.SectionName));
builder.Services.Configure<SyncNewOptions>(
    builder.Configuration.GetSection(SyncNewOptions.SectionName));
builder.Services.AddReportV2Infrastructure();
builder.Services.AddScoped<IBackfillJobProcessor, NoopBackfillJobProcessor>();
builder.Services.AddScoped<YearBackfillRunner>();
builder.Services.AddScoped<SyncNewRunner>();

using var host = builder.Build();

AsposeLicenseInitializer.EnsureLoaded(
    builder.Configuration["Aspose:LicensePath"]);

var command = args.FirstOrDefault();
if (command is null)
{
    Console.WriteLine("ReportV2.Worker is ready.");
    Console.WriteLine("Usage: dotnet run --project ReportV2.Worker -- backfill --year 2026 --dry-run --resume");
    Console.WriteLine("Usage: dotnet run --project ReportV2.Worker -- sync-new --dry-run --resume");
    return;
}

using var scope = host.Services.CreateScope();
var parsed = ParseArgs(args.Skip(1));
if (string.Equals(command, "backfill", StringComparison.OrdinalIgnoreCase))
{
    if (!parsed.TryGetValue("year", out var yearText) || !int.TryParse(yearText, out var year))
    {
        throw new ArgumentException("Missing or invalid --year.");
    }

    var runner = scope.ServiceProvider.GetRequiredService<YearBackfillRunner>();
    await runner.RunAsync(
        year,
        resume: parsed.ContainsKey("resume"),
        dryRun: IsDryRun(parsed),
        cancellationToken: CancellationToken.None);
    return;
}

if (string.Equals(command, "sync-new", StringComparison.OrdinalIgnoreCase))
{
    var runner = scope.ServiceProvider.GetRequiredService<SyncNewRunner>();
    await runner.RunAsync(
        resume: parsed.ContainsKey("resume"),
        dryRun: IsDryRun(parsed),
        cancellationToken: CancellationToken.None);
    return;
}

throw new ArgumentException($"Unknown command: {command}");

static bool IsDryRun(IReadOnlyDictionary<string, string?> parsed)
{
    return parsed.ContainsKey("dry-run") || parsed.ContainsKey("dryRun");
}

static Dictionary<string, string?> ParseArgs(IEnumerable<string> args)
{
    var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    string? currentKey = null;

    foreach (var arg in args)
    {
        if (arg.StartsWith("--", StringComparison.Ordinal))
        {
            currentKey = arg[2..];
            result[currentKey] = null;
            continue;
        }

        if (currentKey is not null)
        {
            result[currentKey] = arg;
            currentKey = null;
        }
    }

    return result;
}
