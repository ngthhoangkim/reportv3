namespace ReportV2.Core.Models;

public sealed record PrescriptionProgress
{
    public int ProgressId { get; init; }
    public int? SessionId { get; init; }
    public int? SubSessionId { get; init; }
    public int? PatientId { get; init; }
    public int? DoctorId { get; init; }
    public string DoctorName { get; init; } = string.Empty;
    public string MainDisease { get; init; } = string.Empty;
    public DateTime? VisitDate { get; init; }
    public string FileNum { get; init; } = string.Empty;
}

public sealed record PrescriptionMedication
{
    public int Index { get; init; }
    public string ItemName { get; init; } = string.Empty;
    public string Property { get; init; } = string.Empty;
    public string Note { get; init; } = string.Empty;
    public string Quantity { get; init; } = string.Empty;
    public string Unit { get; init; } = string.Empty;
    public string Dose { get; init; } = string.Empty;
    public string DoseUnit { get; init; } = string.Empty;
    public string Frequency { get; init; } = string.Empty;
    public string Instructions { get; init; } = string.Empty;
}

public sealed record PrescriptionDocument
{
    public PrescriptionProgress Progress { get; init; } = new();
    public string PatientName { get; init; } = string.Empty;
    public DateTime? Dob { get; init; }
    public string Sex { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string DoctorQualification { get; init; } = string.Empty;
    public string Clinical { get; init; } = string.Empty;
    public string Advice { get; init; } = string.Empty;
    public string Pulse { get; init; } = string.Empty;
    public string Temperature { get; init; } = string.Empty;
    public string BloodPressure { get; init; } = string.Empty;
    public string RespiratoryRate { get; init; } = string.Empty;
    public IReadOnlyList<PrescriptionMedication> Medications { get; init; } = Array.Empty<PrescriptionMedication>();
}
