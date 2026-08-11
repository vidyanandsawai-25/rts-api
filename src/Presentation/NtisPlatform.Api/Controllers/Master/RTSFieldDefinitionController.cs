using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.RTSFieldDefinition;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;

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

        [AllowAnonymous]
        [HttpGet]
        public Task<IActionResult> GetAll([FromQuery] RTSFieldDefinitionQueryParameters queryParameters, CancellationToken ct)
            => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

        [AllowAnonymous]
        [HttpGet("{id}")]
        public Task<IActionResult> GetById(int id, CancellationToken ct)
            => this.ExecuteGetById(_service, id, _logger, ct);
    }
