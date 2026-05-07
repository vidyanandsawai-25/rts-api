using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.CommonRemarkTypeMaster;

public class CommonRemarkTypeQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public int? Id { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? RemarkTypeName { get; set; }

    [Filterable(FilterOperator.Equals)]
    public bool? IsActive { get; set; }
}
