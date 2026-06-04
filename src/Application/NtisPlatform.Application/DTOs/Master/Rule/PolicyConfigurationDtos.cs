using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class PolicyConfigurationDto : BaseDtos
{
    public string PolicyCode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DataType { get; set; } = string.Empty;
    public string? PolicyValue { get; set; }
    public string? DefaultValue { get; set; }
    public string? Unit { get; set; }
    public string? AllowedValues { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class CreatePolicyConfigurationDto : CreateBaseDtos
{
    [Required(ErrorMessage = "PolicyConfiguration_PolicyCode_Required")]
    [StringLength(50, ErrorMessage = "PolicyConfiguration_PolicyCode_MaxLen_50")]
    public string PolicyCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "PolicyConfiguration_Category_Required")]
    [StringLength(50, ErrorMessage = "PolicyConfiguration_Category_MaxLen_50")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "PolicyConfiguration_DisplayName_Required")]
    [StringLength(100, ErrorMessage = "PolicyConfiguration_DisplayName_MaxLen_100")]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "PolicyConfiguration_Description_MaxLen_500")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "PolicyConfiguration_DataType_Required")]
    [StringLength(20, ErrorMessage = "PolicyConfiguration_DataType_MaxLen_20")]
    public string DataType { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "PolicyConfiguration_PolicyValue_MaxLen_500")]
    public string? PolicyValue { get; set; }

    [StringLength(500, ErrorMessage = "PolicyConfiguration_DefaultValue_MaxLen_500")]
    public string? DefaultValue { get; set; }

    [StringLength(30, ErrorMessage = "PolicyConfiguration_Unit_MaxLen_30")]
    public string? Unit { get; set; }

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class UpdatePolicyConfigurationDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "PolicyConfiguration_PolicyCode_Required")]
    [StringLength(50, ErrorMessage = "PolicyConfiguration_PolicyCode_MaxLen_50")]
    public string PolicyCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "PolicyConfiguration_Category_Required")]
    [StringLength(50, ErrorMessage = "PolicyConfiguration_Category_MaxLen_50")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "PolicyConfiguration_DisplayName_Required")]
    [StringLength(100, ErrorMessage = "PolicyConfiguration_DisplayName_MaxLen_100")]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "PolicyConfiguration_Description_MaxLen_500")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "PolicyConfiguration_DataType_Required")]
    [StringLength(20, ErrorMessage = "PolicyConfiguration_DataType_MaxLen_20")]
    public string DataType { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "PolicyConfiguration_PolicyValue_MaxLen_500")]
    public string? PolicyValue { get; set; }

    [StringLength(500, ErrorMessage = "PolicyConfiguration_DefaultValue_MaxLen_500")]
    public string? DefaultValue { get; set; }

    [StringLength(30, ErrorMessage = "PolicyConfiguration_Unit_MaxLen_30")]
    public string? Unit { get; set; }

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
