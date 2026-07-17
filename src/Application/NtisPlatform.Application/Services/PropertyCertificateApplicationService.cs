using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Common;
using NtisPlatform.Application.DTOs.Document;
using NtisPlatform.Application.DTOs.PropertyCertificate;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Enums;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Application service for PropertyCertificate operations.
/// Delegates all file handling to DocumentApplicationService.
/// </summary>
public class PropertyCertificateApplicationService : IPropertyCertificateApplicationService
{
    private readonly IPropertyCertificateService _propertyCertificateService;
    private readonly IDocumentApplicationService _documentApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IModuleLookupService _moduleLookupService;
    private readonly IRepository<PropertyCertificateTypeMasterEntity, int> _certificateTypeRepository;
    private readonly ILogger<PropertyCertificateApplicationService> _logger;

    public PropertyCertificateApplicationService(
        IPropertyCertificateService propertyCertificateService,
        IDocumentApplicationService documentApplicationService,
        IUnitOfWork unitOfWork,
        IModuleLookupService moduleLookupService,
        IRepository<PropertyCertificateTypeMasterEntity, int> certificateTypeRepository,
        ILogger<PropertyCertificateApplicationService> logger)
    {
        _propertyCertificateService = propertyCertificateService;
        _documentApplicationService = documentApplicationService;
        _unitOfWork = unitOfWork;
        _moduleLookupService = moduleLookupService;
        _certificateTypeRepository = certificateTypeRepository;
        _logger = logger;
    }

    public async Task<PropertyCertificateUploadResponseDto> UploadWithDocumentAsync(
        Stream fileStream,
        string originalFileName,
        string mimeType,
        long fileSizeBytes,
        int propertyId,
        int certificateTypeId,
        string? certificateNo,
        DateTime? issueDate,
        int uploadedBy,
        CancellationToken cancellationToken = default)
    {
        // Input validation using Guard clauses
        Guard.AgainstInvalidStream(fileStream, nameof(fileStream));
        Guard.AgainstNullOrWhiteSpace(originalFileName, nameof(originalFileName));
        Guard.AgainstExceedingLength(originalFileName, 255, nameof(originalFileName));
        Guard.AgainstNullOrWhiteSpace(mimeType, nameof(mimeType));
        Guard.AgainstNegativeOrZero(fileSizeBytes, nameof(fileSizeBytes));
        Guard.AgainstNegativeOrZero(propertyId, nameof(propertyId));
        Guard.AgainstNegativeOrZero(certificateTypeId, nameof(certificateTypeId));
        Guard.AgainstNegativeOrZero(uploadedBy, nameof(uploadedBy));

        // Optional field validation
        if (!string.IsNullOrWhiteSpace(certificateNo))
        {
            Guard.AgainstExceedingLength(certificateNo, 100, nameof(certificateNo));
        }

        if (issueDate.HasValue)
        {
            Guard.Against(issueDate.Value > DateTime.Now, "Issue date cannot be in the future.", nameof(issueDate));
        }

        _logger.LogInformation("Starting PropertyCertificate upload: {FileName}, PropertyId: {PropertyId}, CertificateTypeId: {CertificateTypeId}, User: {UserId}",
            originalFileName, propertyId, certificateTypeId, uploadedBy);

        // 1. Create PropertyCertificate first (without DocumentBinding)
        var propertyCertificateId = await _propertyCertificateService.CreateAsync(
            propertyId,
            certificateTypeId,
            certificateNo,
            issueDate,
            uploadedBy,
            cancellationToken);
        _logger.LogInformation("PropertyCertificate created: Id={PropertyCertificateId}",
            propertyCertificateId);

        try
        {
            // 2. Get DepartmentId and ModuleId from database
            var (departmentId, moduleId) = await GetDepartmentAndModuleIdsAsync(cancellationToken);

            // 3. Delegate file handling to DocumentApplicationService
            var uploadDto = new DocumentUploadDto
            {
                DepartmentId = departmentId,
                ModuleId = moduleId,
                ReferenceTableName = "PropertyCertificates",
                ReferenceTableId = propertyCertificateId,
                ReferencePropertyName = "Id",
                BindingPurpose = DocumentBindingPurpose.MainDocument.ToPurposeString(),
                IsPrimaryDocument = true,
                AuthDepartmentId = departmentId,
                AuthReferenceId = propertyId,
                DocumentType = DocumentType.Certificate.ToTypeString()
            };

            var docResponse = await _documentApplicationService.UploadDocumentAsync(
                fileStream,
                originalFileName,
                mimeType,
                fileSizeBytes,
                uploadDto,
                uploadedBy,
                cancellationToken);

            // 4. Update PropertyCertificate with DocumentBinding ID
            if (docResponse.DocumentBindingId.HasValue)
            {
                await _propertyCertificateService.UpdateDocumentBindingAsync(
                    propertyCertificateId,
                    docResponse.DocumentBindingId.Value,
                    uploadedBy,
                    cancellationToken);
            }

            _logger.LogInformation("PropertyCertificate upload completed successfully: PropertyCertificateId={PropertyCertificateId}, DocumentGuid={DocumentGuid}",
                propertyCertificateId, docResponse.DocumentGuid);

            return new PropertyCertificateUploadResponseDto
            {
                PropertyCertificateId = propertyCertificateId,
                DocumentGuid = docResponse.DocumentGuid,
                DocumentId = docResponse.DocumentId,
                DocumentBindingId = docResponse.DocumentBindingId ?? 0,
                PropertyId = propertyId,
                CertificateTypeId = certificateTypeId,
                CertificateNo = certificateNo,
                IssueDate = issueDate,
                FileName = originalFileName,
                FileSizeBytes = fileSizeBytes,
                StoragePath = docResponse.StoragePath ?? string.Empty
            };
        }
        catch
        {
            _logger.LogError("PropertyCertificate upload failed for Id={PropertyCertificateId}. Document service will handle cleanup.",
                propertyCertificateId);
            throw;
        }
    }



