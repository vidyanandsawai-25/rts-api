using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Services;

public class SubTypeOfUseService : BaseCommonCrudService<SubTypeOfUseEntity, SubTypeOfUseDto, CreateSubTypeOfUseDto, UpdateSubTypeOfUseDto, SubTypeOfUseQueryParameters, int>, ISubTypeOfUseService
{
    private readonly IReferenceValidationService _referenceValidator;

    public SubTypeOfUseService(
        IRepository<SubTypeOfUseEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        SubTypeOfUseEntity currentEntity,
        SubTypeOfUseEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<SubTypeOfUseEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        SubTypeOfUseEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<SubTypeOfUseEntity>(id, cancellationToken);
    }
}

