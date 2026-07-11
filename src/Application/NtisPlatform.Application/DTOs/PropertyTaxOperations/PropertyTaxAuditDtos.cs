using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.DTOs.PropertyTaxOperations;

/// <summary>Job-level audit row (the Audit &amp; Monitor master table).</summary>
public class JobAuditDto
{
    public int Id { get; set; }
    public string JobId { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string DoneBy { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public DateTime? CompleteTime { get; set; }
    public string Duration { get; set; } = string.Empty;
    public string Records { get; set; } = string.Empty; // e.g. "8358 / 8574"
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

/// <summary>Full audit detail for a single job.</summary>
public class JobAuditDetailDto
{
    public string JobId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string FinanceYear { get; set; } = string.Empty;
    public string StartedBy { get; set; } = string.Empty;
    public string? UserRole { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? CompleteTime { get; set; }
    public string Duration { get; set; } = string.Empty;

    public ProcessingSummaryDto Summary { get; set; } = new();
    public List<SkippedReasonDto> SkippedReasons { get; set; } = new();
    public PagedResult<JobPropertyResultDto> Properties { get; set; } = new();
}

public class ProcessingSummaryDto
{
    public int TotalSelected { get; set; }
    public int SuccessfullyAdded { get; set; }
    public int SkippedRecords { get; set; }
    public int Failed { get; set; }
}

public class SkippedReasonDto
{
    /// <summary>Localization key, e.g. "Skip_AlreadyProcessed".</summary>
    public string Reason { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>Query parameters for the audit list (or detail when JobCode is supplied).</summary>
public class OperationAuditQueryParameters : BaseQueryParameters
{
    public string? JobCode { get; set; }
    public string? Operation { get; set; }
    public int? FinanceYearId { get; set; }
    public string? Status { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? CompleteTime { get; set; }
    public DateTime? CreatedDate { get; set; }
}

/// <summary>Query parameters for filtering properties inside a job.</summary>
public class JobPropertiesQueryParameters : BaseQueryParameters
{
}
