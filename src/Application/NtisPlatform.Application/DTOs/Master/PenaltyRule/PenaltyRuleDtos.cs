using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;

/// <summary>Read model for a penalty rule.</summary>
public class PenaltyRuleDto : BaseDtos
{
    public string PenaltyCode { get; set; } = string.Empty;
    public string PenaltyName { get; set; } = string.Empty;
    public string CalculationType { get; set; } = string.Empty;
    public decimal PenaltyValue { get; set; }
    public int GracePeriodDays { get; set; }
}

public class CreatePenaltyRuleDto : CreateBaseDtos
{
    [Required(ErrorMessage = "Penaltyrule_Code_Required")]
    [StringLength(50, ErrorMessage = "Penaltyrule_Code_MaxLen_50")]
    public string PenaltyCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Penaltyrule_Name_Required")]
    [StringLength(100, ErrorMessage = "Penaltyrule_Name_MaxLen_100")]
    public string PenaltyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Penaltyrule_CalculationType_Required")]
    public string CalculationType { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Penaltyrule_Value_Invalid")]
    public decimal PenaltyValue { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Penaltyrule_GracePeriodDays_Invalid")]
    public int GracePeriodDays { get; set; }
}

public class UpdatePenaltyRuleDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "Penaltyrule_Code_Required")]
    [StringLength(50, ErrorMessage = "Penaltyrule_Code_MaxLen_50")]
    public string PenaltyCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Penaltyrule_Name_Required")]
    [StringLength(100, ErrorMessage = "Penaltyrule_Name_MaxLen_100")]
    public string PenaltyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Penaltyrule_CalculationType_Required")]
    public string CalculationType { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "Penaltyrule_Value_Invalid")]
    public decimal PenaltyValue { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Penaltyrule_GracePeriodDays_Invalid")]
    public int GracePeriodDays { get; set; }
}

