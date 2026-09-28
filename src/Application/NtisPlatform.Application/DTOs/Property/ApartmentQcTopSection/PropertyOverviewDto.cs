namespace NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;

/// <summary>
/// Left/right overview strip shown above the property top-section panel.
/// </summary>
public sealed class PropertyOverviewDto
{
    public string Upic { get; init; } = string.Empty;

    /// <summary>Display identifier composed as WardNo-PropertyNo-PartitionNo.</summary>
    public string PropertyId { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    /// <summary>True when any active, non-deleted PropertyScreenLock row for this property has IsLocked = true.</summary>
    public bool IsLocked { get; init; }

    /// <summary>"Locked" (takes priority) / "Active" / "Not Active" — ready-to-display status badge text.</summary>
    public string PropertyStatus { get; init; } = string.Empty;

    public string? SecretaryName { get; init; }
    public string? SecretaryNameEnglish { get; init; }
    public string? SecretaryMobileNo { get; init; }
    public string? SecretaryEmailId { get; init; }

    public string? ManagerName { get; init; }
    public string? ManagerNameEnglish { get; init; }
    public string? ManagerMobileNo { get; init; }
    public string? ManagerEmailId { get; init; }

    /// <summary>PropertyMast.OwnerName.</summary>
    public string? PropertyHolder { get; init; }

    public string? PropertyCategory { get; init; }
    public string? PropertyDescription { get; init; }

    /// <summary>SocietyDetailsMast.SocietyName (regional script).</summary>
    public string? SocietyName { get; init; }

    /// <summary>SocietyDetailsMast.SocietyNameEnglish (English/Latin script).</summary>
    public string? SocietyNameEnglish { get; init; }

    public string? SocietyAddress { get; init; }
    public string? SocietyAddressEnglish { get; init; }
    public string? SocietyEmailId { get; init; }

    public string? LandOwnerName { get; init; }
    public string? LandOwnerNameEnglish { get; init; }

    public string? BuilderName { get; init; }
    public string? BuilderNameEnglish { get; init; }
    public string? BuilderMobileNo { get; init; }

    /// <summary>PropertyMast.OwnerNameEnglish — the English/Latin-script owner name.</summary>
    public string? Owner { get; init; }

    /// <summary>PropertyMast.OwnerName — the regional/vernacular-script owner name.</summary>
    public string? HolderRegional { get; init; }

    /// <summary>PropertyMast.OccupierNameEnglish — the English/Latin-script occupier name.</summary>
    public string? OccupierName { get; init; }

    /// <summary>PropertyMast.OccupierName — the regional/vernacular-script occupier name.</summary>
    public string? OccupierRegional { get; init; }

    /// <summary>OwnerTypeMaster.OwnerType, resolved from PropertyMastDetails.OwnerTypeId.</summary>
    public string? OwnerCategory { get; init; }

    /// <summary>
    /// DocumentEntity.DocumentGuid for the society's SOCIETY_PLACE photo (the "main building" photo),
    /// null when none has been uploaded. Frontend builds the thumbnail URL as
    /// <c>GET /api/documents/{guid}/view</c>.
    /// </summary>
    public Guid? SocietyBuildingPhotoGuid { get; init; }

    /// <summary>
    /// Active child wings for this property's society (from PTIS.WingDetailsMast).
    /// Used by frontend to render checkboxes for selective updates.
    /// </summary>
    public List<SocietyWingSummaryDto> Wings { get; init; } = new();
}

/// <summary>
/// Summary of a child wing belonging to the property's society (from PTIS.WingDetailsMast).
/// Used by frontend to display wing lists and selective update checkboxes.
/// </summary>
public sealed class SocietyWingSummaryDto
{
    public int WingDetailId { get; init; }
    public int? WingMasterId { get; init; }
    public string? WingName { get; init; }
    public string? SecretaryName { get; init; }
    public string? SecretaryNameEnglish { get; init; }
    public string? SecretaryMobileNo { get; init; }
    public string? SecretaryEmailId { get; init; }
    public string? ManagerName { get; init; }
    public string? ManagerNameEnglish { get; init; }
    public string? ManagerMobileNo { get; init; }
    public string? ManagerEmailId { get; init; }
}


