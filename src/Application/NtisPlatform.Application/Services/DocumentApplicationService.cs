using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NtisPlatform.Application.Common;
using NtisPlatform.Application.DTOs.Document;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Options;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Global application service for ALL document operations across every module.
/// Routes Upload, View, Download, Retrieve, and Delete through a single pipeline.
///
/// <para>
/// Entity-specific side-effects (e.g. linking back a <c>DocumentBindingId</c> to a
/// <c>PropertyPhoto</c> or <c>PropertyCertificate</c> row) are delegated to registered
/// <see cref="IDocumentBindingHandler"/> implementations via the Open/Closed Principle:
/// no <c>if/else</c> branching on entity names exists in this class.
/// </para>
/// </summary>
public class DocumentApplicationService : IDocumentApplicationService
{
    private readonly IDocumentService _documentService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DocumentApplicationService> _logger;
    private readonly int _bufferSizeBytes;
    private readonly long _maxFileSizeBytes;
    private readonly IRepository<DocumentBindingEntity, int> _bindingRepository;
    private readonly IReadOnlyDictionary<string, IDocumentBindingHandler> _bindingHandlers;

    public DocumentApplicationService(
        IDocumentService documentService,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork,
        IOptions<FileStorageOptions> fileStorageOptions,
        ILogger<DocumentApplicationService> logger,
        IRepository<DocumentBindingEntity, int> bindingRepository,
        IEnumerable<IDocumentBindingHandler> bindingHandlers)
    {
        _documentService = documentService;
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _bindingRepository = bindingRepository;

        // Build a dispatch dictionary from all registered handlers.
        // Each handler declares which table name(s) it is responsible for via Handles().
        // Dictionary key = canonical table name (from ReferenceTableName property).
        // Lookup at runtime uses handler.Handles(name) for alias support.
        _bindingHandlers = bindingHandlers
            .ToDictionary(h => h.ReferenceTableName, h => h, StringComparer.OrdinalIgnoreCase);

        var fileStorage = fileStorageOptions.Value;
        _bufferSizeBytes = fileStorage.BufferSizeBytes;
        _maxFileSizeBytes = fileStorage.MaxFileSizeBytes;
    }

    // ── Upload ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<DocumentUploadResponseDto> UploadDocumentAsync(
        Stream fileStream,
        string originalFileName,
        string mimeType,
        long fileSizeBytes,
        DocumentUploadDto uploadDto,
        int uploadedBy,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstInvalidStream(fileStream, nameof(fileStream));
        Guard.AgainstNullOrWhiteSpace(originalFileName, nameof(originalFileName));
        Guard.AgainstExceedingLength(originalFileName, 255, nameof(originalFileName));
        Guard.AgainstNullOrWhiteSpace(mimeType, nameof(mimeType));
        Guard.AgainstNegativeOrZero(fileSizeBytes, nameof(fileSizeBytes));
        Guard.AgainstOutOfRange(fileSizeBytes, 1, _maxFileSizeBytes, nameof(fileSizeBytes));
        Guard.AgainstNull(uploadDto, nameof(uploadDto));
        Guard.AgainstNegativeOrZero(uploadedBy, nameof(uploadedBy));

        _logger.LogInformation(
            "Starting document upload: {FileName}, Size: {FileSize} bytes, User: {UserId}",
            originalFileName, fileSizeBytes, uploadedBy);

        var fileExtension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var tempFilePath = Path.GetTempFileName();

