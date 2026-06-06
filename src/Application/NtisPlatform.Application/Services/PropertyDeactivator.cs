using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Handles deactivation of combined properties and their related records
/// </summary>
public class PropertyDeactivator : IPropertyDeactivator
{
    private readonly IRepository<PropertyEntity, int> _propertyRepository;
    private readonly IRepository<PropertyMastOldEntity, int> _propertyMastOldRepository;
    private readonly IRepository<PropertyDetailsEntity, int> _propertyDetailsRepository;
    private readonly IRepository<PropertyAssessmentEntity, int> _propertyAssessmentRepository;
    private readonly IRepository<TransMastRVEntity> _transMastRepository;
    private readonly IRepository<TaxPendingDetailsEntity> _taxPendingRepository;
    private readonly IRepository<RoomWiseSubmissionDetailsEntity, int> _roomWiseSubmissionRepository;
    private readonly IRepository<RoomWiseMinusDataEntity, int> _roomWiseMinusDataRepository;
    private readonly ILogger<PropertyDeactivator> _logger;

    public PropertyDeactivator(
        IRepository<PropertyEntity, int> propertyRepository,
        IRepository<PropertyMastOldEntity, int> propertyMastOldRepository,
        IRepository<PropertyDetailsEntity, int> propertyDetailsRepository,
        IRepository<PropertyAssessmentEntity, int> propertyAssessmentRepository,
        IRepository<TransMastRVEntity> transMastRepository,
        IRepository<TaxPendingDetailsEntity> taxPendingRepository,
        IRepository<RoomWiseSubmissionDetailsEntity, int> roomWiseSubmissionRepository,
        IRepository<RoomWiseMinusDataEntity, int> roomWiseMinusDataRepository,
        ILogger<PropertyDeactivator> logger)
    {
        _propertyRepository = propertyRepository;
        _propertyMastOldRepository = propertyMastOldRepository;
        _propertyDetailsRepository = propertyDetailsRepository;
        _propertyAssessmentRepository = propertyAssessmentRepository;
        _transMastRepository = transMastRepository;
        _taxPendingRepository = taxPendingRepository;
        _roomWiseSubmissionRepository = roomWiseSubmissionRepository;
        _roomWiseMinusDataRepository = roomWiseMinusDataRepository;
        _logger = logger;
    }

