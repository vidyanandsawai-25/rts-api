using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class SocietyDetailsQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public int? PropertyId { get; set; }

    public int? WingId { get; set; }

    public string? WingName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? SocietyName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? SecretaryName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? ManagerName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? SocietyEmailId { get; set; }
}
