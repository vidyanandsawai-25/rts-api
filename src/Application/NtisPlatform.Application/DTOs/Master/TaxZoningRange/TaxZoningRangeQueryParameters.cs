using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs;

public class WardAbstractQueryParameters : BaseQueryParameters
{
    /// <summary>Optional. Filters wards whose WardNo contains this value (case-insensitive).</summary>
    public string? SearchTerm { get; set; }
}

public class WardPropertyQueryParameters : BaseQueryParameters
{
    /// <summary>Required. Ward to fetch properties for.</summary>
    public int WardId { get; set; }
}

public class TaxZoningRangeQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Sortable]
    public int? WardId { get; set; }

    [Filterable]
    [Sortable]
    public int? TaxZoneId { get; set; }

    /// <summary>Matched against either FromPropertyNo or ToPropertyNo (contains).</summary>
    public string? PropertyNo { get; set; }

    [Searchable]
    public string? Description { get; set; }
}
