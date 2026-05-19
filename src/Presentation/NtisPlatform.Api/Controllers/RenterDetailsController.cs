using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.RenterDetails;
using NtisPlatform.Application.DTOs.RoomWiseSubmissionDetails;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RenterDetailsController : ControllerBase
    {
        private readonly IRenterDetailService _service;
        private readonly ILogger<RenterDetailsController> _logger;

        public RenterDetailsController(IRenterDetailService service, ILogger<RenterDetailsController> logger)
        {
            _service = service;
            _logger = logger;
        }


        [HttpPost]
        public Task<IActionResult> Create([FromBody] CreateRenterDetailsDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);


        [HttpPut("{id}")]
        public Task<IActionResult> Update(int id, [FromBody] UpdateRenterDetailsDto updateDto, CancellationToken ct)
            => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);


        [HttpDelete("{id}")]
        public Task<IActionResult> Delete(int id, CancellationToken ct)
            => this.ExecuteDelete(_service, id, _logger, ct);


    }
}
