using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;


/// <summary>
/// Read DTO for CombineProperty - Used for GET operations
/// </summary>
public class CombinePropertyDto : BaseDtos
{
    public int? WardId { get; set; }
    public string? WardNo { get; set; }
    public string? PropertyNo { get; set; }
    public string? FromProperty { get; set; }
    public string? ToProperty { get; set; }
    public int? CategoryId { get; set; }
    public int? SocietyDetailId { get; set; }
}

/// <summary>
/// Create DTO for CombineProperty - Used for POST operations
/// </summary>
public class CreateCombinePropertyDto : CreateBaseDtos
{
    [Required(ErrorMessage = "CombineProperty_TaxZoneId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "CombineProperty_TaxZoneId_Invalid")]
    public int TaxZoneId { get; set; }

    [Required(ErrorMessage = "CombineProperty_WardId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "CombineProperty_WardId_Invalid")]
    public int WardId { get; set; }

    [Required(ErrorMessage = "CombineProperty_PropertyNo_Required")]
    [StringLength(10, ErrorMessage = "CombineProperty_PropertyNo_MaxLen_10")]
    public string PropertyNo { get; set; } = string.Empty;

    [StringLength(10, ErrorMessage = "CombineProperty_PartitionNo_MaxLen_10")]
    public string? PartitionNo { get; set; }
}

/// <summary>
/// Update DTO for CombineProperty - Used for PUT operations
/// </summary>
public class UpdateCombinePropertyDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "CombineProperty_TaxZoneId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "CombineProperty_TaxZoneId_Invalid")]
    public int TaxZoneId { get; set; }

    [Required(ErrorMessage = "CombineProperty_WardId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "CombineProperty_WardId_Invalid")]
    public int WardId { get; set; }

    [Required(ErrorMessage = "CombineProperty_PropertyNo_Required")]
    [StringLength(10, ErrorMessage = "CombineProperty_PropertyNo_MaxLen_10")]
    public string PropertyNo { get; set; } = string.Empty;

    [StringLength(10, ErrorMessage = "CombineProperty_PartitionNo_MaxLen_10")]
    public string? PartitionNo { get; set; }
}

/// <summary>
/// DTO for property combine details response
/// </summary>
public class PropertyCombineDetailsDto
{
    public int PropertyId { get; set; }
    public int? WardId { get; set; }
    public string? WardNo { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public string? OldPropertyNo { get; set; }
    public string? OwnerName { get; set; }
    public string? OccupierName { get; set; }
    public int? CategoryId { get; set; }
    public int? PropertyTypeId { get; set; }
    public string? PropertyDescription { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? PendingAmount { get; set; }
}

/// <summary>
/// Request DTO for combining properties
/// </summary>
public class CombinePropertiesRequestDto
{
    [Required(ErrorMessage = "CombineProperty_SourcePropertyId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "CombineProperty_SourcePropertyId_Invalid")]
    public int SourcePropertyId { get; set; }

    [Required(ErrorMessage = "CombineProperty_CombinedPropertyIds_Required")]
    public string CombinedPropertyIds { get; set; } = string.Empty;

    [Required(ErrorMessage = "CombineProperty_Reason_Required")]
    [StringLength(500, ErrorMessage = "CombineProperty_Reason_MaxLen_500")]
    public string CombineReason { get; set; } = string.Empty;

    public int? CreatedBy { get; set; }

    /// <summary>
    /// Set to true to allow combining properties even when owner names are different.
    /// Default is false. When false and owner names differ, the API returns an error message.
    /// When user confirms (clicks Yes), resend the request with this flag set to true to proceed with combine.
    /// </summary>
    public bool OverrideOwnerNameMismatch { get; set; } = false;

    /// <summary>
    /// The PropertyTypeId to set on the main/source property after combining.
    /// This value will be updated in the PropertyMast table.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "CombineProperty_PropertyTypeId_Invalid")]
    public int? PropertyTypeId { get; set; }
}

/// <summary>
/// Response DTO for combine properties operation
/// </summary>
public class CombinePropertiesResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int SourcePropertyId { get; set; }
    public List<int> CombinedPropertyIds { get; set; } = new();
}

/// <summary>
/// DTO for combine property history response - returns all CombinedPropertyId data for a given SourcePropertyId
/// </summary>
public class CombinePropertyHistoryDto
{
    public int PropertyId { get; set; }
    public int? WardId { get; set; }
    public string? WardNo { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public string? OldPropertyNo { get; set; }
    public string? OwnerName { get; set; }
    public string? OccupierName { get; set; }
    public int? CategoryId { get; set; }
    public int? PropertyTypeId { get; set; }
    public string? PropertyDescription { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? PendingAmount { get; set; }
    
    /// <summary>
    /// The reason for combining properties from PTIS.CombinePropertyHistory table.
    /// This will be null for the source property and populated for combined properties.
    /// </summary>
    public string? CombineReason { get; set; }
}

/// <summary>
/// Query parameters for getting combine property history
/// </summary>
public class CombinePropertyHistoryQueryParameters : BaseQueryParameters
{
    /// <summary>
    /// The source property ID to get all combined property history for.
    /// This parameter is optional.
    /// </summary>
    public int? SourcePropertyId { get; set; }
    public string? PropertyNo { get; set; }
    public string? PartitionNo { get; set; }
    public int? WardId { get; set; }
}
