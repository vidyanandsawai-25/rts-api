using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master;

public class AssetOrganizationMasterQueryParameters : BaseQueryParameters
{
    [Filterable]
    public int? AuthorityId { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? OrganizationCode { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? OrganizationName { get; set; }

    [Filterable]
    [Sortable]
    public bool? IsActive { get; set; }
}