    // ── Private helpers ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the DepartmentId and ModuleId from the database dynamically.
    /// Finds the department that contains PropertyMast table and its primary module.
    /// No hardcoding - fully database-driven.
    /// </summary>
    private async Task<(int DepartmentId, int ModuleId)> GetDepartmentAndModuleIdsAsync(
        CancellationToken cancellationToken = default)
    {
        // Delegate to IModuleLookupService for table-driven module/department resolution
        return await _moduleLookupService.GetDepartmentAndModuleAsync("PTIS", "PROPERTY", cancellationToken);
    }

    public async Task<List<PropertyCertificateWithStatusDto>> GetCertificateTypesWithStatusAsync(
        int propertyId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(propertyId, nameof(propertyId));

        // Get all active certificate types
        var allTypes = await _certificateTypeRepository.GetAsync(
            ct => ct.IsActive,
            cancellationToken);

        // Get existing certificates for this property (including inactive ones so status is accurate)
        var existingCertificates = await _propertyCertificateService.GetByPropertyIdIncludingInactiveAsync(
            propertyId,
            PropertyCertificateIncludeOptions.DocumentBinding | PropertyCertificateIncludeOptions.Document,
            cancellationToken);

        // Create a lookup for quick access
        var certificateLookup = existingCertificates
            .GroupBy(c => c.CertificateTypeId)
            .ToDictionary(g => g.Key, g => g.First());

        // Build result combining all types with their status
        var result = allTypes.OrderBy(t => t.DisplayOrder).Select(type =>
        {
            var hasCertificate = certificateLookup.TryGetValue(type.Id, out var certificate);

            return new PropertyCertificateWithStatusDto
            {
                CertificateTypeId = type.Id,
                CertificateTypeName = type.CertificateTypeName,
                DisplayOrder = type.DisplayOrder,
                HasCertificate = hasCertificate,
                PropertyCertificateId = hasCertificate && certificate != null ? certificate.Id : null,
                IsActive = hasCertificate && certificate != null && certificate.IsActive,
                CertificateNo = hasCertificate && certificate != null ? certificate.CertificateNo : null,
                IssueDate = hasCertificate && certificate != null ? certificate.IssueDate : null,
                DocumentGuid = hasCertificate && certificate != null ? NtisPlatform.Application.Common.DocumentBindingHelper.GetSafeDocumentGuid(certificate.DocumentBinding) : null,
                FileName = hasCertificate && certificate != null ? NtisPlatform.Application.Common.DocumentBindingHelper.GetSafeFileName(certificate.DocumentBinding) : null,
                PropertyDetailsId = hasCertificate && certificate != null ? certificate.PropertyDetailsId : null
            };
        }).ToList();

        return result;
    }

