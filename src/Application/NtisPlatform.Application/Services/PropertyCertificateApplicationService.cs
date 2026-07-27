using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Common;
using NtisPlatform.Application.DTOs.Document;
using NtisPlatform.Application.DTOs.PropertyCertificate;
using NtisPlatform.Application.Events;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Enums;
using NtisPlatform.Core.Exceptions;
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
    private readonly IRepository<PropertyDetailsEntity, int> _propertyDetailsRepository;
    private readonly IPublisher _publisher;
    private readonly ICertificateTaxGuidelineReaderService _guidelineReader;
    private readonly ILogger<PropertyCertificateApplicationService> _logger;

    public PropertyCertificateApplicationService(
        IPropertyCertificateService propertyCertificateService,
        IDocumentApplicationService documentApplicationService,
        IUnitOfWork unitOfWork,
        IModuleLookupService moduleLookupService,
        IRepository<PropertyCertificateTypeMasterEntity, int> certificateTypeRepository,
        IRepository<PropertyDetailsEntity, int> propertyDetailsRepository,
        IPublisher publisher,
        ICertificateTaxGuidelineReaderService guidelineReader,
        ILogger<PropertyCertificateApplicationService> logger)
    {
        _propertyCertificateService = propertyCertificateService;
        _documentApplicationService = documentApplicationService;
        _unitOfWork = unitOfWork;
        _moduleLookupService = moduleLookupService;
        _certificateTypeRepository = certificateTypeRepository;
        _propertyDetailsRepository = propertyDetailsRepository;
        _publisher = publisher;
        _guidelineReader = guidelineReader;
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
        CancellationToken cancellationToken = default,
        int? propertyDetailsId = null)
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

        // Scope to the requested certificate scope: floor (propertyDetailsId set) or
        // property-wise (propertyDetailsId null -> PropertyDetailsId IS NULL rows only).
        // A property can have multiple rows for the SAME certificate type (one per floor plus
        // one property-wise), so the lookup key must include PropertyDetailsId, not just the type.
        var scopedCertificates = existingCertificates
            .Where(c => c.PropertyDetailsId == propertyDetailsId)
            .ToList();

        var certificateLookup = scopedCertificates
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
                CertificateTypeCode = type.CertificateTypeCode,
                IsProtected = type.IsProtected,
                IsRequired = type.IsRequired,
                DisplayOrder = type.DisplayOrder,
                HasCertificate = hasCertificate,
                PropertyCertificateId = hasCertificate && certificate != null ? certificate.Id : null,
                IsActive = hasCertificate && certificate != null && certificate.IsActive,
                CertificateNo = hasCertificate && certificate != null ? certificate.CertificateNo : null,
                IssueDate = hasCertificate && certificate != null ? certificate.IssueDate : null,
                DocumentGuid = hasCertificate && certificate != null ? NtisPlatform.Application.Common.DocumentBindingHelper.GetSafeDocumentGuid(certificate.DocumentBinding) : null,
                FileName = hasCertificate && certificate != null ? NtisPlatform.Application.Common.DocumentBindingHelper.GetSafeFileName(certificate.DocumentBinding) : null,
                // Always the requested scope, not just the existing certificate's own value:
                // scopedCertificates is already filtered to c.PropertyDetailsId == propertyDetailsId
                // (line 207), so the two are identical whenever hasCertificate is true; using the
                // requested scope directly also correctly reports which floor/property scope this
                // row represents when hasCertificate is false (no certificate to read a value from
                // yet), which the frontend needs to know where to attach a new certificate.
                PropertyDetailsId = propertyDetailsId
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
        // We need all certificates regardless of IsActive status to properly handle re-enabling.
        // Key on (CertificateTypeId, PropertyDetailsId): the same certificate type can have one
        // property-wise row (PropertyDetailsId NULL) plus one row per floor.
        var existingCertificates = await _propertyCertificateService.GetByPropertyIdIncludingInactiveAsync(
            bulkDto.PropertyId,
            PropertyCertificateIncludeOptions.None,
            cancellationToken);

        var existingLookup = existingCertificates
            .GroupBy(c => (c.CertificateTypeId, c.PropertyDetailsId))
            .ToDictionary(g => g.Key, g => g.First());

        // Looked up once so IsTaxable/CertificateTypeCode can be checked per certDto without a
        // query per row -- IsTaxable decides whether the ONE end-of-batch recalculation (see
        // recalculationNeeded) is warranted, exactly the same condition PropertyCertificateService.
        // ShouldRecalculateAsync checks per-row for the single-certificate save paths.
        // CertificateTypeCode feeds ValidateCcOcDateOrderAsync below.
        var certTypes = await _certificateTypeRepository.GetAsync(_ => true, cancellationToken);
        var isTaxableByTypeId = certTypes.ToDictionary(t => t.Id, t => t.IsTaxable);
        var typeCodeByTypeId = certTypes.ToDictionary(t => t.Id, t => t.CertificateTypeCode);

        // Reject the whole batch up front if it would leave an active OC dated earlier than an
        // active CC -- cheaper and safer than letting OccupationTaxApplicationService silently
        // apply INVALID_CC_OC_DATE_ORDER_ACTION after the fact for data that should never have
        // been savable in the first place.
        ValidateCcOcDateOrder(bulkDto, typeCodeByTypeId, existingCertificates);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // GO-LIVE BLOCKER fix: selecting multiple floors in one bulk save previously let EACH
            // certificate row publish its own PropertyCertificateChangedEvent inline (via
            // CreateAsync/UpdateAsync/ToggleEnabledAsync), so OccupationTaxApplicationService.
            // ApplyAsync/SaveTaxesAsync ran once per floor within the SAME request -- e.g. 3 floors
            // meant 3 separate recalculation passes, each re-querying and re-upserting the same
            // property-aggregated PolicyTaxDetails/TransMast rows for the SAME (PropertyId,
            // PolicyCodeId/FinanceYearId, TaxId) slots, sometimes colliding on
            // PTIS.PolicyTaxDetails' and PTIS.TransMast's unique keys. Final persistence is always
            // property-wise (see the class remarks on OccupationTaxApplicationService), so there is
            // no reason to recompute it mid-batch: every CreateAsync/UpdateAsync/ToggleEnabledAsync
            // call below suppresses its own publish, and exactly ONE
            // PropertyCertificateChangedEvent is published after every certificate in this batch
            // has been saved, reflecting the FINAL state of all selected floors at once.
            var saveRecalculationNeeded = false;
            var deleteRecalculationNeeded = false;

            foreach (var certDto in bulkDto.Certificates)
            {
                try
                {
                    var lookupKey = (certDto.CertificateTypeId, certDto.PropertyDetailsId);
                    var exists = existingLookup.TryGetValue(lookupKey, out var existingCert);
                    var isTaxableType = isTaxableByTypeId.GetValueOrDefault(certDto.CertificateTypeId);

                    if (certDto.IsEnabled)
                    {
                        // User wants this certificate enabled
                        if (!exists)
                        {
                            // Create new certificate. PropertyCertificateEntity.Create already sets
                            // IsActive = true, so it's already enabled -- do NOT also call
                            // ToggleEnabledAsync here (it would be a same-state no-op that still
                            // counts as a second save-shaped change for no reason). This mirrors the
                            // "update existing" branch below, which only calls ToggleEnabledAsync
                            // when the enabled state is actually changing.
                            var newCertId = await _propertyCertificateService.CreateAsync(
                                bulkDto.PropertyId,
                                certDto.CertificateTypeId,
                                certDto.CertificateNumber,
                                certDto.CertificateDate,
                                userId,
                                cancellationToken,
                                certDto.PropertyDetailsId,
                                suppressRecalculation: true);

                            response.EnabledCount++;
                            if (isTaxableType)
                            {
                                saveRecalculationNeeded = true;
                            }
                            _logger.LogDebug("Created new certificate (already enabled): TypeId={TypeId}, CertId={CertId}",
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
                                cancellationToken,
                                suppressRecalculation: true);

                            // Enable if not already enabled
                            if (!existingCert!.IsActive)
                            {
                                await _propertyCertificateService.ToggleEnabledAsync(
                                    existingCert.Id,
                                    true,
                                    userId,
                                    cancellationToken,
                                    suppressRecalculation: true);
                            }

                            response.EnabledCount++;
                            if (isTaxableType)
                            {
                                saveRecalculationNeeded = true;
                            }
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
                                cancellationToken,
                                suppressRecalculation: true);

                            response.DisabledCount++;
                            if (isTaxableType)
                            {
                                deleteRecalculationNeeded = true;
                            }
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

            // Exactly one recalculation for the whole batch, gated by the same
            // RECALCULATE_ON_CERTIFICATE_SAVE/_DELETE guideline toggles ShouldRecalculateAsync
            // checks per-row elsewhere -- a save-shaped change (enable/create/update) triggers it
            // under RecalculateOnSave; a delete-shaped change (disable) under RecalculateOnDelete;
            // either is enough to run the pipeline once, since it recomputes from the FINAL DB
            // state of every certificate regardless of which specific row changed.
            if (saveRecalculationNeeded || deleteRecalculationNeeded)
            {
                var guideline = await _guidelineReader.GetActiveSettingsAsync(cancellationToken);
                if ((saveRecalculationNeeded && guideline.RecalculateOnSave) ||
                    (deleteRecalculationNeeded && guideline.RecalculateOnDelete))
                {
                    await _publisher.Publish(
                        new PropertyCertificateChangedEvent(bulkDto.PropertyId, userId), cancellationToken);
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
            PropertyCertificateIncludeOptions.DocumentBinding | PropertyCertificateIncludeOptions.Document | PropertyCertificateIncludeOptions.CertificateType,
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

            // Removing the document invalidates this certificate for tax purposes, so re-run the
            // certificate-change pipeline (RV refresh then Occupation Tax apply) -- but only when
            // the certificate type is IsTaxable (same gating as every other mutation path) AND
            // PTIS.CertificateTaxGuideline's RECALCULATE_ON_CERTIFICATE_DELETE allows it (this is
            // a delete-shaped change: it removes the certificate's tax relevance).
            if (certificate.CertificateType?.IsTaxable == true)
            {
                var guideline = await _guidelineReader.GetActiveSettingsAsync(cancellationToken);
                if (guideline.RecalculateOnDelete)
                {
                    await _publisher.Publish(
                        new PropertyCertificateChangedEvent(certificate.PropertyId, deletedBy), cancellationToken);
                }
            }
        }
        catch
        {
            _logger.LogError("PropertyCertificate document deletion failed for Id={PropertyCertificateId}",
                propertyCertificateId);
            throw;
        }
    }

    public async Task DeleteCertificateByTypeAsync(
        int propertyId,
        int certificateTypeId,
        int? propertyDetailsId,
        int deletedBy,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(propertyId, nameof(propertyId));
        Guard.AgainstNegativeOrZero(certificateTypeId, nameof(certificateTypeId));
        Guard.AgainstNegativeOrZero(deletedBy, nameof(deletedBy));

        _logger.LogInformation(
            "Deleting PropertyCertificate metadata for PropertyId={PropertyId}, CertificateTypeId={CertificateTypeId}, " +
            "PropertyDetailsId={PropertyDetailsId}, User={UserId}",
            propertyId, certificateTypeId, propertyDetailsId, deletedBy);

        var existingCertificates = await _propertyCertificateService.GetByPropertyIdIncludingInactiveAsync(
            propertyId,
            PropertyCertificateIncludeOptions.DocumentBinding | PropertyCertificateIncludeOptions.Document,
            cancellationToken);

        var match = existingCertificates.FirstOrDefault(c =>
            c.CertificateTypeId == certificateTypeId &&
            c.PropertyDetailsId == propertyDetailsId &&
            !c.MarkedForDeletion);

        if (match == null)
        {
            throw new PropertyCertificateNotFoundException(
                $"PropertyId={propertyId}, CertificateTypeId={certificateTypeId}, PropertyDetailsId={propertyDetailsId?.ToString() ?? "null"}");
        }

        // Deleting the metadata row must not leave an orphaned, still-active document behind --
        // cascade-clean the attached document (if any) first, the same unlink-then-soft-delete
        // steps DeleteDocumentAsync performs, but WITHOUT its own recalculation publish:
        // DeleteAsync below already publishes once, and publishing twice would re-run the
        // RV+Occupation Tax pipeline redundantly for the same property (the same double-execution
        // bug fixed earlier this session for certificate creation).
        if (match.DocumentBinding != null)
        {
            var documentGuid = match.DocumentBinding.Document?.DocumentGuid;

            await _propertyCertificateService.UnlinkDocumentBindingAsync(match.Id, deletedBy, cancellationToken);

            if (documentGuid.HasValue)
            {
                await _documentApplicationService.DeleteDocumentAsync(documentGuid.Value, deletedBy, cancellationToken);
            }

            _logger.LogInformation(
                "PropertyCertificate document cascade-deleted ahead of metadata: PropertyCertificateId={PropertyCertificateId}, DocumentGuid={DocumentGuid}",
                match.Id, documentGuid);
        }

        // Delegates to IPropertyCertificateService.DeleteAsync, which soft-deletes the row and
        // (when the certificate type is IsTaxable) publishes PropertyCertificateChangedEvent to
        // re-run the RV-refresh-then-Occupation-Tax pipeline -- no
        // duplicated deletion behavior between the two lookup styles.
        await _propertyCertificateService.DeleteAsync(match.Id, deletedBy, cancellationToken);

        _logger.LogInformation(
            "PropertyCertificate metadata deleted: PropertyCertificateId={PropertyCertificateId} " +
            "(resolved from PropertyId={PropertyId}, CertificateTypeId={CertificateTypeId}, PropertyDetailsId={PropertyDetailsId})",
            match.Id, propertyId, certificateTypeId, propertyDetailsId);
    }

    public async Task<int> ReplaceCertificateByTypeAsync(
        int propertyId,
        int certificateTypeId,
        int? oldPropertyDetailsId,
        int? newPropertyDetailsId,
        string? newCertificateNo,
        DateTime? newIssueDate,
        int userId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(propertyId, nameof(propertyId));
        Guard.AgainstNegativeOrZero(certificateTypeId, nameof(certificateTypeId));
        Guard.AgainstNegativeOrZero(userId, nameof(userId));

        _logger.LogInformation(
            "Replacing PropertyCertificate for PropertyId={PropertyId}, CertificateTypeId={CertificateTypeId}, " +
            "OldPropertyDetailsId={OldPropertyDetailsId}, NewPropertyDetailsId={NewPropertyDetailsId}, User={UserId}",
            propertyId, certificateTypeId, oldPropertyDetailsId, newPropertyDetailsId, userId);

        var existingCertificates = await _propertyCertificateService.GetByPropertyIdIncludingInactiveAsync(
            propertyId,
            PropertyCertificateIncludeOptions.DocumentBinding | PropertyCertificateIncludeOptions.Document,
            cancellationToken);

        var match = existingCertificates.FirstOrDefault(c =>
            c.CertificateTypeId == certificateTypeId &&
            c.PropertyDetailsId == oldPropertyDetailsId &&
            !c.MarkedForDeletion);

        if (match == null)
        {
            throw new PropertyCertificateNotFoundException(
                $"PropertyId={propertyId}, CertificateTypeId={certificateTypeId}, PropertyDetailsId={oldPropertyDetailsId?.ToString() ?? "null"}");
        }

        var certificateType = await _certificateTypeRepository.GetByIdAsync(certificateTypeId, cancellationToken);
        var isTaxableType = certificateType?.IsTaxable ?? false;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        int newCertificateId;
        try
        {
            // Cascade-clean any attached document ahead of the delete -- same as
            // DeleteCertificateByTypeAsync, so this never leaves an orphaned, still-active document.
            if (match.DocumentBinding != null)
            {
                var documentGuid = match.DocumentBinding.Document?.DocumentGuid;
                await _propertyCertificateService.UnlinkDocumentBindingAsync(match.Id, userId, cancellationToken);
                if (documentGuid.HasValue)
                {
                    await _documentApplicationService.DeleteDocumentAsync(documentGuid.Value, userId, cancellationToken);
                }
            }

            // Both suppressed -- see this method's own PropertyCertificateChangedEvent publish below,
            // which fires exactly once against the FINAL state (new certificate present), not the
            // momentarily-certificate-less intermediate state between these two calls.
            await _propertyCertificateService.DeleteAsync(match.Id, userId, cancellationToken, suppressRecalculation: true);

            newCertificateId = await _propertyCertificateService.CreateAsync(
                propertyId,
                certificateTypeId,
                newCertificateNo,
                newIssueDate,
                userId,
                cancellationToken,
                newPropertyDetailsId,
                suppressRecalculation: true);

            if (isTaxableType)
            {
                var guideline = await _guidelineReader.GetActiveSettingsAsync(cancellationToken);
                if (guideline.RecalculateOnSave || guideline.RecalculateOnDelete)
                {
                    await _publisher.Publish(new PropertyCertificateChangedEvent(propertyId, userId), cancellationToken);
                }
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        _logger.LogInformation(
            "PropertyCertificate replaced: OldId={OldId} -> NewId={NewId} (PropertyId={PropertyId}, CertificateTypeId={CertificateTypeId})",
            match.Id, newCertificateId, propertyId, certificateTypeId);

        return newCertificateId;
    }

    public async Task<FloorCertificatesResponseDto> GetFloorCertificatesAsync(
        int propertyId,
        int? selectedPropertyDetailsId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(propertyId, nameof(propertyId));

        var floors = await _propertyDetailsRepository.GetQueryable()
            .AsNoTracking()
            .Where(pd => pd.PropertyId == propertyId && pd.IsActive && !pd.MarkedForDeletion)
            .Include(pd => pd.Floor)
            .Include(pd => pd.SubFloor)
            .Include(pd => pd.ConstructionType)
            .Include(pd => pd.TypeOfUse)
            .Include(pd => pd.SubTypeOfUse)
            .ToListAsync(cancellationToken);

        if (selectedPropertyDetailsId.HasValue && !floors.Any(f => f.Id == selectedPropertyDetailsId.Value))
        {
            _logger.LogWarning(
                "GetFloorCertificatesAsync: selectedPropertyDetailsId {PropertyDetailsId} does not belong to property {PropertyId}; no floor will be marked selected.",
                selectedPropertyDetailsId, propertyId);
        }

        var response = new FloorCertificatesResponseDto
        {
            PropertyId = propertyId,
            SelectedPropertyDetailsId = selectedPropertyDetailsId
        };

        if (floors.Count == 0)
        {
            // No floors yet — still return property-wise certificates, empty floor list (not an exception).
            response.PropertyWiseCertificates = await GetCertificateTypesWithStatusAsync(
                propertyId, cancellationToken, propertyDetailsId: null);
            return response;
        }

        var allCertificates = await _propertyCertificateService.GetByPropertyIdAsync(
            propertyId,
            PropertyCertificateIncludeOptions.CertificateType,
            cancellationToken);

        var propertyWiseCerts = allCertificates.Where(c => c.PropertyDetailsId == null).ToList();
        var floorWiseCerts = allCertificates
            .Where(c => c.PropertyDetailsId.HasValue)
            .GroupBy(c => c.PropertyDetailsId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        response.PropertyWiseCertificates = await GetCertificateTypesWithStatusAsync(
            propertyId, cancellationToken, propertyDetailsId: null);

        // ELECTRIC_BILL_CERTIFICATE_CODES is read here too (not just in the tax engine) so this
        // display endpoint and OccupationTaxApplicationService never disagree about which
        // certificate is the Electric Bill one -- real seed data has been observed using both
        // "ELECTRIC_BILL" and "EleBillDt" as CertificateTypeCode for the same certificate type.
        var guideline = await _guidelineReader.GetActiveSettingsAsync(cancellationToken);
        var electricBillCodes = guideline.ElectricBillCertificateCodes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (electricBillCodes.Length == 0)
        {
            electricBillCodes = new[] { CertificateTypeCodes.ElectricBill };
        }

        var allFloorDtos = floors.Select(floor =>
        {
            floorWiseCerts.TryGetValue(floor.Id, out var certsForFloor);
            certsForFloor ??= new List<PropertyCertificateEntity>();

            // Floor-wise certificate overrides property-wise for that specific floor/type;
            // property-wise certificate is the fallback for types with no floor-wise row.
            var effectiveCc = ResolveEffectiveDate(certsForFloor, propertyWiseCerts, new[] { CertificateTypeCodes.CC }, "completion");
            var effectiveOc = ResolveEffectiveDate(certsForFloor, propertyWiseCerts, new[] { CertificateTypeCodes.OC }, "occupancy", "occupation");
            var effectiveElectricBill = ResolveEffectiveDate(certsForFloor, propertyWiseCerts, electricBillCodes, "electricity", "electric", "bill");

            var certificateApplicable = certsForFloor.Any(c => c.IsActive) || propertyWiseCerts.Any(c => c.IsActive);

            return new FloorCertificateDto
            {
                PropertyDetailsId = floor.Id,
                PropertyId = propertyId,
                FloorDescription = floor.Floor?.Description,
                SubFloorDescription = floor.SubFloor?.Description,
                ConstructionYear = floor.ConstructionYear,
                AssessmentYear = floor.AssessmentYear,
                ConstructionTypeDescription = floor.ConstructionType?.Description,
                TypeOfUseDescription = floor.TypeOfUse?.Description,
                SubTypeOfUseDescription = floor.SubTypeOfUse?.Description,
                CarpetAreaSqFeet = floor.CarpetAreaSqFeet,
                CarpetAreaSqMeter = floor.CarpetAreaSqMeter,
                BuiltupAreaSqFeet = floor.BuiltupAreaSqFeet,
                BuiltupAreaSqMeter = floor.BuiltupAreaSqMeter,
                IsSelected = selectedPropertyDetailsId.HasValue && floor.Id == selectedPropertyDetailsId.Value,
                CertificateApplicable = certificateApplicable,
                CcDate = effectiveCc?.IssueDate,
                OcDate = effectiveOc?.IssueDate,
                ElectricBillDate = effectiveElectricBill?.IssueDate,
                CcCertificateNo = effectiveCc?.CertificateNo,
                OcCertificateNo = effectiveOc?.CertificateNo,
                ElectricBillNo = effectiveElectricBill?.CertificateNo
            };
        }).ToList();

        // Split into the one selected floor (if any) and every other floor, rather than one flat
        // list the UI has to scan for IsSelected.
        response.SelectedFloor = allFloorDtos.FirstOrDefault(f => f.IsSelected);
        response.OtherFloors = allFloorDtos.Where(f => !f.IsSelected).ToList();

        return response;
    }

    /// <summary>
    /// Matches a certificate type against ANY of several codes (CC/OC have exactly one;
    /// Electric Bill's set is guideline-driven via ELECTRIC_BILL_CERTIFICATE_CODES since real seed
    /// data has been observed using both "ELECTRIC_BILL" and "EleBillDt" for the same certificate
    /// type), preferring CertificateTypeCode when populated and falling back to the display-name
    /// heuristic when it isn't (older/seed data may not have codes backfilled yet) — mirrors
    /// <see cref="TaxEngine.OccupationTaxApplicationService"/>'s matching so this endpoint and the
    /// tax engine never disagree about which certificate is CC/OC/Electric Bill.
    /// </summary>
    private static bool MatchesCertificateType(PropertyCertificateTypeMasterEntity type, IReadOnlyCollection<string> codes, params string[] nameContains)
    {
        if (!string.IsNullOrEmpty(type.CertificateTypeCode))
        {
            return codes.Any(c => string.Equals(type.CertificateTypeCode, c, StringComparison.OrdinalIgnoreCase));
        }

        var name = type.CertificateTypeName.ToLowerInvariant();
        return nameContains.Any(n => name.Contains(n, StringComparison.OrdinalIgnoreCase)) ||
            codes.Any(c => string.Equals(name, c, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Resolves the effective certificate for a given certificate-type code set on one floor:
    /// the floor-wise certificate if present (override), else the property-wise certificate
    /// (fallback).
    /// </summary>
    private static PropertyCertificateEntity? ResolveEffectiveDate(
        List<PropertyCertificateEntity> floorWiseCerts,
        List<PropertyCertificateEntity> propertyWiseCerts,
        IReadOnlyCollection<string> certificateTypeCodes,
        params string[] nameContains)
    {
        bool Matches(PropertyCertificateEntity c) =>
            c.IsActive &&
            c.CertificateType != null &&
            MatchesCertificateType(c.CertificateType, certificateTypeCodes, nameContains);

        return floorWiseCerts.FirstOrDefault(Matches) ?? propertyWiseCerts.FirstOrDefault(Matches);
    }

    /// <summary>
    /// Saves/updates certificate metadata only. Document upload is NOT handled here — the client
    /// uploads the file separately via the Global Document endpoint (<c>POST /api/documents/upload</c>)
    /// with <c>ReferenceTableName=PropertyCertificates</c> and <c>ReferenceTableId=&lt;the id returned
    /// by this call&gt;</c>; <see cref="NtisPlatform.Infrastructure.Services.Handlers.PropertyCertificateDocumentBindingHandler"/>
    /// automatically links the resulting DocumentBindingId back onto this row afterwards.
    /// </summary>
    public async Task<SaveCertificateResponseDto> SaveCertificateAsync(
        SaveCertificateRequestDto request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(request.PropertyId, nameof(request.PropertyId));
        Guard.AgainstNegativeOrZero(request.CertificateTypeId, nameof(request.CertificateTypeId));
        Guard.AgainstNegativeOrZero(userId, nameof(userId));

        if (request.CertificateScope == CertificateScope.Floor && !request.PropertyDetailsId.HasValue)
        {
            throw new ArgumentException("PropertyDetailsId is required when CertificateScope is Floor.", nameof(request.PropertyDetailsId));
        }

        if (request.CertificateScope == CertificateScope.Property && request.PropertyDetailsId.HasValue)
        {
            throw new ArgumentException("PropertyDetailsId must be null when CertificateScope is Property.", nameof(request.PropertyDetailsId));
        }

        var certificateType = await _certificateTypeRepository.GetByIdAsync(request.CertificateTypeId, cancellationToken);
        if (certificateType == null || !certificateType.IsActive)
        {
            throw new InvalidOperationException($"Certificate type {request.CertificateTypeId} was not found or is inactive.");
        }

        // Find the existing row for this exact (PropertyId, PropertyDetailsId, CertificateTypeId) scope.
        var existingCertificates = await _propertyCertificateService.GetByPropertyIdIncludingInactiveAsync(
            request.PropertyId,
            PropertyCertificateIncludeOptions.DocumentBinding | PropertyCertificateIncludeOptions.Document,
            cancellationToken);

        var existing = existingCertificates.FirstOrDefault(c =>
            c.CertificateTypeId == request.CertificateTypeId &&
            c.PropertyDetailsId == request.PropertyDetailsId);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        int propertyCertificateId;

        try
        {
            if (existing == null)
            {
                // PropertyCertificateEntity.Create already sets IsActive = true -- do NOT also call
                // ToggleEnabledAsync here (see the identical fix and full rationale in
                // BulkSaveAllAsync's "create new certificate" branch above).
                propertyCertificateId = await _propertyCertificateService.CreateAsync(
                    request.PropertyId,
                    request.CertificateTypeId,
                    request.CertificateNo,
                    request.CertificateIssueDate,
                    userId,
                    cancellationToken,
                    request.PropertyDetailsId);
            }
            else
            {
                propertyCertificateId = existing.Id;
                await _propertyCertificateService.UpdateAsync(
                    propertyCertificateId,
                    request.CertificateNo,
                    request.CertificateIssueDate,
                    userId,
                    cancellationToken);

                if (!existing.IsActive)
                {
                    await _propertyCertificateService.ToggleEnabledAsync(propertyCertificateId, true, userId, cancellationToken);
                }
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        // No explicit publish here: PropertyCertificateService.CreateAsync/UpdateAsync/
        // ToggleEnabledAsync above already publish PropertyCertificateChangedEvent when the
        // certificate type is IsTaxable (not IsProtected -- that flag only gates whether the
        // type master row can be deleted). Report the same condition here so the response
        // accurately reflects whether recalculation actually ran.
        return new SaveCertificateResponseDto
        {
            PropertyCertificateId = propertyCertificateId,
            PropertyId = request.PropertyId,
            PropertyDetailsId = request.PropertyDetailsId,
            CertificateScope = request.CertificateScope,
            CertificateTypeId = request.CertificateTypeId,
            CertificateNo = request.CertificateNo,
            CertificateIssueDate = request.CertificateIssueDate,
            DocumentGuid = existing?.DocumentBinding?.Document?.DocumentGuid,
            DocumentBindingId = existing?.DocumentBindingId,
            TaxRecalculationTriggered = certificateType.IsTaxable
        };
    }

    /// <summary>
    /// Validates that an active Occupancy Certificate (OC) date is not earlier than an active
    /// Completion Certificate (CC) date for the property, based on CertificateTypeCode -- applied
    /// to the FINAL state a bulk save would produce (existing certificates the batch doesn't touch,
    /// overlaid with what this batch enables/disables/dates), not just the incoming DTOs in
    /// isolation. Only CC and OC participate; Electric Bill has no configured date-order rule.
    /// </summary>
    private static void ValidateCcOcDateOrder(
        PropertyCertificateBulkSaveDto bulkDto,
        Dictionary<int, string> typeCodeById,
        List<PropertyCertificateEntity> existingCertificates)
    {
        DateTime? ccDate = null;
        DateTime? ocDate = null;

        foreach (var cert in existingCertificates)
        {
            if (!cert.IsActive || cert.MarkedForDeletion) continue;
            var code = typeCodeById.GetValueOrDefault(cert.CertificateTypeId);
            if (string.Equals(code, "CC", StringComparison.OrdinalIgnoreCase))
            {
                ccDate = cert.IssueDate;
            }
            else if (string.Equals(code, "OC", StringComparison.OrdinalIgnoreCase))
            {
                ocDate = cert.IssueDate;
            }
        }

        foreach (var certDto in bulkDto.Certificates)
        {
            var code = typeCodeById.GetValueOrDefault(certDto.CertificateTypeId);
            if (certDto.IsEnabled)
            {
                if (string.Equals(code, "CC", StringComparison.OrdinalIgnoreCase))
                {
                    ccDate = certDto.CertificateDate;
                }
                else if (string.Equals(code, "OC", StringComparison.OrdinalIgnoreCase))
                {
                    ocDate = certDto.CertificateDate;
                }
            }
            else
            {
                if (string.Equals(code, "CC", StringComparison.OrdinalIgnoreCase))
                {
                    ccDate = null;
                }
                else if (string.Equals(code, "OC", StringComparison.OrdinalIgnoreCase))
                {
                    ocDate = null;
                }
            }
        }

        if (ccDate.HasValue && ocDate.HasValue && ocDate.Value.Date < ccDate.Value.Date)
        {
            throw new InvalidOperationException(
                $"Occupancy Certificate (OC) date ({ocDate.Value:dd-MM-yyyy}) cannot be earlier than Completion Certificate (CC) date ({ccDate.Value:dd-MM-yyyy}).");
        }
    }
}
