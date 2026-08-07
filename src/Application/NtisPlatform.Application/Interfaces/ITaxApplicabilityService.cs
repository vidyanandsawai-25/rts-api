using NtisPlatform.Application.DTOs.TaxApplicability;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Service interface for tax applicability operations
/// </summary>
public interface ITaxApplicabilityService : ICommonCrudService<ApplyTaxesMasterEntity, TaxApplicabilityResponseDto, CreateTaxApplicabilityRequestDto, UpdateTaxApplicabilityRequestDto, TaxApplicabilityRequestDto, int>
{
    /// <summary>
    /// Gets applicable and exempted taxes for a property based on the given parameters
    /// </summary>
    /// <param name="request">Tax applicability request containing property, financial year, type of use, and RV/CV indicator</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tax applicability response with applicable and exempted taxes</returns>
    Task<TaxApplicabilityResponseDto> GetTaxApplicabilityAsync(
        TaxApplicabilityRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets property details mapped with unique finance years and type of use
    /// </summary>
    /// <param name="propertyId">Property ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of property finance year and type of use details</returns>
    Task<IEnumerable<PropertyFinanceYearTypeOfUseDto>> GetPropertyFinanceYearTypeOfUseAsync(
        int propertyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates tax applicability configurations for a property
    /// </summary>
    /// <param name="request">Create request details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A message indicating the result of the create operation</returns>
    Task<string> CreateTaxApplicabilityAsync(
        CreateTaxApplicabilityRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates tax applicability configurations for a property
    /// </summary>
    /// <param name="id">Tax applicability record ID</param>
    /// <param name="request">Update request details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A message indicating the result of the update operation</returns>
    Task<string> UpdateTaxApplicabilityAsync(
        int id,
        UpdateTaxApplicabilityRequestDto request,
        CancellationToken cancellationToken = default);
}
