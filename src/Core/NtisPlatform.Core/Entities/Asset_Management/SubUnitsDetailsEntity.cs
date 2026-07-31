namespace NtisPlatform.Core.Entities.Asset_Management;

/// <summary>
/// Sub-unit details entity for the Asset Management System.
/// Represents floor-wise details of building assets including construction, usage, and valuation information.
/// Maps to [AMS].[SubUnitsDetails].
/// </summary>
public class SubUnitsDetailsEntity : BaseEntity
{
    public int AssetId { get; set; }
    public int FloorId { get; set; }
    public int? SubFloorId { get; set; }
    public string? ConstructionYear { get; set; }
    public string? AssessmentYear { get; set; }
    public int ConstructionTypeId { get; set; }
    public int TypeOfUseId { get; set; }
    public int? SubTypeOfUseId { get; set; }
    public decimal? CarpetAreaSqMeter { get; set; }
    public decimal? CarpetAreaSqFeet { get; set; }
    public decimal? BuiltUpAreaSqMeter { get; set; }
    public decimal? BuiltUpAreaSqFeet { get; set; }
    public int? NoOfRooms { get; set; }
    public decimal? CVAgeFactor { get; set; }
    public decimal? CVFloorFactor { get; set; }
    public decimal? CVNatureFactor { get; set; }
    public decimal? CVUseFactor { get; set; }
    public decimal? CVBaseRate { get; set; }
    public decimal? BaseValue { get; set; }
    public decimal? CapitalValue { get; set; }
    public bool? IsRented { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }

    // Navigation properties
    public AssetMasterEntity? Asset { get; set; }
    public FloorEntity? Floor { get; set; }
    public SubFloorEntity? SubFloor { get; set; }
    public ConstructionTypeEntity? ConstructionType { get; set; }
    // NOTE: TypeOfUseId/SubTypeOfUseId's *database* FK points at AMS.AssetTypeOfUseMaster /
    // AMS.AssetSubTypeOfUseMaster (the tables the UI's Type of Use / Sub Type of Use dropdowns are
    // populated from), not at these CORE navigation targets. The nav properties stay typed to CORE's
    // TypeOfUseEntity/SubTypeOfUseEntity only because AssetCapitalValueService's rate lookup depends
    // on TypeOfUseEntity.TypeOfUseGroupCV; they are read-only conveniences for that one lookup and
    // are never used on the SubUnitsDetails create/update path, which only writes the scalar ids.
    public TypeOfUseEntity? TypeOfUse { get; set; }
    public SubTypeOfUseEntity? SubTypeOfUse { get; set; }
}
