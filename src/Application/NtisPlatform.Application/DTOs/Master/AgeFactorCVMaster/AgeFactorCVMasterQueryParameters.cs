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
    public int? YearRangeCVId { get; set; }

    [Filterable(FilterOperator.Equals)]
    public bool? IsActive { get; set; }

    [Filterable(FilterOperator.GreaterThanOrEqual)]
    public int? AgeFrom { get; set; }

    [Filterable(FilterOperator.LessThanOrEqual)]
    public int? AgeTo { get; set; }
}
