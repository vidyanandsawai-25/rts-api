using NtisPlatform.Application.DTOs.FieldRegistry;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Interfaces;

public interface IFieldRegistryService
{
    Task<IReadOnlyList<FieldRegistryDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<FieldRegistryDetailsDto>> GetDetailsBySchemaAsync(
        FieldRegistryDetailsQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    Task<PagedResult<FieldRegistryTableDetailsDto>> GetDetailsByTableAsync(
        FieldRegistryTableDetailsQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    Task<FieldRegistryResponseDto> AddFieldRegistryAsync(
        CreateFieldRegistryDto createDto,
        CancellationToken cancellationToken = default);

    Task<PagedResult<FieldRegistryResponseDto>> GetFieldRegistriesAsync(
        FieldRegistryQueryParameters queryParameters,
        CancellationToken cancellationToken = default);

    Task<bool> SetActiveStatusAsync(
        string updateCode,
        bool isActive,
        int? updatedBy,
        CancellationToken cancellationToken = default);
}
