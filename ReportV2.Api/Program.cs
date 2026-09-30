using ReportV2.Application.Options;
using ReportV2.Infrastructure;
using ReportV2.Rendering.Aspose;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<ReportV2Options>(
    builder.Configuration.GetSection(ReportV2Options.SectionName));
builder.Services.Configure<BackfillOptions>(
    builder.Configuration.GetSection(BackfillOptions.SectionName));
builder.Services.AddReportV2Infrastructure();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

AsposeLicenseInitializer.EnsureLoaded(
    app.Configuration["Aspose:LicensePath"]);

app.MapGet("/health", () => Results.Ok(new
{
    ok = true,
    service = "reportv2-dotnet",
    time = DateTimeOffset.UtcNow
}))
.WithName("Health")
.WithOpenApi();

app.Run();
