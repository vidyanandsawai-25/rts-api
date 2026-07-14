using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Master.WaterConnection;

public class WaterConnectionQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    public int? PropertyId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsActive { get; set; }

    [Filterable]
    [Searchable]
    [Sortable]
    public string? ConnectionNo { get; set; }

    public int? FinanceYearId { get; set; }
}

public class WaterConnectionTypeQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsActive { get; set; }

    [Filterable]
    [Searchable]
    [Sortable]
    public string? ConnectionTypeName { get; set; }

    [Filterable]
    [Searchable]
    [Sortable]
    public string? ConnectionTypeCode { get; set; }
}

public class WaterConnectionSizeQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsActive { get; set; }

    [Filterable]
    [Searchable]
    [Sortable]
    public string? ConnectionSizeUnit { get; set; }

    public string? ConnectionSize { get; set; }
    public string? DisplayLabel { get; set; }
}

public class WaterConnectionStatusQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsActive { get; set; }

    [Filterable]
    [Searchable]
    [Sortable]
    public string? StatusName { get; set; }
}

public class WaterRateMasterQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    public int? WaterConnectionTypeId { get; set; }

    [Filterable(FilterOperator.Equals)]
    public int? WaterConnectionSizeId { get; set; }

    [Filterable(FilterOperator.Equals)]
    public int? FinanceYearId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsActive { get; set; }

    [Filterable]
    public decimal? YearlyRate { get; set; }

    public string? ConnectionTypeName { get; set; }
    public string? ConnectionSizeUnit { get; set; }
    public string? ConnectionSize { get; set; }
    public string? YearCode { get; set; }
}

public class WaterConnectionDetailsQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Equals)]
    public int? WaterConnectionId { get; set; }

    [Filterable(FilterOperator.Equals)]
    public int? FinanceYearId { get; set; }

    [Filterable(FilterOperator.Equals)]
    [Sortable]
    public bool? IsActive { get; set; }
}
