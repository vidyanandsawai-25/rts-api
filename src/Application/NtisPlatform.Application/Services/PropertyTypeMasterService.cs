using AutoMapper;
using Microsoft.AspNetCore.Http;
using NtisPlatform.Application.DTOs.Master.PropertyTypeMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Services;

public class PropertyTypeMasterService : BaseCommonCrudService<PropertyTypeMasterEntity, PropertyTypeMasterDto, CreatePropertyTypeMasterDto, UpdatePropertyTypeMasterDto, PropertyTypeMasterQueryParameters, int>, IPropertyTypeMasterService
{
    private readonly IReferenceValidationService _referenceValidator;

    public PropertyTypeMasterService(
        IRepository<PropertyTypeMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }
    /// <summary>
    /// Validates deactivation (IsActive change from true to false) for PropertyTypeMasterEntity.
    /// Uses centralized IReferenceValidationService to check references in related tables.
    /// </summary>
    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        PropertyTypeMasterEntity currentEntity,
        PropertyTypeMasterEntity  updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<PropertyTypeMasterEntity>(id, cancellationToken);
        }

        return ValidationResult.Success();
    }
}
