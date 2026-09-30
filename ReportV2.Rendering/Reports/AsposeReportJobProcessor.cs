using Aspose.Words;
using Aspose.Words.Replacing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReportV2.Application.Abstractions;
using ReportV2.Application.Options;
using ReportV2.Core.Models;
using ReportV2.Rendering.Text;

namespace ReportV2.Rendering.Reports;

public sealed class AsposeReportJobProcessor : IBackfillJobProcessor
{
    private readonly ICdhaReportRepository _cdhaRepository;
    private readonly IPrescriptionRepository _prescriptionRepository;
    private readonly IFileResolver _fileResolver;
    private readonly IUploadClient _uploadClient;
    private readonly ReportV2Options _reportOptions;
    private readonly UploadOptions _uploadOptions;
    private readonly ILogger<AsposeReportJobProcessor> _logger;

    public AsposeReportJobProcessor(
        ICdhaReportRepository cdhaRepository,
        IPrescriptionRepository prescriptionRepository,
        IFileResolver fileResolver,
        IUploadClient uploadClient,
        IOptions<ReportV2Options> reportOptions,
        IOptions<UploadOptions> uploadOptions,
        ILogger<AsposeReportJobProcessor> logger)
    {
        _cdhaRepository = cdhaRepository;
        _prescriptionRepository = prescriptionRepository;
        _fileResolver = fileResolver;
        _uploadClient = uploadClient;
        _reportOptions = reportOptions.Value;
        _uploadOptions = uploadOptions.Value;
        _logger = logger;
    }

    public async Task ProcessAsync(BackfillCandidate candidate, bool dryRun, CancellationToken cancellationToken = default)
    {
        if (dryRun)
        {
            _logger.LogInformation(
                "Dry-run candidate. Source={Source} FileNum={FileNum} SessionId={SessionId} ProgressId={ProgressId}",
                candidate.Source,
                candidate.FileNum,
                candidate.SessionId,
                candidate.ProgressId);
            return;
        }

        if (string.Equals(candidate.Source, "prescription", StringComparison.OrdinalIgnoreCase))
        {
            await ProcessPrescriptionAsync(candidate, dryRun, cancellationToken);
            return;
        }

        await ProcessCdhaAsync(candidate, dryRun, cancellationToken);
    }

    private async Task ProcessCdhaAsync(BackfillCandidate candidate, bool dryRun, CancellationToken cancellationToken)
    {
        var records = await _cdhaRepository.GetRenderRecordsAsync(candidate.FileNum, candidate.SessionId, cancellationToken);
        var imagesByResult = await _cdhaRepository.GetPathologyImagesAsync(
            records.Select(record => record.ImagingResultId).Where(id => id.HasValue).Select(id => id!.Value).ToArray(),
            printedOnly: true,
            cancellationToken);

        foreach (var record in records)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var outputName = OutputName(record);
            var outputPath = Path.Combine(_reportOptions.OutputPath, outputName);
            if (dryRun)
            {
                _logger.LogInformation("Dry-run CDHA render. File={File}", outputPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            var doc = LoadTemplate(record);
            FillCdhaTemplate(doc, record);

            var workDir = Path.Combine(_reportOptions.TempPath, "cdha", $"{record.ImagingResultId}_{Guid.NewGuid():N}");
            try
            {
                if (record.ImagingResultId.HasValue &&
                    imagesByResult.TryGetValue(record.ImagingResultId.Value, out var imageRows))
                {
                    await AppendImagesAsync(doc, imageRows, workDir, cancellationToken);
                }

                doc.Save(outputPath, SaveFormat.Pdf);
                await _uploadClient.UploadPdfAsync(outputPath, _uploadOptions.Prefix, cancellationToken);
                _logger.LogInformation("CDHA PDF generated. File={File}", outputPath);
            }
            finally
            {
                TryDeleteDirectory(workDir);
            }
        }
    }

    private async Task ProcessPrescriptionAsync(BackfillCandidate candidate, bool dryRun, CancellationToken cancellationToken)
    {
        var documents = await _prescriptionRepository.GetPrescriptionDocumentsAsync(
            candidate.FileNum,
            candidate.SessionId,
            candidate.ProgressId,
            cancellationToken);

        foreach (var prescription in documents)
        {
            var outputName = $"{prescription.Progress.ProgressId}.pdf";
            var outputPath = Path.Combine(_reportOptions.PrescriptionOutputPath, outputName);
            if (dryRun)
            {
                _logger.LogInformation("Dry-run prescription render. File={File}", outputPath);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            var front = LoadDocument(_reportOptions.PrescriptionTemplateFront);
            var back = LoadDocument(_reportOptions.PrescriptionTemplateBack);
            FillPrescriptionTemplate(front, prescription, frontSide: true);
            FillPrescriptionTemplate(back, prescription, frontSide: false);
            front.AppendDocument(back, ImportFormatMode.KeepSourceFormatting);
            front.Save(outputPath, SaveFormat.Pdf);
            await _uploadClient.UploadPdfAsync(outputPath, _uploadOptions.PrescriptionPrefix, cancellationToken);
            _logger.LogInformation("Prescription PDF generated. File={File}", outputPath);
        }
    }

    private Document LoadTemplate(CdhaRenderRecord record)
    {
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(record.TemplateFile))
        {
            candidates.Add(Path.Combine(_reportOptions.TemplatesPath, record.TemplateFile));
            candidates.Add(Path.Combine(_reportOptions.TemplatesPath, Path.GetFileName(record.TemplateFile)));
        }

        candidates.Add(Path.Combine(_reportOptions.TemplatesPath, "full-report.docx"));
        candidates.Add(Path.Combine(_reportOptions.TemplatesPath, "full-report.doc"));

        var path = candidates.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException($"No CDHA template found for {record.TemplateFile}");
        return new Document(path);
    }

    private Document LoadDocument(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(path);
        }

        return new Document(path);
    }

