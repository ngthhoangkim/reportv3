using Microsoft.Extensions.DependencyInjection;
using ReportV2.Application.Abstractions;
using ReportV2.Rendering.Reports;

namespace ReportV2.Rendering;

public static class DependencyInjection
{
    public static IServiceCollection AddReportV2Rendering(this IServiceCollection services)
    {
        services.AddScoped<IBackfillJobProcessor, AsposeReportJobProcessor>();
        return services;
    }
}
