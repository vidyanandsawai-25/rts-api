using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Constants;
using NtisPlatform.Application.DTOs.PropertyMergeDetails;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using PropertyDemergePair = NtisPlatform.Application.DTOs.PropertyMergeDetails.PropertyDemergePair;
using PropertyMappingSelection = NtisPlatform.Application.DTOs.PropertyMergeDetails.PropertyMappingSelection;
using PropertyMergeDetailDto = NtisPlatform.Application.DTOs.PropertyMergeDetails.PropertyMergeDetailDto;
using PropertyMergeDto = NtisPlatform.Application.DTOs.PropertyMergeDetails.PropertyMergeDto;

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
    private readonly IRepository<PropertyTypeMasterEntity, int> _propertyTypeRepository;
    private readonly IRepository<WingEntity, int> _wingMasterRepository;
    private readonly IRepository<PropertyAssessmentEntity, int> _assessmentRepository;
    private readonly new IUnitOfWork _unitOfWork;
    private readonly ILogger<PropertyMergeService> _logger;

    public PropertyMergeService(
        IRepository<PropertyMapMasterEntity, int> propertyMapMasterRepository,
        IRepository<PropertyMastOldEntity, int> propertyOldRepository,
        IRepository<PropertyMapDetailEntity, int> propertyMapDetailRepository,
        IRepository<PropertyEntity, int> repository,
        IRepository<WardEntity, int> wardRepository,
        IRepository<SocietyDetailsEntity, int> societyRepository,
        IRepository<PropertyTypeMasterEntity, int> propertyTypeRepository,
        IRepository<WingEntity, int> wingMasterRepository,
        IRepository<PropertyAssessmentEntity, int> assessmentRepository,
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
        _propertyTypeRepository = propertyTypeRepository;
        _wingMasterRepository = wingMasterRepository;
        _assessmentRepository = assessmentRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Merges a new property (PropertyMast) with an old property (PropertyMastOld) by:
    /// <list type="number">
    ///   <item>Validating that the PropertyMap, PropertyMastOld, and PropertyMast all exist and are active.</item>
    ///   <item>Ensuring neither the new nor the old property is already merged.</item>
    ///   <item>Creating <see cref="PropertyMapDetailEntity"/> records.</item>
    /// </list>
    /// </summary>
    public override async Task<PropertyMergeDto> CreateAsync(CreatePropertyMergeDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Validate request
            if (dto == null)
            {
                throw new ValidationException("Property", "Property details are required", OperationType.Create);
            }

            var newPropertyCount = dto.PropertyIds?.Count ?? 0;
            var oldPropertyCount = dto.PropertyOldIds?.Count ?? 0;

            if (newPropertyCount == 0 || oldPropertyCount == 0)
            {
                throw new ValidationException("Property", "Property details are required", OperationType.Create);
            }

            // 2. Determine mapping category
            string mappingCategory;
            if (newPropertyCount == 1 && oldPropertyCount == 1)
            {
                mappingCategory = PropertyMappingCategory.OneToOneMappingCategory;
            }
            else if (newPropertyCount > 1 && oldPropertyCount == 1)
            {
                mappingCategory = PropertyMappingCategory.SplitMappingCategory;
            }
            else if (newPropertyCount == 1 && oldPropertyCount > 1)
            {
                mappingCategory = PropertyMappingCategory.MergeMappingCategory;
            }
            else
            {
                throw new ValidationException("Property", "Multiple old properties cannot be merged with multiple new properties", OperationType.Create);
            }

            // 3. Get property map category ID
            var propertyMapId = await _propertyMapMasterRepository.GetQueryable().AsNoTracking()
                .Where(x => x.IsActive && x.MappingCategory == mappingCategory)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (propertyMapId == 0)
            {
                throw new ValidationException("Property Map Category", $"{mappingCategory} property mapping category not found", OperationType.Create);
            }

            // 4. Route to appropriate merge method
            return mappingCategory switch
            {
                PropertyMappingCategory.OneToOneMappingCategory => await MergeSingleProperty(dto, propertyMapId, cancellationToken),
                PropertyMappingCategory.SplitMappingCategory => await MergeSplitProperty(dto, propertyMapId, cancellationToken),
                PropertyMappingCategory.MergeMappingCategory => await MergeMultipleProperty(dto, propertyMapId, cancellationToken),
                _ => throw new InvalidOperationException($"Unsupported mapping category: {mappingCategory}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging properties");
            throw;
        }
    }

    /// <summary>
    /// Merges a single old property with a single new property (optimized)
    /// </summary>
    private async Task<PropertyMergeDto> MergeSingleProperty(CreatePropertyMergeDto dto, int propertyMapId, CancellationToken cancellationToken = default)
    {
        var transactionStarted = false;

        try
        {
            // 1. Validate input
            if (dto.PropertyOldIds == null || dto.PropertyOldIds.Count != 1)
            {
                throw new ValidationException("Old Property", "Single merge operation requires exactly one old propertyNo", OperationType.Create);
            }

            if (dto.PropertyIds == null || dto.PropertyIds.Count != 1)
            {
                throw new ValidationException("New Property", "Single merge operation requires exactly one new propertyNo", OperationType.Create);
            }

            int propertyOldId = dto.PropertyOldIds[0];
            int propertyId = dto.PropertyIds[0];

            // 2. Load property data
            var propertyMastOld = await _propertyOldRepository.GetQueryable().AsNoTracking()
                .Where(pm => pm.Id == propertyOldId && pm.IsActive && !pm.MarkedForDeletion)
                .Select(p => new { p.OldPropertyNo, p.Id, p.OldWardNo, p.OldPartitionNo, p.OldOwnerName, p.OldOccupierName })
                .FirstOrDefaultAsync(cancellationToken);

            var propertyMast = await (
                from pm in _repository.GetQueryable().AsNoTracking()
                join wd in _wardRepository.GetQueryable().AsNoTracking() on pm.WardId equals wd.Id
                where pm.Id == propertyId && pm.IsActive && !pm.MarkedForDeletion
                select new { pm.Id, wd.WardNo, pm.PropertyNo, pm.PartitionNo, pm.OwnerName, pm.OccupierName })
                .FirstOrDefaultAsync(cancellationToken);

            // 3. Validate results
            if (propertyMastOld == null)
            {
                throw new ValidationException("Old Property", "Old Property not found", OperationType.Create);
            }

            if (propertyMast == null)
            {
                throw new ValidationException("New Property", "New Property not found", OperationType.Create);
            }

            // 4. Parse coordinates
            decimal? latitude = !string.IsNullOrWhiteSpace(dto.Latitude) && decimal.TryParse(dto.Latitude, out var lat) ? lat : null;
            decimal? longitude = !string.IsNullOrWhiteSpace(dto.Longitude) && decimal.TryParse(dto.Longitude, out var lon) ? lon : null;
            var now = DateTime.Now;

            var newPropertyNo = BuildPropertyNumber(propertyMast.WardNo, propertyMast.PropertyNo, propertyMast.PartitionNo);
            var oldPropertyNo = BuildPropertyNumber(propertyMastOld.OldWardNo, propertyMastOld.OldPropertyNo, propertyMastOld.OldPartitionNo);

            // 5. Check if old property is already merged
            var existingMerge = await (
                from pmd in _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                where pmd.PropertyIdOld == propertyOldId
                      && pmd.IsActive
                      && pmd.Status == PropertyMapStatus.Active
                select new
                {
                    OldPropertyNo = pmd.PropertyNoOld,
                    NewPropertyNo = pmd.PropertyNoNew
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (existingMerge != null)
            {
                throw new ValidationException("Old Property", $"Old property no {existingMerge.OldPropertyNo} already merged for new property no : {existingMerge.NewPropertyNo}", OperationType.Create);
            }

            // 6. Begin transaction
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            transactionStarted = true;

            // 7. Check if new property has existing merges (needs category ID update)
            var existingMergeNewProperty = await (
                from pmd in _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                where pmd.PropertyIdNew == propertyId
                      && pmd.IsActive && pmd.Status == PropertyMapStatus.Active
                select pmd.Id)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (existingMergeNewProperty.Count > 0)
            {
                propertyMapId = await _propertyMapMasterRepository.GetQueryable().AsNoTracking()
                    .Where(x => x.IsActive && x.MappingCategory == PropertyMappingCategory.MergeMappingCategory)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                await _propertyMapDetailRepository.GetQueryable()
                    .Where(pmd =>
                        pmd.PropertyIdNew == propertyId &&
                        pmd.IsActive &&
                        pmd.Status == PropertyMapStatus.Active)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(pmd => pmd.PropertyMapId, propertyMapId),
                        cancellationToken);
            }

            // 8. Merge OwnerName and OccupierName into new PropertyMast
            var mergedOwnerName = BuildMergedPersonName(propertyMast.OwnerName, propertyMastOld.OldOwnerName);
            var mergedOccupierName = BuildMergedPersonName(propertyMast.OccupierName, propertyMastOld.OldOccupierName);

            var existingOwnerName = string.IsNullOrWhiteSpace(propertyMast.OwnerName) ? null : propertyMast.OwnerName.Trim();
            var existingOccupierName = string.IsNullOrWhiteSpace(propertyMast.OccupierName) ? null : propertyMast.OccupierName.Trim();

            var ownerNameChanged = !string.Equals(existingOwnerName, mergedOwnerName, StringComparison.OrdinalIgnoreCase);
            var occupierNameChanged = !string.Equals(existingOccupierName, mergedOccupierName, StringComparison.OrdinalIgnoreCase);

            if (ownerNameChanged || occupierNameChanged)
            {
                await _repository.GetQueryable()
                    .Where(pm =>
                        pm.Id == propertyId && pm.IsActive && !pm.MarkedForDeletion)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(pm => pm.OwnerName, mergedOwnerName)
                            .SetProperty(pm => pm.OccupierName, mergedOccupierName)
                            .SetProperty(pm => pm.UpdatedBy, dto.CreatedBy)
                            .SetProperty(pm => pm.UpdatedDate, now),
                        cancellationToken);
            }

            // 9. Build PropertyMapDetail entity
            var createEntity = CreatePropertyMergeDetail(new PropertyMapDetailEntity()
            {
                PropertyMapId = propertyMapId,
                PropertyIdNew = propertyId,
                PropertyIdOld = propertyOldId,
                PropertyNoNew = newPropertyNo,
                PropertyNoOld = oldPropertyNo,
                Status = PropertyMapStatus.Active,
                Remark = "Property Merged - Single Old Property",
                Latitude = latitude,
                Longitude = longitude,
                Location = dto.Location,
                IsActive = true,
                CreatedBy = dto.CreatedBy
            });

            // 10. Insert PropertyMapDetail record
            await _propertyMapDetailRepository.AddAsync(createEntity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 11. Commit transaction
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            transactionStarted = false;

            return new PropertyMergeDto
            {
                Success = true,
                Message = $"Old property no {oldPropertyNo} merge successful in new property no {newPropertyNo}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging single property. PropertyOldId: {PropertyOldId}, PropertyId: {PropertyId}", dto.PropertyOldIds?.FirstOrDefault(), dto.PropertyIds?.FirstOrDefault());
            if (transactionStarted)
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            }
            throw;
        }
    }

    /// <summary>
    /// Merges one old property (PropertyMastOld) into multiple new properties (PropertyMast) for SPLIT scenario
    /// </summary>
    private async Task<PropertyMergeDto> MergeSplitProperty(CreatePropertyMergeDto dto, int propertyMapId, CancellationToken cancellationToken = default)
    {
        var transactionStarted = false;
        try
        {
            // 1. Validate input
            if (dto.PropertyOldIds == null || dto.PropertyOldIds.Count != 1)
            {
                throw new ValidationException("Old Property", "Split operation requires exactly one old propertyNo", OperationType.Create);
            }

            if (dto.PropertyIds == null || dto.PropertyIds.Count < 1)
            {
                throw new ValidationException("New Property", "At least one new propertyNo is required for the split operation", OperationType.Create);
            }

            int propertyOldId = dto.PropertyOldIds[0];
            var propertyIds = dto.PropertyIds;

            // 2. Load property data
            var propertyMastOld = await _propertyOldRepository.GetQueryable().AsNoTracking()
                .Where(pm => pm.Id == propertyOldId && pm.IsActive && !pm.MarkedForDeletion)
                .Select(p => new { p.OldWardNo, p.OldPropertyNo, p.OldPartitionNo, p.Id })
                .FirstOrDefaultAsync(cancellationToken);

            var propertyMasts = await (
                from pm in _repository.GetQueryable()
                join wd in _wardRepository.GetQueryable() on pm.WardId equals wd.Id
                where propertyIds.Contains(pm.Id) && pm.IsActive && !pm.MarkedForDeletion
                      && wd.IsActive
                select new { pm.Id, wd.WardNo, pm.PropertyNo, pm.PartitionNo })
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var existingMerges = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                .Where(pmd =>
                    pmd.PropertyIdNew.HasValue &&
                    propertyIds.Contains(pmd.PropertyIdNew.Value) &&
                    pmd.IsActive && pmd.Status == PropertyMapStatus.Active)
                .Select(pmd => new { pmd.PropertyNoNew, pmd.PropertyNoOld })
                .Distinct()
                .ToListAsync(cancellationToken);

            // 3. Validate results
            if (propertyMastOld == null)
            {
                throw new ValidationException("Old Property", "Old Property not found", OperationType.Create);
            }

            if (propertyMasts.Count != propertyIds.Count)
            {
                throw new ValidationException("New Property", "Selected new properties were not found. Please select the properties one by one and try", OperationType.Create);
            }

            // Check if any new properties are already merged
            if (existingMerges.Any())
            {
                var newPropertyNo = string.Join(", ", existingMerges
                    .Select(x => x.PropertyNoNew).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());

                var oldPropertyNos = string.Join(", ", existingMerges
                    .Select(x => x.PropertyNoOld).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());

                throw new ValidationException("New Property", $"New properties {newPropertyNo} already merged to old properties {oldPropertyNos}", OperationType.Create);
            }

            // 4. Begin transaction
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            transactionStarted = true;

            // 5. Parse coordinates
            decimal? latitude = !string.IsNullOrWhiteSpace(dto.Latitude) && decimal.TryParse(dto.Latitude, out var lat) ? lat : null;
            decimal? longitude = !string.IsNullOrWhiteSpace(dto.Longitude) && decimal.TryParse(dto.Longitude, out var lon) ? lon : null;

            var oldPropertyNo = BuildPropertyNumber(propertyMastOld.OldWardNo, propertyMastOld.OldPropertyNo, propertyMastOld.OldPartitionNo);

            // 6. Build all map-detail records
            var createDtos = new List<PropertyMapDetailEntity>();
            var newPropertyNumbers = new List<string>();

            foreach (var propertyMast in propertyMasts)
            {
                var newPropertyNo = BuildPropertyNumber(propertyMast.WardNo, propertyMast.PropertyNo, propertyMast.PartitionNo);
                newPropertyNumbers.Add(newPropertyNo);

                var createEntity = CreatePropertyMergeDetail(new PropertyMapDetailEntity()
                {
                    PropertyMapId = propertyMapId,
                    PropertyIdNew = propertyMast.Id,
                    PropertyIdOld = propertyOldId,
                    PropertyNoNew = newPropertyNo,
                    PropertyNoOld = oldPropertyNo,
                    Status = PropertyMapStatus.Active,
                    Remark = "Property Merged - Split Old Property",
                    Latitude = latitude,
                    Longitude = longitude,
                    Location = dto.Location,
                    IsActive = true,
                    CreatedBy = dto.CreatedBy
                });

                createDtos.Add(createEntity);
            }

            // 7. Bulk insert PropertyMapDetail records
            await _propertyMapDetailRepository.AddRangeAsync(createDtos, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 8. Commit transaction
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            transactionStarted = false;

            return new PropertyMergeDto
            {
                Success = true,
                Message = $"Old property no {oldPropertyNo} split successfully into new property nos: {string.Join(", ", newPropertyNumbers)}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging split property. PropertyOldId: {PropertyOldId}, PropertyIds: {PropertyIds}", dto.PropertyOldIds?.FirstOrDefault(), dto.PropertyIds != null ? string.Join(", ", dto.PropertyIds) : null);
            if (transactionStarted)
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            }
            throw;
        }
    }

    /// <summary>
    /// Merges multiple old properties (PropertyMastOld) into one new property (PropertyMast) for MERGE scenario
    /// </summary>
    private async Task<PropertyMergeDto> MergeMultipleProperty(CreatePropertyMergeDto dto, int propertyMapId, CancellationToken cancellationToken = default)
    {
        var transactionStarted = false;
        try
        {
            // 1. Validate input
            if (dto.PropertyIds == null || dto.PropertyIds.Count != 1)
            {
                throw new ValidationException("New Property", "Merge operation requires exactly one new propertyNo", OperationType.Create);
            }

            if (dto.PropertyOldIds == null || dto.PropertyOldIds.Count < 1)
            {
                throw new ValidationException("Old Property", "At least one old propertyNo is required for the merge operation", OperationType.Create);
            }

            int propertyId = dto.PropertyIds[0];
            var propertyOldIds = dto.PropertyOldIds;

            // 2. Load property data
            var propertyMast = await (
                from pm in _repository.GetQueryable().AsNoTracking()
                join wd in _wardRepository.GetQueryable().AsNoTracking() on pm.WardId equals wd.Id
                where pm.Id == propertyId && pm.IsActive && !pm.MarkedForDeletion
                select new { pm.Id, wd.WardNo, pm.PropertyNo, pm.PartitionNo })
                .FirstOrDefaultAsync(cancellationToken);

            var propertyMastOlds = await _propertyOldRepository.GetQueryable().AsNoTracking()
                .Where(pm => propertyOldIds.Contains(pm.Id) && pm.IsActive && !pm.MarkedForDeletion)
                .Select(p => new { p.OldWardNo, p.OldPropertyNo, p.OldPartitionNo, p.Id })
                .ToListAsync(cancellationToken);

            var existingMerges = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                .Where(pmd =>
                    pmd.PropertyIdOld.HasValue &&
                    propertyOldIds.Contains(pmd.PropertyIdOld.Value) &&
                    pmd.IsActive && pmd.Status == PropertyMapStatus.Active)
                .Select(pmd => new { pmd.PropertyNoOld, pmd.PropertyNoNew })
                .Distinct()
                .ToListAsync(cancellationToken);

            // 3. Validate results
            if (propertyMast == null)
            {
                throw new ValidationException("New Property", "New Property not found", OperationType.Create);
            }

            if (propertyMastOlds.Count != propertyOldIds.Count)
            {
                throw new ValidationException("Old Property", "Selected old properties were not found. Please select the properties one by one and try", OperationType.Create);
            }

            // Check if any old properties are already merged
            if (existingMerges.Any())
            {
                var oldPropertyNo = string.Join(", ", existingMerges
                    .Select(x => x.PropertyNoOld).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());

                var newPropertyNos = string.Join(", ", existingMerges
                    .Select(x => x.PropertyNoNew).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());

                throw new ValidationException("Old Property", $"Old properties {oldPropertyNo} already merged to new properties {newPropertyNos}", OperationType.Create);
            }

            // 4. Begin transaction
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            transactionStarted = true;

            // 5. Parse coordinates
            decimal? latitude = !string.IsNullOrWhiteSpace(dto.Latitude) && decimal.TryParse(dto.Latitude, out var lat) ? lat : null;
            decimal? longitude = !string.IsNullOrWhiteSpace(dto.Longitude) && decimal.TryParse(dto.Longitude, out var lon) ? lon : null;

            var newPropertyNo = BuildPropertyNumber(propertyMast.WardNo, propertyMast.PropertyNo, propertyMast.PartitionNo);

            // 6. Build all map-detail records
            var createDtos = new List<PropertyMapDetailEntity>();
            var oldPropertyNumbers = new List<string>();

            foreach (var propertyMastOld in propertyMastOlds)
            {
                var oldPropertyNo = BuildPropertyNumber(propertyMastOld.OldWardNo, propertyMastOld.OldPropertyNo, propertyMastOld.OldPartitionNo);
                oldPropertyNumbers.Add(oldPropertyNo);

                var createEntity = CreatePropertyMergeDetail(new PropertyMapDetailEntity()
                {
                    PropertyMapId = propertyMapId,
                    PropertyIdNew = propertyId,
                    PropertyIdOld = propertyMastOld.Id,
                    PropertyNoNew = newPropertyNo,
                    PropertyNoOld = oldPropertyNo,
                    Status = PropertyMapStatus.Active,
                    Remark = "Property Merged - Multiple Old Properties",
                    Latitude = latitude,
                    Longitude = longitude,
                    Location = dto.Location,
                    IsActive = true,
                    CreatedBy = dto.CreatedBy
                });
                createDtos.Add(createEntity);
            }

            // 7. Bulk insert PropertyMapDetail records
            await _propertyMapDetailRepository.AddRangeAsync(createDtos, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 8. Commit transaction
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            transactionStarted = false;

            return new PropertyMergeDto
            {
                Success = true,
                Message = $"Old property nos {string.Join(", ", oldPropertyNumbers)} merged successfully into new property no: {newPropertyNo}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging multiple properties. PropertyId: {PropertyId}, PropertyOldIds: {PropertyOldIds}", dto.PropertyIds?.FirstOrDefault(), dto.PropertyOldIds != null ? string.Join(", ", dto.PropertyOldIds) : null);
            if (transactionStarted)
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            }
            throw;
        }
    }

    ///// <summary>
    ///// Demerges a property by:
    ///// <list type="number">
    /////   <item>Validating that the property exists and is merged with the specified old properties.</item>
    /////   <item>Marking PropertyMapDetail records as CANCELLED and inactive.</item>
    /////   <item>Cleaning up any existing CANCELLED records.</item>
    ///// </list>
    ///// </summary>
    public override async Task<PropertyMergeDto?> UpdateAsync(int id, UpdatePropertyMergeDto dto, CancellationToken cancellationToken = default)
    {
        var response = new PropertyMergeDto();
        try
        {
            if (dto == null)
            {
                throw new ValidationException("Property", "Invalid request data", OperationType.Update);
            }

            var newPropertyCount = dto.PropertyIds?.Count ?? 0;
            var oldPropertyCount = dto.PropertyOldIds?.Count ?? 0;

            if (newPropertyCount <= 0)
            {
                throw new ValidationException("Property", "Invalid propertyNo", OperationType.Update);
            }

            if (oldPropertyCount <= 0)
            {
                throw new ValidationException("Property", "Invalid old propertyNo", OperationType.Update);
            }

            if (newPropertyCount == 1 && !string.Equals(dto.PropertySide, "Old", StringComparison.OrdinalIgnoreCase) && oldPropertyCount > 0)
            {
                var newPropertyId = dto.PropertyIds!.Where(id => id > 0).Distinct().Single();
                var oldPropertyIds = dto.PropertyOldIds!.Where(id => id > 0).Distinct().ToList();

                if (oldPropertyIds.Count == 0)
                {
                    throw new ValidationException("Old Property", "Invalid old property number", OperationType.Update);
                }

                // Load requested OLD rows and the parent NEW row.
                var validationQuery = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                    .Where(pmd =>
                        pmd.PropertyIdNew == newPropertyId &&
                        pmd.PropertyIdOld.HasValue &&
                        oldPropertyIds.Contains(pmd.PropertyIdOld.Value) &&
                        pmd.IsActive && pmd.Status == PropertyMapStatus.Active)
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
                        .AnyAsync(property => property.Id == newPropertyId && property.IsActive && !property.MarkedForDeletion, cancellationToken);

                    throw new ValidationException("Property", propertyExists ? "No merge details found to demerge" : "Property not found", OperationType.Update);
                }

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                //  Load only the rows that will be updated.
                var deletedCancelledCount = await _propertyMapDetailRepository.GetQueryable()
                        .Where(pmd =>
                            pmd.Status == PropertyMapStatus.Cancelled &&
                            pmd.PropertyIdNew == newPropertyId &&
                            pmd.PropertyIdOld.HasValue &&
                            oldPropertyIds.Contains(pmd.PropertyIdOld.Value))
                        .ExecuteDeleteAsync(cancellationToken);

                if (deletedCancelledCount > 0)
                {
                    _logger.LogInformation("Deleted {Count} previous cancelled mapping records for PropertyIdNew: {PropertyIdNew}", deletedCancelledCount, newPropertyId);
                }

                // Update selected rows in one SQL query.
                var now = DateTime.Now;
                var updatedCount = await _propertyMapDetailRepository.GetQueryable()
                    .Where(pmd =>
                        pmd.PropertyIdNew == newPropertyId &&
                        pmd.PropertyIdOld.HasValue &&
                        oldPropertyIds.Contains(pmd.PropertyIdOld.Value) &&
                        pmd.IsActive &&
                        pmd.Status == PropertyMapStatus.Active)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(pmd => pmd.Status, PropertyMapStatus.Cancelled)
                            .SetProperty(pmd => pmd.IsActive, false)
                            .SetProperty(pmd => pmd.UpdatedBy, dto.UpdatedBy)
                            .SetProperty(pmd => pmd.UpdatedDate, now),
                        cancellationToken);

                // Get mappings remaining ACTIVE after demerge.
                var remainingMappings = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                    .Where(pmd =>
                        pmd.PropertyIdNew == newPropertyId && pmd.PropertyIdOld.HasValue &&
                        pmd.IsActive && pmd.Status == PropertyMapStatus.Active)
                    .Select(pmd => new { pmd.Id, pmd.PropertyIdOld, pmd.PropertyMapId })
                    .ToListAsync(cancellationToken);

                if (remainingMappings.Count == 1)
                {
                    var oneToOnePropertyMapId = await _propertyMapMasterRepository.GetQueryable().AsNoTracking()
                        .Where(x => x.IsActive && x.MappingCategory == PropertyMappingCategory.OneToOneMappingCategory)
                        .Select(x => x.Id)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (oneToOnePropertyMapId <= 0)
                    {
                        throw new InvalidOperationException("ONE_TO_ONE property mapping category was not found.");
                    }

                    var remainingMappingId = remainingMappings[0].Id;

                    var categoryUpdatedCount = await _propertyMapDetailRepository.GetQueryable()
                        .Where(pmd =>
                            pmd.PropertyIdNew == newPropertyId && pmd.PropertyIdOld.HasValue &&
                            pmd.IsActive && pmd.Status == PropertyMapStatus.Active)
                        .ExecuteUpdateAsync(
                            setters => setters.SetProperty(pmd => pmd.PropertyMapId, oneToOnePropertyMapId), cancellationToken);

                    if (categoryUpdatedCount == 0)
                    {
                        throw new InvalidOperationException("Remaining property mapping could not be converted to ONE_TO_ONE.");
                    }
                }

                var newPropertyNo = validationQuery
                    .Where(x => !string.IsNullOrWhiteSpace(x.PropertyNoNew))
                    .Select(x => x.PropertyNoNew).FirstOrDefault();

                var oldPropertyNos = validationQuery
                    .Where(x => x.PropertyIdOld.HasValue && oldPropertyIds.Contains(x.PropertyIdOld.Value) && !string.IsNullOrWhiteSpace(x.PropertyNoOld))
                    .Select(x => x.PropertyNoOld!)
                    .Distinct().ToList();

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return new PropertyMergeDto
                {
                    Success = true,
                    Message = $"Old properties {string.Join(", ", oldPropertyNos)} demerged successfully from new property no: {newPropertyNo}"
                };
            }
            else
            {
                var oldPropertyId = dto.PropertyOldIds!.First();
                var propertyIds = dto.PropertyIds!.Where(id => id > 0).Distinct().ToList();

                var validationQuery = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                    .Where(pmd =>
                        pmd.PropertyIdOld == oldPropertyId &&
                        pmd.PropertyIdNew.HasValue && propertyIds.Contains(pmd.PropertyIdNew.Value) &&
                        pmd.IsActive && pmd.Status == PropertyMapStatus.Active)
                    .Select(pmd => new
                    {
                        pmd.Id,
                        pmd.PropertyIdOld,
                        pmd.PropertyIdNew,
                        pmd.PropertyNoOld,
                        pmd.PropertyNoNew
                    })
                    .ToListAsync(cancellationToken);

                if (!validationQuery.Any())
                {
                    var propertyExists = await _propertyOldRepository.GetQueryable().AsNoTracking()
                        .AnyAsync(pm => pm.Id == oldPropertyId && pm.IsActive && !pm.MarkedForDeletion, cancellationToken);

                    throw new ValidationException("Property", propertyExists ? "No split details found to demerge" : "Property not found", OperationType.Update);
                }

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                var deletedCancelledCount = await _propertyMapDetailRepository.GetQueryable()
                       .Where(pmd =>
                           pmd.Status == PropertyMapStatus.Cancelled &&
                           pmd.PropertyIdOld == oldPropertyId &&
                           pmd.PropertyIdNew.HasValue &&
                           propertyIds.Contains(pmd.PropertyIdNew.Value))
                       .ExecuteDeleteAsync(cancellationToken);

                if (deletedCancelledCount > 0)
                {
                    _logger.LogInformation("Deleted {Count} previous cancelled mapping records for PropertyIdOld: {PropertyIdOld}", deletedCancelledCount, oldPropertyId);
                }

                var now = DateTime.Now;
                var updatedCount = await _propertyMapDetailRepository.GetQueryable()
                    .Where(pmd =>
                           pmd.PropertyIdOld == oldPropertyId &&
                           pmd.PropertyIdNew.HasValue &&
                           propertyIds.Contains(pmd.PropertyIdNew.Value) &&
                           pmd.IsActive && pmd.Status == PropertyMapStatus.Active)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(pmd => pmd.Status, PropertyMapStatus.Cancelled)
                            .SetProperty(pmd => pmd.IsActive, false)
                            .SetProperty(pmd => pmd.UpdatedBy, dto.UpdatedBy)
                            .SetProperty(pmd => pmd.UpdatedDate, now),
                        cancellationToken);

                // Get mappings remaining ACTIVE after demerge.
                var remainingMappings = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                    .Where(pmd =>
                        pmd.PropertyIdOld == oldPropertyId && pmd.PropertyIdNew.HasValue &&
                        pmd.IsActive && pmd.Status == PropertyMapStatus.Active)
                    .Select(pmd => new { pmd.Id, pmd.PropertyIdNew, pmd.PropertyMapId })
                    .ToListAsync(cancellationToken);

                if (remainingMappings.Count == 1)
                {
                    var oneToOnePropertyMapId = await _propertyMapMasterRepository.GetQueryable().AsNoTracking()
                        .Where(x => x.IsActive && x.MappingCategory == PropertyMappingCategory.OneToOneMappingCategory)
                        .Select(x => x.Id)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (oneToOnePropertyMapId <= 0)
                    {
                        throw new InvalidOperationException("ONE_TO_ONE property mapping category was not found.");
                    }

                    var remainingMappingId = remainingMappings[0].Id;

                    var categoryUpdatedCount = await _propertyMapDetailRepository.GetQueryable()
                        .Where(pmd =>
                            pmd.PropertyIdOld == oldPropertyId && pmd.PropertyIdNew.HasValue &&
                            pmd.IsActive && pmd.Status == PropertyMapStatus.Active)
                        .ExecuteUpdateAsync(
                            setters => setters.SetProperty(pmd => pmd.PropertyMapId, oneToOnePropertyMapId), cancellationToken);

                    if (categoryUpdatedCount == 0)
                    {
                        throw new InvalidOperationException("Remaining property mapping could not be converted to ONE_TO_ONE.");
                    }
                }

                var newPropertyNos = validationQuery
                    .Where(x => x.PropertyIdNew.HasValue && !string.IsNullOrWhiteSpace(x.PropertyNoNew)).Select(x => x.PropertyNoNew).Distinct().ToList();

                var oldPropertyNo = validationQuery
                    .Where(x => !string.IsNullOrWhiteSpace(x.PropertyNoOld)).Select(x => x.PropertyNoOld).FirstOrDefault();

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return new PropertyMergeDto
                {
                    Success = true,
                    Message = $"New properties {string.Join(", ", newPropertyNos)} demerged successfully from old property no: {oldPropertyNo}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error demerging property for Id: {Id}", id);
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            response.Success = false;
            response.Message = $"Error during demerge: {ex.Message}";
            return response;
        }
    }

    /// <summary>
    /// Gets detailed merge information for specified properties including old property details
    /// </summary>
    public override async Task<PropertyMergeDto?> GetByIdAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        var response = new PropertyMergeDto();
        try
        {
            var mergeDetails = await (
                from pm in _repository.GetQueryable().AsNoTracking()
                join pmd in _propertyMapDetailRepository.GetQueryable().AsNoTracking() on pm.Id equals pmd.PropertyIdNew
                join pmo in _propertyOldRepository.GetQueryable().AsNoTracking() on pmd.PropertyIdOld equals pmo.Id
                join wd in _wardRepository.GetQueryable().AsNoTracking() on pm.WardId equals wd.Id
                where pm.Id == propertyId
                    && pm.IsActive && !pm.MarkedForDeletion
                    && pmd.IsActive && pmd.Status == PropertyMapStatus.Active
                    && pmo.IsActive && !pmo.MarkedForDeletion
                select new PropertyMergeDetailDto
                {
                    Id = pm.Id,
                    WardId = pm.WardId,
                    WardNo = wd.WardNo,
                    PropertyNo = pm.PropertyNo,
                    PartitionNo = pm.PartitionNo,
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
                    OldConstructionYear = Convert.ToInt32(pmo.OldConstructionYear),
                    OldConstructionArea = pmo.OldConstructionArea
                })
                .Distinct()
                .ToListAsync(cancellationToken);

            if (!mergeDetails.Any())
            {
                response.Success = false;
                response.Message = "No merge details found for the specified properties";
                return response;
            }

            response.Success = true;
            response.Message = $"Found {mergeDetails.Count} merge detail(s)";
            response.Data = mergeDetails;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving merge details for property {PropertyId}", propertyId);
            response.Success = false;
            response.Message = $"Error retrieving merge details: {ex.Message}";
            return response;
        }
    }

    /// <summary>
    /// Gets unmerge property details based on property type (NEW or OLD) with pagination
    /// </summary>
    /// <param name="queryParams">Query parameters containing PropertyId, PropertyType ('New' or 'Old'), WingName, and pagination options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated property unmerge details</returns>
    public override async Task<PagedResult<PropertyMergeDto>> GetAllAsync(PropertyMergeQueryParameters queryParams, CancellationToken cancellationToken = default)
    {
        try
        {
            if (queryParams == null)
            {
                throw new ArgumentNullException(nameof(queryParams));
            }

            var propertyType = (queryParams.PropertyType ?? string.Empty).Trim().ToUpperInvariant();
            if (propertyType == SurveySearchStatus.New)
            {
                return await GetUnMergePropertyDetailsAsync(queryParams, cancellationToken);
            }
            else if (propertyType == SurveySearchStatus.Old)
            {
                return await GetUnMergeOldPropertyDetailsAsync(queryParams, cancellationToken);
            }
            else
            {
                throw new FilterValidationException("PropertyType", "Invalid PropertyType. Must be 'New' or 'Old'");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unmerged property details with query parameters: {@QueryParams}", queryParams);
            throw;
        }
    }

    private async Task<PagedResult<PropertyMergeDto>> GetUnMergePropertyDetailsAsync(PropertyMergeQueryParameters request, CancellationToken cancellationToken)
    {
        var propertyKey = await (
            from pm in _repository.GetQueryable().AsNoTracking()
            join societyTemp in _societyRepository.GetQueryable().AsNoTracking()
                on pm.SocietyDetailId equals societyTemp.Id
                into societyGroup
            from propertySociety in societyGroup.DefaultIfEmpty()
            where pm.Id == request.PropertyId && pm.IsActive && !pm.MarkedForDeletion
            select new { pm.WardId, pm.PropertyNo, SocietyName = propertySociety != null ? propertySociety.SocietyName : null })
            .FirstOrDefaultAsync(cancellationToken);

        if (propertyKey is null)
        {
            var emptyDto = new PropertyMergeDto
            {
                Success = false,
                Message = "Property not found",
                NewData = new List<NewPropertyDetailsDto>()
            };
            return new PagedResult<PropertyMergeDto>(new List<PropertyMergeDto> { emptyDto }, 0, request.PageNumber, request.PageSize);
        }

        var query =
            from pm in _repository.GetQueryable().AsNoTracking()
            join ward in _wardRepository.GetQueryable().AsNoTracking().Where(x => x.IsActive)
                on pm.WardId equals ward.Id
            join societyTemp in _societyRepository.GetQueryable().AsNoTracking().Where(x => x.IsActive)
                on pm.SocietyDetailId equals societyTemp.Id
                into societyGroup
            from society in societyGroup.DefaultIfEmpty()
            join propertyTypeTemp in _propertyTypeRepository.GetQueryable().AsNoTracking().Where(x => x.IsActive)
                on pm.PropertyTypeId equals propertyTypeTemp.Id
                into propertyTypeGroup
            from propertyType in propertyTypeGroup.DefaultIfEmpty()
            join wingTemp in _wingMasterRepository.GetQueryable().AsNoTracking().Where(x => x.IsActive)
                on society.WingId equals wingTemp.Id
                into wingGroup
            from wing in wingGroup.DefaultIfEmpty()
            where
                pm.WardId == propertyKey.WardId &&
                pm.PropertyNo == propertyKey.PropertyNo &&
                pm.IsActive && !pm.MarkedForDeletion &&
                pm.PartitionNo != null &&
                pm.PartitionNo != string.Empty &&
                (wing == null || pm.PartitionNo != wing.WingNo) &&
                (propertyType == null || propertyType.PartType != "Amenity") &&
                (string.IsNullOrWhiteSpace(request.WingName) || (society.WingName ?? string.Empty).Trim() == request.WingName.Trim()) &&
                !_propertyMapDetailRepository.GetQueryable()
                    .Any(map => map.PropertyIdNew == pm.Id && map.IsActive && map.Status == PropertyMapStatus.Active)

            select new NewPropertyDetailsDto
            {
                PropertyId = pm.Id,
                WardNo = ward.WardNo,
                PropertyNo = pm.PropertyNo,
                PartitionNo = pm.PartitionNo,
                OwnerName = pm.OwnerName,
                OccupierName = pm.OccupierName,
                Address = pm.Address,
                MobileNo = pm.MobileNo,
                Type = pm.Type,
                SocietyName = propertyKey.SocietyName,
                WingName = society != null ? society.WingName : null,
                FlatOrShopName = pm.FlatOrShopName,
                FlatOrShopNo = pm.FlatOrShopNo,
                PropertyTypeDescription = propertyType != null ? propertyType.PropertyDescription : null,
                BHK = _assessmentRepository.GetQueryable()
                    .Where(detail => detail.PropertyId == pm.Id && detail.IsActive && !detail.MarkedForDeletion)
                    .OrderByDescending(detail => detail.Id)
                    .Select(detail => detail.BHK)
                    .FirstOrDefault(),
            };

        var totalCount = await query.CountAsync(cancellationToken);
        var orderedQuery = query.OrderBy(x => x.PropertyId);
        List<NewPropertyDetailsDto> items;

        if (request.PageSize <= 0)
        {
            items = await orderedQuery.ToListAsync(cancellationToken);
        }
        else
        {
            items = await orderedQuery
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);
        }

        var resultDto = new PropertyMergeDto
        {
            Success = items.Count > 0 ? true : false,
            Message = items.Count > 0 ? $"Found {totalCount} new property unmerge detail(s)" : "No new property unmerge details found",
            NewData = items
        };

        return new PagedResult<PropertyMergeDto>(new List<PropertyMergeDto> { resultDto },
            totalCount, request.PageSize <= 0 ? 1 : request.PageNumber,
            request.PageSize <= 0 ? (totalCount > 0 ? totalCount : 1) : request.PageSize);
    }

    private async Task<PagedResult<PropertyMergeDto>> GetUnMergeOldPropertyDetailsAsync(PropertyMergeQueryParameters request, CancellationToken cancellationToken)
    {
        var propertyMapQuery = _propertyMapDetailRepository.GetQueryable().AsNoTracking();
        var oldPropertyQuery = _propertyOldRepository.GetQueryable().AsNoTracking();

        // get Society name
        var societyQuery = (from map in propertyMapQuery
                            join oldProperty in oldPropertyQuery
                                on map.PropertyIdOld equals oldProperty.Id
                            where map.PropertyIdNew == request.PropertyId
                                  && map.IsActive && map.Status == PropertyMapStatus.Draft
                                  && oldProperty.OldSocietyName != null
                            select oldProperty.OldSocietyName
                           ).Distinct();

        var query = from oldProperty in oldPropertyQuery
                    join societyName in societyQuery
                        on oldProperty.OldSocietyName equals societyName
                    where !propertyMapQuery.Any(map => map.PropertyIdOld == oldProperty.Id && map.IsActive && map.Status == PropertyMapStatus.Active) &&
                          (string.IsNullOrWhiteSpace(request.WingName) || (oldProperty.OldWing ?? string.Empty).Trim() == request.WingName.Trim())

                    select new OldPropertyDetailsDto
                    {
                        PropertyOldId = oldProperty.Id,
                        OldWardNo = oldProperty.OldWardNo,
                        OldPropertyNo = oldProperty.OldPropertyNo,
                        OldPartitionNo = oldProperty.OldPartitionNo,
                        OldOwnerName = oldProperty.OldOwnerName,
                        OldOccupierName = oldProperty.OldOccupierName,
                        OldAddress = oldProperty.OldAddress,
                        OldFlatOrShopNumber = oldProperty.OldFlatOrShopNumber,
                        OldWing = oldProperty.OldWing,
                        OldSocietyName = oldProperty.OldSocietyName,
                        OldRV = oldProperty.OldRV,
                        OldGeneralTax = oldProperty.OldGeneralTax,
                        OldTotalTax = oldProperty.OldTotalTax,
                        OldConstructionYear = Convert.ToInt32(oldProperty.OldConstructionYear),
                        OldConstructionArea = oldProperty.OldConstructionArea,
                        OldUseType = oldProperty.OldUseType,
                        OldMobileNo = oldProperty.OldMobileNo
                    };

        var totalCount = await query.CountAsync(cancellationToken);
        var orderedQuery = query.OrderBy(x => x.PropertyOldId);
        List<OldPropertyDetailsDto> items;

        if (request.PageSize <= 0)
        {
            items = await orderedQuery.ToListAsync(cancellationToken);
        }
        else
        {
            items = await orderedQuery
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);
        }

        var resultDto = new PropertyMergeDto
        {
            Success = items.Count > 0 ? true : false,
            Message = items.Count > 0 ? $"Found {totalCount} old property unmerge detail(s)" : "No old property unmerge details found",
            OldData = items
        };

        return new PagedResult<PropertyMergeDto>(new List<PropertyMergeDto> { resultDto },
            totalCount,
            request.PageSize <= 0 ? 1 : request.PageNumber,
            request.PageSize <= 0 ? (totalCount > 0 ? totalCount : 1) : request.PageSize);
    }

    public async Task<PropertyMergeDto> MergeMultiplePropertyAsync(PropertyMergeMultipleDto dto, CancellationToken cancellationToken = default)
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

        // Remove accidental duplicate exact pairs.
        var propertyPairs = dto.PropertyIdList.Select(x => new { x.PropertyOldId, x.PropertyId }).Distinct().ToList();

        // Validate positive IDs.
        var invalidPair = propertyPairs.FirstOrDefault(x => x.PropertyOldId <= 0 || x.PropertyId <= 0);

        if (invalidPair != null)
        {
            throw new ValidationException("Property", "Invalid data found!! ", OperationType.Create);
        }

        // One old property must be mapped to only one new property.
        var duplicateOldPropertyIds = propertyPairs.GroupBy(x => x.PropertyOldId).Where(group => group.Count() > 1).Select(group => group.Key).ToList();

        if (duplicateOldPropertyIds.Count > 0)
        {
            var duplicateOldProperties = await _propertyOldRepository.GetQueryable().AsNoTracking()
                       .Where(x => duplicateOldPropertyIds.Contains(x.Id) && x.IsActive && !x.MarkedForDeletion)
                       .Select(x => new { x.OldWardNo, x.OldPropertyNo, x.OldPartitionNo })
                       .ToListAsync(cancellationToken);

            var oldPropertyNos = BuildPropertyNumbers(
                duplicateOldProperties.Select(p => (p.OldWardNo ?? string.Empty, p.OldPropertyNo ?? string.Empty, p.OldPartitionNo ?? string.Empty)));

            throw new ValidationException("Old Property", $"The old property numbers are repeated: {string.Join(", ", oldPropertyNos)}", OperationType.Create);
        }

        // One new property must receive only one old property in one-to-one merge.
        var duplicateNewPropertyIds = propertyPairs.GroupBy(x => x.PropertyId).Where(group => group.Count() > 1).Select(group => group.Key).ToList();

        if (duplicateNewPropertyIds.Count > 0)
        {
            var duplicateNewProperties = await (
                    from property in _repository.GetQueryable().AsNoTracking()
                    join ward in _wardRepository.GetQueryable().AsNoTracking()
                        on property.WardId equals ward.Id
                    where duplicateNewPropertyIds.Contains(property.Id)
                        && property.IsActive && !property.MarkedForDeletion && ward.IsActive
                    select new { ward.WardNo, property.PropertyNo, property.PartitionNo })
                    .ToListAsync(cancellationToken);

            var newPropertyNos = BuildPropertyNumbers(
                duplicateNewProperties.Select(p => (p.WardNo ?? string.Empty, p.PropertyNo ?? string.Empty, p.PartitionNo ?? string.Empty)));

            throw new ValidationException("New Property", $"The new property numbers are repeated: {string.Join(", ", newPropertyNos)}", OperationType.Create);
        }

        // Parse and validate coordinates
        decimal? latitude = null;
        decimal? longitude = null;
        if (!string.IsNullOrWhiteSpace(dto.Latitude) && decimal.TryParse(dto.Latitude, out var latValue))
        {
            latitude = latValue;
        }

        if (!string.IsNullOrWhiteSpace(dto.Longitude) && decimal.TryParse(dto.Longitude, out var longValue))
        {
            longitude = longValue;
        }

        var oldPropertyIds = propertyPairs.Select(x => x.PropertyOldId).ToList();
        var newPropertyIds = propertyPairs.Select(x => x.PropertyId).ToList();

        var propertyMapId = await _propertyMapMasterRepository.GetQueryable().AsNoTracking()
             .Where(x => x.IsActive && x.MappingCategory == PropertyMappingCategory.OneToOneMappingCategory)
             .Select(x => x.Id)
             .FirstOrDefaultAsync(cancellationToken);

        var transactionStarted = false;
        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            transactionStarted = true;
            // Fetch all old properties in one query
            var oldProperties = await _propertyOldRepository.GetQueryable().AsNoTracking()
                .Where(property => oldPropertyIds.Contains(property.Id) && property.IsActive && !property.MarkedForDeletion)
                .Select(property => new
                {
                    property.Id,
                    property.OldWardNo,
                    property.OldPropertyNo,
                    property.OldPartitionNo,
                    property.OldOwnerName
                })
                .ToListAsync(cancellationToken);

            if (oldProperties.Count != oldPropertyIds.Count)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                transactionStarted = false;
                throw new ValidationException("Old Property", "Old properties not found ", OperationType.Create);
            }

            // Fetch all new properties and wards in one query
            var newProperties = await (
                from property in _repository.GetQueryable()
                join ward in _wardRepository.GetQueryable().AsNoTracking()
                    on property.WardId equals ward.Id
                where newPropertyIds.Contains(property.Id)
                      && property.IsActive && !property.MarkedForDeletion
                select new
                {
                    Entity = property,
                    ward.WardNo
                })
                .ToListAsync(cancellationToken);

            if (newProperties.Count != newPropertyIds.Count)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                transactionStarted = false;
                throw new ValidationException("New Property", "New properties not found ", OperationType.Create);
            }

            var oldPropertyDictionary = oldProperties.ToDictionary(x => x.Id);
            var newPropertyDictionary = newProperties.ToDictionary(x => x.Entity.Id);

            var mappingsQuery = _propertyMapDetailRepository.GetQueryable().AsNoTracking();
            var existingMappings = await mappingsQuery
                .Where(mapping =>
                    mapping.IsActive &&
                    mapping.Status == PropertyMapStatus.Active &&
                    (mapping.PropertyIdOld.HasValue && oldPropertyIds.Contains(mapping.PropertyIdOld.Value) ||
                    mapping.PropertyIdNew.HasValue && newPropertyIds.Contains(mapping.PropertyIdNew.Value)))
                .Select(mapping => new
                {
                    mapping.PropertyMapId,
                    mapping.PropertyIdOld,
                    mapping.PropertyIdNew,
                    mapping.PropertyNoOld,
                    mapping.PropertyNoNew
                })
                .ToListAsync(cancellationToken);

            // Validate that old properties are not already merged
            var alreadyMergedOldProperties = existingMappings.Select(x => x.PropertyNoOld).Distinct().ToList();
            var alreadyMergedNewProperties = existingMappings.Select(x => x.PropertyNoNew).Distinct().ToList();

            if (alreadyMergedOldProperties.Count > 0)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                transactionStarted = false;
                throw new ValidationException("Old Property", $"{string.Join(", ", alreadyMergedOldProperties)} Old properties already merged into new properties : {string.Join(", ", alreadyMergedNewProperties)}", OperationType.Create);
            }

            if (alreadyMergedNewProperties.Count > 0)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                transactionStarted = false;
                throw new ValidationException("New Property", $"{string.Join(", ", alreadyMergedNewProperties)} New properties already merged into Old properties : {string.Join(", ", alreadyMergedOldProperties)}", OperationType.Create);
            }

            // Build all map-detail records in memory
            var now = DateTime.Now;
            var createDtos = new List<PropertyMapDetailEntity>(propertyPairs.Count);
            var mergedPropertyMessages = new List<string>(propertyPairs.Count);

            foreach (var pair in propertyPairs)
            {
                var oldProperty = oldPropertyDictionary[pair.PropertyOldId];
                var newProperty = newPropertyDictionary[pair.PropertyId];
                var oldPropertyNo = BuildPropertyNumber(oldProperty.OldWardNo, oldProperty.OldPropertyNo, oldProperty.OldPartitionNo);
                var newPropertyNo = BuildPropertyNumber(newProperty.WardNo, newProperty.Entity.PropertyNo, newProperty.Entity.PartitionNo);

                // Copy OldOwnerName from PropertyMastOld to OwnerName in PropertyMast
                if (!string.IsNullOrWhiteSpace(oldProperty.OldOwnerName))
                {
                    newProperty.Entity.OwnerName = oldProperty.OldOwnerName.Trim();
                    newProperty.Entity.UpdatedBy = dto.CreatedBy;
                    newProperty.Entity.UpdatedDate = now;
                }

                var createDto = CreatePropertyMergeDetail(new PropertyMapDetailEntity()
                {
                    PropertyMapId = propertyMapId,
                    PropertyIdNew = pair.PropertyId,
                    PropertyIdOld = pair.PropertyOldId,
                    PropertyNoNew = newPropertyNo,
                    PropertyNoOld = oldPropertyNo,
                    Status = PropertyMapStatus.Active,
                    Remark = "Property Merged - Multiple One To One New Property",
                    Latitude = latitude,
                    Longitude = longitude,
                    Location = dto.Location,
                    IsActive = true,
                    CreatedBy = dto.CreatedBy
                });
                createDtos.Add(createDto);
                mergedPropertyMessages.Add($"{oldPropertyNo} -> {newPropertyNo}");
            }

            // Add all PropertyMapDetail rows
            await _propertyMapDetailRepository.AddRangeAsync(createDtos, cancellationToken);

            var affectedRecords = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (affectedRecords <= 0)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                transactionStarted = false;
                throw new ValidationException("Property", "Property data not merge", OperationType.Create);
            }

            // Commit transaction
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            transactionStarted = false;

            return new PropertyMergeDto
            {
                Success = true,
                Message = $"properties merged successfully. " + string.Join(", ", mergedPropertyMessages)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging multiple properties in bulk.");
            if (transactionStarted)
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            }
            throw;
        }
    }

    public async Task<PropertyMergeDto> DemergeMultiplePropertyAsync(PropertyDemergeMultipleDto dto, CancellationToken cancellationToken = default)
    {
        var transactionStarted = false;
        try
        {
            // 1. Validate request
            if (dto == null)
            {
                throw new ValidationException("Property", "Invalid request", OperationType.Update);
            }

            if (dto.PropertyIdList == null || dto.PropertyIdList.Count == 0)
            {
                throw new ValidationException("Property", "At least one property pair is required", OperationType.Update);
            }

            if (dto.UpdatedBy <= 0)
            {
                throw new ValidationException("User", "Invalid user", OperationType.Update);
            }

            // 2. Remove exact duplicate pairs and validate in one pass
            var propertyPairs = dto.PropertyIdList
                .Where(x => x.PropertyOldId > 0 && x.PropertyId > 0)
                .Select(x => new PropertyDemergePair { PropertyOldId = x.PropertyOldId, PropertyId = x.PropertyId })
                .DistinctBy(x => new { x.PropertyOldId, x.PropertyId })
                .ToList();

            // 3. Check if any invalid pairs were filtered out
            if (propertyPairs.Count == 0)
            {
                throw new ValidationException("Property", "Invalid property data found", OperationType.Update);
            }

            // Extract distinct IDs once
            var oldPropertyIds = propertyPairs.Select(x => x.PropertyOldId).Distinct().ToList();
            var newPropertyIds = propertyPairs.Select(x => x.PropertyId).Distinct().ToList();

            // 4. Get old-property numbers from PropertyMastOld
            var oldProperties = await _propertyOldRepository.GetQueryable().AsNoTracking()
                .Where(x => oldPropertyIds.Contains(x.Id) && x.IsActive && !x.MarkedForDeletion)
                .Select(x => new
                {
                    x.Id,
                    x.OldWardNo,
                    x.OldPropertyNo,
                    x.OldPartitionNo
                })
                .ToListAsync(cancellationToken);

            var oldPropertyNumberDictionary = oldProperties
                .ToDictionary(x => x.Id, x => BuildPropertyNumber(x.OldWardNo, x.OldPropertyNo, x.OldPartitionNo));

            // 5. Get new-property numbers from PropertyMast and WardMaster
            var newProperties = await (
                from property in _repository.GetQueryable().AsNoTracking()
                join ward in _wardRepository.GetQueryable().AsNoTracking() on property.WardId equals ward.Id
                where newPropertyIds.Contains(property.Id)
                      && property.IsActive && !property.MarkedForDeletion
                      && ward.IsActive
                select new
                {
                    property.Id,
                    ward.WardNo,
                    property.PropertyNo,
                    property.PartitionNo
                })
                .ToListAsync(cancellationToken);

            var newPropertyNumberDictionary = newProperties
                .ToDictionary(x => x.Id, x => BuildPropertyNumber(x.WardNo, x.PropertyNo, x.PartitionNo));

            // 6. Load mapping records related to requested IDs
            var allMappingRecords = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                .Where(pmd =>
                    pmd.IsActive && pmd.Status == PropertyMapStatus.Active &&
                    (
                        (pmd.PropertyIdOld.HasValue && oldPropertyIds.Contains(pmd.PropertyIdOld.Value)) ||
                        (pmd.PropertyIdNew.HasValue && newPropertyIds.Contains(pmd.PropertyIdNew.Value))
                    ))
                .Select(pmd => new PropertyMappingSelection
                {
                    Id = pmd.Id,
                    PropertyMapId = pmd.PropertyMapId,
                    PropertyIdOld = pmd.PropertyIdOld,
                    PropertyIdNew = pmd.PropertyIdNew,
                    PropertyNoOld = pmd.PropertyNoOld,
                    PropertyNoNew = pmd.PropertyNoNew,
                    Status = pmd.Status,
                    IsActive = pmd.IsActive
                })
                .ToListAsync(cancellationToken);

            // 7. Validate mapping records exist
            if (allMappingRecords.Count == 0)
            {
                throw new ValidationException("Property", "No property merging details found", OperationType.Update);
            }

            var activeModifiedMappings = allMappingRecords;

            // 8. Build optimized lookup structures for O(1) access
            var mappingsByPair = activeModifiedMappings
                .Where(x => x.PropertyIdOld.HasValue && x.PropertyIdNew.HasValue)
                .GroupBy(x => (x.PropertyIdOld.Value, x.PropertyIdNew.Value))
                .ToDictionary(g => g.Key, g => g.ToList());

            // 9. Select mapping records to update
            var selectedMappingDictionary = new Dictionary<int, PropertyMappingSelection>();
            var missingPairs = new List<string>();
            var operationDetails = new List<string>();

            foreach (var pair in propertyPairs)
            {
                var pairMappings = mappingsByPair.TryGetValue((pair.PropertyOldId, pair.PropertyId), out var found)
                    ? found
                    : new List<PropertyMappingSelection>();

                var oldPropertyNo = oldPropertyNumberDictionary.GetValueOrDefault(pair.PropertyOldId);
                var newPropertyNo = newPropertyNumberDictionary.GetValueOrDefault(pair.PropertyId);

                if (pairMappings.Count == 0)
                {
                    var missingOldDisplay = !string.IsNullOrWhiteSpace(oldPropertyNo) ? oldPropertyNo : pair.PropertyOldId.ToString();
                    var missingNewDisplay = !string.IsNullOrWhiteSpace(newPropertyNo) ? newPropertyNo : pair.PropertyId.ToString();
                    missingPairs.Add($"{missingOldDisplay} -> {missingNewDisplay}");
                    continue;
                }

                var oldPropertyDisplay = !string.IsNullOrWhiteSpace(oldPropertyNo) ? oldPropertyNo : pair.PropertyOldId.ToString();
                var newPropertyDisplay = !string.IsNullOrWhiteSpace(newPropertyNo) ? newPropertyNo : pair.PropertyId.ToString();
                operationDetails.Add($"{oldPropertyDisplay} -> {newPropertyDisplay}");

                foreach (var mapping in pairMappings)
                {
                    selectedMappingDictionary[mapping.Id] = mapping;
                }
            }

            // Validate results
            if (missingPairs.Count > 0)
            {
                throw new ValidationException("Property", "Property merging details not found for property no : " + string.Join(", ", missingPairs), OperationType.Update);
            }

            if (selectedMappingDictionary.Count == 0)
            {
                throw new ValidationException("Property", "No property merging records found to demerge", OperationType.Update);
            }

            var selectedMappings = selectedMappingDictionary.Values.ToList();
            var selectedMappingIds = selectedMappings.Select(x => x.Id).ToList();

            // 10. Begin transaction
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            transactionStarted = true;

            // 11. Find previous CANCELLED records that could cause unique-key conflict
            var selectedPropertyMapIds = selectedMappings.Select(x => x.PropertyMapId).Distinct().ToList();

            var selectedOldIds = selectedMappings
                .Where(x => x.PropertyIdOld.HasValue)
                .Select(x => x.PropertyIdOld!.Value)
                .Distinct()
                .ToList();

            var selectedNewIds = selectedMappings
                .Where(x => x.PropertyIdNew.HasValue)
                .Select(x => x.PropertyIdNew!.Value)
                .Distinct()
                .ToList();

            var cancelledCandidates = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                .Where(pmd =>
                    pmd.Status == PropertyMapStatus.Cancelled &&
                    selectedPropertyMapIds.Contains(pmd.PropertyMapId) &&
                    (
                        (pmd.PropertyIdOld.HasValue && selectedOldIds.Contains(pmd.PropertyIdOld.Value)) ||
                        (pmd.PropertyIdNew.HasValue && selectedNewIds.Contains(pmd.PropertyIdNew.Value))
                    ))
                .Select(pmd => new PropertyMappingSelection
                {
                    Id = pmd.Id,
                    PropertyMapId = pmd.PropertyMapId,
                    PropertyIdOld = pmd.PropertyIdOld,
                    PropertyIdNew = pmd.PropertyIdNew,
                    PropertyNoOld = pmd.PropertyNoOld,
                    PropertyNoNew = pmd.PropertyNoNew,
                    Status = pmd.Status,
                    IsActive = pmd.IsActive
                })
                .ToListAsync(cancellationToken);

            var selectedPairKeys = selectedMappings
                .Where(x => x.PropertyIdOld.HasValue && x.PropertyIdNew.HasValue)
                .Select(x => (x.PropertyIdOld.Value, x.PropertyIdNew.Value))
                .ToHashSet();

            var cancelledMappingIds = cancelledCandidates
                .Where(cancelled =>
                    cancelled.PropertyIdOld.HasValue &&
                    cancelled.PropertyIdNew.HasValue &&
                    selectedPairKeys.Contains((cancelled.PropertyIdOld.Value, cancelled.PropertyIdNew.Value)))
                .Select(x => x.Id)
                .ToList();

            if (cancelledMappingIds.Count > 0)
            {
                await _propertyMapDetailRepository.GetQueryable()
                    .Where(pmd => cancelledMappingIds.Contains(pmd.Id) && pmd.Status == PropertyMapStatus.Cancelled)
                    .ExecuteDeleteAsync(cancellationToken);
            }

            var now = DateTime.Now;

            // 12. Update selected records
            await _propertyMapDetailRepository.GetQueryable()
                .Where(pmd =>
                    selectedMappingIds.Contains(pmd.Id) && pmd.IsActive && pmd.Status == PropertyMapStatus.Active)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(pmd => pmd.Status, PropertyMapStatus.Cancelled)
                        .SetProperty(pmd => pmd.IsActive, false)
                        .SetProperty(pmd => pmd.UpdatedBy, dto.UpdatedBy)
                        .SetProperty(pmd => pmd.UpdatedDate, now),
                    cancellationToken);

            // 13. Check remaining active old/new mappings and update PropertyMapId
            await RecalculateRemainingPropertyMapIdsAsync(propertyPairs, dto.UpdatedBy, now, cancellationToken);

            // 14. Commit transaction
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            transactionStarted = false;

            // 15. Return property numbers instead of IDs
            return new PropertyMergeDto
            {
                Success = true,
                Message = "Properties demerged successfully. " + string.Join(", ", operationDetails)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error demerging multiple properties in bulk.");
            if (transactionStarted)
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            }
            throw;
        }
    }


    private static string BuildPropertyNumber(params string?[] propertyNumberParts)
    {
        return string.Join("-", propertyNumberParts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));
    }

    private static string BuildPropertyNumbers(IEnumerable<(string WardNo, string PropertyNo, string PartitionNo)> properties)
    {
        return string.Join(", ", properties.Select(x => BuildPropertyNumber(x.WardNo, x.PropertyNo, x.PartitionNo)).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
    }

    private static PropertyMapDetailEntity CreatePropertyMergeDetail(PropertyMapDetailEntity entity)
    {
        DateTime createdDate = DateTime.Now;
        return new PropertyMapDetailEntity
        {
            PropertyMapId = entity.PropertyMapId,
            PropertyIdNew = entity.PropertyIdNew,
            PropertyIdOld = entity.PropertyIdOld,
            PropertyNoNew = entity.PropertyNoNew,
            PropertyNoOld = entity.PropertyNoOld,
            Status = entity.Status,
            Remark = entity.Remark,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            Location = entity.Location,
            IsActive = entity.IsActive,
            CreatedBy = entity.CreatedBy,
            CreatedDate = createdDate
        };
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

    private async Task RecalculateRemainingPropertyMapIdsAsync(IReadOnlyCollection<PropertyDemergePair> propertyPairs,
        int? updatedBy, DateTime updatedDate, CancellationToken cancellationToken)
    {
        var affectedOldPropertyIds = propertyPairs
            .Select(x => x.PropertyOldId)
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        var affectedNewPropertyIds = propertyPairs
            .Select(x => x.PropertyId)
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        if (affectedOldPropertyIds.Count == 0 && affectedNewPropertyIds.Count == 0)
        {
            return;
        }

        var remainingMappings = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
            .Where(pmd =>
                pmd.IsActive &&
                pmd.Status == PropertyMapStatus.Active &&
                (
                    (pmd.PropertyIdOld.HasValue && affectedOldPropertyIds.Contains(pmd.PropertyIdOld.Value)) ||
                    (pmd.PropertyIdNew.HasValue && affectedNewPropertyIds.Contains(pmd.PropertyIdNew.Value))
                ))
            .Select(pmd => new PropertyMappingSelection
            {
                Id = pmd.Id,
                PropertyMapId = pmd.PropertyMapId,
                PropertyIdOld = pmd.PropertyIdOld,
                PropertyIdNew = pmd.PropertyIdNew,
                PropertyNoOld = pmd.PropertyNoOld,
                PropertyNoNew = pmd.PropertyNoNew,
                Status = pmd.Status,
                IsActive = pmd.IsActive
            })
            .ToListAsync(cancellationToken);

        if (remainingMappings.Count == 0)
        {
            return;
        }

        var requiredPropertyMapIdByDetailId = new Dictionary<int, int>();
        var propertyMapCategoryIds = await GetPropertyMapCategoryIdsAsync(cancellationToken);

        var remainingMappingsByOldId = remainingMappings
            .Where(x => x.PropertyIdOld.HasValue)
            .ToLookup(x => x.PropertyIdOld!.Value);

        var remainingMappingsByNewId = remainingMappings
            .Where(x => x.PropertyIdNew.HasValue)
            .ToLookup(x => x.PropertyIdNew!.Value);

        var processedMappingIds = new HashSet<int>();

        foreach (var pair in propertyPairs)
        {
            processedMappingIds.Clear();
            var relatedRemainingMappings = new List<PropertyMappingSelection>();

            foreach (var mapping in remainingMappingsByOldId[pair.PropertyOldId])
            {
                if (processedMappingIds.Add(mapping.Id))
                {
                    relatedRemainingMappings.Add(mapping);
                }
            }

            foreach (var mapping in remainingMappingsByNewId[pair.PropertyId])
            {
                if (processedMappingIds.Add(mapping.Id))
                {
                    relatedRemainingMappings.Add(mapping);
                }
            }

            if (relatedRemainingMappings.Count == 0)
            {
                continue;
            }

            var remainingOldIds = new HashSet<int>();
            var remainingNewIds = new HashSet<int>();

            foreach (var mapping in relatedRemainingMappings)
            {
                if (mapping.PropertyIdOld.HasValue)
                {
                    remainingOldIds.Add(mapping.PropertyIdOld.Value);
                }

                if (mapping.PropertyIdNew.HasValue)
                {
                    remainingNewIds.Add(mapping.PropertyIdNew.Value);
                }
            }

            var requiredPropertyMapId = ResolvePropertyMapId(
                remainingOldIds.Count,
                remainingNewIds.Count,
                propertyMapCategoryIds);

            foreach (var mapping in relatedRemainingMappings)
            {
                requiredPropertyMapIdByDetailId[mapping.Id] = requiredPropertyMapId;
            }
        }

        if (requiredPropertyMapIdByDetailId.Count == 0)
        {
            return;
        }

        var categoryGroups = requiredPropertyMapIdByDetailId
            .GroupBy(x => x.Value)
            .ToList();

        foreach (var categoryGroup in categoryGroups)
        {
            var requiredPropertyMapId = categoryGroup.Key;
            var detailIds = categoryGroup.Select(x => x.Key).ToList();

            var detailsToUpdate = remainingMappings
                .Where(x => detailIds.Contains(x.Id))
                .ToList();

            if (detailsToUpdate.Count == 0)
            {
                continue;
            }

            var remainingOldIds = detailsToUpdate
                .Where(x => x.PropertyIdOld.HasValue)
                .Select(x => x.PropertyIdOld!.Value)
                .Distinct()
                .ToList();

            var remainingNewIds = detailsToUpdate
                .Where(x => x.PropertyIdNew.HasValue)
                .Select(x => x.PropertyIdNew!.Value)
                .Distinct()
                .ToList();

            if (remainingOldIds.Count == 0 && remainingNewIds.Count == 0)
            {
                continue;
            }

            var cancelledCandidates = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                .Where(pmd =>
                    pmd.Status == PropertyMapStatus.Cancelled &&
                    pmd.PropertyMapId == requiredPropertyMapId &&
                    (
                        (pmd.PropertyIdOld.HasValue && remainingOldIds.Contains(pmd.PropertyIdOld.Value)) ||
                        (pmd.PropertyIdNew.HasValue && remainingNewIds.Contains(pmd.PropertyIdNew.Value))
                    ))
                .Select(pmd => new PropertyMappingSelection
                {
                    Id = pmd.Id,
                    PropertyMapId = pmd.PropertyMapId,
                    PropertyIdOld = pmd.PropertyIdOld,
                    PropertyIdNew = pmd.PropertyIdNew,
                    PropertyNoOld = pmd.PropertyNoOld,
                    PropertyNoNew = pmd.PropertyNoNew,
                    Status = pmd.Status,
                    IsActive = pmd.IsActive
                })
                .ToListAsync(cancellationToken);

            var activePairKeys = detailsToUpdate
                .Where(x => x.PropertyIdOld.HasValue && x.PropertyIdNew.HasValue)
                .Select(x => (x.PropertyIdOld.Value, x.PropertyIdNew.Value))
                .ToHashSet();

            var conflictingCancelledIds = cancelledCandidates
                .Where(cancelled =>
                    cancelled.PropertyIdOld.HasValue &&
                    cancelled.PropertyIdNew.HasValue &&
                    activePairKeys.Contains((cancelled.PropertyIdOld.Value, cancelled.PropertyIdNew.Value)))
                .Select(x => x.Id)
                .ToList();

            if (conflictingCancelledIds.Count > 0)
            {
                await _propertyMapDetailRepository.GetQueryable()
                    .Where(x =>
                        conflictingCancelledIds.Contains(x.Id) && x.Status == PropertyMapStatus.Cancelled)
                    .ExecuteDeleteAsync(cancellationToken);
            }

            var categoryUpdatedCount = await _propertyMapDetailRepository.GetQueryable()
                .Where(pmd =>
                    detailIds.Contains(pmd.Id) &&
                    pmd.IsActive &&
                    pmd.Status == PropertyMapStatus.Active)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(pmd => pmd.PropertyMapId, requiredPropertyMapId)
                        .SetProperty(pmd => pmd.UpdatedBy, updatedBy)
                        .SetProperty(pmd => pmd.UpdatedDate, updatedDate),
                    cancellationToken);
        }
    }

    private async Task<PropertyMapCategoryIds> GetPropertyMapCategoryIdsAsync(CancellationToken cancellationToken = default)
    {
        var requiredCategories = new[]
        {
            PropertyMappingCategory.OneToOneMappingCategory,
            PropertyMappingCategory.SplitMappingCategory,
            PropertyMappingCategory.MergeMappingCategory
        };

        var categoryRows = await _propertyMapMasterRepository.GetQueryable().AsNoTracking()
            .Where(x => x.IsActive && requiredCategories.Contains(x.MappingCategory))
            .Select(x => new { x.Id, x.MappingCategory })
            .ToListAsync(cancellationToken);

        var categoryIdByName = categoryRows.ToDictionary(x => x.MappingCategory, x => x.Id);

        int GetRequiredCategoryId(string mappingCategory)
        {
            if (!categoryIdByName.TryGetValue(mappingCategory, out var categoryId))
            {
                throw new InvalidOperationException($"Property map category {mappingCategory} was not found in the master table.");
            }
            return categoryId;
        }

        return new PropertyMapCategoryIds(
            OneToOne: GetRequiredCategoryId(PropertyMappingCategory.OneToOneMappingCategory),
            Split: GetRequiredCategoryId(PropertyMappingCategory.SplitMappingCategory),
            Merge: GetRequiredCategoryId(PropertyMappingCategory.MergeMappingCategory));
    }

    private static int ResolvePropertyMapId(int oldPropertyIdCount, int newPropertyIdCount, PropertyMapCategoryIds categoryIds)
    {
        return (oldPropertyIdCount, newPropertyIdCount) switch
        {
            (1, 1) => categoryIds.OneToOne,
            (1, > 1) => categoryIds.Split,
            ( > 1, 1) => categoryIds.Merge,
            _ => throw new InvalidOperationException($"Unable to determine PropertyMapCategory. Remaining old property count: {oldPropertyIdCount}, " +
                $"remaining new property count: {newPropertyIdCount}.")
        };
    }
}
