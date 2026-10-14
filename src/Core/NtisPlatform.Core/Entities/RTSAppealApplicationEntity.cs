using System;
using System.Collections.Generic;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Domain entity representing a Right to Services (RTS) Appeal filed by a citizen
/// against an original RTS application under the Maharashtra Right to Public Services Act (MRTSA 2015).
/// </summary>
public class RTSAppealApplicationEntity : BaseEntity
{
    /// <summary>
    /// Foreign key referencing the original RTS application in RTSApplicationDetails.
    /// </summary>
    public int ApplicationId { get; set; }

    /// <summary>
    /// Unique statutory appeal tracking number (e.g., "RTS/2026/APP123-A1").
    /// </summary>
    public string AppealNo { get; set; } = string.Empty;

    /// <summary>
    /// Statutory appeal tier (1 = First Appeal, 2 = Second Appeal).
    /// </summary>
    public int AppealLevel { get; set; }

    /// <summary>
    /// Foreign key referencing the appeal grievance category in RTSAppealTypeMaster.
    /// </summary>
    public int AppealTypeId { get; set; }

    /// <summary>
    /// Citizen grievance grounds and delay condonation explanation.
    /// </summary>
    public string? ReasonForComplaint { get; set; }

    /// <summary>
    /// Citizen contact mobile number for SMS notifications and appeal tracking.
    /// </summary>
    public string? MobileNumber { get; set; }

    /// <summary>
    /// Citizen email address for statutory appeal notifications.
    /// </summary>
    public string? EmailAddress { get; set; }

    /// <summary>
    /// Current workflow lifecycle status of the appeal (e.g., "Pending", "Approved", "Rejected", "Returned").
    /// </summary>
    public string? AppealStatus { get; set; }

    /// <summary>
    /// Official decision remarks and directions issued by the Appellate Authority.
    /// </summary>
    public string? ActionRemarks { get; set; }

    /// <summary>
    /// User ID of the appellate officer who adjudicated the appeal.
    /// </summary>
    public int? ActionByUserId { get; set; }

    /// <summary>
    /// Timestamp when the appellate officer rendered their decision.
    /// </summary>
    public DateTime? ActionDate { get; set; }

    /// <summary>
    /// Date scheduled for statutory appeal hearing under MRTSA 2015.
    /// </summary>
    public DateTime? HearingDate { get; set; }

    /// <summary>
    /// Official hearing notice details and location instructions.
    /// </summary>
    public string? HearingNoticeRemarks { get; set; }

    /// <summary>
    /// Official order category: Allowed, Dismissed, or Remanded.
    /// </summary>
    public string? OrderType { get; set; }

    /// <summary>
    /// Statutory date by which the competent authority must provide the service to the citizen if appeal is allowed.
    /// </summary>
    public DateTime? ServiceDeliveryDueDate { get; set; }

    /// <summary>
    /// Statutory penalty amount imposed on the defaulting officer under MRTSA Section 9 (up to Rs. 5000).
    /// </summary>
    public decimal? PenaltyAmount { get; set; }

    /// <summary>
    /// Official justification for penalty or departmental inquiry recommendation under Section 10.
    /// </summary>
    public string? PenaltyRemarks { get; set; }

    /// <summary>
    /// File path or GUID of the signed appellate order document.
    /// </summary>
    public string? OrderDocumentPath { get; set; }

    /// <summary>
    /// Soft-delete flag.
    /// </summary>
    public bool MarkedForDeletion { get; set; } = false;

    /// <summary>
    /// Timestamp when this record was soft deleted.
    /// </summary>
    public DateTime? MarkedForDeletionDate { get; set; }

    // Navigation properties
    public virtual RTSApplicationDetailsEntity? Application { get; set; }
    public virtual RTSAppealTypeMasterEntity? AppealType { get; set; }
    public virtual ICollection<RTSTrackAppealHistoryEntity> TrackAppealHistories { get; set; } = new List<RTSTrackAppealHistoryEntity>();
}

/// <summary>
/// Backward-compatibility alias for previous casing.
/// </summary>
public class RtsAppealApplicationEntity : RTSAppealApplicationEntity
{
}
