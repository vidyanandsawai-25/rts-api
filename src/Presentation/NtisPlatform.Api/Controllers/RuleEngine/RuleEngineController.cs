using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.RuleEngine;
using NtisPlatform.Application.Interfaces.RuleEngine;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers.RuleEngine
{
    /// <summary>
    /// Controller for managing rule engine configurations
    /// Provides CRUD operations for rule policies and configurations, plus rule execution.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RuleEngineController : ControllerBase
    {
        private readonly IRuleEngineService _ruleEngineService;
        private readonly IRuleExecutionService _ruleExecutionService;
        private readonly ILogger<RuleEngineController> _logger;

        public RuleEngineController(
            IRuleEngineService ruleEngineService,
            IRuleExecutionService ruleExecutionService,
            ILogger<RuleEngineController> logger)
        {
            _ruleEngineService = ruleEngineService;
            _ruleExecutionService = ruleExecutionService;
            _logger = logger;
        }

        /// <summary>
        /// Get all rule engine configurations with filtering and pagination
        /// </summary>
        /// <param name="queryParameters">Query parameters for filtering, sorting, and pagination</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of rule engine configurations</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<RuleEngineDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> GetAll([FromQuery] RuleEngineQueryParameters queryParameters, CancellationToken cancellationToken)
            => this.ExecuteGetAllPaged(_ruleEngineService, queryParameters, _logger, cancellationToken);

        /// <summary>
        /// Get a specific rule engine configuration by ID
        /// </summary>
        /// <param name="id">The rule engine configuration ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The rule engine configuration</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(RuleEngineDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
            => this.ExecuteGetById(_ruleEngineService, id, _logger, cancellationToken);

        /// <summary>
        /// Create a new rule engine configuration
        /// </summary>
        /// <param name="createDto">The rule engine configuration data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created rule engine configuration</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<RuleEngineDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> Create([FromBody] CreateRuleEngineDto createDto, CancellationToken cancellationToken)
            => this.ExecuteCreate(_ruleEngineService, createDto, _logger, cancellationToken);

        /// <summary>
        /// Update an existing rule engine configuration
        /// </summary>
        /// <param name="id">The rule engine configuration ID</param>
        /// <param name="updateDto">The updated rule engine configuration data</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The updated rule engine configuration</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<RuleEngineDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> Update(int id, [FromBody] UpdateRuleEngineDto updateDto, CancellationToken cancellationToken)
            => this.ExecuteUpdate(_ruleEngineService, id, updateDto, _logger, cancellationToken);

        /// <summary>
        /// Delete a rule engine configuration
        /// </summary>
        /// <param name="id">The rule engine configuration ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
            => this.ExecuteDelete(_ruleEngineService, id, _logger, cancellationToken);

        /// <summary>
        /// Get version history for a specific rule
        /// </summary>
        /// <param name="id">The rule engine configuration ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of version history records</returns>
        [HttpGet("{id}/versions")]
        [ProducesResponseType(typeof(List<RuleVersionHistoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetVersionHistory(int id, CancellationToken cancellationToken)
        {
            try
            {
                var history = await _ruleEngineService.GetVersionHistoryAsync(id, cancellationToken);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving version history for rule {RuleId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "An error occurred while retrieving version history"
                });
            }
        }

        /// <summary>
        /// Get all available rule categories from RuleCategoryMaster.
        /// Used by the frontend rule builder to populate the category dropdown dynamically.
        /// </summary>
        [HttpGet("categories")]
        [ProducesResponseType(typeof(List<RuleCategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
        {
            try
            {
                var categories = await _ruleExecutionService.GetCategoriesAsync(cancellationToken);
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving rule categories");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "An error occurred while retrieving rule categories"
                });
            }
        }

        /// <summary>
        /// Execute all enabled rules for a given category against a dynamic property tax input.
        /// Returns results for every rule whose condition matched, including the computed adjusted rate.
        /// Called internally by CapitalValueService / RateableValueService during tax calculation.
        /// Can also be used as a standalone test/preview endpoint.
        /// </summary>
        /// <remarks>
        /// Sample request body:
        /// <code>
        /// {
        ///   "category": "ARV",
        ///   "input": {
        ///     "Floor": 65,
        ///     "TypeOfUseGroup": 1,
        ///     "Rate": 1000.0
        ///   }
        /// }
        /// </code>
        /// </remarks>
        [HttpPost("execute")]
        [ProducesResponseType(typeof(List<RuleExecutionResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Execute(
            [FromBody] RuleExecutionInputDto inputDto,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(inputDto.Category))
                return BadRequest(new { message = "Category is required." });

            try
            {
                var results = await _ruleExecutionService.ExecuteAsync(inputDto, cancellationToken);
                return Ok(results);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid rule execution input for category={Category}", inputDto.Category);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing rules for category={Category}", inputDto.Category);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "An error occurred while executing rules"
                });
            }
        }
    }
}
