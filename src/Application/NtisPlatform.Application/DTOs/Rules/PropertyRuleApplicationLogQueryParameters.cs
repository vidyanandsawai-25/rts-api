using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Rules
{
    public class PropertyRuleApplicationLogQueryParameters : BaseQueryParameters
    {
        [Filterable(FilterOperator.Equals)]
        public int? PropertyId { get; set; }

        [Filterable(FilterOperator.Equals)]
        public int? PropertyDetailsId { get; set; }

        [Filterable(FilterOperator.Equals)]
        public int? FinanceYear { get; set; }

        [Filterable(FilterOperator.Equals)]
        public string? RuleCategory { get; set; }

        [Filterable(FilterOperator.Equals)]
        public string? RuleCode { get; set; }

        [Searchable]
        [Sortable]
        public string? RuleName { get; set; }
    }
}