    private static void FillCdhaTemplate(Document doc, CdhaRenderRecord record)
    {
        var age = record.Dob.HasValue
            ? Math.Max(0, DateTime.Today.Year - record.Dob.Value.Year).ToString()
            : "";
        var payload = new Dictionary<string, string?>
        {
            ["FileNm"] = record.FileNum,
            ["PatientName"] = record.PatientName,
            ["Age"] = age,
            ["Gender"] = record.Gender,
            ["Diagnosis"] = record.Conclusion,
            ["ReferDoctor"] = record.RequestedDoctor,
            ["Doctor"] = record.Doctor,
            ["ItemNum"] = string.IsNullOrWhiteSpace(record.SampleNumber) ? record.FileNum : record.SampleNumber,
            ["SampleNumber"] = record.SampleNumber,
            ["Address"] = record.Address,
            ["DateRpt"] = record.NgayKham?.ToString("dd/MM/yyyy") ?? "",
            ["Result"] = RtfText.FromBytes(record.ResultData),
            ["Conclusion"] = RtfText.FromBytes(record.ConclusionData),
            ["Suggestion"] = RtfText.FromBytes(record.SuggestionData),
            ["SessionId"] = record.SessionId?.ToString(),
            ["FileNum"] = record.FileNum
        };
        ReplaceTokens(doc, payload);
    }

