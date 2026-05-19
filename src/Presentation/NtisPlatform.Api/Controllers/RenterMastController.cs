using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.RenterMast;
using NtisPlatform.Application.DTOs.RoomWiseSubmissionDetails;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RenterMastController : ControllerBase
    {   
        private readonly IRenterMastService _service;
        private readonly ILogger<RenterMastController> _logger;

        public RenterMastController(IRenterMastService service, ILogger<RenterMastController> logger)
        {
            _logger= logger;
            _service = service;
        }


        [HttpPost]
        public Task<IActionResult> Create([FromBody] CreateRenterMastDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);


        [HttpPut("{id}")]
        public Task<IActionResult> Update(int id, [FromBody] UpdateRenterMastDto updateDto, CancellationToken ct)
            => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);


        [HttpDelete("{id}")]
        public Task<IActionResult> Delete(int id, CancellationToken ct)
            => this.ExecuteDelete(_service, id, _logger, ct);


    }
}
