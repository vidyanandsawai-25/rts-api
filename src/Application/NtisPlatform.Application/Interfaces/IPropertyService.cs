using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.DTOs.PropertyDetails;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Interfaces;

public interface IPropertyService
    : ICommonCrudService<PropertyEntity, PropertyDto, CreatePropertyDto, UpdatePropertyDto, PropertyQueryParameters, int>
{
    Task<PropertyBasicDetailsDto?> GetBasicDetailsAsync(int propertyId, CancellationToken cancellationToken = default);
    Task<PropertyOldDetailsDto?> GetOldDetailsAsync(int propertyId, CancellationToken cancellationToken = default);
    Task<PropertyOldDetailsDto?> UpdateOldDetailsAsync(int propertyId, UpdatePropertyOldDetailsDto dto, CancellationToken cancellationToken = default);
    Task<PropertyBasicDetailsDto?> UpdateBasicDetailsAsync(int propertyId, UpdatePropertyBasicDetailsDto dto, CancellationToken cancellationToken = default);
    Task<PropertySocietyDetailsDto?> GetSocietyDetailsAsync(int propertyId, CancellationToken cancellationToken = default);
    Task<PropertySocietyDetailsDto?> UpdateSocietyDetailsAsync(int propertyId, UpdatePropertySocietyDetailsDto dto, CancellationToken cancellationToken = default);
    Task<PropertyKycDetailsDto?> GetKycDetailsAsync(int propertyId, CancellationToken cancellationToken = default);
    Task<PropertyKycDetailsDto?> UpdateKycDetailsAsync(int propertyId, UpdatePropertyKycDetailsDto dto, CancellationToken cancellationToken = default);
    Task<PropertyTaxDetailsDto?> GetTaxDetailsAsync(int propertyId, CancellationToken cancellationToken = default);
    Task<PropertyTaxDetailsCVDto?> GetTaxDetailsCVAsync(int propertyId, CancellationToken cancellationToken = default);
    Task<PropertyOldTaxesDetailsDto?> GetOldTaxesDetailsAsync(int propertyId, CancellationToken cancellationToken = default);
    Task<PropertyOldTaxesDetailsDto?> UpdateOldTaxesDetailsAsync(int propertyId, UpdatePropertyOldTaxesDetailsDto dto, CancellationToken cancellationToken = default);
    Task<PropertyDetailsOldListDto?> GetFloorDetailsOldAsync(int propertyId, CancellationToken cancellationToken = default);
    Task<PagedResult<PropertyDetailsOldDto>?> GetFloorDetailsOldPagedAsync(int propertyId, FloorDetailsOldQueryParameters queryParameters, CancellationToken cancellationToken = default);
    Task<PropertyDetailsOldDto?> GetFloorDetailsOldByIdAsync(int propertyId, int floorId, CancellationToken cancellationToken = default);
    Task<PropertyDetailsOldDto?> AddFloorDetailsOldAsync(int propertyId, AddPropertyDetailsOldDto dto, CancellationToken cancellationToken = default);
    Task<PropertyDetailsOldDto?> UpdateFloorDetailsOldAsync(int propertyId, int floorId, UpdatePropertyDetailsOldDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteFloorDetailsOldAsync(int propertyId, int floorId, CancellationToken cancellationToken = default);
    Task<List<BuildingGenerateStructureDto>?> GetGenerateBuildingStructureAsync(BuildingGenerateDetailsDto dto, CancellationToken cancellationToken = default);
    Task<List<SocietyAminityDetailsDto>?> GetSocietyAmenityDetailsAsync(int SocietyDetailId, bool isAmenity, CancellationToken cancellationToken = default);
    Task<List<PropertySocietyDetailsDto>?> GetSocietyWingListAsync(int propertyId, CancellationToken cancellationToken = default);
    Task<List<BuildingListDto>?> GetBuildingListAsync(int WardId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the aggregated property tax details for the specified apartment tax request.
    /// </summary>
    /// <param name="dto">The apartment tax request parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation, containing the aggregated property tax details if found.</returns>
    Task<PropertyTaxApartmentDetailsDto?> GetAggregatedPropertyTaxDetailsAsync(PropertyApartmentTaxRequestDto dto, CancellationToken cancellationToken = default);
    /// <summary>
    /// Gets the aggregated property tax CV details for the specified apartment tax request.
    /// </summary>
    /// <param name="dto">The apartment tax request parameters.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>A task representing the asynchronous operation, containing the aggregated property tax CV details if found.</returns>
	Task<PropertyTaxApartmentDetailsCVDto?> GetAggregatedPropertyTaxDetailsCVAsync(PropertyApartmentTaxRequestDto dto, CancellationToken cancellationToken = default);

	/// <summary>
	/// Creates multiple properties based on a range request.
	/// </summary>
	/// <param name="request">The range creation parameters.</param>
	/// <param name="ct">The cancellation token.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	Task<RangeResult<CreateNewPropertyResponseDto>> CreatePropertiesFromRangeAsync(RangeCreateRequest<CreateNewPropertyDto> request, CancellationToken ct);

    /// <summary>
    /// Searches properties based on Quick Search or KYC Search criteria.
    /// </summary>
    /// <param name="queryParameters">Search query parameters with pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged list of properties matching search criteria</returns>
    Task<PagedResult<PropertySearchResponseDto>> SearchPropertiesAsync(PropertySearchQueryParameters queryParameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets property dashboard statistics for the property search screen.
    /// Shows counts like Registered, Geo-Sequencing, Assessed, Unassessed, Survey properties.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dashboard statistics</returns>
    Task<PropertyDashboardStatsDto> GetPropertyDashboardStatsAsync(CancellationToken cancellationToken = default);

    Task<BulkResult<CreateBulkPropertyResponseDto>?> BulkCreateAsync(CreateBulkPropertyDto[] items, CancellationToken ct);
}
