using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.PropertyRuleEvaluationMaster
{
    public class PropertyRuleEvaluationMasterQueryParameters : BaseQueryParameters
    {
        [Filterable(FilterOperator.Contains)]
        [Searchable]
        [Sortable]
        public string? ParameterCode { get; set; }

        [Filterable(FilterOperator.Contains)]
        [Searchable]
        [Sortable]
        public string? ParameterName { get; set; }
    }
}
