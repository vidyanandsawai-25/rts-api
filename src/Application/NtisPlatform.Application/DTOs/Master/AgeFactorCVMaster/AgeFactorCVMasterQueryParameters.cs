using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.AgeFactorCVMaster;

public class AgeFactorCVMasterQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? ConstructionTypeId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public int? YearRangeCVId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public bool? IsActive { get; set; }

    [Filterable(FilterOperator.GreaterThanOrEqual)]
    [Sortable]
    [Searchable]
    public int? AgeFrom { get; set; }

    [Filterable(FilterOperator.LessThanOrEqual)]
    [Sortable]
    [Searchable]
    public int? AgeTo { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public string? ConstructionCode { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public string? ConstructionDescription { get; set; }
}
