namespace NtisPlatform.Application.DTOs.Rules.RuleEngine
{
    /// <summary>
    /// Lightweight metadata for a single sub-rule inside a multi-rule ConditionsJson array.
    /// Returned by GetAll and GetById so the frontend can display sub-rule descriptions
    /// without having to parse the raw ConditionsJson on the client.
    /// </summary>
    public class SubRuleMetaDto
    {
        /// <summary>Sub-rule identifier (maps to the "id" field inside each ConditionsJson array element).</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Human-readable description / label for this sub-rule.</summary>
        public string? Description { get; set; }

        /// <summary>Whether this sub-rule participates in evaluation.</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>When true, rule evaluation stops after this sub-rule fires.</summary>
        public bool StopProcessing { get; set; }
    }
}
