using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Common;
using NtisPlatform.Application.DTOs.Asset_Management.AssetDocument;
using NtisPlatform.Application.DTOs.Document;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Asset_Management;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Asset_Management;
using NtisPlatform.Core.Exceptions;

namespace NtisPlatform.Application.Services.Asset_Management;

/// <summary>
/// Application service for AssetDocument operations.
/// </summary>
public class AssetDocumentApplicationService : IAssetDocumentApplicationService
{
    private readonly IAssetDocumentService _assetDocumentService;
    private readonly IRepository<AssetDocumentDefinitionEntity, int> _documentDefinitionRepository;
    private readonly IRepository<AssetMasterEntity, int> _assetMasterRepository;
    private readonly IDocumentApplicationService _globalDocumentService;
    private readonly FileValidationHelper _fileValidationHelper;
    private readonly IModuleLookupService _moduleLookupService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AssetDocumentApplicationService> _logger;

    public AssetDocumentApplicationService(
        IAssetDocumentService documentService,
        IUnitOfWork unitOfWork,
        IRepository<AssetDocumentDefinitionEntity, int> documentDefinitionRepository,
        IRepository<AssetMasterEntity, int> assetMasterRepository,
        IDocumentApplicationService globalDocumentService,
        FileValidationHelper fileValidationHelper,
        IModuleLookupService moduleLookupService,
        ILogger<AssetDocumentApplicationService> logger)
    {
        _assetDocumentService = documentService;
        _unitOfWork = unitOfWork;
        _documentDefinitionRepository = documentDefinitionRepository;
        _assetMasterRepository = assetMasterRepository;
        _globalDocumentService = globalDocumentService;
        _fileValidationHelper = fileValidationHelper;
        _moduleLookupService = moduleLookupService;
        _logger = logger;
    }

    public async Task<List<AssetDocumentDto>> GetDocumentsByAssetAsync(
        int assetId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(assetId, nameof(assetId));
        var documents = await _assetDocumentService.GetLatestByAssetIdAsync(assetId, cancellationToken);
        return documents.Select(MapToDto).ToList();
    }

