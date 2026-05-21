using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.NatureFactorCVMaster;

public class NatureFactorCVMasterQueryParameters : BaseQueryParameters
{

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public int? ConstructionTypeId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public int? YearRangeCVId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public bool? IsActive { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public string? ConstructionCode { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public string? ConstructionDescription { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public int? FromYear { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public int? ToYear { get; set; }

}
