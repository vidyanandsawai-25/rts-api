using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class RateMasterForCVService : BaseCommonCrudService<RateMasterForCVEntity, RateMasterForCVDto, CreateRateMasterForCVDto, UpdateRateMasterForCVDto, RateMasterForCVQueryParameters, int>, IRateMasterForCVService
{
    private readonly IRepository<CSNDetailsEntity, int> _csnDetailsRepository;
    private readonly IHardDeleteCleanupService _hardDeleteCleanupService;


    public RateMasterForCVService(
        IRepository<RateMasterForCVEntity, int> repository,
        IRepository<CSNDetailsEntity, int> csnDetailsRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IHardDeleteCleanupService hardDeleteCleanupService) // new parameter
        : base(repository, unitOfWork, mapper)
    {
        _csnDetailsRepository = csnDetailsRepository;
        _hardDeleteCleanupService = hardDeleteCleanupService;
    }

    public override async Task<PagedResult<RateMasterForCVDto>> GetAllAsync(
        RateMasterForCVQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        var query = _repository.GetQueryable().Include(x => x.CSNDetails).AsQueryable();
        query = query.ApplyFilters(queryParameters);
        query = query.ApplySearch(queryParameters);
        query = query.ApplySort(queryParameters);

        var totalCount = await query.CountAsync(cancellationToken);

        //var items = await query
        //    .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
        //    .Take(queryParameters.PageSize)
        var items = await query
            .Skip(queryParameters.PageSize == -1 ? 0 : (queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize)

            .ProjectTo<RateMasterForCVDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);


        return new PagedResult<RateMasterForCVDto>(items, totalCount, queryParameters.PageNumber, queryParameters.PageSize);
    }

    public override async Task<RateMasterForCVDto> CreateAsync(CreateRateMasterForCVDto createDto, CancellationToken cancellationToken = default)
    {
        // Create main RateMasterCV entity
        var mainEntity = _mapper.Map<RateMasterForCVEntity>(createDto);
        var savedEntity = await _repository.AddAsync(mainEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Create CSNDetails records from collection, splitting comma-separated CSNs
        if (createDto.CSNDetails != null && createDto.CSNDetails.Any())
        {
            foreach (var csnDto in createDto.CSNDetails)
            {
                if (!string.IsNullOrWhiteSpace(csnDto.CSN) && csnDto.CSN.Contains(','))
                {
                    var csnList = csnDto.CSN.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var csn in csnList)
                    {
                        var csnDetail = _mapper.Map<CSNDetailsEntity>(csnDto);
                        csnDetail.CSN = csn.Trim();
                        csnDetail.RateCVMasterId = savedEntity.Id;
                        await _csnDetailsRepository.AddAsync(csnDetail, cancellationToken);
                    }
                }
                else
                {
                    var csnDetail = _mapper.Map<CSNDetailsEntity>(csnDto);
                    csnDetail.RateCVMasterId = savedEntity.Id;
                    await _csnDetailsRepository.AddAsync(csnDetail, cancellationToken);
                }
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var entityWithCSNs = await _repository.GetQueryable()
            .Include(x => x.CSNDetails)
            .FirstOrDefaultAsync(x => x.Id == savedEntity.Id, cancellationToken);

        return _mapper.Map<RateMasterForCVDto>(entityWithCSNs ?? savedEntity);
    }


    public override async Task<RateMasterForCVDto?> UpdateAsync(int id,UpdateRateMasterForCVDto updateDto,CancellationToken cancellationToken = default)
    {
        var existingEntity = await _repository.GetQueryable()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (existingEntity == null)
            return null;

        // Update parent entity only
        _mapper.Map(updateDto, existingEntity);
        await _repository.UpdateAsync(existingEntity, cancellationToken);

        // Get existing CSNDetails separately, not through parent Include
        var existingCsnDetails = await _csnDetailsRepository.GetQueryable()
            .Where(x => x.RateCVMasterId == id)
            .ToListAsync(cancellationToken);

        foreach (var csnDetail in existingCsnDetails)
        {
            await _hardDeleteCleanupService
                .ForceHardDeleteAsync<CSNDetailsEntity, int>(
                    csnDetail.Id,
                    cancellationToken);
        }

        if (updateDto.CSNDetails != null && updateDto.CSNDetails.Any())
        {
            foreach (var csnDto in updateDto.CSNDetails)
            {
                if (string.IsNullOrWhiteSpace(csnDto.CSN))
                    continue;

                var csnList = csnDto.CSN
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct();

                foreach (var csn in csnList)
                {
                    var newCsnDetail = _mapper.Map<CSNDetailsEntity>(csnDto);

                    newCsnDetail.Id = 0;
                    newCsnDetail.CSN = csn;
                    newCsnDetail.RateCVMasterId = id;
                    newCsnDetail.UpdatedBy = updateDto.UpdatedBy;

                    await _csnDetailsRepository.AddAsync(newCsnDetail, cancellationToken);
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var entityWithUpdatedCSNs = await _repository.GetQueryable()
            .Include(x => x.CSNDetails)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return _mapper.Map<RateMasterForCVDto>(entityWithUpdatedCSNs ?? existingEntity);
    }

    public override async Task<RateMasterForCVDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetQueryable()
            .Include(x => x.CSNDetails)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity == null ? null : _mapper.Map<RateMasterForCVDto>(entity);
    }

    public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var csnDetails = await _csnDetailsRepository.GetQueryable()
            .Where(x => x.RateCVMasterId == id)
            .ToListAsync(cancellationToken);

        foreach (var csnDetail in csnDetails)
        {
            await _csnDetailsRepository.DeleteAsync(csnDetail.Id, cancellationToken);
        }

        await _repository.DeleteAsync(id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

 
    public override async Task<BulkResult<RateMasterForCVDto>> BulkCreateAsync(CreateRateMasterForCVDto[] items,CancellationToken cancellationToken = default)
    {
        var successItems = new List<RateMasterForCVDto>();
        var errors = new List<string>();

        if (items == null || items.Length == 0)
            return new BulkResult<RateMasterForCVDto>(0, 0, []);

        for (int i = 0; i < items.Length; i++)
        {
            var createDto = items[i];

            try
            {
                var entity = _mapper.Map<RateMasterForCVEntity>(createDto);
                await _repository.AddAsync(entity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var parentId = entity.Id;

                if (createDto.CSNDetails != null && createDto.CSNDetails.Any())
                {
                    foreach (var csnDto in createDto.CSNDetails)
                    {
                        if (string.IsNullOrWhiteSpace(csnDto.CSN))
                            continue;

                        var csnList = csnDto.CSN
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Trim())
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .Distinct();

                        foreach (var csn in csnList)
                        {
                            var csnDetail = new CSNDetailsEntity
                            {
                                CSN = csn,
                                RateCVMasterId = parentId,
                                IsActive = true,
                                CreatedBy = entity.CreatedBy
                            };

                            await _csnDetailsRepository.AddAsync(csnDetail, cancellationToken);
                        }
                    }
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                successItems.Add(_mapper.Map<RateMasterForCVDto>(entity));
            }
            catch (Exception ex)
            {
                errors.Add(
                    $"Record at index {i} failed: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new BulkResult<RateMasterForCVDto>(
            successItems.Count,
            items.Length - successItems.Count,
            successItems,
            errors.Count > 0 ? errors : null);
    }


    public override async Task<BulkResult<RateMasterForCVDto>> BulkUpdateAsync(BulkUpdateItem<int, UpdateRateMasterForCVDto>[] items,CancellationToken cancellationToken = default)
    {
        if (items.Length == 0)
            return new BulkResult<RateMasterForCVDto>(0, 0, []);

        var updatedEntities = new List<RateMasterForCVEntity>();
        var errors = new List<string>();
        var failedCount = 0;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var item in items)
            {
                try
                {
                    var updateItem = item.Data;

                    var entity = await _repository.GetByIdAsync(item.Id, cancellationToken);

                    if (entity == null)
                    {
                        failedCount++;
                        errors.Add($"RateMasterForCV with ID {item.Id} not found");
                        continue;
                    }

                    // 1. Update parent
                    _mapper.Map(updateItem, entity);
                    await _repository.UpdateAsync(entity, cancellationToken);
                    updatedEntities.Add(entity);

                    // 2. Get existing CSNDetails
                    var existingCsnDetails = await _csnDetailsRepository.GetQueryable()
                        .Where(x => x.RateCVMasterId == item.Id)
                        .ToListAsync(cancellationToken);

                    foreach (var csnDetail in existingCsnDetails)
                    {
                        await _hardDeleteCleanupService
                            .ForceHardDeleteAsync<CSNDetailsEntity, int>(csnDetail.Id, cancellationToken);
                    }

                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    if (updateItem.CSNDetails != null && updateItem.CSNDetails.Any())
                    {
                        foreach (var csnDto in updateItem.CSNDetails)
                        {
                            if (!string.IsNullOrWhiteSpace(csnDto.CSN))
                            {
                                var csnList = csnDto.CSN
                                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(x => x.Trim())
                                .Where(x => !string.IsNullOrWhiteSpace(x))
                                .Distinct();   

                                foreach (var csn in csnList)
                                {
                                    var newCsnDetail = _mapper.Map<CSNDetailsEntity>(csnDto);
                                    newCsnDetail.CSN = csn;
                                    newCsnDetail.RateCVMasterId = item.Id;

                                    await _csnDetailsRepository.AddAsync(newCsnDetail, cancellationToken);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;
                    errors.Add(ex.InnerException?.Message ?? ex.Message);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        var results = _mapper.Map<List<RateMasterForCVDto>>(updatedEntities);

        return new BulkResult<RateMasterForCVDto>(
            results.Count,
            failedCount,
            results,
            errors.Count > 0 ? errors : null);
    }


    public override async Task<BulkResult<int>> BulkDeleteAsync(int[] ids,CancellationToken cancellationToken = default)
    {
        if (ids == null || ids.Length == 0)
            return new BulkResult<int>(0, 0, []);

        var deletedIds = new List<int>();
        var errors = new List<string>();
        var failedCount = 0;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var id in ids)
            {
                try
                {
                    var exists = await _repository.ExistsAsync(id, cancellationToken);

                    if (!exists)
                    {
                        failedCount++;
                        errors.Add($"RateMasterForCV with ID {id} not found");
                        continue;
                    }

                    var csnDetails = await _csnDetailsRepository.GetQueryable()
                        .Where(x => x.RateCVMasterId == id)
                        .ToListAsync(cancellationToken);

                    foreach (var csnDetail in csnDetails)
                    {
                        await _csnDetailsRepository.DeleteAsync(csnDetail.Id, cancellationToken);
                    }

                    await _repository.DeleteAsync(id, cancellationToken);

                    deletedIds.Add(id);
                }
                catch (Exception ex)
                {
                    failedCount++;

                    errors.Add(
                        ex.InnerException?.Message
                        ?? ex.Message);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return new BulkResult<int>(
            deletedIds.Count,
            failedCount,
            deletedIds,
            errors.Count > 0 ? errors : null);
    }
}
