using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master;

public class DynamicTaxRuleQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Contains)]
    [Sortable]
    [Searchable]
    public string? DisplayName { get; set; }

    [Filterable]
    [Sortable]
    public string? RuleType { get; set; }


    [Filterable]
    [Sortable]
    public bool? IsActive { get; set; }
}
