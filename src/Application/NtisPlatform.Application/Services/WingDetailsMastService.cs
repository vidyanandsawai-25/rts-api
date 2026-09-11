using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.PropertyPhoto;
using NtisPlatform.Application.DTOs.WingDetailsMast;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
namespace NtisPlatform.Application.Services;

public class WingDetailsMastService : BaseCommonCrudService<WingDetailsMastEntity, WingDetailsMastDto, CreateWingDetailsMastDto, UpdateWingDetailsMastDto, WingDetailsMastQueryParameters, int>, IWingDetailsMastService
{
    private readonly IReferenceValidationService _referenceValidator;
    private readonly IRepository<SocietyDetailsEntity, int> _societyRepository;
    private readonly IRepository<WingEntity, int> _wingRepository;
    private readonly IRepository<PropertyMapDetailEntity, int> _propertyMapDetailRepository;
    private readonly IRepository<PropertyMastOldEntity, int> _propertyMastOldRepository;
    private readonly IRepository<PropertyPhotoEntity, int> _propertyPhotoRepository;

    public WingDetailsMastService(
        IRepository<WingDetailsMastEntity, int> repository,
        IRepository<SocietyDetailsEntity, int> societyRepository,
        IRepository<WingEntity, int> wingRepository,
        IRepository<PropertyMapDetailEntity, int> propertyMapDetailRepository,
        IRepository<PropertyMastOldEntity, int> propertyMastOldRepository,
        IRepository<PropertyPhotoEntity, int> propertyPhotoRepository,
        IReferenceValidationService referenceValidator,
        IUnitOfWork unitOfWork,
        IMapper mapper)
        : base(repository, unitOfWork, mapper)
    {
        _societyRepository = societyRepository;
        _wingRepository = wingRepository;
        _propertyMapDetailRepository = propertyMapDetailRepository;
        _propertyMastOldRepository = propertyMastOldRepository;
        _propertyPhotoRepository = propertyPhotoRepository;
        _referenceValidator = referenceValidator;
    }

    // ──────────────────────────────────────────────────────────────────────────────────────────────────
    // Single place that owns the include chain for this aggregate.
    // All read methods go through here — never call _repository directly.
    // Filters out soft-deleted records (MarkedForDeletion).
    // ──────────────────────────────────────────────────────────────────────────────────────────────────
    private IQueryable<WingDetailsMastEntity> QueryWithIncludes()
    {
        return _repository.GetQueryable()
            .AsNoTracking()
            .Where(x => x.IsActive && !x.MarkedForDeletion)
            .Include(x => x.SocietyDetailsMast)
            .Include(x => x.WingMaster)
            .Include(x => x.ManagerMobileNoRemarkMaster)
            .Include(x => x.SecretaryMobileNoRemarkMaster)
            .Include(x => x.SocietyWingDetails);
    }

