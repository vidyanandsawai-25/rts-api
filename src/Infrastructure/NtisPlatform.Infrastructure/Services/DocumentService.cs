using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Exceptions;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Service for document management operations
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly IRepository<DocumentEntity, int> _documentRepository;
    private readonly IRepository<DocumentBindingEntity, int> _bindingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DocumentService(
        IRepository<DocumentEntity, int> documentRepository,
        IRepository<DocumentBindingEntity, int> bindingRepository,
        IUnitOfWork unitOfWork)
    {
        _documentRepository = documentRepository;
        _bindingRepository = bindingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<(int DocumentId, Guid DocumentGuid)> CreateDocumentAsync(
        int uploadedByUserId,
        int? ownerUserId,
        string fileName,
        string originalFileName,
        string fileExtension,
        string mimeType,
        long fileSizeBytes,
        string storagePath,
        string? thumbnailPath,
        string? checksumSha256,
        string? documentType,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(thumbnailPath))
        {
            throw new NotSupportedException("Thumbnail path persistence is not supported by CreateDocumentAsync.");
        }

        var document = DocumentEntity.Create(
            uploadedByUserId,
            fileName,
            originalFileName,
            fileExtension,
            mimeType,
            fileSizeBytes,
            storagePath,
            documentType);

        // OwnerUserId property removed from entity - ownership is always the uploader

        if (!string.IsNullOrWhiteSpace(checksumSha256))
        {
            document.SetChecksum(checksumSha256);
        }

        document.CreatedBy = uploadedByUserId;
        document.CreatedDate = DateTime.Now;
        document.IsActive = true;

        await _documentRepository.AddAsync(document, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (document.Id, document.DocumentGuid);
    }

    public async Task<int> CreateDocumentBindingAsync(
        int documentId,
        int departmentId,
        int moduleId,
        string referenceTableName,
        int? referenceTableId,
        Guid? referenceTableIdGuid,
        string referencePropertyName,
        string? bindingPurpose,
        bool isPrimaryDocument,
        int? authDepartmentId,
        int? authReferenceId,
        int createdBy,
        CancellationToken cancellationToken = default)
    {
        // Validate required fields
        if (departmentId <= 0)
        {
            throw new ArgumentException("Department ID must be greater than zero.", nameof(departmentId));
        }

        if (moduleId <= 0)
        {
            throw new ArgumentException("Module ID must be greater than zero.", nameof(moduleId));
        }

        if (string.IsNullOrWhiteSpace(referenceTableName))
        {
            throw new InvalidBindingException("Reference table name is required.", null, referenceTableName);
        }

        if (string.IsNullOrWhiteSpace(referencePropertyName))
        {
            throw new ArgumentException("Reference property name is required.", nameof(referencePropertyName));
        }

        // Validate XOR semantics: exactly one of referenceTableId or referenceTableIdGuid must be provided
        var (hasReferenceTableId, hasReferenceTableIdGuid) = ValidateReferenceIdXor(referenceTableId, referenceTableIdGuid);

        DocumentBindingEntity binding;

        if (hasReferenceTableId)
        {
            binding = DocumentBindingEntity.CreateWithIntReference(
                documentId,
                departmentId,
                moduleId,
                referenceTableName,
                referenceTableId!.Value,
                referencePropertyName,
                bindingPurpose);
        }
        else
        {
            binding = DocumentBindingEntity.CreateWithGuidReference(
                documentId,
                departmentId,
                moduleId,
                referenceTableName,
                referenceTableIdGuid!.Value,
                referencePropertyName,
                bindingPurpose);
        }

        if (isPrimaryDocument)
        {
            binding.MarkAsPrimary();
        }

        if (authDepartmentId.HasValue && authReferenceId.HasValue)
        {
            binding.SetAuthorizationContext(authDepartmentId.Value, authReferenceId.Value);
        }

        binding.CreatedBy = createdBy;
        binding.CreatedDate = DateTime.Now;
        binding.IsActive = true;

        await _bindingRepository.AddAsync(binding, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return binding.Id;
    }

    public async Task<DocumentEntity?> GetDocumentByGuidAsync(Guid documentGuid, CancellationToken cancellationToken = default)
    {
        var documents = await _documentRepository.GetAsync(
            d => d.DocumentGuid == documentGuid && d.IsActive && !d.MarkedForDeletion,
            cancellationToken);
        return documents.FirstOrDefault();
    }

    public async Task<DocumentEntity?> GetDocumentByIdAsync(int documentId, CancellationToken cancellationToken = default)
    {
        return await _documentRepository.GetByIdAsync(documentId, cancellationToken);
    }

    public async Task<bool> DeleteDocumentAsync(Guid documentGuid, int deletedBy, CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentByGuidAsync(documentGuid, cancellationToken);
        if (document == null)
            return false;

        document.MarkForDeletion(deletedBy);
        document.UpdatedBy = deletedBy;
        document.UpdatedDate = DateTime.Now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task IncrementDownloadCountAsync(Guid documentGuid, int userId, CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentByGuidAsync(documentGuid, cancellationToken);
        if (document != null)
        {
            document.RecordDownload(userId);
            document.UpdatedBy = userId;
            document.UpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<DocumentEntity>> GetDocumentsByReferenceAsync(
        string referenceTableName,
        int? referenceTableId,
        Guid? referenceTableIdGuid,
        CancellationToken cancellationToken = default)
    {
        // Validate that referenceTableName is provided
        if (string.IsNullOrWhiteSpace(referenceTableName))
        {
            throw new InvalidBindingException("Reference table name is required.", null, referenceTableName);
        }

        // Validate XOR semantics: exactly one of referenceTableId or referenceTableIdGuid must be provided
        // This matches the domain constraint enforced in CreateDocumentBindingAsync
        var (hasReferenceTableId, hasReferenceTableIdGuid) = ValidateReferenceIdXor(referenceTableId, referenceTableIdGuid);

        var query = _bindingRepository.GetQueryable()
            .Include(db => db.Document)
            .Where(db => db.ReferenceTableName == referenceTableName && db.IsActive)
            .Where(db => db.Document != null && db.Document.IsActive && !db.Document.MarkedForDeletion);

        // Apply the appropriate filter based on which reference ID type is provided
        if (hasReferenceTableId)
        {
            query = query.Where(db => db.ReferenceTableId == referenceTableId!.Value);
        }
        else // hasReferenceTableIdGuid is true due to XOR validation
        {
            query = query.Where(db => db.ReferenceTableIdGuid == referenceTableIdGuid!.Value);
        }

        var orderedBindings = await query
            .OrderByDescending(db => db.IsPrimaryDocument)
            .ThenByDescending(db => db.CreatedDate)
            .Select(db => new
            {
                db.DocumentId,
                Document = db.Document!
            })
            .ToListAsync(cancellationToken);

        var documents = orderedBindings
            .GroupBy(db => db.DocumentId)
            .Select(group => group.First().Document)
            .ToList();

        return documents;
    }

    /// <summary>
    /// Validates XOR semantics for reference IDs: exactly one of referenceTableId or referenceTableIdGuid must be provided.
    /// </summary>
    /// <param name="referenceTableId">Optional integer reference ID</param>
    /// <param name="referenceTableIdGuid">Optional GUID reference ID</param>
    /// <returns>Tuple indicating which reference type is present</returns>
    /// <exception cref="ArgumentException">Thrown when neither or both reference IDs are provided</exception>
    private static (bool hasReferenceTableId, bool hasReferenceTableIdGuid) ValidateReferenceIdXor(
        int? referenceTableId,
        Guid? referenceTableIdGuid)
    {
        var hasReferenceTableId = referenceTableId.HasValue && referenceTableId.Value > 0;
        var hasReferenceTableIdGuid = referenceTableIdGuid.HasValue && referenceTableIdGuid.Value != Guid.Empty;

        if (!hasReferenceTableId && !hasReferenceTableIdGuid)
        {
            throw new XorValidationException(nameof(referenceTableId), nameof(referenceTableIdGuid));
        }

        if (hasReferenceTableId && hasReferenceTableIdGuid)
        {
            throw new XorValidationException(nameof(referenceTableId), nameof(referenceTableIdGuid));
        }

        return (hasReferenceTableId, hasReferenceTableIdGuid);
    }

    public async Task UpdateDocumentBindingReferenceAsync(
        int documentBindingId,
        int referenceTableId,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        if (referenceTableId <= 0)
            throw new ArgumentException("Reference table ID must be greater than zero.", nameof(referenceTableId));

        var binding = await _bindingRepository.GetByIdAsync(documentBindingId, cancellationToken);

        if (binding != null)
        {
                throw new InvalidOperationException(
                    $"Cannot update binding {documentBindingId} with int reference: binding is GUID-based.");

            binding.UpdatedDate = DateTime.UtcNow;
        }
    }

    public async Task<DocumentBindingEntity?> GetDocumentBindingByIdAsync(
        int documentBindingId,
        CancellationToken cancellationToken = default)
    {
        return await _bindingRepository.GetByIdAsync(documentBindingId, cancellationToken);
    }

    public async Task<DocumentEntity?> GetDocumentByBindingIdAsync(
        int documentBindingId,
        CancellationToken cancellationToken = default)
    {
        var binding = await _bindingRepository.GetByIdAsync(documentBindingId, cancellationToken);
        if (binding == null)
            return null;

        return await _documentRepository.GetByIdAsync(binding.DocumentId, cancellationToken);
    }

    public async Task<List<DocumentEntity>> GetDocumentsByDepartmentModuleReferenceAsync(
        int departmentId,
        int moduleId,
        string referenceTableName,
        int? referenceTableId,
        Guid? referenceTableIdGuid,
        CancellationToken cancellationToken = default)
    {
        var bindings = await _bindingRepository.GetAsync(
            b => b.DepartmentId == departmentId &&
                 b.ModuleId == moduleId &&
                 b.ReferenceTableName == referenceTableName &&
                 ((referenceTableId.HasValue && b.ReferenceTableId == referenceTableId && !b.ReferenceTableIdGuid.HasValue) ||
                  (referenceTableIdGuid.HasValue && b.ReferenceTableIdGuid == referenceTableIdGuid && !b.ReferenceTableId.HasValue)) &&
                 b.IsActive &&
                 !b.MarkedForDeletion,
            cancellationToken);

        if (!bindings.Any())
            return new List<DocumentEntity>();

        var orderedBindings = bindings
            .OrderByDescending(b => b.IsPrimaryDocument)
            .ThenByDescending(b => b.CreatedDate)
            .ToList();

        var documentIds = orderedBindings.Select(b => b.DocumentId).Distinct().ToList();
        var documents = await _documentRepository.GetAsync(
            d => documentIds.Contains(d.Id) && d.IsActive && !d.MarkedForDeletion,
            cancellationToken);

        var documentsById = documents.ToDictionary(d => d.Id);
        return documentIds.Where(id => documentsById.ContainsKey(id)).Select(id => documentsById[id]).ToList();
    }

    public async Task<List<DocumentEntity>> GetSoftDeletedDocumentsAsync(
        DateTime cutoffDate,
        CancellationToken cancellationToken = default)
    {
        var docs = await _documentRepository.GetAsync(
            d => d.MarkedForDeletion &&
                 d.MarkedForDeletionDate != null &&
                 d.MarkedForDeletionDate < cutoffDate,
            cancellationToken);
        return docs.ToList();
    }

    public async Task HardDeleteDocumentAsync(
        int documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document == null)
            return;

        // Hard delete the document record
        await _documentRepository.DeleteAsync(document, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
