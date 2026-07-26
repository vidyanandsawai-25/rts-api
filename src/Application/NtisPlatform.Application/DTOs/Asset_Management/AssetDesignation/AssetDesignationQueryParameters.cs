using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master;

public class AssetDesignationQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public int? OwningDepartmentId { get; set; }

    [Filterable]
    [Searchable]
    [Sortable]
    public string? DesignationCode { get; set; }

    [Filterable]
    [Searchable]
    [Sortable]
    public string? DesignationName { get; set; }

    [Filterable]
    [Searchable]
    [Sortable]
    public string? DesignationLocal { get; set; }

    [Filterable]
    [Sortable]
    public bool? IsActive { get; set; }

    [Filterable]
    [Sortable]
    public bool? MarkedForDeletion { get; set; }
}
