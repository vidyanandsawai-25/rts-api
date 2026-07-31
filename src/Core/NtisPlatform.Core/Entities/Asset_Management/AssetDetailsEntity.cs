namespace NtisPlatform.Core.Entities.Asset_Management;

/// <summary>
/// Auxiliary location + KYC details for an asset (AMS.AssetDetails).
/// PK is AssetId (1:1 with AssetMaster). Holds everything the Basic-Info form captures
/// beyond the AssetMaster core: organization, zone/ward/mouja/sub-zone, plot identification,
/// plot dimensions/area, address, and the in-charge (KYC) contact.
/// </summary>
public class AssetDetailsEntity : BaseEntity
{
    // PK of AMS.AssetDetails is AssetId (1:1 with AssetMaster); BaseEntity.Id is ignored in config.
    public int AssetId { get; set; }

    // Jurisdiction (OrganizationId is NOT NULL in the DB)
    public int OrganizationId { get; set; }

    // Location context
    public int? ZoneId { get; set; }
    public int? WardId { get; set; }
    public int? MoujaId { get; set; }
    public int? SubZoneId { get; set; }
    public string? AssetWardNo { get; set; }

    // Identification
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public string? UpicId { get; set; }
    public string? PlotNo { get; set; }
    public string? CSN { get; set; }

    // Plot dimensions / area
    public decimal? LandRate { get; set; }
    public decimal? LengthFt { get; set; }
    public decimal? LengthMtr { get; set; }
    public decimal? WidthFt { get; set; }
    public decimal? WidthMtr { get; set; }
    public decimal? LandAreaSqFeet { get; set; }
    public decimal? LandAreaSqMeter { get; set; }

    // Address
    public string? Address { get; set; }
    public string? NearestLandmark { get; set; }
    public string? PinCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? BoundaryGeoJson { get; set; }

    // Contact Details (In-Charge). InChargeMobile maps to schema column InchargeContact.
    public string? InChargeName { get; set; }
    public string? InChargeRegionalName { get; set; }
    public int? InChargeDesignationId { get; set; }
    public string? InChargeMobile { get; set; }
    public string? InChargeEmail { get; set; }

    // Soft delete properties
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }

    // ---------------------------------------------------------------------
    // Compatibility shims: dropped from AMS.AssetDetails in the re-architecture.
    // Kept so legacy code referencing them compiles, but excluded from the EF
    // model via Fluent Ignore() in ApplicationDbContext (Core entities stay pure
    // POCOs). Never persisted. (Capital value now lives on AMS.SubUnitsDetails;
    // lift/carpet/builtup are gone.)
    // ---------------------------------------------------------------------
    public decimal? CapitalValue { get; set; }
    public bool HasLift { get; set; }
    public decimal? Length { get; set; }
    public decimal? Width { get; set; }
    public decimal? BuiltupAreaSqMeter { get; set; }
    public decimal? CarpetAreaSqMeter { get; set; }
    public string? GstNo { get; set; }
    public string? ShopActNo { get; set; }

    // Navigation property (no virtual keyword per Guidelines.cs)
    public AssetMasterEntity Asset { get; set; } = null!;
}
