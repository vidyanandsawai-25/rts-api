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
    /// <summary>
    /// Retrieves basic details for a property including joined data from related tables
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Property basic details DTO or null if not found</returns>
    Task<PropertyBasicDetailsDto?> GetBasicDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates old property details across PropertyMastOld and PropertyDetailsOld tables
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="dto">The update data for property basic details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated PropertyBasicDetailsDto if property was found and updated, null otherwise</returns>
    Task<PropertyBasicDetailsDto?> UpdateBasicDetailsAsync(int propertyId, UpdatePropertyBasicDetailsDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves KYC details for a property including joined data from related tables
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Property KYC details DTO or null if not found</returns>
    Task<PropertyKycDetailsDto?> GetKycDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates KYC details for a property
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="dto">The update data for property KYC details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated PropertyKycDetailsDto if property was found and updated, null otherwise</returns>
    Task<PropertyKycDetailsDto?> UpdateKycDetailsAsync(int propertyId, UpdatePropertyKycDetailsDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves society details for a property
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated PropertySocietyDetailsDto if property was found and updated, null otherwise</returns>
    Task<PropertySocietyDetailsDto?> GetSocietyDetailsAsync(int propertyId, CancellationToken cancellationToken = default);
        
    /// <summary>
    /// Updates society details for a property
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="dto">The update data for property society details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated PropertySocietyDetailsDto if property was found and updated, null otherwise</returns>
    Task<PropertySocietyDetailsDto?> UpdateSocietyDetailsAsync(int propertyId, UpdatePropertySocietyDetailsDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves old property details including joined data from PropertyMastOld and PropertyDetailsOld tables
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Property old details DTO or null if not found</returns>
    Task<PropertyOldDetailsDto?> GetOldDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    Task<PropertyOldDetailsDto?> UpdateOldDetailsAsync(int propertyId, UpdatePropertyOldDetailsDto dto, CancellationToken cancellationToken = default);

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
    /// Retrieves old taxes details for a property including historical tax data across finance years
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Property old taxes details DTO or null if property not found</returns>
    Task<PropertyOldTaxesDetailsDto?> GetOldTaxesDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates new old taxes details for a property across multiple finance years.
    /// This is a create-only operation that will fail if any records already exist for the specified years and taxes.
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="dto">The data containing tax information for multiple years to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created PropertyOldTaxesDetailsDto if property was found and records created, null otherwise</returns>
    /// <exception cref="InvalidOperationException">Thrown when records already exist for any of the specified year-tax combinations</exception>
    Task<PropertyOldTaxesDetailsDto?> CreateOldTaxesDetailsAsync(int propertyId, UpdatePropertyOldTaxesDetailsDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates old taxes details for a property across multiple finance years.
    /// This is an upsert operation that will create new records or update existing ones.
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="dto">The update data containing tax information for multiple years</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated PropertyOldTaxesDetailsDto if property was found and updated, null otherwise</returns>
    Task<PropertyOldTaxesDetailsDto?> UpdateOldTaxesDetailsAsync(int propertyId, UpdatePropertyOldTaxesDetailsDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all historical floor details for a property (PropertyDetailsOld records)
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of historical floor details or null if property not found</returns>
    Task<PropertyDetailsOldListDto?> GetFloorDetailsOldAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves paginated historical floor details for a property (PropertyDetailsOld records)
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="query">Query parameters for filtering, sorting, and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of historical floor details or null if property not found</returns>
    Task<FloorDetailsOldPagedResult?> GetFloorDetailsOldPagedAsync(int propertyId, FloorDetailsOldQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single historical floor detail record by ID
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="floorId">The floor record identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Single floor record or null if not found</returns>
    Task<PropertyDetailsOldDto?> GetFloorDetailsOldByIdAsync(int propertyId, int floorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new historical floor detail record for a property
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="dto">The floor data to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The newly created PropertyDetailsOldDto if property was found and record created, null otherwise</returns>
    Task<PropertyDetailsOldDto?> AddFloorDetailsOldAsync(int propertyId, AddPropertyDetailsOldDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing historical floor detail record for a property
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="floorId">The floor record identifier</param>
    /// <param name="dto">The update data for the floor record</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated PropertyDetailsOldDto if property and floor record were found and updated, null otherwise</returns>
    Task<PropertyDetailsOldDto?> UpdateFloorDetailsOldAsync(int propertyId, int floorId, UpdatePropertyDetailsOldDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a historical floor detail record for a property (soft delete)
    /// </summary>
    /// <param name="propertyId">The property identifier</param>
    /// <param name="floorId">The floor record identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the record was found and deleted, false otherwise</returns>
    Task<bool> DeleteFloorDetailsOldAsync(int propertyId, int floorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves aggregated property tax details by filtering properties and summing tax amounts across multiple properties.
    /// Filters properties by WardId, PropertyNo (substring match), PartType (substring match), and PropertyId.
    /// Returns aggregated tax data grouped by TaxName from TransMastRV and TaxMaster tables.
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
	 /// <summary>
    /// Creates a new property along with optional related entities such as society details,
    /// property details, and assessment records within a single transactional scope.
    ///
    /// This method performs:
    /// - Input validation and null checks
    /// - Foreign key validation (PropertyType, Category, Mouja, TaxZone)
    /// - Duplicate property number validation (PropertyNo + WardId)
    /// - Conditional creation of Society (for Apartment category)
    /// - Property creation (main entity)
    /// - Conditional PropertyDetails creation (for Plot category)
    /// - PropertyAssessment record creation
    /// - Transaction management (Commit / Rollback)
    ///
    /// Returns:
    /// - Success response with PropertyId when operation succeeds
    /// - Failure response with meaningful error message otherwise
    /// </summary>
    Task<CreateNewPropertyResponseDto?> CreateNewPropertyAsync(CreateNewPropertyDto dto, CancellationToken cancellationToken = default);
    Task<List<SocietyAminityDetailsDto>?> GetSocietyAmenityDetailsAsync(int SocietyDetailId, bool isAmenity, CancellationToken cancellationToken = default);
    Task<List<PropertySocietyDetailsDto>?> GetSocietyWingListAsync(int SocietyDetailId, CancellationToken cancellationToken = default);
    Task<List<BuildingListDto>?> GetBuildingListAsync(int WardId, CancellationToken cancellationToken = default);
    Task<bool> IsPropertyExists(int wardId, string propertyNo, int? propertyId);

    /// <summary>
    /// Searches properties based on Quick Search or KYC Search criteria with pagination
    /// </summary>
    /// <param name="searchRequest">Search parameters from either Quick Search or KYC Search tab</param>
    /// <param name="pageNumber">Page number for pagination</param>
    /// <param name="pageSize">Page size for pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total count and list of properties matching search criteria</returns>
    Task<(int TotalCount, List<PropertySearchResponseDto> Items)> SearchPropertiesAsync(PropertySearchRequestDto searchRequest, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets property dashboard statistics for the property search screen
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dashboard statistics with various property counts</returns>
    Task<PropertyDashboardStatsDto> GetPropertyDashboardStatsAsync(CancellationToken cancellationToken = default);

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
    Task<List<PropertyTaxCalculationRVResultsEntity>> GetRvResultsByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves Section129 calculation results for a property by PropertyId.
    /// USED FOR DELETION: PropertyId alone is sufficient because it's the primary FK relationship.
    /// All Section129 results for a property MUST have PropertyId, so this query guarantees complete coverage.
    /// </summary>
    Task<List<PropertyTaxCalculationSection129ResultsEntity>> GetSection129ResultsByPropertyIdAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves property occupancy details for the specified property detail IDs.
    /// </summary>
    Task<List<PropertyOccupancyDetailsEntity>> GetPropertyOccupancyByPropertyDetailIdsAsync(List<int> propertyDetailIds, CancellationToken cancellationToken = default);

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

	/// <summary>
	/// Retrieves discount information for a property including all social attributes where IsDiscountApplicable=1.
	/// Returns all discount-applicable attributes with their current values from PropertySocialDetails if they exist.
	/// </summary>
	/// <param name="propertyId">The property identifier</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Property discount details DTO or null if property not found</returns>
	Task<PropertyDiscountInfoResponseDto?> GetDiscountDetailsAsync(int propertyId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates discount information for a property by upserting PropertySocialDetails records.
	/// Handles creating, updating, and managing discount-applicable social attributes.
	/// </summary>
	/// <param name="propertyId">The property identifier</param>
	/// <param name="dto">The discount information update payload</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Updated property discount details DTO or null if property not found</returns>
	Task<PropertyDiscountInfoResponseDto?> UpdateDiscountDetailsAsync(int propertyId, UpsertPropertyDiscountInfoDto dto, CancellationToken cancellationToken = default);

    Task<PropertyEntity?> CheckBuildingIfExists(CreateBulkPropertyDto dto, CancellationToken cancellationToken = default);
    Task<PropertyCategoryEntity?> GetBuildingCategory(int CategoryId, CancellationToken cancellationToken = default);
    Task<bool> CheckPropertyIfExists(CreateBulkPropertyDto dto, CancellationToken cancellationToken = default);
    Task<bool> CheckPropertyFlatIfExists(CreateBulkPropertyDto dto, CancellationToken cancellationToken = default);
    Task<PropertyTypeMasterEntity?> GetAmenityPropertyType(CancellationToken cancellationToken = default);

}
