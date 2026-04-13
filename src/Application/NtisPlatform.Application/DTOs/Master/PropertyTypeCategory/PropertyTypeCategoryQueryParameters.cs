using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs;

public class PropertyTypeCategoryQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    [Searchable]
    public string? PropertyTypeCategory { get; set; }
}
