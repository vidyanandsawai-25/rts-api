using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class AssetDesignationService : BaseCommonCrudService<
    AssetDesignationEntity,
    AssetDesignationDto,
    CreateAssetDesignationDto,
    UpdateAssetDesignationDto,
    AssetDesignationQueryParameters,
    int>, IAssetDesignationService
{
    private readonly IRepository<OwningDepartmentEntity, int> _owningDepartmentRepository;
    private readonly IReferenceValidationService _referenceValidator;

    public AssetDesignationService(
        IRepository<AssetDesignationEntity, int> repository,
        IRepository<OwningDepartmentEntity, int> owningDepartmentRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator)
        : base(repository, unitOfWork, mapper)
    {
        _owningDepartmentRepository = owningDepartmentRepository;
        _referenceValidator = referenceValidator;
    }

    public override async Task<PagedResult<AssetDesignationDto>> GetAllAsync(
        AssetDesignationQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        var result = await base.GetAllAsync(queryParameters, cancellationToken);
        await EnrichNamesAsync(result.Items, cancellationToken);
        return result;
    }

    public override async Task<AssetDesignationDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var dto = await base.GetByIdAsync(id, cancellationToken);
        if (dto != null)
        {
            await EnrichNamesAsync(new[] { dto }, cancellationToken);
        }
        return dto;
    }

    /// <summary>
    /// Batch-resolves OwningDepartmentName for the given rows via a filtered (not full-table)
    /// join keyed by the distinct department ids already present in <paramref name="items"/>.
    /// </summary>
    private async Task EnrichNamesAsync(IEnumerable<AssetDesignationDto> items, CancellationToken cancellationToken)
    {
        var rows = items as ICollection<AssetDesignationDto> ?? items.ToList();
        if (rows.Count == 0)
            return;

        var departmentIds = rows.Select(x => x.OwningDepartmentId).Distinct().ToList();

        var departmentNames = await _owningDepartmentRepository.GetQueryable().AsNoTracking()
            .Where(x => departmentIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.OwningDepartmentName, cancellationToken);

        foreach (var row in rows)
        {
            row.OwningDepartmentName = departmentNames.GetValueOrDefault(row.OwningDepartmentId);
        }
    }

    public override async Task<AssetDesignationDto> CreateAsync(
        CreateAssetDesignationDto createDto, CancellationToken cancellationToken = default)
    {
        // DTO property is validated as [Required] by model binding before the service is reached,
        // so !.Value is safe here — see CreateAssetDesignationDto.
        await EnsureOwningDepartmentExistsAsync(createDto.OwningDepartmentId!.Value, OperationType.Create, cancellationToken);
        return await base.CreateAsync(createDto, cancellationToken);
    }

    public override async Task<AssetDesignationDto?> UpdateAsync(
        int id, UpdateAssetDesignationDto updateDto, CancellationToken cancellationToken = default)
    {
        await EnsureOwningDepartmentExistsAsync(updateDto.OwningDepartmentId!.Value, OperationType.Update, cancellationToken);
        return await base.UpdateAsync(id, updateDto, cancellationToken);
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        AssetDesignationEntity currentEntity,
        AssetDesignationEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<AssetDesignationEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        AssetDesignationEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<AssetDesignationEntity>(id, cancellationToken);
    }

    private async Task EnsureOwningDepartmentExistsAsync(int owningDepartmentId, OperationType operationType, CancellationToken cancellationToken)
    {
        var exists = await _owningDepartmentRepository.GetQueryable().AsNoTracking()
            .AnyAsync(x => x.Id == owningDepartmentId && x.IsActive && !x.MarkedForDeletion, cancellationToken);

        if (!exists)
            throw new ValidationException(nameof(CreateAssetDesignationDto.OwningDepartmentId), $"Owning department with ID {owningDepartmentId} not found.", operationType);
    }
}
