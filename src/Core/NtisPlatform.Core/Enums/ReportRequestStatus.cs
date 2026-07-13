namespace NtisPlatform.Core.Enums;

/// <summary>
/// Lifecycle status of an async report generation request.
/// Stored as a string in the report queue database.
/// </summary>
public enum ReportRequestStatus
{
    /// <summary>Request accepted and queued; not yet picked up by a worker.</summary>
    Pending = 0,

    /// <summary>Claimed by a worker and currently rendering.</summary>
    Processing = 1,

    /// <summary>PDF generated, stored, and a download reference is available.</summary>
    Completed = 2,

    /// <summary>Generation failed permanently; ErrorMessage holds the reason.</summary>
    Failed = 3,

    /// <summary>Cancelled by a user or the system.</summary>
    Cancelled = 4,

    /// <summary>
    /// Legacy transient-failure state from the pre-Hangfire retry mechanism. No longer written
    /// (Hangfire keeps a failing row in Processing between attempts); retained for backward
    /// compatibility with any historical rows.
    /// </summary>
    Retrying = 5,
}
