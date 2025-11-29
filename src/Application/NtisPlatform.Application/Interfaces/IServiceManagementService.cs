using NtisPlatform.Application.DTOs;

namespace NtisPlatform.Application.Interfaces;

public interface IServiceManagementService
{
    Task<List<ServiceDto>> GetServicesAsync();
}
