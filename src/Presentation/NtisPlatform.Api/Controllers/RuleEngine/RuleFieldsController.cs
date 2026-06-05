using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.RuleEngine;
using NtisPlatform.Application.Interfaces.RuleEngine;

namespace NtisPlatform.Api.Controllers.RuleEngine;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RuleFieldsController : ControllerBase
{
    private readonly IRuleFieldsService _ruleFieldsService;
    private readonly ILogger<RuleFieldsController> _logger;

    public RuleFieldsController(IRuleFieldsService ruleFieldsService, ILogger<RuleFieldsController> logger)
    {
        _ruleFieldsService = ruleFieldsService;
        _logger = logger;
    }

    /// <summary>
    /// Get all rule fields with optional filtering and pagination
    /// </summary>
    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] RuleFieldsQueryParameters queryParameters, CancellationToken ct)
        => this.ExecuteGetAllPaged(_ruleFieldsService, queryParameters, _logger, ct);

    /// <summary>
    /// Get a rule field by ID with its configuration
    /// </summary>
    [HttpGet("{id:int}")]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_ruleFieldsService, id, _logger, ct);

    /// <summary>
    /// Get rule fields by RuleScopeId with configuration details (includes mapping)
    /// </summary>
    [HttpGet("by-scope/{ruleScopeId}")]
    public async Task<IActionResult> GetByFieldIdAsync(int ruleScopeId, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _ruleFieldsService.GetByFieldIdAsync(ruleScopeId, cancellationToken);

            if (response == null || !response.Any())
            {
                _logger.LogInformation("No rule fields found for RuleScopeId: {RuleScopeId}", ruleScopeId);
                return NotFound(new { message = "No rule fields found for the specified RuleScopeId" });
            }

            _logger.LogInformation("Retrieved {Count} rule fields for RuleScopeId: {RuleScopeId}", response.Count, ruleScopeId);
            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected or request timed out - let it propagate
            // ASP.NET Core will handle this appropriately (no 500 error logged)
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rule fields for RuleScopeId: {RuleScopeId}", ruleScopeId);
            return StatusCode(500, new { message = "An error occurred while retrieving rule fields" });
        }
    }

    /// <summary>
    /// Create a new rule field with optional configuration
    /// </summary>
    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateRuleFieldsDto createDto, CancellationToken ct)
        => this.ExecuteCreate(_ruleFieldsService, createDto, _logger, ct);

    /// <summary>
    /// Update an existing rule field and its configuration
    /// </summary>
    [HttpPut("{id}")]
    public Task<IActionResult> Update(int id, [FromBody] UpdateRuleFieldsDto updateDto, CancellationToken ct)
        => this.ExecuteUpdate(_ruleFieldsService, id, updateDto, _logger, ct);

    /// <summary>
    /// Delete a rule field and its configuration
    /// </summary>
    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_ruleFieldsService, id, _logger, ct);
}