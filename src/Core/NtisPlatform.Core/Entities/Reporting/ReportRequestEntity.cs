using NtisPlatform.Core.Enums;

namespace NtisPlatform.Core.Entities.Reporting;

/// <summary>
/// A queued async report generation job. Lives in the separate report queue database.
/// Written by ntis-platform (on submit) and the ntis-report worker (claim/status).
///
/// Deliberately does NOT inherit BaseEntity: it is an operational queue row in a different
/// database, not a CRUD master on the BaseCommonCrudService pipeline. It carries its own
/// audit/lifecycle columns. Pure POCO — all schema mapping lives in ReportingDbContext.
/// </summary>
public class ReportRequestEntity
{
    /// <summary>Primary key. A GUID to avoid enumeration/IDOR on the public status/download routes.</summary>
    public Guid ReportRequestId { get; set; }

    /// <summary>ReportDefinition.ReportCode this request is for.</summary>
    public string ReportCode { get; set; } = string.Empty;

    /// <summary>Caller-supplied report parameters, serialized as JSON.</summary>
    public string? ParametersJson { get; set; }

    public ReportRequestStatus Status { get; set; } = ReportRequestStatus.Pending;

    /// <summary>User who submitted the request (from JWT claims, never the body).</summary>
    public int RequestedByUserId { get; set; }

    public int? OrganizationId { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime? StartedDate { get; set; }
    public DateTime? CompletedDate { get; set; }

    public string? ErrorMessage { get; set; }

    /// <summary>DocumentGuid of the stored PDF once Completed (resolved via IDocumentService).</summary>
    public Guid? OutputDocumentGuid { get; set; }

    /// <summary>
    /// Base URL of the ntis-platform instance that submitted this request. The shared worker uses
    /// this to call back the correct tenant's API (authenticate, data pull, upload, notify).
    /// Stamped at submit from Reporting:PlatformBaseUrl in the platform's appsettings.
    /// </summary>
    public string PlatformBaseUrl { get; set; } = string.Empty;

    // ── Short-lived token: the credential the worker presents to obtain an LLT ──
    public string? ShortLivedToken { get; set; }
    public DateTime? SltExpiresAt { get; set; }
    public bool SltConsumed { get; set; }

    // ── Diagnostics ──
    // How many times this row entered Processing. Informational only — Hangfire owns queueing,
    // leasing, and retries; this is incremented by the worker for operator visibility.
    public int AttemptCount { get; set; }

    /// <summary>Optimistic-concurrency token guarding the atomic SltConsumed transition.</summary>
    public byte[]? RowVersion { get; set; }
}
