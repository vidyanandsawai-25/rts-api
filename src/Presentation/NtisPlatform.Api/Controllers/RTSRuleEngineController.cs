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

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => await this.ExecuteGetById(_service, id, _logger, ct);

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

    [AllowAnonymous]
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
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => await this.ExecuteDelete(_service, id, _logger, ct);

}
