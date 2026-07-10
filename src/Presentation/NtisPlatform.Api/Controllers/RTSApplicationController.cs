using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.RTSFieldValue;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers;

    [Route("api/[controller]")]
    [ApiController]
    public class RTSApplicationController : ControllerBase
    {
        private readonly IRTSApplicationService _service;
        private readonly IHardDeleteCleanupService _cleanupService;
        private readonly IReferenceValidationService _referenceValidationService;
        private readonly ILogger<RTSApplicationController> _logger;

        public RTSApplicationController(
            IRTSApplicationService service,
            IHardDeleteCleanupService cleanupService,
            IReferenceValidationService referenceValidationService,
            ILogger<RTSApplicationController> logger)
        {
            _service = service;
            _cleanupService = cleanupService;
            _referenceValidationService = referenceValidationService;
            _logger = logger;
        }

        [HttpGet]
        public Task<IActionResult> GetAll([FromQuery] RTSFieldValueQueryParameters queryParameters, CancellationToken ct)
                => this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

        [HttpGet("{id}")]
        public Task<IActionResult> GetById(int id, CancellationToken ct)
            => this.ExecuteGetById(_service, id, _logger, ct);

        [HttpPost]
        public Task<IActionResult> Create([FromBody] CreateRTSApplicationDetailsDto dto, CancellationToken ct)
            => this.ExecuteCreate(_service, dto, _logger, ct);

        [HttpPut("{id}")]
        public Task<IActionResult> Update(int id, [FromBody] UpdateRTSFieldValueDto dto, CancellationToken ct)
            => this.ExecuteUpdate(_service, id, dto, _logger, ct);

        [HttpDelete("{id}")]
        public Task<IActionResult> Delete(int id, CancellationToken ct)
            => this.ExecuteDelete(_service, id, _logger, ct);

        //[Authorize]
        //[HttpDelete("{id}/purge")]
        //public Task<IActionResult> Purge(int id, CancellationToken ct)
        //    => this.ExecuteForceDelete<RTSFieldValueEntity, int>(_cleanupService, _referenceValidationService, id, _logger, ct);
    }
