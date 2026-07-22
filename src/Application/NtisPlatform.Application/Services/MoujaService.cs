using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Services;

public class MoujaService : BaseCommonCrudService<MoujaEntity, MoujaDto, CreateMoujaDto, UpdateMoujaDto, MoujaQueryParameters, int>, IMoujaService
{
    private readonly IReferenceValidationService _referenceValidator;
    public MoujaService(
        IRepository<MoujaEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper) 
    {
        _referenceValidator = referenceValidator;
    }
    /// <summary>
    /// Validates deactivation (IsActive change from true to false) for MoujaEntity.
    /// Uses centralized IReferenceValidationService to check references in related tables.
    /// </summary>
    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        MoujaEntity currentEntity,
        MoujaEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<MoujaEntity>(id, cancellationToken);
        }

        return ValidationResult.Success();
    }
}
