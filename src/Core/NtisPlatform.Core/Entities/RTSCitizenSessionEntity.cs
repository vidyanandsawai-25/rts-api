namespace NtisPlatform.Core.Entities;

public class RTSCitizenSessionEntity : BaseEntity
{
    public string SessionId { get; set; } = string.Empty;
    public string? CitizenName { get; set; }
    public string? MobileNo { get; set; }

    /// <summary>
    /// Unique Property Identification Code. Renamed from UPIC to Upic (camelCase convention).
    /// </summary>
    public string? Upic { get; set; }

    public string? PropertyNo { get; set; }
    public int? OwnerId { get; set; }
    public DateTime LoginTime { get; set; }
    public DateTime? LastActivityTime { get; set; }
    public DateTime? LogoutTime { get; set; }

    // Note: BaseEntity audit columns (CreatedBy, CreatedDate, UpdatedBy, UpdatedDate)
    // are ignored in DbContext for this table. LoginTime = session creation time,
    // LastActivityTime = last update time. These are configured via Ignore() in DbContext.
}