    public async Task<PropertyCertificateUploadResponseDto> ReplaceDocumentAsync(
        int propertyCertificateId,
        Stream fileStream,
        string originalFileName,
        string mimeType,
        long fileSizeBytes,
        int uploadedBy,
        CancellationToken cancellationToken = default)
    {
        // Input validation
        Guard.AgainstNegativeOrZero(propertyCertificateId, nameof(propertyCertificateId));
        Guard.AgainstInvalidStream(fileStream, nameof(fileStream));
        Guard.AgainstNullOrWhiteSpace(originalFileName, nameof(originalFileName));
        Guard.AgainstNullOrWhiteSpace(mimeType, nameof(mimeType));
        Guard.AgainstNegativeOrZero(fileSizeBytes, nameof(fileSizeBytes));
        Guard.AgainstNegativeOrZero(uploadedBy, nameof(uploadedBy));

        _logger.LogInformation("Replacing document for PropertyCertificateId={Id}, NewFile={FileName}",
            propertyCertificateId, originalFileName);

        // Get existing certificate
        var certificate = await _propertyCertificateService.GetByIdAsync(
            propertyCertificateId,
            PropertyCertificateIncludeOptions.DocumentBinding | PropertyCertificateIncludeOptions.Document,
            cancellationToken);

        if (certificate == null)
        {
            throw new InvalidOperationException($"PropertyCertificate with ID {propertyCertificateId} not found.");
        }

        var oldDocumentGuid = certificate.DocumentBinding?.Document?.DocumentGuid;

        try
        {
            // 1. Get DepartmentId and ModuleId from database
            var (departmentId, moduleId) = await GetDepartmentAndModuleIdsAsync(cancellationToken);

            // 2. Upload new file via DocumentApplicationService
            var uploadDto = new DocumentUploadDto
            {
                DepartmentId = departmentId,
                ModuleId = moduleId,
                ReferenceTableName = "PropertyCertificates",
                ReferenceTableId = propertyCertificateId,
                ReferencePropertyName = "Id",
                BindingPurpose = DocumentBindingPurpose.MainDocument.ToPurposeString(),
                IsPrimaryDocument = true,
                AuthDepartmentId = departmentId,
                AuthReferenceId = certificate.PropertyId,
                DocumentType = DocumentType.Certificate.ToTypeString()
            };

            var docResponse = await _documentApplicationService.UploadDocumentAsync(
                fileStream,
                originalFileName,
                mimeType,
                fileSizeBytes,
                uploadDto,
                uploadedBy,
                cancellationToken);

            // 3. Update certificate with new binding
            if (docResponse.DocumentBindingId.HasValue)
            {
                await _propertyCertificateService.UpdateDocumentBindingAsync(
                    propertyCertificateId,
                    docResponse.DocumentBindingId.Value,
                    uploadedBy,
                    cancellationToken);
            }

            // 4. Soft-delete the old document (DocumentApplicationService handles file cleanup via DeleteDocumentAsync)
            if (oldDocumentGuid.HasValue)
            {
                await _documentApplicationService.DeleteDocumentAsync(oldDocumentGuid.Value, uploadedBy, cancellationToken);
            }

            _logger.LogInformation("PropertyCertificate document replaced: PropertyCertificateId={PropertyCertificateId}, OldDocumentGuid={OldDocumentGuid}, NewDocumentGuid={NewDocumentGuid}",
                propertyCertificateId, oldDocumentGuid, docResponse.DocumentGuid);

            return new PropertyCertificateUploadResponseDto
            {
                PropertyCertificateId = propertyCertificateId,
                DocumentGuid = docResponse.DocumentGuid,
                DocumentId = docResponse.DocumentId,
                DocumentBindingId = docResponse.DocumentBindingId ?? 0,
                PropertyId = certificate.PropertyId,
                CertificateTypeId = certificate.CertificateTypeId,
                CertificateNo = certificate.CertificateNo,
                IssueDate = certificate.IssueDate,
                FileName = originalFileName,
                FileSizeBytes = fileSizeBytes,
                StoragePath = docResponse.StoragePath ?? string.Empty,
                PropertyDetailsId = certificate.PropertyDetailsId
            };
        }
        catch
        {
            _logger.LogError("PropertyCertificate document replacement failed for Id={PropertyCertificateId}. Document service will handle cleanup.",
                propertyCertificateId);
            throw;
        }
    }

