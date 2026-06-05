using NtisPlatform.Application.DTOs;

namespace NtisPlatform.Application.DTOs.RuleEngine
{
    /// <summary>
    /// DTO for retrieving rule exclusion configuration
    /// </summary>
    public class RuleExclusionDto : BaseDtos
    {
        public int AppliedRuleId { get; set; }
        public int SkipRuleId { get; set; }
        public string? Reason { get; set; }

        // Denormalized fields for display
        public string AppliedRuleCode { get; set; } = string.Empty;
        public string AppliedRuleName { get; set; } = string.Empty;
        public string SkipRuleCode { get; set; } = string.Empty;
        public string SkipRuleName { get; set; } = string.Empty;
    }
}
