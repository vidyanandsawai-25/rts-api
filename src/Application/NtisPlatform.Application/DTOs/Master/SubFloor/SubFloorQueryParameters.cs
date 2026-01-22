using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class SubFloorQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Searchable]
    [Sortable]
    public string? SubFloorId { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? SubFloorDescription { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? SubFloorDescriptionEnglish { get; set; }


}
