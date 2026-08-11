using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Core.Interfaces;

/// <summary>
/// Specialized repository interface for Property-specific queries
/// Extends the generic repository with custom query methods
/// </summary>
public interface IPropertyRepository : IRepository<PropertyEntity, int>
{
    // Per-tab data-entry concerns (Basic Details, KYC, Society, Discount, Old Details) have been split
    // out of this aggregate repository into feature repositories + services under the
    // NtisPlatform.*.Property namespaces. What remains here are cross-cutting/aggregate operations
    // (search, dashboard, create/bulk, tax, building structure and deletion helpers).

    /// <summary>
    /// Retrieves tax details for a property
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Property tax details DTO or null if not found</returns>
    Task<PropertyTaxDetailsDto?> GetTaxDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves tax details CV for a property
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Property tax details CV DTO or null if not found</returns>
    Task<PropertyTaxDetailsCVDto?> GetTaxDetailsCVAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves aggregated property tax details by filtering properties and summing tax amounts across multiple properties.
    /// Filters properties by WardId, PropertyNo (substring match), PartType (substring match), and PropertyId.
    /// Returns aggregated tax data grouped by TaxName from TransMast (CalculationType = "RV") and TaxMaster tables.
    /// </summary>
    /// <param name="dto">The request DTO containing property filter fields (e.g., WardId, PropertyNo, PartType, PropertyId)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Aggregated property tax details DTO or null if no matching properties found</returns>
    Task<PropertyTaxApartmentDetailsDto?> GetAggregatedPropertyTaxDetailsAsync(PropertyApartmentTaxRequestDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves aggregated property tax details (Capital Value basis) by filtering properties and summing tax amounts across multiple properties.
    /// Filters properties by WardId, PropertyNo (substring match), PartType (substring match), and PropertyId.
    /// Returns aggregated tax data grouped by TaxName from TransMastCV and TaxMaster tables.
    /// </summary>
    /// <param name="dto">The request DTO containing property filter fields (e.g., WardId, PropertyNo, PartType, PropertyId)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Aggregated property CV tax details DTO or null if no matching properties found</returns>
    Task<PropertyTaxApartmentDetailsCVDto?> GetAggregatedPropertyTaxDetailsCVAsync(PropertyApartmentTaxRequestDto dto, CancellationToken cancellationToken = default);
	
	/// <summary>
    /// Generates property structure based on floor and unit configuration (Vertical Generation).
    /// Creates a cross join of floors and units, ordered by UnitNo then FloorNo.
    /// </summary>
    /// <param name="dto">The generation parameters including floor range, units per floor, and wing info</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of generated property structures or null if validation fails</returns>
    Task<List<BuildingGenerateStructureDto>?> GetGenerateBuildingStructureAsync(BuildingGenerateDetailsDto dto, CancellationToken cancellationToken = default);

    Task<List<SocietyAminityDetailsDto>?> GetSocietyAmenityDetailsAsync(int SocietyDetailId, bool isAmenity, CancellationToken cancellationToken = default);
    Task<List<PropertySocietyDetailsDto>?> GetSocietyWingListAsync(int propertyId, CancellationToken cancellationToken = default);
    Task<List<BuildingListDto>?> GetBuildingListAsync(int WardId, CancellationToken cancellationToken = default);
    Task<bool> IsPropertyExists(int wardId, string propertyNo, int? propertyId);

    // Property search and dashboard statistics have been split out into the PropertySearch feature
    // (IPropertySearchRepository / IPropertySearchService) per the per-feature Clean Architecture split.

    /// <summary>
    /// Retrieves all RoomWiseMinusData entities by list of RoomWiseSubmissionId values.
    /// Used during property deletion to mark all minus data records for deletion.
    /// This entity only has RoomWiseSubmissionId column (no PropertyId), so we query by parent RoomWiseSubmissionDetails IDs.
    /// </summary>
    /// <param name="roomWiseSubmissionIds">List of RoomWiseSubmissionDetails IDs to fetch minus data for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of RoomWiseMinusDataEntity records</returns>
    Task<List<RoomWiseMinusDataEntity>> GetRoomWiseMinusBySubmissionIdsAsync(List<int> roomWiseSubmissionIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves property details for a property.
    /// Used during property deletion to identify related entities.
    /// </summary>
    Task<List<PropertyDetailsEntity>> GetPropertyDetailsByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves RV calculation results for a property by PropertyId.
    /// USED FOR DELETION: PropertyId alone is sufficient because it's the primary FK relationship.
    /// All RV results for a property MUST have PropertyId, so this query guarantees complete coverage.
    /// </summary>
    Task<List<RVCalculationResultsEntity>> GetRvResultsByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves Section129 calculation results for a property by PropertyId.
    /// USED FOR DELETION: PropertyId alone is sufficient because it's the primary FK relationship.
    /// All Section129 results for a property MUST have PropertyId, so this query guarantees complete coverage.
    /// </summary>
    Task<List<PropertyTaxCalculationSection129ResultsEntity>> GetSection129ResultsByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves renter master records for the specified property detail IDs.
    /// </summary>
    Task<List<RenterMastEntity>> GetRentersByPropertyDetailIdsAsync(List<int> propertyDetailIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves room-wise submission details for a property by PropertyId.
    /// USED FOR DELETION: PropertyId alone is sufficient to catch all records.
    /// Catches all records regardless of PropertyDetailsId state (NULL, valid, or orphaned).
    /// Use this method when deleting a property to ensure no orphaned records remain.
    /// </summary>
    Task<List<RoomWiseSubmissionDetailsEntity>> GetRoomWiseSubmissionByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default);

    // TODO: Uncomment when database table structure is finalized for PropertyTaxCalculationCVResultsEntity
    ///// <summary>
    ///// Retrieves CV calculation results for the specified property detail IDs.
    ///// </summary>
    //Task<List<PropertyTaxCalculationCVResultsEntity>> GetCvResultsByPropertyDetailIdsAsync(List<int> propertyDetailIds, CancellationToken cancellationToken = default);

    ///// <summary>
    ///// Retrieves renter detail records for the specified property detail IDs.
    ///// </summary>
    Task<List<RenterDetailEntity>> GetRenterDetailsByPropertyDetailIdsAsync(List<int> propertyDetailIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all related entities for a property that need to be marked for deletion.
    /// Returns entities implementing IHardDeletable.
    /// </summary>
    Task<List<IHardDeletable>> GetRelatedEntitiesForDeletionAsync(int propertyId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Marks a collection of entities for soft deletion using the same logic as Repository.DeleteAsync.
	/// Sets MarkedForDeletion to true, MarkedForDeletionDate to current time (if not already set),
	/// IsActive to false, and UpdatedDate to current time.
	/// This method ensures consistency with the deletion logic in the base Repository class.
	/// </summary>
	/// <typeparam name="T">Entity type that implements IHardDeletable</typeparam>
	/// <param name="entities">The entities to mark for deletion</param>
	void MarkEntitiesForDeletion<T>(IEnumerable<T> entities) where T : class, IHardDeletable;

	/// <summary>
	/// Deactivates a collection of BaseEntity-derived entities by setting IsActive = false and UpdatedDate = now.
	/// Does NOT touch MarkedForDeletion or MarkedForDeletionDate.
	/// Used for entities that don't implement IHardDeletable (e.g., PropertySocialDetails, WaterConnectionMaster).
	/// </summary>
	/// <param name="entities">The entities to deactivate</param>
	void DeactivatePropertyEntities(IEnumerable<BaseEntity> entities);

	/// <summary>
	/// Gets PropertySocialDetails by PropertyId.
	/// This entity extends BaseEntity but does NOT implement IHardDeletable.
	/// Used for deactivation (IsActive=false) during property deletion.
	/// </summary>
	Task<List<PropertySocialDetailsEntity>> GetPropertySocialDetailsByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets WaterConnectionMaster by PropertyId.
	/// This entity extends BaseEntity but does NOT implement IHardDeletable.
	/// Used for deactivation (IsActive=false) during property deletion.
	/// </summary>
	Task<List<WaterConnectionMasterEntity>> GetWaterConnectionsByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default);

	Task<CreateBulkPropertyResponseDto?> CreateBulkPropertyAsync(CreateBulkPropertyDto dto, CancellationToken cancellationToken = default);

    Task<PropertyEntity?> CheckBuildingIfExists(CreateBulkPropertyDto dto, CancellationToken cancellationToken = default);
    Task<PropertyCategoryEntity?> GetBuildingCategory(int CategoryId, CancellationToken cancellationToken = default);
    Task<bool> CheckPropertyIfExists(CreateBulkPropertyDto dto, CancellationToken cancellationToken = default);
    Task<bool> CheckPropertyFlatIfExists(CreateBulkPropertyDto dto, CancellationToken cancellationToken = default);
    Task<PropertyTypeMasterEntity?> GetAmenityPropertyType(CancellationToken cancellationToken = default);

}
