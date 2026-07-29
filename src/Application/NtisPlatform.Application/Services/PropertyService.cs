using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.DTOs.PropertyDetails;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Options;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;
using System.Text;
using DataValidationException = System.ComponentModel.DataAnnotations.ValidationException;


namespace NtisPlatform.Application.Services;

/// <summary>
/// Global Property Service - Used across all features
/// Provides property search, lookup, and master data functionality
/// </summary>
public partial class PropertyService
    : BaseCommonCrudService<PropertyEntity, PropertyDto, CreatePropertyDto, UpdatePropertyDto, PropertyQueryParameters, int>,
      IPropertyService
{
    private readonly IPropertyRepository _propertyRepository;
    private readonly ILogger<PropertyService> _logger;
    private readonly FeatureFlagsOptions _featureFlags;
    private readonly IRepository<WardEntity, int> _wardRepository;
    private readonly IRepository<PropertyCategoryEntity, int> _categoryRepository;
    private readonly IRepository<SocietyDetailsEntity, int> _societyRepository;
    private readonly IRepository<PropertyDetailsEntity, int> _propertyDetailsRepository;
    private readonly IRepository<RoomWiseSubmissionDetailsEntity, int> _roomWiseRepository;
    private readonly IRepository<PropertyAssessmentEntity, int> _assessmentRepository;
    private readonly IRepository<GlobalSurveyWardAllocationEntity, int> _wardAllocationRepository;
    private readonly IRepository<UserEntity, int> _userRepository;
    private readonly IRepository<PropertyMapMasterEntity, int> _propertyMapMasterRepository;
    private readonly IRepository<PropertyMapDetailEntity, int> _propertyMapDetailRepository;
    private readonly IRepository<WingEntity, int> _wingMasterRepository;
    private readonly IPropertyRuleApplicationLogService? _ruleLogService;
    private readonly IRepository<PropertyMastOldEntity, int> _propertyOldRepository;
    private readonly IRepository<PropertyTypeMasterEntity, int> _propertyTypeRepository;


    public PropertyService(
        IRepository<PropertyEntity, int> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IPropertyRepository propertyRepository,
        ILogger<PropertyService> logger,
        IOptions<FeatureFlagsOptions> featureFlags,
        IRepository<WardEntity, int> wardRepository,
        IRepository<PropertyCategoryEntity, int> categoryRepository,
        IRepository<SocietyDetailsEntity, int> societyRepository,
        IRepository<PropertyDetailsEntity, int> propertyDetailsRepository,
        IRepository<RoomWiseSubmissionDetailsEntity, int> roomWiseRepository,
        IRepository<PropertyAssessmentEntity, int> assessmentRepository,
        IRepository<GlobalSurveyWardAllocationEntity, int> wardAllocationRepository,
        IRepository<PropertyMapMasterEntity, int> propertyMapMasterRepository,
        IRepository<PropertyMapDetailEntity, int> propertyMapDetailRepository,
        IRepository<WingEntity, int> wingMasterRepository,
        IRepository<UserEntity, int> userRepository,
        IRepository<PropertyMastOldEntity, int> propertyOldRepository,
        IRepository<PropertyTypeMasterEntity, int> propertyTypeRepository,
        IPropertyRuleApplicationLogService? ruleLogService = null)
        : base(repository, unitOfWork, mapper)
    {
        _propertyRepository = propertyRepository;
        _logger = logger;
        _featureFlags = featureFlags.Value;
        _wardRepository = wardRepository;
        _categoryRepository = categoryRepository;
        _societyRepository = societyRepository;
        _propertyDetailsRepository = propertyDetailsRepository;
        _roomWiseRepository = roomWiseRepository;
        _assessmentRepository = assessmentRepository;
        _ruleLogService = ruleLogService;
        _wardAllocationRepository = wardAllocationRepository;
        _userRepository = userRepository;
        _propertyMapMasterRepository = propertyMapMasterRepository;
        _propertyMapDetailRepository = propertyMapDetailRepository;
        _propertyOldRepository = propertyOldRepository;
        _propertyTypeRepository = propertyTypeRepository;
        _wingMasterRepository = wingMasterRepository;
    }


    public async Task<PropertyTaxDetailsDto?> GetTaxDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.GetTaxDetailsAsync(propertyId, cancellationToken);
    }

    public async Task<PropertyTaxDetailsCVDto?> GetTaxDetailsCVAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.GetTaxDetailsCVAsync(propertyId, cancellationToken);
    }

    public async Task<PropertyTaxApartmentDetailsDto?> GetAggregatedPropertyTaxDetailsAsync(PropertyApartmentTaxRequestDto dto, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.GetAggregatedPropertyTaxDetailsAsync(dto, cancellationToken);
    }

    public async Task<PropertyTaxApartmentDetailsCVDto?> GetAggregatedPropertyTaxDetailsCVAsync(PropertyApartmentTaxRequestDto dto, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.GetAggregatedPropertyTaxDetailsCVAsync(dto, cancellationToken);
    }

    public async Task<List<BuildingGenerateStructureDto>?> GetGenerateBuildingStructureAsync(BuildingGenerateDetailsDto dto, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.GetGenerateBuildingStructureAsync(dto, cancellationToken);
    }
    public async Task<List<SocietyAminityDetailsDto>?> GetSocietyAmenityDetailsAsync(int SocietyDetailId, bool isAmenity, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.GetSocietyAmenityDetailsAsync(SocietyDetailId, isAmenity, cancellationToken);
    }
    public async Task<List<PropertySocietyDetailsDto>?> GetSocietyWingListAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.GetSocietyWingListAsync(propertyId, cancellationToken);
    }
    public async Task<List<BuildingListDto>?> GetBuildingListAsync(int wardId, CancellationToken cancellationToken = default)
    {
        return await _propertyRepository.GetBuildingListAsync(wardId, cancellationToken);
    }



    /// <summary>
    /// Validates if a property can be safely deleted.
    /// 
    /// PARTITION NUMBER VALIDATION: Properties with partition numbers must be deleted in descending PropertyId order.
    /// Since PropertyId is auto-incremented, higher ID = newer/higher partition (A7 > A6 > A1).
    /// Example: For partitions A1-A7 with IDs 552374-552380, you must delete 552380 (A7) first, then 552379 (A6), etc.
    /// No gaps allowed - you cannot delete 552380 and 552378 while 552379 exists.
    /// 
    /// ⚠️ PAYMENT VALIDATION INCOMPLETE - Payment validation is a future feature pending BillTransactionDetails/BillTransactionDiscountDetails entities.
    /// Feature flag acts as temporary ON/OFF gate, not real validation.
    /// 
    /// Feature Flag: AllowPropertyDeletionWithoutPaymentValidation
    /// - false (production default): Blocks all deletions, safe for production
    /// - true (dev only): Allows all deletions without checks, development/testing only
    /// 
    /// Implementation Steps: Create entities, add navigation properties, uncomment validation queries, update flag default, add tests.
    /// Future Validation Roadmap: Phase 1 - Payment validation | Phase 2 - Assessments, tax calculations, property status, legal compliance, business rules
    /// </summary>
    protected override async Task<ValidationResult> ValidateForDeleteAsync(int id, PropertyEntity entity, CancellationToken cancellationToken = default)
    {
        return await ValidateForDeleteInternalAsync(id, entity, cancellationToken, skipPartitionValidation: false);
    }

    /// <summary>
    /// Internal validation method with option to skip partition validation.
    /// Used by bulk delete to avoid redundant validation after upfront bulk partition validation.
    /// </summary>
    private async Task<ValidationResult> ValidateForDeleteInternalAsync(int id, PropertyEntity entity, CancellationToken cancellationToken, bool skipPartitionValidation)
    {
        // PARTITION NUMBER VALIDATION
        // Properties with partition numbers must be deleted in descending PropertyId order (highest ID first)
        // Since PropertyId is auto-incremented, this ensures logical deletion sequence for partitioned properties
        // Skip this validation in bulk delete scenarios where upfront bulk validation already occurred
        if (!skipPartitionValidation && !string.IsNullOrWhiteSpace(entity.PartitionNo))
        {
            var partitionValidation = await ValidatePartitionDeletionOrderAsync(entity, cancellationToken);
            if (!partitionValidation.IsValid)
            {
                return partitionValidation;
            }
        }

        // Check feature flag to determine if payment validation should be enforced
        var allowDeletionWithoutPaymentValidation = _featureFlags.AllowPropertyDeletionWithoutPaymentValidation;

        if (!allowDeletionWithoutPaymentValidation)
        {
            // PHASE 1: Payment validation
            // Check for payment records that would prevent deletion
            // TODO: Uncomment once BillTransactionDetails and BillTransactionDiscountDetails entities are created

            // var billTransactionDetails = await _repository.GetQueryable()
            //     .AsNoTracking()
            //     .Where(p => p.Id == id)
            //     .SelectMany(p => p.BillTransactionDetails)
            //     .AnyAsync(cancellationToken);
            //
            // var billTransactionDiscountDetails = await _repository.GetQueryable()
            //     .AsNoTracking()
            //     .Where(p => p.Id == id)
            //     .SelectMany(p => p.BillTransactionDiscountDetails)
            //     .AnyAsync(cancellationToken);
            //
            // var hasPayments = billTransactionDetails || billTransactionDiscountDetails;
            // if (hasPayments)
            // {
            //     return ValidationResult.Failure("This property cannot be deleted as it has payment transaction records.");
            // }

            // Log warning that payment validation is not yet implemented
            _logger.LogWarning(
                "Property deletion requested for PropertyId={PropertyId}, but payment validation is not yet implemented. " +
                "Set FeatureFlags:AllowPropertyDeletionWithoutPaymentValidation=true to allow deletions without this check.",
                id);

            // Prevent deletion until payment validation is implemented
            return ValidationResult.Failure(
                "Property deletion is currently disabled. Payment transaction validation must be implemented before enabling this feature. " +
                "Contact system administrator.");
        }

        // Feature flag is enabled - allow deletion without payment validation
        _logger.LogWarning(
            "Property deletion allowed for PropertyId={PropertyId} without payment validation. " +
            "FeatureFlags:AllowPropertyDeletionWithoutPaymentValidation is set to true. " +
            "This should only be enabled in development environments.",
            id);

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates partition number deletion order for a single property.
    /// Ensures properties with partition numbers are deleted from highest PropertyId to lowest.
    /// Since PropertyId is auto-incremented, higher ID = newer/higher partition (A7 > A6 > A1).
    /// Example: Property with highest ID (A7) must be deleted before lower IDs (A6, A5, etc.).
    /// </summary>
    private async Task<ValidationResult> ValidatePartitionDeletionOrderAsync(
        PropertyEntity entity,
        CancellationToken cancellationToken)
    {
        // Get all active properties with the same WardId and PropertyNo that have partition numbers
        // Order by PropertyId descending (highest ID = highest partition)
        // Exclude properties already marked for deletion to handle bulk delete scenarios
        var relatedProperties = await _repository.GetQueryable()
            .AsNoTracking()
            .Where(p => p.WardId == entity.WardId &&
                       p.PropertyNo == entity.PropertyNo &&
                       p.IsActive == true &&
                       p.MarkedForDeletion == false &&
                       !string.IsNullOrWhiteSpace(p.PartitionNo))
            .OrderByDescending(p => p.Id)
            .Select(p => new { p.Id, p.PartitionNo })
            .ToListAsync(cancellationToken);

        if (relatedProperties.Count == 0)
        {
            // No related properties found - allow deletion
            return ValidationResult.Success();
        }

        // Get the property with highest PropertyId (newest/highest partition)
        var highestProperty = relatedProperties.First();

        // Check if the property being deleted has the highest PropertyId
        if (entity.Id != highestProperty.Id)
        {
            _logger.LogWarning(
                "Attempted to delete property with partition '{PartitionNo}' (PropertyId={PropertyId}), " +
                "but the highest PropertyId is {HighestPropertyId} (partition '{HighestPartition}'). Deletion blocked.",
                entity.PartitionNo,
                entity.Id,
                highestProperty.Id,
                highestProperty.PartitionNo);

            return ValidationResult.Failure(
                $"Property with partition '{entity.PartitionNo}' cannot be deleted. " +
                $"Properties must be deleted in order starting from the highest partition. " +
                $"Please delete partition '{highestProperty.PartitionNo}' first.");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates partition deletion sequence for bulk deletion.
    /// Ensures all properties being deleted are sequential by PropertyId and start from the highest.
    /// Since PropertyId is auto-incremented, higher ID = newer/higher partition (A7 > A6 > A1).
    /// Example: Can delete [552380, 552379, 552378] but NOT [552380, 552378, 552376] (gaps not allowed).
    /// </summary>
    private async Task<ValidationResult> ValidateBulkPartitionDeletionSequenceAsync(
        List<PropertyEntity> entities,
        CancellationToken cancellationToken)
    {
        // Group properties by WardId and PropertyNo to validate each group separately
        var groupedByProperty = entities
            .Where(e => !string.IsNullOrWhiteSpace(e.PartitionNo))
            .GroupBy(e => new { e.WardId, e.PropertyNo });

        foreach (var group in groupedByProperty)
        {
            // Get all active properties for this WardId/PropertyNo combination, ordered by PropertyId descending
            // Exclude properties already marked for deletion to handle sequential bulk delete
            var allActiveProperties = await _repository.GetQueryable()
                .AsNoTracking()
                .Where(p => p.WardId == group.Key.WardId &&
                           p.PropertyNo == group.Key.PropertyNo &&
                           p.IsActive == true &&
                           p.MarkedForDeletion == false &&
                           !string.IsNullOrWhiteSpace(p.PartitionNo))
                .OrderByDescending(p => p.Id)
                .Select(p => new { p.Id, p.PartitionNo })
                .ToListAsync(cancellationToken);

            if (allActiveProperties.Count == 0)
            {
                continue;
            }

            // Get properties to be deleted, sorted by PropertyId descending
            var propertiesToDelete = group
                .OrderByDescending(g => g.Id)
                .Select(g => new { g.Id, g.PartitionNo })
                .ToList();

            // VALIDATION 1: Check if deletion starts from the highest PropertyId
            var highestActiveProperty = allActiveProperties.First();
            if (propertiesToDelete.First().Id != highestActiveProperty.Id)
            {
                _logger.LogWarning(
                    "Bulk deletion validation failed: Attempted to delete starting from PropertyId={FirstId} (partition '{FirstPartition}'), " +
                    "but highest PropertyId is {HighestId} (partition '{HighestPartition}') for Ward={WardId}, PropertyNo={PropertyNo}",
                    propertiesToDelete.First().Id,
                    propertiesToDelete.First().PartitionNo,
                    highestActiveProperty.Id,
                    highestActiveProperty.PartitionNo,
                    group.Key.WardId,
                    group.Key.PropertyNo);

                return ValidationResult.Failure(
                    $"Bulk deletion must start from the highest partition. " +
                    $"The highest partition is '{highestActiveProperty.PartitionNo}', " +
                    $"but deletion list starts with partition '{propertiesToDelete.First().PartitionNo}'. " +
                    $"Please include all partitions starting from '{highestActiveProperty.PartitionNo}' without gaps.");
            }

            // VALIDATION 2: Check for sequential PropertyIds (no gaps)
            for (int i = 0; i < propertiesToDelete.Count; i++)
            {
                // If we've exhausted active properties but still have properties to delete,
                // it means some requested properties are already marked for deletion (gap detected)
                if (i >= allActiveProperties.Count)
                {
                    _logger.LogWarning(
                        "Bulk deletion validation failed: Property {PropertyId} (partition '{PartitionNo}') is already marked for deletion " +
                        "or does not exist in active properties for Ward={WardId}, PropertyNo={PropertyNo}",
                        propertiesToDelete[i].Id,
                        propertiesToDelete[i].PartitionNo,
                        group.Key.WardId,
                        group.Key.PropertyNo);

                    return ValidationResult.Failure(
                        $"Partition '{propertiesToDelete[i].PartitionNo}' is already marked for deletion or is not an active property. " +
                        $"Please remove it from the deletion list.");
                }

                var expectedProperty = allActiveProperties[i];
                if (propertiesToDelete[i].Id != expectedProperty.Id)
                {
                    _logger.LogWarning(
                        "Bulk deletion validation failed: Gap detected in PropertyId sequence. " +
                        "Expected PropertyId={ExpectedId} (partition '{ExpectedPartition}') at position {Position}, " +
                        "but found PropertyId={ActualId} (partition '{ActualPartition}') for Ward={WardId}, PropertyNo={PropertyNo}",
                        expectedProperty.Id,
                        expectedProperty.PartitionNo,
                        i,
                        propertiesToDelete[i].Id,
                        propertiesToDelete[i].PartitionNo,
                        group.Key.WardId,
                        group.Key.PropertyNo);

                    var validSequence = string.Join(" → ", allActiveProperties.Take(propertiesToDelete.Count)
                        .Select(p => p.PartitionNo));

                    return ValidationResult.Failure(
                        $"Properties must be deleted sequentially without gaps. " +
                        $"Expected partition '{expectedProperty.PartitionNo}' at position {i + 1}, " +
                        $"but found partition '{propertiesToDelete[i].PartitionNo}' in the deletion list. " +
                        $"Valid sequence: {validSequence}");
                }
            }
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Fetches and marks property details and their related entities for deletion.
    /// 
    /// ARCHITECTURE NOTE: This method handles entities with PropertyDetailsId (child-level relationships).
    /// For entities with both PropertyId AND PropertyDetailsId, uses PropertyId-only queries (sufficient for deletion).
    /// For entities with only PropertyDetailsId, uses PropertyDetailsId list queries.
    /// 
    /// NOTE: Queries are executed sequentially to avoid DbContext concurrency issues.
    /// EF Core's DbContext is not thread-safe and cannot handle parallel queries on the same instance.
    /// 
    /// PERFORMANCE OPTIMIZATION: Consolidates multiple MarkEntitiesForDeletion calls by grouping related entities.
    /// Reduces from 7 individual calls to 4 consolidated calls.
    /// </summary>
    private async Task MarkPropertyDetailsAndRelatedAsync(int propertyId, CancellationToken cancellationToken)
    {
        // STEP 1: Fetch and mark property details (required first for subsequent queries)
        var propertyDetails = await _propertyRepository.GetPropertyDetailsByPropertyIdAsync(propertyId, cancellationToken);
        var propertyDetailIds = propertyDetails.Select(x => x.Id).ToList();

        // Call 1 of 4: Mark property details
        _propertyRepository.MarkEntitiesForDeletion(propertyDetails);

        // STEP 2: Fetch entities related by PropertyId (execute sequentially to avoid DbContext concurrency)
        // PropertyId-only queries: For entities with BOTH PropertyId AND PropertyDetailsId columns
        // PropertyId alone is sufficient because it's the primary FK relationship (guaranteed complete coverage)
        // IMPORTANT: These queries must execute even if no PropertyDetails exist, because:
        // - Entities CAN have PropertyId without PropertyDetailsId (property-level data, not floor-specific)
        // - Historical data or partial data entry scenarios
        // - Ensures complete cleanup even in edge cases
        var rvResults = await _propertyRepository.GetRvResultsByPropertyIdAsync(propertyId, cancellationToken);
        var section129Results = await _propertyRepository.GetSection129ResultsByPropertyIdAsync(propertyId, cancellationToken);
        var roomWiseSubmissions = await _propertyRepository.GetRoomWiseSubmissionByPropertyIdAsync(propertyId, cancellationToken);

        // Call 2 of 4: Mark all PropertyId-related entities in a single call
        var allPropertyIdEntities = rvResults.Cast<IHardDeletable>()
            .Concat(section129Results)
            .Concat(roomWiseSubmissions);
        _propertyRepository.MarkEntitiesForDeletion(allPropertyIdEntities);

        // STEP 3: Fetch entities related by PropertyDetailsId (conditionally, only if PropertyDetails exist)
        // PropertyDetailsId-based entities (occupancy, renters, renter details) cannot exist without PropertyDetails
        // However, we must NOT return early here because RoomWiseMinusData (Step 4) needs to be
        // processed even when PropertyDetails don't exist (since RoomWiseSubmissions can exist with
        // PropertyId only and still have child minus-data records).
        if (propertyDetailIds.Count > 0)
        {
            // PropertyDetailsId list queries: For entities with ONLY PropertyDetailsId column (no PropertyId)
            var propertyOccupancy = await _propertyRepository.GetPropertyOccupancyByPropertyDetailIdsAsync(propertyDetailIds, cancellationToken);
            var renters = await _propertyRepository.GetRentersByPropertyDetailIdsAsync(propertyDetailIds, cancellationToken);
            var renterDetails = await _propertyRepository.GetRenterDetailsByPropertyDetailIdsAsync(propertyDetailIds, cancellationToken);

            // Call 3 of 4: Mark all PropertyDetailsId-related entities in a single call
            var allPropertyDetailsIdEntities = propertyOccupancy.Cast<IHardDeletable>()
                .Concat(renters)
                .Concat(renterDetails);
            _propertyRepository.MarkEntitiesForDeletion(allPropertyDetailsIdEntities);
        }

        // TODO: Uncomment when database table structure is finalized for PropertyTaxCalculationCVResultsEntity
        // var cvResults = await _propertyRepository.GetCvResultsByPropertyIdAsync(propertyId, cancellationToken);
        // Add cvResults to allPropertyDetailsIdEntities collection above

        // STEP 4: Fetch and mark RoomWiseMinusData (child of RoomWiseSubmissionDetails)
        // This entity only has RoomWiseSubmissionId FK (no PropertyId), so we query after fetching RoomWiseSubmissionDetails
        // Must be separate because it depends on roomWiseSubmissions being fetched first
        if (roomWiseSubmissions.Count > 0)
        {
            var roomWiseSubmissionIds = roomWiseSubmissions.Select(x => x.Id).ToList();
            var roomWiseMinusData = await _propertyRepository.GetRoomWiseMinusBySubmissionIdsAsync(roomWiseSubmissionIds, cancellationToken);

            // Call 4 of 4: Mark RoomWiseMinusData
            _propertyRepository.MarkEntitiesForDeletion(roomWiseMinusData);
        }

        // STEP 5: Deactivate entities that extend BaseEntity but don't implement IHardDeletable
        // These entities (PropertySocialDetails, WaterConnectionMaster) only get IsActive=false
        // and UpdatedDate set, without MarkedForDeletion flags
        var socialDetails = await _propertyRepository.GetPropertySocialDetailsByPropertyIdAsync(propertyId, cancellationToken);
        var waterConnections = await _propertyRepository.GetWaterConnectionsByPropertyIdAsync(propertyId, cancellationToken);

        var baseEntityOnly = socialDetails.Cast<BaseEntity>()
            .Concat(waterConnections);

        // Call 5: Deactivate BaseEntity-only entities (no MarkedForDeletion)
        _propertyRepository.DeactivatePropertyEntities(baseEntityOnly);
    }

    /// <summary>
    /// Fetches and marks all related entities for a property for deletion.
    /// </summary>
    private async Task MarkRelatedEntitiesForDeletionAsync(int propertyId, CancellationToken cancellationToken)
    {
        var relatedEntities = await _propertyRepository.GetRelatedEntitiesForDeletionAsync(propertyId, cancellationToken);
        _propertyRepository.MarkEntitiesForDeletion(relatedEntities);
    }

    /// <summary>
    /// Internal method containing the shared deletion logic for both single and bulk delete operations.
    /// Performs soft-delete by marking entities with MarkedForDeletion flag and timestamp.
    /// </summary>
    /// <param name="propertyId">The ID of the property to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="skipPartitionValidation">Skip partition validation (used in bulk delete where validation already happened)</param>
    /// <returns>A tuple containing success status and optional error message</returns>
    private async Task<(bool Success, string? ErrorMessage)> DeletePropertyInternalAsync(
        int propertyId,
        CancellationToken cancellationToken,
        bool skipPartitionValidation = false)
    {
        try
        {
            // Fetch parent property
            var entity = await _repository.GetByIdAsync(propertyId, cancellationToken);

            if (entity == null)
            {
                return (false, $"Property with ID {propertyId} does not exist.");
            }

            // Validation (skip partition validation in bulk delete to avoid transaction isolation issues)
            var validationResult = await ValidateForDeleteInternalAsync(propertyId, entity, cancellationToken, skipPartitionValidation);
            if (!validationResult.IsValid)
            {
                return (false, validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed.");
            }

            // Mark property details and their related entities
            await MarkPropertyDetailsAndRelatedAsync(propertyId, cancellationToken);

            // Mark all other related entities
            await MarkRelatedEntitiesForDeletionAsync(propertyId, cancellationToken);

            // Soft-delete related rule application logs
            if (_ruleLogService != null)
            {
                await _ruleLogService.DeleteByPropertyIdAsync(propertyId, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Delete parent entity using repository method (applies soft deletion logic)
            await _repository.DeleteAsync(entity, cancellationToken);

            // Persist parent entity deletion
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete property with ID {PropertyId}", propertyId);
            return (false, ex.Message);
        }
    }

    public override async Task<bool> DeleteAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var (success, errorMessage) = await DeletePropertyInternalAsync(propertyId, cancellationToken);

            if (!success)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogWarning("Property deletion failed for ID {PropertyId}: {Error}", propertyId, errorMessage);

                // Throw ValidationException so the global middleware returns proper error message
                throw new ValidationException(
                    errorMessage ?? "Property deletion failed.",
                    OperationType.Delete);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return true;
        }
        catch (ValidationException)
        {
            // Re-throw ValidationException to be handled by global middleware
            throw;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Transaction failed for property deletion with ID {PropertyId}", propertyId);
            throw;
        }
    }
    public override async Task<BulkResult<int>> BulkDeleteAsync(
    int[] ids,
    CancellationToken cancellationToken = default)
    {
        if (ids.Length == 0)
            return new BulkResult<int>(0, 0, []);

        // Fetch all entities first to perform bulk partition validation
        var entities = await _repository.GetQueryable()
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(cancellationToken);

        if (entities.Count != ids.Length)
        {
            var missingIds = ids.Except(entities.Select(e => e.Id)).ToList();
            var errorMsg = $"Properties not found: {string.Join(", ", missingIds)}";
            _logger.LogWarning("Bulk delete failed: {Error}", errorMsg);
            return new BulkResult<int>(0, ids.Length, [], [errorMsg]);
        }

        // Validate partition deletion sequence for all properties with partitions
        var bulkPartitionValidation = await ValidateBulkPartitionDeletionSequenceAsync(entities, cancellationToken);
        if (!bulkPartitionValidation.IsValid)
        {
            var errorMsg = bulkPartitionValidation.Errors.FirstOrDefault()?.ErrorMessage ?? "Bulk partition validation failed.";
            _logger.LogWarning("Bulk delete partition validation failed: {Error}", errorMsg);
            return new BulkResult<int>(0, ids.Length, [], [errorMsg]);
        }

        var deletedIds = new List<int>();
        var errors = new List<string>();

        foreach (var entity in entities)
        {
            // Each property gets its own transaction to prevent partial deletes
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                // Skip partition validation since we already validated the entire sequence upfront
                var (success, errorMessage) = await DeletePropertyInternalAsync(entity.Id, cancellationToken, skipPartitionValidation: true);

                if (success)
                {
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    deletedIds.Add(entity.Id);
                }
                else
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    errors.Add($"Property {entity.Id}: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                errors.Add($"Property {entity.Id}: {ex.Message}");
                _logger.LogError(ex, "Bulk delete failed for property {PropertyId}", entity.Id);
            }
        }

        return new BulkResult<int>(
            deletedIds.Count,
            errors.Count,
            deletedIds,
            errors.Count > 0 ? errors : null);
    }

    public async Task<BulkResult<CreateBulkPropertyResponseDto>?> BulkCreateAsync(CreateBulkPropertyDto[] items, CancellationToken ct)
    {
        if (items.Length == 0)
        {
            return new BulkResult<CreateBulkPropertyResponseDto>(0, 0, []);
        }

        var results = new List<CreateBulkPropertyResponseDto>();
        var errors = new List<string>();



        var buildingResult = await _propertyRepository.CheckBuildingIfExists(items[0], ct);

        if (buildingResult == null)
        {
            return new BulkResult<CreateBulkPropertyResponseDto>(
            0,
            items.Length,
            [],
            ["Building Not Found"]
        );
        }


        var category = await _propertyRepository.GetBuildingCategory(items[0].CategoryId, ct);
        if (category == null)
        {
            return new BulkResult<CreateBulkPropertyResponseDto>(
                0,
                items.Length,
                [],
                ["Invalid CategoryId - category not found."]
            );
        }

        if (category.PropertyCategoryName != null &&
            category.PropertyCategoryName.Contains("apartment", StringComparison.OrdinalIgnoreCase) && (items[0].SocietyDetailId == null || items[0].SocietyDetailId == 0))
        {
            return new BulkResult<CreateBulkPropertyResponseDto>(
               0,
               items.Length,
               [],
               ["Society Wing Details is not Found"]
           );
        }
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            StringBuilder strPropertyExistsMessage = new StringBuilder();
            StringBuilder strPropertyFlatExistsMessage = new StringBuilder();
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var propertyExists = await _propertyRepository.CheckPropertyIfExists(item, ct);
                if (propertyExists)
                {
                    if (strPropertyExistsMessage.Length == 0)
                    {
                        strPropertyExistsMessage.Append($"{item.PropertyNo}-{item.PartitionNo}");
                    }
                    else
                    {
                        strPropertyExistsMessage.Append($",{item.PartitionNo}");
                    }
                }

                var propertyflatExists = await _propertyRepository.CheckPropertyFlatIfExists(item, ct);
                if (propertyflatExists)
                {
                    if (strPropertyFlatExistsMessage.Length == 0)
                    {
                        strPropertyFlatExistsMessage.Append($"{item.FlatOrShopNo}");
                    }
                    else
                    {
                        strPropertyFlatExistsMessage.Append($",{item.FlatOrShopNo}");
                    }

                }

            }
            if (strPropertyExistsMessage.Length > 0)
            {
                strPropertyExistsMessage.Append(" this property partition already exists in this wing");
            }
            if (strPropertyFlatExistsMessage.Length > 0)
            {
                strPropertyFlatExistsMessage.Append(" this property flat already exists in this wing");
            }
           

            var errorParts = new List<string>(capacity: 2);
            errorParts.Add(strPropertyExistsMessage.ToString());

            if (strPropertyFlatExistsMessage.Length > 0)
                errorParts.Add(strPropertyFlatExistsMessage.ToString());

            var strErrorMsg = string.Join(", ", errorParts);
            if (strErrorMsg.Length > 0)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                return new BulkResult<CreateBulkPropertyResponseDto>(
                 0,
                 items.Length,
                 [],
                 [strErrorMsg.ToString()]
             );
            }

            var amenityPropertyTypeResult = await _propertyRepository.GetAmenityPropertyType(ct);
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (category.PropertyCategoryName != null && category.PropertyCategoryName.Contains(PartTypeConstants.Plot, StringComparison.OrdinalIgnoreCase))
                {
                    item.OpenPlot = true;
                }

                item.Address = buildingResult?.Address;
                item.AddressEnglish = buildingResult?.AddressEnglish;
                item.Location = buildingResult?.Location;
                item.LocationEnglish = buildingResult?.LocationEnglish;
                item.PropertySeqNo = buildingResult?.PropertySeqNo;
                item.OwnerName = "धारक";
                item.OwnerNameEnglish = "The Holder";
                if (amenityPropertyTypeResult != null && item.PartitionNo.Contains(PartitionNoConstants.AmenityPartitionNo, StringComparison.OrdinalIgnoreCase))
                {
                    item.PropertyTypeId = amenityPropertyTypeResult.Id;
                    item.ConstructionTypeId = null;
                    item.ConstructionYear = null;
                    item.TypeOfUseId = null;
                    item.SubTypeOfUseId = null;
                }

                var res = await _propertyRepository.CreateBulkPropertyAsync(item, ct);
                if (res == null || !res.Success )
                {
                    await _unitOfWork.RollbackTransactionAsync(ct);
                    return new BulkResult<CreateBulkPropertyResponseDto>(
                        0,
                        items.Length,
                        [],
                        [$"{i}: {res?.Message ?? "Unknown error"}"]
                    );
                }

                results.Add(res);
            }


            await _unitOfWork.CommitTransactionAsync(ct);

            return new BulkResult<CreateBulkPropertyResponseDto>(
                results.Count,
                0,
                results,
                null
            );
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            return new BulkResult<CreateBulkPropertyResponseDto>(
                0,
                items.Length,
                [],
                [$"Transaction failed: {ex.Message}"]
            );
        }
    }


   
}
