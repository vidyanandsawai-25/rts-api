using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master.PropertyTypeMaster;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Services;

public class PropertyTypeMasterService : BaseCommonCrudService<PropertyTypeMasterEntity, PropertyTypeMasterDto, CreatePropertyTypeMasterDto, UpdatePropertyTypeMasterDto, PropertyTypeMasterQueryParameters, int>, IPropertyTypeMasterService
{
    private readonly IReferenceValidationService _referenceValidator;
    private readonly IHardDeleteCleanupService _hardDeleteCleanupService;
    private readonly IRepository<PropertyEntity, int> _propertyRepository;

    public PropertyTypeMasterService(
        IRepository<PropertyTypeMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator,
        IHardDeleteCleanupService hardDeleteCleanupService,
        IRepository<PropertyEntity, int> propertyRepository)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
        _hardDeleteCleanupService = hardDeleteCleanupService;
        _propertyRepository = propertyRepository;
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

    /// <summary>
    /// Counts the Property records that reference this PropertyType. If none exist, permanently
    /// deletes it via IHardDeleteCleanupService.
    /// </summary>
    public async Task<bool> ForceDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var propertyCount = await _propertyRepository.GetQueryable()
            .CountAsync(p => p.PropertyTypeId == id, cancellationToken);

        if (propertyCount > 0)
        {
            throw new ValidationException(
                $"Cannot delete this Property Type because it is linked to {propertyCount} propert{(propertyCount == 1 ? "y" : "ies")}.",
                OperationType.Delete);
        }

        return await _hardDeleteCleanupService.ForceHardDeleteAsync<PropertyTypeMasterEntity, int>(id, cancellationToken);
    }
}
