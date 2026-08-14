using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.DTOs.Asset_Management.AssetDetails;
using NtisPlatform.Application.DTOs.Asset_Management.AssetDocument;
using NtisPlatform.Application.DTOs.Asset_Management.AssetFieldValue;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using NtisPlatform.Application.DTOs.Document;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Entities.Asset_Management;
using System.Text.Json;

namespace NtisPlatform.Application.Services.Asset_Management
{
    public partial class AssetMasterService
    {
        #region Main CRUD Operations

        /// <summary>
        /// Override GetAllAsync to return all assets with proper pagination using ProjectTo.
        /// Follows the order: Filter -> Search -> Sort -> Count -> Skip/Take -> ProjectTo
        /// Note: FieldValues are excluded from list view for performance.
        /// Use GetByIdAsync to get FieldValues for a specific asset.
        /// When ParentAssetId is provided in queryParameters, returns child assets for that parent.
        /// Otherwise, only returns main assets (those with no ParentAssetId). Sub-assets are excluded.
        /// </summary>
        public override Task<PagedResult<AssetMasterDto>> GetAllAsync(AssetMasterQueryParameters queryParameters, CancellationToken cancellationToken = default)
            => GetAllInternalAsync(queryParameters, cancellationToken);

        /// <summary>
        /// Override GetByIdAsync to include all field values for the asset.
        /// </summary>
        public override async Task<AssetMasterDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            // Single round trip for both counts (previously two near-identical queries over the
            // same child+floor-details join), mirroring the TotalCount/ActiveCount GroupBy pattern
            // used in GetAllInternalAsync.
            var unitFloorStats = await (
                from child in _repository.GetQueryable().AsNoTracking()
                join fd in _floorDetailsRepository.GetQueryable().AsNoTracking()
                    on child.Id equals fd.AssetId
                where child.ParentAssetId == id
                      && !child.MarkedForDeletion
                      && child.IsActive
                      && fd.IsActive
                      && !fd.MarkedForDeletion
                select new { ChildId = child.Id, fd.FloorId }
            )
            .GroupBy(x => 1)
            .Select(g => new
            {
                TotalUnits = g.Select(x => x.ChildId).Distinct().Count(),
                TotalFloors = g.Select(x => x.FloorId).Distinct().Count()
            })
            .FirstOrDefaultAsync(cancellationToken);

            var totalUnits = unitFloorStats?.TotalUnits ?? 0;
            var totalFloors = unitFloorStats?.TotalFloors ?? 0;

