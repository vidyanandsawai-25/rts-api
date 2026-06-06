using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class TypeOfUseGroupCVQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    [Searchable]
    public string? TypeOfUseGroupCVCode { get; set; }

    [Filterable]
    [Sortable]
    [Searchable]
    public string? GroupName { get; set; }

    [Filterable]
    public bool? IsFloorWiseRateApplicable { get; set; }
}
