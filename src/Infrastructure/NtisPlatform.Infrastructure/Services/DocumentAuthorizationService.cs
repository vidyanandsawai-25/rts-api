using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Implementation of document authorization service
/// Checks document ownership and binding-based access control
/// </summary>
public class DocumentAuthorizationService : IDocumentAuthorizationService
{
    private readonly ApplicationDbContext _context;

    public DocumentAuthorizationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CanAccessDocumentAsync(
        Guid documentGuid, 
        int userId, 
        CancellationToken cancellationToken = default)
    {
        // Check if document exists and user is owner OR uploader
        var document = await _context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(
                d => d.DocumentGuid == documentGuid && 
                     d.IsActive && 
                     (d.OwnerUserId == userId || d.UploadedByUserId == userId),
                cancellationToken);

        if (document != null)
            return true;

        // Check if user has access via DocumentBinding authorization
        var hasBindingAccess = await _context.Documents
            .AsNoTracking()
            .Where(d => d.DocumentGuid == documentGuid && d.IsActive)
            .SelectMany(d => d.DocumentBindings)
            .AnyAsync(
                b => b.IsActive && 
                     b.AuthModuleCode != null && 
                     b.AuthReferenceId == userId, // User is referenced in binding auth
                cancellationToken);

        return hasBindingAccess;
    }

    public async Task<bool> CanModifyDocumentAsync(
        Guid documentGuid, 
        int userId, 
        CancellationToken cancellationToken = default)
    {
        // Only owner or uploader can modify/delete documents
        var canModify = await _context.Documents
            .AsNoTracking()
            .AnyAsync(
                d => d.DocumentGuid == documentGuid && 
                     d.IsActive && 
                     (d.OwnerUserId == userId || d.UploadedByUserId == userId),
                cancellationToken);

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

        if (binding == null)
            return false;

        // User must own or have uploaded the document
        var isOwnerOrUploader = binding.Document.OwnerUserId == userId || 
                                binding.Document.UploadedByUserId == userId;

        if (isOwnerOrUploader)
            return true;

        // Or user must be authorized via binding's auth context
        var isAuthorized = binding.AuthReferenceId.HasValue && 
                          binding.AuthReferenceId.Value == userId;

        return isAuthorized;
    }
}
