using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.PropertyDetails;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class DataEntryService : BaseCommonCrudService<PropertyDetailsEntity, PropertyDetailsDto, CreatePropertyDetailsDto, UpdatePropertyDetailsDto, PropertyDetailsQueryParameters, int>, IDataEntryService
{
    // Injected child services — each owns its own entity's persistence logic
    private readonly IRenterDetailService _renterDetailService;
    private readonly IRenterMastService _renterMastService;
    private readonly IRoomWiseSubmissionDetailsService _roomWiseService;

    public DataEntryService(
        IRepository<PropertyDetailsEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IRenterDetailService renterDetailService,
        IRenterMastService renterMastService,
        IRoomWiseSubmissionDetailsService roomWiseService)
        : base(repository, unitOfWork, mapper)
    {
        _renterDetailService = renterDetailService;
        _renterMastService = renterMastService;
        _roomWiseService = roomWiseService;
    }

    // ────────────────────────────────────────────────────────────────
    // Single place that owns the include chain for this aggregate.
    // All read methods go through here — never call _dbSet directly.
    // Filters collection navigation properties to exclude soft-deleted records.
    // Note: Single reference properties (Floor, SubFloor, etc.) are included as-is;
    // they should be filtered at the database level via global query filters if needed.
    // ────────────────────────────────────────────────────────────────
    private IQueryable<PropertyDetailsEntity> QueryWithIncludes()
        => _repository.GetQueryable()
            .Where(x => x.IsActive && !x.MarkedForDeletion)                    // global soft-delete filter
            .Include(x => x.Floor)
            .Include(x => x.SubFloor)
            .Include(x => x.ConstructionType)
            .Include(x => x.TypeOfUse)
            .Include(x => x.SubTypeOfUse)
            .Include(x => x.RenterDetails.Where(r => r.IsActive && !r.MarkedForDeletion))
            .Include(x => x.Renters.Where(r => r.IsActive && !r.MarkedForDeletion))
             .Include(x => x.RoomWiseSubmissionDetails.Where(r => r.IsActive && !r.MarkedForDeletion))
                .ThenInclude(r => r.PropertyRoomMinus!.Where(rm => rm.IsActive && !rm.MarkedForDeletion))
            .Include(x => x.RoomWiseSubmissionDetails.Where(r => r.IsActive && !r.MarkedForDeletion))
                .ThenInclude(r => r.RoomTypeMaster);


    public override async Task<PagedResult<PropertyDetailsDto>> GetAllAsync( PropertyDetailsQueryParameters queryParameters, CancellationToken cancellationToken = default)
   {
        var query = QueryWithIncludes();

        if (queryParameters.PropertyId is > 0)
        {
            query = query.Where(x => x.PropertyId == queryParameters.PropertyId.Value);
        }

        query = query .ApplyFilters(queryParameters) .ApplySearch(queryParameters) .ApplySort(queryParameters);

         var totalCount = await query.CountAsync(cancellationToken);

        List<PropertyDetailsEntity> entities;
        int pageNumber;
        int pageSize;

        if (queryParameters.PageSize == -1)
        {
            entities = await query.ToListAsync(cancellationToken);

            pageNumber = 1;
            pageSize = totalCount == 0 ? 1 : totalCount;
        }
        else
        {
            entities = await query
                .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
                .Take(queryParameters.PageSize)
                .ToListAsync(cancellationToken);

            pageNumber = queryParameters.PageNumber;
            pageSize = queryParameters.PageSize;
        }

        var items = _mapper.Map<List<PropertyDetailsDto>>(entities);

        return new PagedResult<PropertyDetailsDto>(
            items,
            totalCount,
            pageNumber,
            pageSize);
    }
    // ────────────────────────────────────────────────────────────────
    // GET BY ID
    // ────────────────────────────────────────────────────────────────
    public override async Task<PropertyDetailsDto?> GetByIdAsync( int id, CancellationToken cancellationToken = default)
    {
        var entity = await QueryWithIncludes()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
         return entity is null ? null : _mapper.Map<PropertyDetailsDto>(entity);
    }

    // ────────────────────────────────────────────────────────────────
    // CREATE
    // Parent is saved first to get the generated Id, then children
    // are saved by their own services using that Id as foreign key.
    // All within the same UnitOfWork so one rollback covers everything.
    // ────────────────────────────────────────────────────────────────
    public override async Task<PropertyDetailsDto> CreateAsync( CreatePropertyDetailsDto createDto, CancellationToken cancellationToken = default)
    {
       
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Save the parent entity (no children yet)
            var entity = _mapper.Map<PropertyDetailsEntity>(createDto);
            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);   // gets entity.Id

            // 2. Create all child entities using the parent's Id
            await SaveNestedListsOnCreateAsync(entity.Id, createDto, cancellationToken);

            // 3. Commit transaction (includes implicit SaveChangesAsync for children)
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // 4. Reload with includes so returned DTO has all descriptions populated
            return await GetByIdAsync(entity.Id, cancellationToken) ?? _mapper.Map<PropertyDetailsDto>(entity);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────
    // UPDATE
    // Scalar fields are mapped onto the tracked entity.
     
    // All operations wrapped in transaction for aggregate consistency.
    // ────────────────────────────────────────────────────────────────
    public override async Task<PropertyDetailsDto?> UpdateAsync( int id, UpdatePropertyDetailsDto updateDto, CancellationToken cancellationToken = default)
    {
        // Load the tracked entity WITH children so EF knows what exists
        var entity = await QueryWithIncludes()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
            return null;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Map scalar fields onto the tracked entity
            _mapper.Map(updateDto, entity);
            await _repository.UpdateAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);   // flush parent changes

            // 2. Full-replace nested lists via child services
            //    isUpdate: true tells the helper to delete existing rows first
            await SaveNestedListsOnUpdateAsync(
                id,
                updateDto,
                cancellationToken);

            // 3. Commit transaction (includes SaveChangesAsync for all changes)
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // 4. Reload with includes so returned DTO has all descriptions populated
            return await GetByIdAsync(id, cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────
    // SOFT DELETE
    // Delegates to the repository's built-in IsActive flag logic.
    // Children are soft-deleted by their own services.
    // All operations wrapped in transaction for aggregate consistency.
    // ────────────────────────────────────────────────────────────────
    public override async Task<bool> DeleteAsync( int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetQueryable().FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        if (entity is null)
            return false;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Soft-delete parent via repository (sets IsActive = false internally)
            await _repository.DeleteAsync(id, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);   // flush parent changes

            // Soft-delete all children through their own services
            await _renterDetailService.DeleteByPropertyIdAsync(id, cancellationToken);
            await _renterMastService.DeleteByPropertyIdAsync(id, cancellationToken);
            await _roomWiseService.DeleteByPropertyIdAsync(id, cancellationToken);

            // Commit transaction (includes SaveChangesAsync for all changes)
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return true;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────────
    // PRIVATE HELPER — nested list orchestration
    // Keeps Create and Update free of list-management noise.
    // Child services are responsible for their own mapping and
    // repository calls. They must NOT call SaveChanges themselves.
    // ────────────────────────────────────────────────────────────────
    private async Task SaveNestedListsOnCreateAsync( int propertyDetailsId, CreatePropertyDetailsDto createDto, CancellationToken cancellationToken)
    {
        if (createDto.RenterDetails?.Any() == true)
            await _renterDetailService.CreateRangeAsync(propertyDetailsId, createDto.RenterDetails, cancellationToken);

        if (createDto.Renters?.Any() == true)
            await _renterMastService.CreateRangeAsync(propertyDetailsId, createDto.Renters, cancellationToken);

        if (createDto.RoomWiseSubmissionDetails?.Any() == true)
            await _roomWiseService.CreateRangeAsync(propertyDetailsId, createDto.RoomWiseSubmissionDetails, cancellationToken);
    }

    private async Task SaveNestedListsOnUpdateAsync(int propertyDetailsId,UpdatePropertyDetailsDto updateDto,CancellationToken cancellationToken)
    {
        // UpdateRangeAsync internally does delete-then-insert (full replace)
        // Only touch a collection if the caller actually sent it
        if (updateDto.RenterDetails is not null)
            await _renterDetailService.UpdateRangeAsync(propertyDetailsId, updateDto.RenterDetails, cancellationToken);

        if (updateDto.Renters is not null)
            await _renterMastService.UpdateRangeAsync(propertyDetailsId, updateDto.Renters, cancellationToken);

        if (updateDto.RoomWiseSubmissionDetails is not null)
            await _roomWiseService.UpdateRangeAsync(propertyDetailsId, updateDto.RoomWiseSubmissionDetails, cancellationToken);
    }

}