            var dto = await _repository.GetQueryable()
                .AsNoTracking()
                .Where(a => a.Id == id && !a.MarkedForDeletion)
                .Select(a => new AssetMasterDto
                {
                    Id = a.Id,
                    IsActive = a.IsActive,
                    CreatedDate = a.CreatedDate,
                    UpdatedDate = a.UpdatedDate,
                    AssetNo = a.AssetNo,
                    AssetName = a.AssetName,
                    AssetRegionalName = a.AssetRegionalName,
                    AssetCategoryId = a.AssetCategoryId,
                    AssetTypeId = a.AssetTypeId,
                    ParentAssetId = a.ParentAssetId,
                    HierarchyLevel = a.HierarchyLevel,
                    HierarchyPath = a.HierarchyPath,
                    TotalUnits = totalUnits,
                    TotalFloors = totalFloors,
                    OwnershipType = a.OwnershipType,
                    OccupancyStatus = a.OccupancyStatus,
                    AssetConditionId = a.AssetConditionId,
                    Details = new AssetDetailsDto
                    {
                        Id                  = a.Details != null ? a.Details.Id : 0,
                        AssetId             = a.Details != null ? a.Details.AssetId : 0,
                        OrganizationId      = a.Details != null ? a.Details.OrganizationId : 0,
                        ZoneId              = a.Details != null ? a.Details.ZoneId : null,
                        WardId              = a.Details != null ? a.Details.WardId : null,
                        MoujaId             = a.Details != null ? a.Details.MoujaId : null,
                        SubZoneId           = a.Details != null ? a.Details.SubZoneId : null,
                        AssetWardNo         = a.Details != null ? a.Details.AssetWardNo : null,
                        PropertyNo          = a.Details != null ? a.Details.PropertyNo : null,
                        PartitionNo         = a.Details != null ? a.Details.PartitionNo : null,
                        UpicId              = a.Details != null ? a.Details.UpicId : null,
                        PlotNo              = a.Details != null ? a.Details.PlotNo : null,
                        CSN                 = a.Details != null ? a.Details.CSN : null,
                        LandRate            = a.Details != null ? a.Details.LandRate : null,
                        LengthFt            = a.Details != null ? a.Details.LengthFt : null,
                        LengthMtr           = a.Details != null ? a.Details.LengthMtr : null,
                        WidthFt             = a.Details != null ? a.Details.WidthFt : null,
                        WidthMtr            = a.Details != null ? a.Details.WidthMtr : null,
                        LandAreaSqFeet      = a.Details != null ? a.Details.LandAreaSqFeet : null,
                        LandAreaSqMeter     = a.Details != null ? a.Details.LandAreaSqMeter : null,
                        Address             = a.Details != null ? a.Details.Address : null,
                        NearestLandmark     = a.Details != null ? a.Details.NearestLandmark : null,
                        PinCode             = a.Details != null ? a.Details.PinCode : null,
                        Latitude            = a.Details != null ? a.Details.Latitude : null,
                        Longitude           = a.Details != null ? a.Details.Longitude : null,
                        BoundaryGeoJson     = a.Details != null ? a.Details.BoundaryGeoJson : null,
                        InChargeName        = a.Details != null ? a.Details.InChargeName : null,
                        InChargeRegionalName = a.Details != null ? a.Details.InChargeRegionalName : null,
                        InChargeDesignationId = a.Details != null ? a.Details.InChargeDesignationId : null,
                        InChargeMobile      = a.Details != null ? a.Details.InChargeMobile : null,
                        InChargeEmail       = a.Details != null ? a.Details.InChargeEmail : null,
                    },
                    Names = new AssetMasterNamesDto
                    {
                        AssetCategoryName = a.AssetCategory != null ? a.AssetCategory.CategoryName : null,
                        AssetTypeName = a.AssetType != null ? a.AssetType.TypeName : null,
                        ParentAssetName = a.ParentAsset != null ? a.ParentAsset.AssetName : null
                    },
                    FieldValues = a.FieldValues!
                        .Where(fv => fv.IsActive == a.IsActive && !fv.MarkedForDeletion)
                        .Select(fv => new AssetFieldValueDto
                        {
                            Id = fv.Id,
                            FieldDefinitionId = fv.FieldDefinitionId,
                            FieldName = fv.FieldName,
                            FieldValue = fv.FieldValue
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (dto != null)
            {
                var locations = await GetLocationInfoByAssetIdsAsync(new[] { dto.Id }, cancellationToken);
                if (locations.TryGetValue(dto.Id, out var location))
                {
                    dto.DepartmentId = location.DepartmentId;
                    ApplyLocation(dto.Details, dto.Names, location);
                }

                if (dto.Details != null && dto.Details.InChargeDesignationId.HasValue)
                {
                    dto.Details.InChargeDesignationName = await _designationRepository.GetQueryable()
                        .AsNoTracking()
                        .Where(d => d.Id == dto.Details.InChargeDesignationId.Value)
                        .Select(d => d.DesignationName)
                        .FirstOrDefaultAsync(cancellationToken);
                }

                if (dto.AssetConditionId.HasValue)
                {
                    dto.Names.AssetCondition = await _conditionRepository.GetQueryable()
                        .AsNoTracking()
                        .Where(c => c.Id == dto.AssetConditionId.Value)
                        .Select(c => c.ConditionName)
                        .FirstOrDefaultAsync(cancellationToken);
                }

                // Single round trip for both CapitalValue and AssetLife (previously two separate
                // queries) — both are derived from the same floor-details row set: CapitalValue sums
                // every row (this asset's own + its children's), AssetLife looks only at children's
                // construction years, so the child-only filter is applied in-memory below.
                var floorRows = await _floorDetailsRepository.GetQueryable()
                    .AsNoTracking()
                    .Where(fd => fd.IsActive && !fd.MarkedForDeletion &&
                                 (fd.AssetId == id ||
                                  (fd.Asset!.ParentAssetId == id &&
                                   fd.Asset.IsActive &&
                                   !fd.Asset.MarkedForDeletion)))
                    .Select(fd => new { fd.AssetId, fd.CapitalValue, fd.ConstructionYear })
                    .ToListAsync(cancellationToken);

                var capitalValue = floorRows.Sum(x => x.CapitalValue ?? 0m);
                dto.CapitalValue = capitalValue;

                // Resolve AssetLife from child assets' AMS.SubUnitsDetails oldest construction year
                var oldestYearParsed = floorRows
                    .Where(x => x.AssetId != id && x.ConstructionYear != null)
                    .Select(x => ParseConstructionYear(x.ConstructionYear))
                    .Where(y => y.HasValue && y.Value > 0)
                    .Min();

                if (oldestYearParsed.HasValue && oldestYearParsed.Value <= DateTime.Now.Year)
                {
                    dto.AssetLife = DateTime.Now.Year - oldestYearParsed.Value;
                }
                else
                {
                    dto.AssetLife = null;
                }

                // Fetch photos directly from AMS.AssetPhoto filtered by AssetId
                dto.Photos = await _assetPhotoRepository.GetQueryable()
                    .AsNoTracking()
                    .Where(p => p.AssetId == id && p.IsLatest && p.IsActive && !p.MarkedForDeletion)
                    .OrderBy(p => p.DisplayOrder)
                    .ThenBy(p => p.PhotoTypeId)
                    .Select(p => new AssetPhotoDto
                    {
                        PhotoId             = p.Id,
                        AssetId             = p.AssetId,
                        PhotoTypeId         = p.PhotoTypeId,
                        PhotoTypeCode       = p.PhotoType != null ? p.PhotoType.PhotoTypeCode : string.Empty,
                        PhotoTypeName       = p.PhotoType != null ? p.PhotoType.PhotoTypeName : string.Empty,
                        DisplayOrder        = p.DisplayOrder,
                        Remarks             = p.Remarks,
                        DocumentBindingId   = p.DocumentBindingId,
                        DocumentGuid        = p.DocumentBinding != null && p.DocumentBinding.Document != null
                                             && p.DocumentBinding.Document.IsActive && !p.DocumentBinding.Document.MarkedForDeletion
                                             ? p.DocumentBinding.Document.DocumentGuid : (Guid?)null,
                        FileName            = p.DocumentBinding != null && p.DocumentBinding.Document != null
                                             && p.DocumentBinding.Document.IsActive && !p.DocumentBinding.Document.MarkedForDeletion
                                             ? p.DocumentBinding.Document.OriginalFileName : null,
                        MimeType            = p.DocumentBinding != null && p.DocumentBinding.Document != null
                                             && p.DocumentBinding.Document.IsActive && !p.DocumentBinding.Document.MarkedForDeletion
                                             ? p.DocumentBinding.Document.MimeType : null,
                    })
                    .ToListAsync(cancellationToken);

                // Fetch documents directly from AMS.AssetDocument filtered by AssetId
                dto.Documents = await _assetDocumentRepository.GetQueryable()
                    .AsNoTracking()
                    .Where(d => d.AssetId == id && d.IsLatest && d.IsActive && !d.MarkedForDeletion)
                    .OrderBy(d => d.DisplayOrder)
                    .ThenBy(d => d.DocumentDefinitionId)
                    .ThenBy(d => d.Id)
                    .Select(d => new AssetDocumentDto
                    {
                        DocumentId = d.Id,
                        AssetId = d.AssetId,
                        DocumentDefinitionId = d.DocumentDefinitionId,
                        DocumentCode = d.DocumentDefinition != null ? d.DocumentDefinition.DocumentCode : string.Empty,
                        DocumentName = d.DocumentDefinition != null ? d.DocumentDefinition.DocumentName : string.Empty,
                        DisplayOrder = d.DisplayOrder,
                        Remarks = d.Remarks,
                        DocumentBindingId = d.DocumentBindingId,
                        DocumentGuid = d.DocumentBinding != null && d.DocumentBinding.Document != null
                                       && d.DocumentBinding.Document.IsActive && !d.DocumentBinding.Document.MarkedForDeletion
                                       ? d.DocumentBinding.Document.DocumentGuid : (Guid?)null,
                        FileName = d.DocumentBinding != null && d.DocumentBinding.Document != null
                                   && d.DocumentBinding.Document.IsActive && !d.DocumentBinding.Document.MarkedForDeletion
                                   ? d.DocumentBinding.Document.OriginalFileName : null,
                        MimeType = d.DocumentBinding != null && d.DocumentBinding.Document != null
                                   && d.DocumentBinding.Document.IsActive && !d.DocumentBinding.Document.MarkedForDeletion
                                   ? d.DocumentBinding.Document.MimeType : null,
                    })
                    .ToListAsync(cancellationToken);

                PopulateFlatProperties(dto);
            }

            return dto;
        }

        /// <summary>
        /// Same as <see cref="GetByIdAsync"/>. Retained for callers that pass a <paramref name="currentUserId"/>.
        /// </summary>
        public Task<AssetMasterDto?> GetByIdForUserAsync(int id, int currentUserId, CancellationToken cancellationToken = default)
            => GetByIdAsync(id, cancellationToken);

        /// <summary>
        /// Override CreateAsync to handle field values in a transaction.
        /// </summary>
        public override async Task<AssetMasterDto> CreateAsync(CreateAssetMasterDto createDto, CancellationToken cancellationToken = default)
        {
            List<CreateAssetFieldValueDto> fieldValuesList = new();

            if (!string.IsNullOrWhiteSpace(createDto.FieldValuesJson))
            {
                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var trimmed = createDto.FieldValuesJson.Trim();
                    List<CreateAssetFieldValueDto>? deserialized = null;
                    if (trimmed.StartsWith("["))
                    {
                        deserialized = JsonSerializer.Deserialize<List<CreateAssetFieldValueDto>>(trimmed, options);
                    }
                    else if (trimmed.StartsWith("{"))
                    {
                        var single = JsonSerializer.Deserialize<CreateAssetFieldValueDto>(trimmed, options);
                        if (single != null)
                        {
                            deserialized = new List<CreateAssetFieldValueDto> { single };
                        }
                    }
                    if (deserialized != null)
                    {
                        fieldValuesList = deserialized;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to deserialize FieldValuesJson in CreateAsync");
                }
            }

            AssetMasterEntity entity = _mapper.Map<AssetMasterEntity>(createDto);
            var shouldRollbackTransaction = true;
            List<AssetPhotoDto>? photoMetadata = null;
            var photoFiles = createDto.PhotoFiles;

            entity.AssetNo = await GenerateAssetNoAsync(createDto.AssetCategoryId, createDto.AssetTypeId, cancellationToken);

            if (string.IsNullOrWhiteSpace(createDto.AssetWardNo))
            {
                var assetType = await _assetTypeRepository.GetByIdAsync(createDto.AssetTypeId, cancellationToken);
                if (assetType != null && !string.IsNullOrWhiteSpace(assetType.AssetWardNo))
                {
                    createDto.AssetWardNo = assetType.AssetWardNo;
                }
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                entity.IsActive = false;
                entity.CreatedDate = DateTime.Now;

                var validationResult = await ValidateForCreateAsync(entity, cancellationToken);
                if (!validationResult.IsValid)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    throw new ValidationException("Validation failed for create operation", validationResult.ToDictionary(), OperationType.Create);
                }

                await _repository.AddAsync(entity, cancellationToken);

                // GenerateAssetNoAsync's sequence lock only guards the max-sequence read, not
                // persistence (coverage roadmap Section B item 5) -- a concurrent create can reserve
                // the same AssetNo before either commits. Retry with a freshly generated number instead
                // of surfacing that race as a client-facing failure; UQ_AssetMaster_AssetNo is the
                // last-resort guard this checks for.
                const int maxAssetNoAttempts = 3;
                for (var assetNoAttempt = 1; ; assetNoAttempt++)
                {
                    try
                    {
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        break;
                    }
                    catch (DbUpdateException ex) when (assetNoAttempt < maxAssetNoAttempts && IsUniqueAssetNoViolation(ex))
                    {
                        entity.AssetNo = await GenerateAssetNoAsync(createDto.AssetCategoryId, createDto.AssetTypeId, cancellationToken);
                    }
                }

                var details = BuildDetails(createDto, entity.Id, createDto.CreatedBy);
                details.IsActive = false;
                await _detailsRepository.AddAsync(details, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (fieldValuesList?.Count > 0)
                {
                    var fieldValues = new List<AssetFieldValueEntity>();
                    foreach (var field in fieldValuesList)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var fieldValue = new AssetFieldValueEntity
                        {
                            AssetId = entity.Id,
                            FieldDefinitionId = field.FieldDefinitionId,
                            FieldName = field.FieldName,
                            FieldValue = field.FieldValue,
                            CreatedBy = createDto.CreatedBy,
                            CreatedDate = DateTime.Now,
                            IsActive = false,
                            MarkedForDeletion = false,
                            MarkedForDeletionDate = null
                        };
                        fieldValues.Add(fieldValue);
                    }
                    await _fieldValueRepository.AddRangeAsync(fieldValues.ToArray(), cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                if (photoFiles is { Count: > 0 })
                {
                    photoMetadata = new List<AssetPhotoDto>();

                    if (!string.IsNullOrWhiteSpace(createDto.PhotoMetadataJson))
                    {
                        try
                        {
                            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var trimmed = createDto.PhotoMetadataJson.Trim();
                            photoMetadata = trimmed.StartsWith('{')
                                ? new List<AssetPhotoDto> { JsonSerializer.Deserialize<AssetPhotoDto>(trimmed, options)! }
                                : JsonSerializer.Deserialize<List<AssetPhotoDto>>(trimmed, options) ?? [];
                        }
                        catch (JsonException)
                        {
                            throw new ValidationException(
                                "Photo metadata JSON is invalid.",
                                new Dictionary<string, string>
                                {
                                    ["PhotoMetadataJson"] = "Provide valid JSON (object or array) for photo metadata."
                                },
                                OperationType.Create);
                        }
                    }

                    if (photoFiles.Count != photoMetadata.Count)
                    {
                        throw new ValidationException(
                            "PhotoFiles count must match photo metadata count.",
                            new Dictionary<string, string>
                            {
                                ["PhotoFiles"] = "Each file must have matching metadata at same index."
                            },
                            OperationType.Create);
                    }
                }

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                shouldRollbackTransaction = false;

                if (photoFiles is { Count: > 0 } && photoMetadata is not null)
                {
                    var deptEntity = (await _deptMasterRepository.GetAsync(
                        d => d.DepartmentName != null && d.DepartmentName.ToLower().Contains("asset"),
                        cancellationToken)).FirstOrDefault();
                    var modEntity = (await _moduleMasterRepository.GetAsync(
                        m => m.ModuleName != null && m.ModuleName.ToLower().Contains("asset"),
                        cancellationToken)).FirstOrDefault();

                    var dynamicDeptId = deptEntity?.Id ?? 3;
                    var dynamicModId = modEntity?.Id ?? 2;

                    var bulkDto = new AssetPhotoBulkSaveDto
                    {
                        AssetId = entity.Id,
                        Photos = photoMetadata.Select(m => new AssetPhotoItemDto
                        {
                            PhotoTypeId = m.PhotoTypeId,
                            DisplayOrder = m.DisplayOrder ?? 0,
                            Remarks = m.Remarks,
                            IsEnabled = true
                        }).ToList()
                    };

                    var savedPhotosResponse = await _assetPhotoApplicationService.BulkSaveAllAsync(bulkDto, createDto.CreatedBy ?? 1, cancellationToken);

                    for (var i = 0; i < photoFiles.Count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var file = photoFiles[i];
                        var meta = photoMetadata[i];

                        if (file == null || file.Length <= 0)
                            continue;

                        var registeredSlot = savedPhotosResponse.UpdatedPhotoTypes
                            .FirstOrDefault(x => x.PhotoTypeId == meta.PhotoTypeId);

                        if (registeredSlot?.PhotoId == null)
                            continue;

                        await using var fileStream = file.OpenReadStream();
                        await _documentApplicationService.UploadDocumentAsync(
                            fileStream,
                            file.FileName,
                            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                            file.Length,
                            new DocumentUploadDto
                            {
                                ReferenceTableName = "AssetPhoto",
                                ReferenceTableId = registeredSlot.PhotoId.Value,
                                ModuleId = dynamicModId,
                                DepartmentId = dynamicDeptId,
                                DocumentType = registeredSlot.PhotoTypeCode,
                                AuthDepartmentId = dynamicDeptId,
                                IsPrimaryDocument = true
                            },
                            uploadedBy: createDto.CreatedBy ?? 1,
                            cancellationToken: cancellationToken);
                    }
                }

                var savedDto = await GetByIdAsync(entity.Id, cancellationToken);
                if (savedDto is null)
                {
                    throw new InvalidOperationException($"Asset was created but could not be reloaded. AssetId: {entity.Id}");
                }
                return savedDto;
            }
            catch (ValidationException)
            {
                if (shouldRollbackTransaction)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                }
                throw;
            }
            catch
            {
                if (shouldRollbackTransaction)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                }
                throw;
            }
        }

        /// <summary>
        /// Override UpdateAsync to handle field values in a transaction.
        /// Supports create, update, and delete of field values.
        /// </summary>
        public override async Task<AssetMasterDto?> UpdateAsync(int id, UpdateAssetMasterDto updateDto, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            if (entity == null)
                return default;
            var currentEntitySnapshot = await _repository.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (currentEntitySnapshot == null)
                return default;

            List<AssetPhotoDto>? photoMetadata = null;
            var photoFiles = updateDto.PhotoFiles;

            if (photoFiles is { Count: > 0 })
            {
                photoMetadata = new List<AssetPhotoDto>();

                if (!string.IsNullOrWhiteSpace(updateDto.PhotoMetadataJson))
                {
                    try
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var trimmed = updateDto.PhotoMetadataJson.Trim();
                        photoMetadata = trimmed.StartsWith('{')
                            ? new List<AssetPhotoDto> { JsonSerializer.Deserialize<AssetPhotoDto>(trimmed, options)! }
                            : JsonSerializer.Deserialize<List<AssetPhotoDto>>(trimmed, options) ?? [];
                    }
                    catch (JsonException)
                    {
                        throw new ValidationException(
                            "Photo metadata JSON is invalid.",
                            new Dictionary<string, string>
                            {
                                ["PhotoMetadataJson"] = "Provide valid JSON array for photo metadata."
                            },
                            OperationType.Update);
                    }
                }

                if (photoFiles.Count != photoMetadata.Count)
                {
                    throw new ValidationException(
                        "PhotoFiles count must match photo metadata count.",
                        new Dictionary<string, string>
                        {
                            ["PhotoFiles"] = "Each file must have matching metadata at same index."
                        },
                        OperationType.Update);
                }
            }

            if (string.IsNullOrWhiteSpace(updateDto.AssetWardNo))
            {
                var assetType = await _assetTypeRepository.GetByIdAsync(updateDto.AssetTypeId, cancellationToken);
                if (assetType != null && !string.IsNullOrWhiteSpace(assetType.AssetWardNo))
                {
                    updateDto.AssetWardNo = assetType.AssetWardNo;
                }
            }

            var shouldRollbackTransaction = true;
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var oldCategoryId = entity.AssetCategoryId;
                var oldTypeId = entity.AssetTypeId;

                _mapper.Map(updateDto, entity);
                entity.UpdatedDate = DateTime.Now;

                var assetNoRegenerated = false;
                if (oldCategoryId != entity.AssetCategoryId || oldTypeId != entity.AssetTypeId || string.IsNullOrWhiteSpace(entity.AssetNo))
                {
                    entity.AssetNo = await GenerateAssetNoAsync(entity.AssetCategoryId, entity.AssetTypeId, cancellationToken);
                    assetNoRegenerated = true;
                }
                var validationResult = await ValidateForDeactivationAsync(id, currentEntitySnapshot, entity, cancellationToken);
                if (!validationResult.IsValid)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    throw new ValidationException("Validation failed for update operation", validationResult.ToDictionary(), OperationType.Update);
                }
                await _repository.UpdateAsync(entity, cancellationToken);

                var details = await _detailsRepository.GetQueryable()
                    .FirstOrDefaultAsync(d => d.AssetId == id, cancellationToken);
                if (details != null)
                {
                    ApplyDetailsUpdate(updateDto, details, updateDto.UpdatedBy);
                    await _detailsRepository.UpdateAsync(details, cancellationToken);
                }
                // AMS.AssetDetails is 1:1 with a PARENT asset only — a child (shop/unit/floor) never
                // gets its own row; its Zone/Ward/Address are resolved from the parent's row instead
                // (see GetLocationInfoByAssetIdsAsync). Without this guard, updating a child asset
                // that has no row yet (e.g. from the lease/rent registration flow) would create one.
                else if (entity.ParentAssetId == null)
                {
                    var newDetails = new AssetDetailsEntity
                    {
                        AssetId        = id,
                        OrganizationId = updateDto.OrganizationId,
                        ZoneId         = updateDto.ZoneId,
                        WardId         = updateDto.WardId,
                        MoujaId        = updateDto.MoujaId,
                        SubZoneId      = updateDto.SubZoneId,
                        AssetWardNo    = updateDto.AssetWardNo,
                        PropertyNo     = updateDto.PropertyNo,
                        PartitionNo    = updateDto.PartitionNo,
                        UpicId         = updateDto.UpicId,
                        PlotNo         = updateDto.PlotNo,
                        CSN            = updateDto.CSN,
                        LandRate       = updateDto.LandRate,
                        LengthMtr       = updateDto.TotalLength,
                        WidthMtr        = updateDto.AverageWidth,
                        LandAreaSqMeter = updateDto.LandAreaSqMeter,
                        LengthFt        = updateDto.LengthFt,
                        WidthFt         = updateDto.WidthFt,
                        LandAreaSqFeet  = updateDto.LandAreaSqFeet,
                        Address        = updateDto.Address,
                        NearestLandmark = updateDto.Locality,
                        PinCode        = updateDto.PinCode,
                        Latitude       = updateDto.Latitude,
                        Longitude      = updateDto.Longitude,
                        InChargeName        = updateDto.InChargeName,
                        InChargeDesignationId = updateDto.InChargeDesignationId,
                        InChargeMobile      = updateDto.InChargeMobile,
                        InChargeEmail       = updateDto.InChargeEmail,
                        IsActive = true,
                        CreatedBy = updateDto.UpdatedBy,
                        CreatedDate = DateTime.Now,
                        MarkedForDeletion = false,
                        MarkedForDeletionDate = null
                    };
                    await _detailsRepository.AddAsync(newDetails, cancellationToken);
                }
                if (updateDto.FieldValues != null)
                {
                    var existingFieldValues = await _fieldValueRepository.GetQueryable()
                        .Where(fv => fv.AssetId == id)
                        .ToListAsync(cancellationToken);

                    static string NormalizeFieldName(string? fieldName) => (fieldName ?? string.Empty).Trim().ToLowerInvariant();
                    var duplicateFieldNames = updateDto.FieldValues
                        .GroupBy(fv => NormalizeFieldName(fv.FieldName))
                        .Where(g => !string.IsNullOrWhiteSpace(g.Key) && g.Count() > 1)
                        .Select(g => g.First().FieldName)
                        .ToList();

                    if (duplicateFieldNames.Count > 0)
                    {
                        throw new InvalidOperationException(
                            $"Duplicate field values are not allowed for the same asset. Duplicate fields: {string.Join(", ", duplicateFieldNames)}");
                    }

                    // Pre-index existing field values by Id and by normalized name (first occurrence
                    // wins, same as the FirstOrDefault scans below) so each incoming fieldDto resolves
                    // its match in O(1) instead of scanning existingFieldValues twice per iteration.
                    var existingById = new Dictionary<int, AssetFieldValueEntity>();
                    var existingByNormalizedName = new Dictionary<string, AssetFieldValueEntity>();
                    foreach (var fv in existingFieldValues)
                    {
                        existingById[fv.Id] = fv;
                        var key = NormalizeFieldName(fv.FieldName);
                        if (!existingByNormalizedName.ContainsKey(key))
                        {
                            existingByNormalizedName[key] = fv;
                        }
                    }

                    foreach (var fieldDto in updateDto.FieldValues)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        AssetFieldValueEntity? targetFieldValue = null;

                        if (fieldDto.Id.HasValue)
                        {
                            existingById.TryGetValue(fieldDto.Id.Value, out targetFieldValue);
                        }
                        if (targetFieldValue == null)
                        {
                            var normalizedFieldName = NormalizeFieldName(fieldDto.FieldName);
                            existingByNormalizedName.TryGetValue(normalizedFieldName, out targetFieldValue);
                        }
                        if (targetFieldValue != null)
                        {
                            targetFieldValue.FieldDefinitionId = fieldDto.FieldDefinitionId;
                            targetFieldValue.FieldName = fieldDto.FieldName;
                            targetFieldValue.FieldValue = fieldDto.FieldValue;
                            targetFieldValue.IsActive = true;
                            targetFieldValue.MarkedForDeletion = false;
                            targetFieldValue.MarkedForDeletionDate = null;
                            targetFieldValue.UpdatedDate = DateTime.Now;
                            targetFieldValue.UpdatedBy = updateDto.UpdatedBy;
                            await _fieldValueRepository.UpdateAsync(targetFieldValue, cancellationToken);
                        }
                        else
                        {
                            var newFieldValue = new AssetFieldValueEntity
                            {
                                AssetId = id,
                                FieldDefinitionId = fieldDto.FieldDefinitionId,
                                FieldName = fieldDto.FieldName,
                                FieldValue = fieldDto.FieldValue,
                                CreatedBy = updateDto.UpdatedBy,
                                CreatedDate = DateTime.Now,
                                IsActive = true,
                                MarkedForDeletion = false,
                                MarkedForDeletionDate = null
                            };
                            await _fieldValueRepository.AddAsync(newFieldValue, cancellationToken);
                        }
                    }
                }

