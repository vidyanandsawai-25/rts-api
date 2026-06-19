using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.Rules.RuleEngine;
using NtisPlatform.Application.DTOs.Rules.RuleExecution;
using NtisPlatform.Application.DTOs.Rules.RuleCategory;
using NtisPlatform.Application.Interfaces.Rules;
using NtisPlatform.Application.Models;


namespace NtisPlatform.Api.Controllers.Rules
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
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<RuleEngineDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> GetAll([FromQuery] RuleEngineQueryParameters queryParameters, CancellationToken cancellationToken)
            => this.ExecuteGetAllPaged(_ruleEngineService, queryParameters, _logger, cancellationToken);

        /// <summary>
        /// Get a specific rule engine configuration by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(RuleEngineDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
            => this.ExecuteGetById(_ruleEngineService, id, _logger, cancellationToken);

        /// <summary>
        /// Create a new rule engine configuration
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<RuleEngineDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> Create([FromBody] CreateRuleEngineDto createDto, CancellationToken cancellationToken)
            => this.ExecuteCreate(_ruleEngineService, createDto, _logger, cancellationToken);



        /// <summary>
        /// Update an existing rule engine configuration
        /// </summary>
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
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
            => this.ExecuteDelete(_ruleEngineService, id, _logger, cancellationToken);

        /// <summary>
        /// Get version history for a specific rule
        /// </summary>
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
        /// Get a lightweight, priority-ordered summary list of all active rules with pagination.
        /// Returns RuleCode, RuleName, Description, RuleCategory, Priority, IsEnabled,
        /// StopProcessing, RuleScopeId, RuleScopeName, and SubRules metadata.
        /// Heavy JSON blobs (RuleJson, ConditionsJson, EffectJson, TargetFiltersJson) are excluded.
        /// Use GET /{id} to fetch the full rule detail needed for editing.
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(PagedResult<RuleEngineSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSummary([FromQuery] RuleEngineQueryParameters queryParameters, CancellationToken cancellationToken)
        {
            try
            {
                var summary = await _ruleEngineService.GetSummaryAsync(queryParameters, cancellationToken);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving rule engine summary list");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "An error occurred while retrieving the rule summary list"
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
        /// Performs a full dry-run of all rules for the given category and input.
        /// Returns a detailed trace of every sub-rule — both matched and unmatched —
        /// so rule authors can debug expressions before deploying them to production.
        ///
        /// Optionally accepts a raw <c>ruleJson</c> in the body to test a rule
        /// without saving it to the database first.
        /// </summary>
        [HttpPost("dry-run")]
        [ProducesResponseType(typeof(RuleDryRunResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DryRun(
            [FromBody] RuleDryRunInputDto inputDto,
            CancellationToken cancellationToken)
        {
            if (inputDto == null)
                return BadRequest(new { message = "Request body is required." });

            if (string.IsNullOrWhiteSpace(inputDto.Category) && string.IsNullOrWhiteSpace(inputDto.RuleJson))
                return BadRequest(new { message = "Either Category (to load rules from DB) or RuleJson (ad-hoc test) must be provided." });

            if (inputDto.Input == null || !inputDto.Input.Any())
                return BadRequest(new { message = "Input dictionary cannot be null or empty." });

            try
            {
                var result = await _ruleExecutionService.DryRunAsync(inputDto, cancellationToken);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid dry-run input for category={Category}", inputDto.Category);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during dry-run for category={Category}", inputDto.Category);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "An error occurred during the dry-run."
                });
            }
        }

        /// <summary>
        /// Execute all enabled rules for a given category against a dynamic property tax input.
        /// Returns results for every rule whose condition matched, including the computed adjusted rate.
        /// </summary>
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
