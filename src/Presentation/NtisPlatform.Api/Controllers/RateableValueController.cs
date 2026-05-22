using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.RateableValue;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/rateable-value")]
    public class RateableValueController : ControllerBase
    {
        private readonly IRateableValueService _rateableValueService;

        public RateableValueController(IRateableValueService rateableValueService)
        {
            _rateableValueService = rateableValueService;
        }

        [HttpPost("{propertyId:int}")]
        public async Task<ActionResult<RateableValueResponseDto>> Calculate(int propertyId)
        {
            var result = await _rateableValueService.CalculateAndSaveAsync(propertyId);
            return Ok(result);
        }
    }
}
