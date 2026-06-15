using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NtisPlatform.Application.Common;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.DTOs.PropertyDiscount;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Enums;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Services;

/// <summary>
/// Service for handling document uploads for discount-related PropertySocialDetails
/// </summary>
public class PropertyDiscountDocumentService : IPropertyDiscountDocumentService
{
    private readonly IDocumentService _documentService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ApplicationDbContext _context;
    private readonly IRepository<DepartmentMasterEntity, int> _departmentRepository;
    private readonly IRepository<ModuleMasterEntity, int> _moduleRepository;
    private readonly ILogger<PropertyDiscountDocumentService> _logger;
    private readonly int _bufferSizeBytes;
    private readonly long _maxFileSizeBytes;

    public PropertyDiscountDocumentService(
        IDocumentService documentService,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork,
        ApplicationDbContext context,
        IRepository<DepartmentMasterEntity, int> departmentRepository,
        IRepository<ModuleMasterEntity, int> moduleRepository,
        IOptions<FileStorageOptions> fileStorageOptions,
        ILogger<PropertyDiscountDocumentService> logger)
    {
        _documentService = documentService;
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
        _context = context;
        _departmentRepository = departmentRepository;
        _moduleRepository = moduleRepository;
        _logger = logger;

        var fileStorage = fileStorageOptions.Value;
        _bufferSizeBytes = fileStorage.BufferSizeBytes;
        _maxFileSizeBytes = fileStorage.MaxFileSizeBytes;
    }