                // See the matching comment in CreateAsync: GenerateAssetNoAsync's sequence lock only
                // guards the max-sequence read, not persistence, so a concurrent request can reserve
                // the same AssetNo before either commits. Only retry here when this call actually
                // regenerated the number -- an update that didn't touch AssetNo hitting this unique
                // index would indicate a different, unrelated problem worth surfacing as-is.
                const int maxAssetNoAttempts = 3;
                for (var assetNoAttempt = 1; ; assetNoAttempt++)
                {
                    try
                    {
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        break;
                    }
                    catch (DbUpdateException ex) when (assetNoRegenerated && assetNoAttempt < maxAssetNoAttempts && IsUniqueAssetNoViolation(ex))
                    {
                        entity.AssetNo = await GenerateAssetNoAsync(entity.AssetCategoryId, entity.AssetTypeId, cancellationToken);
                    }
                }
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                shouldRollbackTransaction = false;

                if (photoFiles is { Count: > 0 } && photoMetadata is not null)
                {
                    var deptEntity = (await _deptMasterRepository.GetAsync(
                        d => d.DepartmentName != null && d.DepartmentName.ToLower().Contains("asset"),
                        cancellationToken)).FirstOrDefault();
                    var modEntity = (await _moduleMasterRepository.GetAsync(
                        m => m.ModuleName != null && m.ModuleName.ToLower().Contains("asset"),
                        cancellationToken)).FirstOrDefault();

                    var dynamicDeptId = deptEntity?.Id ?? 3;
                    var dynamicModId = modEntity?.Id ?? 2;

                    var bulkDto = new AssetPhotoBulkSaveDto
                    {
                        AssetId = id,
                        Photos = photoMetadata.Select(m => new AssetPhotoItemDto
                        {
                            PhotoTypeId = m.PhotoTypeId,
                            DisplayOrder = m.DisplayOrder ?? 0,
                            Remarks = m.Remarks,
                            IsEnabled = true
                        }).ToList()
                    };

                    var savedPhotosResponse = await _assetPhotoApplicationService.BulkSaveAllAsync(bulkDto, updateDto.UpdatedBy ?? 1, cancellationToken);

                    for (var i = 0; i < photoFiles.Count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var file = photoFiles[i];
                        var meta = photoMetadata[i];

                        if (file == null || file.Length <= 0)
                            continue;

                        var registeredSlot = savedPhotosResponse.UpdatedPhotoTypes
                            .FirstOrDefault(x => x.PhotoTypeId == meta.PhotoTypeId);

                        if (registeredSlot?.PhotoId == null)
                            continue;

                        await using var fileStream = file.OpenReadStream();
                        await _documentApplicationService.UploadDocumentAsync(
                            fileStream,
                            file.FileName,
                            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                            file.Length,
                            new DocumentUploadDto
                            {
                                ReferenceTableName = "AssetPhoto",
                                ReferenceTableId = registeredSlot.PhotoId.Value,
                                ModuleId = dynamicModId,
                                DepartmentId = dynamicDeptId,
                                DocumentType = registeredSlot.PhotoTypeCode,
                                AuthDepartmentId = dynamicDeptId,
                                IsPrimaryDocument = true
                            },
                            uploadedBy: updateDto.UpdatedBy ?? 1,
                            cancellationToken: cancellationToken);
                    }
                }

