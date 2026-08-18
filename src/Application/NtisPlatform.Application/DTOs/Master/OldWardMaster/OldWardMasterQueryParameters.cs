using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs;

public class OldWardMasterQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? OldZoneName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? OldWardNo { get; set; }
}
