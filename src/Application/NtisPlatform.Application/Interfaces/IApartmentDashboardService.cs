using NtisPlatform.Application.DTOs.Property.ApartmentQC.ApartmentDashboard;

namespace NtisPlatform.Application.Interfaces;

public interface IApartmentDashboardService
{
    Task<ApartmentDashboardDto> GetAllAsync(ApartmentDashboardQueryParameters queryParams, CancellationToken cancellationToken = default);
}
