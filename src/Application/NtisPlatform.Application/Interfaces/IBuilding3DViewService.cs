using NtisPlatform.Application.DTOs.Building3DView;
using NtisPlatform.Application.DTOs.Property;

namespace NtisPlatform.Application.Interfaces;

public interface IBuilding3DViewService
{
    Task<Building3DViewDto?> GetBuilding3DViewAsync(Building3DViewQueryParameters queryParameters, CancellationToken cancellationToken = default);
}
