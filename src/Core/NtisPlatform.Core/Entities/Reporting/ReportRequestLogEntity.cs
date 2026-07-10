namespace NtisPlatform.Core.Entities.Reporting;

/// <summary>
/// Audit trail of report-request status transitions. Lives in the report queue database.
/// One row per transition (e.g. Pending → Processing, Processing → Completed/Failed/Retrying).
/// Pure POCO — schema mapping lives in ReportingDbContext.
/// </summary>
public class ReportRequestLogEntity
{
    public long Id { get; set; }

    public Guid ReportRequestId { get; set; }

    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }

    public string? Message { get; set; }
    public string? WorkerId { get; set; }

    public DateTime CreatedDate { get; set; }
}
