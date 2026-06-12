using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.RateableValue;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/rateable-value")]
    [Authorize]
    public class RateableValueController : ControllerBase
    {
        private readonly IRateableValueService _rateableValueService;
        private readonly ILogger<RateableValueController> _logger;

        public RateableValueController(
            IRateableValueService rateableValueService,
            ILogger<RateableValueController> logger)
        {
            _rateableValueService = rateableValueService;
            _logger = logger;
        }

        [HttpPost("{propertyId:int}")]
        public async Task<ActionResult<RateableValueResponseDto>> Calculate(int propertyId)
        {
            if (propertyId <= 0)
                return BadRequest("propertyId must be a positive integer.");

            try
            {
                var result = await _rateableValueService.CalculateAndSaveAsync(propertyId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "RV calculation rejected for PropertyId={PropertyId}", propertyId);
                return NotFound(new { error = "Rateable value calculation could not be completed for the requested property." });
            }
        }
    }
}
