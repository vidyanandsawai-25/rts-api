namespace NtisPlatform.Application.DTOs.PropertyTaxOperations;

/// <summary>Payload for the Property Tax Operations screen's initial load.</summary>
public class OperationsInitDto
{
    public List<FinanceYearOptionDto> FinanceYears { get; set; } = new();
    public OperationPermissionsDto Permissions { get; set; } = new();
    public OperationsSummaryDto Summary { get; set; } = new();
}

public class FinanceYearOptionDto
{
    /// <summary>Stored value, e.g. "2025" (the April-start year).</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Display label, e.g. "2025-26".</summary>
    public string Label { get; set; } = string.Empty;
}

public class OperationPermissionsDto
{
    public bool AddTax { get; set; }
}

public class OperationsSummaryDto
{
    public int TotalProperties { get; set; }
    public int EligibleRecords { get; set; }
    public int SkippedRecords { get; set; }
    public int RunningJobs { get; set; }
}
