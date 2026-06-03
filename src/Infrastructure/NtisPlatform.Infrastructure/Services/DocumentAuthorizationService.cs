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
        // Check if document exists and user is the uploader
        var document = await _context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(
                d => d.DocumentGuid == documentGuid && 
                     d.IsActive && 
                     d.UploadedByUserId == userId,
                cancellationToken);

        if (document != null)
            return true;

        // TODO: Implement proper entity-level authorization
        // For binding-based access, we need to check if the user has permission
        // to access the entity referenced by (AuthDepartmentId, AuthReferenceId).
        // For example, for PropertyCertificate documents:
        //   - AuthReferenceId = PropertyId (not userId)
        //   - Need to verify user can access that Property via department/module permissions
        // This requires a proper authorization service that can check:
        //   "Does user {userId} have access to Property {AuthReferenceId} in Department {AuthDepartmentId}?"
        // Until that service is implemented, only uploaders have access.

        return false;
    }

    public async Task<bool> CanModifyDocumentAsync(
        Guid documentGuid, 
        int userId, 
        CancellationToken cancellationToken = default)
    {
        // Only uploader can modify/delete documents
        var canModify = await _context.Documents
            .AsNoTracking()
            .AnyAsync(
                d => d.DocumentGuid == documentGuid && 
                     d.IsActive && 
                     d.UploadedByUserId == userId,
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

        if (binding == null || binding.Document == null)
            return false;

        // User must have uploaded the document
        var isUploader = binding.Document.UploadedByUserId == userId;

        if (isUploader)
            return true;

        // CRITICAL: Do NOT authorize using AuthReferenceId == userId
        // because AuthReferenceId is entity-id (e.g., PropertyId), not user-id.
        // Return false until proper entity-level authorization is implemented.
        return false;
    }
}