    private static void FillPrescriptionTemplate(Document doc, PrescriptionDocument prescription, bool frontSide)
    {
        var visit = prescription.Progress.VisitDate ?? DateTime.Today;
        var medicationBlock = string.Join("\v\v", prescription.Medications.Select(item =>
        {
            var amount = string.Join(" ", new[] { item.Quantity, item.Unit }.Where(s => !string.IsNullOrWhiteSpace(s)));
            var dose = string.Join(" ", new[] { item.Dose, item.DoseUnit }.Where(s => !string.IsNullOrWhiteSpace(s)));
            return string.Join("\v", new[]
            {
                $"{item.Index}/ {amount}".Trim(),
                item.ItemName,
                item.Note,
                string.Join(", ", new[] { item.Frequency, string.IsNullOrWhiteSpace(dose) ? "" : $"mỗi lần {dose}" }.Where(s => !string.IsNullOrWhiteSpace(s))),
                item.Instructions
            }.Where(s => !string.IsNullOrWhiteSpace(s)));
        }));

        var diagnosis = prescription.Progress.MainDisease;
        var backConclusion = string.Join("\v", new[]
        {
            "Tiền căn:",
            "",
            "Lâm sàng:",
            prescription.Clinical,
            "CLS bất thường:",
            "Chẩn đoán:",
            diagnosis,
            "Hướng điều trị và các chế độ tiếp theo:",
            prescription.Advice
        });

        var payload = new Dictionary<string, string?>
        {
            ["So"] = prescription.Progress.ProgressId.ToString(),
            ["SO"] = prescription.Progress.ProgressId.ToString(),
            ["MaPhieu"] = prescription.Progress.ProgressId.ToString(),
            ["Barcode"] = $"*{prescription.Progress.ProgressId}*",
            ["MaBN"] = prescription.Progress.FileNum,
            ["PatientID"] = frontSide ? prescription.PatientName : $"*{prescription.Progress.FileNum}*",
            ["PatientName"] = prescription.PatientName,
            ["Address"] = prescription.Address,
            ["Conclusion"] = frontSide ? diagnosis : backConclusion,
            ["ChanDoan"] = diagnosis,
            ["LamSang"] = prescription.Clinical,
            ["LoiDan"] = prescription.Advice,
            ["Advice"] = prescription.Advice,
            ["HR"] = prescription.Pulse,
            ["Temp"] = prescription.Temperature,
            ["BP"] = prescription.BloodPressure,
            ["RR"] = prescription.RespiratoryRate,
            ["G"] = prescription.Sex,
            ["Dtb"] = prescription.Dob?.Year.ToString(),
            ["Date"] = visit.Day.ToString("00"),
            ["Month"] = visit.Month.ToString("00"),
            ["Mont"] = visit.Month.ToString("00"),
            ["Year"] = visit.Year.ToString(),
            ["Doctor"] = string.Join(" ", new[] { prescription.DoctorQualification, prescription.Progress.DoctorName }.Where(s => !string.IsNullOrWhiteSpace(s))),
            ["#"] = medicationBlock,
            ["MedicationBlock"] = medicationBlock
        };
        ReplaceAngleTokens(doc, payload);
        ReplaceTokens(doc, payload);
    }

    private async Task AppendImagesAsync(
        Document doc,
        IReadOnlyList<PathologyImageRecord> imageRows,
        string workDir,
        CancellationToken cancellationToken)
    {
        var builder = new DocumentBuilder(doc);
        builder.MoveToDocumentEnd();

        foreach (var row in imageRows)
        {
            var paths = await _fileResolver.ResolveImagesAsync(row.Filename, workDir, cancellationToken);
            foreach (var path in paths)
            {
                builder.InsertBreak(BreakType.PageBreak);
                var shape = builder.InsertImage(path);
                var page = builder.PageSetup;
                var maxWidth = page.PageWidth - page.LeftMargin - page.RightMargin;
                var maxHeight = page.PageHeight - page.TopMargin - page.BottomMargin;
                var scale = Math.Min(maxWidth / shape.Width, maxHeight / shape.Height);
                if (scale < 1)
                {
                    shape.Width *= scale;
                    shape.Height *= scale;
                }
            }
        }
    }

    private static void ReplaceTokens(Document doc, IReadOnlyDictionary<string, string?> payload)
    {
        var options = new FindReplaceOptions { MatchCase = false, FindWholeWordsOnly = false };
        foreach (var item in payload)
        {
            var value = (item.Value ?? "").Replace("\r\n", "\v").Replace("\n", "\v");
            doc.Range.Replace($"<<{item.Key}>>", value, options);
            doc.Range.Replace($"{{{item.Key}}}", value, options);
        }
    }

    private static void ReplaceAngleTokens(Document doc, IReadOnlyDictionary<string, string?> payload)
    {
        var options = new FindReplaceOptions { MatchCase = false, FindWholeWordsOnly = false };
        foreach (var item in payload)
        {
            var value = (item.Value ?? "").Replace("\r\n", "\v").Replace("\n", "\v");
            doc.Range.Replace($"<{item.Key}>", value, options);
            doc.Range.Replace($"< {item.Key}>", value, options);
            doc.Range.Replace($"<{item.Key} >", value, options);
            doc.Range.Replace($"< {item.Key} >", value, options);
        }
    }

    private static string OutputName(CdhaRenderRecord record)
    {
        var stem = Path.GetFileNameWithoutExtension(record.FileName);
        if (string.IsNullOrWhiteSpace(stem))
        {
            stem = record.ImagingResultId?.ToString() ?? $"{record.FileNum}-{record.SessionId}";
        }

        return $"{stem}.pdf";
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Best-effort temp cleanup.
        }
    }
}
