using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.UseFactorCVMaster;

public class UseFactorCVMasterQueryParameters : BaseQueryParameters
{

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public int? TypeOfUseId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public int? SubTypeOfUseId { get; set; }

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
    public string? TypeOfUseCode { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public string? TypeOfUseDescription { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public string? Type { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public int? TypeOfUseGroupId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public string? SubTypeOfUseDescription { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public int? FromYear { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    [Searchable]
    public int? ToYear { get; set; }

}
