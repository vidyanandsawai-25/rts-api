using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.PropertyMapDetails;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class PropertyMappingService : BaseCommonCrudService<PropertyMapDetailEntity, PropertyMapDetailDto, CreatePropertyMapDetailsDto, UpdatePropertyMapDetailsDto, PropertyMapDetailsQueryParameters, int>, IPropertyMappingService
{
    private readonly IRepository<PropertyMapMasterEntity, int> _propertyMapMasterRepository;
    private readonly IRepository<PropertyMastOldEntity, int> _propertyOldRepository;
    private readonly IRepository<PropertyMapDetailEntity, int> _propertyMapDetailRepository;
    private readonly new IRepository<PropertyEntity, int> _repository;
    private readonly IRepository<WardEntity, int> _wardRepository;
    private readonly new IUnitOfWork _unitOfWork;
    private readonly ILogger<PropertyMappingService> _logger;

    public PropertyMappingService(
        IRepository<PropertyMapMasterEntity, int> propertyMapMasterRepository,
        IRepository<PropertyMastOldEntity, int> propertyOldRepository,
        IRepository<PropertyMapDetailEntity, int> propertyMapDetailRepository,
        IRepository<PropertyEntity, int> repository,
        IRepository<WardEntity, int> wardRepository,
        IUnitOfWork unitOfWork,
        ILogger<PropertyMappingService> logger,
        IMapper mapper) : base(propertyMapDetailRepository, unitOfWork, mapper)
    {
        _propertyMapMasterRepository = propertyMapMasterRepository;
        _propertyOldRepository = propertyOldRepository;
        _propertyMapDetailRepository = propertyMapDetailRepository;
        _repository = repository;
        _wardRepository = wardRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public override async Task<PropertyMapDetailDto> CreateAsync(CreatePropertyMapDetailsDto dto,CancellationToken cancellationToken = default)
    {
        try
        {
            // Validation
            if (dto.PropertyOldId == null || dto.PropertyOldId.Count == 0 || dto.PropertyOldId.Any(id => !id.HasValue || id.Value <= 0))
            {
                throw new ValidationException("Old property ", "Old property number is required.", OperationType.Create);
            }
            // Parse coordinates
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
            // get property map id and validate exists
            var propertyMapId = await _propertyMapMasterRepository.GetQueryable().AsNoTracking()
                        .Where(pm => pm.MappingCategory == "MAP" && pm.IsActive)
                        .Select(pm => pm.Id).FirstOrDefaultAsync(cancellationToken);

            if (propertyMapId <= 0)
            {
                throw new ValidationException("Property Map Category", "Property mapping type was not found.", OperationType.Create);
            }

            var upperFlag = dto.Flag.ToUpperInvariant();
            var propertyOldIds = dto.PropertyOldId.Distinct().ToList();

            if (upperFlag == "MAP")
            {
                return await HandleMapOperationAsync(dto, propertyOldIds, latitude, longitude, propertyMapId, cancellationToken);
            }
            else if (upperFlag == "UNMAP")
            {
                return await HandleUnmapOperationAsync(dto, propertyOldIds, propertyMapId, cancellationToken);
            }
            else
            {
                throw new ValidationException("Flag", "Invalid flag.", OperationType.Create);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding property mapping details for PropertyId {PropertyId}", dto.PropertyId);
            throw;
        }
    }

    private async Task<PropertyMapDetailDto> HandleMapOperationAsync(CreatePropertyMapDetailsDto dto, List<int?> propertyOldIds, decimal? latitude, decimal? longitude, int propertyMapId, CancellationToken cancellationToken)
    {
        // Validate all PropertyOld records exist
        var propertyMastOld = await _propertyOldRepository.GetQueryable().AsNoTracking()
            .Where(p => propertyOldIds.Contains(p.Id) && p.IsActive && !p.MarkedForDeletion && p.OldPropertyNo != null)
            .Select(p => new { p.Id, p.OldWardNo, p.OldPropertyNo, p.OldPartitionNo })
            .ToListAsync(cancellationToken);

        var missingIds = propertyOldIds.Where(id => !propertyMastOld.Any(p => p.Id == id)).ToList();

        if (missingIds.Any())
        {
            throw new ValidationException("Old Property no", "Old property was not found.", OperationType.Create);
        }

        // Check for existing attached property
        var existingPropertyNumbers = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                    .Where(pmd =>
                        pmd.PropertyMapId == propertyMapId &&
                        pmd.Status == "DRAFT" &&
                        pmd.PropertyIdOld.HasValue &&
                        propertyOldIds.Contains(pmd.PropertyIdOld.Value) &&
                        pmd.IsActive)
                    .Select(pmd => new { pmd.PropertyNoOld, pmd.PropertyNoNew })
                    .Where(propertyNo => !string.IsNullOrWhiteSpace(propertyNo.PropertyNoOld))
                    .Distinct().ToListAsync(cancellationToken);

        if (existingPropertyNumbers.Any())
        {
            var oldPropertyNumbers = string.Join(", ", existingPropertyNumbers.Select(x => x.PropertyNoOld).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
            var newPropertyNumber = existingPropertyNumbers.Select(x => x.PropertyNoNew).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            throw new ValidationException("Old Property No", $"{oldPropertyNumbers} This Properties already attached to property no : {newPropertyNumber}", OperationType.Create);
        }

        // Get existing draft records
        var existingDraftIds = await _propertyMapDetailRepository.GetQueryable()
            .Where(p => p.PropertyMapId == propertyMapId
                     && propertyOldIds.Contains(p.PropertyIdOld)
                     && p.PropertyIdNew == dto.PropertyId
                     && p.Status == "DRAFT"
                     && !p.IsActive)
            .Select(p => p.PropertyIdOld!.Value)
            .ToListAsync(cancellationToken);

        // Prepare entities
        var now = DateTime.UtcNow;
        var entitiesToAdd = new List<PropertyMapDetailEntity>();
        var entitiesToUpdate = new List<PropertyMapDetailEntity>();
        foreach (var propertyOld in propertyMastOld)
        {
            if (!existingDraftIds.Contains(propertyOld.Id))
            {
                var oldPropertyNo = BuildPropertyNumber(propertyOld.OldWardNo, propertyOld.OldPropertyNo, propertyOld.OldPartitionNo);

                var newEntity = new PropertyMapDetailEntity
                {
                    PropertyMapId = propertyMapId,
                    PropertyIdNew = dto.PropertyId,
                    PropertyIdOld = propertyOld.Id,
                    PropertyNoOld = oldPropertyNo,
                    PropertyNoNew = "",
                    Status = "DRAFT",
                    Remark = dto.Remark,
                    Latitude = latitude,
                    Longitude = longitude,
                    Location = dto.Location,
                    IsActive = false,
                    CreatedBy = dto.CreatedBy,
                    CreatedDate = now
                };
                entitiesToAdd.Add(newEntity);
            }
        }

        // Save changes
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (existingDraftIds.Count > 0)
            {
                await _propertyMapDetailRepository.GetQueryable()
                    .Where(p => p.PropertyMapId == propertyMapId
                             && existingDraftIds.Contains(p.PropertyIdOld!.Value)
                             && p.PropertyIdNew == dto.PropertyId
                             && p.Status == "DRAFT"
                             && !p.IsActive)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.Remark, dto.Remark)
                        .SetProperty(p => p.Latitude, latitude)
                        .SetProperty(p => p.Longitude, longitude)
                        .SetProperty(p => p.Location, dto.Location)
                        .SetProperty(p => p.UpdatedBy, dto.CreatedBy)
                        .SetProperty(p => p.UpdatedDate, now),
                        cancellationToken);
            }
            if (entitiesToAdd.Count > 0)
            {
                await _propertyMapDetailRepository.AddRangeAsync(entitiesToAdd, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Saved {AddCount} new and updated {UpdateCount} property map details", entitiesToAdd.Count, entitiesToUpdate.Count);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        _logger.LogInformation("Property draft saved successfully for PropertyMapId: {PropertyMapId}", propertyMapId);
        return null!;
    }

    private async Task<PropertyMapDetailDto> HandleUnmapOperationAsync(CreatePropertyMapDetailsDto dto, List<int?> propertyOldIds, int propertyMapId, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            // First, delete any existing CANCELLED records for cleanup
            var deletedRows = await _propertyMapDetailRepository.GetQueryable()
                .Where(p => p.PropertyMapId == propertyMapId
                         && propertyOldIds.Contains(p.PropertyIdOld)
                         && p.Status == "CANCELLED")
                .ExecuteDeleteAsync(cancellationToken);

            if (deletedRows > 0)
            {
                _logger.LogInformation("Deleted {Count} existing CANCELLED records for PropertyMapId: {PropertyMapId}", deletedRows, propertyMapId);
            }

            var affectedRows = await _propertyMapDetailRepository.GetQueryable()
                .Where(p => p.PropertyMapId == propertyMapId
                         && propertyOldIds.Contains(p.PropertyIdOld)
                         && (p.Status == "DRAFT" || p.Status == "ACTIVE"))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.Status, "CANCELLED")
                    .SetProperty(p => p.IsActive, false)
                    .SetProperty(p => p.UpdatedBy, dto.CreatedBy)
                    .SetProperty(p => p.UpdatedDate, now), cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Unmapped {Count} property details for PropertyMapId: {PropertyMapId}", affectedRows, propertyMapId);

            if (affectedRows == 0)
            {
                throw new ValidationException("Old Property no", "No records found to DeAttached.", OperationType.Update);
            }
            return null!;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public override async Task<PropertyMapDetailDto?> UpdateAsync(int id, UpdatePropertyMapDetailsDto dto, CancellationToken cancellationToken = default)
    {
        // Validate property exists
        var propertyMast = await (
                   from pm in _repository.GetQueryable().AsNoTracking()
                   join wd in _wardRepository.GetQueryable().AsNoTracking().Where(x => x.IsActive) on pm.WardId equals wd.Id
                   where pm.Id == dto.PropertyId && pm.IsActive && !pm.MarkedForDeletion
                   select new { pm.Id, wd.WardNo, pm.PropertyNo, pm.PartitionNo })
                   .FirstOrDefaultAsync(cancellationToken);

        if (propertyMast == null)
        {
            throw new ValidationException("Property No", "New Property not found", OperationType.Update);
        }

        // get property map id and validate exists
        var propertyMapId = await _propertyMapMasterRepository.GetQueryable().AsNoTracking()
                    .Where(pm => pm.MappingCategory == "MAP" && pm.IsActive)
                    .Select(pm => pm.Id).FirstOrDefaultAsync(cancellationToken);

        if (propertyMapId <= 0)
        {
            throw new ValidationException("Property Map Category", "Property mapping type was not found.", OperationType.Update);
        }

        // Extract filter criteria
        var societyNames = dto.SocietyDetails
            .Where(s => !string.IsNullOrWhiteSpace(s.OldSocietyName)).Select(s => s.OldSocietyName.Trim())
            .Distinct().ToHashSet();

        var wardNumbers = dto.SocietyDetails
            .Where(s => !string.IsNullOrWhiteSpace(s.OldWardNo)).Select(s => s.OldWardNo.Trim())
            .Distinct().ToHashSet();

        // Get matching PropertyOld IDs
        var hasSocietyFilter = societyNames.Count > 0;
        var hasWardFilter = wardNumbers.Count > 0;

        var matchingMappings = await (
            from pmo in _propertyOldRepository.GetQueryable()
            join pmd in _propertyMapDetailRepository.GetQueryable() on pmo.Id equals pmd.PropertyIdOld
            where pmd.Status == "DRAFT"
                  && pmd.PropertyMapId == propertyMapId
                  && (!hasSocietyFilter || (pmo.OldSocietyName != null && societyNames.Contains(pmo.OldSocietyName.Trim())))
                  && (!hasWardFilter || (pmo.OldWardNo != null && wardNumbers.Contains(pmo.OldWardNo.Trim())))
                  //&& !pmd.IsActive
                  && pmo.IsActive
                  && !pmo.MarkedForDeletion
            select new
            {
                PropertyOldId = pmo.Id,
                pmd.PropertyIdNew,
                pmd.PropertyNoOld,
                pmd.PropertyNoNew,
                pmd.IsActive
            })
            .Distinct()
            .ToListAsync(cancellationToken);

        var matchingPropertyOldIds = matchingMappings.Where(x => !x.IsActive).Select(x => x.PropertyOldId).Distinct().ToList();

        if (!matchingPropertyOldIds.Any())
        {
            throw new ValidationException("SocietyName", "Property attach details were not found.", OperationType.Update);
        }

        // Check if any of the matching records are already ACTIVE
        var activeMappings = matchingMappings.Where(x => x.IsActive && x.PropertyIdNew != dto.PropertyId).ToList();

        if (activeMappings.Count > 0)
        {
            var oldPropertyNumbers = string.Join(", ", activeMappings.Select(x => x.PropertyNoOld).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
            var newPropertyNumber = activeMappings.Select(x => x.PropertyNoNew).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            throw new ValidationException("Old Properties", $"{oldPropertyNumbers} This Properties already attached to property no : {newPropertyNumber}", OperationType.Update);
        }

        // Update mapping details
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var updateTime = DateTime.UtcNow;
            var newPropertyNo = BuildPropertyNumber(propertyMast.WardNo, propertyMast.PropertyNo, propertyMast.PartitionNo);
            var updateCount = await _propertyMapDetailRepository.GetQueryable()
                .Where(pmd => pmd.Status == "DRAFT"
                           && pmd.PropertyMapId == propertyMapId
                           && matchingPropertyOldIds.Contains(pmd.PropertyIdOld!.Value)
                           && !pmd.IsActive)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.PropertyIdNew, dto.PropertyId)
                    .SetProperty(p => p.PropertyNoNew, newPropertyNo)
                    .SetProperty(p => p.IsActive, true)
                    .SetProperty(p => p.UpdatedBy, dto.UpdatedBy)
                    .SetProperty(p => p.UpdatedDate, updateTime),
                    cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Updated {Count} attach details to active for PropertyMapId: {PropertyMapId}", updateCount, propertyMapId);

            if (updateCount == 0)
            {
                throw new ValidationException("SocietyName", "Property attach details were not found.", OperationType.Update);
            }

            var updatedEntity = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                    .Where(pmd =>
                        pmd.Status == "DRAFT" &&
                        pmd.PropertyMapId == propertyMapId &&
                        pmd.PropertyIdNew == dto.PropertyId &&
                        pmd.IsActive &&
                        pmd.PropertyIdOld.HasValue &&
                        matchingPropertyOldIds.Contains(pmd.PropertyIdOld.Value))
                    .OrderByDescending(pmd => pmd.UpdatedDate)
                    .FirstOrDefaultAsync(cancellationToken);

            _logger.LogInformation("Property attach details updated successfully for PropertyId: {PropertyId}", dto.PropertyId);
            return _mapper.Map<PropertyMapDetailDto>(updatedEntity);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private static string BuildPropertyNumber(params string?[] propertyNumberParts)
    {
        return string.Join("-", propertyNumberParts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));
    }
}