    public async Task<DiscountDocumentUploadResponseDto> UploadDiscountDocumentAsync(
        Stream fileStream,
        string originalFileName,
        string mimeType,
        long fileSizeBytes,
        int propertyId,
        int socialAttributeId,
        string? remark,
        int uploadedBy,
        bool isPhoto = false,
        CancellationToken cancellationToken = default)
    {
        // Input validation
        Guard.AgainstInvalidStream(fileStream, nameof(fileStream));
        Guard.AgainstNullOrWhiteSpace(originalFileName, nameof(originalFileName));
        Guard.AgainstExceedingLength(originalFileName, 255, nameof(originalFileName));
        Guard.AgainstNullOrWhiteSpace(mimeType, nameof(mimeType));
        Guard.AgainstNegativeOrZero(fileSizeBytes, nameof(fileSizeBytes));
        Guard.AgainstOutOfRange(fileSizeBytes, 1, _maxFileSizeBytes, nameof(fileSizeBytes));
        Guard.AgainstNegativeOrZero(propertyId, nameof(propertyId));
        Guard.AgainstNegativeOrZero(socialAttributeId, nameof(socialAttributeId));
        Guard.AgainstNegativeOrZero(uploadedBy, nameof(uploadedBy));

        var propertyExists = await _context.PropertyMast.AnyAsync(
            p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion,
            cancellationToken);
        if (!propertyExists)
            throw new ArgumentException($"Property with ID {propertyId} not found", nameof(propertyId));

        var attributeExists = await _context.Set<SocialAttributeEntity>().AnyAsync(
            sa => sa.Id == socialAttributeId && sa.IsActive && sa.IsDiscountApplicable,
            cancellationToken);
        if (!attributeExists)
            throw new ArgumentException($"SocialAttributeId {socialAttributeId} is not a valid discount-applicable attribute", nameof(socialAttributeId));

        _logger.LogInformation("Starting discount document upload: {FileName}, PropertyId: {PropertyId}, SocialAttributeId: {SocialAttributeId}, IsPhoto: {IsPhoto}",
            originalFileName, propertyId, socialAttributeId, isPhoto);

        var fileExtension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var tempFilePath = Path.GetTempFileName();
        string? storagePath = null;

        try
        {
            // 1. Buffer and compute checksum
            await using var tempFileStream = new FileStream(
                tempFilePath,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None,
                _bufferSizeBytes,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            using var sha256 = SHA256.Create();
            var buffer = new byte[_bufferSizeBytes];
            int bytesRead;

            while ((bytesRead = await fileStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                await tempFileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
            }

            sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            var checksumSha256 = Convert.ToHexString(sha256.Hash!).ToLowerInvariant();

            // 2. Save to storage
            tempFileStream.Position = 0;
            storagePath = await _fileStorageService.SaveFileAsync(tempFileStream, originalFileName, cancellationToken);
            _logger.LogInformation("Discount document saved to storage: {StoragePath}", storagePath);

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                // 3. Create CORE.Document
                var result = await _documentService.CreateDocumentAsync(
                    uploadedBy,
                    null,
                    Path.GetFileName(storagePath),
                    originalFileName,
                    fileExtension,
                    mimeType,
                    fileSizeBytes,
                    storagePath,
                    null,
                    checksumSha256,
                    DocumentType.Proof.ToTypeString(),
                    cancellationToken);
                var documentId = result.DocumentId;
                var documentGuid = result.DocumentGuid;

                // 4. Find or create PropertySocialDetails record
                var propertySocialDetail = await _context.Set<PropertySocialDetailsEntity>()
                    .FirstOrDefaultAsync(x => x.PropertyId == propertyId && x.SocialAttributeId == socialAttributeId && x.IsActive, cancellationToken);

                if (propertySocialDetail == null)
                {
                    // Create new record
                    propertySocialDetail = new PropertySocialDetailsEntity
                    {
                        PropertyId = propertyId,
                        SocialAttributeId = socialAttributeId,
                        Remark = remark,
                        CreatedBy = uploadedBy,
                        CreatedDate = DateTime.Now,
                        IsActive = true
                    };
                    _context.Set<PropertySocialDetailsEntity>().Add(propertySocialDetail);
                    await _context.SaveChangesAsync(cancellationToken);
                }

                // 5. Get DepartmentId and ModuleId
                var (departmentId, moduleId) = await GetDepartmentAndModuleIdsAsync(cancellationToken);

                // 6. Create CORE.DocumentBinding
                var documentBindingId = await _documentService.CreateDocumentBindingAsync(
                    documentId,
                    departmentId,
                    moduleId,
                    "PropertySocialDetails",
                    propertySocialDetail.Id,
                    null,
                    "Id",
                    isPhoto ? DocumentBindingPurpose.Photo.ToPurposeString() : DocumentBindingPurpose.SupportingDocument.ToPurposeString(),
                    false,
                    departmentId,
                    uploadedBy,
                    uploadedBy,
                    cancellationToken);

                // 7. Update PropertySocialDetails with DocumentBinding (only if not photo)
                if (!isPhoto)
                {
                    propertySocialDetail.DocumentBindingId = documentBindingId;
                }
                if (!string.IsNullOrEmpty(remark))
                {
                    propertySocialDetail.Remark = remark;
                }
                propertySocialDetail.UpdatedBy = uploadedBy;
                propertySocialDetail.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync(cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return new DiscountDocumentUploadResponseDto
                {
                    PropertySocialDetailId = propertySocialDetail.Id,
                    PropertyId = propertyId,
                    SocialAttributeId = socialAttributeId,
                    DocumentBindingId = documentBindingId,
                    DocumentGuid = documentGuid,
                    FileName = originalFileName,
                    Remark = remark
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                if (storagePath != null)
                {
                    try
                    {
                        await _fileStorageService.DeleteFileAsync(storagePath, cancellationToken);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogWarning(cleanupEx, "Failed to delete stored file during upload rollback: {StoragePath}", storagePath);
                    }
                }
                throw;
            }
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                    _logger.LogDebug("Deleted temporary file: {TempFilePath}", tempFilePath);
                }
                catch (Exception ex)
                {
                    // Don't fail the upload if temp-file cleanup fails
                    _logger.LogWarning(ex,
                        "Failed to delete temporary file: {TempFilePath}. File may need manual cleanup.",
                        tempFilePath);
                }
            }
        }
    }

    public async Task<DiscountDocumentUploadResponseDto> ReplaceDiscountDocumentAsync(
        int propertySocialDetailId,
        Stream fileStream,
        string originalFileName,
        string mimeType,
        long fileSizeBytes,
        string? remark,
        int uploadedBy,
        bool isPhoto = false,
        CancellationToken cancellationToken = default)
    {
        // Input validation
        Guard.AgainstInvalidStream(fileStream, nameof(fileStream));
        Guard.AgainstNullOrWhiteSpace(originalFileName, nameof(originalFileName));
        Guard.AgainstExceedingLength(originalFileName, 255, nameof(originalFileName));
        Guard.AgainstNullOrWhiteSpace(mimeType, nameof(mimeType));
        Guard.AgainstNegativeOrZero(fileSizeBytes, nameof(fileSizeBytes));
        Guard.AgainstOutOfRange(fileSizeBytes, 1, _maxFileSizeBytes, nameof(fileSizeBytes));
        Guard.AgainstNegativeOrZero(propertySocialDetailId, nameof(propertySocialDetailId));
        Guard.AgainstNegativeOrZero(uploadedBy, nameof(uploadedBy));

        _logger.LogInformation("Replacing discount document for PropertySocialDetailId: {Id}, IsPhoto: {IsPhoto}", propertySocialDetailId, isPhoto);

        // Get existing record
        var propertySocialDetail = await _context.Set<PropertySocialDetailsEntity>()
            .FirstOrDefaultAsync(x => x.Id == propertySocialDetailId && x.IsActive, cancellationToken);

        if (propertySocialDetail == null)
        {
            throw new InvalidOperationException($"PropertySocialDetails with ID {propertySocialDetailId} not found");
        }

        var isDiscountApplicable = await _context.Set<SocialAttributeEntity>().AnyAsync(
            sa => sa.Id == propertySocialDetail.SocialAttributeId && sa.IsActive && sa.IsDiscountApplicable,
            cancellationToken);

        if (!isDiscountApplicable)
        {
            throw new ArgumentException(
                $"PropertySocialDetails with ID {propertySocialDetailId} is not linked to a discount-applicable SocialAttribute.",
                nameof(propertySocialDetailId));
        }
        var fileExtension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var tempFilePath = Path.GetTempFileName();
        string? storagePath = null;

        try
        {
            // 1. Buffer and compute checksum
            await using var tempFileStream = new FileStream(
                tempFilePath,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None,
                _bufferSizeBytes,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            using var sha256 = SHA256.Create();
            var buffer = new byte[_bufferSizeBytes];
            int bytesRead;

            while ((bytesRead = await fileStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                await tempFileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
            }

            sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            var checksumSha256 = Convert.ToHexString(sha256.Hash!).ToLowerInvariant();

            // 2. Save to storage
            tempFileStream.Position = 0;
            storagePath = await _fileStorageService.SaveFileAsync(tempFileStream, originalFileName, cancellationToken);

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // If replacing a photo, mark existing photo bindings for deletion
                if (isPhoto)
                {
                    var existingPhotoBindings = await _context.Set<DocumentBindingEntity>()
                        .Where(db => db.ReferenceTableName == "PropertySocialDetails"
                                  && db.ReferenceTableId == propertySocialDetailId
                                  && db.BindingPurpose == "Photo"
                                  && db.IsActive
                                  && !db.MarkedForDeletion)
                        .ToListAsync(cancellationToken);

                    foreach (var binding in existingPhotoBindings)
                    {
                        binding.MarkForDeletion();
                    }
                }

                // 3. Create new CORE.Document
                var result = await _documentService.CreateDocumentAsync(
                    uploadedBy,
                    null,
                    Path.GetFileName(storagePath),
                    originalFileName,
                    fileExtension,
                    mimeType,
                    fileSizeBytes,
                    storagePath,
                    null,
                    checksumSha256,
                    DocumentType.Proof.ToTypeString(),
                    cancellationToken);
                var documentId = result.DocumentId;
                var documentGuid = result.DocumentGuid;

                // 4. Get DepartmentId and ModuleId
                var (departmentId, moduleId) = await GetDepartmentAndModuleIdsAsync(cancellationToken);

                // 5. Create new DocumentBinding
                var documentBindingId = await _documentService.CreateDocumentBindingAsync(
                    documentId,
                    departmentId,
                    moduleId,
                    "PropertySocialDetails",
                    propertySocialDetail.Id,
                    null,
                    "Id",
                    isPhoto ? DocumentBindingPurpose.Photo.ToPurposeString() : DocumentBindingPurpose.SupportingDocument.ToPurposeString(),
                    false,
                    departmentId,
                    uploadedBy,
                    uploadedBy,
                    cancellationToken);

                // 6. Update PropertySocialDetails
                if (!isPhoto)
                {
                    propertySocialDetail.DocumentBindingId = documentBindingId;
                }
                if (!string.IsNullOrEmpty(remark))
                {
                    propertySocialDetail.Remark = remark;
                }
                propertySocialDetail.UpdatedBy = uploadedBy;
                propertySocialDetail.UpdatedDate = DateTime.Now;
                await _context.SaveChangesAsync(cancellationToken);

                // 7. Note: Old document binding will remain for audit trail
                // The new binding reference replaces the old one in PropertySocialDetails

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return new DiscountDocumentUploadResponseDto
                {
                    PropertySocialDetailId = propertySocialDetail.Id,
                    PropertyId = propertySocialDetail.PropertyId,
                    SocialAttributeId = propertySocialDetail.SocialAttributeId,
                    DocumentBindingId = documentBindingId,
                    DocumentGuid = documentGuid,
                    FileName = originalFileName,
                    Remark = remark
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                if (storagePath != null)
                {
                    try
                    {
                        await _fileStorageService.DeleteFileAsync(storagePath, cancellationToken);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogWarning(cleanupEx, "Failed to delete stored file during replace rollback: {StoragePath}", storagePath);
                    }
                }
                throw;
            }
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                    _logger.LogDebug("Deleted temporary file: {TempFilePath}", tempFilePath);
                }
                catch (Exception ex)
                {
                    // Don't fail the replace if temp-file cleanup fails
                    _logger.LogWarning(ex,
                        "Failed to delete temporary file: {TempFilePath}. File may need manual cleanup.",
                        tempFilePath);
                }
            }
        }
    }

    private async Task<(int DepartmentId, int ModuleId)> GetDepartmentAndModuleIdsAsync(CancellationToken cancellationToken)
    {
        // Step 1: Find PTIS or PROPERTY department (exact match, case-insensitive)
        // Order by ID for deterministic selection if multiple departments match
        var departments = await _departmentRepository.GetAsync(
            d => d.IsActive,
            cancellationToken);

        var department = departments
            .Where(d => d.DepartmentCode != null)
            .OrderBy(d => d.Id)
            .FirstOrDefault(d =>
                d.DepartmentCode!.Equals("PTIS", StringComparison.OrdinalIgnoreCase) ||
                d.DepartmentCode!.Equals("PROPERTY", StringComparison.OrdinalIgnoreCase));

        if (department == null)
        {
            var availableDepts = string.Join(", ", departments.Select(d => $"{d.DepartmentCode ?? "NULL"} (ID: {d.Id})"));
            throw new InvalidOperationException(
                $"No department with code PTIS or PROPERTY found in database. Available departments: {availableDepts}. " +
                "Please ensure a department with exact code 'PTIS' or 'PROPERTY' exists in DepartmentMaster table.");
        }

        // Step 2: Find PropertyManagement module under this department
        // Use exact match first, then substring match, always ordered by ID for deterministic selection
        var modules = await _moduleRepository.GetAsync(
            m => m.DepartmentId == department.Id && m.IsActive,
            cancellationToken);

        // Try exact matches first (deterministic)
        var module = modules
            .Where(m => m.ModuleCode != null)
            .OrderBy(m => m.Id)
            .FirstOrDefault(m =>
                m.ModuleCode!.Equals("PROPERTY", StringComparison.OrdinalIgnoreCase) ||
                m.ModuleCode!.Equals("PROPERTYSOCIAL", StringComparison.OrdinalIgnoreCase) ||
                m.ModuleCode!.Equals("DISCOUNT", StringComparison.OrdinalIgnoreCase));

        // Fallback to substring match if no exact match (still ordered by ID)
        if (module == null)
        {
            module = modules
                .Where(m => m.ModuleCode != null)
                .OrderBy(m => m.Id)
                .FirstOrDefault(m =>
                    m.ModuleCode!.Contains("PROPERTY", StringComparison.OrdinalIgnoreCase) ||
                    m.ModuleCode!.Contains("SOCIAL", StringComparison.OrdinalIgnoreCase) ||
                    m.ModuleCode!.Contains("DISCOUNT", StringComparison.OrdinalIgnoreCase));
        }

        // Fail fast if no match found with descriptive error
        if (module == null)
        {
            var availableModules = string.Join(", ", modules.Select(m => $"{m.ModuleCode ?? "NULL"} (ID: {m.Id})"));
            throw new InvalidOperationException(
                $"No module with code PROPERTY/PROPERTYSOCIAL/DISCOUNT found for department '{department.DepartmentCode}' (ID: {department.Id}). Available modules: {availableModules}. " +
                "Please ensure ModuleMaster table has at least one active module for this department.");
        }

        _logger.LogDebug("Resolved PropertyDiscount context: Department={DeptCode} (ID={DeptId}), Module={ModCode} (ID={ModId})",
            department.DepartmentCode, department.Id, module.ModuleCode, module.Id);

        return (department.Id, module.Id);
    }
}
