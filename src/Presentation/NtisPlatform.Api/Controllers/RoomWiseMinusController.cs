using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.RenterDetails;
using NtisPlatform.Application.DTOs.RoomWiseMinusData;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RoomWiseMinusController : ControllerBase
    {
        private readonly  IRoomWiseMinusService _service;
        private readonly ILogger<RoomWiseMinusController> _logger;
        public RoomWiseMinusController(IRoomWiseMinusService service, ILogger<RoomWiseMinusController> logger)
        {
            _service = service;
            _logger= logger;
        }


        [HttpPost]
        public Task<IActionResult> Create([FromBody] CreateRoomWiseMinusDataDto createDto, CancellationToken ct)
      => this.ExecuteCreate(_service, createDto, _logger, ct);


        [HttpPut("{id}")]
        public Task<IActionResult> Update(int id, [FromBody] UpdateRoomWiseMinusDataDto updateDto, CancellationToken ct)
            => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);


        [HttpDelete("{id}")]
        public Task<IActionResult> Delete(int id, CancellationToken ct)
            => this.ExecuteDelete(_service, id, _logger, ct);



    }
}
