using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.Constants;
using NtisPlatform.Application.DTOs.RTSApplication;
using NtisPlatform.Application.DTOs.RTSApplicationApproval;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RTSApplicationApprovalController : ControllerBase
{
    private readonly IRTSApplicationApprovalService _service;
    private readonly ILogger<RTSApplicationApprovalController> _logger;

    public RTSApplicationApprovalController(IRTSApplicationApprovalService service, ILogger<RTSApplicationApprovalController> logger)
    {
        _service = service;
        _logger= logger;
    }

    /// <summary>
    /// Get Application Approval Dashboard Cards Count
    /// </summary>
    [AllowAnonymous]
    [HttpGet("dashboard-cards")]
    [ProducesResponseType(typeof(ApiResponse<RTSApplicationDashboardCardsCountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDashboardCards(CancellationToken ct)
    {
        var result = await _service.GetDashboardCardsDataAsync(ct);
        return Ok(new ApiResponse<RTSApplicationDashboardCardsCountDto>
        {
            Success = true,
            Message = "Dashboard Cards Retrieved Successfully",
            Items = result
        });
    }

    /// <summary>
    /// Get Application Approval Dashboard Application Details
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<RTSApplicationDashboardDetailsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll([FromQuery] RTSApplicationQueryParameters query, CancellationToken ct)
    {
        var result = await _service.GetAllDashboardApplicationAsync(query, ct);
        return Ok(new ApiResponse<PagedResult< RTSApplicationDashboardDetailsDto>>
        {
            Success = true,
            Message = "Application Details Retrieved Successfully",
            Items = result
        });
    }


    /// <summary>
    /// this api get application facing how many desk related data and documents
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpGet("{applicationId}/details")]
    [ProducesResponseType(typeof(ApiResponse<RTSApplicationViewDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetApplicationDetails(int applicationId, CancellationToken ct)
    {
        var result = await _service.ViewApplicationApprovalSummaryAsync(applicationId, ct);
        if (result == null)
            return NotFound(new ApiResponse<RTSApplicationViewDetailsDto> { Success = false, Message = "Application not found" });

        return Ok(new ApiResponse<RTSApplicationViewDetailsDto>
        {
            Success = true,
            Message = "Application Details Retrieved Successfully",
            Items = result
        });
    }


    [AllowAnonymous]
    [HttpGet("{applicationId}/approval-stages")]
    [ProducesResponseType(typeof(ApiResponse<ApplicationApprovalStageDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ApplicationApprovalStages(int applicationId, CancellationToken ct)
    {

        var result = await _service.GetApplicationApprovalStagesAsync(applicationId, ct);

        if (result == null)
            return NotFound(new ApiResponse<RTSApplicationViewDetailsDto> { Success = false, Message = "Application Approval Stages not found" });

        return Ok(new ApiResponse<ApplicationApprovalStageDetailsDto>
        {
            Success = true,
            Message = "Approval Stages Retrieved Successfully",
            Items = result
        });
    }


    [AllowAnonymous]
    [HttpGet("{applicationId}/approval-officer")]
    [ProducesResponseType(typeof(ApiResponse<CurrentApprovalOfficerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetApplicationApprovalOfficer(int applicationId, CancellationToken ct)
    {

        var result = await _service.GetCurrentApprovalOfficerAsync(applicationId, ct);

        if (result == null)
            return NotFound(new ApiResponse<CurrentApprovalOfficerDto> { Success = false, Message = "Application not found or workflow completed" });

        return Ok(new ApiResponse<CurrentApprovalOfficerDto>
        {
            Success = true,
            Message = "RTS Application Approval Officer Retrieved Successfully",
            Items = result
        });
    }


    [AllowAnonymous]
    [HttpPut("{applicationId}/verify-documents")]
    [ProducesResponseType(typeof(ApiResponse<RTSApplicationApprovalResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> VerifyDocumentsAndProcessApplication(int applicationId, [FromBody] UpdateRTSApplicationProcessDto dto, CancellationToken ct)
    {
        var result = await _service.VerifyDocumentsAndProcessApplicationAsync(applicationId, dto, ct);

        return Ok(new ApiResponse<RTSApplicationApprovalResponseDto>
        {
            Success = true,
            Message = "Documents Verified Successfully",
            Items = result
        });
    }


    [AllowAnonymous]
    [HttpPut("{applicationId}/process-approval")]
    [ProducesResponseType(typeof(ApiResponse<RTSApplicationApprovalResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> VerifyApplicationAndSentToApprove(int applicationId, [FromBody] UpdateRTSApplicationProcessDto dto, CancellationToken ct)
    {
        var result = await _service.VerifyApplicationAndSentToApproveAsync(applicationId, dto, ct);
        return Ok(new ApiResponse<RTSApplicationApprovalResponseDto>
        {
            Success = true,
            Message = result.Status==ApplicationStatus.Approved ? "Application Approved Successfully" : "Application Verified And Forwarded Successfully",
            Items = result
        });
    }

    [AllowAnonymous]
    [HttpPut("{applicationId}/verify-and-correct")]
    [ProducesResponseType(typeof(ApiResponse<RTSApplicationApprovalResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> VerifyAndCorrectApplication(int applicationId, [FromBody] UpdateRTSApplicationVerificationDto dto, CancellationToken ct)
    {
        var result = await _service.VerifyAndCorrectApplicationAsync(applicationId, dto, ct);
        return Ok(new ApiResponse<RTSApplicationApprovalResponseDto>
        {
            Success = true,
            Message = "Application Corrected Successfully",
            Items = result
        });
    }


    [AllowAnonymous]
    [HttpPut("{applicationId}/Rejected-Application")]
    [ProducesResponseType(typeof(ApiResponse<RTSApplicationApprovalResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RejectApplicationByOfficerAsync(int applicationId, [FromBody] UpdateRTSApplicationProcessDto dto, CancellationToken ct)
    {
        var result = await _service.RejectApplicationByOfficerAsync(applicationId, dto, ct);
        return Ok(new ApiResponse<RTSApplicationApprovalResponseDto>
        {
            Success = true,
            Message = "Application Rejected Successfully",
            Items = result
        });
    }

    // <summary>
    // this api is used to revert the application to previous stage
    // </summary>


    [AllowAnonymous]
    [HttpPut("{applicationId}/Revert-Application")]
    [ProducesResponseType(typeof(ApiResponse<RTSApplicationApprovalResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> VerifyAndRevertApplication(int applicationId, [FromBody] UpdateRTSApplicationProcessDto dto, CancellationToken ct)
    {
        var result = await _service.VerifyAndRevertApplicationAsync(applicationId, dto, ct);
        return Ok(new ApiResponse<RTSApplicationApprovalResponseDto>
        {
            Success = true,
            Message = "Application Reverted Successfully",
            Items = result
        });
    }

    /// <summary>
    /// Complete RTS Application Audit Trail & Track History
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{applicationId}/track-history")]
    [ProducesResponseType(typeof(ApiResponse<List<NtisPlatform.Application.DTOs.RTSTrackApplicationHistory.RTSTrackApplicationHistoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTrackApplicationHistory(int applicationId, CancellationToken ct)
    {
        var result = await _service.GetTrackApplicationHistoryAsync(applicationId, ct);
        return Ok(new ApiResponse<List<NtisPlatform.Application.DTOs.RTSTrackApplicationHistory.RTSTrackApplicationHistoryDto>>
        {
            Success = true,
            Message = "Application Track History & Audit Trail Retrieved Successfully",
            Items = result
        });
    }
}

