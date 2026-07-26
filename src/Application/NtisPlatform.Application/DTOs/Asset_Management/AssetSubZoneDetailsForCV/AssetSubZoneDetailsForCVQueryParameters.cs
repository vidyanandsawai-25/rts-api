using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Asset_Management;

public class AssetSubZoneDetailsForCVQueryParameters : BaseQueryParameters
{
    [Filterable]
    public int? MoujaId { get; set; }

    [Filterable]
    [Searchable]
    [Sortable]
    public string? SubZoneNo { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? SubZoneName { get; set; }

    [Filterable]
    public bool? IsActive { get; set; }

    [Filterable]
    public bool? MarkedForDeletion { get; set; }
}
