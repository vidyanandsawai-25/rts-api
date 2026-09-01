using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <inheritdoc cref="IAliasMasterService"/>
public class AliasMasterService : BaseCommonCrudService<AliasMasterEntity, AliasMasterDto, CreateAliasMasterDto, UpdateAliasMasterDto, AliasMasterQueryParameters, int>, IAliasMasterService
{
    private readonly ICurrentUserService _currentUserService;

    public AliasMasterService(
        IRepository<AliasMasterEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService)
        : base(repository, unitOfWork, mapper)
    {
        _currentUserService = currentUserService;
    }

    public override Task<PagedResult<AliasMasterDto>> GetAllAsync(AliasMasterQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        // Preserves the pre-pagination default (alphabetical by label) when the caller doesn't
        // ask for a specific sort; ApplySort's own fallback would otherwise sort by Id.
        queryParameters.SortBy ??= nameof(AliasMasterEntity.LabelName);
        return base.GetAllAsync(queryParameters, cancellationToken);
    }

    public override Task<AliasMasterDto> CreateAsync(CreateAliasMasterDto createDto, CancellationToken cancellationToken = default)
    {
        createDto.CreatedBy = _currentUserService.GetCurrentUserId();
        createDto.IsActive = true;
        return base.CreateAsync(createDto, cancellationToken);
    }

    public override Task<AliasMasterDto?> UpdateAsync(int id, UpdateAliasMasterDto updateDto, CancellationToken cancellationToken = default)
    {
        updateDto.UpdatedBy = _currentUserService.GetCurrentUserId();
        return base.UpdateAsync(id, updateDto, cancellationToken);
    }

    public async Task<bool> SetActiveStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return false;

        if (entity.IsActive == isActive)
            return true;

        entity.IsActive = isActive;
        entity.UpdatedBy = _currentUserService.GetCurrentUserId();

        await _repository.UpdateAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<List<AliasLabelDto>> GetActiveAliasesAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetQueryable().AsNoTracking()
            .Where(a => a.IsActive)
            .Select(a => new AliasLabelDto
            {
                KeyName = a.KeyName,
                EnglishName = a.EnglishName,
                RegionalName = a.RegionalName,
                HindiName = a.HindiName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AliasMasterCountDto> GetCountsAsync(CancellationToken cancellationToken = default)
    {
        var query = _repository.GetQueryable().AsNoTracking();

        var counts = await query
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Count(), Active = g.Count(a => a.IsActive) })
            .FirstOrDefaultAsync(cancellationToken);

        var total = counts?.Total ?? 0;
        var active = counts?.Active ?? 0;

        return new AliasMasterCountDto
        {
            TotalCount = total,
            ActiveCount = active,
            InactiveCount = total - active
        };
    }

    protected override async Task<ValidationResult> ValidateForCreateAsync(AliasMasterEntity entity, CancellationToken cancellationToken = default)
    {
        var exists = await _repository.GetQueryable().AsNoTracking()
            .AnyAsync(a => a.KeyName == entity.KeyName, cancellationToken);
        if (exists)
        {
            return ValidationResult.Failure(nameof(entity.KeyName), $"Field '{entity.KeyName}' already has an alias record.");
        }
        return ValidationResult.Success();
    }
}
