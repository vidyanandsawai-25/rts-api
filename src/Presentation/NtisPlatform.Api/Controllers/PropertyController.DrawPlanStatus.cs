using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
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
        CancellationToken ct)
    {
        var property = await propertyRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion)
            .Select(p => new { p.CategoryId, p.PropertyTypeId, p.Type })
            .FirstOrDefaultAsync(ct);

        if (property == null)
        {
            _logger.LogWarning("Property with ID {PropertyId} not found", propertyId);
            return NotFound(new ApiResponse<PropertyDrawPlanStatusDto>
            {
                Success = false,
                Message = $"Property with ID {propertyId} not found"
            });
        }

        var dto = new PropertyDrawPlanStatusDto
        {
            PropertyId = propertyId,
            CategoryId = property.CategoryId,
            PropertyTypeId = property.PropertyTypeId,
            Type = property.Type
        };

        return Ok(new ApiResponse<PropertyDrawPlanStatusDto>
        {
            Success = true,
            Message = "Draw plan status fetched successfully",
            Items = dto
        });
    }
}
