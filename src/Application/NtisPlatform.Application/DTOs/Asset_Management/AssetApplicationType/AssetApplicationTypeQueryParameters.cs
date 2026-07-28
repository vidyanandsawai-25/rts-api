using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master;

public class AssetApplicationTypeQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Searchable]
    [Sortable]
    public string? ApplicationTypeCode { get; set; }

    [Filterable]
    [Searchable]
    [Sortable]
    public string? ApplicationTypeName { get; set; }

    [Sortable]
    public int? DisplayOrder { get; set; }

    [Filterable]
    [Sortable]
    public bool? IsActive { get; set; }

    [Filterable]
    [Sortable]
    public bool? MarkedForDeletion { get; set; }
}
