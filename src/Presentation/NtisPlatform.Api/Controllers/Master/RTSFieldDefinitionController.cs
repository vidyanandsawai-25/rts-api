using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Master.RTSFieldDefinition;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;

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
        [HttpGet("service-cards")]
        public async Task<IActionResult> GetServiceCards(
            [FromQuery] RTSFieldDefinitionServiceCardQueryParameters query,
            CancellationToken ct)
        {
            try
            {
                return Ok(await _service.GetServiceCardsAsync(query, ct));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load RTS field-definition service cards");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Unable to load RTS field configuration cards." });
            }
        }

        [HttpPut("service/{serviceId:int}/form")]
        public async Task<IActionResult> UpdateServiceForm(
            int serviceId,
            [FromBody] UpdateRTSFieldDefinitionFormDto request,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var fields = await _service.UpdateServiceFormAsync(serviceId, request, ct);
                return Ok(new ApiResponse<IReadOnlyList<RTSFieldDefinitionDto>>
                {
                    Success = true,
                    Message = "RTS field form updated successfully.",
                    Items = fields
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new ApiResponse<IReadOnlyList<RTSFieldDefinitionDto>>
                {
                    Success = false,
                    Message = ex.Message,
                    Errors = ex.Errors.Values.ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update RTS field form for service {ServiceId}", serviceId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ApiResponse<IReadOnlyList<RTSFieldDefinitionDto>>
                    {
                        Success = false,
                        Message = "Unable to update RTS field form."
                    });
            }
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public Task<IActionResult> GetById(int id, CancellationToken ct)
            => this.ExecuteGetById(_service, id, _logger, ct);
    }
