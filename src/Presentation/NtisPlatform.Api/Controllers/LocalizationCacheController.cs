using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Admin endpoints to control in-memory localization dictionary. Use after inserting/updating translations in DB.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/localization-cache")]
public class LocalizationCacheController : ControllerBase
{
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<LocalizationCacheController> _logger;

    public LocalizationCacheController(
        ILocalizationService localizationService,
        ILogger<LocalizationCacheController> logger)
    {
        _localizationService = localizationService;
        _logger = logger;
    }

    /// <summary>
    /// Clears cached localization entries from memory.
    /// - No params: clears entire cache
    /// - resource only: clears all buckets for that resource
    /// - resource + language: clears only that bucket
    /// - resource + language + key: clears only that specific key inside the bucket
    /// </summary>
    /// <example>POST /api/localization-cache/invalidate?resource=ValidationMessages&amp;language=hi&amp;key=FloorID_Required</example>
    [HttpPost("invalidate")]
    public IActionResult Invalidate(
        [FromQuery] string? resource = null,
        [FromQuery] string? language = null,
        [FromQuery] string? key = null)
    {
        try
        {
            _logger.LogInformation(
                "Cache invalidation requested - Resource: {Resource}, Language: {Language}, Key: {Key}",
                resource ?? "ALL", language ?? "ALL", key ?? "ALL");

            _localizationService.Invalidate(resource, language, key);

            return Ok(new
            {
                success = true,
                message = "Localization cache invalidated successfully",
                resource = resource ?? "ALL",
                language = language ?? "ALL",
                key = key ?? "ALL"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to invalidate cache - Resource: {Resource}, Language: {Language}, Key: {Key}",
                resource, language, key);

            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while invalidating the cache",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Reloads translations from DB into memory.
    /// - No params: reloads entire cache
    /// - resource only: reloads all language buckets for that resource
    /// - resource + language: reloads only that language bucket for the resource
    /// - resource + language + key: reloads only that single key for that language bucket
    /// </summary>
    /// <example>POST /api/localization-cache/reload?resource=ValidationMessages&amp;language=mr&amp;key=FloorID_MaxLen_5</example>
    [HttpPost("reload")]
    public async Task<IActionResult> Reload(
        [FromQuery] string? resource = null,
        [FromQuery] string? language = null,
        [FromQuery] string? key = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Cache reload requested - Resource: {Resource}, Language: {Language}, Key: {Key}",
                resource ?? "ALL", language ?? "ALL", key ?? "ALL");

            await _localizationService.ReloadAsync(resource, language, key, ct: ct);

            return Ok(new
            {
                success = true,
                message = "Localization cache reloaded successfully",
                resource = resource ?? "ALL",
                language = language ?? "ALL",
                key = key ?? "ALL"
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Cache reload operation was cancelled");

            return StatusCode(499, new
            {
                success = false,
                message = "Cache reload operation was cancelled"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to reload cache - Resource: {Resource}, Language: {Language}, Key: {Key}",
                resource, language, key);

            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while reloading the cache",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Convenience endpoint: Invalidate + Reload.
    /// Recommended after DB updates to ensure cache contains the latest values.
    /// </summary>
    /// <example>POST /api/localization-cache/refresh?resource=ValidationMessages&amp;language=hi&amp;key=FloorID_Required</example>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromQuery] string? resource = null,
        [FromQuery] string? language = null,
        [FromQuery] string? key = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Cache refresh requested - Resource: {Resource}, Language: {Language}, Key: {Key}",
                resource ?? "ALL", language ?? "ALL", key ?? "ALL");

            await _localizationService.RefreshAsync(resource, language, key, ct);

            return Ok(new
            {
                success = true,
                message = "Localization cache refreshed successfully (invalidate + reload)",
                resource = resource ?? "ALL",
                language = language ?? "ALL",
                key = key ?? "ALL"
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Cache refresh operation was cancelled");

            return StatusCode(499, new
            {
                success = false,
                message = "Cache refresh operation was cancelled"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to refresh cache - Resource: {Resource}, Language: {Language}, Key: {Key}",
                resource, language, key);

            return StatusCode(500, new
            {
                success = false,
                message = "An error occurred while refreshing the cache",
                error = ex.Message
            });
        }
    }
}