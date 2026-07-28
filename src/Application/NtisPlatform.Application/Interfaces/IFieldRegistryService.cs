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

    Task<FieldRegistryResponseDto?> UpdateFieldRegistryAsync(
        string updateCode,
        UpdateFieldRegistryDto updateDto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hard-deletes field registry data. If <paramref name="fieldConfigId"/> (comma-separated
    /// PTIS.BulkUpdateFieldConfig.Id values) is supplied, only those specific BulkUpdateFieldConfig
    /// rows are removed and it takes precedence over <paramref name="updateCode"/>. Otherwise, all data
    /// (BulkUpdateFieldConfig, BulkUpdateHistory, and BulkUpdateMaster) for the given UpdateCode is removed.
    /// </summary>
    Task<PurgeFieldRegistryResultDto> PurgeFieldRegistryAsync(
        string? updateCode,
        string? fieldConfigId,
        CancellationToken cancellationToken = default);
}
