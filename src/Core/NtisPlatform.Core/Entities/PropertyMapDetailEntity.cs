namespace NtisPlatform.Core.Entities;

/// <summary>
/// Entity representing the PropertyMapDetail table.
/// Stores mapping details between properties (OLD and NEW sides) in PropertyMapMaster.
/// </summary>
public class PropertyMapDetailEntity : BaseEntity
{
    public int PropertyMapId { get; set; }
    public string PropertySide { get; set; } = string.Empty; // OLD or NEW
    public int? PropertyIdNew { get; set; }
    public int? PropertyIdOld { get; set; }
    public string PropertyNoOld { get; set; } = string.Empty;
    public string PropertyNoNew { get; set; } = string.Empty;
    public string PropertyNo { get; set; } = string.Empty;
    public decimal? TaxSharePercent { get; set; }
    public decimal? AreaSharePercent { get; set; }
    public string Status { get; set; } = "ACTIVE"; // ACTIVE, MODIFIED, CANCELLED, DRAFT
    public bool IsCurrent { get; set; } = true;
    public string? ChangeReason { get; set; }
    public string? Remark { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Location { get; set; }
    public virtual MergeDetailEntity? MergeDetail { get; set; }
}