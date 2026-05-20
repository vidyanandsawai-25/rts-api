using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.FloorFactorCVMaster;

public class FloorFactorCVMasterQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public int? FloorId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public int? YearRangeCVId { get; set; }

    [Filterable(FilterOperator.Equals)]
    public bool? IsActive { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public string? FloorCode { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public string? FloorDescription { get; set; }
}
