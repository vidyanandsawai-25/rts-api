using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.PropertyTaxOperations;

/// <summary>
/// Request to execute an operation. The server recomputes the eligible set and reads the actor
/// from the JWT — the client never supplies eligibleRecords / CreatedBy.
/// </summary>
public class ExecuteOperationRequestDto
{
    [Required(ErrorMessage = "FinanceYearId_Required")]
    public int FinanceYearId { get; set; }

    [Required(ErrorMessage = "Operation_Required")]
    public string Operation { get; set; } = string.Empty;

    [Required(ErrorMessage = "ScopeType_Required")]
    public string ScopeType { get; set; } = string.Empty;

    public OperationScopeDto Scope { get; set; } = new();
    public OperationOptionsDto Options { get; set; } = new();
}

public class OperationOptionsDto
{
    public bool PreviewBeforeExecute { get; set; }
    public bool IsScheduled { get; set; }
    public DateTime? ScheduledDateTime { get; set; }
}

public class ExecuteOperationResponseDto
{
    public string JobId { get; set; } = string.Empty; // JobCode, e.g. "JOB-ADD-2025-0001"
    public string Status { get; set; } = string.Empty;
    public JobSummaryDto Summary { get; set; } = new();
}

public class JobSummaryDto
{
    public int Total { get; set; }
    public int Processed { get; set; }
    public int Success { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
}
