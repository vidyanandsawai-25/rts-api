using NtisPlatform.Application.Interfaces.Master;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using NtisPlatform.Application.DTOs.Master.RuleEffectTypeMaster;

namespace NtisPlatform.Api.Controllers.Master
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RuleEffectTypeController : ControllerBase
    {
        private readonly IRuleEffectTypeService _service;
        private readonly ILogger<RuleEffectTypeController> _logger;

        public RuleEffectTypeController(IRuleEffectTypeService service, ILogger<RuleEffectTypeController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public Task<IActionResult> GetAll([FromQuery] RuleEffectTypeQueryParameters queryParameters, CancellationToken ct)
            => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

        [HttpGet("{id}")]
        public Task<IActionResult> GetById(int id, CancellationToken ct)
            => this.ExecuteGetById(_service, id, _logger, ct);

        [HttpPost]
        public Task<IActionResult> Create([FromBody] CreateRuleEffectTypeDto createDto, CancellationToken ct)
            => this.ExecuteCreate(_service, createDto, _logger, ct);

        [HttpPut("{id}")]
        public Task<IActionResult> Update(int id, [FromBody] UpdateRuleEffectTypeDto updateDto, CancellationToken ct)
            => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

        [HttpDelete("{id}")]
        public Task<IActionResult> Delete(int id, CancellationToken ct)
            => this.ExecuteDelete(_service, id, _logger, ct);
    }
}
