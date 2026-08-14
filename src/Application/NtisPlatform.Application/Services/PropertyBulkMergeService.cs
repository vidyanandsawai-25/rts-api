using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Constants;
using NtisPlatform.Application.DTOs.PropertyBulkMerge;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

public class PropertyBulkMergeService : BaseCommonCrudService<PropertyMapDetailEntity, PropertyBulkMergeDto, CreatePropertyBulkMergeDto, UpdatePropertyBulkMergeDto, PropertyBulkMergeQueryParameters, int>, IPropertyBulkMergeService
{
    private readonly IRepository<PropertyMapMasterEntity, int> _propertyMapMasterRepository;
    private readonly IRepository<PropertyMastOldEntity, int> _propertyOldRepository;
    private readonly IRepository<PropertyMapDetailEntity, int> _propertyMapDetailRepository;
    private readonly new IRepository<PropertyEntity, int> _repository;
    private readonly IRepository<WardEntity, int> _wardRepository;
    private readonly IRepository<SocietyDetailsEntity, int> _societyRepository;
    private readonly IRepository<MergeDetailEntity, int> _mergeDetailRepository;
    private readonly new IUnitOfWork _unitOfWork;
    private readonly ILogger<PropertyBulkMergeService> _logger;
    private readonly IMapper _mapper;

