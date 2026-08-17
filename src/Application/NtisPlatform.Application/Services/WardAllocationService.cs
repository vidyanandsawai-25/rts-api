using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.DTOs.wardallocation;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Ward Allocation Service.
/// Uses generic repositories and keeps all ward-allocation business logic
/// inside the Application Service.
/// </summary>
public class WardAllocationService :
    BaseCommonCrudService<
        GlobalSurveyWardAllocationEntity,
        WardAllocationDto,
        CreateWardAllocationDto,
        UpdateWardAllocationDto,
        WardAllocationQueryParameters,
        int>,
    IWardAllocationService
{
    private readonly IRepository<UserEntity, int> _userRepository;

    private readonly IRepository<
        UserDepartmentAllocationEntity,
        int> _userDepartmentAllocationRepository;

    private readonly IRepository<
        UserModuleAllocationEntity,
        int> _userModuleAllocationRepository;

    private readonly IRepository<
        DepartmentMasterEntity,
        int> _departmentRepository;

    private readonly IRepository<
        ModuleMasterEntity,
        int> _moduleRepository;

    private readonly IRepository<ZoneEntity, int> _zoneRepository;
    private readonly IRepository<WardEntity, int> _wardRepository;
	private readonly IRepository<OldWardMasterEntity, int>_oldWardMasterRepository;

    public WardAllocationService(
        IRepository<GlobalSurveyWardAllocationEntity, int> repository,
        IRepository<UserEntity, int> userRepository,
        IRepository<UserDepartmentAllocationEntity, int>
            userDepartmentAllocationRepository,
        IRepository<UserModuleAllocationEntity, int>
            userModuleAllocationRepository,
        IRepository<DepartmentMasterEntity, int> departmentRepository,
        IRepository<ModuleMasterEntity, int> moduleRepository,
        IRepository<ZoneEntity, int> zoneRepository,
        IRepository<WardEntity, int> wardRepository,
		IRepository<OldWardMasterEntity, int> oldWardMasterRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
        _userRepository = userRepository;

        _userDepartmentAllocationRepository =
            userDepartmentAllocationRepository;

        _userModuleAllocationRepository =
            userModuleAllocationRepository;

        _departmentRepository = departmentRepository;
        _moduleRepository = moduleRepository;
        _zoneRepository = zoneRepository;
        _wardRepository = wardRepository;
		  _oldWardMasterRepository = oldWardMasterRepository;
    }

    #region Common CRUD

    public override async Task<WardAllocationDto?> GetByIdAsync(
     int id,
     CancellationToken cancellationToken = default)
    {
        var entity = await BuildAllocationEntityQuery()
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (entity == null)
        {
            return null;
        }

        var dto = _mapper.Map<WardAllocationDto>(entity);

        dto.OldWardId = entity.OldWardId;

        if (entity.OldWardId.HasValue)
        {
            var oldWard = await _oldWardMasterRepository
                .GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .FirstOrDefaultAsync(
                    x => x.Id == entity.OldWardId.Value,
                    cancellationToken);

            if (oldWard != null)
            {
                dto.OldWardNo = oldWard.OldWardNo;
                dto.OldZoneName = oldWard.OldZoneName;
            }
        }

        return dto;
    }

    public override async Task<PagedResult<WardAllocationDto>> GetAllAsync(
        WardAllocationQueryParameters queryParameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        var query = BuildAllocationEntityQuery();

        query = query.ApplyFilters(queryParameters);

        var searchKeyword = queryParameters.SearchTerm?
            .Trim()
            .ToLower();

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            query = query.Where(x =>
                (
                    x.User != null &&
                    (
                        (
                            x.User.UserName != null &&
                            x.User.UserName
                                .ToLower()
                                .Contains(searchKeyword)
                        ) ||
                        (
                            x.User.FirstName != null &&
                            x.User.FirstName
                                .ToLower()
                                .Contains(searchKeyword)
                        ) ||
                        (
                            x.User.LastName != null &&
                            x.User.LastName
                                .ToLower()
                                .Contains(searchKeyword)
                        ) ||
                        (
                            x.User.UserCode != null &&
                            x.User.UserCode
                                .ToLower()
                                .Contains(searchKeyword)
                        )
                    )
                ) ||
                (
                    x.Department != null &&
                    x.Department.DepartmentName != null &&
                    x.Department.DepartmentName
                        .ToLower()
                        .Contains(searchKeyword)
                ) ||
                (
                    x.Module != null &&
                    x.Module.ModuleName != null &&
                    x.Module.ModuleName
                        .ToLower()
                        .Contains(searchKeyword)
                ) ||
                (
                    x.Zone != null &&
                    x.Zone.ZoneNo != null &&
                    x.Zone.ZoneNo
                        .ToLower()
                        .Contains(searchKeyword)
                ) ||
                (
                    x.Ward != null &&
                    x.Ward.WardNo != null &&
                    x.Ward.WardNo
                        .ToLower()
                        .Contains(searchKeyword)
                ));
        }

        query = query.ApplySort(queryParameters);

        if (string.IsNullOrWhiteSpace(queryParameters.SortBy))
        {
            query = query.OrderBy(x => x.Id);
        }

        var totalCount = await query.CountAsync(
            cancellationToken);

        var entities = await query
            .Skip(
                (queryParameters.PageNumber - 1) *
                queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ToListAsync(cancellationToken);

        var items = _mapper.Map<List<WardAllocationDto>>(
            entities);

        var oldWardIds = entities
    .Where(x => x.OldWardId.HasValue)
    .Select(x => x.OldWardId!.Value)
    .Distinct()
    .ToList();

        var oldWardDictionary = oldWardIds.Count == 0
            ? new Dictionary<int, OldWardMasterEntity>()
            : await _oldWardMasterRepository
                .GetQueryable()
                .AsNoTracking()
                .Where(x => oldWardIds.Contains(x.Id) && x.IsActive)
                .ToDictionaryAsync(
                    x => x.Id,
                    cancellationToken);

        for (var index = 0; index < entities.Count; index++)
        {
            var entity = entities[index];
            var dto = items[index];

            dto.OldWardId = entity.OldWardId;

            if (entity.OldWardId.HasValue &&
                oldWardDictionary.TryGetValue(
                    entity.OldWardId.Value,
                    out var oldWard))
            {
                dto.OldWardNo = oldWard.OldWardNo;
                dto.OldZoneName = oldWard.OldZoneName;
            }
        }

        return new PagedResult<WardAllocationDto>(
            items,
            totalCount,
            queryParameters.PageNumber,
            queryParameters.PageSize);
    }

    public override async Task<WardAllocationDto> CreateAsync(
        CreateWardAllocationDto createDto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto);

        await ValidateUserDepartmentAndModuleAsync(
            createDto.UserId,
            createDto.DepartmentId,
            createDto.ModuleId,
            cancellationToken);

        var entity =
            _mapper.Map<GlobalSurveyWardAllocationEntity>(
                createDto);

        // Keep this assignment if DepartmentId is ignored by AutoMapper.
        entity.DepartmentId = createDto.DepartmentId;

        entity.CreatedDate ??= DateTime.Now;

        await _repository.AddAsync(
            entity,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return await GetByIdAsync(
                   entity.Id,
                   cancellationToken)
               ?? throw new InvalidOperationException(
                   "Ward allocation was created but could not be retrieved.");
    }

    public override async Task<WardAllocationDto?> UpdateAsync(
        int id,
        UpdateWardAllocationDto updateDto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateDto);

        var entity = await _repository.GetByIdAsync(
            id,
            cancellationToken);

        if (entity == null)
        {
            return null;
        }

        _mapper.Map(updateDto, entity);

        entity.UpdatedDate = DateTime.Now;

        await _repository.UpdateAsync(
            entity,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return await GetByIdAsync(
            id,
            cancellationToken);
    }

    #endregion

    public async Task<List<WardAllocationModuleDto>>
        GetModulesByUserIdAsync(
            int userId,
            CancellationToken cancellationToken = default)
    {
        return await (
            from userModuleAllocation in
                _userModuleAllocationRepository
                    .GetQueryable()
                    .AsNoTracking()

            join module in
                _moduleRepository
                    .GetQueryable()
                    .AsNoTracking()
                on userModuleAllocation.ModuleId equals module.Id

            join department in
                _departmentRepository
                    .GetQueryable()
                    .AsNoTracking()
                on userModuleAllocation.DepartmentId
                equals department.Id
                into departmentJoin

            from department in departmentJoin.DefaultIfEmpty()

            where
                userModuleAllocation.UserId == userId &&
                userModuleAllocation.IsActive &&
                module.IsActive

            orderby module.ModuleName

            select new WardAllocationModuleDto
            {
                ModuleId = module.Id,
                ModuleCode = module.ModuleCode,
                ModuleName = module.ModuleName,

                DepartmentId =
                    userModuleAllocation.DepartmentId,

                DepartmentCode = department != null
                    ? department.DepartmentCode
                    : null,

                DepartmentName = department != null
                    ? department.DepartmentName
                    : null
            })
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WardAllocationWardDto>>
        GetWardsByZoneIdAsync(
            int zoneId,
            CancellationToken cancellationToken = default)
    {
        return await _wardRepository.GetQueryable()
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                x.ZoneId == zoneId)
            .OrderBy(x => x.SequenceNo)
            .ThenBy(x => x.Description)
            .Select(x => new WardAllocationWardDto
            {
                WardId = x.Id,
                ZoneId = x.ZoneId,
                WardNo = x.WardNo,
                WardName = x.Description
            })
            .ToListAsync(cancellationToken);
    }


    #region Allocation Queries

    public async Task<List<WardAllocationDto>>
        GetByUserModuleZoneAsync(
            int userId,
            int moduleId,
            int zoneId,
            CancellationToken cancellationToken = default)
    {
        return await BuildWardAllocationDetailsQuery()
            .Where(x =>
                x.UserId == userId &&
                x.ModuleId == moduleId &&
                x.ZoneId == zoneId)
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.WardId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WardAllocationDto>>
        CreateFlexibleAsync(
            CreateFlexibleWardAllocationDto createDto,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto);

        var prepared =
            await PrepareAllocationRequestAsync(
                createDto.UserId,
                createDto.DepartmentId,
                createDto.ModuleId,
                createDto.Allocations,
                cancellationToken);

        var existingAllocations =
            await GetExistingAllocationsAsync(
                createDto.UserId,
                createDto.DepartmentId,
                createDto.ModuleId,
                prepared.ZoneIds,
                prepared.WardIds,
                cancellationToken);

        if (existingAllocations.Count > 0)
        {
            var duplicateMessages = existingAllocations
                .Select(x =>
                    $"Zone {x.ZoneId}, Ward {x.WardId}");

            throw new InvalidOperationException(
                "User is already allocated to the following: " +
                string.Join("; ", duplicateMessages));
        }

        var now = DateTime.Now;

        var entities = prepared.Pairs
            .Select(pair =>
                CreateAllocationEntity(
                    createDto.UserId,
                    createDto.DepartmentId,
                    createDto.ModuleId,
                    pair,
                    createDto.IsActive,
                    createDto.CreatedBy,
                    now))
            .ToList();

        await ExecuteInTransactionAsync(
            async () =>
            {
                foreach (var entity in entities)
                {
                    await _repository.AddAsync(
                        entity,
                        cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);
            },
            cancellationToken);

        var allocationIds = entities
            .Select(x => x.Id)
            .ToList();

        return await BuildWardAllocationDetailsQuery()
            .Where(x => allocationIds.Contains(x.Id))
            .OrderBy(x => x.ZoneId)
            .ThenBy(x => x.WardId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WardAllocationDto>>
        ReplaceAllocationsAsync(
            int userId,
            int moduleId,
            UpdateFlexibleWardAllocationDto updateDto,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateDto);

        if (updateDto.UserId != userId ||
            updateDto.ModuleId != moduleId)
        {
            throw new ArgumentException(
                "UserId and ModuleId in request body must match " +
                "the route parameters.");
        }

        var prepared =
            await PrepareAllocationRequestAsync(
                updateDto.UserId,
                updateDto.DepartmentId,
                updateDto.ModuleId,
                updateDto.Allocations,
                cancellationToken);

        var existingAllocations =
            await _repository.GetQueryable()
                .Where(x =>
                    x.UserId == userId &&
                    x.DepartmentId == updateDto.DepartmentId &&
                    x.ModuleId == moduleId)
                .ToListAsync(cancellationToken);

        var activeAllocations = existingAllocations
            .Where(x => x.IsActive)
            .ToList();

        /*
         * Create a lookup once instead of repeatedly searching
         * allExistingAllocations for every requested ward.
         */
        var historyLookup = existingAllocations
            .GroupBy(x => new
            {
                x.ZoneId,
                x.WardId
            })
            .ToDictionary(
                group => (
                    group.Key.ZoneId,
                    group.Key.WardId),
                group => group
                    .OrderByDescending(x => x.Id)
                    .First());

        var now = DateTime.Now;

        var newEntities = prepared.Pairs
            .Select(pair =>
            {
                historyLookup.TryGetValue(
                    (pair.ZoneId, pair.WardId),
                    out var history);

                var entity = CreateAllocationEntity(
                    userId,
                    updateDto.DepartmentId,
                    moduleId,
                    pair,
                    true,
                    history?.CreatedBy ??
                    updateDto.UpdatedBy,
                    history?.CreatedDate ?? now);

                if (history != null)
                {
                    entity.UpdatedBy =
                        updateDto.UpdatedBy;

                    entity.UpdatedDate = now;
                }

                return entity;
            })
            .ToList();

        await ExecuteInTransactionAsync(
            async () =>
            {
                /*
                 * Save deactivation before inserting new active rows.
                 * This is important if the database has a unique
                 * constraint or filtered index for active allocations.
                 */
                foreach (var existing in activeAllocations)
                {
                    existing.IsActive = false;
                    existing.UpdatedBy =
                        updateDto.UpdatedBy;
                    existing.UpdatedDate = now;

                    await _repository.UpdateAsync(
                        existing,
                        cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);

                foreach (var entity in newEntities)
                {
                    await _repository.AddAsync(
                        entity,
                        cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);
            },
            cancellationToken);

        /*
         * Return only currently active allocations.
         * Inactive historical rows are not included in the result count.
         */
        return await BuildWardAllocationDetailsQuery()
            .Where(x =>
                x.UserId == userId &&
                x.DepartmentId ==
                    updateDto.DepartmentId &&
                x.ModuleId == moduleId &&
                x.IsActive)
            .OrderBy(x => x.ZoneId)
            .ThenBy(x => x.WardId)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Allocated Zone and Ward APIs

    public async Task<List<UserAllocatedZoneWardDto>>
     GetAllocatedZonesAndWardsByUserIdAsync(
         int userId,
         CancellationToken cancellationToken = default)
    {
        var allocations = await (
            from allocation in
                _repository.GetQueryable().AsNoTracking()

            join zone in
                _zoneRepository.GetQueryable().AsNoTracking()
                on allocation.ZoneId equals zone.Id
                into zoneJoin

            from zone in zoneJoin.DefaultIfEmpty()

            join ward in
                _wardRepository.GetQueryable().AsNoTracking()
                on allocation.WardId equals ward.Id
                into wardJoin

            from ward in wardJoin.DefaultIfEmpty()

            where allocation.UserId == userId &&
                  allocation.IsActive

            select new
            {
                allocation.ZoneId,

                ZoneNo = zone != null
                    ? zone.ZoneNo
                    : null,

                ZoneName = zone != null
                    ? zone.Description
                    : null,

                allocation.WardId,

                WardNo = ward != null
                    ? ward.WardNo
                    : null
            })
            .Distinct()
            .OrderBy(x => x.ZoneId)
            .ThenBy(x => x.WardNo)
            .ToListAsync(cancellationToken);

        return allocations
            .GroupBy(x => new
            {
                x.ZoneId,
                x.ZoneNo,
                x.ZoneName
            })
            .Select(group =>
                new UserAllocatedZoneWardDto
                {
                    ZoneId = group.Key.ZoneId,
                    ZoneNo = group.Key.ZoneNo,
                    ZoneName = group.Key.ZoneName,

                    Wards = group
                        .GroupBy(x => new
                        {
                            x.WardId,
                            x.WardNo
                        })
                        .Select(wardGroup =>
                            new UserAllocatedWardDto
                            {
                                WardId = wardGroup.Key.WardId,
                                WardNo = wardGroup.Key.WardNo
                            })
                        .OrderBy(x => x.WardNo)
                        .ToList()
                })
            .OrderBy(x => x.ZoneNo)
            .ToList();
    }
    public Task<List<AllocatedZoneByUserDto>>
        GetAllocatedZonesByUserIdAsync(
            int userId,
            CancellationToken cancellationToken = default)
    {
        return (
            from allocation in
                _repository.GetQueryable().AsNoTracking()

            join module in
                _moduleRepository.GetQueryable().AsNoTracking()
                on allocation.ModuleId equals module.Id
                into moduleJoin

            from module in moduleJoin.DefaultIfEmpty()

            join zone in
                _zoneRepository.GetQueryable().AsNoTracking()
                on allocation.ZoneId equals zone.Id
                into zoneJoin

            from zone in zoneJoin.DefaultIfEmpty()

            where allocation.UserId == userId &&
                  allocation.IsActive

            select new AllocatedZoneByUserDto
            {
                ModuleId = allocation.ModuleId,

                ModuleName = module != null
                    ? module.ModuleName
                    : null,

                ZoneId = allocation.ZoneId,

                ZoneNo = zone != null
                    ? zone.ZoneNo
                    : null,

                ZoneName = zone != null
                    ? zone.Description
                    : null
            })
            .Distinct()
            .OrderBy(x => x.ModuleName)
            .ThenBy(x => x.ZoneNo)
            .ToListAsync(cancellationToken);
    }

    public Task<List<AllocatedWardByUserDto>>
    GetAllocatedWardsByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return (
            from allocation in
                _repository.GetQueryable().AsNoTracking()

            join module in
                _moduleRepository.GetQueryable().AsNoTracking()
                on allocation.ModuleId equals module.Id
                into moduleJoin

            from module in moduleJoin.DefaultIfEmpty()

            join ward in
                _wardRepository.GetQueryable().AsNoTracking()
                on allocation.WardId equals ward.Id
                into wardJoin

            from ward in wardJoin.DefaultIfEmpty()

            join oldWard in
            _oldWardMasterRepository
                .GetQueryable()
                .AsNoTracking()
            on allocation.OldWardId equals oldWard.Id
            into oldWardJoin

            from oldWard in oldWardJoin.DefaultIfEmpty()

            where allocation.UserId == userId &&
                  allocation.IsActive

            select new AllocatedWardByUserDto
            {
                ModuleId = allocation.ModuleId,

                ModuleName = module != null
                    ? module.ModuleName
                    : null,

                ZoneId = allocation.ZoneId,
                WardId = allocation.WardId,

                WardNo = ward != null
                    ? ward.WardNo
                    : null,

                OldWardId = allocation.OldWardId,

                OldWardNo = oldWard != null
                ? oldWard.OldWardNo
                : null,

                            OldZoneName = oldWard != null
                ? oldWard.OldZoneName
                : null
            })
            .Distinct()
            .OrderBy(x => x.ModuleId)
            .ThenBy(x => x.ZoneId)
            .ThenBy(x => x.WardNo)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsUserDeallocatedAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetQueryable()
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.MarkedForDeletion)
            .FirstOrDefaultAsync(cancellationToken);
    }

    #endregion

    #region Shared Query Builders

    /// <summary>
    /// Builds the common entity query with all navigation properties.
    /// </summary>
    private IQueryable<GlobalSurveyWardAllocationEntity>
        BuildAllocationEntityQuery()
    {
        return _repository.GetQueryable()
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.Department)
            .Include(x => x.Module)
            .Include(x => x.Zone)
            .Include(x => x.Ward);
    }
    /// <summary>
    /// Builds the detailed ward-allocation projection.
    /// </summary>
    private IQueryable<WardAllocationDto>
        BuildWardAllocationDetailsQuery()
    {
        return
            from allocation in
                _repository.GetQueryable().AsNoTracking()

            join user in
                _userRepository
                    .GetQueryable()
                    .AsNoTracking()
                on allocation.UserId equals user.Id
                into userJoin

            from user in userJoin.DefaultIfEmpty()

            join department in
                _departmentRepository
                    .GetQueryable()
                    .AsNoTracking()
                on allocation.DepartmentId equals department.Id
                into departmentJoin

            from department in departmentJoin.DefaultIfEmpty()

            join module in
                _moduleRepository
                    .GetQueryable()
                    .AsNoTracking()
                on allocation.ModuleId equals module.Id
                into moduleJoin

            from module in moduleJoin.DefaultIfEmpty()

            join zone in
                _zoneRepository
                    .GetQueryable()
                    .AsNoTracking()
                on allocation.ZoneId equals zone.Id
                into zoneJoin

            from zone in zoneJoin.DefaultIfEmpty()

            join ward in
                _wardRepository
                    .GetQueryable()
                    .AsNoTracking()
                on allocation.WardId equals ward.Id
                into wardJoin
            from ward in wardJoin.DefaultIfEmpty()

            join oldWard in
                _oldWardMasterRepository
                    .GetQueryable()
                    .AsNoTracking()
                on allocation.OldWardId equals oldWard.Id
                into oldWardJoin
            from oldWard in oldWardJoin.DefaultIfEmpty()
            select new WardAllocationDto
            {
                Id = allocation.Id,
                UserId = allocation.UserId,
                EmployeeName = user != null
                    ? (
                        (
                            (user.FirstName ?? string.Empty) +
                            (
                                string.IsNullOrWhiteSpace(
                                    user.MiddleName)
                                    ? string.Empty
                                    : " " + user.MiddleName
                            ) +
                            (
                                string.IsNullOrWhiteSpace(
                                    user.LastName)
                                    ? string.Empty
                                    : " " + user.LastName
                            )
                        ).Trim().Length > 0
                            ? (
                                (user.FirstName ??
                                 string.Empty) +
                                (
                                    string.IsNullOrWhiteSpace(
                                        user.MiddleName)
                                        ? string.Empty
                                        : " " + user.MiddleName
                                ) +
                                (
                                    string.IsNullOrWhiteSpace(
                                        user.LastName)
                                        ? string.Empty
                                        : " " + user.LastName
                                )
                            ).Trim()
                            : user.UserName
                    )
                    : null,

                EmpCode = user != null
                    ? user.UserCode
                    : null,

                DepartmentId =
                    allocation.DepartmentId,

                DepartmentName = department != null
                    ? department.DepartmentName
                    : null,

                ModuleId = allocation.ModuleId,

                ModuleName = module != null
                    ? module.ModuleName
                    : null,

                ZoneId = allocation.ZoneId,

                ZoneNo = zone != null
                    ? zone.ZoneNo
                    : null,

                WardId = allocation.WardId,

                WardNo = ward != null
                    ? ward.WardNo
                    : null,

                OldWardId = allocation.OldWardId,

                OldWardNo = oldWard != null
                ? oldWard.OldWardNo
                : null,

                            OldZoneName = oldWard != null
                ? oldWard.OldZoneName
                : null,

                IsActive = allocation.IsActive,

                CreatedDate = allocation.CreatedDate,

                UpdatedDate = allocation.UpdatedDate
            };
    }

    #endregion

    #region Allocation Preparation

    /// <summary>
    /// Performs the shared validation and flattening required by both
    /// CreateFlexibleAsync and ReplaceAllocationsAsync.
    /// </summary>
    private async Task<(
    List<(int ZoneId, int WardId,int? OldWardId)> Pairs,
    List<int> ZoneIds,
    List<int> WardIds)>
    PrepareAllocationRequestAsync(
        int userId,
        int departmentId,
        int moduleId,
        IReadOnlyCollection<ZoneWardAllocationDto>? allocations,
        CancellationToken cancellationToken)
    {
        await ValidateUserDepartmentAndModuleAsync(
            userId,
            departmentId,
            moduleId,
            cancellationToken);

        ValidateAllocationsRequest(allocations);

        var validAllocations = allocations!;

        var pairs = validAllocations
            .SelectMany(allocation =>
                allocation.WardIds
                    .Distinct()
                    .Select(wardId => (
                        ZoneId: allocation.ZoneId,
                        WardId: wardId,
                        OldWardId: allocation.OldWardId)))
            .Distinct()
            .ToList();

        var zoneIds = pairs
            .Select(x => x.ZoneId)
            .Distinct()
            .ToList();

        var wardIds = pairs
            .Select(x => x.WardId)
            .Distinct()
            .ToList();

        var validWardDictionary =
            await GetValidWardZoneRelationshipsAsync(
                zoneIds,
                wardIds,
                cancellationToken);

        ValidateWardZoneRelationships(
            validAllocations,
            validWardDictionary);

        await ValidateOldWardIdsAsync(
        validAllocations,
        cancellationToken);

        return (
            Pairs: pairs,
            ZoneIds: zoneIds,
            WardIds: wardIds);
    }

    private async Task ValidateOldWardIdsAsync(
    IEnumerable<ZoneWardAllocationDto> allocations,
    CancellationToken cancellationToken)
    {
        var oldWardIds = allocations
            .Where(x => x.OldWardId.HasValue)
            .Select(x => x.OldWardId!.Value)
            .Distinct()
            .ToList();

        if (oldWardIds.Count == 0)
        {
            return;
        }

        var validOldWardIds = await _oldWardMasterRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(x =>
                oldWardIds.Contains(x.Id) &&
                x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var invalidOldWardIds = oldWardIds
            .Except(validOldWardIds)
            .ToList();

        if (invalidOldWardIds.Count > 0)
        {
            throw new ArgumentException(
                "Invalid or inactive OldWardId(s): " +
                string.Join(", ", invalidOldWardIds));
        }
    }

    /// <summary>
    /// Creates a ward-allocation entity using common properties.
    /// </summary>
    private static GlobalSurveyWardAllocationEntity
     CreateAllocationEntity(
         int userId,
         int departmentId,
         int moduleId,
         (int ZoneId, int WardId,int? OldWardId) pair,
         bool isActive,
         int? createdBy,
         DateTime createdDate)
    {
        return new GlobalSurveyWardAllocationEntity
        {
            UserId = userId,
            DepartmentId = departmentId,
            ModuleId = moduleId,

            ZoneId = pair.ZoneId,
            WardId = pair.WardId,
            OldWardId = pair.OldWardId,
            IsActive = isActive,

            CreatedBy = createdBy,
            CreatedDate = createdDate,

            UpdatedBy = null,
            UpdatedDate = null
        };
    }

    #endregion

    #region Allocated Zone and Ward APIs
    public async Task<List<OldWardByWardDto>>
    GetOldWardsByWardIdAsync(
        int wardId,
        CancellationToken cancellationToken = default)
    {
        if (wardId <= 0)
        {
            throw new ArgumentException(
                "WardId must be greater than zero.",
                nameof(wardId));
        }

        var oldWardIds = await _repository
            .GetQueryable()
            .AsNoTracking()
            .Where(x =>
                x.WardId == wardId &&
                x.IsActive &&
                x.OldWardId.HasValue)
            .Select(x => x.OldWardId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (oldWardIds.Count == 0)
        {
            return new List<OldWardByWardDto>();
        }

        return await _oldWardMasterRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(x =>
                oldWardIds.Contains(x.Id) &&
                x.IsActive)
            .OrderBy(x => x.OldWardNo)
            .Select(x => new OldWardByWardDto
            {
                OldWardId = x.Id,
                OldWardNo = x.OldWardNo,
                OldZoneName = x.OldZoneName
            })
            .ToListAsync(cancellationToken);
    }

    #endregion
    #region Validation

    private async Task ValidateUserDepartmentAndModuleAsync(
        int userId,
        int departmentId,
        int moduleId,
        CancellationToken cancellationToken)
    {
        var userExists =
            await _userRepository.GetQueryable()
                .AsNoTracking()
                .AnyAsync(
                    x => x.Id == userId,
                    cancellationToken);

        if (!userExists)
        {
            throw new ArgumentException(
                $"User {userId} not found.");
        }

        var departmentExists =
            await _departmentRepository.GetQueryable()
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.Id == departmentId &&
                        x.IsActive,
                    cancellationToken);

        if (!departmentExists)
        {
            throw new ArgumentException(
                $"Department {departmentId} not found or inactive.");
        }

        var moduleExists =
            await _moduleRepository.GetQueryable()
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.Id == moduleId &&
                        x.IsActive,
                    cancellationToken);

        if (!moduleExists)
        {
            throw new ArgumentException(
                $"Module {moduleId} not found or inactive.");
        }

        var hasDepartmentAllocation =
            await _userDepartmentAllocationRepository
                .GetQueryable()
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.UserId == userId &&
                        x.DepartmentId == departmentId &&
                        x.IsActive,
                    cancellationToken);

        if (!hasDepartmentAllocation)
        {
            throw new ArgumentException(
                $"User {userId} is not allocated to " +
                $"Department {departmentId}.");
        }

        var hasModuleAllocation =
            await _userModuleAllocationRepository
                .GetQueryable()
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.UserId == userId &&
                        x.DepartmentId == departmentId &&
                        x.ModuleId == moduleId &&
                        x.IsActive,
                    cancellationToken);

        if (!hasModuleAllocation)
        {
            throw new ArgumentException(
                $"User {userId} is not allocated to Module " +
                $"{moduleId} in Department {departmentId}.");
        }
    }

    private static void ValidateAllocationsRequest(
        IReadOnlyCollection<ZoneWardAllocationDto>?
            allocations)
    {
        if (allocations == null ||
            allocations.Count == 0)
        {
            throw new ArgumentException(
                "At least one zone allocation is required.");
        }

        foreach (var allocation in allocations)
        {
            if (allocation.ZoneId <= 0)
            {
                throw new ArgumentException(
                    "A valid ZoneId is required.");
            }

            if (allocation.WardIds == null ||
                allocation.WardIds.Count == 0)
            {
                throw new ArgumentException(
                    $"At least one ward is required for zone " +
                    $"{allocation.ZoneId}.");
            }

            if (allocation.WardIds.Any(x => x <= 0))
            {
                throw new ArgumentException(
                    $"One or more invalid ward IDs were supplied " +
                    $"for zone {allocation.ZoneId}.");
            }
        }
    }

    private static void ValidateWardZoneRelationships(
        IEnumerable<ZoneWardAllocationDto> allocations,
        IReadOnlyDictionary<int, int>
            validWardDictionary)
    {
        foreach (var allocation in allocations)
        {
            var invalidWardIds = allocation.WardIds
                .Distinct()
                .Where(wardId =>
                    !validWardDictionary.TryGetValue(
                        wardId,
                        out var actualZoneId) ||
                    actualZoneId != allocation.ZoneId)
                .ToList();

            if (invalidWardIds.Count > 0)
            {
                throw new ArgumentException(
                    $"The following ward IDs do not belong to " +
                    $"zone {allocation.ZoneId}: " +
                    $"{string.Join(", ", invalidWardIds)}.");
            }
        }
    }

    #endregion

    #region Database Helpers

    private async Task<Dictionary<int, int>>
        GetValidWardZoneRelationshipsAsync(
            IReadOnlyCollection<int> zoneIds,
            IReadOnlyCollection<int> wardIds,
            CancellationToken cancellationToken)
    {
        var validWards =
            await _wardRepository.GetQueryable()
                .AsNoTracking()
                .Where(ward =>
                    zoneIds.Contains(ward.ZoneId) &&
                    wardIds.Contains(ward.Id) &&
                    ward.IsActive)
                .Select(ward => new
                {
                    WardId = ward.Id,
                    ward.ZoneId
                })
                .ToListAsync(cancellationToken);

        return validWards.ToDictionary(
            ward => ward.WardId,
            ward => ward.ZoneId);
    }

    private async Task<List<(int ZoneId, int WardId)>>
    GetExistingAllocationsAsync(
        int userId,
        int departmentId,
        int moduleId,
        IReadOnlyCollection<int> zoneIds,
        IReadOnlyCollection<int> wardIds,
        CancellationToken cancellationToken)
    {
        var existingAllocations =
            await _repository.GetQueryable()
                .AsNoTracking()
                .Where(allocation =>
                    allocation.UserId == userId &&
                    allocation.DepartmentId == departmentId &&
                    allocation.ModuleId == moduleId &&
                    zoneIds.Contains(allocation.ZoneId) &&
                    wardIds.Contains(allocation.WardId) &&
                    allocation.IsActive)
                .Select(allocation => new
                {
                    allocation.ZoneId,
                    allocation.WardId
                })
                .Distinct()
                .ToListAsync(cancellationToken);

        return existingAllocations
            .Select(x => (
                ZoneId: x.ZoneId,
                WardId: x.WardId))
            .ToList();
    }

    /// <summary>
    /// Executes an operation inside a database transaction.
    /// SaveChanges is controlled by the supplied operation so that
    /// intermediate saves can be performed when required.
    /// </summary>
    private async Task ExecuteInTransactionAsync(
        Func<Task> operation,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(
            cancellationToken);

        try
        {
            await operation();

            await _unitOfWork.CommitTransactionAsync(
                cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(
                cancellationToken);

            throw;
        }
    }

    #endregion
}