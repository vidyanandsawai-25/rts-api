using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.YearMaster;
using NtisPlatform.Application.Interfaces.Master;

namespace NtisPlatform.Api.Controllers.Master
{
    [ApiController]
    [Route("api/[controller]")]
     
    public class YearMasterController : ControllerBase
    {
        private readonly IYearMasterService _service;
        private readonly ILogger<YearMasterController> _logger;

        public YearMasterController(IYearMasterService service, ILogger<YearMasterController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public Task<IActionResult> GetAll([FromQuery] YearMasterQueryParameters queryParameters, CancellationToken ct)
            => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

        [HttpGet("{id}")]
        public Task<IActionResult> GetById(int id, CancellationToken ct)
            => this.ExecuteGetById(_service, id, _logger, ct);

        [HttpPost]
        public Task<IActionResult> Create([FromBody] CreateYearMasterDto createDto, CancellationToken ct)
            => this.ExecuteCreate(_service, createDto, _logger, ct);

        [HttpPut("{id}")]
        public Task<IActionResult> Update(int id, [FromBody] UpdateYearMasterDto updateDto, CancellationToken ct)
            => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

        [HttpDelete("{id}")]
        public Task<IActionResult> Delete(int id, CancellationToken ct)
            => this.ExecuteDelete(_service, id, _logger, ct);
    }
}
