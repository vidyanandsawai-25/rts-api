using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.Rules;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers.Rules
{
    /// <summary>
    /// Controller for viewing property rule application logs
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PropertyRuleApplicationLogController : ControllerBase
    {
        private readonly IPropertyRuleApplicationLogService _service;
        private readonly ILogger<PropertyRuleApplicationLogController> _logger;

        public PropertyRuleApplicationLogController(
            IPropertyRuleApplicationLogService service,
            ILogger<PropertyRuleApplicationLogController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Get all property rule application logs with filtering and pagination
        /// Only returns logs where IsActive = 1 and MarkedForDeletion = 0
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<PropertyRuleApplicationLogDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll(
            [FromQuery] PropertyRuleApplicationLogQueryParameters queryParameters,
            CancellationToken cancellationToken)
        {
            try
            {
                var logs = await _service.GetLogsAsync(queryParameters, cancellationToken);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving property rule application logs");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "An error occurred while retrieving property rule application logs"
                });
            }
        }

        /// <summary>
        /// Get a specific property rule application log by ID
        /// Only returns log if IsActive = 1 and MarkedForDeletion = 0
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(PropertyRuleApplicationLogDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            try
            {
                var log = await _service.GetByIdAsync(id, cancellationToken);
                if (log == null)
                {
                    return NotFound(new { message = $"Property rule application log with ID {id} not found." });
                }
                return Ok(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving property rule application log by ID {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "An error occurred while retrieving the property rule application log"
                });
            }
        }
    }
}
