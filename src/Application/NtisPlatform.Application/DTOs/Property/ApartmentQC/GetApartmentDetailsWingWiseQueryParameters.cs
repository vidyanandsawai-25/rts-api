using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Property.ApartmentQC;

/// <summary>
/// Query parameters for GetApartmentDetailsWingWise service/endpoint.
/// Inherits standard pagination, search, sort, and filter logic from <see cref="BaseQueryParameters"/>.
/// </summary>
public sealed class GetApartmentDetailsWingWiseQueryParameters : BaseQueryParameters
{
    /// <summary>Property ID (PropertyMast.Id).</summary>
    [Filterable]
    public int? PropertyId { get; set; }

    /// <summary>Wing Master ID (WingDetailsMast.WingMasterId).</summary>
    [Filterable]
    public int? WingId { get; set; }

    /// <summary>Wing Detail ID (WingDetailsMast.Id / PropertyMast.WingDetailId).</summary>
    [Filterable]
    public int? WingDetailId { get; set; }
}