    public async Task DeactivateCombinedPropertiesAsync(
        List<int> propertyIds,
        CancellationToken cancellationToken = default)
    {
        if (propertyIds == null || propertyIds.Count == 0)
        {
            _logger.LogWarning("DeactivateCombinedPropertiesAsync called with empty or null propertyIds");
            return;
        }

        _logger.LogInformation("Starting deactivation for {Count} properties: [{PropertyIds}]",
            propertyIds.Count, string.Join(", ", propertyIds));

        // 1. PropertyMast - Set IsActive=0
        var propertyMastCount = await _propertyRepository.GetQueryable()
            .Where(pm => propertyIds.Contains(pm.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false), cancellationToken);
        _logger.LogInformation("Deactivated {Count} PropertyMast records", propertyMastCount);

        // 2. PropertyMastDetails - Set IsActive=0
        var propertyAssessmentCount = await _propertyAssessmentRepository.GetQueryable()
            .Where(pmd => propertyIds.Contains(pmd.PropertyId))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false), cancellationToken);
        _logger.LogInformation("Deactivated {Count} PropertyAssessment records", propertyAssessmentCount);

        // 3. PropertyMastOld - Soft delete for combined properties
        var combinedPropertiesWithOldIds = await _propertyRepository.GetQueryable()
            .Where(pm => propertyIds.Contains(pm.Id) && pm.PropertyMastOldId != null)
            .Select(pm => new { pm.Id, pm.PropertyMastOldId })
            .ToListAsync(cancellationToken);

        var propertyMastOldIds = combinedPropertiesWithOldIds
            .Where(x => x.PropertyMastOldId.HasValue)
            .Select(x => x.PropertyMastOldId!.Value)
            .Distinct()
            .ToList();

        if (propertyMastOldIds.Count > 0)
        {
            var softDeleteTimestamp = DateTime.Now;
            var propertyMastOldCount = await _propertyMastOldRepository.GetQueryable()
                .Where(pmo => propertyMastOldIds.Contains(pmo.Id))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.IsActive, false)
                    .SetProperty(p => p.MarkedForDeletion, true)
                    .SetProperty(p => p.MarkedForDeletionDate, softDeleteTimestamp)
                    .SetProperty(p => p.UpdatedDate, softDeleteTimestamp),
                    cancellationToken);
            _logger.LogInformation("Soft-deleted {Count} PropertyMastOld records", propertyMastOldCount);
        }
        else
        {
            _logger.LogDebug("No PropertyMastOld records found for combined properties {PropertyIds}",
                string.Join(",", propertyIds));
        }

        // 4. PropertyDetails - Set BOTH IsActive=0 AND IsTaxable=0
        var propertyDetailsCount = await _propertyDetailsRepository.GetQueryable()
            .Where(pd => propertyIds.Contains(pd.PropertyId))
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.IsActive, false)
                .SetProperty(p => p.IsTaxable, false), cancellationToken);
        _logger.LogInformation("Deactivated {Count} PropertyDetails records", propertyDetailsCount);

        // 5. RoomWiseSubmissionDetails - Set IsActive=0 (with null check for PropertyId)
        var roomWiseSubmissionCount = await _roomWiseSubmissionRepository.GetQueryable()
            .Where(rwsd => rwsd.PropertyId.HasValue && propertyIds.Contains(rwsd.PropertyId.Value))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false), cancellationToken);
        _logger.LogInformation("Deactivated {Count} RoomWiseSubmissionDetails records", roomWiseSubmissionCount);

        // 6. RoomWiseMinusData (via RoomWiseSubmissionDetails) - Set IsActive=0
        var roomWiseSubmissionIds = await _roomWiseSubmissionRepository.GetQueryable()
            .Where(rwsd => rwsd.PropertyId.HasValue && propertyIds.Contains(rwsd.PropertyId.Value))
            .Select(rwsd => rwsd.Id)
            .ToListAsync(cancellationToken);

        if (roomWiseSubmissionIds.Count > 0)
        {
            var roomWiseMinusDataCount = await _roomWiseMinusDataRepository.GetQueryable()
                .Where(rwmd => roomWiseSubmissionIds.Contains(rwmd.RoomWiseSubmissionId))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false), cancellationToken);
            _logger.LogInformation("Deactivated {Count} RoomWiseMinusData records", roomWiseMinusDataCount);
        }
        else
        {
            _logger.LogDebug("No RoomWiseSubmissionDetails found, skipping RoomWiseMinusData deactivation");
        }

        // 7. TransMast - Set IsActive=0
        var transMastCount = await _transMastRepository.GetQueryable()
            .Where(tm => propertyIds.Contains(tm.PropertyId))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false), cancellationToken);
        _logger.LogInformation("Deactivated {Count} TransMast records", transMastCount);

        // 8. TaxPendingDetails - Do NOT set IsActive=0
        // TaxPendingDetails records are handled by CombinePropertyTaxService.AggregatePendingTaxesAsync()
        // which zeroes out the PendingAmount and sets PendingFixed=true, but keeps IsActive=1
        //
        // FUTURE WORK NOTE:
        // - Currently, only Rateable Value (RV) taxes are calculated and updated during property combination
        //   using RateableValueService.CalculateAndSaveAsync()
        // - Capital Value (CV) tax calculation and update will be implemented in a future PR
        _logger.LogInformation("TaxPendingDetails records will be handled by CombinePropertyTaxService (IsActive kept as 1)");

        _logger.LogInformation("Completed deactivation for {Count} properties", propertyIds.Count);
    }

    public async Task EnsureMainPropertyRecordsActiveAsync(
        int mainPropertyId,
        CancellationToken cancellationToken = default)
    {
        if (mainPropertyId <= 0)
        {
            _logger.LogWarning("EnsureMainPropertyRecordsActiveAsync called with invalid mainPropertyId: {PropertyId}", mainPropertyId);
            return;
        }

        _logger.LogInformation("Ensuring active status for main property {PropertyId}", mainPropertyId);

        // PropertyDetails
        var propertyDetailsCount = await _propertyDetailsRepository.GetQueryable()
            .Where(pd => pd.PropertyId == mainPropertyId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, true), cancellationToken);
        _logger.LogInformation("Activated {Count} PropertyDetails records for main property", propertyDetailsCount);

        // RoomWiseSubmissionDetails (with null check for PropertyId)
        var roomWiseSubmissionCount = await _roomWiseSubmissionRepository.GetQueryable()
            .Where(rwsd => rwsd.PropertyId.HasValue && rwsd.PropertyId.Value == mainPropertyId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, true), cancellationToken);
        _logger.LogInformation("Activated {Count} RoomWiseSubmissionDetails records for main property", roomWiseSubmissionCount);

        // RoomWiseMinusData (via RoomWiseSubmissionDetails)
        var mainRoomWiseIds = await _roomWiseSubmissionRepository.GetQueryable()
            .Where(rwsd => rwsd.PropertyId.HasValue && rwsd.PropertyId.Value == mainPropertyId)
            .Select(rwsd => rwsd.Id)
            .ToListAsync(cancellationToken);

        if (mainRoomWiseIds.Count > 0)
        {
            var roomWiseMinusDataCount = await _roomWiseMinusDataRepository.GetQueryable()
                .Where(rwmd => mainRoomWiseIds.Contains(rwmd.RoomWiseSubmissionId))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, true), cancellationToken);
            _logger.LogInformation("Activated {Count} RoomWiseMinusData records for main property", roomWiseMinusDataCount);
        }
        else
        {
            _logger.LogDebug("No RoomWiseSubmissionDetails found for main property, skipping RoomWiseMinusData activation");
        }

        _logger.LogInformation("Completed activation check for main property {PropertyId}", mainPropertyId);
    }
}