        try
        {
            // Single-pass: buffer to temp file + compute SHA-256 checksum simultaneously.
            string checksumSha256;
            await using (var tempFileStream = new FileStream(
                tempFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                _bufferSizeBytes,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                using var sha256 = SHA256.Create();
                var buffer = new byte[_bufferSizeBytes];
                int bytesRead;

                while ((bytesRead = await fileStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
                {
                    await tempFileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                }

                sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                checksumSha256 = Convert.ToHexString(sha256.Hash!).ToLowerInvariant();
                _logger.LogDebug("Computed SHA-256: {Checksum} for file: {FileName}", checksumSha256, originalFileName);
            }

            // Save from temp location to final storage.
            await using (var tempFileStream = new FileStream(
                tempFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                _bufferSizeBytes,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var storagePath = await _fileStorageService.SaveFileAsync(tempFileStream, originalFileName, cancellationToken);
                _logger.LogInformation("File saved to storage: {StoragePath}", storagePath);

                try
                {
                    await _unitOfWork.BeginTransactionAsync(cancellationToken);
                    try
                    {
                        // 1. Persist Document record.
                        var (documentId, documentGuid) = await _documentService.CreateDocumentAsync(
                            uploadedBy,
                            uploadDto.OwnerUserId,
                            Path.GetFileName(storagePath),
                            originalFileName,
                            fileExtension,
                            mimeType,
                            fileSizeBytes,
                            storagePath,
                            null,
                            checksumSha256,
                            uploadDto.DocumentType ?? DocumentType.Certificate.ToTypeString(),
                            cancellationToken);

                        _logger.LogInformation(
                            "Document created: DocumentId={DocumentId}, DocumentGuid={DocumentGuid}",
                            documentId, documentGuid);

                        // 2. Create DocumentBinding if sufficient context was provided.
                        int? bindingId = null;
                        if (ShouldCreateBinding(uploadDto))
                        {
                            bindingId = await _documentService.CreateDocumentBindingAsync(
                                documentId,
                                uploadDto.DepartmentId!.Value,
                                uploadDto.ModuleId!.Value,
                                uploadDto.ReferenceTableName!,
                                uploadDto.ReferenceTableId,
                                uploadDto.ReferenceTableIdGuid,
                                uploadDto.ReferencePropertyName ?? "Id",
                                uploadDto.BindingPurpose,
                                uploadDto.IsPrimaryDocument,
                                uploadDto.AuthDepartmentId,
                                uploadDto.AuthReferenceId,
                                uploadedBy,
                                cancellationToken);

                            _logger.LogInformation("DocumentBinding created: BindingId={BindingId}", bindingId);

                            // 3. Delegate any entity-specific post-processing to the matching handler.
                            //    No if/else here — the handler registry is the extension point.
                            if (bindingId.HasValue
                                && uploadDto.ReferenceTableId.HasValue
                                && uploadDto.ReferenceTableId.Value > 0)
                            {
                                var handler = FindHandler(uploadDto.ReferenceTableName!);
                                if (handler != null)
                                {
                                    await handler.OnAfterUploadAsync(
                                        documentId,
                                        bindingId.Value,
                                        uploadDto.ReferenceTableId.Value,
                                        uploadedBy,
                                        cancellationToken);
                                }
                            }
                        }

                        await _unitOfWork.CommitTransactionAsync(cancellationToken);

                        _logger.LogInformation("Document upload completed: {DocumentGuid}", documentGuid);

                        return new DocumentUploadResponseDto
                        {
                            DocumentGuid = documentGuid,
                            DocumentId = documentId,
                            DocumentBindingId = bindingId,
                            FileName = originalFileName,
                            FileSizeBytes = fileSizeBytes,
                            StoragePath = storagePath
                        };
                    }
                    catch
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        throw;
                    }
                }
                catch
                {
                    // Clean up the orphaned storage file if the DB transaction failed.
                    try
                    {
                        await _fileStorageService.DeleteFileAsync(storagePath, cancellationToken);
                        _logger.LogWarning("Deleted orphaned file during upload rollback: {StoragePath}", storagePath);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogWarning(cleanupEx,
                            "Failed to delete stored file during upload rollback: {StoragePath}. Manual cleanup may be required.",
                            storagePath);
                    }
                    throw;
                }
            }
        }
        finally
        {
            // Always clean up the temp file.
            if (File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                    _logger.LogDebug("Deleted temporary file: {TempFilePath}", tempFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to delete temporary file: {TempFilePath}. Manual cleanup may be required.",
                        tempFilePath);
                }
            }
        }
    }

    // ── Read operations ────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<DocumentDto?> GetDocumentAsync(Guid documentGuid, CancellationToken cancellationToken = default)
    {
        Guard.AgainstEmptyGuid(documentGuid, nameof(documentGuid));

        var document = await _documentService.GetDocumentByGuidAsync(documentGuid, cancellationToken);
        if (document == null)
        {
            _logger.LogWarning("Document not found: {DocumentGuid}", documentGuid);
            return null;
        }

        return MapToDocumentDto(document);
    }

    /// <inheritdoc/>
    public async Task<(Stream? FileStream, string FileName, string MimeType)> DownloadDocumentAsync(
        Guid documentGuid,
        int userId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstEmptyGuid(documentGuid, nameof(documentGuid));
        Guard.AgainstNegativeOrZero(userId, nameof(userId));

        var document = await _documentService.GetDocumentByGuidAsync(documentGuid, cancellationToken);
        if (document == null)
        {
            _logger.LogWarning("Download failed — document not found: {DocumentGuid}", documentGuid);
            return (null, string.Empty, string.Empty);
        }

        var fileStream = await _fileStorageService.ReadFileAsync(document.StoragePath, cancellationToken);
        if (fileStream != null)
        {
            await _documentService.IncrementDownloadCountAsync(documentGuid, userId, cancellationToken);
            _logger.LogInformation("Document downloaded: {DocumentGuid} by user {UserId}", documentGuid, userId);
        }
        else
        {
            _logger.LogWarning("File not found in storage: {StoragePath} for document {DocumentGuid}",
                document.StoragePath, documentGuid);
        }

        return (fileStream, document.OriginalFileName, document.MimeType);
    }

    /// <inheritdoc/>
    public async Task<(Stream? FileStream, string FileName, string MimeType)> ViewDocumentAsync(
        Guid documentGuid,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstEmptyGuid(documentGuid, nameof(documentGuid));

        var document = await _documentService.GetDocumentByGuidAsync(documentGuid, cancellationToken);
        if (document == null)
        {
            _logger.LogWarning("View failed — document not found: {DocumentGuid}", documentGuid);
            return (null, string.Empty, string.Empty);
        }

        var fileStream = await _fileStorageService.ReadFileAsync(document.StoragePath, cancellationToken);
        if (fileStream == null)
        {
            _logger.LogWarning("File not found in storage for view: {StoragePath}, document {DocumentGuid}",
                document.StoragePath, documentGuid);
        }

        return (fileStream, document.OriginalFileName, document.MimeType);
    }

    // ── Delete ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<bool> DeleteDocumentAsync(
        Guid documentGuid,
        int deletedBy,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstEmptyGuid(documentGuid, nameof(documentGuid));
        Guard.AgainstNegativeOrZero(deletedBy, nameof(deletedBy));

        var document = await _documentService.GetDocumentByGuidAsync(documentGuid, cancellationToken);
        if (document != null)
        {
            // Notify each matching handler so it can perform entity-specific cleanup
            // (e.g. unlinking the binding from a PropertyCertificate or soft-deleting a PropertyPhoto).
            // No if/else on entity names — handlers are the extension point.
            var activeBindings = await _bindingRepository.GetAsync(
                b => b.DocumentId == document.Id && b.IsActive,
                cancellationToken);

            foreach (var binding in activeBindings)
            {
                if (string.IsNullOrWhiteSpace(binding.ReferenceTableName))
                    continue;

                var handler = FindHandler(binding.ReferenceTableName);
                if (handler != null)
                {
                    await handler.OnBeforeDeleteAsync(binding, deletedBy, cancellationToken);
                }
            }
        }

        var result = await _documentService.DeleteDocumentAsync(documentGuid, deletedBy, cancellationToken);
        if (result)
            _logger.LogInformation("Document deleted: {DocumentGuid} by user {UserId}", documentGuid, deletedBy);
        else
            _logger.LogWarning("Delete failed — document not found: {DocumentGuid}", documentGuid);

        return result;
    }

