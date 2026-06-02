using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService;

namespace NtisPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CapitalValueController : ControllerBase
    {
        private readonly ICapitalValueService _service;


        public CapitalValueController(ICapitalValueService service)
        {
            _service = service;
        }

        [HttpGet("{propertyId}")]
        public async Task<IActionResult> Get(int propertyId, CancellationToken ct)
        {
            var result = await _service.GetAsync(propertyId, ct);
            return Ok(result);
        }
         
    }
}
