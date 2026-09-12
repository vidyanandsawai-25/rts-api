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
        string? typeStr = request?.Type switch
        {
            null => null,
            string s => s,
            System.Text.Json.JsonElement je when je.ValueKind == System.Text.Json.JsonValueKind.String => je.GetString(),
            System.Text.Json.JsonElement je when je.ValueKind == System.Text.Json.JsonValueKind.Number => je.GetRawText(),
            _ => null
        };
        typeStr = typeStr?.Trim();

        if (string.IsNullOrWhiteSpace(typeStr)
            || typeStr.Equals("null", System.StringComparison.OrdinalIgnoreCase)
            || typeStr.Length > 5)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid Type provided." });
        }

        var property = await propertyRepository.GetQueryable()
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion, ct);
        if (property == null)
        {
            return NotFound(new ApiResponse<object> { Success = false, Message = "Property not found." });
        }

        property.Type = typeStr;

        await propertyRepository.UpdateAsync(property, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Property type updated successfully.",
            Items = new { propertyId = property.Id, type = property.Type }
        });
    }

    [HttpGet("building-types")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBuildingTypes(
        [FromQuery] int? wingDetailId,
        [FromQuery] int? societyDetailId,
        [FromServices] IRepository<PropertyEntity, int> propertyRepository,
        [FromServices] IRepository<WingDetailsMastEntity, int> wingDetailsMastRepository,
        CancellationToken ct)
    {
        var query = propertyRepository.GetQueryable().AsNoTracking().Where(p => p.IsActive && !p.MarkedForDeletion);

        if (wingDetailId.HasValue && wingDetailId.Value > 0)
        {
            query = query.Where(p => p.WingDetailId == wingDetailId.Value);
        }
        else if (societyDetailId.HasValue && societyDetailId.Value > 0)
        {
            var wingIds = await wingDetailsMastRepository.GetQueryable()
                .AsNoTracking()
                .Where(w => w.SocietyDetailsMastId == societyDetailId.Value && w.IsActive && !w.MarkedForDeletion)
                .Select(w => w.Id)
                .ToListAsync(ct);

            query = query.Where(p => p.WingDetailId.HasValue && wingIds.Contains(p.WingDetailId.Value));
        }
        else
        {
            return Ok(new ApiResponse<List<string>>
            {
                Success = true,
                Message = "No building filters provided",
                Items = new List<string>()
            });
        }

        var types = await query
            .Where(p => p.Type != null && p.Type.Trim() != "" && p.Type.Trim().ToLower() != "null")
            .Select(p => p.Type!.Trim())
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(ct);

        return Ok(new ApiResponse<List<string>>
        {
            Success = true,
            Message = "Building types retrieved successfully",
            Items = types
        });
    }
}

public class SetPropertyTypeRequest
{
    public object? Type { get; set; }
}
