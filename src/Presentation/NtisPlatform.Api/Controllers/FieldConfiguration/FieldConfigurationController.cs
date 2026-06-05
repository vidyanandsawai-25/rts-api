using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.FieldConfiguration;
using NtisPlatform.Application.Interfaces.FieldConfiguration;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers.FieldConfiguration
{
    /// <summary>
    /// Controller for managing field configurations
    /// Provides CRUD operations for configuring how fields behave in the UI
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FieldConfigurationController : ControllerBase
    {
        private readonly IFieldConfigurationService _fieldConfigurationService;
        private readonly ILogger<FieldConfigurationController> _logger;

        public FieldConfigurationController(
            IFieldConfigurationService fieldConfigurationService,
            ILogger<FieldConfigurationController> logger)
        {
            _fieldConfigurationService = fieldConfigurationService;
            _logger = logger;
        }

        /// <summary>
        /// Get all field configurations with filtering and pagination
        /// </summary>
        /// <param name="queryParameters">Query parameters for filtering, sorting, and pagination</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of field configurations</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<FieldConfigurationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> GetAll([FromQuery] FieldConfigurationQueryParameters queryParameters, CancellationToken cancellationToken)
            => this.ExecuteGetAllPaged(_fieldConfigurationService, queryParameters, _logger, cancellationToken);

        /// <summary>
        /// Get a specific field configuration by ID
        /// </summary>
        /// <param name="id">The field configuration ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The field configuration</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(FieldConfigurationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
            => this.ExecuteGetById(_fieldConfigurationService, id, _logger, cancellationToken);

        /// <summary>
        /// Get field configuration by RulesFieldId
        /// </summary>
        /// <param name="rulesFieldId">The RulesField ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The field configuration for the specified RulesField</returns>
        [HttpGet("by-field/{rulesFieldId}")]
        [ProducesResponseType(typeof(FieldConfigurationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetByRulesFieldId(int rulesFieldId, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _fieldConfigurationService.GetByRulesFieldIdAsync(rulesFieldId, cancellationToken);

                if (result == null)
                {
                    return NotFound(new { message = $"Field configuration not found for RulesFieldId {rulesFieldId}" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting field configuration by RulesFieldId {RulesFieldId}", rulesFieldId);
                return StatusCode(500, new { message = "An error occurred while retrieving the field configuration" });
            }
        }

        /// <summary>
        /// Create a new field configuration
        /// </summary>
        /// <param name="createDto">The field configuration data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created field configuration</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<FieldConfigurationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> Create([FromBody] CreateFieldConfigurationDto createDto, CancellationToken cancellationToken)
            => this.ExecuteCreate(_fieldConfigurationService, createDto, _logger, cancellationToken);

        /// <summary>
        /// Update an existing field configuration
        /// </summary>
        /// <param name="id">The field configuration ID</param>
        /// <param name="updateDto">The updated field configuration data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The updated field configuration</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<FieldConfigurationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> Update(int id, [FromBody] UpdateFieldConfigurationDto updateDto, CancellationToken cancellationToken)
            => this.ExecuteUpdate(_fieldConfigurationService, id, updateDto, _logger, cancellationToken);

        /// <summary>
        /// Delete a field configuration
        /// </summary>
        /// <param name="id">The field configuration ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
            => this.ExecuteDelete(_fieldConfigurationService, id, _logger, cancellationToken);
    }
}
