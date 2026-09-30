using Dapper;
using ReportV2.Application.Abstractions;
using ReportV2.Core.Models;
using ReportV2.Infrastructure.Sql;

namespace ReportV2.Infrastructure.Reports;

public sealed class PrescriptionRepository : IPrescriptionRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public PrescriptionRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<PrescriptionDocument>> GetPrescriptionDocumentsAsync(
        string fileNum,
        int? sessionId,
        int? progressId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateConnection();
        var progresses = (await connection.QueryAsync<PrescriptionProgress>(
            """
            SELECT
              cp.Id AS ProgressId,
              s.PatientID,
              cp.SubSessionId,
              cp.DoctorId,
              cp.DoctorName,
              cp.PathologyResult AS MainDisease,
              COALESCE(cp.FinishDate, cp.VisitDate) AS VisitDate,
              s.Id AS SessionId,
              CONVERT(VARCHAR(50), p.FileNum) AS FileNum
            FROM dbo.CN_Progress cp WITH (NOLOCK)
            INNER JOIN dbo.CR_SubSession ss WITH (NOLOCK) ON ss.Id = cp.SubSessionId
            INNER JOIN dbo.CR_Session s WITH (NOLOCK) ON s.Id = ss.SessionId
            INNER JOIN dbo.CR_Patient p WITH (NOLOCK) ON p.ContactId = s.PatientID
            WHERE LTRIM(RTRIM(CONVERT(VARCHAR(50), p.FileNum))) = LTRIM(RTRIM(@FileNum))
              AND (@SessionId IS NULL OR s.Id = @SessionId)
              AND (@ProgressId IS NULL OR cp.Id = @ProgressId)
              AND EXISTS (
                SELECT 1 FROM dbo.CN_Prescription pr WITH (NOLOCK)
                WHERE pr.DeletedDate IS NULL AND pr.ProgressID = cp.Id
              )
            ORDER BY cp.Id ASC
            """,
            new { FileNum = fileNum, SessionId = sessionId, ProgressId = progressId })).ToArray();

        var result = new List<PrescriptionDocument>();
        foreach (var progress in progresses)
        {
            var person = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT TOP 1 * FROM dbo.PersonView WITH (NOLOCK) WHERE ContactId = @PatientId",
                new { progress.PatientId });
            var qualification = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT TOP 1 Qualification FROM dbo.ViewStaff WITH (NOLOCK) WHERE ContactId = @DoctorId",
                new { progress.DoctorId }) ?? "";
            var exam = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT TOP 1 * FROM dbo.CN_GeneralExam WITH (NOLOCK) WHERE SubSessionId = @SubSessionId ORDER BY ID DESC",
                new { progress.SubSessionId });
            var notes = (await connection.QueryAsync<dynamic>(
                """
                SELECT *
                FROM dbo.CN_Note WITH (NOLOCK)
                WHERE TypeCde IN (2, 8)
                  AND (ProgressId = @ProgressId OR SubSessionId = @SubSessionId)
                ORDER BY ID DESC
                """,
                new { progress.ProgressId, progress.SubSessionId })).ToArray();
            var medications = (await connection.QueryAsync<dynamic>(
                """
                SELECT
                  pr.RxId AS ID,
                  pr.ITEM,
                  pr.DOSE,
                  pr.UnitUsage,
                  pr.FREQUENCY,
                  pr.INSTRUCTIONS,
                  pr.QUANTITY,
                  pr.REASON,
                  pr.UNITNAME,
                  pr.Property,
                  rx.Property AS RxProperty
                FROM dbo.CN_Prescription pr WITH (NOLOCK)
                LEFT JOIN dbo.CN_RX rx WITH (NOLOCK) ON rx.ID = pr.RxId
                WHERE pr.DeletedDate IS NULL
                  AND pr.ProgressID = @ProgressId
                  AND (@SubSessionId IS NULL OR pr.SubSessionId = @SubSessionId)
                ORDER BY pr.CreatedDate ASC, pr.ID ASC
                """,
                new { progress.ProgressId, progress.SubSessionId }))
                .Select((row, index) =>
                {
                    string property = Text(row.Property);
                    if (string.IsNullOrWhiteSpace(property))
                    {
                        property = Text(row.RxProperty);
                    }

                    string doseUnit = Text(row.UnitUsage);
                    if (string.IsNullOrWhiteSpace(doseUnit))
                    {
                        doseUnit = Text(row.UNITNAME);
                    }

                    var frequency = Text(row.FREQUENCY);
                    return new PrescriptionMedication
                    {
                        Index = index + 1,
                        ItemName = Text(row.ITEM),
                        Property = property,
                        Note = Text(row.REASON),
                        Quantity = Text(row.QUANTITY),
                        Unit = Text(row.UNITNAME),
                        Dose = Text(row.DOSE),
                        DoseUnit = doseUnit,
                        Frequency = string.IsNullOrWhiteSpace(frequency) ? "" : $"{frequency} lần/ngày",
                        Instructions = Text(row.INSTRUCTIONS)
                    };
                })
                .ToArray();

            dynamic? clinical = notes.FirstOrDefault(row => Number(row.TypeCde) == 8);
            dynamic? advice = notes.FirstOrDefault(row => Number(row.TypeCde) == 2);
            result.Add(new PrescriptionDocument
            {
                Progress = progress,
                PatientName = Text(Dyn(person, "FullName")),
                Dob = Dyn(person, "Dob") as DateTime?,
                Sex = Text(Dyn(person, "Sex")),
                Address = AddressFrom(person),
                DoctorQualification = qualification,
                Clinical = Text(Dyn(clinical, "Note")),
                Advice = Text(Dyn(advice, "Note")),
                Pulse = Text(Dyn(exam, "Pulse")),
                Temperature = Text(Dyn(exam, "Temperature")),
                BloodPressure = Text(Dyn(exam, "BloodPressure")),
                RespiratoryRate = Text(Dyn(exam, "RespiratoryRate")),
                Medications = medications
            });
        }

        return result;
    }

    private static object? Dyn(dynamic? row, string name)
    {
        if (row is null) return null;
        var dict = (IDictionary<string, object?>)row;
        return dict.TryGetValue(name, out var value) ? value : null;
    }

    private static string Text(object? value) => value?.ToString()?.Trim() ?? "";

    private static int Number(object? value) => int.TryParse(value?.ToString(), out var n) ? n : 0;

    private static string AddressFrom(dynamic? row)
    {
        var address = Text(Dyn(row, "Address"));
        return string.IsNullOrWhiteSpace(address) ? Text(Dyn(row, "Street")) : address;
    }
}
