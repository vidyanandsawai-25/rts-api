using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.RuleEngine
{
    /// <summary>
    /// Query parameters for filtering rule exclusions
    /// </summary>
    public class RuleExclusionQueryParameters : BaseQueryParameters
    {
        public int? AppliedRuleId { get; set; }
        public int? SkipRuleId { get; set; }
        public string? RuleCode { get; set; }
    }
}
