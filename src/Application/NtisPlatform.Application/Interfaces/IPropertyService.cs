using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.DTOs.PropertyBuildingInformation;
using NtisPlatform.Application.DTOs.PropertyDetails;
using NtisPlatform.Application.DTOs.Bulk;
using NtisPlatform.Application.DTOs.Range;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Models;
using NtisPlatform.Application.DTOs.PropertySurveySearch;
namespace NtisPlatform.Application.Interfaces;

public interface IPropertyService
    : ICommonCrudService<PropertyEntity, PropertyDto, CreatePropertyDto, UpdatePropertyDto, PropertyQueryParameters, int>
{
    // Basic Details, KYC, Society, Discount and Old Details tabs moved to per-tab services
    // (per-tab Clean Architecture split). What remains here are cross-cutting/aggregate operations.
    Task<PropertyTaxDetailsDto?> GetTaxDetailsAsync(int propertyId, CancellationToken cancellationToken = default);
    Task<PropertyTaxDetailsCVDto?> GetTaxDetailsCVAsync(int propertyId, CancellationToken cancellationToken = default);
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

    // Property search and dashboard statistics have been split out into the PropertySearch feature
    // (IPropertySearchService) per the per-feature Clean Architecture split.

    Task<BulkResult<CreateBulkPropertyResponseDto>?> BulkCreateAsync(CreateBulkPropertyDto[] items, CancellationToken ct);

    /// <summary>
    /// Updates all property details (PropertyMast, SocietyDetailsMast, PropertyMastDetails,
    /// PropertyDetails, RoomWiseSubmissionDetails) within a single transaction.
    /// Business logic lives in Application layer per CLAUDE.md guidelines.
    /// </summary>
    Task<UpdateAllPropertyDetailsResponseDto> UpdatePropertyAsync(int propertyId, UpdateAllPropertyDetailsDto dto, CancellationToken ct);


    Task<PropertySplitResultDto> SplitProperty(PropertySplitCreateDto dto, CancellationToken cancellationToken = default);

    Task<ApiResponse<PropertySurveySearchPaginatedResponseDto>>
    SearchSurveyPropertiesAsync(
        PropertySurveySearchQueryParameters request,
        CancellationToken cancellationToken = default);
		
		 Task<PagedResult<PropertyBuildingInformationDto>>
    SearchBuildingInformationAsync(
        BuildingInformationQueryParameters queryParameters,CancellationToken cancellationToken = default);
}
