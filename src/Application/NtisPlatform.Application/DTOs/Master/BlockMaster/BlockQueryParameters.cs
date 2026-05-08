using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.BlockMaster;

public class BlockQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public int? Id { get; set; }

    [Filterable]
    [Sortable]
    public int? WardId { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? BlockNo { get; set; }

    [Filterable(FilterOperator.Equals)]
    public bool? IsActive { get; set; }
}