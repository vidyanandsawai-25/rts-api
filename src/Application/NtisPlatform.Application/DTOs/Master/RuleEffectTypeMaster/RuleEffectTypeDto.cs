using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.RuleEffectTypeMaster
{
    public class RuleEffectTypeDto : BaseDtos
    {
        public string? EffectType { get; set; }
    }
    public class CreateRuleEffectTypeDto : CreateBaseDtos
    {
        [Required(ErrorMessage = "EffectType_Required")]
        [StringLength(100, ErrorMessage = "EffectType_MaxLen_100")]
        public string EffectType { get; set; } = string.Empty;
    }

    public class UpdateRuleEffectTypeDto : UpdateBaseDtos
    {
        [Required(ErrorMessage = "EffectType_Required")]
        [StringLength(100, ErrorMessage = "EffectType_MaxLen_100")]
        public string EffectType { get; set; } = string.Empty;
    }

}