using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;

public class DynamicTaxRuleDto : BaseDtos
{
    public string? DisplayName { get; set; }
    public string? RuleType { get; set; }
    public string? AttachedReference { get; set; }
    public int SortOrder { get; set; }
    public string? Description { get; set; }
}

public class CreateDynamicTaxRuleDto : CreateBaseDtos
{
    [Required(ErrorMessage = "DynamicTaxRule_DisplayName_Required")]
    [StringLength(200, ErrorMessage = "DynamicTaxRule_DisplayName_MaxLengthExceeded_200")]
    public string? DisplayName { get; set; }

    [Required(ErrorMessage = "DynamicTaxRule_RuleType_Required")]
    [StringLength(20, ErrorMessage = "DynamicTaxRule_RuleType_MaxLengthExceeded_20")]
    public string? RuleType { get; set; }

    [StringLength(200, ErrorMessage = "DynamicTaxRule_AttachedReference_MaxLengthExceeded_200")]
    public string? AttachedReference { get; set; }

    public int SortOrder { get; set; } = 0;

    public string? Description { get; set; }
}

public class UpdateDynamicTaxRuleDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "DynamicTaxRule_DisplayName_Required")]
    [StringLength(200, ErrorMessage = "DynamicTaxRule_DisplayName_MaxLengthExceeded_200")]
    public string? DisplayName { get; set; }

    [Required(ErrorMessage = "DynamicTaxRule_RuleType_Required")]
    [StringLength(20, ErrorMessage = "DynamicTaxRule_RuleType_MaxLengthExceeded_20")]
    public string? RuleType { get; set; }

    [StringLength(200, ErrorMessage = "DynamicTaxRule_AttachedReference_MaxLengthExceeded_200")]
    public string? AttachedReference { get; set; }

    public int SortOrder { get; set; } = 0;

    public string? Description { get; set; }
}
