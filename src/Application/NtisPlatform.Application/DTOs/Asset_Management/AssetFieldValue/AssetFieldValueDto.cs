using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Asset_Management.AssetFieldValue;

/// <summary>
/// DTO for AssetFieldValueEntity - Dynamic field values for assets.
/// </summary>
public class AssetFieldValueDto : BaseDtos
{
    public int AssetId { get; set; }
    public int? FieldDefinitionId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? FieldValue { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }

    // Navigation property names
    public string? AssetName { get; set; }
    public string? FieldDefinitionName { get; set; }
}

public class CreateAssetFieldValueDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AMS_AssetFieldValue_AssetId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetFieldValue_AssetId_InvalidRange")]
    public int AssetId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetFieldValue_FieldDefinitionId_InvalidRange")]
    public int? FieldDefinitionId { get; set; }

    [Required(ErrorMessage = "AMS_AssetFieldValue_FieldName_Required")]
    [StringLength(100, ErrorMessage = "AMS_AssetFieldValue_FieldName_MaxLengthExceeded_100")]
    public string FieldName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "AMS_AssetFieldValue_FieldValue_MaxLengthExceeded_500")]
    public string? FieldValue { get; set; }
}

public class UpdateAssetFieldValueDto : UpdateBaseDtos
{
    public int? Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AMS_AssetFieldValue_FieldDefinitionId_InvalidRange")]
    public int? FieldDefinitionId { get; set; }

    [Required(ErrorMessage = "AMS_AssetFieldValue_FieldName_Required")]
    [StringLength(100, ErrorMessage = "AMS_AssetFieldValue_FieldName_MaxLengthExceeded_100")]
    public string FieldName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "AMS_AssetFieldValue_FieldValue_MaxLengthExceeded_500")]
    public string? FieldValue { get; set; }
}
