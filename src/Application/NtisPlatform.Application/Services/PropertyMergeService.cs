using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Constants;
using NtisPlatform.Application.DTOs.PropertyMergeDetails;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class PropertyMergeService : BaseCommonCrudService<PropertyMapDetailEntity, PropertyMergeDto, CreatePropertyMergeDto, UpdatePropertyMergeDto, PropertyMergeQueryParameters, int>, IPropertyMergeService
{
    private sealed record PropertyMapCategoryIds(int OneToOne, int Split, int Merge);
    private readonly IRepository<PropertyMapMasterEntity, int> _propertyMapMasterRepository;
    private readonly IRepository<PropertyMastOldEntity, int> _propertyOldRepository;
    private readonly IRepository<PropertyMapDetailEntity, int> _propertyMapDetailRepository;
    private readonly new IRepository<PropertyEntity, int> _repository;
    private readonly IRepository<WardEntity, int> _wardRepository;
    private readonly IRepository<SocietyDetailsEntity, int> _societyRepository;
    private readonly IRepository<MergeDetailEntity, int> _mergeDetailRepository;
    private readonly new IUnitOfWork _unitOfWork;
    private readonly ILogger<PropertyMergeService> _logger;

    public PropertyMergeService(
        IRepository<PropertyMapMasterEntity, int> propertyMapMasterRepository,
        IRepository<PropertyMastOldEntity, int> propertyOldRepository,
        IRepository<PropertyMapDetailEntity, int> propertyMapDetailRepository,
        IRepository<PropertyEntity, int> repository,
        IRepository<WardEntity, int> wardRepository,
        IRepository<SocietyDetailsEntity, int> societyRepository,
        IRepository<MergeDetailEntity, int> mergeDetailRepository,
        IUnitOfWork unitOfWork,
        ILogger<PropertyMergeService> logger,
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
    }


    public override async Task<PropertyMergeDto> CreateAsync(CreatePropertyMergeDto dto, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var propertyMapId = await _propertyMapMasterRepository.GetQueryable().AsNoTracking()
                 .Where(x => x.IsActive && x.MappingCategory == PropertyMappingCategory.MergeMappingCategory)
                 .Select(x => x.Id)
                 .FirstOrDefaultAsync(cancellationToken);

            if (propertyMapId == 0)
            {
                throw new ValidationException("Property Map Category", $"{PropertyMappingCategory.MergeMappingCategory} property mapping category not found", OperationType.Create);
            }

            var oldPropertyIds = dto.PropertyOldIds?.Where(x => x > 0).Distinct().ToList() ?? new List<int>();
            var propertyId = dto.PropertyId;

            if (oldPropertyIds.Count < 2)
            {
                throw new ValidationException("Old Property","Multiple merge requires at least two unique old properties",OperationType.Create);
            }

            //  Load all old properties - ONE DB call
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
                var foundIds = oldProperties.Select(x => x.Id).ToHashSet();
                var missingIds = oldPropertyIds.Where(x => !foundIds.Contains(x));
                throw new ValidationException("Old Property",$"Old properties not found: {string.Join(", ", missingIds)}",OperationType.Create);
            }

            //  Load new property + Society - ONE DB call
            var newProperty = await (
                from pm in _repository.GetQueryable().AsNoTracking()
                join ward in _wardRepository.GetQueryable().AsNoTracking()
                    on pm.WardId equals ward.Id
                join society in _societyRepository.GetQueryable().AsNoTracking()
                    on pm.SocietyDetailId equals society.Id
                    into societyGroup
                from society in societyGroup.DefaultIfEmpty()
                where
                    pm.Id == propertyId &&
                    pm.IsActive && !pm.MarkedForDeletion
                select new
                {
                    pm.Id,
                    pm.SocietyDetailId,
                    ward.WardNo,
                    pm.PropertyNo,
                    pm.PartitionNo,
                    pm.OwnerName,
                    pm.OwnerNameEnglish,
                    pm.OccupierName,
                    pm.OccupierNameEnglish,
                    pm.MobileNo,
                    pm.Address,
                    pm.AddressEnglish,
                    pm.FlatOrShopNo,
                    pm.FlatOrShopNoEnglish,
                    pm.FlatOrShopName,
                    pm.FlatOrShopNameEnglish,
                    BuilderName = society != null ? society.BuilderName : null,
                    BuilderNameEnglish = society != null ? society.BuilderNameEnglish : null
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (newProperty == null)
            {
                throw new ValidationException("New Property","New Property not found",OperationType.Create);
            }

            // Check all old properties already merged - ONE DB call
            var alreadyMerged = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                .Where(x =>
                    x.PropertyIdOld.HasValue &&
                    oldPropertyIds.Contains(x.PropertyIdOld.Value) &&
                    x.IsActive && x.Status == PropertyMapStatus.Active)
                .Select(x => new
                {
                    x.PropertyIdOld,
                    x.PropertyNoOld,
                    x.PropertyNoNew
                })
                .ToListAsync(cancellationToken);

            if (alreadyMerged.Count > 0)
            {
                var oldNos = alreadyMerged.Select(x => x.PropertyNoOld).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct();
                throw new ValidationException("Old Property", $"Old properties already merged: {string.Join(", ", oldNos)}", OperationType.Create);
            }

            var newPropertyNo = BuildPropertyNumber(newProperty.WardNo,newProperty.PropertyNo,newProperty.PartitionNo);
            var now = DateTime.Now;
            var latitude = decimal.TryParse(dto.Latitude, out var lat) ? lat : (decimal?)null;
            var longitude = decimal.TryParse(dto.Longitude, out var lon) ? lon : (decimal?)null;

            //  Preserve input ordering without repeated LINQ joins
            var oldPropertyLookup = oldProperties.ToDictionary(x => x.Id);
            var orderedOldProperties = oldPropertyIds.Select(id => oldPropertyLookup[id]).ToList();

            // Build final PropertyMaster values in memory
            var ownerName = newProperty.OwnerName;
            var ownerNameEnglish = newProperty.OwnerNameEnglish;
            var occupierName = newProperty.OccupierName;
            var occupierNameEnglish = newProperty.OccupierNameEnglish;
            var mobileNo = newProperty.MobileNo;
            var address = newProperty.Address;
            var addressEnglish = newProperty.AddressEnglish;
            var flatOrShopNo = newProperty.FlatOrShopNo;
            var propertyMapDetails = new List<PropertyMapDetailEntity>(orderedOldProperties.Count);
            var oldPropertyNos = new List<string>(orderedOldProperties.Count);

           
            //  Build entities ONLY in memory
            //    NO database call inside loop
            foreach (var oldProperty in orderedOldProperties)
            {
                var oldPropertyNo = BuildPropertyNumber(oldProperty.OldWardNo,oldProperty.OldPropertyNo,oldProperty.OldPartitionNo);
                oldPropertyNos.Add(oldPropertyNo);

                // Snapshot BEFORE merging this particular old property
                var propertyMapDetailSource = new PropertyMapDetailEntity
                {
                    PropertyMapId = propertyMapId,
                    PropertyIdNew = propertyId,
                    PropertyIdOld = oldProperty.Id,
                    PropertyNoNew = newPropertyNo,
                    PropertyNoOld = oldPropertyNo,
                    Status = PropertyMapStatus.Active,
                    Remark = "Property Merged - Multiple Old Properties",
                    Latitude = latitude,
                    Longitude = longitude,
                    Location = dto.Location,
                    CreatedBy = dto.CreatedBy
                };
                var propertyMapDetail = _mapper.Map<PropertyMapDetailEntity>(propertyMapDetailSource);

                var mergeDetailSource = new MergeDetailEntity
                {
                    OwnerName = ownerName,
                    OwnerNameEnglish = ownerNameEnglish,
                    OccupierName = occupierName,
                    OccupierNameEnglish = occupierNameEnglish,
                    MobileNo = mobileNo,
                    Address = address,
                    AddressEnglish = addressEnglish,
                    FlatOrShopNo = flatOrShopNo,
                    FlatOrShopNoEnglish = newProperty.FlatOrShopNoEnglish,
                    FlatOrShopName = newProperty.FlatOrShopName,
                    FlatOrShopNameEnglish = newProperty.FlatOrShopNameEnglish,
                    BuilderName = newProperty.BuilderName,
                    BuilderNameEnglish = newProperty.BuilderNameEnglish,
                    CreatedBy = dto.CreatedBy
                };
                propertyMapDetail.MergeDetail = _mapper.Map<MergeDetailEntity>(mergeDetailSource);
                propertyMapDetails.Add(propertyMapDetail);

                // Update current in-memory state
                ownerName = BuildMergedPersonName(ownerName, oldProperty.OldOwnerName);
                ownerNameEnglish = BuildMergedPersonName(ownerNameEnglish, oldProperty.OldOwnerNameEnglish);
                occupierName = BuildMergedPersonName(occupierName, oldProperty.OldOccupierName);
                occupierNameEnglish = BuildMergedPersonName(occupierNameEnglish, oldProperty.OldOccupierNameEnglish);

                if (!string.IsNullOrWhiteSpace(oldProperty.OldMobileNo))
                {
                    mobileNo = oldProperty.OldMobileNo;
                }
                if (!string.IsNullOrWhiteSpace(oldProperty.OldAddress))
                {
                    address = oldProperty.OldAddress;
                }
                if (!string.IsNullOrWhiteSpace(oldProperty.OldAddressEnglish))
                {
                    addressEnglish = oldProperty.OldAddressEnglish;
                }
                if (!string.IsNullOrWhiteSpace(oldProperty.OldFlatOrShopNumber))
                {
                    flatOrShopNo = oldProperty.OldFlatOrShopNumber;
                }
            }

            //  Convert any existing mapping to MERGE mapping
            await _propertyMapDetailRepository.GetQueryable()
                .Where(x =>
                    x.PropertyIdNew == propertyId &&
                    x.IsActive &&
                    x.Status == PropertyMapStatus.Active &&
                    x.PropertyMapId != propertyMapId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.PropertyMapId,propertyMapId)
                        .SetProperty(x => x.UpdatedBy,dto.CreatedBy)
                        .SetProperty(x => x.UpdatedDate,now),
                    cancellationToken);

            //  Update PropertyMaster - ONE SQL UPDATE
            int updatedCount;
            if (dto.IsOldDataUpdate)
            {
                 updatedCount = await _repository.GetQueryable()
                    .Where(x =>
                        x.Id == propertyId && x.IsActive && !x.MarkedForDeletion)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(x => x.OwnerName, x => !string.IsNullOrWhiteSpace(ownerName) ? ownerName : x.OwnerName)
                            .SetProperty(x => x.OwnerNameEnglish, x => !string.IsNullOrWhiteSpace(ownerNameEnglish) ? ownerNameEnglish : x.OwnerNameEnglish)
                            .SetProperty(x => x.OccupierName, x => !string.IsNullOrWhiteSpace(occupierName) ? occupierName : x.OccupierName)
                            .SetProperty(x => x.OccupierNameEnglish, x => !string.IsNullOrWhiteSpace(occupierNameEnglish) ? occupierNameEnglish : x.OccupierNameEnglish)
                            .SetProperty(x => x.MobileNo, x => !string.IsNullOrWhiteSpace(mobileNo) ? mobileNo : x.MobileNo)
                            .SetProperty(x => x.Address, x => !string.IsNullOrWhiteSpace(address) ? address : x.Address)
                            .SetProperty(x => x.AddressEnglish, x => !string.IsNullOrWhiteSpace(addressEnglish) ? addressEnglish : x.AddressEnglish)
                            .SetProperty(x => x.FlatOrShopNo, x => !string.IsNullOrWhiteSpace(flatOrShopNo) ? flatOrShopNo : x.FlatOrShopNo)
                            .SetProperty(x => x.UpdatedBy, dto.CreatedBy)
                            .SetProperty(x => x.UpdatedDate, now),
                        cancellationToken);
            }
            else
            {
                updatedCount = await _repository.GetQueryable()
                   .Where(x =>
                       x.Id == propertyId && x.IsActive && !x.MarkedForDeletion)
                   .ExecuteUpdateAsync(
                       setters => setters
                           .SetProperty(x => x.OwnerName, x => !string.IsNullOrWhiteSpace(ownerName) ? ownerName : x.OwnerName)
                           .SetProperty(x => x.OwnerNameEnglish, x => !string.IsNullOrWhiteSpace(ownerNameEnglish) ? ownerNameEnglish : x.OwnerNameEnglish)
                           .SetProperty(x => x.OccupierName, x => !string.IsNullOrWhiteSpace(occupierName) ? occupierName : x.OccupierName)
                           .SetProperty(x => x.OccupierNameEnglish, x => !string.IsNullOrWhiteSpace(occupierNameEnglish) ? occupierNameEnglish : x.OccupierNameEnglish)
                           .SetProperty(x => x.UpdatedBy, dto.CreatedBy)
                           .SetProperty(x => x.UpdatedDate, now),
                       cancellationToken);
            }

            if (updatedCount == 0)
            {
                throw new InvalidOperationException("Property merge failed. New property was not updated.");
            }


            // AddRangeAsync if your repository supports it.
            await _propertyMapDetailRepository.AddRangeAsync(propertyMapDetails, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return new PropertyMergeDto
            {
                Success = true,
                Message = $"Old properties {string.Join(", ", oldPropertyNos)} " +$"merged successfully in new property no {newPropertyNo}",
                Data = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"Multiple property merge failed Old:{OldIds} New:{NewId}",dto.PropertyOldIds != null ? string.Join(",", dto.PropertyOldIds) : null,dto.PropertyId);      
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public override async Task<PropertyMergeDto?> UpdateAsync(int id, UpdatePropertyMergeDto dto, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var newPropertyId = dto.PropertyId;
            var oldPropertyIds = dto.PropertyOldIds?.Where(x => x > 0).Distinct().ToList() ?? [];

            if (oldPropertyIds.Count < 2)
            {
                throw new ValidationException("Old Property","Multiple demerge requires at least two old properties",OperationType.Update);
            }

            //  Load selected ACTIVE merge mappings
            var validationQuery =
                await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                    .Where(pmd =>
                        pmd.PropertyIdNew == newPropertyId && pmd.PropertyIdOld.HasValue &&
                        oldPropertyIds.Contains(pmd.PropertyIdOld.Value) && pmd.IsActive &&
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

            if (validationQuery.Count == 0)
            {
                var propertyExists = await _repository.GetQueryable().AsNoTracking()
                        .AnyAsync(x =>
                                x.Id == newPropertyId &&
                                x.IsActive && !x.MarkedForDeletion,
                            cancellationToken);

                throw new ValidationException("Property",propertyExists ? "No merge details found to demerge" : "Property not found",OperationType.Update);
            }

            // All requested old properties must have active mapping.
            var mappedOldPropertyIds = validationQuery.Where(x => x.PropertyIdOld.HasValue)
                .Select(x => x.PropertyIdOld!.Value).Distinct().ToHashSet();

            var invalidOldPropertyIds = oldPropertyIds.Where(x => !mappedOldPropertyIds.Contains(x)).ToList();

            if (invalidOldPropertyIds.Count > 0)
            {
                throw new ValidationException("Old Property",$"Active merge mapping not found for old property Id(s): {string.Join(", ", invalidOldPropertyIds)}",OperationType.Update);
            }

            var propertyMapDetailIds = validationQuery.Select(x => x.Id).ToList();
            //  Load all MergeDetail snapshots
            var mergeDetails =
                await _mergeDetailRepository.GetQueryable().AsNoTracking()
                    .Where(md =>
                        propertyMapDetailIds.Contains(
                            md.PropertyMapDetailId) && md.IsActive)
                    .ToListAsync(cancellationToken);
            if (mergeDetails.Count == 0)
            {
                throw new ValidationException("Merge Details","Original property data not found", OperationType.Update);
            }

            // Ensure every selected mapping has snapshot
            var snapshotMapIds = mergeDetails.Select(x => x.PropertyMapDetailId).ToHashSet();
            var mappingsWithoutSnapshot = propertyMapDetailIds.Where(x => !snapshotMapIds.Contains(x)).ToList();

            if (mappingsWithoutSnapshot.Count > 0)
            {
                throw new ValidationException("Merge Details","Merge snapshot not found for one or more selected properties",OperationType.Update);
            }

            //  Load current PropertyMaster
            var currentProperty =
                await _repository.GetQueryable().AsNoTracking()
                    .Where(x =>
                        x.Id == newPropertyId &&
                        x.IsActive && !x.MarkedForDeletion)
                    .Select(x => new
                    {
                        x.OwnerName,
                        x.OwnerNameEnglish,
                        x.OccupierName,
                        x.OccupierNameEnglish,
                        x.SocietyDetailId
                    })
                    .FirstOrDefaultAsync(cancellationToken);

            if (currentProperty == null)
            {
                throw new ValidationException("Property","Property not found", OperationType.Update);
            }

            //  Load ALL selected old properties
            var oldPropertyData =
                await _propertyOldRepository.GetQueryable().AsNoTracking()
                    .Where(x =>
                        oldPropertyIds.Contains(x.Id))
                    .Select(x => new
                    {
                        x.Id,
                        x.OldOwnerName,
                        x.OldOwnerNameEnglish,
                        x.OldOccupierName,
                        x.OldOccupierNameEnglish
                    })
                    .ToListAsync(cancellationToken);

            if (oldPropertyData.Count != oldPropertyIds.Count)
            {
                var foundIds = oldPropertyData.Select(x => x.Id).ToHashSet();
                var missingIds = oldPropertyIds.Where(x => !foundIds.Contains(x));
                throw new ValidationException("Old Property",$"Old property not found for Id(s): " +$"{string.Join(", ", missingIds)}",OperationType.Update);
            }

            //Remove ALL selected old Owner / Occupier names
            var updatedOwnerName = currentProperty.OwnerName;
            var updatedOwnerNameEnglish = currentProperty.OwnerNameEnglish;
            var updatedOccupierName = currentProperty.OccupierName;
            var updatedOccupierNameEnglish = currentProperty.OccupierNameEnglish;

            foreach (var oldProperty in oldPropertyData)
            {
                updatedOwnerName = RemoveOwnerNameFromCommaSeparated(updatedOwnerName,oldProperty.OldOwnerName);
                updatedOwnerNameEnglish = RemoveOwnerNameFromCommaSeparated(updatedOwnerNameEnglish,oldProperty.OldOwnerNameEnglish);
                updatedOccupierName = RemoveOwnerNameFromCommaSeparated(updatedOccupierName,oldProperty.OldOccupierName);
                updatedOccupierNameEnglish = RemoveOwnerNameFromCommaSeparated(updatedOccupierNameEnglish,oldProperty.OldOccupierNameEnglish);
            }

            // Multiple merge stores snapshots sequentially.
            var firstPropertyMapDetailId = validationQuery.OrderBy(x => x.Id).Select(x => x.Id).First();
            var restoreData = mergeDetails.FirstOrDefault(x => x.PropertyMapDetailId == firstPropertyMapDetailId);
            if (restoreData == null)
            {
                throw new ValidationException("Merge Details","Restore snapshot not found",OperationType.Update);
            }

            var updatedDate = DateTime.Now;
            //  Update PropertyMaster
            int propertyUpdatedCount;
            if (dto.IsPreviousDataUpdate)
            {
                propertyUpdatedCount =
                    await _repository.GetQueryable()
                        .Where(pm =>
                            pm.Id == newPropertyId &&
                            pm.IsActive && !pm.MarkedForDeletion)
                        .ExecuteUpdateAsync(
                            setters => setters
                                .SetProperty(pm => pm.OwnerName, string.IsNullOrWhiteSpace(updatedOwnerName) ? "The Holder" : updatedOwnerName)
                                .SetProperty(pm => pm.OwnerNameEnglish, string.IsNullOrWhiteSpace(updatedOwnerNameEnglish) ? "The Holder" : updatedOwnerNameEnglish)
                                .SetProperty(pm => pm.OccupierName, updatedOccupierName)
                                .SetProperty(pm => pm.OccupierNameEnglish, updatedOccupierNameEnglish)
                                .SetProperty(pm => pm.MobileNo, restoreData.MobileNo)
                                .SetProperty(pm => pm.Address, restoreData.Address)
                                .SetProperty(pm => pm.AddressEnglish, restoreData.AddressEnglish)
                                .SetProperty(pm => pm.FlatOrShopNo, restoreData.FlatOrShopNo)
                                .SetProperty(pm => pm.FlatOrShopNoEnglish, restoreData.FlatOrShopNoEnglish)
                                .SetProperty(pm => pm.FlatOrShopName, restoreData.FlatOrShopName)
                                .SetProperty(pm => pm.FlatOrShopNameEnglish, restoreData.FlatOrShopNameEnglish)
                                .SetProperty(pm => pm.UpdatedBy, dto.UpdatedBy)
                                .SetProperty(pm => pm.UpdatedDate, updatedDate),
                            cancellationToken);

                //  Restore Society Builder details
                if (currentProperty.SocietyDetailId.HasValue)
                {
                    await _societyRepository.GetQueryable()
                        .Where(s =>
                            s.Id == currentProperty.SocietyDetailId.Value && s.IsActive)
                        .ExecuteUpdateAsync(
                            setters => setters
                                .SetProperty(s => s.BuilderName, restoreData.BuilderName)
                                .SetProperty(s => s.BuilderNameEnglish, restoreData.BuilderNameEnglish)
                                .SetProperty(s => s.UpdatedBy, dto.UpdatedBy)
                                .SetProperty(s => s.UpdatedDate, updatedDate),
                            cancellationToken);
                }
            }
            else
            {
                propertyUpdatedCount =
                    await _repository.GetQueryable()
                        .Where(pm =>
                            pm.Id == newPropertyId &&
                            pm.IsActive && !pm.MarkedForDeletion)
                        .ExecuteUpdateAsync(
                            setters => setters
                                .SetProperty(pm => pm.OwnerName, string.IsNullOrWhiteSpace(updatedOwnerName) ? "The Holder" : updatedOwnerName)
                                .SetProperty(pm => pm.OwnerNameEnglish, string.IsNullOrWhiteSpace(updatedOwnerNameEnglish) ? "The Holder" : updatedOwnerNameEnglish)
                                .SetProperty(pm => pm.OccupierName, updatedOccupierName)
                                .SetProperty(pm => pm.OccupierNameEnglish, updatedOccupierNameEnglish)
                                .SetProperty(pm => pm.UpdatedBy, dto.UpdatedBy)
                                .SetProperty(pm => pm.UpdatedDate, updatedDate),
                            cancellationToken);
            }
            
            if (propertyUpdatedCount == 0)
            {
                throw new InvalidOperationException("Demerge failed. Property was not updated.");
            }

            //  Delete PREVIOUS cancelled mappings
            var cancelledPropertyMapDetailIds =
                await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                    .Where(pmd =>
                        pmd.Status == PropertyMapStatus.Cancelled &&
                        pmd.PropertyIdNew == newPropertyId &&
                        pmd.PropertyIdOld.HasValue &&
                        oldPropertyIds.Contains(pmd.PropertyIdOld.Value))
                    .Select(pmd => pmd.Id)
                    .ToListAsync(cancellationToken);

            if (cancelledPropertyMapDetailIds.Count > 0)
            {
                // Delete child first
                await _mergeDetailRepository.GetQueryable()
                    .Where(md =>
                        cancelledPropertyMapDetailIds.Contains(md.PropertyMapDetailId))
                    .ExecuteDeleteAsync(cancellationToken);

                // Delete parent
                await _propertyMapDetailRepository.GetQueryable()
                    .Where(pmd =>
                        cancelledPropertyMapDetailIds.Contains(pmd.Id))
                    .ExecuteDeleteAsync(cancellationToken);
            }

            // Cancel ALL selected active mappings
            var updatedMappingCount =
                await _propertyMapDetailRepository.GetQueryable()
                    .Where(pmd =>
                        pmd.PropertyIdNew == newPropertyId &&
                        pmd.PropertyIdOld.HasValue &&
                        oldPropertyIds.Contains(pmd.PropertyIdOld.Value) &&
                        pmd.IsActive && pmd.Status == PropertyMapStatus.Active)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(pmd => pmd.Status,PropertyMapStatus.Cancelled)
                            .SetProperty(pmd => pmd.IsActive,false)
                            .SetProperty(pmd => pmd.UpdatedBy,dto.UpdatedBy)
                            .SetProperty(pmd => pmd.UpdatedDate,updatedDate),
                        cancellationToken);

            if (updatedMappingCount != oldPropertyIds.Count)
            {
                throw new InvalidOperationException($"Demerge failed. Expected to update {oldPropertyIds.Count} mapping(s), but updated {updatedMappingCount}.");
            }

            //  Deactivate MergeDetail rows in ONE query
            var updatedMergeDetailCount = await _mergeDetailRepository.GetQueryable()
                    .Where(md =>
                        propertyMapDetailIds.Contains(md.PropertyMapDetailId) && md.IsActive)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(md => md.IsActive,false)
                            .SetProperty(md => md.UpdatedBy,dto.UpdatedBy)
                            .SetProperty(md => md.UpdatedDate,updatedDate),
                        cancellationToken);

            if (updatedMergeDetailCount == 0)
            {
                throw new InvalidOperationException("Demerge failed. Merge details were not updated.");
            }

            //  Check remaining ACTIVE mappings 
            var remainingMappingCount = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                .CountAsync(
                        pmd =>
                            pmd.PropertyIdNew == newPropertyId &&
                            pmd.PropertyIdOld.HasValue &&
                            pmd.IsActive &&
                            pmd.Status == PropertyMapStatus.Active,
                        cancellationToken);


           
            //If only ONE mapping remains,convert it to One-To-One
            if (remainingMappingCount == 1)
            {
                var oneToOnePropertyMapId =
                    await _propertyMapMasterRepository.GetQueryable().AsNoTracking()
                        .Where(x => x.IsActive &&
                            x.MappingCategory == PropertyMappingCategory.OneToOneMappingCategory)
                        .Select(x => x.Id)
                        .FirstOrDefaultAsync(cancellationToken);

                if (oneToOnePropertyMapId <= 0)
                {
                    throw new InvalidOperationException("ONE_TO_ONE mapping category not found");
                }

                await _propertyMapDetailRepository.GetQueryable()
                    .Where(pmd =>
                        pmd.PropertyIdNew == newPropertyId &&
                        pmd.IsActive &&
                        pmd.Status == PropertyMapStatus.Active)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(pmd => pmd.PropertyMapId,oneToOnePropertyMapId)
                            .SetProperty(pmd => pmd.UpdatedBy,dto.UpdatedBy)
                            .SetProperty(pmd => pmd.UpdatedDate,updatedDate),
                        cancellationToken);
            }

            var newPropertyNo = validationQuery.Select(x => x.PropertyNoNew).FirstOrDefault();
            var oldPropertyNos = validationQuery.Select(x => x.PropertyNoOld).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return new PropertyMergeDto
            {
                Success = true,
                Message = $"Old properties {string.Join(", ", oldPropertyNos)} demerged successfully from new property no : {newPropertyNo}",
                Data = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError( ex, "Multiple demerge failed NewProperty:{PropertyId} OldProperties:{OldPropertyIds}",dto.PropertyId,dto.PropertyOldIds != null? string.Join(",", dto.PropertyOldIds) : null);
             await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public override async Task<PropertyMergeDto?> GetByIdAsync(int propertyId,CancellationToken cancellationToken = default)
    {
        try
        {
            //  GET NEW PROPERTY HEADER
            var property = await (
                from pm in _repository.GetQueryable().AsNoTracking()
                join wd in _wardRepository.GetQueryable().AsNoTracking()
                    on pm.WardId equals wd.Id
                where pm.Id == propertyId
                      && pm.IsActive
                      && !pm.MarkedForDeletion
                select new PropertyMergeDetailDto
                {
                    Id = pm.Id,
                    WardId = pm.WardId,
                    WardNo = wd.WardNo,
                    PropertyNo = pm.PropertyNo,
                    PartitionNo = pm.PartitionNo
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (property == null)
            {
                return new PropertyMergeDto
                {
                    Success = false,
                    Message = "Property not found"
                };
            }

            //  GET ACTIVE OLD PROPERTIES
            var oldProperties = await (
                from pmd in _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                join pmo in _propertyOldRepository.GetQueryable().AsNoTracking()
                    on pmd.PropertyIdOld equals pmo.Id
                where pmd.PropertyIdNew == propertyId
                      && pmd.IsActive
                      && pmd.Status == PropertyMapStatus.Active
                      && pmo.IsActive && !pmo.MarkedForDeletion
                select new PropertyOldDetails
                {
                    PropertyOldId = pmo.Id,
                    OldWardNo = pmo.OldWardNo,
                    OldPropertyNo = pmo.OldPropertyNo,
                    OldPartitionNo = pmo.OldPartitionNo,
                    OldOwnerName = pmo.OldOwnerName,
                    OldMobileNo = pmo.OldMobileNo,
                    OldOccupierName = pmo.OldOccupierName,
                    OldAddress = pmo.OldAddress,
                    OldSocietyName = pmo.OldSocietyName,
                    OldRV = pmo.OldRV,
                    OldTotalTax = pmo.OldTotalTax,
                    OldPlotArea = pmo.OldPlotArea,
                    OldGeneralTax = pmo.OldGeneralTax,
                    OldConstructionYear = pmo.OldConstructionYear == null ? null: Convert.ToInt32(pmo.OldConstructionYear),
                    OldConstructionArea = pmo.OldConstructionArea
                })
                .ToListAsync(cancellationToken);

            if (oldProperties.Count == 0)
            {
                return new PropertyMergeDto
                {
                    Success = false,
                    Message = "No merge details found for the specified property"
                };
            }

            property.PropertyOldDetails = oldProperties;
            return new PropertyMergeDto
            {
                Success = true,
                Message = $"Found {oldProperties.Count} merge detail(s)",
                Data = property
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"Error retrieving merge details for property {PropertyId}",propertyId);
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
