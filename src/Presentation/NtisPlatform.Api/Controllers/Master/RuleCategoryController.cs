using NtisPlatform.Application.Interfaces.Master;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using NtisPlatform.Application.DTOs.Master.RuleCategory;

namespace NtisPlatform.Api.Controllers.Master
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RuleCategoryController : ControllerBase
    {
        private readonly IRuleCategoryService _service;
        private readonly ILogger<RuleCategoryController> _logger;

        public RuleCategoryController(IRuleCategoryService service, ILogger<RuleCategoryController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public Task<IActionResult> GetAll([FromQuery] RuleCategoryQueryParameters queryParameters, CancellationToken ct)
            => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

        [HttpGet("{id}")]
        public Task<IActionResult> GetById(int id, CancellationToken ct)
            => this.ExecuteGetById(_service, id, _logger, ct);

        [HttpPost]
        public Task<IActionResult> Create([FromBody] CreateRuleCategoryDto createDto, CancellationToken ct)
            => this.ExecuteCreate(_service, createDto, _logger, ct);

        [HttpPut("{id}")]
        public Task<IActionResult> Update(int id, [FromBody] UpdateRuleCategoryDto updateDto, CancellationToken ct)
            => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

        [HttpDelete("{id}")]
        public Task<IActionResult> Delete(int id, CancellationToken ct)
            => this.ExecuteDelete(_service, id, _logger, ct);
    }
}
