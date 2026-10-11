using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Api.Extensions;
using NtisPlatform.Application.DTOs.RTSRuleMaster;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
namespace NtisPlatform.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RTSRuleEngineController : ControllerBase
{
    private readonly IRTSRuleMasterService _service;
    private readonly ILogger<RTSRuleEngineController> _logger;
    public RTSRuleEngineController(IRTSRuleMasterService service, ILogger<RTSRuleEngineController> logger)
    {
        _service = service;
        _logger = logger;
    }
    
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] RTSRuleMasterQueryParameters queryParameters, CancellationToken ct)
       => await this.ExecuteGetAllPaged(_service, queryParameters, _logger, ct);

    [HttpGet("ServiceCard")]
    public async Task<IActionResult> GetServiceCards(
        [FromQuery] RTSRuleServiceCardQueryParameters query,
        [FromServices] IRTSRuleServiceCardService cards,
        CancellationToken ct)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(query.RuleGroup))
            return BadRequest(ModelState);
        try
        {
            return Ok(await cards.GetCardsAsync(query, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load RTS rule service cards");
            return StatusCode(500, new { message = "Unable to load rule service cards." });
        }
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(RTSRuleMasterDto), StatusCodes.Status200OK)]
    public Task<IActionResult> GetById(int id, CancellationToken ct)
        => this.ExecuteGetById(_service, id, _logger, ct);

    [AllowAnonymous]
    [HttpPost("evaluate-fee")]
    [ProducesResponseType(typeof(ApiResponse<RTSFeeEvaluationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RTSFeeEvaluationResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EvaluateFee(
        [FromBody] RTSFeeEvaluationRequestDto request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiResponse<RTSFeeEvaluationResponseDto>
            {
                Success = false,
                Message = "Invalid fee evaluation request."
            });

        try
        {
            var result = await _service.EvaluateFeeAsync(request, ct);
            return Ok(new ApiResponse<RTSFeeEvaluationResponseDto>
            {
                Success = true,
                Message = "Fee evaluated successfully.",
                Items = result
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<RTSFeeEvaluationResponseDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<RTSFeeEvaluationResponseDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRTSRuleMasterRequestDto createDto, CancellationToken ct)
    {
        try
        {
            var result = await _service.CreateRulesAsync(createDto, ct);
            return Ok(new ApiResponse<RTSRuleMasterResponseDto>
            {
                Success = true,
                Message = $"{result.RuleCount} rule created successfully",
                Items = result
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<RTSRuleMasterResponseDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create rules operation failed");
            return StatusCode(500, new ApiResponse<RTSRuleMasterResponseDto>
            {
                Success = false,
                Message = "An error occurred while creating the rules"
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRTSRuleMasterRequestDto updateDto, CancellationToken ct)
    {
        try
        {
            var result = await _service.UpdateRulesAsync(id, updateDto, ct);
            return Ok(new ApiResponse<RTSRuleMasterResponseDto>
            {
                Success = true,
                Message = $"{result.RuleCount} rule updated successfully",
                Items = result
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<RTSRuleMasterResponseDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update rules operation failed");
            return StatusCode(500, new ApiResponse<RTSRuleMasterResponseDto>
            {
                Success = false,
                Message = "An error occurred while updating the rules"
            });
        }
    }


    [AllowAnonymous]
    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id, CancellationToken ct)
        => this.ExecuteDelete(_service, id, _logger, ct);
}
