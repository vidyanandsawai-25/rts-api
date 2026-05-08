using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class SocietyDetailsService : BaseCommonCrudService<SocietyDetailsEntity, SocietyDetailsDto, CreateSocietyDetailsDto, UpdateSocietyDetailsDto, SocietyDetailsQueryParameters, int>, ISocietyDetailsService
{
    private readonly IReferenceValidationService _referenceValidator;

    public SocietyDetailsService(
        IRepository<SocietyDetailsEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    /// <summary>
    /// Validates deactivation (IsActive change from true to false) for SocietyDetailsEntity.
    /// Uses centralized IReferenceValidationService to check references in related tables.
    /// </summary>
    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        SocietyDetailsEntity currentEntity,
        SocietyDetailsEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<SocietyDetailsEntity>(id, cancellationToken);
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates delete operation for SocietyDetailsEntity.
    /// Uses centralized IReferenceValidationService to check references in related tables.
    /// </summary>
    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        SocietyDetailsEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<SocietyDetailsEntity>(id, cancellationToken);
    }
}
