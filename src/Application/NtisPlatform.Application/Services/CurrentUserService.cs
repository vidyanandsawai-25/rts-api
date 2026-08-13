using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Application.Services;

/// <inheritdoc cref="ICurrentUserService"/>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var claim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? user?.FindFirst(ClaimTypes.Name)?.Value
                 ?? user?.FindFirst("userId")?.Value;

        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id) || id <= 0)
        {
            throw new UnauthorizedAccessException("Valid user identification is required.");
        }

        return id;
    }
}
