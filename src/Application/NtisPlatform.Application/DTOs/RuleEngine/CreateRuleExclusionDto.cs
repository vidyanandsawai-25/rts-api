using NtisPlatform.Application.DTOs;

namespace NtisPlatform.Application.DTOs.RuleEngine
{
    /// <summary>
    /// DTO for creating a new rule exclusion
    /// </summary>
    public class CreateRuleExclusionDto : CreateBaseDtos
    {
        public int AppliedRuleId { get; set; }
        public int SkipRuleId { get; set; }
        public string? Reason { get; set; }
    }
}
