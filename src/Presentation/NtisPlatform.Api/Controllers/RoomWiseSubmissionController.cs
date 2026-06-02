using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.RoomWiseSubmissionDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;

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
        [ProducesResponseType(typeof(PagedResult<RoomWiseSubmissionDetailsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> GetAll([FromQuery] RoomWiseSubmissionQueryParameters query, CancellationToken ct)
            => this.ExecuteGetAllPaged(_service, query, _logger, ct);

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
