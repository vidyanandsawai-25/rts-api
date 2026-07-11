namespace NtisPlatform.Application.Enums;

/// <summary>The bulk tax operation a job performs. Only <see cref="AddTax"/> is functional today.</summary>
public enum JobOperation
{
    AddTax,
    QuarterlyAdd,
    RemoveTax,
    QuarterlyRemove
}

/// <summary>Lifecycle status of a <c>PropertyTaxJob</c>.</summary>
public enum JobStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Scheduled
}

/// <summary>Per-property execution status within a job.</summary>
public enum JobDetailStatus
{
    Pending,
    Processing,
    Added,
    Failed,
    Completed,
    Paused,
    Skipped
}

/// <summary>How the set of target properties is selected.</summary>
public enum JobScopeType
{   
    Zone,
    Ward,
    Building,
    Property,
    Range
}

/// <summary>Reason a candidate property was excluded from execution.</summary>
public enum SkipReason
{
    AlreadyProcessed,
    PropertyLocked,
    PendingVerification,
    InvalidScope,
    PermissionRestricted,
    ApprovalRequired
}