    public PropertyBulkMergeService(
        IRepository<PropertyMapMasterEntity, int> propertyMapMasterRepository,
        IRepository<PropertyMastOldEntity, int> propertyOldRepository,
        IRepository<PropertyMapDetailEntity, int> propertyMapDetailRepository,
        IRepository<PropertyEntity, int> repository,
        IRepository<WardEntity, int> wardRepository,
        IRepository<SocietyDetailsEntity, int> societyRepository,
        IRepository<MergeDetailEntity, int> mergeDetailRepository,
        IUnitOfWork unitOfWork,
        ILogger<PropertyBulkMergeService> logger,
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

    public override async Task<PropertyBulkMergeDto> CreateAsync(CreatePropertyBulkMergeDto dto, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (dto == null)
            {
                throw new ValidationException("Property", "Invalid Request", OperationType.Create);
            }

            if (dto.PropertyIdList == null || dto.PropertyIdList.Count == 0)
            {
                throw new ValidationException("Property", "Invalid Request", OperationType.Create);
            }

            if (dto.CreatedBy <= 0)
            {
                throw new ValidationException("User", "Invalid User", OperationType.Create);
            }

            // Prepare request data
            var propertyPairs = dto.PropertyIdList.Select(x => new
            {
                x.PropertyId,
                x.PropertyOldId
            }).ToList();

            // Validate positive IDs.
            if (propertyPairs.Any(x => x.PropertyId <= 0 || x.PropertyOldId <= 0))
            {
                throw new ValidationException("Property", "Invalid property data found", OperationType.Create);
            }

            // Duplicate NEW Property validation
            var duplicateNewPropertyIds = propertyPairs.GroupBy(x => x.PropertyId).Where(x => x.Count() > 1).Select(x => x.Key).ToList();

            if (duplicateNewPropertyIds.Count > 0)
            {
                throw new ValidationException("New Property", $"Duplicate new property found", OperationType.Create);
            }

            // Duplicate OLD Property validation
            var duplicateOldPropertyIds = propertyPairs.GroupBy(x => x.PropertyOldId).Where(x => x.Count() > 1).Select(x => x.Key).ToList();

            if (duplicateOldPropertyIds.Count > 0)
            {
                throw new ValidationException("Old Property", $"Duplicate old property  found", OperationType.Create);
            }

            //exact duplicate pair is also automatically rejected.
            var oldPropertyIds = propertyPairs.Select(x => x.PropertyOldId).ToList();
            var newPropertyIds = propertyPairs.Select(x => x.PropertyId).ToList();

            decimal? latitude = decimal.TryParse(dto.Latitude, out var lat) ? lat : null;
            decimal? longitude = decimal.TryParse(dto.Longitude, out var lon) ? lon : null;

            // Load Mapping Categories - ONE query
            var mappingCategories = await _propertyMapMasterRepository.GetQueryable().AsNoTracking()
                    .Where(x =>
                        x.IsActive &&
                        (
                            x.MappingCategory == PropertyMappingCategory.OneToOneMappingCategory ||
                            x.MappingCategory == PropertyMappingCategory.MergeMappingCategory
                        ))
                    .Select(x => new
                    {
                        x.Id,
                        x.MappingCategory
                    }).ToListAsync(cancellationToken);

            var oneToOneMapId = mappingCategories
                .Where(x => x.MappingCategory == PropertyMappingCategory.OneToOneMappingCategory)
                .Select(x => x.Id).FirstOrDefault();

            if (oneToOneMapId <= 0)
            {
                throw new ValidationException("Property Map Category", $"{PropertyMappingCategory.OneToOneMappingCategory} property mapping category not found", OperationType.Create);
            }

            var mergeMapId = mappingCategories
                .Where(x => x.MappingCategory == PropertyMappingCategory.MergeMappingCategory)
                .Select(x => x.Id).FirstOrDefault();

            // Load ALL Old Properties - ONE query
            var oldProperties = await _propertyOldRepository.GetQueryable().AsNoTracking()
                    .Where(x =>
                        oldPropertyIds.Contains(x.Id) &&
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
                    .ToListAsync(cancellationToken);

            if (oldProperties.Count != oldPropertyIds.Count)
            {
                throw new ValidationException("Old Property", "One or more old properties not found", OperationType.Create);
            }

            // Load ALL New Properties - ONE query
            var newProperties = await (
                from property in _repository.GetQueryable().AsNoTracking()
                join ward in _wardRepository.GetQueryable().AsNoTracking()
                    on property.WardId equals ward.Id
                join society in _societyRepository.GetQueryable().AsNoTracking()
                    on property.SocietyDetailId equals society.Id
                    into societyGroup
                from society in societyGroup.DefaultIfEmpty()
                where newPropertyIds.Contains(property.Id)
                      && property.IsActive
                      && !property.MarkedForDeletion
                select new
                {
                    Entity = property,
                    ward.WardNo,
                    BuilderName = society != null ? society.BuilderName : null,
                    BuilderNameEnglish = society != null ? society.BuilderNameEnglish : null
                })
                .ToListAsync(cancellationToken);

            if (newProperties.Count != newPropertyIds.Count)
            {
                throw new ValidationException("New Property", "One or more new properties not found", OperationType.Create);
            }

            var oldPropertyLookup = oldProperties.ToDictionary(x => x.Id);
            var newPropertyLookup = newProperties.ToDictionary(x => x.Entity.Id);

            // Load existing mappings - ONE query
            var existingMappings =
                await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                    .Where(x =>
                        x.IsActive && x.Status == PropertyMapStatus.Active &&
                        (
                            (x.PropertyIdOld.HasValue && oldPropertyIds.Contains(x.PropertyIdOld.Value)) ||
                            (x.PropertyIdNew.HasValue && newPropertyIds.Contains(x.PropertyIdNew.Value))
                        ))
                    .Select(x => new
                    {
                        x.PropertyIdOld,
                        x.PropertyIdNew,
                        x.PropertyNoOld,
                        x.PropertyNoNew
                    })
                    .ToListAsync(cancellationToken);

            // Validate Old Properties Already Merged
            var alreadyMergedOldProperties = existingMappings.Where(x => x.PropertyIdOld.HasValue && oldPropertyIds.Contains(x.PropertyIdOld.Value)).ToList();
            if (alreadyMergedOldProperties.Count > 0)
            {
                var alreadyMergedMessage = string.Join(", ", alreadyMergedOldProperties.Select(x => $"{x.PropertyNoOld} -> {x.PropertyNoNew}"));
                throw new ValidationException("Old Property", $"Old properties already merged: {alreadyMergedMessage}", OperationType.Create);
            }

            // Existing New Properties
            var existingMappedNewIds = existingMappings
                    .Where(x => x.PropertyIdNew.HasValue &&
                        newPropertyIds.Contains(x.PropertyIdNew.Value))
                    .Select(x => x.PropertyIdNew!.Value).ToHashSet();

            // No request-level duplicate NewProperty logic anymore.
            var mergePropertyIds = new HashSet<int>(existingMappedNewIds);

            if (mergePropertyIds.Count > 0 && mergeMapId <= 0)
            {
                throw new ValidationException("Property Map Category", $"{PropertyMappingCategory.MergeMappingCategory} property mapping category not found", OperationType.Create);
            }

            // Convert existing mappings to MergeMappingCategory
            if (existingMappedNewIds.Count > 0)
            {
                await _propertyMapDetailRepository.GetQueryable()
                    .Where(x =>
                        x.PropertyIdNew.HasValue &&
                        existingMappedNewIds.Contains(x.PropertyIdNew.Value) &&
                        x.IsActive && x.Status == PropertyMapStatus.Active)
                    .ExecuteUpdateAsync(
                        set => set
                            .SetProperty(x => x.PropertyMapId, mergeMapId),
                        cancellationToken);
            }

            // Update PropertyMast
            var now = DateTime.Now;
            foreach (var pair in propertyPairs)
            {
                var oldProperty = oldPropertyLookup[pair.PropertyOldId];
                var newProperty = newPropertyLookup[pair.PropertyId].Entity;

                var mergedOwnerName = BuildMergedPersonName(newProperty.OwnerName, oldProperty.OldOwnerName);
                var mergedOwnerNameEnglish = BuildMergedPersonName(newProperty.OwnerNameEnglish, oldProperty.OldOwnerNameEnglish);
                var mergedOccupierName = BuildMergedPersonName(newProperty.OccupierName, oldProperty.OldOccupierName);
                var mergedOccupierNameEnglish = BuildMergedPersonName(newProperty.OccupierNameEnglish, oldProperty.OldOccupierNameEnglish);

                var mobileNo = !string.IsNullOrWhiteSpace(oldProperty.OldMobileNo) ? oldProperty.OldMobileNo : newProperty.MobileNo;
                var address = !string.IsNullOrWhiteSpace(oldProperty.OldAddress) ? oldProperty.OldAddress : newProperty.Address;
                var addressEnglish = !string.IsNullOrWhiteSpace(oldProperty.OldAddressEnglish) ? oldProperty.OldAddressEnglish : newProperty.AddressEnglish;
                var flatOrShopNo = !string.IsNullOrWhiteSpace(oldProperty.OldFlatOrShopNumber) ? oldProperty.OldFlatOrShopNumber : newProperty.FlatOrShopNo;

                var finalOwnerName = mergedOwnerName;
                var finalOwnerNameEnglish = mergedOwnerNameEnglish;
                var finalOccupierName = mergedOccupierName;
                var finalOccupierNameEnglish = mergedOccupierNameEnglish;
                var finalMobileNo = mobileNo;
                var finalAddress = address;
                var finalAddressEnglish = addressEnglish;
                var finalFlatOrShopNo = flatOrShopNo;

                await _repository.GetQueryable()
                    .Where(x =>
                        x.Id == pair.PropertyId && x.IsActive && !x.MarkedForDeletion)
                    .ExecuteUpdateAsync(
                        set => set
                            .SetProperty(x => x.OwnerName, x => !string.IsNullOrWhiteSpace(finalOwnerName) ? finalOwnerName : x.OwnerName)
                            .SetProperty(x => x.OwnerNameEnglish, x => !string.IsNullOrWhiteSpace(finalOwnerNameEnglish) ? finalOwnerNameEnglish : x.OwnerNameEnglish)
                            .SetProperty(x => x.OccupierName, x => !string.IsNullOrWhiteSpace(finalOccupierName) ? finalOccupierName : x.OccupierName)
                            .SetProperty(x => x.OccupierNameEnglish, x => !string.IsNullOrWhiteSpace(finalOccupierNameEnglish) ? finalOccupierNameEnglish : x.OccupierNameEnglish)

                            .SetProperty(x => x.MobileNo, x => dto.IsOldDataUpdate && !string.IsNullOrWhiteSpace(finalMobileNo) ? finalMobileNo : x.MobileNo)
                            .SetProperty(x => x.Address, x => dto.IsOldDataUpdate && !string.IsNullOrWhiteSpace(finalAddress) ? finalAddress : x.Address)
                            .SetProperty(x => x.AddressEnglish, x => dto.IsOldDataUpdate && !string.IsNullOrWhiteSpace(finalAddressEnglish) ? finalAddressEnglish : x.AddressEnglish)
                            .SetProperty(x => x.FlatOrShopNo, x => dto.IsOldDataUpdate && !string.IsNullOrWhiteSpace(finalFlatOrShopNo) ? finalFlatOrShopNo : x.FlatOrShopNo)
                            .SetProperty(x => x.UpdatedBy, dto.CreatedBy)
                            .SetProperty(x => x.UpdatedDate, now),
                        cancellationToken);
            }

            // Build PropertyMapDetail + MergeDetail
            var propertyMapDetails = new List<PropertyMapDetailEntity>(propertyPairs.Count);
            var mergeMessages = new List<string>(propertyPairs.Count);

            foreach (var pair in propertyPairs)
            {
                var oldProperty = oldPropertyLookup[pair.PropertyOldId];
                var newPropertyInfo = newPropertyLookup[pair.PropertyId];
                var newProperty = newPropertyInfo.Entity;
                var oldPropertyNo = BuildPropertyNumber(oldProperty.OldWardNo, oldProperty.OldPropertyNo, oldProperty.OldPartitionNo);
                var newPropertyNo = BuildPropertyNumber(newPropertyInfo.WardNo, newProperty.PropertyNo, newProperty.PartitionNo);

                // If New Property was already mapped in DB, use MergeMappingCategory. Otherwise this remains One-To-One.
                var propertyMapId = mergePropertyIds.Contains(pair.PropertyId) ? mergeMapId : oneToOneMapId;

                var propertyMapDetail = new PropertyMapDetailEntity
                {
                    PropertyMapId = propertyMapId,
                    PropertyIdNew = pair.PropertyId,
                    PropertyIdOld = pair.PropertyOldId,
                    PropertyNoNew = newPropertyNo,
                    PropertyNoOld = oldPropertyNo,
                    Status = PropertyMapStatus.Active,
                    Remark = "Property Merged - Multiple Property",
                    Latitude = latitude,
                    Longitude = longitude,
                    Location = dto.Location,
                    CreatedBy = dto.CreatedBy,

                    // Original New Property data.
                    MergeDetail = new MergeDetailEntity
                    {
                        OwnerName = newProperty.OwnerName,
                        OwnerNameEnglish = newProperty.OwnerNameEnglish,
                        OccupierName = newProperty.OccupierName,
                        OccupierNameEnglish = newProperty.OccupierNameEnglish,
                        MobileNo = newProperty.MobileNo,
                        Address = newProperty.Address,
                        AddressEnglish = newProperty.AddressEnglish,
                        FlatOrShopNo = newProperty.FlatOrShopNo,
                        FlatOrShopNoEnglish = newProperty.FlatOrShopNoEnglish,
                        FlatOrShopName = newProperty.FlatOrShopName,
                        FlatOrShopNameEnglish = newProperty.FlatOrShopNameEnglish,
                        BuilderName = newPropertyInfo.BuilderName,
                        BuilderNameEnglish = newPropertyInfo.BuilderNameEnglish,
                        CreatedBy = dto.CreatedBy
                    }
                };

                propertyMapDetails.Add(propertyMapDetail);
                mergeMessages.Add($"{oldPropertyNo} -> {newPropertyNo}");
            }

            // Bulk Insert PropertyMapDetail + MergeDetail
            await _propertyMapDetailRepository.AddRangeAsync(propertyMapDetails, cancellationToken);
            var affectedRecords = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (affectedRecords <= 0)
            {
                throw new ValidationException("Property", "Property data not merged", OperationType.Create);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return new PropertyBulkMergeDto
            {
                Success = true,
                Message = $"Properties merged successfully. " + string.Join(", ", mergeMessages)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Merge multiple properties failed. Count:{Count}", dto?.PropertyIdList?.Count ?? 0);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public override async Task<PropertyBulkMergeDto?> UpdateAsync(int id, UpdatePropertyBulkMergeDto dto, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (dto == null)
            {
                throw new ValidationException("Property","Invalid request",OperationType.Update);
            }

            if (dto.PropertyIdList == null || dto.PropertyIdList.Count == 0)
            {
                throw new ValidationException("Property","At least one property pair is required",OperationType.Update);
            }

            if (dto.UpdatedBy <= 0)
            {
                throw new ValidationException("User","Invalid user",OperationType.Update);
            }

            // Prepare request DO NOT use Distinct(). Duplicate NewId / OldId / pair must be rejected.
            var propertyPairs = dto.PropertyIdList.Select(x => new
                {
                    x.PropertyId,
                    x.PropertyOldId
                }).ToList();

            if (propertyPairs.Any(x => x.PropertyId <= 0 || x.PropertyOldId <= 0))
            {
                throw new ValidationException("Property","Invalid property data found",OperationType.Update);
            }

            // Duplicate NEW property validation
            var duplicateNewPropertyIds = propertyPairs.GroupBy(x => x.PropertyId).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
            if (duplicateNewPropertyIds.Count > 0)
            {
                throw new ValidationException("New Property",$"Duplicate new property found",OperationType.Update);
            }

            // Duplicate OLD property validation
            var duplicateOldPropertyIds = propertyPairs.GroupBy(x => x.PropertyOldId).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
            if (duplicateOldPropertyIds.Count > 0)
            {
                throw new ValidationException("Old Property",$"Duplicate old property found",OperationType.Update);
            }

            // Because both NewId and OldId are unique, exact duplicate pairs are automatically rejected.
            var oldPropertyIds = propertyPairs.Select(x => x.PropertyOldId).ToList();
            var newPropertyIds = propertyPairs.Select(x => x.PropertyId).ToList();
            var now = DateTime.Now;

           // Load Old Properties - ONE query
            var oldProperties = await _propertyOldRepository.GetQueryable().AsNoTracking()
                .Where(x =>
                    oldPropertyIds.Contains(x.Id) && x.IsActive && !x.MarkedForDeletion)
                .Select(x => new
                {
                    x.Id,
                    x.OldWardNo,
                    x.OldPropertyNo,
                    x.OldPartitionNo,
                    x.OldOwnerName,
                    x.OldOwnerNameEnglish,
                    x.OldOccupierName,
                    x.OldOccupierNameEnglish
                })
                .ToListAsync(cancellationToken);

            if (oldProperties.Count != oldPropertyIds.Count)
            {
                throw new ValidationException("Old Property","One or more old properties not found",OperationType.Update);
            }

            var oldPropertyLookup = oldProperties.ToDictionary(x => x.Id);

           
            // Load current New Properties - ONE query
            var newProperties = await (
                from property in _repository.GetQueryable().AsNoTracking()
                join ward in _wardRepository.GetQueryable().AsNoTracking()
                    on property.WardId equals ward.Id
                where newPropertyIds.Contains(property.Id)
                      && property.IsActive
                      && !property.MarkedForDeletion
                select new
                {
                    property.Id,
                    property.OwnerName,
                    property.OwnerNameEnglish,
                    property.OccupierName,
                    property.OccupierNameEnglish,
                    property.SocietyDetailId,
                    ward.WardNo,
                    property.PropertyNo,
                    property.PartitionNo
                })
                .ToListAsync(cancellationToken);

            if (newProperties.Count != newPropertyIds.Count)
            {
                throw new ValidationException("New Property","One or more new properties not found",OperationType.Update);
            }

            var newPropertyLookup = newProperties.ToDictionary(x => x.Id);

            // Load ACTIVE mappings for exact requested pairs
            var activeMappings = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                .Where(x =>
                    x.PropertyIdOld.HasValue &&
                    x.PropertyIdNew.HasValue &&
                    oldPropertyIds.Contains(x.PropertyIdOld.Value) &&
                    newPropertyIds.Contains(x.PropertyIdNew.Value) &&
                    x.IsActive && x.Status == PropertyMapStatus.Active)
                .Select(x => new
                {
                    x.Id,
                    x.PropertyMapId,
                    PropertyIdOld = x.PropertyIdOld!.Value,
                    PropertyIdNew = x.PropertyIdNew!.Value,
                    x.PropertyNoOld,
                    x.PropertyNoNew
                })
                .ToListAsync(cancellationToken);

            var mappingByPair = activeMappings.GroupBy(x => (x.PropertyIdOld,x.PropertyIdNew))
                .ToDictionary(x => x.Key,x => x.ToList());

            var selectedMappings = new List<dynamic>();
            var selectedMappingIds = new List<int>(propertyPairs.Count);
            var selectedMappingInfo = new Dictionary<(int OldId, int NewId),(int MappingId, string? OldNo, string? NewNo)>();
            var missingPairs = new List<string>();
            var operationDetails = new List<string>(propertyPairs.Count);

            foreach (var pair in propertyPairs)
            {
                if (!mappingByPair.TryGetValue((pair.PropertyOldId, pair.PropertyId),out var mappings) || mappings.Count == 0)
                {
                    var oldProperty = oldPropertyLookup[pair.PropertyOldId];
                    var newProperty = newPropertyLookup[pair.PropertyId];
                    var oldNo = BuildPropertyNumber(oldProperty.OldWardNo,oldProperty.OldPropertyNo,oldProperty.OldPartitionNo);
                    var newNo = BuildPropertyNumber(newProperty.WardNo,newProperty.PropertyNo,newProperty.PartitionNo);
                    missingPairs.Add($"{oldNo} -> {newNo}");
                    continue;
                }

                // Normally one active mapping should exist for a pair.
                foreach (var mapping in mappings)
                {
                    selectedMappingIds.Add(mapping.Id);
                }

                var firstMapping = mappings[0];
                selectedMappingInfo[(pair.PropertyOldId, pair.PropertyId)] = (firstMapping.Id,firstMapping.PropertyNoOld,firstMapping.PropertyNoNew);

                operationDetails.Add($"{firstMapping.PropertyNoOld} -> {firstMapping.PropertyNoNew}");
            }

            if (missingPairs.Count > 0)
            {
                throw new ValidationException("Property",$"Property merging details not found for property no: {string.Join(", ", missingPairs)}",OperationType.Update);
            }

            selectedMappingIds = selectedMappingIds.Distinct().ToList();

            if (selectedMappingIds.Count == 0)
            {
                throw new ValidationException("Property","No property merging records found to demerge",OperationType.Update);
            }

           // Load MergeDetail snapshots - ONE query
            var mergeDetails = await _mergeDetailRepository.GetQueryable().AsNoTracking()
                .Where(x => selectedMappingIds.Contains(x.PropertyMapDetailId) && x.IsActive)
                .Select(x => new
                {
                    x.PropertyMapDetailId,
                    x.MobileNo,
                    x.Address,
                    x.AddressEnglish,
                    x.FlatOrShopNo,
                    x.FlatOrShopNoEnglish,
                    x.FlatOrShopName,
                    x.FlatOrShopNameEnglish,
                    x.BuilderName,
                    x.BuilderNameEnglish
                })
                .ToListAsync(cancellationToken);

            if (mergeDetails.Count == 0)
            {
                throw new ValidationException("Merge Details","Original property data not found",OperationType.Update);
            }

            var mergeDetailLookup = mergeDetails.GroupBy(x => x.PropertyMapDetailId).ToDictionary(x => x.Key,x => x.First());

            // Restore/remove PropertyMast values
            foreach (var pair in propertyPairs)
            {
                var oldProperty = oldPropertyLookup[pair.PropertyOldId];
                var currentProperty = newPropertyLookup[pair.PropertyId];
                var mapping = selectedMappingInfo[(pair.PropertyOldId, pair.PropertyId)];
                if (!mergeDetailLookup.TryGetValue(mapping.MappingId, out var restoreData))
                {
                    throw new ValidationException("Merge Details",$"Original property data not found for {mapping.OldNo} -> {mapping.NewNo}",OperationType.Update);
                }

                // Remove old property's merged names from current new-property values.
                var updatedOwnerName = RemoveOwnerNameFromCommaSeparated(currentProperty.OwnerName,oldProperty.OldOwnerName);
                var updatedOwnerNameEnglish = RemoveOwnerNameFromCommaSeparated(currentProperty.OwnerNameEnglish,oldProperty.OldOwnerNameEnglish);
                var updatedOccupierName = RemoveOwnerNameFromCommaSeparated(currentProperty.OccupierName,oldProperty.OldOccupierName);
                var updatedOccupierNameEnglish = RemoveOwnerNameFromCommaSeparated(currentProperty.OccupierNameEnglish,oldProperty.OldOccupierNameEnglish);

                var finalOwnerName = string.IsNullOrWhiteSpace(updatedOwnerName) ? "The Holder" : updatedOwnerName;
                var finalOwnerNameEnglish = string.IsNullOrWhiteSpace(updatedOwnerNameEnglish) ? "The Holder" : updatedOwnerNameEnglish;

                if (dto.IsPreviousDataUpdate)
                {
                    await _repository.GetQueryable()
                        .Where(x =>
                            x.Id == pair.PropertyId &&
                            x.IsActive && !x.MarkedForDeletion)
                        .ExecuteUpdateAsync(
                            setters => setters
                                .SetProperty(x => x.OwnerName, finalOwnerName)
                                .SetProperty(x => x.OwnerNameEnglish, finalOwnerNameEnglish)
                                .SetProperty(x => x.OccupierName, updatedOccupierName)
                                .SetProperty(x => x.OccupierNameEnglish, updatedOccupierNameEnglish)

                                .SetProperty(x => x.MobileNo, restoreData.MobileNo)
                                .SetProperty(x => x.Address, restoreData.Address)
                                .SetProperty(x => x.AddressEnglish, restoreData.AddressEnglish)
                                .SetProperty(x => x.FlatOrShopNo, restoreData.FlatOrShopNo)
                                .SetProperty(x => x.FlatOrShopNoEnglish, restoreData.FlatOrShopNoEnglish)
                                .SetProperty(x => x.FlatOrShopName, restoreData.FlatOrShopName)
                                .SetProperty(x => x.FlatOrShopNameEnglish, restoreData.FlatOrShopNameEnglish)
                                .SetProperty(x => x.UpdatedBy, dto.UpdatedBy)
                                .SetProperty(x => x.UpdatedDate, now),
                            cancellationToken);

                    // Same Society restore logic as Single UpdateAsync.
                    if (currentProperty.SocietyDetailId.HasValue)
                    {
                        await _societyRepository.GetQueryable()
                            .Where(x => x.Id == currentProperty.SocietyDetailId.Value && x.IsActive)
                            .ExecuteUpdateAsync(
                                setters => setters
                                    .SetProperty(x => x.BuilderName, restoreData.BuilderName)
                                    .SetProperty(x => x.BuilderNameEnglish, restoreData.BuilderNameEnglish)
                                    .SetProperty(x => x.UpdatedBy, dto.UpdatedBy)
                                    .SetProperty(x => x.UpdatedDate, now),
                                cancellationToken);
                    }
                }
                else
                {
                    await _repository.GetQueryable()
                        .Where(x =>
                            x.Id == pair.PropertyId &&
                            x.IsActive && !x.MarkedForDeletion)
                        .ExecuteUpdateAsync(
                            setters => setters
                                .SetProperty(x => x.OwnerName, finalOwnerName)
                                .SetProperty(x => x.OwnerNameEnglish, finalOwnerNameEnglish)
                                .SetProperty(x => x.OccupierName,updatedOccupierName)
                                .SetProperty(x => x.OccupierNameEnglish, updatedOccupierNameEnglish)
                                .SetProperty(x => x.UpdatedBy, dto.UpdatedBy)
                                .SetProperty(x => x.UpdatedDate, now),
                            cancellationToken);
                }
            }

            // Find previous CANCELLED mappings
            var cancelledMappings = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                .Where(x =>
                    x.PropertyIdOld.HasValue &&
                    x.PropertyIdNew.HasValue &&
                    oldPropertyIds.Contains(x.PropertyIdOld.Value) &&
                    newPropertyIds.Contains(x.PropertyIdNew.Value) &&
                    x.Status == PropertyMapStatus.Cancelled)
                .Select(x => new
                {
                    x.Id,
                    PropertyIdOld = x.PropertyIdOld!.Value,
                    PropertyIdNew = x.PropertyIdNew!.Value
                }).ToListAsync(cancellationToken);

            // Only delete exact requested pairs.
            var requestedPairSet = propertyPairs.Select(x => (x.PropertyOldId,x.PropertyId)).ToHashSet();

            var cancelledMappingIds = cancelledMappings
                .Where(x => requestedPairSet.Contains(
                        (
                            x.PropertyIdOld,
                            x.PropertyIdNew
                        )))
                .Select(x => x.Id).ToList();

            if (cancelledMappingIds.Count > 0)
            {
                // Delete child records FIRST.
                await _mergeDetailRepository.GetQueryable()
                    .Where(x =>
                        cancelledMappingIds.Contains(x.PropertyMapDetailId))
                    .ExecuteDeleteAsync(cancellationToken);

                // Then parent mappings.
                await _propertyMapDetailRepository.GetQueryable().Where(x => cancelledMappingIds.Contains(x.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

           // Cancel ALL requested active mappings
            var updatedCount =
                await _propertyMapDetailRepository.GetQueryable()
                    .Where(x =>
                        selectedMappingIds.Contains(x.Id) &&
                        x.IsActive && x.Status == PropertyMapStatus.Active)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(x => x.Status,PropertyMapStatus.Cancelled)
                            .SetProperty(x => x.IsActive, false)
                            .SetProperty(x => x.UpdatedBy, dto.UpdatedBy)
                            .SetProperty(x => x.UpdatedDate, now),
                        cancellationToken);

            if (updatedCount == 0)
            {
                throw new ValidationException("Property","Demerge failed. No mapping updated.",OperationType.Update);
            }

            // Deactivate MergeDetail - ONE bulk UPDATE
            await _mergeDetailRepository.GetQueryable()
                .Where(x =>
                    selectedMappingIds.Contains(x.PropertyMapDetailId) && x.IsActive)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.IsActive, false)
                        .SetProperty(x => x.UpdatedBy, dto.UpdatedBy)
                        .SetProperty(x => x.UpdatedDate, now),
                    cancellationToken);

            // Find remaining ACTIVE mappings - ONE query
            var remainingMappings = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                    .Where(x =>
                        x.PropertyIdNew.HasValue &&
                        newPropertyIds.Contains(x.PropertyIdNew.Value) &&
                        x.IsActive && x.Status == PropertyMapStatus.Active)
                    .Select(x => new
                    {
                        x.Id,
                        PropertyIdNew = x.PropertyIdNew!.Value
                    })
                    .ToListAsync(cancellationToken);

           //  Find New Properties with exactly ONE remaining mapping
            var oneRemainingNewPropertyIds = remainingMappings.GroupBy(x => x.PropertyIdNew).Where(x => x.Count() == 1).Select(x => x.Key).ToHashSet();
            if (oneRemainingNewPropertyIds.Count > 0)
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
                    throw new ValidationException("Property Map Category", $"{PropertyMappingCategory.OneToOneMappingCategory} property mapping category not found",OperationType.Update);
                }

                await _propertyMapDetailRepository.GetQueryable()
                    .Where(x =>
                        x.PropertyIdNew.HasValue &&
                        oneRemainingNewPropertyIds.Contains(x.PropertyIdNew.Value) &&
                        x.IsActive && x.Status == PropertyMapStatus.Active)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(x => x.PropertyMapId, oneToOnePropertyMapId)
                            .SetProperty(x => x.UpdatedBy,dto.UpdatedBy)
                            .SetProperty(x => x.UpdatedDate, now),
                        cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return new PropertyBulkMergeDto
            {
                Success = true,
                Message ="Properties demerged successfully. " + string.Join(", ",operationDetails)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"Error demerging multiple properties in bulk. Count:{Count}",dto?.PropertyIdList?.Count ?? 0);
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
