using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.GenderMaster;

public class GenderQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public int? Id { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? GenderName { get; set; }

    [Filterable(FilterOperator.Equals)]
    public bool? IsActive { get; set; }
}