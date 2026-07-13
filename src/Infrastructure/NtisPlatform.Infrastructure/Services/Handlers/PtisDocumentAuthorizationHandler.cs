using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Infrastructure.Services.Handlers;

/// <summary>
/// Authorization handler for PTIS (Property Tax Information System) documents.
/// Checks if a user can access documents bound to Property entities.
/// </summary>
public class PtisDocumentAuthorizationHandler : IDocumentAuthorizationHandler
{
    // PTIS department ID - should match DepartmentMaster.Id for "PTIS"
    // This value should ideally come from configuration, but for now we accept it via constructor
    public int DepartmentId { get; private set; }

    private readonly ILogger<PtisDocumentAuthorizationHandler> _logger;

    public PtisDocumentAuthorizationHandler(
        int ptisDepartmentId,
        ILogger<PtisDocumentAuthorizationHandler> logger)
    {
        DepartmentId = ptisDepartmentId;
        _logger = logger;
    }

    public async Task<bool> CanAccessAsync(
        DocumentBindingEntity binding,
        int userId,
        CancellationToken cancellationToken)
    {
        // Validate binding is for PTIS
        if (binding.AuthDepartmentId != DepartmentId)
        {
            _logger.LogWarning(
                "Authorization handler for PTIS (DeptId={HandlerDeptId}) received binding for different department {BindingDeptId}",
                DepartmentId, binding.AuthDepartmentId);
            return false;
        }

        // Extract property ID from binding's auth reference
        if (!binding.AuthReferenceId.HasValue || binding.AuthReferenceId.Value <= 0)
        {
            _logger.LogWarning("Document binding {BindingId} has invalid AuthReferenceId", binding.Id);
            return false;
        }

        var propertyId = binding.AuthReferenceId.Value;

        // Property-level authorization check: deny-by-default
        // In production: check user's role vs property permissions
        // For now: deny to prevent unauthorized access
        // Future enhancement: integrate with property-level access control system
        _logger.LogInformation(
            "PTIS document access check: User {UserId} requesting Property {PropertyId} document (BindingId={BindingId})",
            userId, propertyId, binding.Id);

        // Deny-by-default: only grant if explicit authorization added
        return await Task.FromResult(false);
    }
}
