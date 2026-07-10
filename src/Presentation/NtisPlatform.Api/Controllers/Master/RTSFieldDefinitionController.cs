using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.RTSFieldDefinition;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Api.Controllers.Master;

    [Route("api/[controller]")]
    [ApiController]
    public class RTSFieldDefinitionController : ControllerBase
    {
        private readonly IRTSFieldDefinitionService _service;
        private readonly IHardDeleteCleanupService _cleanupService;
        private readonly IReferenceValidationService _referenceValidationService;
        private readonly ILogger<RTSFieldDefinitionController> _logger;

        public RTSFieldDefinitionController(
            IRTSFieldDefinitionService service,
            IHardDeleteCleanupService cleanupService,
            IReferenceValidationService referenceValidationService,
            ILogger<RTSFieldDefinitionController> logger)
        {
            _service = service;
            _cleanupService = cleanupService;
            _referenceValidationService = referenceValidationService;
            _logger = logger;
    }

        [HttpGet]
        public Task<IActionResult> GetAll([FromQuery] RTSFieldDefinitionQueryParameters queryParameters, CancellationToken ct)
            => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

        [HttpGet("{id}")]
        public Task<IActionResult> GetById(int id, CancellationToken ct)
            => this.ExecuteGetById(_service, id, _logger, ct);

        [HttpPost]
        public Task<IActionResult> Create([FromBody] CreateRTSFieldDefinitionDto dto, CancellationToken ct)
            => this.ExecuteCreate(_service, dto, _logger, ct);

        [HttpPut("{id}")]
        public Task<IActionResult> Update(int id, [FromBody] UpdateRTSFieldDefinitionDto dto, CancellationToken ct)
            => this.ExecuteUpdate(_service, id, dto, _logger, ct);

        [HttpDelete("{id}")]
        public Task<IActionResult> Delete(int id, CancellationToken ct)
            => this.ExecuteDelete(_service, id, _logger, ct);

        [Authorize]
        [HttpDelete("{id}/purge")]
        public Task<IActionResult> Purge(int id, CancellationToken ct)
            => this.ExecuteForceDelete<RTSFieldDefinitionEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);
    }
