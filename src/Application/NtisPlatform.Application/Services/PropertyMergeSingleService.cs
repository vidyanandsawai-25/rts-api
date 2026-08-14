using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Constants;
using NtisPlatform.Application.DTOs.PropertyMergeSingle;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
namespace NtisPlatform.Application.Services;

public class PropertyMergeSingleService : BaseCommonCrudService<PropertyMapDetailEntity, PropertyMergeSingleDto, CreatePropertyMergeSingleDto, UpdatePropertyMergeSingleDto, PropertyMergeSingleQueryParameters, int>, IPropertyMergeSingleService
{
    private readonly IRepository<PropertyMapMasterEntity, int> _propertyMapMasterRepository;
    private readonly IRepository<PropertyMastOldEntity, int> _propertyOldRepository;
    private readonly IRepository<PropertyMapDetailEntity, int> _propertyMapDetailRepository;
    private readonly new IRepository<PropertyEntity, int> _repository;
    private readonly IRepository<WardEntity, int> _wardRepository;
    private readonly IRepository<SocietyDetailsEntity, int> _societyRepository;
    private readonly IRepository<MergeDetailEntity, int> _mergeDetailRepository;
    private readonly new IUnitOfWork _unitOfWork;
    private readonly ILogger<PropertyMergeSingleService> _logger;
    private readonly IMapper _mapper;

