using NtisPlatform.Application.DTOs;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service interface for Sample entity operations
/// </summary>
public interface ISampleService
{
    Task<SampleDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<SampleDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SampleDto> CreateAsync(CreateSampleDto dto, CancellationToken cancellationToken = default);
    Task UpdateAsync(UpdateSampleDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
