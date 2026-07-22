using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class OwningDepartmentService : BaseCommonCrudService<
    OwningDepartmentEntity,
    OwningDepartmentDto,
    CreateOwningDepartmentDto,
    UpdateOwningDepartmentDto,
    OwningDepartmentQueryParameters,
    int>, IOwningDepartmentService
{
    private readonly IReferenceValidationService _referenceValidator;

    public OwningDepartmentService(
        IRepository<OwningDepartmentEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        OwningDepartmentEntity currentEntity,
        OwningDepartmentEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<OwningDepartmentEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        OwningDepartmentEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<OwningDepartmentEntity>(id, cancellationToken);
    }
}
