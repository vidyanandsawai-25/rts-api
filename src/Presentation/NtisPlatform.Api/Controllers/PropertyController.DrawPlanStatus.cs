using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Api.Controllers;

public partial class PropertyController
{
    /// <summary>
    /// Retrieves the necessary property details to determine if a property can proceed
    /// directly to the Draw Plan application, or if it requires type assignment.
    /// </summary>
    [HttpGet("{propertyId}/draw-plan-status")]
    [ProducesResponseType(typeof(ApiResponse<PropertyDrawPlanStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDrawPlanStatus(
        int propertyId,
        [FromServices] IRepository<PropertyEntity, int> propertyRepository,
        [FromServices] IRepository<PropertyCategoryEntity, int> categoryRepository,
        [FromServices] IRepository<PropertyTypeMasterEntity, int> propertyTypeRepository,
        CancellationToken ct)
    {
        var propertyData = await (
            from p in propertyRepository.GetQueryable().AsNoTracking()
            where p.Id == propertyId && p.IsActive && !p.MarkedForDeletion
            join cat in categoryRepository.GetQueryable().AsNoTracking() on p.CategoryId equals cat.Id into catJoin
            from category in catJoin.DefaultIfEmpty()
            join ptm in propertyTypeRepository.GetQueryable().AsNoTracking() on p.PropertyTypeId equals (int?)ptm.Id into ptmJoin
            from propertyType in ptmJoin.DefaultIfEmpty()
            select new
            {
                p.Id,
                p.CategoryId,
                CategoryName = category != null ? category.PropertyCategoryName : null,
                p.PropertyTypeId,
                PropertyTypePartType = propertyType != null ? propertyType.PartType : null,
                p.Type,
                p.WingDetailId
            }
        ).FirstOrDefaultAsync(ct);

        if (propertyData == null)
        {
            _logger.LogWarning("Property with ID {PropertyId} not found", propertyId);
            return NotFound(new ApiResponse<PropertyDrawPlanStatusDto>
            {
                Success = false,
                Message = $"Property with ID {propertyId} not found"
            });
        }

        var catName = propertyData.CategoryName?.Trim() ?? string.Empty;
        var partType = propertyData.PropertyTypePartType?.Trim() ?? string.Empty;

        bool isApartmentCategory = !string.IsNullOrWhiteSpace(catName) &&
            PropertyCategoryConstants.ApartmentCategoryNames.Contains(catName, StringComparer.OrdinalIgnoreCase);

        const int amenityPropertyTypeId = 140;
        bool isIndividualOrAmenity = !isApartmentCategory ||
            catName.Contains("Individual", StringComparison.OrdinalIgnoreCase) ||
            catName.Contains("Amenity", StringComparison.OrdinalIgnoreCase) ||
            catName.Contains("Society", StringComparison.OrdinalIgnoreCase) ||
            catName.Contains("Wing", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(partType, "Amenity", StringComparison.OrdinalIgnoreCase) ||
            propertyData.PropertyTypeId == amenityPropertyTypeId ||
            propertyData.CategoryId == 3 ||
            propertyData.CategoryId == 4;

        var rawType = propertyData.Type?.Trim();
        var hasExplicitType = !string.IsNullOrWhiteSpace(rawType) && !string.Equals(rawType, "null", StringComparison.OrdinalIgnoreCase);

        // Individual, Amenity, Society main, and Wing main properties do NOT require type assignment.
        bool requiresTypeAssignment = !isIndividualOrAmenity && !hasExplicitType;
        bool hasType = !requiresTypeAssignment;

        var dto = new PropertyDrawPlanStatusDto
        {
            PropertyId = propertyId,
            CategoryId = propertyData.CategoryId,
            CategoryName = propertyData.CategoryName,
            PropertyTypeId = propertyData.PropertyTypeId,
            Type = hasExplicitType ? rawType : null,
            HasType = hasType,
            CurrentType = hasExplicitType ? rawType : null,
            IsIndividualOrAmenity = isIndividualOrAmenity,
            RequiresTypeAssignment = requiresTypeAssignment
        };

        return Ok(new ApiResponse<PropertyDrawPlanStatusDto>
        {
            Success = true,
            Message = "Draw plan status fetched successfully",
            Items = dto
        });
    }
}
