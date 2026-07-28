using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.RTSApplication;
using NtisPlatform.Application.DTOs.RTSFieldValue;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers;

    [AllowAnonymous]
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

        [HttpPost]
        public Task<IActionResult> Create([FromBody] CreateRTSApplicationDetailsDto dto, CancellationToken ct)
            => this.ExecuteCreate(_service, dto, _logger, ct);

        [HttpGet("{id}")]
        public Task<IActionResult> GetById(int id, CancellationToken ct)
           => this.ExecuteGetById(_service, id, _logger, ct);

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll([FromQuery] RTSApplicationQueryParameters query, CancellationToken ct)
        {
            try
            {
                var result = await _service.GetAllDashboardApplicationAsync(query, ct);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching RTS application dashboard data");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

}
