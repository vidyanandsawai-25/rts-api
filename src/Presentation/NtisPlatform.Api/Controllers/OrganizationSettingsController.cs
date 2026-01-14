using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.DTOs.Organization;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Organization settings management (key-value configuration)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class OrganizationSettingsController : ControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly ILogger<OrganizationSettingsController> _logger;

    public OrganizationSettingsController(
        IOrganizationService organizationService,
        ILogger<OrganizationSettingsController> logger)
    {
        _organizationService = organizationService;
        _logger = logger;
    }

    /// <summary>
    /// Get specific organization settings by keys
    /// </summary>
    /// <param name="keys">Comma-separated list of setting keys (e.g., "Branding.LogoUrl,Organization.Email")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet]
    [ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Dictionary<string, string>>> GetSettings(
        [FromQuery] string? keys,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(keys))
        {
            return BadRequest(new { error = "At least one setting key is required. Use ?keys=Key1,Key2" });
        }

        var keyList = keys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var settings = await _organizationService.GetOrganizationSettingsAsync(keyList, cancellationToken);

        return Ok(settings);
    }

    /// <summary>
    /// Get all settings for a specific category (Branding, Organization, Security, etc.)
    /// </summary>
    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Dictionary<string, string>>> GetSettingsByCategory(
        string category,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return BadRequest(new { error = "Category is required" });
        }

        var settings = await _organizationService.GetOrganizationSettingsByCategoryAsync(category, cancellationToken);

        return Ok(settings);
    }

    /// <summary>
    /// Update organization settings (key-value pairs)
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(UpdateOrganizationSettingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpdateOrganizationSettingsResponse>> UpdateSettings(
        [FromBody] UpdateOrganizationSettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || request.Settings == null || request.Settings.Count == 0)
        {
            return BadRequest(new UpdateOrganizationSettingsResponse
            {
                Success = false,
                Message = "Invalid settings data"
            });
        }

        var updatedCount = await _organizationService.UpdateOrganizationSettingsAsync(request.Settings, cancellationToken);

        return Ok(new UpdateOrganizationSettingsResponse
        {
            Success = true,
            Message = $"Successfully updated {updatedCount} settings",
            UpdatedCount = updatedCount
        });
    }

    /// <summary>
    /// Delete a specific setting by key
    /// </summary>
    [HttpDelete("{key}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSetting(
        string key,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequest(new { error = "Setting key is required" });
        }

        var deleted = await _organizationService.DeleteOrganizationSettingAsync(key, cancellationToken);

        if (!deleted)
        {
            return NotFound(new { error = $"Setting '{key}' not found" });
        }

        return NoContent();
    }
}