                return await GetByIdAsync(id, cancellationToken);
            }
            catch (ValidationException)
            {
                if (shouldRollbackTransaction)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                }
                throw;
            }
            catch
            {
                if (shouldRollbackTransaction)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                }
                throw;
            }
        }

        /// <summary>
        /// Override DeleteAsync to soft delete field values along with the asset.
        /// </summary>
        public override async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            if (entity == null)
                return false;
            var validationResult = await ValidateForDeleteAsync(id, entity, cancellationToken);
            if (!validationResult.IsValid)
            {
                var firstError = validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed for delete operation";
                throw new ValidationException(firstError, validationResult.ToDictionary(), OperationType.Delete);
            }
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var fieldValues = await _fieldValueRepository.GetQueryable()
                    .Where(fv => fv.AssetId == id && fv.IsActive)
                    .ToListAsync(cancellationToken);
                foreach (var fieldValue in fieldValues)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    fieldValue.IsActive = false;
                    fieldValue.MarkedForDeletion = true;
                    fieldValue.MarkedForDeletionDate = DateTime.Now;
                    fieldValue.UpdatedDate = DateTime.Now;
                    await _fieldValueRepository.UpdateAsync(fieldValue, cancellationToken);
                }

                var details = await _detailsRepository.GetQueryable()
                    .FirstOrDefaultAsync(d => d.AssetId == id && d.IsActive, cancellationToken);
                if (details != null)
                {
                    details.IsActive = false;
                    details.MarkedForDeletion = true;
                    details.MarkedForDeletionDate = DateTime.Now;
                    details.UpdatedDate = DateTime.Now;
                    await _detailsRepository.UpdateAsync(details, cancellationToken);
                }

                entity.MarkedForDeletion = true;
                entity.MarkedForDeletionDate = DateTime.Now;
                entity.UpdatedDate = DateTime.Now;
                await _repository.DeleteAsync(entity, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return true;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        #endregion

        #region CRUD Helper Methods

        private static AssetDetailsEntity BuildDetails(CreateAssetMasterDto dto, int assetId, int? userId)
            => new()
            {
                AssetId        = assetId,
                OrganizationId = dto.OrganizationId,
                ZoneId         = dto.ZoneId,
                WardId         = dto.WardId,
                MoujaId        = dto.MoujaId,
                SubZoneId      = dto.SubZoneId,
                AssetWardNo    = dto.AssetWardNo,
                PropertyNo     = dto.PropertyNo,
                PartitionNo    = dto.PartitionNo,
                UpicId         = dto.UpicId,
                PlotNo         = dto.PlotNo,
                CSN            = dto.CSN,
                LandRate       = dto.LandRate,
                LengthMtr       = dto.TotalLength,
                WidthMtr        = dto.AverageWidth,
                LandAreaSqMeter = dto.LandAreaSqMeter,
                LengthFt        = dto.LengthFt,
                WidthFt         = dto.WidthFt,
                LandAreaSqFeet  = dto.LandAreaSqFeet,
                Address        = dto.Address,
                NearestLandmark = dto.Locality,
                PinCode        = dto.PinCode,
                Latitude       = dto.Latitude,
                Longitude      = dto.Longitude,
                InChargeName        = dto.InChargeName,
                InChargeRegionalName = dto.InChargeRegionalName,
                InChargeDesignationId = dto.InChargeDesignationId,
                InChargeMobile      = dto.InChargeMobile,
                InChargeEmail       = dto.InChargeEmail,
                IsActive       = true,
                CreatedBy      = userId,
                CreatedDate    = DateTime.Now,
                MarkedForDeletion = false
            };

        private static void ApplyDetailsUpdate(UpdateAssetMasterDto dto, AssetDetailsEntity d, int? userId)
        {
            d.OrganizationId = dto.OrganizationId;
            d.ZoneId         = dto.ZoneId;
            d.WardId         = dto.WardId;
            d.MoujaId        = dto.MoujaId;
            d.SubZoneId      = dto.SubZoneId;
            d.AssetWardNo    = dto.AssetWardNo;
            d.PropertyNo     = dto.PropertyNo;
            d.PartitionNo    = dto.PartitionNo;
            d.UpicId         = dto.UpicId;
            d.PlotNo         = dto.PlotNo;
            d.CSN            = dto.CSN;
            d.LandRate       = dto.LandRate;
            d.LengthMtr       = dto.TotalLength;
            d.WidthMtr        = dto.AverageWidth;
            d.LandAreaSqMeter = dto.LandAreaSqMeter;
            d.LengthFt        = dto.LengthFt;
            d.WidthFt         = dto.WidthFt;
            d.LandAreaSqFeet  = dto.LandAreaSqFeet;
            d.Address        = dto.Address;
            d.NearestLandmark = dto.Locality;
            d.PinCode        = dto.PinCode;
            d.Latitude       = dto.Latitude;
            d.Longitude      = dto.Longitude;
            d.InChargeName        = dto.InChargeName;
            d.InChargeRegionalName = dto.InChargeRegionalName;
            d.InChargeDesignationId = dto.InChargeDesignationId;
            d.InChargeMobile      = dto.InChargeMobile;
            d.InChargeEmail       = dto.InChargeEmail;
            d.UpdatedBy      = userId;
            d.UpdatedDate    = DateTime.Now;
        }

        private async Task<PagedResult<AssetMasterDto>> GetAllInternalAsync(AssetMasterQueryParameters queryParameters, CancellationToken cancellationToken)
        {
            var query = _repository.GetQueryable()
                .AsNoTracking()
                .Where(a => !a.MarkedForDeletion);

            if (queryParameters.ParentAssetId.HasValue)
            {
                query = query.Where(a => a.ParentAssetId == queryParameters.ParentAssetId.Value);
            }
            else
            {
                query = query.Where(a => a.ParentAssetId == null);
            }

            query = query.ApplyFilters(queryParameters);
            query = query.ApplySearch(queryParameters);

            if (queryParameters.ZoneId.HasValue || queryParameters.WardId.HasValue || !string.IsNullOrWhiteSpace(queryParameters.Address))
            {
                var detailsQuery = _detailsRepository.GetQueryable().AsNoTracking();
                if (queryParameters.ZoneId.HasValue)
                    detailsQuery = detailsQuery.Where(d => d.ZoneId == queryParameters.ZoneId.Value);
                if (queryParameters.WardId.HasValue)
                    detailsQuery = detailsQuery.Where(d => d.WardId == queryParameters.WardId.Value);
                if (!string.IsNullOrWhiteSpace(queryParameters.Address))
                    detailsQuery = detailsQuery.Where(d => d.Address != null && d.Address.Contains(queryParameters.Address));
                var matchingAssetIds = detailsQuery.Select(d => d.AssetId);
                query = query.Where(a => matchingAssetIds.Contains(a.Id));
            }

            var stats = await query
                .GroupBy(a => 1)
                .Select(g => new
                {
                    TotalCount = g.Count(),
                    ActiveCount = g.Count(a => a.IsActive)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var totalCount = stats?.TotalCount ?? 0;
            var activeAssetsCount = stats?.ActiveCount ?? 0;

            decimal totalCapitalValue = 0m;
            if (totalCount > 0)
            {
                var matchingAssetIdsQuery = query.Select(a => a.Id);
                totalCapitalValue = await _floorDetailsRepository.GetQueryable()
                    .AsNoTracking()
                    .Where(fd => fd.IsActive && !fd.MarkedForDeletion &&
                                 (matchingAssetIdsQuery.Contains(fd.AssetId) ||
                                  (fd.Asset!.ParentAssetId.HasValue &&
                                   fd.Asset.IsActive &&
                                   !fd.Asset.MarkedForDeletion &&
                                   matchingAssetIdsQuery.Contains(fd.Asset.ParentAssetId.Value))))
                    .SumAsync(fd => fd.CapitalValue ?? 0m, cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(queryParameters.SortBy))
            {
                query = query.OrderByDescending(a => a.CreatedDate);
            }
            else if (string.Equals(queryParameters.SortBy, "CapitalValue", StringComparison.OrdinalIgnoreCase))
            {
                var fdQuery = _floorDetailsRepository.GetQueryable()
                    .Where(fd => fd.IsActive && !fd.MarkedForDeletion);

                if (queryParameters.SortOrder?.ToLower() == "desc")
                {
                    query = query.OrderByDescending(a =>
                        fdQuery.Where(fd => fd.AssetId == a.Id || (fd.Asset != null && fd.Asset.ParentAssetId == a.Id && fd.Asset.IsActive && !fd.Asset.MarkedForDeletion))
                               .Sum(fd => fd.CapitalValue ?? 0m)
                    );
                }
                else
                {
                    query = query.OrderBy(a =>
                        fdQuery.Where(fd => fd.AssetId == a.Id || (fd.Asset != null && fd.Asset.ParentAssetId == a.Id && fd.Asset.IsActive && !fd.Asset.MarkedForDeletion))
                               .Sum(fd => fd.CapitalValue ?? 0m)
                    );
                }
            }
            else if (string.Equals(queryParameters.SortBy, "AssetLife", StringComparison.OrdinalIgnoreCase))
            {
                var fdQuery = _floorDetailsRepository.GetQueryable()
                    .Where(fd => fd.IsActive && !fd.MarkedForDeletion && fd.ConstructionYear != null && fd.ConstructionYear != "");

                if (queryParameters.SortOrder?.ToLower() == "desc")
                {
                    query = query.OrderBy(a =>
                        _repository.GetQueryable()
                            .Where(child => child.ParentAssetId == a.Id && child.IsActive && !child.MarkedForDeletion)
                            .Join(fdQuery, child => child.Id, fd => fd.AssetId, (child, fd) => fd.ConstructionYear)
                            .Min()
                    );
                }
                else
                {
                    query = query.OrderByDescending(a =>
                        _repository.GetQueryable()
                            .Where(child => child.ParentAssetId == a.Id && child.IsActive && !child.MarkedForDeletion)
                            .Join(fdQuery, child => child.Id, fd => fd.AssetId, (child, fd) => fd.ConstructionYear)
                            .Min()
                    );
                }
            }
            else
            {
                query = query.ApplySort(queryParameters);
            }
            int pageNumber = queryParameters.PageNumber;
            int pageSize = queryParameters.PageSize;
            if (pageSize != -1)
            {
                query = query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize);
            }
            else
            {
                pageSize = totalCount > 0 ? totalCount : 1;
                pageNumber = 1;
            }
            var items = await query
                .Select(a => new AssetMasterDto
                {
                    Id = a.Id,
                    IsActive = a.IsActive,
                    CreatedDate = a.CreatedDate,
                    UpdatedDate = a.UpdatedDate,
                    AssetNo = a.AssetNo,
                    AssetName = a.AssetName,
                    AssetRegionalName = a.AssetRegionalName,
                    AssetCategoryId = a.AssetCategoryId,
                    AssetTypeId = a.AssetTypeId,
                    ParentAssetId = a.ParentAssetId,
                    HierarchyLevel = a.HierarchyLevel,
                    HierarchyPath = a.HierarchyPath,
                    OwnershipType = a.OwnershipType,
                    OccupancyStatus = a.OccupancyStatus,
                    AssetConditionId = a.AssetConditionId,
                    Details = new AssetDetailsDto
                    {
                        Id = a.Details != null ? a.Details.Id : 0,
                        PropertyNo = a.Details != null ? a.Details.PropertyNo : null,
                        PartitionNo = a.Details != null ? a.Details.PartitionNo : null,
                        PlotNo = a.Details != null ? a.Details.PlotNo : null,
                        UpicId = a.Details != null ? a.Details.UpicId : null,
                        InChargeName = a.Details != null ? a.Details.InChargeName : null,
                        InChargeRegionalName = a.Details != null ? a.Details.InChargeRegionalName : null,
                        InChargeDesignationId = a.Details != null ? a.Details.InChargeDesignationId : null,
                        InChargeMobile = a.Details != null ? a.Details.InChargeMobile : null,
                        InChargeEmail = a.Details != null ? a.Details.InChargeEmail : null,
                        LandRate = a.Details != null ? a.Details.LandRate : null
                    },
                    Names = new AssetMasterNamesDto
                    {
                        AssetCategoryName = a.AssetCategory != null ? a.AssetCategory.CategoryName : null,
                        AssetTypeName = a.AssetType != null ? a.AssetType.TypeName : null,
                        ParentAssetName = a.ParentAsset != null ? a.ParentAsset.AssetName : null
                    }
                })
                .ToListAsync(cancellationToken);

            if (items.Count > 0)
            {
                var assetIds = items.Select(x => x.Id).ToList();
                var assetImages = await _assetPhotoRepository.GetQueryable()
                    .AsNoTracking()
                    .Where(p => p.IsActive && !p.MarkedForDeletion && p.IsLatest && p.Remarks == "Asset Image" && assetIds.Contains(p.AssetId) && p.DocumentBinding != null)
                    .Select(p => new { p.AssetId, DocumentId = p.DocumentBinding!.DocumentId })
                    .ToListAsync(cancellationToken);

                var imageMap = assetImages
                    .GroupBy(x => x.AssetId)
                    .ToDictionary(g => g.Key, g => (int?)g.FirstOrDefault()?.DocumentId);

                // Single round trip for both the sub-unit counts and the oldest-construction-year lookup
                // (previously two separate queries over the same child+floor-details join) — both are
                // derived in-memory from the same row set.
                var childUnitRows = await (
                    from child in _repository.GetQueryable().AsNoTracking()
                    join fd in _floorDetailsRepository.GetQueryable().AsNoTracking()
                        on child.Id equals fd.AssetId
                    where child.ParentAssetId.HasValue
                          && assetIds.Contains(child.ParentAssetId.Value)
                          && !child.MarkedForDeletion
                          && child.IsActive
                          && fd.IsActive
                          && !fd.MarkedForDeletion
                    select new
                    {
                        ParentAssetId = child.ParentAssetId!.Value,
                        ChildId = child.Id,
                        fd.ConstructionYear
                    })
                    .ToListAsync(cancellationToken);

                var subUnitCounts = childUnitRows
                    .GroupBy(x => x.ParentAssetId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.ChildId).Distinct().Count());

                var conditionIds = items.Where(x => x.AssetConditionId.HasValue)
                    .Select(x => x.AssetConditionId!.Value).Distinct().ToList();
                var conditionNames = conditionIds.Count == 0
                    ? new Dictionary<int, string?>()
                    : await _conditionRepository.GetQueryable()
                        .AsNoTracking()
                        .Where(c => conditionIds.Contains(c.Id))
                        .ToDictionaryAsync(c => c.Id, c => (string?)c.ConditionName, cancellationToken);

                var oldestConstructionYearByAsset = childUnitRows
                    .Where(x => x.ConstructionYear != null)
                    .Select(x => new
                    {
                        x.ParentAssetId,
                        Year = ParseConstructionYear(x.ConstructionYear)
                    })
                    .Where(x => x.Year.HasValue && x.Year.Value > 0)
                    .GroupBy(x => x.ParentAssetId)
                    .ToDictionary(g => g.Key, g => g.Min(x => x.Year!.Value));

                int currentYear = DateTime.Now.Year;
                foreach (var item in items)
                {
                    if (imageMap.TryGetValue(item.Id, out var documentId))
                    {
                        item.AssetDocumentId = documentId;
                    }

                    item.TotalSubUnits = subUnitCounts.TryGetValue(item.Id, out var subUnitCount) ? subUnitCount : 0;

                    if (item.AssetConditionId.HasValue &&
                        conditionNames.TryGetValue(item.AssetConditionId.Value, out var conditionName))
                    {
                        item.Names.AssetCondition = conditionName;
                    }

                    if (oldestConstructionYearByAsset.TryGetValue(item.Id, out var oldestYear) &&
                        oldestYear > 0 &&
                        oldestYear <= currentYear)
                    {
                        item.AssetLife = currentYear - oldestYear;
                    }
                    else
                    {
                        item.AssetLife = null;
                    }
                }

                var capitalValues = await _floorDetailsRepository.GetQueryable()
                    .AsNoTracking()
                    .Where(fd => fd.IsActive && !fd.MarkedForDeletion &&
                                 (assetIds.Contains(fd.AssetId) ||
                                  (fd.Asset!.ParentAssetId.HasValue &&
                                   fd.Asset.IsActive &&
                                   !fd.Asset.MarkedForDeletion &&
                                   assetIds.Contains(fd.Asset.ParentAssetId.Value))))
                    .Select(fd => new
                    {
                        ParentAssetId = fd.Asset!.ParentAssetId ?? fd.AssetId,
                        CapitalValue = fd.CapitalValue ?? 0m
                    })
                    .GroupBy(x => x.ParentAssetId)
                    .Select(g => new { ParentAssetId = g.Key, Value = g.Sum(x => x.CapitalValue) })
                    .ToDictionaryAsync(g => g.ParentAssetId, g => g.Value, cancellationToken);

                foreach (var item in items)
                {
                    item.CapitalValue = capitalValues.TryGetValue(item.Id, out var capVal) ? capVal : 0m;
                }

                await EnrichLocationAsync(items, cancellationToken);
            }

            return new PagedAssetMasterResult(items, totalCount, pageNumber, pageSize)
            {
                ActiveAssetsCount = activeAssetsCount,
                TotalCapitalValue = totalCapitalValue
            };
        }

        protected override async Task<ValidationResult> ValidateForDeactivationAsync(
            int id,
            AssetMasterEntity currentEntity,
            AssetMasterEntity updatedEntity,
            CancellationToken cancellationToken = default)
        {
            if (currentEntity.IsActive && !updatedEntity.IsActive)
            {
                return await _referenceValidator.ValidateReferencesAsync<AssetMasterEntity>(id, cancellationToken);
            }
            return ValidationResult.Success();
        }

        protected override async Task<ValidationResult> ValidateForDeleteAsync(
            int id,
            AssetMasterEntity entity,
            CancellationToken cancellationToken = default)
        {
            return await _referenceValidator.ValidateReferencesAsync<AssetMasterEntity>(id, cancellationToken);
        }

        private static int? ParseConstructionYear(string? yearText)
        {
            if (string.IsNullOrWhiteSpace(yearText))
                return null;

            if (int.TryParse(yearText.Trim(), out var exactYear))
                return exactYear;

            var firstFourDigits = new string(yearText.Where(char.IsDigit).Take(4).ToArray());
            if (firstFourDigits.Length == 4 && int.TryParse(firstFourDigits, out var extractedYear))
                return extractedYear;

            return null;
        }

        #endregion
    }
}
