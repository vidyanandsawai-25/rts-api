using NtisPlatform.Application.DTOs.Property.ApartmentQC;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service interface for retrieving apartment details filtered wing-wise and property-wise.
/// </summary>
public interface IGetApartmentDetailsWingWiseService
{
    /// <summary>
    /// Returns a paginated list of apartment QC records filtered wing-wise using WingDetailId.
    /// </summary>
    Task<PagedResult<ApartmentQCComparisonDto>> GetApartmentDetailsWingWiseAsync(
        GetApartmentDetailsWingWiseQueryParameters query,
        CancellationToken cancellationToken = default);
}