    public PropertyMergeSingleService(
        IRepository<PropertyMapMasterEntity, int> propertyMapMasterRepository,
        IRepository<PropertyMastOldEntity, int> propertyOldRepository,
        IRepository<PropertyMapDetailEntity, int> propertyMapDetailRepository,
        IRepository<PropertyEntity, int> repository,
        IRepository<WardEntity, int> wardRepository,
        IRepository<SocietyDetailsEntity, int> societyRepository,
        IRepository<MergeDetailEntity, int> mergeDetailRepository,
        IUnitOfWork unitOfWork,
        ILogger<PropertyMergeSingleService> logger,
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

    public override async Task<PropertyMergeSingleDto> CreateAsync(CreatePropertyMergeSingleDto dto, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var propertyMapId = await _propertyMapMasterRepository.GetQueryable().AsNoTracking()
               .Where(x => x.IsActive && x.MappingCategory == PropertyMappingCategory.OneToOneMappingCategory)
               .Select(x => x.Id)
               .FirstOrDefaultAsync(cancellationToken);

            if (propertyMapId <= 0)
            {
                throw new ValidationException("Property Map Category", $"{PropertyMappingCategory.OneToOneMappingCategory} property mapping category not found", OperationType.Create);
            }

            int propertyOldId = dto.PropertyOldId;
            int propertyId = dto.PropertyId;

            //  Load Old Property
            var propertyMastOld = await _propertyOldRepository.GetQueryable().AsNoTracking()
                .Where(x => x.Id == propertyOldId && x.IsActive && !x.MarkedForDeletion)
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

            //  Load New Property
            var propertyMast = await (
                from pm in _repository.GetQueryable().AsNoTracking()
                join wd in _wardRepository.GetQueryable().AsNoTracking() on pm.WardId equals wd.Id
                join society in _societyRepository.GetQueryable().AsNoTracking() on pm.SocietyDetailId equals society.Id into societyGroup
                from sd in societyGroup.DefaultIfEmpty()
                where pm.Id == propertyId && pm.IsActive && !pm.MarkedForDeletion
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
                }).FirstOrDefaultAsync(cancellationToken);

            if (propertyMastOld == null)
            {
                throw new ValidationException("Old Property", "Old Property not found", OperationType.Create);
            }

            if (propertyMast == null)
            {
                throw new ValidationException("New Property", "New Property not found", OperationType.Create);
            }

            //  Build property number
            var newPropertyNo = BuildPropertyNumber(propertyMast.WardNo, propertyMast.PropertyNo, propertyMast.PartitionNo);
            var oldPropertyNo = BuildPropertyNumber(propertyMastOld.OldWardNo, propertyMastOld.OldPropertyNo, propertyMastOld.OldPartitionNo);

            //  Check old already merged
            var oldAlreadyMerged =
                await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                .Where(x =>
                    x.PropertyIdOld == propertyOldId &&
                    x.IsActive && x.Status == PropertyMapStatus.Active)
                .Select(x => new
                {
                    x.PropertyNoOld,
                    x.PropertyNoNew
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (oldAlreadyMerged != null)
            {
                throw new ValidationException("Old Property", $"Old property no {oldAlreadyMerged.PropertyNoOld} already merged for new property no {oldAlreadyMerged.PropertyNoNew}", OperationType.Create);
            }

            // Update existing merge mapping
            var existingMergeExists =
                await _propertyMapDetailRepository.GetQueryable()
                .AnyAsync(x =>
                    x.PropertyIdNew == propertyId &&
                    x.IsActive && x.Status == PropertyMapStatus.Active,
                    cancellationToken);

            if (existingMergeExists)
            {
                propertyMapId =
                    await _propertyMapMasterRepository.GetQueryable().AsNoTracking()
                    .Where(x => x.IsActive &&
                        x.MappingCategory == PropertyMappingCategory.MergeMappingCategory)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                await _propertyMapDetailRepository.GetQueryable()
                    .Where(x =>
                        x.PropertyIdNew == propertyId &&
                        x.IsActive && x.Status == PropertyMapStatus.Active)
                    .ExecuteUpdateAsync(
                        set => set.SetProperty(x => x.PropertyMapId, propertyMapId),
                        cancellationToken);
            }

            //  Merge names
            var mergedOwnerName = BuildMergedPersonName(propertyMast.OwnerName, propertyMastOld.OldOwnerName);
            var mergedOwnerNameEnglish = BuildMergedPersonName(propertyMast.OwnerNameEnglish, propertyMastOld.OldOwnerNameEnglish);
            var mergedOccupierName = BuildMergedPersonName(propertyMast.OccupierName, propertyMastOld.OldOccupierName);
            var mergedOccupierNameEnglish = BuildMergedPersonName(propertyMast.OccupierNameEnglish, propertyMastOld.OldOccupierNameEnglish);
            var now = DateTime.Now;

            //  Update Property Master
            if (dto.IsOldDataUpdate)
            {
                await _repository.GetQueryable()
                   .Where(x =>
                       x.Id == propertyId && x.IsActive && !x.MarkedForDeletion)
                   .ExecuteUpdateAsync(
                       set => set
                       .SetProperty(x => x.OwnerName, x => !string.IsNullOrWhiteSpace(mergedOwnerName) ? mergedOwnerName : x.OwnerName)
                       .SetProperty(x => x.OwnerNameEnglish, x => !string.IsNullOrWhiteSpace(mergedOwnerNameEnglish) ? mergedOwnerNameEnglish : x.OwnerNameEnglish)
                       .SetProperty(x => x.OccupierName, x => !string.IsNullOrWhiteSpace(mergedOccupierName) ? mergedOccupierName : x.OccupierName)
                       .SetProperty(x => x.OccupierNameEnglish, x => !string.IsNullOrWhiteSpace(mergedOccupierNameEnglish) ? mergedOccupierNameEnglish : x.OccupierNameEnglish)
                       .SetProperty(x => x.MobileNo, x => !string.IsNullOrWhiteSpace(propertyMastOld.OldMobileNo) ? propertyMastOld.OldMobileNo : x.MobileNo)
                       .SetProperty(x => x.Address, x => !string.IsNullOrWhiteSpace(propertyMastOld.OldAddress) ? propertyMastOld.OldAddress : x.Address)
                       .SetProperty(x => x.AddressEnglish, x => !string.IsNullOrWhiteSpace(propertyMastOld.OldAddressEnglish) ? propertyMastOld.OldAddressEnglish : x.AddressEnglish)
                       .SetProperty(x => x.FlatOrShopNo, x => !string.IsNullOrWhiteSpace(propertyMastOld.OldFlatOrShopNumber) ? propertyMastOld.OldFlatOrShopNumber : x.FlatOrShopNo)
                       .SetProperty(x => x.UpdatedBy, dto.CreatedBy)
                       .SetProperty(x => x.UpdatedDate, now),
                       cancellationToken);
            }
            else
            {
                await _repository.GetQueryable()
                   .Where(x =>
                       x.Id == propertyId && x.IsActive && !x.MarkedForDeletion)
                   .ExecuteUpdateAsync(
                       set => set
                       .SetProperty(x => x.OwnerName, x => !string.IsNullOrWhiteSpace(mergedOwnerName) ? mergedOwnerName : x.OwnerName)
                       .SetProperty(x => x.OwnerNameEnglish, x => !string.IsNullOrWhiteSpace(mergedOwnerNameEnglish) ? mergedOwnerNameEnglish : x.OwnerNameEnglish)
                       .SetProperty(x => x.OccupierName, x => !string.IsNullOrWhiteSpace(mergedOccupierName) ? mergedOccupierName : x.OccupierName)
                       .SetProperty(x => x.OccupierNameEnglish, x => !string.IsNullOrWhiteSpace(mergedOccupierNameEnglish) ? mergedOccupierNameEnglish : x.OccupierNameEnglish)
                       .SetProperty(x => x.UpdatedBy, dto.CreatedBy)
                       .SetProperty(x => x.UpdatedDate, now),
                       cancellationToken);
            }
           

            //  Create PropertyMapDetail
            var propertyMapDetailSource = new PropertyMapDetailEntity
            {
                PropertyMapId = propertyMapId,
                PropertyIdNew = propertyId,
                PropertyIdOld = propertyOldId,
                PropertyNoNew = newPropertyNo,
                PropertyNoOld = oldPropertyNo,
                Status = PropertyMapStatus.Active,
                Remark = "Property Merged - Single Old Property",
                Latitude = decimal.TryParse(dto.Latitude, out var lat) ? lat : null,
                Longitude = decimal.TryParse(dto.Longitude, out var lon) ? lon : null,
                Location = dto.Location,
                CreatedBy = dto.CreatedBy
            };

            var propertyMapDetail = _mapper.Map<PropertyMapDetailEntity>(propertyMapDetailSource);
            //  Attach MergeDetails together
            var mergeDetailSource = new MergeDetailEntity
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
            };

            propertyMapDetail.MergeDetail = _mapper.Map<MergeDetailEntity>(mergeDetailSource);
            await _propertyMapDetailRepository.AddAsync(propertyMapDetail, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return new PropertyMergeSingleDto
            {
                Success = true,
                Message = $"Old property no {oldPropertyNo} merge successful in new property no {newPropertyNo}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Merge failed Old:{OldId} New:{NewId}", dto.PropertyOldId, dto.PropertyId);
             await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public override async Task<PropertyMergeSingleDto?> UpdateAsync(int id, UpdatePropertyMergeSingleDto dto, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var PropertyId = dto.PropertyId;
            var PropertyOldId = dto.PropertyOldId;
                
            var validationQuery =
                await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                .Where(pmd =>
                    pmd.PropertyIdNew == PropertyId &&
                    pmd.PropertyIdOld.HasValue &&
                    pmd.PropertyIdOld.Value == PropertyOldId &&
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

            if (validationQuery.Count == 0)
            {
                var propertyExists =
                    await _repository.GetQueryable()
                    .AsNoTracking()
                    .AnyAsync(
                        x =>
                        x.Id == PropertyId &&
                        x.IsActive &&
                        !x.MarkedForDeletion,
                        cancellationToken);

                throw new ValidationException("Property", propertyExists? "No merge details found to demerge": "Property not found", OperationType.Update);
            }

           // Get MergeDetails Snapshot
            var propertyMapDetailIds = validationQuery.Select(x => x.Id).ToList();

            var mergeDetails = await _mergeDetailRepository.GetQueryable().AsNoTracking()
                .Where(md =>
                    propertyMapDetailIds.Contains(md.PropertyMapDetailId) && md.IsActive)
                .ToListAsync(cancellationToken);

            if (mergeDetails.Count == 0)
            {
                throw new ValidationException("Merge Details", "Original property data not found", OperationType.Update);
            }

            //  Load current PropertyMaster
            var currentProperty =
                await _repository.GetQueryable().AsNoTracking()
                .Where(x => x.Id == PropertyId &&
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
                throw new ValidationException("Property", "Property not found", OperationType.Update);
            }

            //  Load OLD property owner names
            var oldPropertyData =
                await _propertyOldRepository.GetQueryable().AsNoTracking()
                .Where(x => x.Id == PropertyOldId)
                .Select(x => new
                {
                    x.OldOwnerName,
                    x.OldOwnerNameEnglish,
                    x.OldOccupierName,
                    x.OldOccupierNameEnglish
                })
                .FirstOrDefaultAsync(cancellationToken);

            //  Remove merged owner names only
            var updatedOwnerName = RemoveOwnerNameFromCommaSeparated(currentProperty.OwnerName, oldPropertyData!.OldOwnerName);
            var updatedOwnerNameEnglish = RemoveOwnerNameFromCommaSeparated(currentProperty.OwnerNameEnglish, oldPropertyData.OldOwnerNameEnglish);
            var updatedOccupierName = RemoveOwnerNameFromCommaSeparated(currentProperty.OccupierName, oldPropertyData!.OldOccupierName);
            var updatedOccupierNameEnglish = RemoveOwnerNameFromCommaSeparated(currentProperty.OccupierNameEnglish, oldPropertyData.OldOccupierNameEnglish);

            var UpdatedDate = DateTime.Now;
            var restoreData = mergeDetails.First();

            if (dto.IsPreviousDataUpdate)
            {
                await _repository.GetQueryable()
                    .Where(pm => pm.Id == PropertyId &&
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
                        .SetProperty(pm => pm.UpdatedDate, UpdatedDate),
                        cancellationToken);

                if (currentProperty.SocietyDetailId.HasValue)
                {
                    await _societyRepository.GetQueryable()
                        .Where(s => s.Id == currentProperty.SocietyDetailId.Value && s.IsActive)
                        .ExecuteUpdateAsync(
                            setters => setters
                                .SetProperty(s => s.BuilderName, restoreData.BuilderName)
                                .SetProperty(s => s.BuilderNameEnglish, restoreData.BuilderNameEnglish)
                                .SetProperty(s => s.UpdatedBy, dto.UpdatedBy)
                                .SetProperty(s => s.UpdatedDate, UpdatedDate),
                            cancellationToken);
                }
            }
            else
            {
                await _repository.GetQueryable()
                    .Where(pm => pm.Id == PropertyId &&
                        pm.IsActive && !pm.MarkedForDeletion)
                    .ExecuteUpdateAsync(
                        setters => setters
                        .SetProperty(pm => pm.OwnerName, string.IsNullOrWhiteSpace(updatedOwnerName) ? "The Holder" : updatedOwnerName)
                        .SetProperty(pm => pm.OwnerNameEnglish, string.IsNullOrWhiteSpace(updatedOwnerNameEnglish) ? "The Holder" : updatedOwnerNameEnglish)
                        .SetProperty(pm => pm.OccupierName, updatedOccupierName)
                        .SetProperty(pm => pm.OccupierNameEnglish, updatedOccupierNameEnglish)
                        .SetProperty(pm => pm.UpdatedBy, dto.UpdatedBy)
                        .SetProperty(pm => pm.UpdatedDate, UpdatedDate),
                        cancellationToken);
            }
            

            var cancelledPropertyMapDetailIds = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                            .Where(pmd =>
                                pmd.Status == PropertyMapStatus.Cancelled &&
                                pmd.PropertyIdNew == PropertyId &&
                                pmd.PropertyIdOld.HasValue &&
                                pmd.PropertyIdOld.Value == PropertyOldId)
                            .Select(pmd => pmd.Id)
                            .ToListAsync(cancellationToken);

            // First DELETE MergeDetail records
            if (cancelledPropertyMapDetailIds.Count > 0)
            {
                await _mergeDetailRepository.GetQueryable()
                    .Where(md => cancelledPropertyMapDetailIds.Contains(md.PropertyMapDetailId))
                    .ExecuteDeleteAsync(cancellationToken);

                //  Delete previous cancelled rows
                await _propertyMapDetailRepository.GetQueryable()
                    .Where(pmd =>
                        pmd.Status == PropertyMapStatus.Cancelled &&
                        pmd.PropertyIdNew == PropertyId &&
                        pmd.PropertyIdOld.HasValue &&
                        pmd.PropertyIdOld.Value == PropertyOldId)
                    .ExecuteDeleteAsync(cancellationToken);
            }

            //  Cancel Current Merge Mapping
            var updatedCount =
                await _propertyMapDetailRepository.GetQueryable()
                .Where(pmd =>
                    pmd.PropertyIdNew == PropertyId &&
                    pmd.PropertyIdOld.HasValue &&
                    pmd.PropertyIdOld.Value == PropertyOldId &&
                    pmd.IsActive &&
                    pmd.Status == PropertyMapStatus.Active)
                .ExecuteUpdateAsync(
                    setters => setters
                    .SetProperty(pmd => pmd.Status,PropertyMapStatus.Cancelled)
                    .SetProperty(pmd => pmd.IsActive, false)
                    .SetProperty(pmd => pmd.UpdatedBy, dto.UpdatedBy)
                    .SetProperty(pmd => pmd.UpdatedDate, UpdatedDate),
                    cancellationToken);

            if (updatedCount == 0)
            {
                throw new InvalidOperationException("Demerge failed. No mapping updated.");
            }

            // 7. Check remaining active mappings
            var remainingMappings =
                await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                .Where(pmd =>
                    pmd.PropertyIdNew == PropertyId &&
                    pmd.PropertyIdOld.HasValue &&
                    pmd.IsActive &&
                    pmd.Status == PropertyMapStatus.Active)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            if (remainingMappings.Count == 1)
            {

                var oneToOnePropertyMapId =
                    await _propertyMapMasterRepository.GetQueryable().AsNoTracking()
                    .Where(x => x.IsActive && x.MappingCategory == PropertyMappingCategory.OneToOneMappingCategory)
                    .Select(x => x.Id).FirstOrDefaultAsync(cancellationToken);

                if (oneToOnePropertyMapId <= 0)
                {
                    throw new InvalidOperationException("ONE_TO_ONE mapping category not found");
                }

                await _propertyMapDetailRepository
                    .GetQueryable()
                    .Where(pmd =>
                        pmd.PropertyIdNew == PropertyId &&
                        pmd.IsActive &&
                        pmd.Status == PropertyMapStatus.Active)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(pmd => pmd.PropertyMapId,oneToOnePropertyMapId),
                        cancellationToken);
            }

            await _mergeDetailRepository.GetQueryable()
            .Where(md => propertyMapDetailIds.Contains(md.PropertyMapDetailId) && md.IsActive)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(md => md.IsActive, false)
                    .SetProperty(md => md.UpdatedBy, dto.UpdatedBy)
                    .SetProperty(md => md.UpdatedDate, UpdatedDate),
                cancellationToken);

            var newPropertyNo = validationQuery.Select(x => x.PropertyNoNew).FirstOrDefault();
            var oldPropertyNos = validationQuery.Select(x => x.PropertyNoOld).Distinct().ToList();

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
           

            return new PropertyMergeSingleDto
            {
                Success = true,
                Message = $"Old properties {string.Join(", ", oldPropertyNos)} demerged successfully from new property no : {newPropertyNo}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Demerge failed NewProperty:{PropertyId}", dto.PropertyId);
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
