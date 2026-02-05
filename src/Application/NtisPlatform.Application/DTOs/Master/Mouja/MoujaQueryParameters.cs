using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs;

public class MoujaQueryParameters : BaseQueryParameters
{
    
    [Filterable]
    public int? Year { get; set; }
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? MoujaName { get; set; }

}

