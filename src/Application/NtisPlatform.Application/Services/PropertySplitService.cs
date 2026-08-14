using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Constants;
using NtisPlatform.Application.DTOs.PropertySplit;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class PropertySplitService : BaseCommonCrudService<PropertyMapDetailEntity, PropertySplitDto, CreatePropertySplitDto, UpdatePropertySplitDto, PropertySplitQueryParameters, int>, IPropertySplitService  
{
    private readonly IRepository<PropertyMapMasterEntity, int> _propertyMapMasterRepository;
    private readonly IRepository<PropertyMastOldEntity, int> _propertyOldRepository;
    private readonly IRepository<PropertyMapDetailEntity, int> _propertyMapDetailRepository;
    private readonly new IRepository<PropertyEntity, int> _repository;
    private readonly IRepository<WardEntity, int> _wardRepository;
    private readonly IRepository<SocietyDetailsEntity, int> _societyRepository;
    private readonly IRepository<MergeDetailEntity, int> _mergeDetailRepository;
    private readonly new IUnitOfWork _unitOfWork;
    private readonly ILogger<PropertySplitService> _logger;
    private readonly IMapper _mapper;

    public PropertySplitService(
        IRepository<PropertyMapMasterEntity, int> propertyMapMasterRepository,
        IRepository<PropertyMastOldEntity, int> propertyOldRepository,
        IRepository<PropertyMapDetailEntity, int> propertyMapDetailRepository,
        IRepository<PropertyEntity, int> repository,
        IRepository<WardEntity, int> wardRepository,
        IRepository<SocietyDetailsEntity, int> societyRepository,
        IRepository<MergeDetailEntity, int> mergeDetailRepository,
        IUnitOfWork unitOfWork,
        ILogger<PropertySplitService> logger,
        IMapper mapper) : base(propertyMapDetailRepository, unitOfWork, mapper)
    {
        _propertyMapMasterRepository = propertyMapMasterRepository;
        _propertyOldRepository = propertyOldRepository;
        _propertyMapDetailRepository = propertyMapDetailRepository;
        _repository = repository;
        _wardRepository = wardRepository;
        _societyRepository = societyRepository;
        _mergeDetailRepository = mergeDetailRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
    }

    public override async Task<PropertySplitDto> CreateAsync(CreatePropertySplitDto dto,CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            //  GET PROPERTY MAP CATEGORY ID
            var propertyMapId = await _propertyMapMasterRepository.GetQueryable().AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.MappingCategory == PropertyMappingCategory.SplitMappingCategory)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (propertyMapId <= 0)
            {
                throw new ValidationException("Property Map Category", $"{PropertyMappingCategory.SplitMappingCategory} property mapping category not found", OperationType.Create);
            }

            var propertyIds = dto.PropertyIds.Distinct().ToList();
            var propertyOldId = dto.PropertyOldId;

            // VALIDATE DUPLICATE NEW PROPERTIES
            if (propertyIds.Count != dto.PropertyIds.Count)
            {
                throw new ValidationException("New Property","Duplicate new property found",OperationType.Create);
            }

            //  LOAD OLD PROPERTY
            var propertyMastOld = await _propertyOldRepository.GetQueryable().AsNoTracking()
                .Where(x =>
                    x.Id == propertyOldId &&
                    x.IsActive && !x.MarkedForDeletion)
                .Select(x => new
                {
                    x.Id,
                    x.OldPropertyNo,
                    x.OldWardNo,
                    x.OldPartitionNo,
                    x.OldOwnerName,
                    x.OldOwnerNameEnglish,
                    x.OldOccupierName,
                    x.OldOccupierNameEnglish,
                    x.OldMobileNo,
                    x.OldAddress,
                    x.OldAddressEnglish,
                    x.OldFlatOrShopNumber
                })
                .FirstOrDefaultAsync(cancellationToken);


            if (propertyMastOld == null)
            {
                throw new ValidationException("Old Property","Old Property not found",OperationType.Create);
            }
            
            //  BUILD OLD PROPERTY NUMBER
            var oldPropertyNo = BuildPropertyNumber(propertyMastOld.OldWardNo,propertyMastOld.OldPropertyNo,propertyMastOld.OldPartitionNo);

           
            // LOAD NEW PROPERTIES + WARD + SOCIETY
            var propertyMastList = await (
                from pm in _repository.GetQueryable().AsNoTracking()
                join wd in _wardRepository.GetQueryable().AsNoTracking()
                    on pm.WardId equals wd.Id
                join society in _societyRepository.GetQueryable().AsNoTracking()
                    on pm.SocietyDetailId equals society.Id
                    into societyGroup
                from sd in societyGroup.DefaultIfEmpty()
                where
                    propertyIds.Contains(pm.Id) &&
                    pm.IsActive && !pm.MarkedForDeletion
                select new
                {
                    pm.Id,
                    wd.WardNo,
                    pm.PropertyNo,
                    pm.PartitionNo,
                    pm.OwnerName,
                    pm.OwnerNameEnglish,
                    pm.OccupierName,
                    pm.OccupierNameEnglish,
                    pm.MobileNo,
                    pm.Address,
                    pm.AddressEnglish,
                    BuilderName = sd != null ? sd.BuilderName : null,
                    BuilderNameEnglish = sd != null ? sd.BuilderNameEnglish : null,
                    pm.FlatOrShopNo,
                    pm.FlatOrShopNoEnglish,
                    pm.FlatOrShopName,
                    pm.FlatOrShopNameEnglish
                })
                .ToListAsync(cancellationToken);


            //  VALIDATE ALL NEW PROPERTIES
            if (propertyMastList.Count != propertyIds.Count)
            {
                var foundPropertyIds = propertyMastList.Select(x => x.Id).ToHashSet();
                var missingPropertyIds = propertyIds.Where(x => !foundPropertyIds.Contains(x)).ToList();

                throw new ValidationException("New Property",$"New property not found or inactive for id(s): {string.Join(", ", missingPropertyIds)}",OperationType.Create);
            }

            //  CHECK EXISTING ACTIVE NEW PROPERTY MAPPINGS
            var existingNewPropertyMapping =
                await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                    .Where(x =>
                        x.PropertyIdNew.HasValue &&
                        propertyIds.Contains(x.PropertyIdNew.Value) &&
                        x.IsActive &&
                        x.Status == PropertyMapStatus.Active)
                    .Select(x => new
                    {
                        PropertyIdNew = x.PropertyIdNew!.Value,
                        x.PropertyNoNew,
                        x.PropertyNoOld
                    })
                    .FirstOrDefaultAsync(cancellationToken);


            
            //  IF ANY NEW PROPERTY ALREADY MERGED, STOP
            if (existingNewPropertyMapping != null)
            {
                throw new ValidationException("New Property",$"New property no {existingNewPropertyMapping.PropertyNoNew} is already merged with old property no {existingNewPropertyMapping.PropertyNoOld}",OperationType.Create);
            }

            //  LOAD ALL PROPERTY MASTER ROWS TO UPDATE
            var propertiesToUpdate = await _repository
                .GetQueryable()
                .Where(x =>
                    propertyIds.Contains(x.Id) &&
                    x.IsActive && !x.MarkedForDeletion)
                .ToListAsync(cancellationToken);

            //  VALIDATE TRACKED PROPERTY ROWS
            if (propertiesToUpdate.Count != propertyIds.Count)
            {
                var foundIds = propertiesToUpdate.Select(x => x.Id).ToHashSet();
                var missingIds = propertyIds.Where(x => !foundIds.Contains(x)).ToList();
                throw new ValidationException("New Property",$"Unable to update new property id(s): {string.Join(", ", missingIds)}",OperationType.Create);
            }

            //  CREATE O(1) LOOKUP
            var propertyUpdateLookup = propertiesToUpdate.ToDictionary(x => x.Id);
            var now = DateTime.Now;
            decimal? latitude = decimal.TryParse(dto.Latitude, out var lat) ? lat : null;
            decimal? longitude = decimal.TryParse(dto.Longitude, out var lon) ? lon : null;
            var mergedNewPropertyNumbers = new List<string>(propertyMastList.Count);
            var propertyMapDetails = new List<PropertyMapDetailEntity>(propertyMastList.Count);

            // BUILD EVERYTHING IN MEMORY
            foreach (var propertyMast in propertyMastList)
            {
                var propertyId = propertyMast.Id;
               
                // BUILD NEW PROPERTY NUMBER
                var newPropertyNo = BuildPropertyNumber(propertyMast.WardNo,propertyMast.PropertyNo,propertyMast.PartitionNo);
                mergedNewPropertyNumbers.Add(newPropertyNo);

               
                // BUILD FINAL MERGED OWNER/OCCUPIER VALUES
                var ownerName = BuildMergedPersonName(propertyMast.OwnerName,propertyMastOld.OldOwnerName);
                var ownerNameEnglish = BuildMergedPersonName(propertyMast.OwnerNameEnglish,propertyMastOld.OldOwnerNameEnglish);
                var occupierName = BuildMergedPersonName(propertyMast.OccupierName,propertyMastOld.OldOccupierName);
                var occupierNameEnglish = BuildMergedPersonName(propertyMast.OccupierNameEnglish,propertyMastOld.OldOccupierNameEnglish);

                // GET TRACKED PROPERTY MASTER
                var propertyEntity = propertyUpdateLookup[propertyId];

                // ALWAYS UPDATE OWNER/OCCUPIER
                if (!string.IsNullOrWhiteSpace(ownerName))
                {
                    propertyEntity.OwnerName = ownerName;
                }
                if (!string.IsNullOrWhiteSpace(ownerNameEnglish))
                {
                    propertyEntity.OwnerNameEnglish = ownerNameEnglish;
                }
                if (!string.IsNullOrWhiteSpace(occupierName))
                {
                    propertyEntity.OccupierName = occupierName;
                }
                if (!string.IsNullOrWhiteSpace(occupierNameEnglish))
                {
                    propertyEntity.OccupierNameEnglish = occupierNameEnglish;
                }

                if (dto.IsOldDataUpdate)
                {
                    if (!string.IsNullOrWhiteSpace(propertyMastOld.OldMobileNo))
                    {
                        propertyEntity.MobileNo = propertyMastOld.OldMobileNo;
                    }

                    if (!string.IsNullOrWhiteSpace(propertyMastOld.OldAddress))
                    {
                        propertyEntity.Address = propertyMastOld.OldAddress;
                    }

                    if (!string.IsNullOrWhiteSpace(propertyMastOld.OldAddressEnglish))
                    {
                        propertyEntity.AddressEnglish = propertyMastOld.OldAddressEnglish;
                    }

                    if (!string.IsNullOrWhiteSpace(propertyMastOld.OldFlatOrShopNumber))
                    {
                        propertyEntity.FlatOrShopNo = propertyMastOld.OldFlatOrShopNumber;
                    }
                }

                propertyEntity.UpdatedBy = dto.CreatedBy;
                propertyEntity.UpdatedDate = now;
               
                // CREATE PROPERTY MAP DETAIL IN MEMORY
                var propertyMapDetail = new PropertyMapDetailEntity
                    {
                        PropertyMapId = propertyMapId,
                        PropertyIdNew = propertyId,
                        PropertyIdOld = propertyOldId,
                        PropertyNoNew = newPropertyNo,
                        PropertyNoOld = oldPropertyNo,
                        Status = PropertyMapStatus.Active,
                        Remark = "Property Merged - Single Old Property Into Multiple New Properties",
                        Latitude = latitude,
                        Longitude = longitude,
                        Location = dto.Location,
                        CreatedBy = dto.CreatedBy,

                        // ORIGINAL NEW PROPERTY SNAPSHOT
                        MergeDetail = new MergeDetailEntity
                        {
                            OwnerName = propertyMast.OwnerName,
                            OwnerNameEnglish = propertyMast.OwnerNameEnglish,
                            OccupierName = propertyMast.OccupierName,
                            OccupierNameEnglish = propertyMast.OccupierNameEnglish,
                            MobileNo = propertyMast.MobileNo,
                            Address = propertyMast.Address,
                            AddressEnglish = propertyMast.AddressEnglish,
                            FlatOrShopNo = propertyMast.FlatOrShopNo,
                            FlatOrShopNoEnglish = propertyMast.FlatOrShopNoEnglish,
                            FlatOrShopName = propertyMast.FlatOrShopName,
                            FlatOrShopNameEnglish = propertyMast.FlatOrShopNameEnglish,
                            BuilderName = propertyMast.BuilderName,
                            BuilderNameEnglish = propertyMast.BuilderNameEnglish,
                            CreatedBy = dto.CreatedBy
                        }
                    };
                propertyMapDetails.Add(propertyMapDetail);
            }

            // CONVERT EXISTING OLD PROPERTY MAPPING
            await _propertyMapDetailRepository.GetQueryable()
                .Where(x =>
                    x.PropertyIdOld == propertyOldId && x.IsActive &&
                    x.Status == PropertyMapStatus.Active && x.PropertyMapId != propertyMapId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.PropertyMapId,propertyMapId)
                        .SetProperty(x => x.UpdatedBy,dto.CreatedBy)
                        .SetProperty(x => x.UpdatedDate,now),
                    cancellationToken);


            // ADD ALL PROPERTY MAP + MERGE DETAIL RECORDS
            await _propertyMapDetailRepository.AddRangeAsync(propertyMapDetails,cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return new PropertySplitDto
            {
                Success = true,
                Message =$"Old property no {oldPropertyNo} successfully merged into {mergedNewPropertyNumbers.Count} new properties: {string.Join(", ", mergedNewPropertyNumbers)}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"Multiple property merge failed. Old:{OldId} New:{NewIds}",dto.PropertyOldId, dto.PropertyIds != null ? string.Join(",", dto.PropertyIds) : string.Empty);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
   
    public override async Task<PropertySplitDto?> UpdateAsync(int id,UpdatePropertySplitDto dto,CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var oldPropertyId = dto.PropertyOldId;
            var newPropertyIds = dto.PropertyIds?.Where(x => x > 0).Distinct().ToList() ?? new List<int>();

            if (newPropertyIds.Count < 2)
            {
                throw new ValidationException("Property","Multiple demerge requires at least two new properties", OperationType.Update);
            }

            //  LOAD SELECTED ACTIVE MERGE MAPPINGS
            var validationQuery = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                .Where(pmd =>
                    pmd.PropertyIdOld == oldPropertyId &&
                    pmd.PropertyIdNew.HasValue &&
                    newPropertyIds.Contains(pmd.PropertyIdNew.Value) &&
                    pmd.IsActive &&
                    pmd.Status == PropertyMapStatus.Active)
                .Select(pmd => new
                {
                    pmd.Id,
                    pmd.PropertyMapId,
                    pmd.PropertyIdOld,
                    pmd.PropertyIdNew,
                    pmd.PropertyNoNew,
                    pmd.PropertyNoOld
                })
                .ToListAsync(cancellationToken);

            //  VALIDATE ACTIVE MAPPINGS
            if (validationQuery.Count == 0)
            {
                var oldPropertyExists = await _propertyOldRepository.GetQueryable().AsNoTracking()
                    .AnyAsync(
                        x => x.Id == oldPropertyId &&
                            x.IsActive && !x.MarkedForDeletion,
                        cancellationToken);

                throw new ValidationException("Old Property",oldPropertyExists ? "No merge details found to demerge" : "Old property not found", OperationType.Update);
            }

            //  ALL REQUESTED PROPERTIES MUST HAVE ACTIVE MAPPING
            var mappedNewPropertyIds = validationQuery.Where(x => x.PropertyIdNew.HasValue)
                .Select(x => x.PropertyIdNew!.Value).Distinct().ToHashSet();

            var invalidNewPropertyIds = newPropertyIds.Where(x => !mappedNewPropertyIds.Contains(x)).ToList();

            if (invalidNewPropertyIds.Count > 0)
            {
                throw new ValidationException("Property",$"Active merge mapping not found for new property Id(s): {string.Join(", ", invalidNewPropertyIds)}",OperationType.Update);
            }

            var propertyMapDetailIds = validationQuery.Select(x => x.Id).ToList();

            //  LOAD ALL MERGE DETAIL SNAPSHOTS
            var mergeDetails = await _mergeDetailRepository.GetQueryable().AsNoTracking()
                .Where(md =>
                    propertyMapDetailIds.Contains(md.PropertyMapDetailId) &&
                    md.IsActive)
                .ToListAsync(cancellationToken);

            if (mergeDetails.Count == 0)
            {
                throw new ValidationException("Merge Details","Original property data not found",OperationType.Update);
            }

            //  EVERY MAPPING MUST HAVE SNAPSHOT
            var snapshotMapIds = mergeDetails.Select(x => x.PropertyMapDetailId).ToHashSet();
            var mappingsWithoutSnapshot = propertyMapDetailIds.Where(x => !snapshotMapIds.Contains(x)).ToList();

            if (mappingsWithoutSnapshot.Count > 0)
            {
                throw new ValidationException("Merge Details","Merge snapshot not found for one or more selected properties",OperationType.Update);
            }

            // LOAD CURRENT PROPERTY MASTER ENTITIES
            var currentProperties = await _repository
                .GetQueryable()
                .Where(x =>
                    newPropertyIds.Contains(x.Id) &&
                    x.IsActive && !x.MarkedForDeletion)
                .ToListAsync(cancellationToken);

            if (currentProperties.Count != newPropertyIds.Count)
            {
                var foundPropertyIds = currentProperties.Select(x => x.Id).ToHashSet();
                var missingPropertyIds = newPropertyIds.Where(x => !foundPropertyIds.Contains(x)).ToList();

                throw new ValidationException("Property",$"Property not found for Id(s): {string.Join(", ", missingPropertyIds)}",OperationType.Update);
            }

            //  LOAD OLD PROPERTY
            var oldPropertyData = await _propertyOldRepository.GetQueryable().AsNoTracking()
                .Where(x =>
                    x.Id == oldPropertyId &&
                    x.IsActive && !x.MarkedForDeletion)
                .Select(x => new
                {
                    x.Id,
                    x.OldOwnerName,
                    x.OldOwnerNameEnglish,
                    x.OldOccupierName,
                    x.OldOccupierNameEnglish
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (oldPropertyData == null)
            {
                throw new ValidationException("Old Property","Old property not found",OperationType.Update);
            }

            //  CREATE LOOKUPS
            var mappingByNewPropertyId = validationQuery.Where(x => x.PropertyIdNew.HasValue).ToDictionary(x => x.PropertyIdNew!.Value,x => x);
            var mergeDetailByMapDetailId = mergeDetails.ToDictionary(x => x.PropertyMapDetailId,x => x);

            //  LOAD ALL REQUIRED SOCIETIES
            var societyIds = currentProperties.Where(x => x.SocietyDetailId.HasValue).Select(x => x.SocietyDetailId!.Value).Distinct().ToList();
            List<SocietyDetailsEntity> societies = new();
            if (societyIds.Count > 0)
            {
                societies = await _societyRepository.GetQueryable()
                    .Where(x => societyIds.Contains(x.Id) && x.IsActive)
                    .ToListAsync(cancellationToken);
            }
            var societyLookup = societies.ToDictionary(x => x.Id);
            var updatedDate = DateTime.Now;
            
            //  BUILD ALL PROPERTY/SOCIETY CHANGES IN MEMORY
            foreach (var currentProperty in currentProperties)
            {
                if (!mappingByNewPropertyId.TryGetValue(currentProperty.Id,out var propertyMapping))
                {
                    throw new ValidationException("Property",$"Active merge mapping not found for new property Id: {currentProperty.Id}",OperationType.Update);
                }

                // Find original snapshot
                if (!mergeDetailByMapDetailId.TryGetValue(propertyMapping.Id,out var restoreData))
                {
                    throw new ValidationException("Merge Details",$"Restore snapshot not found for property no {propertyMapping.PropertyNoNew}",OperationType.Update);
                }

                // REMOVE OLD OWNER / OCCUPIER
                var updatedOwnerName = RemoveOwnerNameFromCommaSeparated(currentProperty.OwnerName,oldPropertyData.OldOwnerName);
                var updatedOwnerNameEnglish = RemoveOwnerNameFromCommaSeparated(currentProperty.OwnerNameEnglish,oldPropertyData.OldOwnerNameEnglish);
                var updatedOccupierName = RemoveOwnerNameFromCommaSeparated(currentProperty.OccupierName,oldPropertyData.OldOccupierName);
                var updatedOccupierNameEnglish = RemoveOwnerNameFromCommaSeparated(currentProperty.OccupierNameEnglish,oldPropertyData.OldOccupierNameEnglish);

                // ALWAYS RESTORE OWNER/OCCUPIER
                currentProperty.OwnerName = string.IsNullOrWhiteSpace(updatedOwnerName) ? "The Holder" : updatedOwnerName;
                currentProperty.OwnerNameEnglish = string.IsNullOrWhiteSpace(updatedOwnerNameEnglish) ? "The Holder" : updatedOwnerNameEnglish;
                currentProperty.OccupierName = string.IsNullOrWhiteSpace(updatedOccupierName) ? string.Empty : updatedOccupierName;
                currentProperty.OccupierNameEnglish = string.IsNullOrWhiteSpace(updatedOccupierNameEnglish) ? string.Empty : updatedOccupierNameEnglish;

                // RESTORE OLD PROPERTY DATA
                if (dto.IsPreviousDataUpdate)
                {
                    currentProperty.MobileNo =restoreData.MobileNo;
                    currentProperty.Address = restoreData.Address;
                    currentProperty.AddressEnglish = restoreData.AddressEnglish;
                    currentProperty.FlatOrShopNo = restoreData.FlatOrShopNo;
                    currentProperty.FlatOrShopNoEnglish = restoreData.FlatOrShopNoEnglish;
                    currentProperty.FlatOrShopName = restoreData.FlatOrShopName;
                    currentProperty.FlatOrShopNameEnglish = restoreData.FlatOrShopNameEnglish;
                }
                currentProperty.UpdatedBy = dto.UpdatedBy;
                currentProperty.UpdatedDate = updatedDate;

                // RESTORE SOCIETY BUILDER
                if (dto.IsPreviousDataUpdate && currentProperty.SocietyDetailId.HasValue && societyLookup.TryGetValue(currentProperty.SocietyDetailId.Value,out var society))
                {
                    society.BuilderName = restoreData.BuilderName;
                    society.BuilderNameEnglish = restoreData.BuilderNameEnglish;
                    society.UpdatedBy = dto.UpdatedBy;
                    society.UpdatedDate = updatedDate;
                }
            }

            //  FIND PREVIOUS CANCELLED MAPPINGS
            var cancelledPropertyMapDetailIds =
                await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                    .Where(pmd =>
                        pmd.Status == PropertyMapStatus.Cancelled &&
                        pmd.PropertyIdOld == oldPropertyId &&
                        pmd.PropertyIdNew.HasValue &&
                        newPropertyIds.Contains(pmd.PropertyIdNew.Value))
                    .Select(pmd => pmd.Id)
                    .ToListAsync(cancellationToken);

            // DELETE PREVIOUS CANCELLED MAPPINGS
            if (cancelledPropertyMapDetailIds.Count > 0)
            {
                // Child first
                await _mergeDetailRepository
                    .GetQueryable()
                    .Where(md =>
                        cancelledPropertyMapDetailIds.Contains(md.PropertyMapDetailId))
                    .ExecuteDeleteAsync(cancellationToken);

                // Parent second
                await _propertyMapDetailRepository
                    .GetQueryable()
                    .Where(pmd =>
                        cancelledPropertyMapDetailIds.Contains(pmd.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            //  CANCEL ALL SELECTED ACTIVE MAPPINGS
            var updatedMappingCount =
                await _propertyMapDetailRepository
                    .GetQueryable()
                    .Where(pmd =>
                        pmd.PropertyIdOld == oldPropertyId &&
                        pmd.PropertyIdNew.HasValue &&
                        newPropertyIds.Contains(pmd.PropertyIdNew.Value) &&
                        pmd.IsActive &&
                        pmd.Status ==
                            PropertyMapStatus.Active)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(pmd => pmd.Status,PropertyMapStatus.Cancelled)
                            .SetProperty(pmd => pmd.IsActive,false)
                            .SetProperty(pmd => pmd.UpdatedBy,dto.UpdatedBy)
                            .SetProperty(pmd => pmd.UpdatedDate,updatedDate),
                        cancellationToken);


            if (updatedMappingCount != newPropertyIds.Count)
            {
                throw new InvalidOperationException($"Demerge failed. Expected to update {newPropertyIds.Count} mapping(s), but updated {updatedMappingCount}.");
            }

            // DEACTIVATE MERGE DETAIL ROWS
            var updatedMergeDetailCount =
                await _mergeDetailRepository
                    .GetQueryable()
                    .Where(md =>
                        propertyMapDetailIds.Contains(md.PropertyMapDetailId) &&
                        md.IsActive)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty( md => md.IsActive,false)
                            .SetProperty( md => md.UpdatedBy, dto.UpdatedBy)
                            .SetProperty( md => md.UpdatedDate, updatedDate),
                        cancellationToken);

            if (updatedMergeDetailCount == 0)
            {
                throw new InvalidOperationException("Demerge failed. Merge details were not updated.");
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            //  CHECK REMAINING ACTIVE MAPPINGS
            var remainingMappings = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                    .Where(pmd =>
                        pmd.PropertyIdOld == oldPropertyId &&
                        pmd.PropertyIdNew.HasValue &&
                        pmd.IsActive &&
                        pmd.Status == PropertyMapStatus.Active)
                    .Select(pmd => new
                    {
                        pmd.Id,
                        PropertyIdNew = pmd.PropertyIdNew!.Value
                    })
                    .ToListAsync(cancellationToken);

            //  IF ONLY ONE ACTIVE MAPPING REMAINS
            if (remainingMappings.Count == 1)
            {
                var oneToOnePropertyMapId =
                    await _propertyMapMasterRepository.GetQueryable().AsNoTracking()
                        .Where(x =>
                            x.IsActive &&
                            x.MappingCategory == PropertyMappingCategory.OneToOneMappingCategory)
                        .Select(x => x.Id)
                        .FirstOrDefaultAsync(cancellationToken);

                if (oneToOnePropertyMapId <= 0)
                {
                    throw new InvalidOperationException("ONE_TO_ONE mapping category not found");
                }

                var remainingMapDetailId =remainingMappings[0].Id;
                await _propertyMapDetailRepository.GetQueryable()
                    .Where(pmd =>
                        pmd.Id == remainingMapDetailId &&
                        pmd.IsActive &&
                        pmd.Status ==
                            PropertyMapStatus.Active)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(pmd => pmd.PropertyMapId,oneToOnePropertyMapId)
                            .SetProperty(pmd => pmd.UpdatedBy,dto.UpdatedBy)
                            .SetProperty(pmd => pmd.UpdatedDate,updatedDate),
                        cancellationToken);
            }

            //  RESPONSE VALUES
            var oldPropertyNo = validationQuery.Select(x => x.PropertyNoOld).FirstOrDefault(x =>!string.IsNullOrWhiteSpace(x));
            var newPropertyNos = validationQuery.Select(x => x.PropertyNoNew).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return new PropertySplitDto
            {
                Success = true,
                Message =$"New properties {string.Join(", ", newPropertyNos)} demerged successfully from old property no : {oldPropertyNo}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"Split demerge failed OldProperty:{OldPropertyId} NewProperties:{NewPropertyIds}",dto.PropertyOldId,dto.PropertyIds != null ? string.Join(",", dto.PropertyIds) : null);    
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
    private static string BuildPropertyNumber(params string?[] propertyNumberParts)
    {
        return string.Join("-", propertyNumberParts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));
    }

    private static string? BuildMergedPersonName(string? newPropertyName, string? oldPropertyName)
    {
        var names = new List<string>();
        void AddNames(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }
            var splitNames = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var splitName in splitNames)
            {
                var name = splitName.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }
                // Remove placeholder value
                if (string.Equals(name, "The Holder", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                // Avoid duplicate names
                var alreadyExists = names.Any(existingName => string.Equals(existingName, name, StringComparison.OrdinalIgnoreCase));
                if (!alreadyExists)
                {
                    names.Add(name);
                }
            }
        }
        // Keep existing new-property name first
        AddNames(newPropertyName);
        // Append old-property name
        AddNames(oldPropertyName);
        return names.Count > 0 ? string.Join(", ", names) : null;
    }
    private static string RemoveOwnerNameFromCommaSeparated(string? currentOwnerName, string? oldOwnerName)
    {
        if (string.IsNullOrWhiteSpace(currentOwnerName))
            return string.Empty;
        if (string.IsNullOrWhiteSpace(oldOwnerName))
            return currentOwnerName.Trim();

        var currentNames = currentOwnerName.Split(',')
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        var oldNames = oldOwnerName.Split(',')
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        currentNames.RemoveAll(x =>
            oldNames.Any(old => string.Equals(x, old, StringComparison.OrdinalIgnoreCase)));

        var result = string.Join(", ", currentNames);
        return result;
    }
}
