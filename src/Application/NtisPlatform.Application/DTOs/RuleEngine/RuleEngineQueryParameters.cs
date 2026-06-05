using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.RuleEngine
{
    /// <summary>
    /// Query parameters for filtering and searching rule engine configurations
    /// </summary>
    public class RuleEngineQueryParameters : BaseQueryParameters
    {
        [Filterable(FilterOperator.Equals)]
        public string? RuleCode { get; set; }

        [Searchable]
        public string? RuleName { get; set; }

        [Filterable(FilterOperator.Equals)]
        public string? RuleCategory { get; set; }

        [Filterable(FilterOperator.Equals)]
        public bool? IsEnabled { get; set; }

        [Filterable(FilterOperator.GreaterThanOrEqual, EntityProperty = "Priority")]
        public int? MinPriority { get; set; }

        [Filterable(FilterOperator.LessThanOrEqual, EntityProperty = "Priority")]
        public int? MaxPriority { get; set; }
    }
}
