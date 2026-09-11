using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Common;
using NtisPlatform.Application.DTOs.Document;
using NtisPlatform.Application.DTOs.PropertyCertificate;
using NtisPlatform.Application.Events;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
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
    private readonly IRepository<PropertyEntity, int> _propertyRepository;
    private readonly IRepository<SocietyDetailsEntity, int> _societyRepository;
    private readonly IRepository<WingDetailsMastEntity, int> _wingDetailsMastRepository;
    private readonly IRepository<WingEntity, int> _wingRepository;
    private readonly IPublisher _publisher;
    private readonly ILogger<PropertyCertificateApplicationService> _logger;
    private readonly IRateableValueApiClient _rateableValueApiClient;
    private readonly IRetrospectiveTaxCalculationEngineService _retrospectiveTaxEngine;
    private readonly IRepository<PropertyCertificateEntity>? _propertyCertificateRepository;
    private readonly IRepository<DocumentBindingEntity>? _documentBindingRepository;
    private readonly IRepository<DocumentEntity>? _documentRepository;
    private readonly IServiceProvider? _serviceProvider;

    public PropertyCertificateApplicationService(
        IPropertyCertificateService propertyCertificateService,
        IDocumentApplicationService documentApplicationService,
        IUnitOfWork unitOfWork,
        IModuleLookupService moduleLookupService,
        IRepository<PropertyCertificateTypeMasterEntity, int> certificateTypeRepository,
        IRepository<PropertyDetailsEntity, int> propertyDetailsRepository,
        IRepository<PropertyEntity, int> propertyRepository,
        IRepository<SocietyDetailsEntity, int> societyRepository,
        IRepository<WingDetailsMastEntity, int> wingDetailsMastRepository,
        IRepository<WingEntity, int> wingRepository,
        IPublisher publisher,
        ILogger<PropertyCertificateApplicationService> logger,
        IRateableValueApiClient rateableValueApiClient,
        IRetrospectiveTaxCalculationEngineService retrospectiveTaxEngine,
        IRepository<PropertyCertificateEntity>? propertyCertificateRepository = null,
        IRepository<DocumentBindingEntity>? documentBindingRepository = null,
        IRepository<DocumentEntity>? documentRepository = null,
        IServiceProvider? serviceProvider = null)
    {
        _propertyCertificateService = propertyCertificateService;
        _documentApplicationService = documentApplicationService;
        _unitOfWork = unitOfWork;
        _moduleLookupService = moduleLookupService;
        _certificateTypeRepository = certificateTypeRepository;
        _propertyDetailsRepository = propertyDetailsRepository;
        _propertyRepository = propertyRepository;
        _societyRepository = societyRepository;
        _wingDetailsMastRepository = wingDetailsMastRepository;
        _wingRepository = wingRepository;
        _publisher = publisher;
        _logger = logger;
        _rateableValueApiClient = rateableValueApiClient;
        _retrospectiveTaxEngine = retrospectiveTaxEngine;
        _propertyCertificateRepository = propertyCertificateRepository;
        _documentBindingRepository = documentBindingRepository;
        _documentRepository = documentRepository;
        _serviceProvider = serviceProvider;
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
        // EntityType is always "P" for both -- per CK_PropertyCertificates_EntityScope, 'F' is not
        // a valid EntityType; PropertyDetailsId NULL vs NOT NULL is what distinguishes them.
        // This also excludes Society/Wing-scoped rows (EntityType "S"/"W"): those are stored under a
        // representative unit's PropertyId with PropertyDetailsId == null too, so without this
        // check a unit that happens to be that representative would have its building's
        // Society/Wing certificate misreported as its own property-wise certificate.
        var scopedCertificates = existingCertificates
            .Where(c => c.PropertyDetailsId == propertyDetailsId && c.EntityType == "P")
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
                PropertyDetailsId = propertyDetailsId,
                EntityType = hasCertificate && certificate != null ? certificate.EntityType : null,
                SocietyDetailId = hasCertificate && certificate != null ? certificate.SocietyDetailId : null,
                WingDetailId = hasCertificate && certificate != null ? certificate.WingDetailId : null
            };
        }).ToList();

        return result;
    }

    public async Task<List<PropertyCertificateWithStatusDto>> GetSocietyOrWingCertificateTypesWithStatusAsync(
        int? societyDetailId,
        int? wingDetailId,
        CancellationToken cancellationToken = default)
    {
        if (!societyDetailId.HasValue && !wingDetailId.HasValue)
            throw new ArgumentException("Either societyDetailId or wingDetailId must be provided.");

        var isWing = wingDetailId.HasValue;
        var targetEntityType = isWing ? "W" : "S";

        // 1. Fetch all active master certificate types
        var allTypes = (await _certificateTypeRepository.GetAsync(
            t => t.IsActive,
            cancellationToken))
            .OrderBy(t => t.DisplayOrder)
            .ToList();

        // 2. Resolve repositories
        using var scope = _serviceProvider?.CreateScope();
        var sp = scope?.ServiceProvider;

        var certRepo = _propertyCertificateRepository ?? sp?.GetService<IRepository<PropertyCertificateEntity>>();
        var docBindingRepo = _documentBindingRepository ?? sp?.GetService<IRepository<DocumentBindingEntity>>();
        var docRepo = _documentRepository ?? sp?.GetService<IRepository<DocumentEntity>>();

        Dictionary<int, PropertyCertificateEntity> certLookup = new();
        Dictionary<int, DocumentBindingEntity> docBindings = new();
        Dictionary<int, DocumentEntity> docs = new();

        if (certRepo != null)
        {
            var certs = isWing
                ? await certRepo.GetAsync(c => c.EntityType == targetEntityType && c.WingDetailId == wingDetailId.Value && !c.MarkedForDeletion, cancellationToken)
                : await certRepo.GetAsync(c => c.EntityType == targetEntityType && c.SocietyDetailId == societyDetailId.Value && !c.MarkedForDeletion, cancellationToken);

            certLookup = certs
                .GroupBy(c => c.CertificateTypeId)
                .ToDictionary(g => g.Key, g => g.First());

            var bindingIds = certs.Where(c => c.DocumentBindingId.HasValue).Select(c => c.DocumentBindingId!.Value).Distinct().ToList();

            if (bindingIds.Any() && docBindingRepo != null)
            {
                var bindings = await docBindingRepo.GetAsync(b => bindingIds.Contains(b.Id), cancellationToken);
                docBindings = bindings.ToDictionary(b => b.Id);

                var docIds = bindings.Select(b => b.DocumentId).Distinct().ToList();
                if (docIds.Any() && docRepo != null)
                {
                    var docList = await docRepo.GetAsync(d => docIds.Contains(d.Id), cancellationToken);
                    docs = docList.ToDictionary(d => d.Id);
                }
            }
        }

        // 3. Build response combining all types with their status
        return allTypes.Select(type =>
        {
            var hasCert = certLookup.TryGetValue(type.Id, out var cert);
            DocumentBindingEntity? binding = null;
            DocumentEntity? doc = null;

            if (hasCert && cert?.DocumentBindingId.HasValue == true && docBindings.TryGetValue(cert.DocumentBindingId.Value, out binding))
            {
                docs.TryGetValue(binding.DocumentId, out doc);
            }

            return new PropertyCertificateWithStatusDto
            {
                CertificateTypeId = type.Id,
                CertificateTypeName = type.CertificateTypeName,
                CertificateTypeCode = type.CertificateTypeCode,
                IsProtected = type.IsProtected,
                IsRequired = type.IsRequired,
                DisplayOrder = type.DisplayOrder,
                HasCertificate = hasCert,
                PropertyCertificateId = hasCert && cert != null ? cert.Id : null,
                IsActive = hasCert && cert != null && cert.IsActive,
                CertificateNo = hasCert && cert != null ? cert.CertificateNo : null,
                IssueDate = hasCert && cert != null ? cert.IssueDate : null,
                DocumentGuid = doc?.DocumentGuid,
                FileName = doc?.FileName,
                PropertyDetailsId = null,
                EntityType = targetEntityType,
                SocietyDetailId = societyDetailId,
                WingDetailId = wingDetailId
            };
        }).ToList();
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
                AuthReferenceId = certificate.PropertyId ?? 0,
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
                PropertyId = certificate.PropertyId ?? 0,
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
        // active CC -- cheaper and safer than letting the tax engine silently resolve an invalid
        // CC/OC date order after the fact for data that should never have been savable in the
        // first place.
        ValidateCcOcDateOrder(bulkDto, typeCodeByTypeId, existingCertificates);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // GO-LIVE BLOCKER fix: selecting multiple floors in one bulk save previously let EACH
            // certificate row publish its own PropertyCertificateChangedEvent inline (via
            // CreateAsync/UpdateAsync/ToggleEnabledAsync), so the tax engine ran once per floor
            // within the SAME request -- e.g. 3 floors meant 3 separate recalculation passes, each
            // re-querying and re-upserting the same property-aggregated PolicyTaxDetails/TransMast
            // rows for the SAME (PropertyId, PolicyCodeId/FinanceYearId, TaxId) slots, sometimes
            // colliding on PTIS.PolicyTaxDetails' and PTIS.TransMast's unique keys. Final
            // persistence is always property-wise, so there is no reason to recompute it mid-batch:
            // every CreateAsync/UpdateAsync/ToggleEnabledAsync call below suppresses its own
            // publish, and exactly ONE PropertyCertificateChangedEvent is published after every
            // certificate in this batch has been saved, reflecting the FINAL state of all selected
            // floors at once.
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
                                suppressRecalculation: true,
                                entityType: certDto.EntityType ?? "P",
                                societyDetailId: certDto.SocietyDetailId,
                                wingDetailId: certDto.WingDetailId);

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

            // Exactly one recalculation for the whole batch -- a save-shaped change
            // (enable/create/update) or a delete-shaped change (disable) either one is enough to
            // run the pipeline once, since it recomputes from the FINAL DB state of every
            // certificate regardless of which specific row changed.
            if (saveRecalculationNeeded || deleteRecalculationNeeded)
            {
                await _publisher.Publish(
                    new PropertyCertificateChangedEvent(bulkDto.PropertyId, userId), cancellationToken);
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
            // 1. Soft-delete the document and physical file via DocumentApplicationService
            if (documentGuid.HasValue)
            {
                await _documentApplicationService.DeleteDocumentAsync(documentGuid.Value, deletedBy, cancellationToken);
            }

            _logger.LogInformation("PropertyCertificate document deleted: PropertyCertificateId={PropertyCertificateId}, DocumentGuid={DocumentGuid}",
                propertyCertificateId, documentGuid);

            // Removing the document invalidates this certificate for tax purposes, so re-run the
            // certificate-change pipeline (RV refresh then Retrospective Tax Engine) -- but only
            // when the certificate type is IsTaxable (same gating as every other mutation path).
            if (certificate.CertificateType?.IsTaxable == true)
            {
                if (certificate.PropertyId.HasValue)
                {
                    await _publisher.Publish(
                        new PropertyCertificateChangedEvent(certificate.PropertyId.Value, deletedBy), cancellationToken);
                }
                else
                {
                    var memberPropertyIds = await ResolveMemberPropertyIdsAsync(
                        certificate.EntityType, certificate.SocietyDetailId, certificate.WingDetailId, cancellationToken);
                    foreach (var memberPropertyId in memberPropertyIds)
                    {
                        await _publisher.Publish(
                            new PropertyCertificateChangedEvent(memberPropertyId, deletedBy), cancellationToken);
                    }
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

        // EntityType is always "P" for property/floor scope (see GetCertificateTypesWithStatusAsync);
        // this also excludes Society/Wing-scoped rows (EntityType "S"/"W") sharing this PropertyId
        // with PropertyDetailsId == null.
        var match = existingCertificates.FirstOrDefault(c =>
            c.CertificateTypeId == certificateTypeId &&
            c.PropertyDetailsId == propertyDetailsId &&
            c.EntityType == "P" &&
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
        CancellationToken cancellationToken = default,
        string entityType = "P",
        int? societyDetailId = null,
        int? wingDetailId = null)
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

        // EntityType is always "P" for property/floor scope (see GetCertificateTypesWithStatusAsync);
        // this also excludes Society/Wing-scoped rows (EntityType "S"/"W") sharing this PropertyId
        // with PropertyDetailsId == null.
        var match = existingCertificates.FirstOrDefault(c =>
            c.CertificateTypeId == certificateTypeId &&
            c.PropertyDetailsId == oldPropertyDetailsId &&
            c.EntityType == "P" &&
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
                suppressRecalculation: true,
                entityType: entityType,
                societyDetailId: societyDetailId,
                wingDetailId: wingDetailId);

            if (isTaxableType)
            {
                await _publisher.Publish(new PropertyCertificateChangedEvent(propertyId, userId), cancellationToken);
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

        var electricBillCodes = new[] { CertificateTypeCodes.ElectricBill };

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
    /// Matches a certificate type against ANY of several codes (CC/OC/Electric Bill each have
    /// exactly one, from <see cref="CertificateTypeCodes"/>), preferring CertificateTypeCode when
    /// populated and falling back to the display-name heuristic when it isn't (older/seed data may
    /// not have codes backfilled yet) — mirrors the Retrospective Tax Engine's own evidence
    /// resolution so this endpoint and the tax engine never disagree about which certificate is
    /// CC/OC/Electric Bill.
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
        Guard.AgainstNegativeOrZero(request.CertificateTypeId, nameof(request.CertificateTypeId));
        Guard.AgainstNegativeOrZero(userId, nameof(userId));

        // Floor scope resolves to "P" here too (see the else branch below) -- 'F' is not a
        // valid EntityType per CK_PropertyCertificates_EntityScope.
        string entityType = !string.IsNullOrWhiteSpace(request.EntityType)
            ? request.EntityType.Trim().ToUpperInvariant()
            : (request.CertificateScope == CertificateScope.Society ? "S" :
               request.CertificateScope == CertificateScope.Wing ? "W" : "P");

        if (entityType == "S" || request.CertificateScope == CertificateScope.Society)
        {
            entityType = "S";
            request.CertificateScope = CertificateScope.Society;
            request.WingDetailId = null;
            request.PropertyDetailsId = null;
            request.PropertyId = null;
            Guard.AgainstNegativeOrZero(request.SocietyDetailId ?? 0, nameof(request.SocietyDetailId));
        }
        else if (entityType == "W" || request.CertificateScope == CertificateScope.Wing)
        {
            entityType = "W";
            request.CertificateScope = CertificateScope.Wing;
            request.PropertyDetailsId = null;
            request.PropertyId = null;
            Guard.AgainstNegativeOrZero(request.SocietyDetailId ?? 0, nameof(request.SocietyDetailId));
            Guard.AgainstNegativeOrZero(request.WingDetailId ?? 0, nameof(request.WingDetailId));
        }
        else
        {
            // EntityType is always "P" here, for both Property and Floor scope -- per
            // CK_PropertyCertificates_EntityScope, 'F' is not a valid EntityType at all;
            // PropertyDetailsId NULL vs NOT NULL on a 'P' row is what distinguishes property-wise
            // from floor-wise (see PropertyCertificateEntity.ValidateEntityScope).
            entityType = "P";
            request.CertificateScope = request.PropertyDetailsId.HasValue
                ? CertificateScope.Floor
                : CertificateScope.Property;
            Guard.AgainstNegativeOrZero(request.PropertyId ?? 0, nameof(request.PropertyId));
        }

        var certificateType = await _certificateTypeRepository.GetByIdAsync(request.CertificateTypeId, cancellationToken);
        if (certificateType == null || !certificateType.IsActive)
        {
            throw new InvalidOperationException($"Certificate type {request.CertificateTypeId} was not found or is inactive.");
        }

        // Find the existing row for this exact (PropertyId, EntityType, SocietyDetailId, WingDetailId, PropertyDetailsId, CertificateTypeId) scope.
        // A Society/Wing-scoped row has no PropertyId to look up by, so it's resolved directly by
        // EntityType + SocietyDetailId/WingDetailId instead (same lookup GetSocietyOrWingCertificateTypesWithStatusAsync uses).
        var existingCertificates = entityType == "S" || entityType == "W"
            ? await GetSocietyOrWingCertificatesRawAsync(entityType, request.SocietyDetailId, request.WingDetailId, cancellationToken)
            : await _propertyCertificateService.GetByPropertyIdIncludingInactiveAsync(
                request.PropertyId!.Value,
                PropertyCertificateIncludeOptions.DocumentBinding | PropertyCertificateIncludeOptions.Document,
                cancellationToken);

        var existing = existingCertificates.FirstOrDefault(c =>
            c.CertificateTypeId == request.CertificateTypeId &&
            (c.EntityType ?? "P") == entityType &&
            c.SocietyDetailId == request.SocietyDetailId &&
            c.WingDetailId == request.WingDetailId &&
            c.PropertyDetailsId == request.PropertyDetailsId);

        // Validate CC/OC date order upfront against the final proposed state
        var certTypes = await _certificateTypeRepository.GetAsync(_ => true, cancellationToken);
        var typeCodeByTypeId = certTypes.ToDictionary(t => t.Id, t => t.CertificateTypeCode);
        var singleBulkDto = new PropertyCertificateBulkSaveDto
        {
            PropertyId = request.PropertyId ?? 0,
            Certificates = new List<PropertyCertificateItemDto>
            {
                new PropertyCertificateItemDto
                {
                    CertificateTypeId = request.CertificateTypeId,
                    IsEnabled = true,
                    CertificateNumber = request.CertificateNo,
                    CertificateDate = request.CertificateIssueDate,
                    PropertyCertificateId = existing?.Id,
                    PropertyDetailsId = request.PropertyDetailsId
                }
            }
        };
        ValidateCcOcDateOrder(singleBulkDto, typeCodeByTypeId, existingCertificates);

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
                    request.PropertyDetailsId,
                    suppressRecalculation: true,
                    entityType: entityType,
                    societyDetailId: request.SocietyDetailId,
                    wingDetailId: request.WingDetailId);
            }
            else
            {
                propertyCertificateId = existing.Id;
                await _propertyCertificateService.UpdateAsync(
                    propertyCertificateId,
                    request.CertificateNo,
                    request.CertificateIssueDate,
                    userId,
                    cancellationToken,
                    suppressRecalculation: true);

                if (!existing.IsActive)
                {
                    await _propertyCertificateService.ToggleEnabledAsync(
                        propertyCertificateId,
                        true,
                        userId,
                        cancellationToken,
                        suppressRecalculation: true);
                }
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        // Property/Floor-scoped row recalculates its one PropertyId -- unchanged, fire-and-forget,
        // so the save response returns immediately. A Society/Wing-scoped row has no PropertyId of
        // its own, so every property currently under that society/wing needs recalculating instead;
        // business requires this save to report back how many succeeded and how many failed (in
        // plain language, not a raw error), so that path runs synchronously here instead of
        // publishing fire-and-forget events for the background queue to pick up later.
        PropertyTaxRecalculationSummaryDto? recalculationSummary = null;
        if (certificateType.IsTaxable)
        {
            if (request.PropertyId.HasValue)
            {
                await _publisher.Publish(
                    new PropertyCertificateChangedEvent(request.PropertyId.Value, userId), cancellationToken);
            }
            else
            {
                var memberPropertyIds = await ResolveMemberPropertyIdsAsync(entityType, request.SocietyDetailId, request.WingDetailId, cancellationToken);
                var summary = new PropertyTaxRecalculationSummaryDto { TotalProperties = memberPropertyIds.Count };

                foreach (var memberPropertyId in memberPropertyIds)
                {
                    try
                    {
                        await _rateableValueApiClient.RecalculateAsync(memberPropertyId, cancellationToken);
                        await _retrospectiveTaxEngine.CalculateAndSaveAsync(memberPropertyId, userId, cancellationToken);
                        summary.SucceededCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Tax recalculation failed for PropertyId={PropertyId} after {EntityType}-scoped certificate save (SocietyDetailId={SocietyDetailId}, WingDetailId={WingDetailId}).",
                            memberPropertyId, entityType, request.SocietyDetailId, request.WingDetailId);
                        summary.FailedCount++;
                        summary.Failures.Add(new PropertyTaxRecalculationFailureDto
                        {
                            PropertyId = memberPropertyId,
                            Reason = "Tax could not be recalculated for this property. Please try again, or contact support if this keeps happening."
                        });
                    }
                }

                recalculationSummary = summary;
            }
        }

        return new SaveCertificateResponseDto
        {
            PropertyCertificateId = propertyCertificateId,
            PropertyId = request.PropertyId ?? 0,
            PropertyDetailsId = request.PropertyDetailsId,
            CertificateScope = request.CertificateScope,
            CertificateTypeId = request.CertificateTypeId,
            CertificateNo = request.CertificateNo,
            CertificateIssueDate = request.CertificateIssueDate,
            DocumentGuid = existing?.DocumentBinding?.Document?.DocumentGuid,
            DocumentBindingId = existing?.DocumentBindingId,
            TaxRecalculationTriggered = certificateType.IsTaxable,
            RecalculationSummary = recalculationSummary,
            EntityType = request.EntityType ?? existing?.EntityType,
            SocietyDetailId = request.SocietyDetailId ?? existing?.SocietyDetailId,
            WingDetailId = request.WingDetailId ?? existing?.WingDetailId
        };
    }

    /// <summary>
    /// Raw (untransformed) Society/Wing-scoped certificate rows for a given scope -- used to find
    /// an existing row to update/re-enable, since these rows have no PropertyId to look up by.
    /// Mirrors the repository resolution <see cref="GetSocietyOrWingCertificateTypesWithStatusAsync"/>
    /// already uses.
    /// </summary>
    private async Task<List<PropertyCertificateEntity>> GetSocietyOrWingCertificatesRawAsync(
        string entityType, int? societyDetailId, int? wingDetailId, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider?.CreateScope();
        var certRepo = _propertyCertificateRepository ?? scope?.ServiceProvider.GetService<IRepository<PropertyCertificateEntity>>();
        if (certRepo == null)
        {
            return new List<PropertyCertificateEntity>();
        }

        var certificates = entityType == "W"
            ? await certRepo.GetAsync(c => c.EntityType == "W" && c.WingDetailId == wingDetailId!.Value && !c.MarkedForDeletion, cancellationToken)
            : await certRepo.GetAsync(c => c.EntityType == "S" && c.SocietyDetailId == societyDetailId!.Value && !c.MarkedForDeletion, cancellationToken);

        return certificates.ToList();
    }

    /// <summary>
    /// Every active PropertyId currently under the given Society or Wing scope -- used to
    /// recalculate Retrospective Tax for every affected unit when a Society/Wing-scoped
    /// certificate (which has no PropertyId of its own) is saved. For a Society, this is the
    /// society's own representative property (SocietyDetailsMast.PropertyId, e.g. the "apartment
    /// society property" with no partition) plus every unit under each of its wings; for a Wing,
    /// it's every unit directly under that wing.
    /// </summary>
    private async Task<List<int>> ResolveMemberPropertyIdsAsync(
        string entityType, int? societyDetailId, int? wingDetailId, CancellationToken cancellationToken)
    {
        var memberIds = new HashSet<int>();

        if (entityType == "W")
        {
            if (!wingDetailId.HasValue) return memberIds.ToList();

            var wingUnitIds = await _propertyRepository.GetQueryable()
                .Where(p => p.WingDetailId == wingDetailId.Value && p.IsActive && !p.MarkedForDeletion)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
            foreach (var id in wingUnitIds) memberIds.Add(id);
            return memberIds.ToList();
        }

        if (entityType == "S")
        {
            if (!societyDetailId.HasValue) return memberIds.ToList();

            var representativePropertyId = await _societyRepository.GetQueryable()
                .Where(s => s.Id == societyDetailId.Value)
                .Select(s => s.PropertyId)
                .FirstOrDefaultAsync(cancellationToken);
            if (representativePropertyId.HasValue) memberIds.Add(representativePropertyId.Value);

            var wingIds = await _wingDetailsMastRepository.GetQueryable()
                .Where(w => w.SocietyDetailsMastId == societyDetailId.Value && w.IsActive && !w.MarkedForDeletion)
                .Select(w => w.Id)
                .ToListAsync(cancellationToken);

            if (wingIds.Count > 0)
            {
                var unitIds = await _propertyRepository.GetQueryable()
                    .Where(p => p.WingDetailId.HasValue && wingIds.Contains(p.WingDetailId.Value) && p.IsActive && !p.MarkedForDeletion)
                    .Select(p => p.Id)
                    .ToListAsync(cancellationToken);
                foreach (var id in unitIds) memberIds.Add(id);
            }
        }

        return memberIds.ToList();
    }

    public async Task<CreateCertificateRecordResponseDto> CreateCertificateRecordAsync(
        CreateCertificateRecordRequestDto request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(request.SocietyDetailId, nameof(request.SocietyDetailId));
        Guard.AgainstNegativeOrZero(request.CertificateTypeId, nameof(request.CertificateTypeId));
        Guard.AgainstNegativeOrZero(userId, nameof(userId));

        var certificateType = await _certificateTypeRepository.GetByIdAsync(request.CertificateTypeId, cancellationToken);
        if (certificateType == null || !certificateType.IsActive)
        {
            throw new InvalidOperationException($"Certificate type {request.CertificateTypeId} was not found or is inactive.");
        }

        if (request.Level == CertificateRecordLevel.Apartment)
        {
            var saveResult = await SaveCertificateAsync(new SaveCertificateRequestDto
            {
                EntityType = "S",
                SocietyDetailId = request.SocietyDetailId,
                CertificateTypeId = request.CertificateTypeId,
                CertificateNo = request.CertificateNo,
                CertificateIssueDate = request.CertificateIssueDate
            }, userId, cancellationToken);

            var societyMemberIds = await ResolveMemberPropertyIdsAsync("S", request.SocietyDetailId, null, cancellationToken);

            return new CreateCertificateRecordResponseDto
            {
                EffectiveScope = "Society",
                PropertyCertificateIds = new List<int> { saveResult.PropertyCertificateId },
                UnitCount = societyMemberIds.Count,
                TaxRecalculationTriggered = saveResult.TaxRecalculationTriggered,
                RecalculationSummary = saveResult.RecalculationSummary
            };
        }

        // Wing and Unit levels both need the wing's full member list -- Wing level to report
        // UnitCount, Unit level to additionally decide whether the selection covers every unit
        // (collapses to a single Wing-scoped row) or only some (one Property-scoped row per unit).
        if (!request.WingDetailId.HasValue || request.WingDetailId.Value <= 0)
        {
            throw new ArgumentException("WingDetailId is required for Wing and Unit level certificate records.", nameof(request.WingDetailId));
        }

        var wingMemberIds = await ResolveMemberPropertyIdsAsync("W", request.SocietyDetailId, request.WingDetailId, cancellationToken);

        if (request.Level == CertificateRecordLevel.Wing)
        {
            var saveResult = await SaveCertificateAsync(new SaveCertificateRequestDto
            {
                EntityType = "W",
                SocietyDetailId = request.SocietyDetailId,
                WingDetailId = request.WingDetailId,
                CertificateTypeId = request.CertificateTypeId,
                CertificateNo = request.CertificateNo,
                CertificateIssueDate = request.CertificateIssueDate
            }, userId, cancellationToken);

            return new CreateCertificateRecordResponseDto
            {
                EffectiveScope = "Wing",
                PropertyCertificateIds = new List<int> { saveResult.PropertyCertificateId },
                UnitCount = wingMemberIds.Count,
                TaxRecalculationTriggered = saveResult.TaxRecalculationTriggered,
                RecalculationSummary = saveResult.RecalculationSummary
            };
        }

        // Unit level.
        if (request.UnitPropertyIds == null || request.UnitPropertyIds.Count == 0)
        {
            throw new ArgumentException("UnitPropertyIds is required and must contain at least one PropertyId for Unit level certificate records.", nameof(request.UnitPropertyIds));
        }

        var selectedUnitIds = request.UnitPropertyIds.Distinct().ToList();
        var wingMemberIdSet = new HashSet<int>(wingMemberIds);
        var unknownUnitIds = selectedUnitIds.Where(id => !wingMemberIdSet.Contains(id)).ToList();
        if (unknownUnitIds.Count > 0)
        {
            throw new ArgumentException(
                $"The following PropertyIds are not units under WingDetailId {request.WingDetailId}: {string.Join(", ", unknownUnitIds)}.",
                nameof(request.UnitPropertyIds));
        }

        // Every unit under the wing selected -- functionally identical to picking Wing level
        // directly, so it's stored the same single-row way rather than one row per unit.
        if (selectedUnitIds.Count == wingMemberIdSet.Count)
        {
            var saveResult = await SaveCertificateAsync(new SaveCertificateRequestDto
            {
                EntityType = "W",
                SocietyDetailId = request.SocietyDetailId,
                WingDetailId = request.WingDetailId,
                CertificateTypeId = request.CertificateTypeId,
                CertificateNo = request.CertificateNo,
                CertificateIssueDate = request.CertificateIssueDate
            }, userId, cancellationToken);

            return new CreateCertificateRecordResponseDto
            {
                EffectiveScope = "Wing",
                PropertyCertificateIds = new List<int> { saveResult.PropertyCertificateId },
                UnitCount = wingMemberIds.Count,
                TaxRecalculationTriggered = saveResult.TaxRecalculationTriggered,
                RecalculationSummary = saveResult.RecalculationSummary
            };
        }

        // Partial selection -- one Property-scoped row per selected unit, each carrying
        // SocietyDetailId/WingDetailId alongside its own PropertyId.
        var createdIds = new List<int>();
        var taxRecalculationTriggered = false;
        foreach (var unitPropertyId in selectedUnitIds)
        {
            var saveResult = await SaveCertificateAsync(new SaveCertificateRequestDto
            {
                EntityType = "P",
                PropertyId = unitPropertyId,
                SocietyDetailId = request.SocietyDetailId,
                WingDetailId = request.WingDetailId,
                CertificateTypeId = request.CertificateTypeId,
                CertificateNo = request.CertificateNo,
                CertificateIssueDate = request.CertificateIssueDate
            }, userId, cancellationToken);

            createdIds.Add(saveResult.PropertyCertificateId);
            taxRecalculationTriggered = taxRecalculationTriggered || saveResult.TaxRecalculationTriggered;
        }

        return new CreateCertificateRecordResponseDto
        {
            EffectiveScope = "Unit",
            PropertyCertificateIds = createdIds,
            UnitCount = selectedUnitIds.Count,
            TaxRecalculationTriggered = taxRecalculationTriggered
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

    public async Task<List<object>> GetCertificateTypeMasterAsync(CancellationToken cancellationToken = default)
    {
        var types = await _certificateTypeRepository.GetQueryable()
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .Select(t => new
            {
                certificateTypeId = t.Id,
                certificateTypeCode = t.CertificateTypeCode,
                certificateTypeName = t.CertificateTypeName,
                description = t.Description,
                displayOrder = t.DisplayOrder,
                badgeCode = t.CertificateTypeCode != null && t.CertificateTypeCode.Length >= 2 ? t.CertificateTypeCode.Substring(0, 2).ToUpper() : "DOC"
            })
            .ToListAsync(cancellationToken);

        return types.Cast<object>().ToList();
    }

    public async Task<List<object>> GetWingsByPropertyAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        var wingMap = new Dictionary<string, (int wingDetailId, string wingName)>();

        var wingDetailsQuery = _wingDetailsMastRepository.GetQueryable().AsNoTracking()
            .Where(wd => wd.IsActive && !wd.MarkedForDeletion);

        var wingQuery = _wingRepository.GetQueryable().AsNoTracking()
            .Where(wm => wm.IsActive);

        var societyQuery = _societyRepository.GetQueryable().AsNoTracking()
            .Where(s => s.IsActive && !s.MarkedForDeletion);

        var propertyQuery = _propertyRepository.GetQueryable().AsNoTracking()
            .Where(pm => pm.IsActive && !pm.MarkedForDeletion);

        // Path 0 (PRIMARY & DIRECT): SocietyDetailsMast linked directly to target PropertyId
        var directSocietyWings = await (
            from s in societyQuery
            where s.PropertyId == propertyId
            join wd in wingDetailsQuery on s.Id equals wd.SocietyDetailsMastId
            join wm in wingQuery on wd.WingMasterId equals wm.Id into wmJoin
            from wm in wmJoin.DefaultIfEmpty()
            select new
            {
                WingDetailId = wd.Id,
                WingName = wd.WingName ?? (wm != null ? wm.WingNo : null),
                WingNo = wm != null ? wm.WingNo : null
            })
            .ToListAsync(cancellationToken);

        foreach (var w in directSocietyWings)
        {
            var name = w.WingName ?? w.WingNo;
            if (!string.IsNullOrWhiteSpace(name) && !wingMap.ContainsKey(name))
            {
                wingMap[name] = (w.WingDetailId, name);
            }
        }

        // If exact society wings exist for this property, return them directly to avoid mixing with unrelated properties
        if (wingMap.Count > 0)
        {
            return wingMap.Values
                .Select(w => (object)new
                {
                    wingDetailId = w.wingDetailId,
                    wingName = w.wingName
                })
                .ToList();
        }

        var targetProperty = await _propertyRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.IsActive && !p.MarkedForDeletion, cancellationToken);

        if (targetProperty != null)
        {
            var targetWardId = targetProperty.WardId;
            var targetPropertyNo = targetProperty.PropertyNo ?? string.Empty;
            var cleanPropertyNo = targetPropertyNo.Contains('/') ? targetPropertyNo.Split('/')[0].Trim() : targetPropertyNo.Trim();

            // Path 1: Target property's own WingDetailId -> find parent society -> find all society wings
            if (targetProperty.WingDetailId.HasValue)
            {
                var targetWing = await wingDetailsQuery
                    .FirstOrDefaultAsync(wd => wd.Id == targetProperty.WingDetailId.Value, cancellationToken);

                if (targetWing != null && targetWing.SocietyDetailsMastId > 0)
                {
                    var societyWings = await (
                        from wd in wingDetailsQuery
                        where wd.SocietyDetailsMastId == targetWing.SocietyDetailsMastId
                        join wm in wingQuery on wd.WingMasterId equals wm.Id into wmJoin
                        from wm in wmJoin.DefaultIfEmpty()
                        select new
                        {
                            WingDetailId = wd.Id,
                            WingName = wd.WingName ?? (wm != null ? wm.WingNo : null),
                            WingNo = wm != null ? wm.WingNo : null
                        })
                        .ToListAsync(cancellationToken);

                    foreach (var w in societyWings)
                    {
                        var name = w.WingName ?? w.WingNo;
                        if (!string.IsNullOrWhiteSpace(name) && !wingMap.ContainsKey(name))
                        {
                            wingMap[name] = (w.WingDetailId, name);
                        }
                    }

                    if (wingMap.Count > 0)
                    {
                        return wingMap.Values
                            .Select(w => (object)new
                            {
                                wingDetailId = w.wingDetailId,
                                wingName = w.wingName
                            })
                            .ToList();
                    }
                }
            }

            // Path 2: Sibling properties sharing WardId + PropertyNo (exact or prefix) with WingDetailId
            var wingsFromProperties = await (
                from pm in propertyQuery
                where pm.WardId == targetWardId &&
                      (pm.PropertyNo == targetPropertyNo || pm.PropertyNo == cleanPropertyNo || (pm.PropertyNo != null && pm.PropertyNo.StartsWith(cleanPropertyNo))) &&
                      pm.WingDetailId.HasValue
                join wd in wingDetailsQuery on pm.WingDetailId!.Value equals wd.Id
                join wm in wingQuery on wd.WingMasterId equals wm.Id into wmJoin
                from wm in wmJoin.DefaultIfEmpty()
                select new
                {
                    WingDetailId = wd.Id,
                    WingName = wd.WingName ?? (wm != null ? wm.WingNo : null),
                    WingNo = wm != null ? wm.WingNo : null
                })
                .ToListAsync(cancellationToken);

            foreach (var w in wingsFromProperties)
            {
                var name = w.WingName ?? w.WingNo;
                if (!string.IsNullOrWhiteSpace(name) && !wingMap.ContainsKey(name))
                {
                    wingMap[name] = (w.WingDetailId, name);
                }
            }

            // Path 3: SocietyDetailsMast linked to target property or sibling properties
            var wingsFromSociety = await (
                from s in societyQuery
                join sp in propertyQuery on s.PropertyId equals sp.Id
                where sp.WardId == targetWardId && (sp.PropertyNo == targetPropertyNo || sp.PropertyNo == cleanPropertyNo || (sp.PropertyNo != null && sp.PropertyNo.StartsWith(cleanPropertyNo)))
                join wd in wingDetailsQuery on s.Id equals wd.SocietyDetailsMastId
                join wm in wingQuery on wd.WingMasterId equals wm.Id into wmJoin
                from wm in wmJoin.DefaultIfEmpty()
                select new
                {
                    WingDetailId = wd.Id,
                    WingName = wd.WingName ?? (wm != null ? wm.WingNo : null),
                    WingNo = wm != null ? wm.WingNo : null
                })
                .ToListAsync(cancellationToken);

            foreach (var w in wingsFromSociety)
            {
                var name = w.WingName ?? w.WingNo;
                if (!string.IsNullOrWhiteSpace(name) && !wingMap.ContainsKey(name))
                {
                    wingMap[name] = (w.WingDetailId, name);
                }
            }
        }

        return wingMap.Values
            .Select(w => (object)new
            {
                wingDetailId = w.wingDetailId,
                wingName = w.wingName
            })
            .ToList();
    }

    public async Task<List<object>> GetUnitsByPropertyAsync(int propertyId, int? wingDetailId = null, CancellationToken cancellationToken = default)
    {
        var propertyDetailsQuery = _propertyDetailsRepository.GetQueryable()
            .AsNoTracking()
            .Where(pd => pd.IsActive && !pd.MarkedForDeletion);

        var propertyQuery = _propertyRepository.GetQueryable()
            .AsNoTracking()
            .Where(p => p.IsActive && !p.MarkedForDeletion);

        var wingDetailsQuery = _wingDetailsMastRepository.GetQueryable()
            .AsNoTracking()
            .Where(wd => wd.IsActive && !wd.MarkedForDeletion);

        var wingQuery = _wingRepository.GetQueryable()
            .AsNoTracking()
            .Where(wm => wm.IsActive);

        // 1. If wingDetailId is specified, fetch all units linked to that wingDetailId via PropertyMast
        if (wingDetailId.HasValue && wingDetailId.Value > 0)
        {
            var wingUnits = await (
                from pm in propertyQuery
                where pm.WingDetailId == wingDetailId.Value
                join wd in wingDetailsQuery on pm.WingDetailId.Value equals wd.Id into wdJoin
                from wd in wdJoin.DefaultIfEmpty()
                join wm in wingQuery on wd.WingMasterId equals wm.Id into wmJoin
                from wm in wmJoin.DefaultIfEmpty()
                join pd in propertyDetailsQuery on pm.Id equals pd.PropertyId into pdJoin
                from pd in pdJoin.DefaultIfEmpty()
                select new
                {
                    PropertyId = pm.Id,
                    PropertyDetailsId = pd != null ? pd.Id : pm.Id,
                    PartitionNo = pm.PartitionNo ?? pm.PropertyNo ?? $"Unit {pm.Id}",
                    WingDetailId = pm.WingDetailId ?? 0,
                    WingName = wd != null ? (wd.WingName ?? (wm != null ? wm.WingNo : "Wing")) : "Wing",
                    FloorId = pd != null ? pd.FloorId : 1,
                    UseId = pd != null ? pd.TypeOfUseId : 1
                }
            ).ToListAsync(cancellationToken);

            if (wingUnits.Count > 0)
            {
                return wingUnits.Select(u => (object)new
                {
                    propertyId = u.PropertyId,
                    propertyDetailsId = u.PropertyDetailsId,
                    unitNo = !string.IsNullOrWhiteSpace(u.PartitionNo) ? u.PartitionNo : $"Unit {u.PropertyId}",
                    wingDetailId = u.WingDetailId,
                    wingName = u.WingName,
                    floorId = u.FloorId,
                    floorName = "1st Floor",
                    useId = u.UseId,
                    useName = "Residential",
                    isSelected = false
                }).ToList();
            }
        }

        // 2. Fallback: query target property and its siblings sharing WardId + PropertyNo
        var targetProperty = await propertyQuery.FirstOrDefaultAsync(p => p.Id == propertyId, cancellationToken);
        if (targetProperty != null)
        {
            var targetWardId = targetProperty.WardId;
            var targetPropertyNo = targetProperty.PropertyNo ?? string.Empty;
            var cleanPropertyNo = targetPropertyNo.Contains('/') ? targetPropertyNo.Split('/')[0].Trim() : targetPropertyNo.Trim();

            var siblingUnits = await (
                from pm in propertyQuery
                where pm.WardId == targetWardId &&
                      (pm.PropertyNo == targetPropertyNo || pm.PropertyNo == cleanPropertyNo || (pm.PropertyNo != null && pm.PropertyNo.StartsWith(cleanPropertyNo)))
                join wd in wingDetailsQuery on pm.WingDetailId equals wd.Id into wdJoin
                from wd in wdJoin.DefaultIfEmpty()
                join wm in wingQuery on wd.WingMasterId equals wm.Id into wmJoin
                from wm in wmJoin.DefaultIfEmpty()
                join pd in propertyDetailsQuery on pm.Id equals pd.PropertyId into pdJoin
                from pd in pdJoin.DefaultIfEmpty()
                select new
                {
                    PropertyId = pm.Id,
                    PropertyDetailsId = pd != null ? pd.Id : pm.Id,
                    PartitionNo = pm.PartitionNo ?? pm.PropertyNo ?? $"Unit {pm.Id}",
                    WingDetailId = pm.WingDetailId ?? 0,
                    WingName = wd != null ? (wd.WingName ?? (wm != null ? wm.WingNo : "Wing")) : "Wing",
                    FloorId = pd != null ? pd.FloorId : 1,
                    UseId = pd != null ? pd.TypeOfUseId : 1
                }
            ).ToListAsync(cancellationToken);

            if (siblingUnits.Count > 0)
            {
                return siblingUnits.Select(u => (object)new
                {
                    propertyId = u.PropertyId,
                    propertyDetailsId = u.PropertyDetailsId,
                    unitNo = !string.IsNullOrWhiteSpace(u.PartitionNo) ? u.PartitionNo : $"Unit {u.PropertyId}",
                    wingDetailId = u.WingDetailId,
                    wingName = u.WingName,
                    floorId = u.FloorId,
                    floorName = "1st Floor",
                    useId = u.UseId,
                    useName = "Residential",
                    isSelected = false
                }).ToList();
            }
        }

        // 3. Final Fallback: Direct PropertyDetails for target propertyId
        var floors = await propertyDetailsQuery
            .Where(pd => pd.PropertyId == propertyId)
            .Include(pd => pd.Floor)
            .Include(pd => pd.TypeOfUse)
            .ToListAsync(cancellationToken);

        return floors.Select(pd => (object)new
        {
            propertyId,
            propertyDetailsId = pd.Id,
            unitNo = $"Unit {pd.Id}",
            wingDetailId = 0,
            wingName = "Wing A",
            floorId = pd.FloorId,
            floorName = pd.Floor != null ? (pd.Floor.Description ?? "1st Floor") : "1st Floor",
            useId = pd.TypeOfUseId,
            useName = pd.TypeOfUse != null ? (pd.TypeOfUse.Description ?? "Residential") : "Residential",
            isSelected = false
        }).ToList();
    }

    public async Task<(List<object> Items, int TotalCount)> GetUnitsByPropertyPagedAsync(
        int propertyId,
        int? wingDetailId = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var allUnits = await GetUnitsByPropertyAsync(propertyId, wingDetailId, cancellationToken);
        var totalCount = allUnits.Count;
        var safePage = Math.Max(1, pageNumber);
        var safeSize = Math.Max(1, pageSize);

        var pagedItems = allUnits
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .ToList();

        return (pagedItems, totalCount);
    }

    public async Task<List<PropertyCertificateDto>> GetByPropertyIdAsync(
        int propertyId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(propertyId, nameof(propertyId));

        using var scope = _serviceProvider?.CreateScope();
        var sp = scope?.ServiceProvider;

        var certRepo = _propertyCertificateRepository ?? sp?.GetService<IRepository<PropertyCertificateEntity>>();
        var docBindingRepo = _documentBindingRepository ?? sp?.GetService<IRepository<DocumentBindingEntity>>();
        var docRepo = _documentRepository ?? sp?.GetService<IRepository<DocumentEntity>>();

        var certTypes = (await _certificateTypeRepository.GetAsync(
            pctm => pctm.IsActive,
            cancellationToken))
            .OrderBy(pctm => pctm.Id)
            .ToList();

        List<PropertyCertificateEntity> certificates = new();
        if (certRepo != null)
        {
            certificates = await certRepo.GetQueryable().AsNoTracking()
                .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
                .ToListAsync(cancellationToken);
        }

        var certLookup = certificates
            .GroupBy(c => c.CertificateTypeId)
            .ToDictionary(g => g.Key, g => g.First());

        var bindingIds = certificates
            .Where(c => c.DocumentBindingId.HasValue)
            .Select(c => c.DocumentBindingId!.Value)
            .Distinct()
            .ToList();

        Dictionary<int, DocumentBindingEntity> docBindings = new();
        Dictionary<int, DocumentEntity> docs = new();

        if (bindingIds.Count > 0 && docBindingRepo != null)
        {
            var bindings = await docBindingRepo.GetQueryable().AsNoTracking()
                .Where(b => bindingIds.Contains(b.Id) && b.IsActive && !b.MarkedForDeletion)
                .ToListAsync(cancellationToken);

            docBindings = bindings.ToDictionary(b => b.Id);

            var docIds = bindings.Select(b => b.DocumentId).Distinct().ToList();
            if (docIds.Count > 0 && docRepo != null)
            {
                var docList = await docRepo.GetQueryable().AsNoTracking()
                    .Where(d => docIds.Contains(d.Id) && d.IsActive && !d.MarkedForDeletion)
                    .ToListAsync(cancellationToken);

                docs = docList.ToDictionary(d => d.Id);
            }
        }

        var result = new List<PropertyCertificateDto>();

        foreach (var pctm in certTypes)
        {
            var hasCert = certLookup.TryGetValue(pctm.Id, out var pc);
            DocumentBindingEntity? db = null;
            DocumentEntity? d = null;

            if (hasCert && pc?.DocumentBindingId.HasValue == true && docBindings.TryGetValue(pc.DocumentBindingId.Value, out var binding))
            {
                db = binding;
                docs.TryGetValue(binding.DocumentId, out d);
            }

            result.Add(new PropertyCertificateDto
            {
                Id = pc != null ? pc.Id : 0,
                PropertyId = pc != null && pc.PropertyId.HasValue ? pc.PropertyId.Value : propertyId,
                WingDetailId = pc?.WingDetailId,
                SocietyDetailId = pc?.SocietyDetailId,
                CertificateTypeId = pctm.Id,
                CertificateTypeName = pctm.CertificateTypeName,
                CertificateTypeCode = pctm.CertificateTypeCode,
                CertificateNo = pc?.CertificateNo,
                IssueDate = pc?.IssueDate,
                PropertyDetailsId = pc?.PropertyDetailsId,
                EntityType = pc?.EntityType ?? "P",
                DocumentBindingId = pc?.DocumentBindingId,
                DocumentId = db?.DocumentId,
                DocumentGuid = d?.DocumentGuid
            });
        }

        return result;
    }

    public async Task<bool> DeleteByDocumentIdAsync(
        int documentId,
        int deletedBy,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(documentId, nameof(documentId));
        Guard.AgainstNegativeOrZero(deletedBy, nameof(deletedBy));

        using var scope = _serviceProvider?.CreateScope();
        var sp = scope?.ServiceProvider;

        var certRepo = _propertyCertificateRepository ?? sp?.GetService<IRepository<PropertyCertificateEntity>>();
        var docBindingRepo = _documentBindingRepository ?? sp?.GetService<IRepository<DocumentBindingEntity>>();
        var docRepo = _documentRepository ?? sp?.GetService<IRepository<DocumentEntity>>();

        if (docBindingRepo == null || certRepo == null || docRepo == null)
        {
            return false;
        }

        var documentBinding = await docBindingRepo.GetQueryable().AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.DocumentId == documentId &&
                (x.ReferenceTableName == "PropertyCertificate" || 
                 x.ReferenceTableName == "PropertyCertificates") &&
                x.IsActive &&
                !x.MarkedForDeletion,
                cancellationToken);

        if (documentBinding == null)
        {
            return false;
        }

        var propertyCertificate = await certRepo.GetQueryable()
            .FirstOrDefaultAsync(x =>
                x.Id == documentBinding.ReferenceTableId &&
                x.DocumentBindingId == documentBinding.Id &&
                x.IsActive &&
                !x.MarkedForDeletion,
                cancellationToken);

        if (propertyCertificate == null)
        {
            return false;
        }

        var documentExists = await docRepo.GetQueryable().AsNoTracking()
            .AnyAsync(x =>
                x.Id == documentId &&
                x.IsActive &&
                !x.MarkedForDeletion,
                cancellationToken);

        if (!documentExists)
        {
            return false;
        }

        var currentDate = DateTime.Now;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            propertyCertificate.MarkForDeletion();
            propertyCertificate.UpdatedBy = deletedBy;
            propertyCertificate.UpdatedDate = currentDate;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var bindingUpdated = await docBindingRepo.GetQueryable()
                .Where(x =>
                    x.Id == documentBinding.Id &&
                    x.IsActive &&
                    !x.MarkedForDeletion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.MarkedForDeletion, true)
                    .SetProperty(x => x.IsActive, false)
                    .SetProperty(x => x.UpdatedBy, deletedBy)
                    .SetProperty(x => x.UpdatedDate, currentDate),
                    cancellationToken);

            if (bindingUpdated == 0)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return false;
            }

            var documentUpdated = await docRepo.GetQueryable()
                .Where(x =>
                    x.Id == documentId &&
                    x.IsActive &&
                    !x.MarkedForDeletion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.MarkedForDeletion, true)
                    .SetProperty(x => x.IsActive, false)
                    .SetProperty(x => x.UpdatedBy, deletedBy)
                    .SetProperty(x => x.UpdatedDate, currentDate),
                    cancellationToken);

            if (documentUpdated == 0)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return false;
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            if (propertyCertificate.PropertyId.HasValue)
            {
                var isTaxable = await _certificateTypeRepository.GetQueryable().AsNoTracking()
                    .Where(t => t.Id == propertyCertificate.CertificateTypeId)
                    .Select(t => t.IsTaxable)
                    .FirstOrDefaultAsync(cancellationToken);

                if (isTaxable)
                {
                    await _publisher.Publish(
                        new PropertyCertificateChangedEvent(propertyCertificate.PropertyId.Value, deletedBy),
                        cancellationToken);
                }
            }

            return true;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