    public async Task<PropertyCertificateBulkSaveResponseDto> BulkSaveAllAsync(
        PropertyCertificateBulkSaveDto bulkDto,
        int userId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(bulkDto.PropertyId, nameof(bulkDto.PropertyId));
        Guard.AgainstNegativeOrZero(userId, nameof(userId));

        _logger.LogInformation("Bulk saving {Count} certificates for PropertyId={PropertyId}, User={UserId}",
            bulkDto.Certificates.Count, bulkDto.PropertyId, userId);

        var response = new PropertyCertificateBulkSaveResponseDto
        {
            PropertyId = bulkDto.PropertyId,
            TotalProcessed = bulkDto.Certificates.Count
        };

        // Get existing certificates for this property (including inactive ones)
        // We need all certificates regardless of IsActive status to properly handle re-enabling
        var existingCertificates = await _propertyCertificateService.GetByPropertyIdIncludingInactiveAsync(
            bulkDto.PropertyId,
            PropertyCertificateIncludeOptions.None,
            cancellationToken);

        var existingLookup = existingCertificates
            .GroupBy(c => c.CertificateTypeId)
            .ToDictionary(g => g.Key, g => g.First());

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var certDto in bulkDto.Certificates)
            {
                try
                {
                    var exists = existingLookup.TryGetValue(certDto.CertificateTypeId, out var existingCert);

                    if (certDto.IsEnabled)
                    {
                        // User wants this certificate enabled
                        if (!exists)
                        {
                            // Create new certificate
                            var newCertId = await _propertyCertificateService.CreateAsync(
                                bulkDto.PropertyId,
                                certDto.CertificateTypeId,
                                certDto.CertificateNumber,
                                certDto.CertificateDate,
                                userId,
                                cancellationToken);

                            // Enable it
                            await _propertyCertificateService.ToggleEnabledAsync(
                                newCertId,
                                true,
                                userId,
                                cancellationToken);

                            response.EnabledCount++;
                            _logger.LogDebug("Created and enabled new certificate: TypeId={TypeId}, CertId={CertId}",
                                certDto.CertificateTypeId, newCertId);
                        }
                        else
                        {
                            // Update existing certificate (always call UpdateAsync to allow clearing values)
                            await _propertyCertificateService.UpdateAsync(
                                existingCert!.Id,
                                certDto.CertificateNumber,
                                certDto.CertificateDate,
                                userId,
                                cancellationToken);

                            // Enable if not already enabled
                            if (!existingCert!.IsActive)
                            {
                                await _propertyCertificateService.ToggleEnabledAsync(
                                    existingCert.Id,
                                    true,
                                    userId,
                                    cancellationToken);
                            }

                            response.EnabledCount++;
                            _logger.LogDebug("Updated and enabled existing certificate: CertId={CertId}",
                                existingCert.Id);
                        }
                    }
                    else
                    {
                        // User wants this certificate disabled
                        if (exists && existingCert != null && existingCert.IsActive)
                        {
                            // Disable existing certificate
                            await _propertyCertificateService.ToggleEnabledAsync(
                                existingCert.Id,
                                false,
                                userId,
                                cancellationToken);

                            response.DisabledCount++;
                            _logger.LogDebug("Disabled certificate: CertId={CertId}", existingCert.Id);
                        }
                        else
                        {
                            response.DisabledCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process certificate: TypeId={TypeId}",
                        certDto.CertificateTypeId);
                    response.Errors.Add($"Certificate Type {certDto.CertificateTypeId}: {ex.Message}");
                }
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // Get updated status for all certificates
            response.UpdatedCertificates = await GetCertificateTypesWithStatusAsync(
                bulkDto.PropertyId,
                cancellationToken);

            _logger.LogInformation("Bulk save completed: Enabled={Enabled}, Disabled={Disabled}, Errors={ErrorCount}",
                response.EnabledCount, response.DisabledCount, response.Errors.Count);

            return response;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteDocumentAsync(
        int propertyCertificateId,
        int deletedBy,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(propertyCertificateId, nameof(propertyCertificateId));
        Guard.AgainstNegativeOrZero(deletedBy, nameof(deletedBy));

        _logger.LogInformation("Deleting document for PropertyCertificateId={Id}, User={UserId}",
            propertyCertificateId, deletedBy);

        var certificate = await _propertyCertificateService.GetByIdAsync(
            propertyCertificateId,
            PropertyCertificateIncludeOptions.DocumentBinding | PropertyCertificateIncludeOptions.Document,
            cancellationToken);

        if (certificate == null)
        {
            throw new InvalidOperationException($"PropertyCertificate with ID {propertyCertificateId} not found.");
        }

        if (certificate.DocumentBinding == null)
        {
            throw new InvalidOperationException($"PropertyCertificate with ID {propertyCertificateId} does not have an associated document.");
        }

        var documentGuid = certificate.DocumentBinding.Document?.DocumentGuid;

        try
        {
            // 1. Unlink the document binding from the certificate
            await _propertyCertificateService.UnlinkDocumentBindingAsync(
                propertyCertificateId,
                deletedBy,
                cancellationToken);

            // 2. Soft-delete the document and physical file via DocumentApplicationService
            if (documentGuid.HasValue)
            {
                await _documentApplicationService.DeleteDocumentAsync(documentGuid.Value, deletedBy, cancellationToken);
            }

            _logger.LogInformation("PropertyCertificate document deleted: PropertyCertificateId={PropertyCertificateId}, DocumentGuid={DocumentGuid}",
                propertyCertificateId, documentGuid);
        }
        catch
        {
            _logger.LogError("PropertyCertificate document deletion failed for Id={PropertyCertificateId}",
                propertyCertificateId);
            throw;
        }
    }
}
