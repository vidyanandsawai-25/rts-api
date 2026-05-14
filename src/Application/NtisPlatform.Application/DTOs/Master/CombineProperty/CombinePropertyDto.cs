using System.ComponentModel.DataAnnotations;

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
    public decimal? TaxAmount { get; set; }
    public decimal? PendingAmount { get; set; }
}

/// <summary>
/// Request DTO for combining properties
/// </summary>
public class CombinePropertiesRequestDto
{
    [Required(ErrorMessage = "CombineProperty_MainPropertyId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "CombineProperty_MainPropertyId_Invalid")]
    public int MainPropertyId { get; set; }

    [Required(ErrorMessage = "CombineProperty_CombinePropertyIds_Required")]
    public string CombinePropertyIds { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "CombineProperty_Remark_MaxLen_500")]
    public string? Remark { get; set; }

    public int? CreatedBy { get; set; }
}

/// <summary>
/// Response DTO for combine properties operation
/// </summary>
public class CombinePropertiesResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int MainPropertyId { get; set; }
    public List<int> CombinedPropertyIds { get; set; } = new();
}
