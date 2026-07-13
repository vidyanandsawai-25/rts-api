using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Implementation of document authorization service.
/// Uses dispatcher + handler pattern for deny-by-default entity-level authorization.
/// Handlers are per-department (e.g., PtisDocumentAuthorizationHandler for PTIS).
/// </summary>
public class DocumentAuthorizationService : IDocumentAuthorizationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEnumerable<IDocumentAuthorizationHandler> _authorizationHandlers;
    private readonly ILogger<DocumentAuthorizationService> _logger;

    public DocumentAuthorizationService(
        ApplicationDbContext context,
        IEnumerable<IDocumentAuthorizationHandler> authorizationHandlers,
        ILogger<DocumentAuthorizationService> logger)
    {
        _context = context;
        _authorizationHandlers = authorizationHandlers ?? Enumerable.Empty<IDocumentAuthorizationHandler>();
        _logger = logger;
    }

    public async Task<bool> CanAccessDocumentAsync(
        Guid documentGuid,
        int userId,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Check if user is the document uploader (document owner)
        var document = await _context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(
                d => d.DocumentGuid == documentGuid &&
                     d.IsActive &&
                     d.UploadedByUserId == userId,
                cancellationToken);

        if (document != null)
        {
            _logger.LogDebug("Document {DocumentGuid} access granted to uploader {UserId}",
                documentGuid, userId);
            return true;
        }

        // Step 2: Check entity-level authorization via handlers (deny-by-default)
        // Get document and its bindings
        var docWithBindings = await _context.Documents
            .AsNoTracking()
            .Include(d => d.DocumentBindings.Where(b => b.IsActive && !b.MarkedForDeletion))
            .FirstOrDefaultAsync(d => d.DocumentGuid == documentGuid && d.IsActive && !d.MarkedForDeletion, cancellationToken);

        if (docWithBindings == null)
        {
            _logger.LogWarning("Document {DocumentGuid} not found", documentGuid);
            return false;
        }

        if (docWithBindings.DocumentBindings.Count == 0)
        {
            _logger.LogWarning("Document {DocumentGuid} has no active bindings for authorization check",
                documentGuid);
            return false;
        }

        // Check each binding with appropriate handler
        foreach (var binding in docWithBindings.DocumentBindings)
        {
            if (!binding.AuthDepartmentId.HasValue)
            {
                _logger.LogWarning(
                    "Document binding {BindingId} missing AuthDepartmentId for authorization check",
                    binding.Id);
                continue;
            }

            // Find handler for this binding's department
            var handler = _authorizationHandlers.FirstOrDefault(h => h.DepartmentId == binding.AuthDepartmentId);
            if (handler == null)
            {
                _logger.LogWarning(
                    "No authorization handler registered for department {DepartmentId}. Deny-by-default.",
                    binding.AuthDepartmentId);
                continue; // No handler = deny access
            }

            // Ask handler if user can access this binding's entity
            var canAccess = await handler.CanAccessAsync(binding, userId, cancellationToken);
            if (canAccess)
            {
                _logger.LogDebug(
                    "Document {DocumentGuid} access granted via handler for department {DepartmentId}",
                    documentGuid, binding.AuthDepartmentId);
                return true;
            }
        }

        // No handler granted access = deny
        _logger.LogWarning("User {UserId} denied access to document {DocumentGuid}: no handler approved",
            userId, documentGuid);
        return false;
    }

    public async Task<bool> CanModifyDocumentAsync(
        Guid documentGuid,
        int userId,
        CancellationToken cancellationToken = default)
    {
        // Only uploader can modify/delete documents (stricter than view access)
        var canModify = await _context.Documents
            .AsNoTracking()
            .AnyAsync(
                d => d.DocumentGuid == documentGuid &&
                     d.IsActive &&
                     d.UploadedByUserId == userId,
                cancellationToken);

        if (!canModify)
        {
            _logger.LogWarning("User {UserId} denied modification of document {DocumentGuid}: not uploader",
                userId, documentGuid);
        }

        return canModify;
    }

    public async Task<bool> CanAccessDocumentBindingAsync(
        int documentBindingId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        // Check if user owns the document that this binding references
        var binding = await _context.DocumentBindings
            .AsNoTracking()
            .Include(b => b.Document)
            .FirstOrDefaultAsync(b => b.Id == documentBindingId && b.IsActive, cancellationToken);

        if (binding == null || binding.Document == null)
        {
            _logger.LogWarning("Document binding {BindingId} not found", documentBindingId);
            return false;
        }

        // Check uploader (document owner)
        var isUploader = binding.Document.UploadedByUserId == userId;
        if (isUploader)
        {
            _logger.LogDebug("Binding {BindingId} access granted to uploader {UserId}",
                documentBindingId, userId);
            return true;
        }

        // Check entity-level authorization via handler
        if (!binding.AuthDepartmentId.HasValue)
        {
            _logger.LogWarning(
                "Binding {BindingId} missing AuthDepartmentId for authorization check",
                documentBindingId);
            return false;
        }

        var handler = _authorizationHandlers.FirstOrDefault(h => h.DepartmentId == binding.AuthDepartmentId);
        if (handler == null)
        {
            _logger.LogWarning(
                "No authorization handler for binding {BindingId}, department {DepartmentId}. Deny.",
                documentBindingId, binding.AuthDepartmentId);
            return false;
        }

        var canAccess = await handler.CanAccessAsync(binding, userId, cancellationToken);
        if (!canAccess)
        {
            _logger.LogWarning(
                "User {UserId} denied access to binding {BindingId}: handler denied",
                userId, documentBindingId);
        }

        return canAccess;
    }
}
