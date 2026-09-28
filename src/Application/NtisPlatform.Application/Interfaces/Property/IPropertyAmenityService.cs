using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.DTOs.PropertyAmenity;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Interfaces.Property;

/// <summary>
/// Domain service interface for Amenity property operations.
/// </summary>
public interface IPropertyAmenityService
{
    /// <summary>
    /// Retrieves all amenities based on ward, property number prefix and generic filters.
    /// </summary>
    Task<PagedResult<AmenityPropertyDto>> GetAllAmenitiesAsync(AmenityQueryParameters request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes an amenity property and its associated property details.
    /// </summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates amenity-related data across Property, PropertyDetails, Assessment, and Workflow tables within a single transaction.
    /// </summary>
    Task<ApiResponse<AmenityPropertyDto>> UpdateAmenityAsync(int propertyId, UpdateAmenityDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the maximum amenity partition number and calculates the next available one.
    /// </summary>
    Task<ApiResponse<GetMaxPropertyAmenityResponseDto>> GetMaxPropertyAmenityAsync(MaxPropertyAmenityQueryParameters request, CancellationToken cancellationToken = default);
}