    public async Task<AssetDocumentGalleryDto> GetGroupedDocumentsByAssetAsync(
        int assetId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(assetId, nameof(assetId));

        var assetList = await _assetMasterRepository.GetAsync(a => a.Id == assetId, cancellationToken);
        var asset = assetList.FirstOrDefault();
        if (asset == null)
        {
            throw new ArgumentException($"Asset with ID {assetId} not found", nameof(assetId));
        }

        var existingDocuments = await _assetDocumentService.GetLatestByAssetIdAsync(assetId, cancellationToken);
        var existingDocDefIds = existingDocuments.Select(d => d.DocumentDefinitionId).Distinct().ToList();

        var allDefinitions = await _documentDefinitionRepository.GetAsync(
            d => d.IsActive 
                 && ((d.AssetCategoryId == null && d.AssetTypeId == null)
                     || (asset.AssetCategoryId != null
                         && d.AssetCategoryId == asset.AssetCategoryId
                         && (d.AssetTypeId == null || d.AssetTypeId == asset.AssetTypeId))
                     || existingDocDefIds.Contains(d.Id)), 
            cancellationToken);

        var docsByType = existingDocuments
            .GroupBy(d => d.DocumentDefinitionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var groups = allDefinitions.OrderBy(d => d.DisplayOrder).Select(definition =>
        {
            docsByType.TryGetValue(definition.Id, out var typeDocs);
            var documents = (typeDocs ?? new List<AssetDocumentEntity>())
                .OrderBy(d => d.DisplayOrder)
                .ThenBy(d => d.Id)
                .Select(MapToDto)
                .ToList();

            return new AssetDocumentTypeGroupDto
            {
                DocumentDefinitionId = definition.Id,
                DocumentCode = definition.DocumentCode,
                DocumentName = definition.DocumentName,
                DisplayOrder = definition.DisplayOrder,
                HasDocument = documents.Count > 0,
                DocumentCount = documents.Count,
                Documents = documents
            };
        }).ToList();

        return new AssetDocumentGalleryDto
        {
            AssetId = assetId,
            TotalDocuments = existingDocuments.Count,
            DocumentTypes = groups
        };
    }

    public async Task<List<AssetDocumentTypeWithStatusDto>> GetDocumentTypesWithStatusAsync(
        int assetId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(assetId, nameof(assetId));

        var assetList = await _assetMasterRepository.GetAsync(a => a.Id == assetId, cancellationToken);
        var asset = assetList.FirstOrDefault();
        if (asset == null)
        {
            throw new ArgumentException($"Asset with ID {assetId} not found", nameof(assetId));
        }

        var existingDocuments = await _assetDocumentService.GetLatestByAssetIdIncludingInactiveAsync(assetId, cancellationToken);
        var existingDocDefIds = existingDocuments.Select(d => d.DocumentDefinitionId).Distinct().ToList();

        var allDefinitions = await _documentDefinitionRepository.GetAsync(
            d => d.IsActive 
                 && ((d.AssetCategoryId == null && d.AssetTypeId == null)
                     || (asset.AssetCategoryId != null
                         && d.AssetCategoryId == asset.AssetCategoryId
                         && (d.AssetTypeId == null || d.AssetTypeId == asset.AssetTypeId))
                     || existingDocDefIds.Contains(d.Id)), 
            cancellationToken);

        var docsByType = existingDocuments
            .GroupBy(d => d.DocumentDefinitionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return allDefinitions.OrderBy(d => d.DisplayOrder).Select(definition =>
        {
            docsByType.TryGetValue(definition.Id, out var typeDocs);
            var count = typeDocs?.Count(d => d.IsActive && !d.MarkedForDeletion) ?? 0;
            var representative = typeDocs?.OrderBy(d => d.DisplayOrder).ThenBy(d => d.Id).FirstOrDefault();

            return new AssetDocumentTypeWithStatusDto
            {
                DocumentDefinitionId = definition.Id,
                DocumentCode = definition.DocumentCode,
                DocumentName = definition.DocumentName,
                DisplayOrder = definition.DisplayOrder,
                HasDocument = count > 0,
                DocumentCount = count,
                DocumentId = representative?.Id,
                Remarks = representative?.Remarks,
                DocumentBindingId = representative?.DocumentBindingId,
                DocumentGuid = representative != null ? GetSafeDocumentGuid(representative.DocumentBinding) : null,
                FileName = representative != null ? GetSafeFileName(representative.DocumentBinding) : null,
                MimeType = representative != null ? GetSafeMimeType(representative.DocumentBinding) : null
            };
        }).ToList();
    }

    public async Task<AssetDocumentBulkSaveResponseDto> BulkSaveAllAsync(
        AssetDocumentBulkSaveDto bulkDto,
        int userId,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(bulkDto.AssetId, nameof(bulkDto.AssetId));
        Guard.AgainstNegativeOrZero(userId, nameof(userId));

        _logger.LogInformation("Bulk saving {Count} documents for AssetId={AssetId}, User={UserId}",
            bulkDto.Documents.Count, bulkDto.AssetId, userId);

        var response = new AssetDocumentBulkSaveResponseDto
        {
            AssetId = bulkDto.AssetId,
            TotalProcessed = bulkDto.Documents.Count
        };

        var existingDocuments = await _assetDocumentService.GetLatestByAssetIdIncludingInactiveAsync(
            bulkDto.AssetId,
            cancellationToken);

        var existingLookup = existingDocuments
            .GroupBy(d => d.DocumentDefinitionId)
            .ToDictionary(g => g.Key, g => g.First());

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var docDto in bulkDto.Documents)
            {
                try
                {
                    var exists = existingLookup.TryGetValue(docDto.DocumentDefinitionId, out var existingDoc);

                    if (docDto.IsEnabled)
                    {
                        if (!exists)
                        {
                            var newDocId = await _assetDocumentService.CreateAsync(
                                bulkDto.AssetId,
                                docDto.DocumentDefinitionId,
                                docDto.DisplayOrder,
                                docDto.Remarks,
                                userId,
                                cancellationToken);

                            await _assetDocumentService.ToggleEnabledAsync(
                                newDocId,
                                true,
                                userId,
                                cancellationToken);

                            response.EnabledCount++;
                            _logger.LogDebug("Created and enabled new asset document: DefinitionId={DefId}, DocumentId={DocId}",
                                docDto.DocumentDefinitionId, newDocId);
                        }
                        else
                        {
                            await _assetDocumentService.UpdateAsync(
                                existingDoc!.Id,
                                docDto.DisplayOrder,
                                docDto.Remarks,
                                userId,
                                cancellationToken);

                            if (!existingDoc.IsActive)
                            {
                                await _assetDocumentService.ToggleEnabledAsync(
                                    existingDoc.Id,
                                    true,
                                    userId,
                                    cancellationToken);
                            }

                            response.EnabledCount++;
                            _logger.LogDebug("Updated and enabled existing asset document: DocumentId={DocId}",
                                existingDoc.Id);
                        }
                    }
                    else
                    {
                        if (exists && existingDoc != null && existingDoc.IsActive)
                        {
                            await _assetDocumentService.ToggleEnabledAsync(
                                existingDoc.Id,
                                false,
                                userId,
                                cancellationToken);

                            response.DisabledCount++;
                            _logger.LogDebug("Disabled asset document: DocumentId={DocId}", existingDoc.Id);
                        }
                        else
                        {
                            response.DisabledCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process bulk asset document item: DefinitionId={DefId}",
                        docDto.DocumentDefinitionId);
                    response.Errors.Add($"Document Definition {docDto.DocumentDefinitionId}: {ex.Message}");
                }
            }

            response.UpdatedDocumentTypes = await GetDocumentTypesWithStatusAsync(
                bulkDto.AssetId,
                cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

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

    private static AssetDocumentDto MapToDto(AssetDocumentEntity d) => new()
    {
        DocumentId = d.Id,
        AssetId = d.AssetId,
        DocumentDefinitionId = d.DocumentDefinitionId,
        DocumentCode = d.DocumentDefinition?.DocumentCode ?? string.Empty,
        DocumentName = d.DocumentDefinition?.DocumentName ?? string.Empty,
        DisplayOrder = d.DisplayOrder,
        Remarks = d.Remarks,
        DocumentBindingId = d.DocumentBindingId,
        DocumentGuid = GetSafeDocumentGuid(d.DocumentBinding),
        FileName = GetSafeFileName(d.DocumentBinding),
        MimeType = GetSafeMimeType(d.DocumentBinding)
    };

    private static Guid? GetSafeDocumentGuid(DocumentBindingEntity? documentBinding)
    {
        var document = documentBinding?.Document;
        if (document == null || !document.IsActive || document.MarkedForDeletion)
            return null;

        return document.DocumentGuid;
    }

    private static string? GetSafeFileName(DocumentBindingEntity? documentBinding)
    {
        var document = documentBinding?.Document;
        if (document == null || !document.IsActive || document.MarkedForDeletion)
            return null;

        return document.OriginalFileName;
    }

    private static string? GetSafeMimeType(DocumentBindingEntity? documentBinding)
    {
        var document = documentBinding?.Document;
        if (document == null || !document.IsActive || document.MarkedForDeletion)
            return null;

        return document.MimeType;
    }

    /// <inheritdoc />
    public async Task<AssetDocumentDto> SaveWithUploadAsync(
        AssetDocumentSaveWithUploadDto request,
        int uploadedBy,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNegativeOrZero(request.AssetId, nameof(request.AssetId));
        Guard.AgainstNegativeOrZero(request.DocumentDefinitionId, nameof(request.DocumentDefinitionId));
        Guard.AgainstNegativeOrZero(uploadedBy, nameof(uploadedBy));

        if (request.DocumentFile == null || request.DocumentFile.Length == 0)
            throw new ArgumentException("DocumentFile is required and cannot be empty.", nameof(request.DocumentFile));

        if (!string.IsNullOrWhiteSpace(request.Remarks))
            Guard.AgainstExceedingLength(request.Remarks, 500, nameof(request.Remarks));

        if (!_fileValidationHelper.IsValidFileType(request.DocumentFile.ContentType, request.DocumentFile.FileName))
            throw new ArgumentException(_fileValidationHelper.GetInvalidFileTypeMessage(), nameof(request.DocumentFile));

        _logger.LogInformation(
            "SaveWithUploadAsync: Saving slot for AssetId={AssetId}, DocumentDefinitionId={DocumentDefinitionId}, User={UserId}",
            request.AssetId, request.DocumentDefinitionId, uploadedBy);

        int savedId;
        AssetDocumentEntity entity;
        if (request.ExistingDocumentId.HasValue && request.ExistingDocumentId.Value > 0)
        {
            savedId = request.ExistingDocumentId.Value;
            entity = await _assetDocumentService.GetByIdAsync(savedId, cancellationToken)
                ?? throw new InvalidOperationException($"Failed to retrieve AssetDocument after save (Id={savedId}).");

            if (entity.AssetId != request.AssetId)
                throw new ArgumentException("ExistingDocumentId does not match the provided AssetId.", nameof(request.AssetId));
            if (entity.DocumentDefinitionId != request.DocumentDefinitionId)
                throw new ArgumentException("ExistingDocumentId does not match the provided DocumentDefinitionId.", nameof(request.DocumentDefinitionId));
        }
        else
        {
            savedId = await _assetDocumentService.CreateAsync(
                request.AssetId,
                request.DocumentDefinitionId,
                request.DisplayOrder,
                request.Remarks,
                uploadedBy,
                cancellationToken);

            entity = await _assetDocumentService.GetByIdAsync(savedId, cancellationToken)
                ?? throw new InvalidOperationException($"Failed to retrieve AssetDocument after save (Id={savedId}).");
        }

        var (deptId, modId) = await _moduleLookupService.GetDepartmentAndModuleAsync("AMS", "ASSET", cancellationToken);

        await using var fileStream = request.DocumentFile.OpenReadStream();
        await _globalDocumentService.UploadDocumentAsync(
            fileStream,
            request.DocumentFile.FileName,
            string.IsNullOrWhiteSpace(request.DocumentFile.ContentType)
                ? "application/octet-stream"
                : request.DocumentFile.ContentType,
            request.DocumentFile.Length,
            new DocumentUploadDto
            {
                ReferenceTableName = "AssetDocument",
                ReferenceTableId = savedId,
                DepartmentId = deptId,
                ModuleId = modId,
                DocumentType = entity.DocumentDefinition?.DocumentCode,
                AuthDepartmentId = deptId,
                IsPrimaryDocument = true
            },
            uploadedBy: uploadedBy,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "SaveWithUploadAsync: Completed. AssetDocumentId={AssetDocumentId}, File='{FileName}'",
            savedId, request.DocumentFile.FileName);

        var updated = await _assetDocumentService.GetByIdAsync(savedId, cancellationToken);
        return MapToDto(updated ?? entity);
    }
}
