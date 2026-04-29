using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.NatureFactorCVMaster;

public class NatureFactorCVMasterQueryParameters : BaseQueryParameters
{

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? ConstructionTypeId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? YearRangeCVId { get; set; }

    [Filterable(FilterOperator.Equals)]
    public bool? IsActive { get; set; }

}
