using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.RuleScopeMaster
{
    public class RuleScopeDto : BaseDtos
    {
        public string? RuleScope { get; set; }
    }
    public class CreateRuleScopeDto : CreateBaseDtos
    {
        [Required(ErrorMessage = "RuleScope_Required")]
        [StringLength(100, ErrorMessage = "RuleScope_MaxLen_100")]
        public string RuleScope { get; set; } = string.Empty;
    }

    public class UpdateRuleScopeDto : UpdateBaseDtos
    {
        [Required(ErrorMessage = "RuleScope_Required")]
        [StringLength(100, ErrorMessage = "RuleScope_MaxLen_100")]
        public string RuleScope { get; set; } = string.Empty;
    }

}