namespace NtisPlatform.Application.DTOs.Rules.RuleCategory
{
    /// <summary>DTO for a rule category item returned by GET /api/RuleEngine/categories.</summary>
    public class RuleCategoryDto
    {
        /// <summary>Category code used as value in dropdowns (e.g. "ARV", "ALV").</summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>Human-readable label (e.g. "ARV (Annual Rateable Value)").</summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>Display order.</summary>
        public int SortOrder { get; set; }
    }
}
