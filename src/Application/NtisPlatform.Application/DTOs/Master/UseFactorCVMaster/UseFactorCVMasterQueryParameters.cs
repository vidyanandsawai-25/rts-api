using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.UseFactorCVMaster;

public class UseFactorCVMasterQueryParameters : BaseQueryParameters
{

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? TypeOfUseId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? SubTypeOfUseId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public int? YearRangeCVId { get; set; }

    [Filterable(FilterOperator.Equals)]
    public bool? IsActive { get; set; }

}
