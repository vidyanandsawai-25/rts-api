using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Asset_Management.SubUnitsDetails;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.Asset_Management;

/// <summary>
/// Service for managing sub-unit details (AMS.SubUnitsDetails) operations.
/// </summary>
public class SubUnitsDetailsService : BaseCommonCrudService<SubUnitsDetailsEntity, SubUnitsDetailsDto, CreateSubUnitsDetailsDto, UpdateSubUnitsDetailsDto, SubUnitsDetailsQueryParameters, int>,
    ISubUnitsDetailsService
{
    private readonly IReferenceValidationService _referenceValidator;
    private readonly ILogger<SubUnitsDetailsService> _logger;
    private readonly IRepository<AssetMasterEntity, int> _assetRepository;
    private readonly IRepository<AssetRoomWiseSubmissionDetailsEntity, int> _roomWiseRepository;
    private readonly IRepository<AssetLeaseRentDetailsEntity, int> _leaseRentDetailsRepository;
    private readonly IRepository<AssetRoomWiseMinusDataEntity, int> _minusRepository;
    private readonly IRepository<AssetTypeOfUseMasterEntity, int> _amsTypeOfUseRepository;
    private readonly IRepository<AssetSubTypeOfUseEntity, int> _amsSubTypeOfUseRepository;

    public SubUnitsDetailsService(
        IRepository<SubUnitsDetailsEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IReferenceValidationService referenceValidator,
        IRepository<AssetMasterEntity, int> assetRepository,
        IRepository<AssetRoomWiseSubmissionDetailsEntity, int> roomWiseRepository,
        IRepository<AssetLeaseRentDetailsEntity, int> leaseRentDetailsRepository,
        IRepository<AssetRoomWiseMinusDataEntity, int> minusRepository,
        IRepository<AssetTypeOfUseMasterEntity, int> amsTypeOfUseRepository,
        IRepository<AssetSubTypeOfUseEntity, int> amsSubTypeOfUseRepository,
        ILogger<SubUnitsDetailsService> logger)
        : base(repository, unitOfWork, mapper)
    {
        _referenceValidator = referenceValidator;
        _assetRepository = assetRepository;
        _roomWiseRepository = roomWiseRepository;
        _leaseRentDetailsRepository = leaseRentDetailsRepository;
        _minusRepository = minusRepository;
        _amsTypeOfUseRepository = amsTypeOfUseRepository;
        _amsSubTypeOfUseRepository = amsSubTypeOfUseRepository;
        _logger = logger;
    }

    protected override async Task<ValidationResult> ValidateForDeactivationAsync(
        int id,
        SubUnitsDetailsEntity currentEntity,
        SubUnitsDetailsEntity updatedEntity,
        CancellationToken cancellationToken = default)
    {
        if (currentEntity.IsActive && !updatedEntity.IsActive)
        {
            return await _referenceValidator.ValidateReferencesAsync<SubUnitsDetailsEntity>(id, cancellationToken);
        }
        return ValidationResult.Success();
    }

    protected override async Task<ValidationResult> ValidateForDeleteAsync(
        int id,
        SubUnitsDetailsEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await _referenceValidator.ValidateReferencesAsync<SubUnitsDetailsEntity>(id, cancellationToken);
    }

    protected override Task<ValidationResult> ValidateForCreateAsync(
        SubUnitsDetailsEntity entity,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ValidationResult.Success());
    }

    public override async Task<SubUnitsDetailsDto> CreateAsync(CreateSubUnitsDetailsDto createDto, CancellationToken cancellationToken = default)
    {
        // Check if there are any child subunits for this asset (meaning it is a parent building with subunits).
        // Fetched once and reused below, instead of a separate AnyAsync existence check followed by
        // a second query for the same ids when it turns out to be a parent building.
        var childAssetIds = await _assetRepository.GetQueryable()
            .Where(a => a.ParentAssetId == createDto.AssetId && !a.MarkedForDeletion)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        if (childAssetIds.Count > 0)
        {
            // Do NOT insert a parent building row.
            // Look up if any child subunit already has a configuration record on this floor level.
            var existingChildDetail = await _repository.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(f => childAssetIds.Contains(f.AssetId) && f.FloorId == createDto.FloorId && !f.MarkedForDeletion, cancellationToken);

            if (existingChildDetail != null)
            {
                // Return the existing child subunit's config DTO mapped back to the requested parent asset ID
                var dto = _mapper.Map<SubUnitsDetailsDto>(existingChildDetail);
                dto.AssetId = createDto.AssetId;
                return dto;
            }
            else
            {
                // If no child subunit detail exists yet, we return a successful response with ID = 0.
                // Since the frontend uses it only as a placeholder before createChildAssetAction, this is completely safe!
                return new SubUnitsDetailsDto
                {
                    Id = 0,
                    AssetId = createDto.AssetId,
                    FloorId = createDto.FloorId,
                    SubFloorId = createDto.SubFloorId,
                    IsActive = true
                };
            }
        }

        var resultDto = await base.CreateAsync(createDto, cancellationToken);

        if (createDto.RoomDetails != null && createDto.RoomDetails.Count > 0)
        {
            // Build every room entity first and insert them as one batch (AddRangeAsync + a single
            // SaveChangesAsync) instead of a SaveChangesAsync round trip per room. Room ids are only
            // known after this save, so offsets — which reference RoomWiseSubmissionId — are built
            // and saved as their own single batch afterwards. Same rows/values end up persisted;
            // just far fewer DB round trips for multi-room submissions.
            var roomEntities = new List<AssetRoomWiseSubmissionDetailsEntity>(createDto.RoomDetails.Count);
            foreach (var room in createDto.RoomDetails)
            {
                var areaSqMtr = room.AreaSqMtr ??
                    (createDto.CarpetAreaSqFeet.HasValue ? (double)(createDto.CarpetAreaSqFeet.Value * 0.092903m) : null);

                roomEntities.Add(new AssetRoomWiseSubmissionDetailsEntity
                {
                    AssetId = createDto.AssetId,
                    SubUnitsDetailsId = resultDto.Id,

                    LengthMtr = room.LengthMtr,
                    WidthMtr = room.WidthMtr,
                    AreaSqMtr = areaSqMtr,
                    HeightMtr = room.HeightMtr,
                    TotalAreaSqMtr = room.TotalAreaSqMtr ?? (areaSqMtr * (room.NoOfRooms ?? 1)),
                    Shape = room.Shape ?? "Rectangle",
                    RoomNo = room.RoomNo ?? "1",
                    RoomType = room.RoomType ?? "Commercial",
                    OuterYesNo = room.OuterYesNo,
                    MinusYesNo = room.MinusYesNo,

                    IsActive = true,
                    CreatedBy = createDto.CreatedBy ?? 1,
                    CreatedDate = DateTime.UtcNow
                });
            }

            await _roomWiseRepository.AddRangeAsync(roomEntities, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var minusEntities = new List<AssetRoomWiseMinusDataEntity>();
            for (var i = 0; i < createDto.RoomDetails.Count; i++)
            {
                var offsets = createDto.RoomDetails[i].Offsets;
                if (offsets == null || offsets.Count == 0)
                {
                    continue;
                }

                var roomEntity = roomEntities[i];
                foreach (var offset in offsets)
                {
                    minusEntities.Add(new AssetRoomWiseMinusDataEntity
                    {
                        RoomWiseSubmissionId = roomEntity.Id,
                        LengthMtr = offset.Length,
                        WidthMtr = offset.Width,
                        AreaSqMtr = offset.AreaSqM,
                        HeightMtr = offset.Height,
                        Shape = offset.Shape ?? "Rectangle",
                        IsActive = true,
                        CreatedBy = createDto.CreatedBy ?? 1,
                        CreatedDate = DateTime.UtcNow
                    });
                }
            }

            if (minusEntities.Count > 0)
            {
                await _minusRepository.AddRangeAsync(minusEntities, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        return resultDto;
    }

    public override async Task<PagedResult<SubUnitsDetailsDto>> GetAllAsync(SubUnitsDetailsQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        var query = _repository.GetQueryable();

        query = query.ApplyFilters(queryParameters);
        query = query.ApplySearch(queryParameters);
        query = query.ApplySort(queryParameters);

        var totalCount = await query.CountAsync(cancellationToken);

        var pagedQuery = query
            .Skip(queryParameters.PageSize == -1 ? 0 : (queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize);

        var items = await pagedQuery
            .AsNoTracking()
            .Include(f => f.Floor)
            .Include(f => f.SubFloor)
            .Include(f => f.ConstructionType)
            .Include(f => f.TypeOfUse)
            .Include(f => f.SubTypeOfUse)
            .Include(f => f.Asset)
            .Select(f => new SubUnitsDetailsDto
            {
                Id = f.Id,
                IsActive = f.IsActive,
                CreatedDate = f.CreatedDate,
                UpdatedDate = f.UpdatedDate,
                AssetId = f.AssetId,
                FloorId = f.FloorId,
                SubFloorId = f.SubFloorId,
                ConstructionYear = f.ConstructionYear,
                AssessmentYear = f.AssessmentYear,
                ConstructionTypeId = f.ConstructionTypeId,
                TypeOfUseId = f.TypeOfUseId,
                SubTypeOfUseId = f.SubTypeOfUseId,
                CarpetAreaSqMeter = f.CarpetAreaSqMeter,
                CarpetAreaSqFeet = f.CarpetAreaSqFeet,
                BuiltUpAreaSqMeter = f.BuiltUpAreaSqMeter,
                BuiltUpAreaSqFeet = f.BuiltUpAreaSqFeet,
                NoOfRooms = f.NoOfRooms,
                SubAssetCount = _roomWiseRepository.GetQueryable()
                    .Count(r => r.SubUnitsDetailsId == f.Id && r.IsActive && !r.MarkedForDeletion),
                CapitalValue = f.CapitalValue,
                BaseValue = f.BaseValue,
                CVBaseRate = f.CVBaseRate,
                MarkedForDeletion = f.MarkedForDeletion,
                MarkedForDeletionDate = f.MarkedForDeletionDate,
                Names = new SubUnitsDetailsNamesDto
                {
                    AssetName = f.Asset != null ? f.Asset.AssetName : null,
                    FloorName = f.Floor != null ? f.Floor.Description : null,
                    SubFloorName = f.SubFloor != null ? f.SubFloor.Description : null,
                    ConstructionTypeName = f.ConstructionType != null ? f.ConstructionType.Description : null,
                    TypeOfUseName = f.TypeOfUse != null ? f.TypeOfUse.Description : null,
                    SubTypeOfUseName = f.SubTypeOfUse != null ? f.SubTypeOfUse.Description : null
                }
            })
            .ToListAsync(cancellationToken);

        var pageNumber = queryParameters.PageSize == -1 ? 1 : queryParameters.PageNumber;
        var pageSize = queryParameters.PageSize == -1 ? totalCount : queryParameters.PageSize;

        return new PagedResult<SubUnitsDetailsDto>(items, totalCount, pageNumber, pageSize);
    }

    public override async Task<SubUnitsDetailsDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var dto = await _repository.GetQueryable()
            .AsNoTracking()
            .Include(f => f.Floor)
            .Include(f => f.SubFloor)
            .Include(f => f.ConstructionType)
            .Include(f => f.TypeOfUse)
            .Include(f => f.SubTypeOfUse)
            .Include(f => f.Asset)
            .Where(f => f.Id == id)
            .Select(f => new SubUnitsDetailsDto
            {
                Id = f.Id,
                IsActive = f.IsActive,
                CreatedDate = f.CreatedDate,
                UpdatedDate = f.UpdatedDate,
                AssetId = f.AssetId,
                FloorId = f.FloorId,
                SubFloorId = f.SubFloorId,
                ConstructionYear = f.ConstructionYear,
                AssessmentYear = f.AssessmentYear,
                ConstructionTypeId = f.ConstructionTypeId,
                TypeOfUseId = f.TypeOfUseId,
                SubTypeOfUseId = f.SubTypeOfUseId,
                CarpetAreaSqMeter = f.CarpetAreaSqMeter,
                CarpetAreaSqFeet = f.CarpetAreaSqFeet,
                BuiltUpAreaSqMeter = f.BuiltUpAreaSqMeter,
                BuiltUpAreaSqFeet = f.BuiltUpAreaSqFeet,
                NoOfRooms = f.NoOfRooms,
                SubAssetCount = _roomWiseRepository.GetQueryable()
                    .Count(r => r.SubUnitsDetailsId == f.Id && r.IsActive && !r.MarkedForDeletion),
                CapitalValue = f.CapitalValue,
                BaseValue = f.BaseValue,
                CVBaseRate = f.CVBaseRate,
                MarkedForDeletion = f.MarkedForDeletion,
                MarkedForDeletionDate = f.MarkedForDeletionDate,
                Names = new SubUnitsDetailsNamesDto
                {
                    AssetName = f.Asset != null ? f.Asset.AssetName : null,
                    FloorName = f.Floor != null ? f.Floor.Description : null,
                    SubFloorName = f.SubFloor != null ? f.SubFloor.Description : null,
                    ConstructionTypeName = f.ConstructionType != null ? f.ConstructionType.Description : null,
                    TypeOfUseName = f.TypeOfUse != null ? f.TypeOfUse.Description : null,
                    SubTypeOfUseName = f.SubTypeOfUse != null ? f.SubTypeOfUse.Description : null
                }
            })
            .FirstOrDefaultAsync(cancellationToken);

        return dto;
    }

    /// <summary>
    /// Gets all floor details for a specific asset with summary totals.
    /// </summary>
    public async Task<SubUnitsDetailsSummaryDto> GetByAssetIdAsync(int assetId, CancellationToken cancellationToken = default)
    {
        // Find all child asset IDs if this is a parent building
        var childAssetIds = await _assetRepository.GetQueryable()
            .Where(a => a.ParentAssetId == assetId && !a.MarkedForDeletion)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        var assetIdsToQuery = new List<int> { assetId };
        if (childAssetIds.Any())
        {
            assetIdsToQuery.AddRange(childAssetIds);
        }

        var allFloorDetails = await _repository.GetQueryable()
            .AsNoTracking()
            .Include(f => f.Floor)
            .Include(f => f.SubFloor)
            .Include(f => f.ConstructionType)
            // Removed Include for TypeOfUse and SubTypeOfUse since they map to PTIS tables, not AMS
            .Include(f => f.Asset)
            .Where(f => assetIdsToQuery.Contains(f.AssetId) && f.IsActive)
            .OrderBy(f => f.FloorId)
            .ThenBy(f => f.SubFloorId)
            .ToListAsync(cancellationToken);

        // Resolve display names only for the TypeOfUse/SubTypeOfUse ids actually referenced by these
        // rows, instead of loading the entire AMS type-of-use master tables into memory on every call.
        var typeOfUseIdsNeeded = allFloorDetails.Select(f => f.TypeOfUseId).Distinct().ToList();
        var subTypeOfUseIdsNeeded = allFloorDetails.Where(f => f.SubTypeOfUseId.HasValue)
            .Select(f => f.SubTypeOfUseId!.Value).Distinct().ToList();

        var typeOfUseLookup = await _amsTypeOfUseRepository.GetQueryable()
            .AsNoTracking()
            .Where(t => t.IsActive && typeOfUseIdsNeeded.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Description, cancellationToken);

        var subTypeOfUseLookup = await _amsSubTypeOfUseRepository.GetQueryable()
            .AsNoTracking()
            .Where(t => t.IsActive && subTypeOfUseIdsNeeded.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Description, cancellationToken);

        var floorDetails = new List<SubUnitsDetailsDto>();
        var groupedByFloor = allFloorDetails.GroupBy(f => new { f.FloorId, f.SubFloorId });
        foreach (var group in groupedByFloor)
        {
            var rep = group.FirstOrDefault(g => g.AssetId != assetId) ?? group.First();

            var dto = new SubUnitsDetailsDto
            {
                Id = rep.Id,
                IsActive = rep.IsActive,
                CreatedDate = rep.CreatedDate,
                UpdatedDate = rep.UpdatedDate,
                AssetId = assetId, // Map back to parent building assetId so frontend functions correctly
                FloorId = rep.FloorId,
                SubFloorId = rep.SubFloorId,
                ConstructionYear = rep.ConstructionYear,
                AssessmentYear = rep.AssessmentYear,
                ConstructionTypeId = rep.ConstructionTypeId,
                TypeOfUseId = rep.TypeOfUseId,
                SubTypeOfUseId = rep.SubTypeOfUseId,
                CarpetAreaSqMeter = group.Sum(x => x.CarpetAreaSqMeter ?? 0m),
                CarpetAreaSqFeet = group.Sum(x => x.CarpetAreaSqFeet ?? 0m),
                BuiltUpAreaSqMeter = group.Sum(x => x.BuiltUpAreaSqMeter ?? 0m),
                BuiltUpAreaSqFeet = group.Sum(x => x.BuiltUpAreaSqFeet ?? 0m),
                NoOfRooms = group.Sum(x => x.NoOfRooms ?? 0),
                SubAssetCount = group.Count(x => x.AssetId != assetId),
                CapitalValue = group.Sum(x => x.CapitalValue ?? 0m),
                BaseValue = group.Sum(x => x.BaseValue ?? 0m),
                CVBaseRate = rep.CVBaseRate,
                MarkedForDeletion = rep.MarkedForDeletion,
                MarkedForDeletionDate = rep.MarkedForDeletionDate,
                Names = new SubUnitsDetailsNamesDto
                {
                    AssetName = rep.Asset?.AssetName,
                    FloorName = rep.Floor?.Description,
                    SubFloorName = rep.SubFloor?.Description,
                    ConstructionTypeName = rep.ConstructionType?.Description,
                    TypeOfUseName = rep.TypeOfUseId > 0 && typeOfUseLookup.TryGetValue(rep.TypeOfUseId, out var tou) ? tou : null,
                    SubTypeOfUseName = rep.SubTypeOfUseId.HasValue && subTypeOfUseLookup.TryGetValue(rep.SubTypeOfUseId.Value, out var stou) ? stou : null
                }
            };
            floorDetails.Add(dto);
        }

        if (floorDetails.Any())
        {
            var floorDetailIds = floorDetails.Select(f => f.Id).ToList();
            var allRooms = await _roomWiseRepository.GetQueryable()
                .AsNoTracking()
                .Include(r => r.RoomMinusData)
                .Where(r => r.SubUnitsDetailsId.HasValue && floorDetailIds.Contains(r.SubUnitsDetailsId.Value) && !r.MarkedForDeletion)
                .ToListAsync(cancellationToken);

            // Group once into a lookup instead of re-scanning the full allRooms list per floor detail
            // (that was an O(floors * rooms) re-filter; this is a single O(rooms) pass).
            var roomsByFloorDetailId = allRooms.ToLookup(r => r.SubUnitsDetailsId!.Value);

            foreach (var fd in floorDetails)
            {
                var fdRooms = roomsByFloorDetailId[fd.Id];
                fd.RoomDetails = fdRooms.Select(r => new NtisPlatform.Application.DTOs.Asset_Management.AssetMaster.RoomDetailDto
                {
                    LengthMtr = r.LengthMtr,
                    WidthMtr = r.WidthMtr,
                    HeightMtr = r.HeightMtr,
                    AreaSqMtr = r.AreaSqMtr,
                    TotalAreaSqMtr = r.TotalAreaSqMtr,
                    Shape = r.Shape,
                    RoomNo = r.RoomNo,
                    RoomType = r.RoomType,
                    OuterYesNo = r.OuterYesNo,
                    MinusYesNo = r.MinusYesNo,
                    Offsets = r.RoomMinusData != null
                        ? r.RoomMinusData.Where(m => !m.MarkedForDeletion).Select(m => new NtisPlatform.Application.DTOs.Asset_Management.AssetMaster.RoomOffsetDto
                        {
                            Id = m.Id,
                            Shape = m.Shape,
                            Length = m.LengthMtr,
                            Width = m.WidthMtr,
                            Height = m.HeightMtr,
                            Base1 = null,
                            Base2 = null,
                            Radius = null,
                            AreaSqM = m.AreaSqMtr,
                            Op = "Subtract"
                        }).ToList()
                        : new List<NtisPlatform.Application.DTOs.Asset_Management.AssetMaster.RoomOffsetDto>()
                }).ToList();
            }
        }

        var summary = new SubUnitsDetailsSummaryDto
        {
            FloorDetails = floorDetails,
            TotalBaseValue = floorDetails.Sum(f => f.BaseValue ?? 0),
            TotalCapitalValue = floorDetails.Sum(f => f.CapitalValue ?? 0),
            TotalMarketValue = floorDetails.Sum(f => f.CapitalValue ?? 0),
            TotalFloors = floorDetails.Count
        };

        return summary;
    }

    public async Task<bool> CreateDirectRoomsAsync(DirectRoomRegistrationDto dto, int currentUserId, CancellationToken cancellationToken = default)
    {
        var parentAsset = await _assetRepository.GetByIdAsync(dto.ParentAssetId, cancellationToken);
        if (parentAsset == null) throw new KeyNotFoundException($"Parent asset with Id {dto.ParentAssetId} not found.");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Delete existing rooms/floors for this specific floor
            var existingFloors = await _repository.GetQueryable()
                .Where(f => f.AssetId == dto.ParentAssetId && f.FloorId == dto.FloorId)
                .ToListAsync(cancellationToken);

            var existingFloorIds = existingFloors.Select(f => f.Id).ToList();

            if (existingFloorIds.Any())
            {
                var existingRooms = await _roomWiseRepository.GetQueryable()
                    .Where(r => r.SubUnitsDetailsId.HasValue && existingFloorIds.Contains(r.SubUnitsDetailsId.Value))
                    .ToListAsync(cancellationToken);
                // Delete via the already-loaded entity (DeleteAsync(id) would just re-resolve the same
                // tracked instance through FindAsync) — avoids a redundant lookup per row.
                foreach (var room in existingRooms)
                    await _roomWiseRepository.DeleteAsync(room, cancellationToken);

                var existingLeases = await _leaseRentDetailsRepository.GetQueryable()
                    .Where(l => l.FloorDetailsId.HasValue && existingFloorIds.Contains(l.FloorDetailsId.Value))
                    .ToListAsync(cancellationToken);
                foreach (var lease in existingLeases)
                    await _leaseRentDetailsRepository.DeleteAsync(lease, cancellationToken);

                foreach (var f in existingFloors)
                    await _repository.DeleteAsync(f, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 2. Create property groups and rooms
            int subFloorId = 1;
            int? firstFloorDetailsId = null;

            foreach (var group in dto.PropertyGroups)
            {
                // Calculate area
                double totalAreaSqMtr = 0;
                foreach (var room in group.Rooms)
                {
                    var area = room.AreaSqMtr ?? (room.LengthMtr * room.WidthMtr) ?? 0;
                    var count = room.NoOfRooms ?? 1;
                    totalAreaSqMtr += (area * count);
                }

                var floorDetail = new SubUnitsDetailsEntity
                {
                    AssetId = dto.ParentAssetId,
                    FloorId = dto.FloorId,
                    SubFloorId = dto.PropertyGroups.Count > 1 ? subFloorId++ : null,
                    ConstructionYear = group.ConstructionYear,
                    AssessmentYear = DateTime.UtcNow.Year.ToString(),
                    ConstructionTypeId = group.ConstructionTypeId,
                    TypeOfUseId = group.TypeOfUseId,
                    SubTypeOfUseId = group.SubTypeOfUseId,
                    CarpetAreaSqMeter = (decimal)totalAreaSqMtr,
                    CarpetAreaSqFeet = (decimal)(totalAreaSqMtr / 0.092903),
                    BuiltUpAreaSqMeter = (decimal)(totalAreaSqMtr * 1.2),
                    BuiltUpAreaSqFeet = (decimal)((totalAreaSqMtr * 1.2) / 0.092903),
                    NoOfRooms = group.Rooms.Sum(r => r.NoOfRooms ?? 1),
                    IsActive = true,
                    CreatedBy = currentUserId,
                    CreatedDate = DateTime.UtcNow
                };

                await _repository.AddAsync(floorDetail, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (firstFloorDetailsId == null) firstFloorDetailsId = floorDetail.Id;

                var roomEntitiesForGroup = new List<AssetRoomWiseSubmissionDetailsEntity>(group.Rooms.Count);
                foreach (var room in group.Rooms)
                {
                    var roomArea = room.AreaSqMtr ?? (room.LengthMtr * room.WidthMtr) ?? 0;
                    roomEntitiesForGroup.Add(new AssetRoomWiseSubmissionDetailsEntity
                    {
                        AssetId = dto.ParentAssetId, // No child asset
                        SubUnitsDetailsId = floorDetail.Id,
                        LengthMtr = room.LengthMtr,
                        WidthMtr = room.WidthMtr,
                        HeightMtr = room.HeightMtr,
                        AreaSqMtr = roomArea,
                        TotalAreaSqMtr = roomArea * (room.NoOfRooms ?? 1),
                        Shape = room.Shape ?? "Rectangle",
                        RoomNo = room.RoomNo ?? "1",
                        RoomType = room.RoomType ?? "Room",
                        OuterYesNo = room.OuterYesNo,
                        MinusYesNo = room.MinusYesNo,
                        IsActive = true,
                        CreatedBy = currentUserId,
                        CreatedDate = DateTime.UtcNow
                    });
                }
                await _roomWiseRepository.AddRangeAsync(roomEntitiesForGroup, cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 3. Create Lease Rent Details if provided
            if (dto.RentInformation != null && firstFloorDetailsId.HasValue)
            {
                var leaseType = (dto.RentInformation.LeaseRentType ?? string.Empty)
                    .ToLower().Contains("lease") ? "Lease" : "Rent";

                var leaseRent = new AssetLeaseRentDetailsEntity
                {
                    AssetId = dto.ParentAssetId,
                    FloorDetailsId = firstFloorDetailsId,
                    LeaseType = leaseType,
                    LeaseStartDate = dto.RentInformation.LeaseStart ?? DateTime.UtcNow,
                    LeaseEndDate = dto.RentInformation.LeaseEnd,
                    Duration = dto.RentInformation.Duration,
                    PaymentFrequency = dto.RentInformation.RentFrequency ?? "Monthly",
                    RentAmount = dto.RentInformation.RentAmount,
                    SecurityDeposit = dto.RentInformation.SecurityDeposit ?? 0,
                    DepositType = dto.RentInformation.DepositType,
                    IsActive = true,
                    CreatedBy = currentUserId,
                    CreatedDate = DateTime.UtcNow
                };
                await _leaseRentDetailsRepository.AddAsync(leaseRent, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error creating direct rooms");
            throw;
        }
    }
}
