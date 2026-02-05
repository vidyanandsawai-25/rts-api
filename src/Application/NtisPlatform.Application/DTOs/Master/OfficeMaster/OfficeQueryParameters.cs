using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.OfficeMaster;

public class OfficeQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? OfficeCode { get; set; }
    
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? OfficeName { get; set; }
    
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? Type { get; set; }
    
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? City { get; set; }
    
    [Filterable(FilterOperator.Equals)]
    public bool? IsActive { get; set; }
    
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? Status { get; set; }
}
