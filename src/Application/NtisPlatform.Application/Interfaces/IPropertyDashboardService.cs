using NtisPlatform.Application.DTOs.PropertyDashboard;

namespace NtisPlatform.Application.Interfaces;

public interface IPropertyDashboardService
{
    Task<PropertyDashboardDto> GetAllAsync(PropertyDashboardQueryParameters queryParams, CancellationToken cancellationToken = default);
}
