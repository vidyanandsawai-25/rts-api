using System.ComponentModel.DataAnnotations;
namespace NtisPlatform.Application.DTOs;

public class RuleDto : BaseDtos
{
    public string RuleCode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DataType { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
}

public class CreateRuleDto : CreateBaseDtos
{
    [Required(ErrorMessage = "Rule_RuleCode_Required")]
    [StringLength(50, ErrorMessage = "Rule_RuleCode_MaxLen_50")]
    public string RuleCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rule_Category_Required")]
    [StringLength(50, ErrorMessage = "Rule_Category_MaxLen_50")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rule_DisplayName_Required")]
    [StringLength(100, ErrorMessage = "Rule_DisplayName_MaxLen_100")]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Rule_Description_MaxLen_500")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Rule_DataType_Required")]
    [StringLength(20, ErrorMessage = "Rule_DataType_MaxLen_20")]
    public string DataType { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Rule_DefaultValue_MaxLen_50")]
    public string? DefaultValue { get; set; }

}

public class UpdateRuleDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "Rule_RuleCode_Required")]
    [StringLength(50, ErrorMessage = "Rule_RuleCode_MaxLen_50")]
    public string RuleCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rule_Category_Required")]
    [StringLength(50, ErrorMessage = "Rule_Category_MaxLen_50")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rule_DisplayName_Required")]
    [StringLength(100, ErrorMessage = "Rule_DisplayName_MaxLen_100")]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Rule_Description_MaxLen_500")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Rule_DataType_Required")]
    [StringLength(20, ErrorMessage = "Rule_DataType_MaxLen_20")]
    public string DataType { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Rule_DefaultValue_MaxLen_50")]
    public string? DefaultValue { get; set; }

}


