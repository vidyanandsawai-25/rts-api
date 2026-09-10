using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.Property.ApartmentQC;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Api.Controllers;

public partial class ApartmentQCController
{
    /// <summary>
    /// Returns the distinct plan/property type codes across every property in every wing
    /// belonging to the same society as the given property.
    /// Resolution chain: PropertyMast.WingDetailId -> WingDetailsMast.SocietyDetailsMastId ->
    /// every WingDetailsMast row in that society -> every PropertyMast row in those wings.
    /// </summary>
    /// <param name="propertyId">PropertyMast Id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Distinct plan type codes (empty list when the property or its society has none).</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("{propertyId:int}/plan-type")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<string>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPlanType(int propertyId, CancellationToken ct)
    {
        var result = await _service.GetPlanTypesAsync(propertyId, ct);
        return Ok(new ApiResponse<IReadOnlyList<string>>
        {
            Success = true,
            Message = result.Count > 0 ? "Plan types retrieved successfully" : "No plan types found",
            Items   = result
        });
    }

    /// <summary>
    /// Returns the next available plan type for the given property's society: the highest
    /// numeric value among <see cref="GetPlanType"/>'s distinct results, plus 1.
    /// E.g. a society whose wings have plan types 1..6 returns 7.
    /// Returns 1 when the society has no numeric plan type yet.
    /// </summary>
    /// <param name="propertyId">PropertyMast Id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Next plan type value.</response>
    /// <response code="500">Internal server error.</response>
    [HttpGet("{propertyId:int}/new-plan-type")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetNewPlanType(int propertyId, CancellationToken ct)
    {
        var result = await _service.GetNextPlanTypeAsync(propertyId, ct);
        return Ok(new ApiResponse<int>
        {
            Success = true,
            Message = "New plan type generated successfully",
            Items   = result
        });
    }

    /// <summary>
    /// Saves the caller's chosen plan type onto PropertyMast.Type for the given property.
    /// The submitted <c>Type</c> must equal either one of the existing plan types returned by
    /// <see cref="GetPlanType"/> or the next available plan type returned by
    /// <see cref="GetNewPlanType"/> — only one of the two may be chosen and saved.
    /// </summary>
    /// <param name="propertyId">PropertyMast Id.</param>
    /// <param name="dto">The chosen plan type.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Plan type saved successfully.</response>
    /// <response code="400">Type is missing, or does not match an existing or the next available plan type.</response>
    /// <response code="401">Caller is not authenticated.</response>
    /// <response code="404">No active property found for the given propertyId.</response>
    /// <response code="500">Internal server error.</response>
    [Authorize]
    [HttpPatch("{propertyId:int}/save-plan-type")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SavePlanType(
        int propertyId,
        [FromBody] SavePlanTypeDto dto,
        CancellationToken ct)
    {
        if (dto is null)
            return BadRequest(new ApiResponse<object> { Success = false, Message = "Request body is required." });

        var outcome = await _service.SavePlanTypeAsync(propertyId, dto.Type, GetCurrentUserId(), ct);

        return outcome switch
        {
            SavePlanTypeOutcome.PropertyNotFound => NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = $"No active property found for propertyId {propertyId}."
            }),
            SavePlanTypeOutcome.InvalidType => BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = $"Type '{dto.Type}' must match either an existing plan type or the next available plan type for this property's society."
            }),
            _ => Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Plan type saved successfully."
            })
        };
    }
}
