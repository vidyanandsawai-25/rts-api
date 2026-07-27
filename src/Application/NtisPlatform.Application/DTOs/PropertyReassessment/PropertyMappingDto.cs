namespace NtisPlatform.Application.DTOs.PropertyReassessment;

/// <summary>
/// Represents a property mapping relationship between old and new properties.
/// Returned as part of the property reassessment response to provide context on how
/// the current (new) property relates to historical (old) property records via PropertyMapMaster/PropertyMapDetail.
/// </summary>
public class PropertyMappingDto
{
    /// <summary>The PropertyMapMaster record ID for this mapping relationship.</summary>
    public int PropertyMapId { get; set; }

    /// <summary>
    /// Mapping category: ONE_TO_ONE (1 old ↔ 1 new), SPLIT (1 old → multiple new),
    /// MERGE (multiple old → 1 new), or MAP (general/manual mapping).
    /// </summary>
    public string MappingCategory { get; set; } = string.Empty;

    /// <summary>Version number of the PropertyMapMaster record.</summary>
    public int VersionNo { get; set; }

    /// <summary>The old (historical) property ID, if this row represents an old-side mapping. Null for NEW-side only rows.</summary>
    public int? PropertyIdOld { get; set; }

    /// <summary>The new (current) property ID.</summary>
    public int? PropertyIdNew { get; set; }

    /// <summary>Property number from the mapping detail record.</summary>
    public string PropertyNo { get; set; } = string.Empty;

    /// <summary>Tax share percentage (if applicable to this mapping).</summary>
    public decimal? TaxSharePercent { get; set; }

    /// <summary>Area share percentage (if applicable to this mapping).</summary>
    public decimal? AreaSharePercent { get; set; }

    /// <summary>Status: ACTIVE, MODIFIED, CANCELLED, or DRAFT.</summary>
    public string Status { get; set; } = string.Empty;
}
