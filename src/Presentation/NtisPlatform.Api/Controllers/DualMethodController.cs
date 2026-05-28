using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DualMethodController : ControllerBase
    {
        private readonly IDualMethodService _service;

        public DualMethodController(IDualMethodService service)
        {
            _service = service;
        }

        [HttpGet("{propertyId}")]
        public async Task<IActionResult> GetRvCvTaxes(int propertyId, CancellationToken cancellationToken)
        {
            var result = await _service.GetRVCVTaxesAsync(propertyId, cancellationToken);
            return Ok(result);
        }

    }
}
