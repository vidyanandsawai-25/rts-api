using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.PropertyTypeMaster;

public class PropertyTypeMasterQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Contains)]
    [Sortable]
    [Searchable]
    public string? PropertyDescription { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? Type { get; set; }
}
