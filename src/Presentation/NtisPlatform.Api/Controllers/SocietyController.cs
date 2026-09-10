using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NtisPlatform.Api.Controllers;

[ApiController]
[Route("api/society")]
public class SocietyController : ControllerBase
{
    private readonly IRepository<WingDetailsMastEntity, int> _wingDetailsMastRepository;
    private readonly IRepository<PropertyEntity, int> _propertyRepository;

    public SocietyController(
        IRepository<WingDetailsMastEntity, int> wingDetailsMastRepository,
        IRepository<PropertyEntity, int> propertyRepository)
    {
        _wingDetailsMastRepository = wingDetailsMastRepository;
        _propertyRepository = propertyRepository;
    }

    [HttpGet("{societyDetailId}/max-type")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMaxTypeForSociety(int societyDetailId, CancellationToken ct)
    {
        if (societyDetailId <= 0)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = "Invalid SocietyDetailId" });
        }

        // Get all wings in the society
        var wingsInSociety = await _wingDetailsMastRepository
            .GetQueryable()
            .Where(w => w.SocietyDetailsMastId == societyDetailId && w.IsActive && !w.MarkedForDeletion)
            .Select(w => w.Id)
            .ToListAsync(ct);

        int maxType = 0;

        if (wingsInSociety.Any())
        {
            var properties = await _propertyRepository
                .GetQueryable()
                .Where(p => p.WingDetailId.HasValue && wingsInSociety.Contains(p.WingDetailId.Value) && p.IsActive && !p.MarkedForDeletion && !string.IsNullOrEmpty(p.Type))
                .Select(p => p.Type)
                .ToListAsync(ct);

            foreach (var typeStr in properties)
            {
                if (int.TryParse(typeStr, out int t) && t > maxType)
                {
                    maxType = t;
                }
            }
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Max type fetched successfully.",
            Items = new { maxType = maxType + 1 }
        });
    }
}
