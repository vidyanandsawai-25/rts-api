using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.SocialAttributeMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Api.Controllers.Master
{
    [Route("api/[controller]")]
    [ApiController]
    public class SocialAttributeController : ControllerBase
    {
        private readonly ISocialAttributeService _service;
        private readonly IHardDeleteCleanupService _cleanupService;
        private readonly IReferenceValidationService _referenceValidationService;
        private readonly ILogger<SocialAttributeController> _logger;

        public SocialAttributeController(
            ILogger<SocialAttributeController> logger,
            ISocialAttributeService service,
            IHardDeleteCleanupService cleanupService,
            IReferenceValidationService referenceValidationService)
        {
            _service = service;
            _cleanupService = cleanupService;
            _referenceValidationService = referenceValidationService;
            _logger = logger;
        }

        [HttpGet]
        public Task<IActionResult> GetAll([FromQuery] SocialAttributeMasterQueryParameters queryParameters, CancellationToken ct)
         => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

        [HttpGet("{id}")]
        public Task<IActionResult> GetById(int id, CancellationToken ct)
            => this.ExecuteGetById(_service, id, _logger, ct);

        [HttpPost]
        public Task<IActionResult> Create([FromBody] CreateSocialAttributeDto createDto, CancellationToken ct)
            => this.ExecuteCreate(_service, createDto, _logger, ct);

        [HttpPut("{id}")]
        public Task<IActionResult> Update(int id, [FromBody] UpdateSocialAttributeDto updateDto, CancellationToken ct)
            => this.ExecuteUpdate(_service, id, updateDto, _logger, ct);

        [HttpDelete("{id}")]
        public Task<IActionResult> Delete(int id, CancellationToken ct)
            => this.ExecuteDelete(_service, id, _logger, ct);

        [Authorize]
        [HttpDelete("{id}/purge")]
        public Task<IActionResult> Purge(int id, CancellationToken ct)
            => this.ExecuteForceDelete<SocialAttributeEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);
    }
}