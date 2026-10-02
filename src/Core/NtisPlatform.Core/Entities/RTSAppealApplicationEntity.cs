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
