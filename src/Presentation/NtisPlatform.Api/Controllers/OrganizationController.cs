using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NtisPlatform.Application.DTOs.Auth;
using NtisPlatform.Application.DTOs.Organization;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Auth;
using NtisPlatform.Core.Entities;
using static NtisPlatform.Core.Entities.SettingKeys;

namespace NtisPlatform.Api.Controllers;

/// <summary>
/// Organization configuration and settings management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrganizationController : ControllerBase
{
    private readonly IOrganizationService _organizationService;
    private readonly IAuthService _authService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<OrganizationController> _logger;

    public OrganizationController(
        IOrganizationService organizationService,
        IAuthService authService,
        IPasswordHasher passwordHasher,
        ILogger<OrganizationController> logger)
    {
        _organizationService = organizationService;
        _authService = authService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <summary>
    /// Check if initial setup is required (public endpoint)
    /// Rate limited: 100 requests per minute per IP (global default)
    /// </summary>
    [HttpGet("setup-status")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SetupStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<SetupStatusResponse>> GetSetupStatus(CancellationToken cancellationToken)
    {
        var isSetupRequired = await _organizationService.IsSetupRequiredAsync(cancellationToken);
        var organization = await _organizationService.GetOrganizationAsync(cancellationToken);

        return Ok(new SetupStatusResponse
        {
            IsSetupRequired = isSetupRequired,
            OrganizationName = organization?.Name
        });
    }

    /// <summary>
    /// Complete initial setup (public endpoint - one-time use)
    /// Rate limited: 100 requests per minute per IP (global default)
    /// </summary>
    [HttpPost("initial-setup")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(InitialSetupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<InitialSetupResponse>> CompleteInitialSetup(
        [FromBody] InitialSetupRequest request,
        CancellationToken cancellationToken)
    {
        // Check if setup already completed
        var isSetupRequired = await _organizationService.IsSetupRequiredAsync(cancellationToken);
        if (!isSetupRequired)
        {
            return BadRequest(new InitialSetupResponse
            {
                Success = false,
                Message = "Initial setup has already been completed"
            });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new InitialSetupResponse
            {
                Success = false,
                Message = "Invalid request data"
            });
        }

        // Create organization with minimal fields
        var organization = new Organization
        {
            Name = request.OrganizationName
        };

        // Prepare organization settings
        var orgSettings = new Dictionary<string, string>
        {
            [SettingKeys.PrimaryContactEmail] = request.PrimaryContactEmail,
            [SettingKeys.PrimaryContactPhone] = request.PrimaryContactPhone ?? string.Empty,
            [SettingKeys.OrganizationAddress] = request.Address ?? string.Empty,
            [SettingKeys.City] = request.City ?? string.Empty,
            [SettingKeys.State] = request.State ?? string.Empty,
            [SettingKeys.Country] = "India"
        };

        // Create admin user
        var adminUser = new User
        {
            Username = request.AdminUsername,
            Email = request.AdminEmail,
            FirstName = request.AdminFirstName,
            LastName = request.AdminLastName,
            PasswordHash = _passwordHasher.HashPassword(request.AdminPassword)
        };

        // Complete setup (creates org and admin user)
        var completedOrg = await _organizationService.CompleteInitialSetupAsync(organization, adminUser, cancellationToken);
        
        // Save organization settings
        await _organizationService.UpdateOrganizationSettingsAsync(orgSettings, cancellationToken);

        return Ok(new InitialSetupResponse
        {
            Success = true,
            Message = "Initial setup completed successfully. You can now login with your admin credentials.",
            Organization = new BasicOrganizationResponse
            {
                Id = completedOrg.Id,
                Name = completedOrg.Name,
                IsActive = completedOrg.IsActive,
                IsSetupComplete = completedOrg.IsSetupComplete,
                CreatedAt = completedOrg.CreatedAt,
                UpdatedAt = completedOrg.UpdatedAt
            }
        });
    }

    /// <summary>
    /// Get organization configuration for login page (public endpoint)
    /// Returns default values if setup not complete
    /// Rate limited: 100 requests per minute per IP (global default)
    /// </summary>
    [HttpGet("info")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OrganizationConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<OrganizationConfigResponse>> GetOrganizationInfo(CancellationToken cancellationToken)
    {
        var organization = await _organizationService.GetOrganizationAsync(cancellationToken);
        
        // If setup not complete, return default values
        if (organization == null || !organization.IsSetupComplete)
        {
            return Ok(new OrganizationConfigResponse
            {
                OrganizationId = "0",
                Name = "Municipal Services Portal",
                LogoUrl = null, // Frontend will use default logo
                EnabledAuthProviders = new List<AuthProviderConfig>
                {
                    new AuthProviderConfig
                    {
                        Type = AuthProviderType.Basic,
                        DisplayName = "Username/Password",
                        IsDefault = true
                    }
                },
                RequiresTwoFactor = false
            });
        }

        var config = await _authService.GetOrganizationConfigAsync(cancellationToken);

        if (config == null)
        {
            return NotFound(new { error = "Organization configuration not found" });
        }

        return Ok(config);
    }

    /// <summary>
    /// Get basic organization details (super admin only)
    /// Use /api/OrganizationSettings for configuration values
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(BasicOrganizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BasicOrganizationResponse>> GetOrganization(CancellationToken cancellationToken)
    {
        var organization = await _organizationService.GetOrganizationAsync(cancellationToken);

        if (organization == null)
        {
            return NotFound(new { error = "Organization not found" });
        }

        var response = new BasicOrganizationResponse
        {
            Id = organization.Id,
            Name = organization.Name,
            IsActive = organization.IsActive,
            IsSetupComplete = organization.IsSetupComplete,
            CreatedAt = organization.CreatedAt,
            UpdatedAt = organization.UpdatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Update organization name (super admin only)
    /// Use /api/OrganizationSettings to update configuration values
    /// </summary>
    [HttpPut]
    [Authorize(Roles = "SuperAdmin")]
    [ProducesResponseType(typeof(BasicOrganizationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BasicOrganizationResponse>> UpdateOrganization(
        [FromBody] UpdateBasicOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var organization = await _organizationService.GetOrganizationAsync(cancellationToken);
        
        if (organization == null)
        {
            return NotFound(new { error = "Organization not found" });
        }

        // Update Name and optionally IsSetupComplete
        organization.Name = request.Name;
        if (request.IsSetupComplete.HasValue)
        {
            organization.IsSetupComplete = request.IsSetupComplete.Value;
        }
        var updated = await _organizationService.UpdateOrganizationAsync(organization, cancellationToken);
        
        var response = new BasicOrganizationResponse
        {
            Id = updated.Id,
            Name = updated.Name,
            IsActive = updated.IsActive,
            IsSetupComplete = updated.IsSetupComplete,
            CreatedAt = updated.CreatedAt,
            UpdatedAt = updated.UpdatedAt
        };

        return Ok(response);
    }

}

