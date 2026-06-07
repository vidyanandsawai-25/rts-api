using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;

public class AssetFieldDefinitionDto : BaseDtos
{
    public int AssetCategoryId { get; set; }
    public int AssetTypeId { get; set; }
    public string FieldCode { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public string? FieldGroup { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public string? ValidationRules { get; set; }
    public string? DefaultValue { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public int? MaxLength { get; set; }
}

public class CreateAssetFieldDefinitionDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AssetFieldDefinition_AssetCategoryId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AssetFieldDefinition_AssetCategoryId_InvalidRange")]
    public int AssetCategoryId { get; set; }

    [Required(ErrorMessage = "AssetFieldDefinition_AssetTypeId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AssetFieldDefinition_AssetTypeId_InvalidRange")]
    public int AssetTypeId { get; set; }

    [Required(ErrorMessage = "AssetFieldDefinition_FieldCode_Required")]
    [StringLength(50, ErrorMessage = "AssetFieldDefinition_FieldCode_MaxLengthExceeded_50")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetFieldDefinition_FieldCode_Invalid")]
    public string FieldCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetFieldDefinition_FieldName_Required")]
    [StringLength(100, ErrorMessage = "AssetFieldDefinition_FieldName_MaxLengthExceeded_100")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetFieldDefinition_FieldName_Invalid")]
    public string FieldName { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetFieldDefinition_FieldLabel_Required")]
    [StringLength(200, ErrorMessage = "AssetFieldDefinition_FieldLabel_MaxLengthExceeded_200")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetFieldDefinition_FieldLabel_Invalid")]
    public string FieldLabel { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetFieldDefinition_FieldType_Required")]
    [StringLength(50, ErrorMessage = "AssetFieldDefinition_FieldType_MaxLengthExceeded_50")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetFieldDefinition_FieldType_Invalid")]
    public string FieldType { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "AssetFieldDefinition_FieldGroup_MaxLengthExceeded_100")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetFieldDefinition_FieldGroup_Invalid")]
    public string? FieldGroup { get; set; }

    public bool IsRequired { get; set; }

    [Required(ErrorMessage = "AssetFieldDefinition_DisplayOrder_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AssetFieldDefinition_DisplayOrder_InvalidRange")]
    public int DisplayOrder { get; set; }

    public string? ValidationRules { get; set; }

    [StringLength(500, ErrorMessage = "AssetFieldDefinition_DefaultValue_MaxLengthExceeded_500")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetFieldDefinition_DefaultValue_Invalid")]
    public string? DefaultValue { get; set; }

    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AssetFieldDefinition_MaxLength_InvalidRange")]
    public int? MaxLength { get; set; }
}

public class UpdateAssetFieldDefinitionDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AssetFieldDefinition_AssetCategoryId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AssetFieldDefinition_AssetCategoryId_InvalidRange")]
    public int AssetCategoryId { get; set; }

    [Required(ErrorMessage = "AssetFieldDefinition_AssetTypeId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AssetFieldDefinition_AssetTypeId_InvalidRange")]
    public int AssetTypeId { get; set; }

    [Required(ErrorMessage = "AssetFieldDefinition_FieldCode_Required")]
    [StringLength(50, ErrorMessage = "AssetFieldDefinition_FieldCode_MaxLengthExceeded_50")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetFieldDefinition_FieldCode_Invalid")]
    public string FieldCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetFieldDefinition_FieldName_Required")]
    [StringLength(100, ErrorMessage = "AssetFieldDefinition_FieldName_MaxLengthExceeded_100")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetFieldDefinition_FieldName_Invalid")]
    public string FieldName { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetFieldDefinition_FieldLabel_Required")]
    [StringLength(200, ErrorMessage = "AssetFieldDefinition_FieldLabel_MaxLengthExceeded_200")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetFieldDefinition_FieldLabel_Invalid")]
    public string FieldLabel { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetFieldDefinition_FieldType_Required")]
    [StringLength(50, ErrorMessage = "AssetFieldDefinition_FieldType_MaxLengthExceeded_50")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetFieldDefinition_FieldType_Invalid")]
    public string FieldType { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "AssetFieldDefinition_FieldGroup_MaxLengthExceeded_100")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetFieldDefinition_FieldGroup_Invalid")]
    public string? FieldGroup { get; set; }

    public bool IsRequired { get; set; }

    [Required(ErrorMessage = "AssetFieldDefinition_DisplayOrder_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AssetFieldDefinition_DisplayOrder_InvalidRange")]
    public int DisplayOrder { get; set; }

    public string? ValidationRules { get; set; }

    [StringLength(500, ErrorMessage = "AssetFieldDefinition_DefaultValue_MaxLengthExceeded_500")]
    [RegularExpression(@"^[^@#]*$", ErrorMessage = "AssetFieldDefinition_DefaultValue_Invalid")]
    public string? DefaultValue { get; set; }

    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AssetFieldDefinition_MaxLength_InvalidRange")]
    public int? MaxLength { get; set; }
}
