namespace NtisPlatform.Application.Options;

/// <summary>
/// Configuration for the async reporting pipeline (queue + worker token handshake + pagination).
/// Bound from the "Reporting" section of configuration.
/// </summary>
public sealed class ReportingOptions
{
    public const string Section = "Reporting";

    /// <summary>
    /// The tenant this platform instance belongs to. Each tenant runs its own single-tenant deployment,
    /// so this is a per-instance constant stamped onto every submitted ReportRequest.OrganizationId.
    /// The matching single-tenant worker then claims only rows carrying this value.
     /// If left as 0, OrganizationId is not stamped and the tenant-guard check is effectively disabled.
    /// </summary>
    public int OrganizationId { get; set; }

    /// <summary>
    /// Lifetime of the SLT JWT created at submit time (minutes). Must cover the maximum possible
    /// queue wait + retry cycle: 3 retries × ~40 min each = ~120 min minimum. Set higher if jobs
    /// regularly queue longer than that.
    /// </summary>
    public int SltMinutes { get; set; } = 120;

    /// <summary>Lifetime of the long-lived worker token (LLT) issued on authenticate. Minutes.</summary>
    public int LltMinutes { get; set; } = 45;

    /// <summary>Default page size for the worker data-pull endpoint.</summary>
    public int DefaultPageSize { get; set; } = 5000;

    /// <summary>
    /// The externally-reachable base URL of THIS platform instance (e.g. "https://tenant1.ntis.example.com/").
    /// The shared ntis-report worker calls this URL to authenticate, pull data, upload PDFs, and
    /// push status notifications. Because every tenant runs its own platform deployment, this value
    /// differs per instance and is stamped onto dbo.ReportRequest at submit time so the worker
    /// knows which tenant's API to call back. Must not be empty — submit will fail if unset.
    /// </summary>
    public string PlatformBaseUrl { get; set; } = string.Empty;
}
