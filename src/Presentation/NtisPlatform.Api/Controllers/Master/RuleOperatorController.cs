using NtisPlatform.Application.Interfaces.Master;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using NtisPlatform.Application.DTOs.Master.RuleOperatorMaster;

namespace NtisPlatform.Api.Controllers.Master
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RuleOperatorController : ControllerBase
    {
        private readonly IRuleOperatorService _service;
        private readonly ILogger<RuleOperatorController> _logger;

        public RuleOperatorController(IRuleOperatorService service, ILogger<RuleOperatorController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public Task<IActionResult> GetAll([FromQuery] RuleOperatorQueryParameters queryParameters, CancellationToken ct)
            => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

        [HttpGet("{id}")]
        public Task<IActionResult> GetById(int id, CancellationToken ct)
            => this.ExecuteGetById(_service, id, _logger, ct);

        [HttpPost]
        public Task<IActionResult> Create([FromBody] CreateRuleOperatorDto createDto, CancellationToken ct)
            => this.ExecuteCreate(_service, createDto, _logger, ct);

        [HttpPut("{id}")]
        public Task<IActionResult> Update(int id, [FromBody] UpdateRuleOperatorDto updateDto, CancellationToken ct)
            => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

        [HttpDelete("{id}")]
        public Task<IActionResult> Delete(int id, CancellationToken ct)
            => this.ExecuteDelete(_service, id, _logger, ct);
    }
}
