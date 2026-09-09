using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace NtisPlatform.Api.Controllers;

public partial class PropertyController
{
    [HttpPut("{propertyId}/set-type")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPropertyType(
        int propertyId,
        [FromBody] SetPropertyTypeRequest request,
        [FromServices] IRepository<PropertyEntity, int> propertyRepository,
        [FromServices] IRepository<WingDetailsMastEntity, int> wingDetailsMastRepository,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        if (request == null || request.Type < 0)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid Type provided." });
        }

        var property = await propertyRepository.GetQueryable().FirstOrDefaultAsync(p => p.Id == propertyId, ct);
        if (property == null)
        {
            return NotFound(new ApiResponse<object> { Success = false, Message = "Property not found." });
        }

        // Check if type already exists in the same society for Apartment units
        if (property.WingDetailId.HasValue)
        {
            var wing = await wingDetailsMastRepository.GetQueryable().FirstOrDefaultAsync(w => w.Id == property.WingDetailId.Value, ct);
            if (wing != null && wing.SocietyDetailsMastId > 0)
            {
                var wingsInSociety = await wingDetailsMastRepository.GetQueryable()
                    .Where(w => w.SocietyDetailsMastId == wing.SocietyDetailsMastId && w.IsActive && !w.MarkedForDeletion)
                    .Select(w => w.Id)
                    .ToListAsync(ct);

                var existingType = await propertyRepository.GetQueryable()
                    .AnyAsync(p => p.WingDetailId.HasValue && wingsInSociety.Contains(p.WingDetailId.Value) && p.Type == request.Type.ToString() && p.IsActive && !p.MarkedForDeletion && p.Id != propertyId, ct);

                if (existingType)
                {
                    return StatusCode(409, new ApiResponse<object> { Success = false, Message = "Type already exists in this society." });
                }
            }
        }

        property.Type = request.Type.ToString();

        await propertyRepository.UpdateAsync(property, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Property type updated successfully.",
            Items = new { propertyId = property.Id, type = property.Type }
        });
    }
}

public class SetPropertyTypeRequest
{
    public int Type { get; set; }
}
