using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Services;

public class TypeOfUseCategoryService : BaseCommonCrudService<TypeOfUseCategoryEntity, TypeOfUseCategoryDto, CreateTypeOfUseCategoryDto, UpdateTypeOfUseCategoryDto, TypeOfUseCategoryQueryParameters, int>, ITypeOfUseCategoryService
{
    private readonly IReferenceValidationService _referenceValidator;

    public TypeOfUseCategoryService(
        IRepository<TypeOfUseCategoryEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        TypeOfUseCategoryEntity currentEntity,
        TypeOfUseCategoryEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<TypeOfUseCategoryEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        TypeOfUseCategoryEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<TypeOfUseCategoryEntity>(id, cancellationToken);
    }
}
