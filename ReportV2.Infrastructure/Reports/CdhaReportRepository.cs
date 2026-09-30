using Dapper;
using ReportV2.Application.Abstractions;
using ReportV2.Core.Models;
using ReportV2.Infrastructure.Sql;

namespace ReportV2.Infrastructure.Reports;

public sealed class CdhaReportRepository : ICdhaReportRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public CdhaReportRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<CdhaRenderRecord>> GetRenderRecordsAsync(
        string fileNum,
        int? sessionId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
              CONVERT(VARCHAR(50), v.FileNum) AS FileNum,
              v.SessionId,
              v.PatientName,
              v.ServiceName,
              v.Dob,
              v.Sex AS Gender,
              v.Street AS Address,
              r.Conclusion,
              v.Doctor,
              v.RequestedDoctor,
              r.CreatedDate AS NgayKham,
              r.FileName,
              r.RequestId,
              r.Id AS ImagingResultId,
              r.SampleNumber,
              d.ResultData,
              d.ConclusionData,
              d.SuggestionData,
              r.TemplateFile,
              r.PathologyType
            FROM dbo.CN_ImagingResult r WITH (NOLOCK)
            INNER JOIN dbo.CN_ImagingResultData d WITH (NOLOCK) ON r.Id = d.ImagingResultId
            INNER JOIN dbo.ViewImagingResult v WITH (NOLOCK) ON v.Id = r.Id
            WHERE r.DeletedDate IS NULL
              AND LTRIM(RTRIM(CONVERT(VARCHAR(50), v.FileNum))) = LTRIM(RTRIM(@FileNum))
              AND (@SessionId IS NULL OR v.SessionId = @SessionId)
            ORDER BY r.CreatedDate ASC, r.Id ASC
            """;

        await using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<CdhaRenderRecord>(sql, new { FileNum = fileNum, SessionId = sessionId });
        return rows.ToArray();
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<PathologyImageRecord>>> GetPathologyImagesAsync(
        IReadOnlyCollection<int> resultIds,
        bool printedOnly,
        CancellationToken cancellationToken = default)
    {
        if (resultIds.Count == 0)
        {
            return new Dictionary<int, IReadOnlyList<PathologyImageRecord>>();
        }

        const string sql = """
            SELECT
              ResultId,
              Filename,
              Printed,
              CreatedDate
            FROM dbo.CN_PathologyImage WITH (NOLOCK)
            WHERE ResultId IN @ResultIds
              AND DeletedDate IS NULL
              AND (@PrintedOnly = 0 OR Printed = 1)
            ORDER BY ResultId ASC, CreatedDate ASC, ID ASC
            """;

        await using var connection = _connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<PathologyImageRecord>(sql, new
        {
            ResultIds = resultIds.ToArray(),
            PrintedOnly = printedOnly ? 1 : 0
        });

        return rows
            .Where(row => row.ResultId.HasValue && !string.IsNullOrWhiteSpace(row.Filename))
            .GroupBy(row => row.ResultId!.Value)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<PathologyImageRecord>)group.ToArray());
    }
}
