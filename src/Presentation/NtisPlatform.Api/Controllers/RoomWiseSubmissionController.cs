using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.PropertyDetails;
using NtisPlatform.Application.DTOs.RenterDetails;
using NtisPlatform.Application.DTOs.RoomWiseSubmissionDetails;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RoomWiseSubmissionController : ControllerBase
    {
         private readonly IRoomWiseSubmissionDetailsService _service;
         private readonly ILogger<RoomWiseSubmissionController> _logger;

        public RoomWiseSubmissionController(IRoomWiseSubmissionDetailsService service, ILogger<RoomWiseSubmissionController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public Task<IActionResult> GetAll([FromQuery] PropertyDetailsQueryParameters queryParameters, CancellationToken ct)
            => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

        [HttpPost]
        public Task<IActionResult> Create([FromBody] CreateRoomWiseSubmissionDetailsDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_service, createDto, _logger, ct);


        [HttpPut("{id}")]
        public Task<IActionResult> Update(int id, [FromBody] UpdateRoomWiseSubmissionDetailsDto updateDto, CancellationToken ct)
            => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);


        [HttpDelete("{id}")]
        public Task<IActionResult> Delete(int id, CancellationToken ct)
            => this.ExecuteDelete(_service, id, _logger, ct);


    }
}
