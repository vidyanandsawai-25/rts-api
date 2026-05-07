using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.RuleOperatorMaster
{
    public class RuleOperatorDto : BaseDtos
    {
        public string? Operator { get; set; }
        public string? OperatorDescription { get; set; }
    }
    public class CreateRuleOperatorDto : CreateBaseDtos
    {
        [Required(ErrorMessage = "Operator_Required")]
        [StringLength(100, ErrorMessage = "Operator_MaxLen_100")]
        public string Operator { get; set; } = string.Empty;
        [StringLength(100, ErrorMessage = "OperatorDescription_MaxLen_100")]
        public string OperatorDescription { get; set; } = string.Empty;
    }

    public class UpdateRuleOperatorDto : UpdateBaseDtos
    {
        [Required(ErrorMessage = "Operator_Required")]
        [StringLength(100, ErrorMessage = "Operator_MaxLen_100")]
        public string Operator { get; set; } = string.Empty;
        [StringLength(100, ErrorMessage = "OperatorDescription_MaxLen_100")]
        public string OperatorDescription { get; set; } = string.Empty;
    }

}