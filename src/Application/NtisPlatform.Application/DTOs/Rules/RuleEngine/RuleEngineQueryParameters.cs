using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Rules.RuleEngine
{
    /// <summary>
    /// Query parameters for filtering and searching rule engine configurations
    /// </summary>
    public class RuleEngineQueryParameters : BaseQueryParameters
    {
        /// <summary>
        /// Default to Priority ascending so callers always see rules in execution order
        /// unless they explicitly override SortBy / SortOrder.
        /// </summary>
        public RuleEngineQueryParameters()
        {
            SortBy    = "Priority";
            SortOrder = "asc";
        }

        [Filterable(FilterOperator.Equals)]
        public string? RuleCode { get; set; }

        [Searchable]
        [Sortable]
        public string? RuleName { get; set; }

        [Filterable(FilterOperator.Equals)]
        public string? RuleCategory { get; set; }

        [Filterable(FilterOperator.Equals)]
        [Sortable]
        public bool? IsEnabled { get; set; }

        /// <summary>Filter: Priority &gt;= MinPriority</summary>
        [Filterable(FilterOperator.GreaterThanOrEqual, EntityProperty = "Priority")]
        public int? MinPriority { get; set; }

        /// <summary>Filter: Priority &lt;= MaxPriority</summary>
        [Filterable(FilterOperator.LessThanOrEqual, EntityProperty = "Priority")]
        public int? MaxPriority { get; set; }

        /// <summary>
        /// Dedicated sortable handle for Priority — allows SortBy=Priority without
        /// requiring a filter value on MinPriority/MaxPriority.
        /// </summary>
        [Sortable]
        public int? Priority { get; set; }
    }
}
