using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Asset_Management;

public class AssetMoujaQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Searchable]
    [Sortable]
    public string? MoujaNo { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? MoujaName { get; set; }

    [Filterable]
    public bool? IsActive { get; set; }

    [Filterable]
    public bool? MarkedForDeletion { get; set; }
}
