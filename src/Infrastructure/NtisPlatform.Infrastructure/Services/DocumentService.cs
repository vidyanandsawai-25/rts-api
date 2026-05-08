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

        if (ownerUserId.HasValue && ownerUserId.Value != uploadedByUserId)
        {
            document.TransferOwnership(ownerUserId.Value);
        }

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
        string moduleCode,
        string referenceTableName,
        int? referenceTableId,
        Guid? referenceTableIdGuid,
        string? bindingPurpose,
        bool isPrimaryDocument,
        string? authModuleCode,
        int? authReferenceId,
        int createdBy,
        CancellationToken cancellationToken = default)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(moduleCode))
        {
            throw new InvalidBindingException("Module code is required.", moduleCode);
        }

        if (string.IsNullOrWhiteSpace(referenceTableName))
        {
            throw new InvalidBindingException("Reference table name is required.", null, referenceTableName);
        }

        // Validate XOR semantics: exactly one of referenceTableId or referenceTableIdGuid must be provided
        var (hasReferenceTableId, hasReferenceTableIdGuid) = ValidateReferenceIdXor(referenceTableId, referenceTableIdGuid);

        DocumentBindingEntity binding;

        if (hasReferenceTableId)
        {
            binding = DocumentBindingEntity.CreateWithIntReference(
                documentId,
                moduleCode,
                referenceTableName,
                referenceTableId!.Value,
                bindingPurpose);
        }
        else
        {
            binding = DocumentBindingEntity.CreateWithGuidReference(
                documentId,
                moduleCode,
                referenceTableName,
                referenceTableIdGuid!.Value,
                bindingPurpose);
        }

        if (isPrimaryDocument)
        {
            binding.MarkAsPrimary();
        }

        if (!string.IsNullOrWhiteSpace(authModuleCode) && authReferenceId.HasValue)
        {
            binding.SetAuthorizationContext(authModuleCode, authReferenceId.Value);
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
            .ThenBy(db => db.DisplayOrder)
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
        var binding = await _bindingRepository.GetByIdAsync(documentBindingId, cancellationToken);

        if (binding != null)
        {
            binding.UpdateReferenceTableId(referenceTableId);
            binding.UpdatedBy = updatedBy;
            binding.UpdatedDate = DateTime.Now;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
