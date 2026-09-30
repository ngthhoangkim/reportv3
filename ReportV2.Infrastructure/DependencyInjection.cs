using Microsoft.Extensions.DependencyInjection;
using ReportV2.Application.Abstractions;
using ReportV2.Infrastructure.Backfill;
using ReportV2.Infrastructure.Files;
using ReportV2.Infrastructure.Reports;
using ReportV2.Infrastructure.Sql;
using ReportV2.Infrastructure.Sync;
using ReportV2.Infrastructure.Upload;

namespace ReportV2.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddReportV2Infrastructure(this IServiceCollection services)
    {
        services.AddSingleton<SqlConnectionFactory>();
        services.AddSingleton<IBackfillCursorStore, FileBackfillCursorStore>();
        services.AddSingleton<ISyncCursorStore, FileSyncCursorStore>();
        services.AddScoped<IBackfillCandidateSource, HisBackfillCandidateSource>();
        services.AddScoped<ICdhaReportRepository, CdhaReportRepository>();
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
        services.AddScoped<IFileResolver, FileShareResolver>();
        services.AddSingleton<TempCleanupService>();
        services.AddHttpClient<IUploadClient, UploadClient>();

        return services;
    }
}