    // ── Binding operations ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task UpdateDocumentBindingReferenceAsync(
        int documentBindingId,
        int referenceTableId,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(documentBindingId, nameof(documentBindingId));
        Guard.AgainstNegativeOrZero(referenceTableId, nameof(referenceTableId));
        Guard.AgainstNegativeOrZero(updatedBy, nameof(updatedBy));

        await _documentService.UpdateDocumentBindingReferenceAsync(
            documentBindingId, referenceTableId, updatedBy, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeactivateDocumentBindingAsync(
        int documentBindingId,
        int updatedBy,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(documentBindingId, nameof(documentBindingId));
        Guard.AgainstNegativeOrZero(updatedBy, nameof(updatedBy));

        var binding = await _bindingRepository.GetByIdAsync(documentBindingId, cancellationToken);
        if (binding == null)
        {
            _logger.LogWarning("DeactivateDocumentBinding: binding {BindingId} not found", documentBindingId);
            return;
        }

        binding.MarkForDeletion();
        binding.UpdatedBy = updatedBy;
        binding.UpdatedDate = DateTime.UtcNow;

        await _bindingRepository.UpdateAsync(binding, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DocumentDto?> GetDocumentByBindingAsync(
        int documentBindingId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(documentBindingId, nameof(documentBindingId));

        var binding = await _bindingRepository.GetQueryable()
            .Include(b => b.Document)
            .FirstOrDefaultAsync(b => b.Id == documentBindingId
                && b.IsActive
                && !b.MarkedForDeletion
                && b.Document != null
                && b.Document.IsActive
                && !b.Document.MarkedForDeletion,
                cancellationToken);

        return binding?.Document == null ? null : MapToDocumentDto(binding.Document);
    }

    /// <inheritdoc/>
    public async Task<DocumentDto?> GetDocumentByReferenceAsync(
        int departmentId,
        int moduleId,
        string referenceTableName,
        int referenceTableId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(referenceTableName))
            throw new ArgumentException("Reference table name cannot be empty.", nameof(referenceTableName));

        if (referenceTableId <= 0)
            throw new ArgumentException("Reference table ID must be greater than zero.", nameof(referenceTableId));

        var canonicalTableName = FindHandler(referenceTableName)?.ReferenceTableName ?? referenceTableName;

        var documents = await _documentService.GetDocumentsByDepartmentModuleReferenceAsync(
            departmentId, moduleId, canonicalTableName, referenceTableId, null, cancellationToken);

        return documents.Count == 0 ? null : MapToDocumentDto(documents.First());
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DocumentBindingInfoDto>> GetDocumentsByReferenceTableAsync(
        string referenceTableName,
        IReadOnlyList<int> referenceTableIds,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(referenceTableName))
            throw new ArgumentException("Reference table name cannot be empty.", nameof(referenceTableName));

        if (referenceTableIds == null || referenceTableIds.Count == 0)
            return Array.Empty<DocumentBindingInfoDto>();

        var bindings = await _bindingRepository.GetQueryable()
            .Include(db => db.Document)
            .Where(db => db.ReferenceTableName == referenceTableName
                      && db.ReferenceTableId.HasValue
                      && referenceTableIds.Contains(db.ReferenceTableId.Value)
                      && db.IsActive
                      && !db.MarkedForDeletion
                      && db.Document != null
                      && db.Document.IsActive
                      && !db.Document.MarkedForDeletion)
            .OrderBy(db => db.Id)
            .Select(db => new DocumentBindingInfoDto
            {
                ReferenceTableId = db.ReferenceTableId!.Value,
                BindingId = db.Id,
                DocumentGuid = db.Document!.DocumentGuid,
                BindingPurpose = db.BindingPurpose,
                OriginalFileName = db.Document.OriginalFileName,
                MimeType = db.Document.MimeType
            })
            .ToListAsync(cancellationToken);

        return bindings;
    }

    // ── Metadata ───────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<DocumentMetadataDto?> GetDocumentMetadataAsync(
        Guid documentGuid,
        CancellationToken cancellationToken = default)
    {
        var document = await _documentService.GetDocumentByGuidAsync(documentGuid, cancellationToken);
        return document == null ? null : MapToDocumentMetadataDto(document);
    }

    // ── Private helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Finds a registered <see cref="IDocumentBindingHandler"/> for the given reference table name.
    /// Returns <c>null</c> if no handler is registered — this is valid for generic/unbound uploads.
    /// </summary>
    private IDocumentBindingHandler? FindHandler(string referenceTableName)
    {
        // Fast path: exact canonical match
        if (_bindingHandlers.TryGetValue(referenceTableName, out var handler))
            return handler;

        // Slow path: alias match (e.g. "PropertyPhotos" → PropertyPhotoDocumentBindingHandler)
        return _bindingHandlers.Values.FirstOrDefault(h => h.Handles(referenceTableName));
    }

    /// <summary>
    /// Determines whether a <see cref="DocumentBindingEntity"/> should be created from the upload DTO.
    /// Returns <c>false</c> if binding context is incomplete or the reference ID format is invalid.
    /// </summary>
    private static bool ShouldCreateBinding(DocumentUploadDto dto)
    {
        if (!dto.DepartmentId.HasValue || dto.DepartmentId.Value <= 0)
            return false;

        if (!dto.ModuleId.HasValue || dto.ModuleId.Value <= 0)
            return false;

        if (string.IsNullOrWhiteSpace(dto.ReferenceTableName))
            return false;

        // ReferenceTableName: starts with letter, alphanumeric, 2-100 chars
        if (!System.Text.RegularExpressions.Regex.IsMatch(
                dto.ReferenceTableName, @"^[A-Za-z][A-Za-z0-9]{1,99}$"))
            return false;

        // XOR: exactly one of int or GUID reference must be provided
        var hasIntId = dto.ReferenceTableId.HasValue && dto.ReferenceTableId.Value > 0;
        var hasGuidId = dto.ReferenceTableIdGuid.HasValue && dto.ReferenceTableIdGuid.Value != Guid.Empty;
        return hasIntId ^ hasGuidId;
    }

    private static DocumentDto MapToDocumentDto(DocumentEntity document) => new()
    {
        Id = document.Id,
        DocumentGuid = document.DocumentGuid,
        UploadedByUserId = document.UploadedByUserId,
        FileName = document.FileName,
        OriginalFileName = document.OriginalFileName,
        FileExtension = document.FileExtension,
        MimeType = document.MimeType,
        FileSizeBytes = document.FileSizeBytes,
        StorageProvider = document.StorageProvider,
        StoragePath = document.StoragePath,
        DocumentType = document.DocumentType,
        DocumentCategory = document.DocumentCategory,
        Description = document.Description,
        UploadStatusCode = document.UploadStatusCode,
        ScanStatusCode = document.ScanStatusCode,
        DownloadCount = document.DownloadCount,
        CreatedDate = document.CreatedDate,
        IsActive = document.IsActive
    };

    private static DocumentMetadataDto MapToDocumentMetadataDto(DocumentEntity document) => new()
    {
        DocumentGuid = document.DocumentGuid,
        DocumentTitle = document.DocumentTitle,
        Description = document.Description,
        DocumentType = document.DocumentType,
        DocumentCategory = document.DocumentCategory,
        MimeType = document.MimeType,
        FileSizeBytes = document.FileSizeBytes,
        OriginalFileName = document.OriginalFileName,
        FileExtension = document.FileExtension,
        UploadedByUserId = document.UploadedByUserId,
        CreatedDate = document.CreatedDate,
        ChecksumSha256 = document.ChecksumSha256,
        UploadStatusCode = document.UploadStatusCode,
        ScanStatusCode = document.ScanStatusCode,
        DocumentBindingIds = document.DocumentBindings?.Select(b => b.Id).ToList() ?? new List<int>(),
        MarkedForDeletion = document.MarkedForDeletion,
        MarkedForDeletionDate = document.MarkedForDeletionDate,
        DownloadCount = document.DownloadCount
    };
}
