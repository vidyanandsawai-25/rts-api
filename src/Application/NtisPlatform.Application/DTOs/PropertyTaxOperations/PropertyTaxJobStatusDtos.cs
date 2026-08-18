namespace NtisPlatform.Application.DTOs.PropertyTaxOperations;

/// <summary>Runtime status of a job (used by the background-task bar / polling).</summary>
public class JobStatusDto
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Processed { get; set; }
    public int Success { get; set; }
    public int Failed { get; set; }
    public int Pending { get; set; }
    public int Percentage { get; set; }
}

/// <summary>One per-property execution row of a job.</summary>
public class JobPropertyResultDto
{
    public string Zone { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string WardNo { get; set; } = string.Empty;
    public string UPICID { get; set; } = string.Empty;
    public string PropertyNo { get; set; } = string.Empty;
    public string PartitionNo { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string TaxHead { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