    // ──────────────────────────────────────────────────────────────────────────────────────────────────
    // GET ALL (paged)
    // ──────────────────────────────────────────────────────────────────────────────────────────────────
    public override async Task<PagedResult<WingDetailsMastDto>> GetAllAsync(WingDetailsMastQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        var query = QueryWithIncludes();

        if (queryParameters.SocietyDetailsMastId is > 0)
        {
            query = query.Where(x => x.SocietyDetailsMastId == queryParameters.SocietyDetailsMastId.Value);
        }

        if (queryParameters.PropertyId is > 0)
        {
            query = query.Where(x => x.SocietyDetailsMast != null && x.SocietyDetailsMast.PropertyId == queryParameters.PropertyId.Value);
        }

        query = query
            .ApplyFilters(queryParameters)
            .ApplySearch(queryParameters)
            .ApplySort(queryParameters);

        var totalCount = await query.CountAsync(cancellationToken);

        List<WingDetailsMastEntity> entities;
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

        var items = _mapper.Map<List<WingDetailsMastDto>>(entities);

        //── Fetch photos separately to avoid collection-include pagination issues ──────────

        var wingDetailIds = entities.Select(x => x.Id).ToList();

        var allPhotos = wingDetailIds.Count > 0 ? await _propertyPhotoRepository.GetQueryable().AsNoTracking()
                .Where(p =>
                        p.WingDetailId.HasValue &&
                        wingDetailIds.Contains(p.WingDetailId.Value) &&
                        p.IsActive &&
                        !p.MarkedForDeletion &&
                        p.IsLatest &&
                        p.DocumentBinding != null &&
                        p.DocumentBinding.Document != null &&
                        p.DocumentBinding.IsActive &&
                        !p.DocumentBinding.MarkedForDeletion)
                .Include(p => p.PhotoType)
                .Include(p => p.DocumentBinding)
                    .ThenInclude(db => db!.Document)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync(cancellationToken) : new List<PropertyPhotoEntity>();

        // Group photos by WingDetailId for O(1) lookup
        var photosByWingId = allPhotos
            .GroupBy(p => p.WingDetailId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var dto in items)
        {
            if (dto.SocietyWingDetails == null)
                continue;

            if (photosByWingId.TryGetValue(dto.Id, out var wingPhotos))
            {
                dto.SocietyWingDetails.WingPhotos = _mapper.Map<List<PropertyPhotoUploadResponseDto>>(
                    wingPhotos
                        .Where(x => x.PhotoType != null &&
                            (
                                x.PhotoType.PhotoTypeCode == "WP" ||
                                x.PhotoType.PhotoTypeCode == "WING_BUILDING" ||
                                x.PhotoType.PhotoTypeCode == "WING_PLACE"
                            )));

                dto.SocietyWingDetails.BoardPhotos = _mapper.Map<List<PropertyPhotoUploadResponseDto>>(
                    wingPhotos
                        .Where(x => x.PhotoType != null &&
                            (
                                x.PhotoType.PhotoTypeCode == "BP" ||
                                x.PhotoType.PhotoTypeCode == "WING_BOARD"
                            )));
            }
            else
            {
                dto.SocietyWingDetails.WingPhotos = new List<PropertyPhotoUploadResponseDto>();
                dto.SocietyWingDetails.BoardPhotos = new List<PropertyPhotoUploadResponseDto>();
            }
        }

        return new PagedResult<WingDetailsMastDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<WingDetailsMastResponseDto> GetAllWithOldDetailsAsync(WingDetailsMastQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        var pagedResult = await GetAllAsync(queryParameters, cancellationToken);

        //── Fetch OldWingDetails for the properties in the paged result ─────────────────────────

        var oldWingDetails = new List<OldWingDetailsDto>();

            var distinctPropertyIds = queryParameters.PropertyId.HasValue
            ? new List<int> { queryParameters.PropertyId.Value }
            : pagedResult.Items
                .Where(e => e.SocietyWingDetails != null && e.SocietyWingDetails.PropertyId.HasValue)
                .Select(e => e.SocietyWingDetails!.PropertyId!.Value)
                .Distinct()
                .ToList();

        if (distinctPropertyIds.Count > 0)
        {
            var pmdQuery = _propertyMapDetailRepository.GetQueryable().AsNoTracking();
            var pmoQuery = _propertyMastOldRepository.GetQueryable().AsNoTracking();

            var societyPropertiesQuery =
                from pmo in pmoQuery
                where (from pmd in pmdQuery
                       join pmo2 in pmoQuery on pmd.PropertyIdOld equals pmo2.Id
                       where pmd.PropertyIdNew.HasValue && distinctPropertyIds.Contains(pmd.PropertyIdNew.Value)
                          && pmd.Status == "DRAFT"
                          && pmd.IsActive
                          && pmo2.OldSocietyName == pmo.OldSocietyName
                          && pmo2.OldWardNo == pmo.OldWardNo
                       select 1).Any()
                select new
                {
                    pmo.OldSocietyName,
                    pmo.OldWardNo,
                    pmo.OldAddress,
                    pmo.OldWing,
                    pmo.OldFlatOrShopNumber,
                    pmo.OldFloor
                };

            var societyPropertiesRaw = await societyPropertiesQuery.ToListAsync(cancellationToken);

            oldWingDetails = societyPropertiesRaw
                .GroupBy(x => new { x.OldSocietyName, x.OldWardNo })
                .Select(g => new OldWingDetailsDto
                {
                    OldSocietyName = g.Key.OldSocietyName,
                    OldWardNo = g.Key.OldWardNo,
                    OldAddress = g.Max(x => x.OldAddress),
                    OldWingNo = g.Where(x => !string.IsNullOrWhiteSpace(x.OldWing))
                                 .Select(x => x.OldWing!.Trim())
                                 .Distinct()
                                 .ToList(),
                    FlatShopCount = g.Where(x => x.OldFlatOrShopNumber != null)
                                     .Select(x => x.OldFlatOrShopNumber)
                                     .Distinct()
                                     .Count(),
                    OldFloorCount = g.Where(x => !string.IsNullOrWhiteSpace(x.OldFloor))
                                     .Select(x => x.OldFloor)
                                     .Distinct()
                                     .Count()
                })
                .ToList();
        }

        return new WingDetailsMastResponseDto
        {
            WingDetailsMast = pagedResult.Items.ToList(),
            TotalCount = pagedResult.TotalCount,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalPages = pagedResult.TotalPages,
            HasPrevious = pagedResult.HasPrevious,
            HasNext = pagedResult.HasNext,
            OldWingDetails = oldWingDetails
        };
    }

    // ──────────────────────────────────────────────────────────────────────────────────────────────────
    // GET BY ID
    // ──────────────────────────────────────────────────────────────────────────────────────────────────
    public override async Task<WingDetailsMastDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await QueryWithIncludes().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
            return null;

        return _mapper.Map<WingDetailsMastDto>(entity);
    }

