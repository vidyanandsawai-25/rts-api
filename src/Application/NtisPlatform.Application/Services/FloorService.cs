using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class FloorService : BaseCommonCrudService<FloorEntity, FloorDto, CreateFloorDto, UpdateFloorDto, FloorQueryParameters, int>, IFloorService
{
    private readonly IReferenceValidationService _referenceValidator;

    public FloorService(
        IRepository<FloorEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    /// <summary>
    /// Validates deactivation (IsActive change from true to false) for FloorEntity.
    /// Uses centralized IReferenceValidationService to check references in related tables.
    /// </summary>
    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        FloorEntity currentEntity,
        FloorEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<FloorEntity>(id, cancellationToken);
        }

        return ValidationResult.Success();
    }
    /// <summary>
    /// DeleteAsync behavior and validation:
    ///
    /// - Soft Delete / Mark-for-Delete (entities with BaseEntity):
    ///     - The repository sets IsActive = false and/or MarkedForDeletion = true.
    ///     - The service's DeleteAsync calls ValidateForDeleteAsync before deleting.
    ///     - If the record is referenced elsewhere (checked via IReferenceValidationService), deletion is blocked and a ValidationException is thrown.
    ///     - This prevents soft delete/mark-for-delete if the record is in use.
    ///     - Note: UpdateAsync is NOT called during delete, so ValidateForDeactivationAsync is NOT triggered for deletes.
    ///
    /// - Hard Delete (entities without BaseEntity, e.g., via HardDeleteCleanupService):
    ///     - The record is physically removed from the database.
    ///     - No service-level validation is performed.
    ///     - Database constraints (e.g., foreign keys) prevent deletion if the record is referenced elsewhere.
    ///     - If a constraint violation occurs, it is caught and handled at the controller/middleware level.
    ///
    /// Summary:
    /// - Soft delete/mark-for-delete: ValidateForDeleteAsync uses centralized IReferenceValidationService for referential integrity/business rules.
    /// - Hard delete: Database constraints enforce integrity; no service validation needed.
    /// </summary>
    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        FloorEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<FloorEntity>(id, cancellationToken);
    }

public async Task<RangeResult<FloorDto>> CreateFromRangeAsync(RangeCreateRequest<CreateFloorDto> request, CancellationToken cancellationToken = default)    {
         // Internal transformer logic as previously in the controller
        Func<CreateFloorDto, string, int, CreateFloorDto> transformer = (template, rangeValue, sequenceNo) =>
            new CreateFloorDto
            {
                FloorCode = rangeValue,
                MaxFloorNo = template.MaxFloorNo,
                Description = string.IsNullOrEmpty(template.Description) ? $"{rangeValue} Floor" : template.Description.Replace("{value}", rangeValue),
                SequenceNo = sequenceNo,
                IsActive = template.IsActive,
                CreatedBy = template.CreatedBy
            };
        return await base.CreateFromRangeAsync(request, transformer, cancellationToken);
    }
}
