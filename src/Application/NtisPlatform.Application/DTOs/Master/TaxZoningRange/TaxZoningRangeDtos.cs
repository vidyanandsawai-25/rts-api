using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

/// <summary>
/// Lightweight property row returned by the properties-by-ward lookup endpoint.
/// </summary>
public class WardPropertyDto
{
    public int PropertyId { get; set; }
    public int WardId { get; set; }
    public string WardNo { get; set; } = string.Empty;
    public string? PropertyNo { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Read model for a persisted tax zoning range/whole-ward assignment.
/// </summary>
public class TaxZoningRangeDto : BaseDtos
{
    public int WardId { get; set; }
    public string WardNo { get; set; } = string.Empty;
    public int TaxZoneId { get; set; }
    public string TaxZoneNo { get; set; } = string.Empty;
    public string? FromPropertyNo { get; set; }
    public string? ToPropertyNo { get; set; }
    public bool AssignEntireWard { get; set; }
    public string ZoneDescription { get; set; } = string.Empty;
    /// <summary>Populated only when <see cref="AssignEntireWard"/> is true: first property No (natural sort).</summary>
    public string? MinPropertyNo { get; set; }
    /// <summary>Populated only when <see cref="AssignEntireWard"/> is true: last property No (natural sort).</summary>
    public string? MaxPropertyNo { get; set; }
    /// <summary>
    /// Count of PropertyMast rows (partitions included) covered by this range — every property in
    /// the ward when <see cref="AssignEntireWard"/> is true, otherwise every property whose
    /// PropertyNo falls within [<see cref="FromPropertyNo"/>, <see cref="ToPropertyNo"/>].
    /// </summary>
    public int TotalProperties { get; set; }
}

/// <summary>
/// Create request. <see cref="WardIds"/> takes 1..n wards — the service branches on count:
/// exactly one ward allows range mode (From/ToPropertyNo); more than one forces whole-ward mode
/// for every selected ward, matching the mockup's "editing/ranges blocked when >1 ward checked".
/// </summary>
public class CreateTaxZoningRangeDto : CreateBaseDtos
{
    [Required(ErrorMessage = "TaxZoningRange_WardIds_Required")]
    [MinLength(1, ErrorMessage = "TaxZoningRange_WardIds_Required")]
    public List<int> WardIds { get; set; } = new();

    [Required(ErrorMessage = "TaxZoningRange_TaxZoneId_Required")]
    public int TaxZoneId { get; set; }

    /// <summary>Ignored (forced true) when more than one WardId is supplied.</summary>
    public bool AssignEntireWard { get; set; }

    [StringLength(10, ErrorMessage = "TaxZoningRange_FromPropertyNo_MaxLen_10")]
    public string? FromPropertyNo { get; set; }

    [StringLength(10, ErrorMessage = "TaxZoningRange_ToPropertyNo_MaxLen_10")]
    public string? ToPropertyNo { get; set; }

    [Required(ErrorMessage = "TaxZoningRange_ZoneDescription_Required")]
    [StringLength(200, MinimumLength = 15, ErrorMessage = "TaxZoningRange_ZoneDescription_Length")]
    public string ZoneDescription { get; set; } = string.Empty;
}

/// <summary>
/// Update request — editing is always single-range/single-ward, matching the mockup's
/// "editing blocked when >1 ward checked" rule.
/// </summary>
public class UpdateTaxZoningRangeDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "TaxZoningRange_WardId_Required")]
    public int WardId { get; set; }

    [Required(ErrorMessage = "TaxZoningRange_TaxZoneId_Required")]
    public int TaxZoneId { get; set; }

    public bool AssignEntireWard { get; set; }

    [StringLength(10, ErrorMessage = "TaxZoningRange_FromPropertyNo_MaxLen_10")]
    public string? FromPropertyNo { get; set; }

    [StringLength(10, ErrorMessage = "TaxZoningRange_ToPropertyNo_MaxLen_10")]
    public string? ToPropertyNo { get; set; }

    [Required(ErrorMessage = "TaxZoningRange_ZoneDescription_Required")]
    [StringLength(200, MinimumLength = 15, ErrorMessage = "TaxZoningRange_ZoneDescription_Length")]
    public string ZoneDescription { get; set; } = string.Empty;
}
