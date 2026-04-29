using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Services;

public class TypeOfUseGroupService : BaseCommonCrudService<TypeOfUseGroupEntity, TypeOfUseGroupDto, CreateTypeOfUseGroupDto, UpdateTypeOfUseGroupDto, TypeOfUseGroupQueryParameters, int>, ITypeOfUseGroupService
{
    private readonly IReferenceValidationService _referenceValidator;

    public TypeOfUseGroupService(
        IRepository<TypeOfUseGroupEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        TypeOfUseGroupEntity currentEntity,
        TypeOfUseGroupEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<TypeOfUseGroupEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        TypeOfUseGroupEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<TypeOfUseGroupEntity>(id, cancellationToken);
    }
}

