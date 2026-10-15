using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Infrastructure.Services.Handlers;

/// <summary>
/// Authorization handler for RTS (Right to Services) documents.
/// Grants access to officers and users viewing documents bound to RTS applications.
/// </summary>
public sealed class RtsDocumentAuthorizationHandler : IDocumentAuthorizationHandler
{
    public int DepartmentId { get; } = 5; // RTS Department

    private readonly ILogger<RtsDocumentAuthorizationHandler> _logger;

    public RtsDocumentAuthorizationHandler(ILogger<RtsDocumentAuthorizationHandler> logger)
    {
        _logger = logger;
    }

    public Task<bool> CanAccessAsync(
        DocumentBindingEntity binding,
        int userId,
        CancellationToken cancellationToken)
    {
        if (binding.AuthDepartmentId != DepartmentId)
        {
            return Task.FromResult(false);
        }

        // Active RTS scrutiny and application documents are accessible to authenticated officers and citizens
        _logger.LogDebug(
            "RTS document access granted to User {UserId} for BindingId {BindingId}, AppId {AppId}",
            userId, binding.Id, binding.AuthReferenceId);

        return Task.FromResult(true);
    }
}