    // ──────────────────────────────────────────────────────────────────────────────────────────────────
    // CREATE
    // ──────────────────────────────────────────────────────────────────────────────────────────────────
    public override async Task<WingDetailsMastDto> CreateAsync(CreateWingDetailsMastDto createDto, CancellationToken cancellationToken = default)
    {
        if (createDto.SocietyWingDetails == null)
            throw new ValidationException("Society wing details are required.", OperationType.Create);

        if (createDto.WingMasterId != createDto.SocietyWingDetails.WingId ||
            createDto.SocietyDetailsMastId != createDto.SocietyWingDetails.SocietyDetailId)
        {
            throw new ValidationException("Wing or Society details do not match.", OperationType.Create);
        }

        // Validate Society
        if (!await _societyRepository.GetQueryable()
            .AnyAsync(x => x.Id == createDto.SocietyDetailsMastId, cancellationToken))
        {
            throw new ValidationException("The specified society does not exist.", OperationType.Create);
        }

        // Validate Wing
        if (!await _wingRepository.GetQueryable()
            .AnyAsync(x => x.Id == createDto.WingMasterId, cancellationToken))
        {
            throw new ValidationException("The specified wing does not exist.", OperationType.Create);
        }

        // Check Duplicate Wing
        if (await _repository.GetQueryable()
            .AnyAsync(x =>
                x.SocietyDetailsMastId == createDto.SocietyDetailsMastId &&
                x.WingMasterId == createDto.WingMasterId &&
                x.IsActive &&
                !x.MarkedForDeletion,
                cancellationToken))
        {
            throw new ValidationException("The selected wing already exists for the specified society.", OperationType.Create);
        }

        // Validate Floor Range
        if (!string.IsNullOrWhiteSpace(createDto.SocietyWingDetails.FromFloor) &&
            !string.IsNullOrWhiteSpace(createDto.SocietyWingDetails.ToFloor))
        {
            bool isFromFloorValid = int.TryParse(createDto.SocietyWingDetails.FromFloor, out int fromFloor);
            bool isToFloorValid = int.TryParse(createDto.SocietyWingDetails.ToFloor, out int toFloor);

            if (!isFromFloorValid || !isToFloorValid)
            {
                throw new ValidationException(
                    "Both FromFloor and ToFloor must contain valid numeric values.",
                    OperationType.Create);
            }

            if (fromFloor > toFloor)
            {
                throw new ValidationException(
                    "FromFloor cannot be greater than ToFloor.",
                    OperationType.Create);
            }
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var entity = _mapper.Map<WingDetailsMastEntity>(createDto);

            entity.CreatedDate = DateTime.Now;

            if (entity.SocietyWingDetails != null)
            {
                entity.SocietyWingDetails.WingDetailsMastId = 0;
                entity.SocietyWingDetails.CreatedDate = DateTime.Now;
                entity.SocietyWingDetails.WingId = entity.WingMasterId;
                entity.SocietyWingDetails.SocietyDetailId = entity.SocietyDetailsMastId;

                if (entity.SocietyWingDetails.PropertyId.HasValue && entity.SocietyWingDetails.PropertyId.Value <= 0)
                {
                    entity.SocietyWingDetails.PropertyId = null;
                }

                // Connect both entities
                entity.SocietyWingDetails.WingDetailsMast = entity;
            }

            await _repository.AddAsync(entity, cancellationToken);

            // Single SaveChanges
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return await GetByIdAsync(entity.Id, cancellationToken) ?? _mapper.Map<WingDetailsMastDto>(entity);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.DiscardChanges();
            throw;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────────────────────────────
    // UPDATE
    // ──────────────────────────────────────────────────────────────────────────────────────────────────
    public override async Task<WingDetailsMastDto?> UpdateAsync(int id, UpdateWingDetailsMastDto updateDto, CancellationToken cancellationToken = default)
    {
        // Fetch WITH tracking so EF Core detects property changes automatically
        var entity = await _repository.GetQueryable()
            .Where(x => x.IsActive && !x.MarkedForDeletion)
            .Include(x => x.SocietyWingDetails)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
            throw new ValidationException("The specified Wing Details record does not exist.", OperationType.Update);

        // Floor Validation
        if (updateDto.SocietyWingDetails != null &&
            !string.IsNullOrWhiteSpace(updateDto.SocietyWingDetails.FromFloor) &&
            !string.IsNullOrWhiteSpace(updateDto.SocietyWingDetails.ToFloor))
        {
            bool isFromFloorValid = int.TryParse(updateDto.SocietyWingDetails.FromFloor, out int fromFloor);
            bool isToFloorValid = int.TryParse(updateDto.SocietyWingDetails.ToFloor, out int toFloor);

            if (!isFromFloorValid || !isToFloorValid)
            {
                throw new ValidationException("Both FromFloor and ToFloor must contain valid numeric values.", OperationType.Update);
            }

            if (fromFloor > toFloor)
            {
                throw new ValidationException("FromFloor cannot be greater than ToFloor.", OperationType.Update);
            }
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Update parent fields (SocietyDetailsMastId & WingMasterId are ignored by mapping profile)
            _mapper.Map(updateDto, entity);
            entity.UpdatedDate = DateTime.Now;

            if (updateDto.SocietyWingDetails != null)
            {
                if (entity.SocietyWingDetails != null)
                {
                    // ── Update existing child ──
                    _mapper.Map(updateDto.SocietyWingDetails, entity.SocietyWingDetails);
                    entity.SocietyWingDetails.UpdatedDate = DateTime.Now;

                    // Keep FK fields in sync with the parent
                    entity.SocietyWingDetails.WingId = entity.WingMasterId;
                    entity.SocietyWingDetails.SocietyDetailId = entity.SocietyDetailsMastId;

                    if (entity.SocietyWingDetails.PropertyId.HasValue && entity.SocietyWingDetails.PropertyId.Value <= 0)
                    {
                        entity.SocietyWingDetails.PropertyId = null;
                    }
                }
                else
                {
                    // ── Create new child (parent existed without SocietyWingDetails) ──
                    entity.SocietyWingDetails = _mapper.Map<SocietyWingDetailsEntity>(updateDto.SocietyWingDetails);
                    entity.SocietyWingDetails.CreatedDate = DateTime.Now;

                    // Sync FK fields from the parent
                    entity.SocietyWingDetails.WingId = entity.WingMasterId;
                    entity.SocietyWingDetails.SocietyDetailId = entity.SocietyDetailsMastId;
                    entity.SocietyWingDetails.WingDetailsMastId = entity.Id;

                    if (entity.SocietyWingDetails.PropertyId.HasValue && entity.SocietyWingDetails.PropertyId.Value <= 0)
                    {
                        entity.SocietyWingDetails.PropertyId = null;
                    }

                    // Establish navigation
                    entity.SocietyWingDetails.WingDetailsMast = entity;
                }
            }

            await _repository.UpdateAsync(entity, cancellationToken);

            // Single SaveChanges
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return await GetByIdAsync(id, cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.DiscardChanges();
            throw;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────────────────────────────
    // VALIDATIONS
    // ──────────────────────────────────────────────────────────────────────────────────────────────────
    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        WingDetailsMastEntity currentEntity,
        WingDetailsMastEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<WingDetailsMastEntity>(id, cancellationToken);
        }

        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        WingDetailsMastEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<WingDetailsMastEntity>(id, cancellationToken);
    }

    // ──────────────────────────────────────────────────────────────────────────────────────────────────
    // SOFT DELETE
    // Sets MarkedForDeletion = true and IsActive = false.
    // ──────────────────────────────────────────────────────────────────────────────────────────────────
    public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetQueryable()
            .Include(x => x.Properties)
            .Include(x => x.SocietyWingDetails)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);

        if (entity == null)
            return false;

        // Check active properties
        if (entity.Properties?.Any(x => x.IsActive && !x.MarkedForDeletion) == true)
        {
            throw new ValidationException("Cannot delete Wing Details as there are active properties associated with it.", OperationType.Delete);
        }

        var validationResult = await ValidateForDeleteAsync(entity.Id, entity, cancellationToken);

        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                validationResult.Errors.First().ErrorMessage,
                validationResult.ToDictionary(),
                OperationType.Delete);
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Parent
            entity.IsActive = false;
            entity.MarkedForDeletion = true;
            entity.MarkedForDeletionDate = DateTime.Now;

            // Child
            if (entity.SocietyWingDetails != null)
            {
                entity.SocietyWingDetails.IsActive = false;
            }

            await _repository.UpdateAsync(entity, cancellationToken);

            // Single SaveChanges
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return true;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _unitOfWork.DiscardChanges();
            throw;
        }
    }

}
