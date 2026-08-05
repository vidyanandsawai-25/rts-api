using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Constants;
using NtisPlatform.Application.DTOs.PropertyMapDetails;

namespace NtisPlatform.Application.Services
{
    public partial class PropertyMappingService
    {
        public async Task<List<PropertyMatchingResponseDto>> GetPropertyMatchingDetailsAsync(
            PropertyMapDetailsQueryParameters request,CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Retrieving property matching details for PropertyId: {PropertyId}, WingId: {WingId}, UserId: {UserId}",request.PropertyId,request.SocietyId,request.CreatedBy);

                var propertyId = request.PropertyId;
                var wingId = request.SocietyId ?? 0;
                var userId = request.CreatedBy;

                /*=========================================================
                  Step 1: Get selected property details
                =========================================================*/
                var propertyKey = await _repository.GetQueryable().AsNoTracking()
                    .Where(pm => pm.Id == propertyId && pm.IsActive && !pm.MarkedForDeletion)
                    .Select(pm => new { pm.WardId, pm.PropertyNo })
                    .FirstOrDefaultAsync(cancellationToken);

                if (propertyKey == null)
                {
                    _logger.LogWarning("Property not found for PropertyId: {PropertyId}", propertyId);
                    return new List<PropertyMatchingResponseDto>();
                }

                /*=========================================================
                  Step 2: Base properties
                =========================================================*/
                var baseProperties = await _repository.GetQueryable().AsNoTracking()
                    .Where(pm => pm.WardId == propertyKey.WardId &&
                                 pm.PropertyNo == propertyKey.PropertyNo &&
                                 pm.IsActive && !pm.MarkedForDeletion)
                    .Select(pm => new { pm.Id, pm.SocietyDetailId })
                    .Distinct()
                    .ToListAsync(cancellationToken);

                if (baseProperties.Count == 0)
                {
                    _logger.LogWarning("No base properties found for PropertyId: {PropertyId}", propertyId);
                    return new List<PropertyMatchingResponseDto>();
                }

                var basePropertyIds = baseProperties.Select(x => x.Id).ToHashSet();
                var societyDetailIds = baseProperties
                    .Where(x => x.SocietyDetailId.HasValue)
                    .Select(x => x.SocietyDetailId!.Value).Distinct().ToList();

                if (societyDetailIds.Count == 0)
                {
                    return new List<PropertyMatchingResponseDto>();
                }

                /*=========================================================
                  Step 3: Get wing key
                =========================================================*/
                string? wingKeyFilter = null;
                if (wingId != 0)
                {
                    wingKeyFilter = await _societyRepository.GetQueryable().AsNoTracking()
                        .Where(sd => societyDetailIds.Contains(sd.Id) &&
                                     sd.WingId == wingId &&
                                     sd.IsActive && !sd.MarkedForDeletion)
                        .Select(sd => (sd.WingName ?? string.Empty).Trim().ToUpper())
                        .FirstOrDefaultAsync(cancellationToken);

                    if (wingKeyFilter == null)
                    {
                        return new List<PropertyMatchingResponseDto>();
                    }
                }

                /*=========================================================
                  Step 4: New-property base records
                =========================================================*/
                var rawNewProperties = await
                    (
                        from pm in _repository.GetQueryable().AsNoTracking()
                        join sd in _societyRepository.GetQueryable().AsNoTracking()
                            on pm.SocietyDetailId equals sd.Id
                        join ptm in _propertyTypeRepository.GetQueryable().AsNoTracking().Where(x => x.IsActive)
                            on pm.PropertyTypeId equals ptm.Id into ptmJoin
                        from ptm in ptmJoin.DefaultIfEmpty()
                        join wm in _wingMasterRepository.GetQueryable().AsNoTracking()
                            on sd.WingId equals wm.Id into wmJoin
                        from wm in wmJoin.DefaultIfEmpty()
                        where pm.SocietyDetailId.HasValue
                           && societyDetailIds.Contains(pm.SocietyDetailId.Value)
                           && pm.IsActive && !pm.MarkedForDeletion
                           && sd.IsActive && !sd.MarkedForDeletion
                           && !string.IsNullOrWhiteSpace(pm.PartitionNo)
                           && (wingId == 0 || sd.WingId == wingId)
                           && (ptm == null || ptm.PartType != "Amenity")
                           && (wm == null || pm.PartitionNo != wm.WingNo)
                        select new
                        {
                            pm.Id,
                            pm.WardId,
                            pm.PropertyNo,
                            pm.PartitionNo,
                            pm.SocietyDetailId,
                            pm.FlatOrShopNo,
                            pm.FlatOrShopName,
                            pm.MobileNo,
                            pm.PropertyTypeId,
                            pm.OwnerName,
                            pm.OccupierName,
                            pm.Type,
                            sd.WingName,
                            sd.WingId
                        }
                    ).ToListAsync(cancellationToken);

                if (rawNewProperties.Count == 0)
                {
                    return new List<PropertyMatchingResponseDto>();
                }

                var newPropertyBaseRecords = rawNewProperties.Select(pm => new
                {
                    pm.Id,
                    pm.WardId,
                    pm.PropertyNo,
                    pm.PartitionNo,
                    pm.SocietyDetailId,
                    pm.FlatOrShopNo,
                    pm.FlatOrShopName,
                    pm.MobileNo,
                    pm.PropertyTypeId,
                    pm.OwnerName,
                    pm.OccupierName,
                    pm.Type,
                    pm.WingName,
                    pm.WingId,
                    WingKey = (pm.WingName ?? string.Empty).Trim().ToUpper(),
                    FlatKey = (pm.FlatOrShopNo ?? string.Empty).Trim().ToUpper()
                }).ToList();

                var newPropertyIds = newPropertyBaseRecords
                    .Select(x => x.Id)
                    .Distinct()
                    .ToList();

                /*=========================================================
                  Step 5: Latest PropertyMastDetails (High-perf in-memory group)
                =========================================================*/
                var assessmentRecords = await _assessmentRepository.GetQueryable().AsNoTracking()
                    .Where(pmd => newPropertyIds.Contains(pmd.PropertyId) && pmd.IsActive && !pmd.MarkedForDeletion)
                    .Select(pmd => new { pmd.Id, pmd.PropertyId, pmd.BHK })
                    .ToListAsync(cancellationToken);

                var propertyMastDetailsLookup = assessmentRecords
                    .GroupBy(x => x.PropertyId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First().BHK);

                /*=========================================================
                  Step 6: Latest PropertyDetails (High-perf in-memory group)
                =========================================================*/
                var propertyDetailsRaw = await _propertyDetailsRepository.GetQueryable().AsNoTracking()
                    .Where(pd => newPropertyIds.Contains(pd.PropertyId) && pd.IsActive && !pd.MarkedForDeletion)
                    .Select(pd => new
                    {
                        pd.Id,
                        pd.PropertyId,
                        pd.TypeOfUseId,
                        pd.FloorId,
                        pd.AssessmentYear,
                        pd.ConstructionYear,
                        pd.SubTypeOfUseId,
                        pd.ConstructionTypeId
                    })
                    .ToListAsync(cancellationToken);

                var propertyDetailsLookup = propertyDetailsRaw
                    .GroupBy(x => x.PropertyId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First());

                /*=========================================================
                  Step 7: Master lookups for descriptions & enrich AllNewProperties
                =========================================================*/
                var typeOfUseIds = propertyDetailsLookup.Values
                    .Select(x => x.TypeOfUseId)
                    .Distinct()
                    .ToList();

                var floorIds = propertyDetailsLookup.Values
                    .Where(x => x.FloorId.HasValue && x.FloorId.Value > 0)
                    .Select(x => x.FloorId!.Value)
                    .Distinct()
                    .ToList();

                var propertyTypeIds = newPropertyBaseRecords
                    .Where(x => x.PropertyTypeId.HasValue && x.PropertyTypeId.Value > 0)
                    .Select(x => x.PropertyTypeId!.Value)
                    .Distinct()
                    .ToList();

                var subTypeOfUseIds = propertyDetailsLookup.Values
                    .Where(x => x.SubTypeOfUseId.HasValue && x.SubTypeOfUseId.Value > 0)
                    .Select(x => x.SubTypeOfUseId!.Value)
                    .Distinct()
                    .ToList();

                var constructionTypeIds = propertyDetailsLookup.Values
                    .Where(x => x.ConstructionTypeId.HasValue && x.ConstructionTypeId.Value > 0)
                    .Select(x => x.ConstructionTypeId!.Value)
                    .Distinct()
                    .ToList();

                var typeOfUseLookup = typeOfUseIds.Count == 0
                    ? new Dictionary<int, string?>()
                    : await _typeOfUseRepository.GetQueryable().AsNoTracking()
                        .Where(tou => typeOfUseIds.Contains(tou.Id) && tou.IsActive)
                        .Select(tou => new { tou.Id, tou.Description })
                        .ToDictionaryAsync(x => x.Id, x => x.Description, cancellationToken);

                var floorLookup = floorIds.Count == 0
                    ? new Dictionary<int, string?>()
                    : await _floorRepository.GetQueryable().AsNoTracking()
                        .Where(fm => floorIds.Contains(fm.Id) && fm.IsActive)
                        .Select(fm => new { fm.Id, fm.Description })
                        .ToDictionaryAsync(x => x.Id, x => x.Description, cancellationToken);

                var propertyTypeLookup = propertyTypeIds.Count == 0
                    ? new Dictionary<int, string?>()
                    : await _propertyTypeRepository.GetQueryable().AsNoTracking()
                        .Where(pt => propertyTypeIds.Contains(pt.Id) && pt.IsActive)
                        .Select(pt => new { pt.Id, pt.PropertyDescription })
                        .ToDictionaryAsync(x => x.Id, x => (string?)x.PropertyDescription, cancellationToken);

                var subTypeOfUseLookup = subTypeOfUseIds.Count == 0
                    ? new Dictionary<int, string?>()
                    : await _subTypeOfUseRepository.GetQueryable().AsNoTracking()
                        .Where(stu => subTypeOfUseIds.Contains(stu.Id) && stu.IsActive)
                        .Select(stu => new { stu.Id, stu.Description })
                        .ToDictionaryAsync(x => x.Id, x => x.Description, cancellationToken);

                var constructionTypeLookup = constructionTypeIds.Count == 0
                    ? new Dictionary<int, string?>()
                    : await _constructionTypeRepository.GetQueryable().AsNoTracking()
                        .Where(ct => constructionTypeIds.Contains(ct.Id) && ct.IsActive)
                        .Select(ct => new { ct.Id, ct.Description })
                        .ToDictionaryAsync(x => x.Id, x => x.Description, cancellationToken);

                var allNewProperties = newPropertyBaseRecords.Select(newProperty =>
                {
                    propertyMastDetailsLookup.TryGetValue(newProperty.Id, out var bhk);
                    propertyDetailsLookup.TryGetValue(newProperty.Id, out var propertyDetail);

                    typeOfUseLookup.TryGetValue(propertyDetail?.TypeOfUseId ?? 0, out var typeOfUseDescription);
                    floorLookup.TryGetValue(propertyDetail?.FloorId ?? 0, out var floorDescription);
                    propertyTypeLookup.TryGetValue(newProperty.PropertyTypeId ?? 0, out var propertyTypeDescription);
                    subTypeOfUseLookup.TryGetValue(propertyDetail?.SubTypeOfUseId ?? 0, out var subTypeOfUseDescription);
                    constructionTypeLookup.TryGetValue(propertyDetail?.ConstructionTypeId ?? 0, out var constructionTypeDescription);

                    return new
                    {
                        newProperty.Id,
                        newProperty.WardId,
                        newProperty.PropertyNo,
                        newProperty.PartitionNo,
                        newProperty.SocietyDetailId,
                        newProperty.FlatOrShopNo,
                        newProperty.FlatOrShopName,
                        newProperty.MobileNo,
                        newProperty.WingName,
                        newProperty.WingId,
                        newProperty.WingKey,
                        newProperty.FlatKey,
                        newProperty.OwnerName,
                        newProperty.OccupierName,
                        newProperty.Type,
                        newProperty.PropertyTypeId,
                        BHK = bhk,
                        TypeOfUseId = propertyDetail?.TypeOfUseId,
                        TypeOfUseDescription = typeOfUseDescription,
                        FloorId = propertyDetail?.FloorId,
                        FloorDescription = floorDescription,
                        AssessmentYear = propertyDetail?.AssessmentYear,
                        ConstructionYear = propertyDetail?.ConstructionYear,
                        PropertyTypeDescription = propertyTypeDescription,
                        SubTypeOfUseId = propertyDetail?.SubTypeOfUseId,
                        SubTypeOfUse = subTypeOfUseDescription,
                        ConstructionTypeId = propertyDetail?.ConstructionTypeId,
                        ConstructionType = constructionTypeDescription
                    };
                }).ToList();

                /*=========================================================
                  Step 7.1: Deduplicate normal new properties
                =========================================================*/
                var uniqueNewProperties = allNewProperties
                    .GroupBy(x => new { x.WingKey, x.FlatKey })
                    .Select(g => g.OrderBy(x => x.Id).First())
                    .ToList();

                /*=========================================================
                  Step 8: Get old society names
                =========================================================*/
                var rawOldSocietyNames = await
                (
                    from opm in _propertyOldRepository.GetQueryable().AsNoTracking()
                    join pmap in _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                        on opm.Id equals pmap.PropertyIdOld
                    where pmap.PropertyIdNew.HasValue
                       && basePropertyIds.Contains(pmap.PropertyIdNew.Value)
                       && pmap.IsActive
                       && pmap.Status == PropertyMapStatus.Draft
                       && pmap.UpdatedBy == userId
                       && opm.IsActive
                       && !opm.MarkedForDeletion
                       && opm.OldSocietyName != null
                       && opm.OldSocietyName != ""
                    select opm.OldSocietyName
                )
                .Distinct()
                .ToListAsync(cancellationToken);

                var oldSocietyNames = rawOldSocietyNames
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct()
                    .ToList();

                if(oldSocietyNames.Count == 0)
                {
                    return new List<PropertyMatchingResponseDto>();
                }

                /*=========================================================
                  Step 8.1: Merge mappings (Status = ACTIVE) filtered by newPropertyIds
                =========================================================*/
                var mergeMappingRecords = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                    .Where(pmap => pmap.PropertyIdOld.HasValue
                                && pmap.PropertyIdNew.HasValue
                                && newPropertyIds.Contains(pmap.PropertyIdNew.Value)
                                && pmap.IsActive
                                && pmap.Status == PropertyMapStatus.Active)
                    .Select(pmap => new
                    {
                        MappingId = pmap.Id,
                        pmap.PropertyMapId,
                        PropertyIdOld = pmap.PropertyIdOld!.Value,
                        PropertyIdNew = pmap.PropertyIdNew!.Value,
                        pmap.PropertyNo,
                        pmap.PropertySide,
                        pmap.Remark,
                        pmap.UpdatedDate,
                        pmap.IsCurrent
                    })
                    .ToListAsync(cancellationToken);

                var mergeMappings = mergeMappingRecords
                    .GroupBy(pmap => new
                    {
                        pmap.PropertyIdOld,
                        pmap.PropertyIdNew
                    })
                    .Select(g => g.OrderByDescending(x => x.IsCurrent)
                                  .ThenByDescending(x => x.UpdatedDate)
                                  .ThenByDescending(x => x.MappingId)
                                  .First())
                    .ToList();

                var mergeOldPropertyIds = mergeMappings.Select(x => x.PropertyIdOld).ToHashSet();
                var mergeNewPropertyIds = mergeMappings.Select(x => x.PropertyIdNew).ToHashSet();

                /*=========================================================
                  Step 9: Get old properties
                =========================================================*/
                var oldPropertyBaseList = oldSocietyNames.Count == 0
                    ? new List<OldPropertyBase>()
                    : (await _propertyOldRepository.GetQueryable().AsNoTracking()
                        .Where(opm => opm.OldSocietyName != null
                                   && oldSocietyNames.Contains(opm.OldSocietyName.Trim())
                                   && opm.IsActive
                                   && !opm.MarkedForDeletion)
                        .Select(opm => new
                        {
                            opm.Id,
                            opm.OldSocietyName,
                            opm.OldWardNo,
                            opm.OldPropertyNo,
                            opm.OldPartitionNo,
                            opm.OldOwnerName,
                            opm.OldOccupierName,
                            opm.OldRV,
                            opm.OldTotalTax,
                            opm.OldGeneralTax,
                            opm.OldAddress,
                            opm.OldWing,
                            opm.OldFlatOrShopNumber
                        })
                        .ToListAsync(cancellationToken))
                        .Select(opm => new OldPropertyBase
                        {
                            Id = opm.Id,
                            OldSocietyName = opm.OldSocietyName,
                            OldWardNo = opm.OldWardNo,
                            OldPropertyNo = opm.OldPropertyNo,
                            OldPartitionNo = opm.OldPartitionNo,
                            OldOwnerName = opm.OldOwnerName,
                            OldOccupierName = opm.OldOccupierName,
                            OldRV = opm.OldRV,
                            OldTotalTax = opm.OldTotalTax,
                            OldGeneralTax = opm.OldGeneralTax,
                            OldAddress = opm.OldAddress,
                            OldWing = opm.OldWing,
                            OldFlatOrShopNumber = opm.OldFlatOrShopNumber,
                            WingKey = (opm.OldWing ?? string.Empty).Trim().ToUpper(),
                            FlatKey = (opm.OldFlatOrShopNumber ?? string.Empty).Trim().ToUpper()
                        })
                        .ToList();

                if (wingId != 0 && wingKeyFilter != null && oldPropertyBaseList.Count > 0)
                {
                    oldPropertyBaseList = oldPropertyBaseList
                        .Where(x => x.WingKey == wingKeyFilter)
                        .ToList();
                }

                var oldPropertyIds = oldPropertyBaseList.Select(x => x.Id).Distinct().ToList();

                /*=========================================================
                  Step 10: Latest active mappings (Status = DRAFT) for Identify
                =========================================================*/
                var latestActiveMappings = oldPropertyIds.Count == 0
                    ? new List<LatestMapping>()
                    : (await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                        .Where(pmap => pmap.PropertyIdOld.HasValue
                                    && oldPropertyIds.Contains(pmap.PropertyIdOld.Value)
                                    && pmap.IsActive
                                    && pmap.Status == PropertyMapStatus.Draft)
                        .Select(pmap => new LatestMapping
                        {
                            PropertyIdOld = pmap.PropertyIdOld!.Value,
                            Id = pmap.Id,
                            UpdatedBy = pmap.UpdatedBy,
                            UpdatedDate = pmap.UpdatedDate
                        })
                        .ToListAsync(cancellationToken))
                        .GroupBy(x => x.PropertyIdOld)
                        .Select(g => g.OrderByDescending(x => x.UpdatedDate).ThenByDescending(x => x.Id).First())
                        .ToList();

                var mappingLookup = latestActiveMappings.ToDictionary(x => x.PropertyIdOld);

                var updatedByIds = latestActiveMappings
                    .Where(x => x.UpdatedBy.HasValue)
                    .Select(x => x.UpdatedBy!.Value)
                    .Distinct()
                    .ToList();

                var userLookup = updatedByIds.Count == 0
                    ? new Dictionary<int, string>()
                    : await _userRepository.GetQueryable().AsNoTracking()
                        .Where(u => updatedByIds.Contains(u.Id) && u.IsActive)
                        .Select(u => new
                        {
                            u.Id,
                            FullName = (u.FirstName + " " + (u.LastName ?? string.Empty)).Trim()
                        })
                        .ToDictionaryAsync(x => x.Id, x => x.FullName, cancellationToken);

                /*=========================================================
                  Step 11: Enrich and deduplicate old properties
                =========================================================*/
                var enrichedOldProperties = oldPropertyBaseList.Select(oldProperty =>
                {
                    mappingLookup.TryGetValue(oldProperty.Id, out var mapping);
                    string? identifyName = null;
                    if (mapping?.UpdatedBy != null && userLookup.TryGetValue(mapping.UpdatedBy.Value, out var fullName))
                    {
                        identifyName = string.IsNullOrWhiteSpace(fullName) ? null : fullName;
                    }

                    var isMerge = mergeOldPropertyIds.Contains(oldProperty.Id);

                    return new
                    {
                        oldProperty.Id,
                        oldProperty.OldSocietyName,
                        oldProperty.OldWardNo,
                        oldProperty.OldPropertyNo,
                        oldProperty.OldPartitionNo,
                        oldProperty.OldOwnerName,
                        oldProperty.OldOccupierName,
                        oldProperty.OldRV,
                        oldProperty.OldTotalTax,
                        oldProperty.OldGeneralTax,
                        oldProperty.OldAddress,
                        oldProperty.OldWing,
                        oldProperty.OldFlatOrShopNumber,
                        oldProperty.WingKey,
                        oldProperty.FlatKey,
                        IsMerge = isMerge,
                        Identify = mapping != null,
                        IdentifyName = identifyName,
                        IdentifyDate = mapping?.UpdatedDate
                    };
                }).ToList();

                var uniqueOldProperties = enrichedOldProperties
                    .GroupBy(x => new { x.WingKey, x.FlatKey })
                    .Select(g => g.OrderByDescending(x => x.IsMerge).ThenBy(x => x.Id).First())
                    .ToList();

                /*=========================================================
                  Step 12 & 13: Combine Results and Apply Ordering
                =========================================================*/
                var combinedResults = new List<(
                    PropertyMatchingResponseDto Result,
                    int SortSource,
                    string SortWing,
                    string SortFlat,
                    long? OldPropertyId,
                    long? NewPropertyId)>();

                var allNewPropertiesById = allNewProperties.ToDictionary(x => x.Id);

                // Step 12.1: Merge Rows (OLD.IsMerge = 1)
                var mergeOldProperties = uniqueOldProperties.Where(x => x.IsMerge).ToList();
                foreach (var oldProperty in mergeOldProperties)
                {
                    var relevantMergeMappings = mergeMappings.Where(m => m.PropertyIdOld == oldProperty.Id).ToList();
                    foreach (var mm in relevantMergeMappings)
                    {
                        if (!allNewPropertiesById.TryGetValue(mm.PropertyIdNew, out var newProperty))
                        {
                            continue;
                        }

                        combinedResults.Add((
                            new PropertyMatchingResponseDto
                            {
                                RowSource = "Merge",
                                PropertyId = newProperty.Id,
                                WardNo = newProperty.WardId.ToString(),
                                PropertyNo = newProperty.PropertyNo,
                                PartitionNo = newProperty.PartitionNo,
                                WingName = newProperty.WingName,
                                FlatShopNo = newProperty.FlatOrShopNo,
                                MobileNo = newProperty.MobileNo,
                                ShopName = newProperty.FlatOrShopName,
                                BHK = newProperty.BHK,
                                TypeOfUseId = newProperty.TypeOfUseId,
                                TypeOfUse = newProperty.TypeOfUseDescription,
                                FloorId = newProperty.FloorId,
                                Floor = newProperty.FloorDescription,
                                AssessmentYear = newProperty.AssessmentYear,
                                ConstructionYear = newProperty.ConstructionYear,
                                PropertyTypeId = newProperty.PropertyTypeId,
                                PropertyTypeDescription = newProperty.PropertyTypeDescription,
                                SubTypeOfUseId = newProperty.SubTypeOfUseId,
                                SubTypeOfUse = newProperty.SubTypeOfUse,
                                OwnerName = newProperty.OwnerName,
                                OccupierName = newProperty.OccupierName,
                                Type = newProperty.Type,
                                ConstructionTypeId = newProperty.ConstructionTypeId,
                                ConstructionType = newProperty.ConstructionType,
                                IsPhoto = null,
                                OldSocietyName = oldProperty.OldSocietyName,
                                OldPropertyId = oldProperty.Id,
                                OldWardNo = oldProperty.OldWardNo,
                                OldPropertyNo = oldProperty.OldPropertyNo,
                                OldPartitionNo = oldProperty.OldPartitionNo,
                                OldOwnerName = oldProperty.OldOwnerName,
                                OldOccupierName = oldProperty.OldOccupierName,
                                OldRv = oldProperty.OldRV?.ToString(),
                                OldTotalTax = oldProperty.OldTotalTax.HasValue ? Convert.ToDecimal(oldProperty.OldTotalTax.Value) : null,
                                OldPropertyTax = oldProperty.OldGeneralTax.HasValue ? Convert.ToDecimal(oldProperty.OldGeneralTax.Value) : null,
                                OldAddress = oldProperty.OldAddress,
                                OldWingName = oldProperty.OldWing,
                                OldFlatShopNo = oldProperty.OldFlatOrShopNumber,
                                IsMatchProperty = true,
                                Identify = oldProperty.Identify,
                                IdentifyName = oldProperty.IdentifyName,
                                IdentifyDate = oldProperty.IdentifyDate,
                                IsMerge = true
                            },
                            2, // SortSource = 2 for Merge
                            newProperty.WingName ?? oldProperty.OldWing ?? string.Empty,
                            newProperty.FlatOrShopNo ?? oldProperty.OldFlatOrShopNumber ?? string.Empty,
                            oldProperty.Id,
                            newProperty.Id));
                    }
                }

                // Step 12.2: Normal Rows (MATCHED / OLD / NEW)
                // Filter: OLD where IsMerge = 0
                var normalOldProperties = uniqueOldProperties.Where(x => !x.IsMerge).ToList();

                // Filter: NEW where not exists in MergeMappings (NEW.Id not in mergeNewPropertyIds)
                var normalNewProperties = uniqueNewProperties.Where(x => !mergeNewPropertyIds.Contains(x.Id)).ToList();

                var oldPropertyLookup = normalOldProperties.ToDictionary(x => (x.WingKey, x.FlatKey));
                var newPropertyLookup = normalNewProperties.ToDictionary(x => (x.WingKey, x.FlatKey));

                var allNormalKeys = oldPropertyLookup.Keys.Union(newPropertyLookup.Keys).ToList();

                foreach (var key in allNormalKeys)
                {
                    oldPropertyLookup.TryGetValue(key, out var oldProperty);
                    newPropertyLookup.TryGetValue(key, out var newProperty);

                    var rowSource = oldProperty != null && newProperty != null
                        ? "MATCHED"
                        : newProperty != null
                            ? "NEW"
                            : "OLD";

                    var sortSource = rowSource switch
                    {
                        "MATCHED" => 1,
                        "NEW" => 3,
                        "OLD" => 4,
                        _ => 5
                    };

                    combinedResults.Add((
                        new PropertyMatchingResponseDto
                        {
                            RowSource = rowSource,
                            PropertyId = newProperty?.Id,
                            WardNo = newProperty?.WardId.ToString(),
                            PropertyNo = newProperty?.PropertyNo,
                            PartitionNo = newProperty?.PartitionNo,
                            WingName = newProperty?.WingName,
                            FlatShopNo = newProperty?.FlatOrShopNo,
                            MobileNo = newProperty?.MobileNo,
                            ShopName = newProperty?.FlatOrShopName,
                            BHK = newProperty?.BHK,
                            TypeOfUseId = newProperty?.TypeOfUseId,
                            TypeOfUse = newProperty?.TypeOfUseDescription,
                            FloorId = newProperty?.FloorId,
                            Floor = newProperty?.FloorDescription,
                            AssessmentYear = newProperty?.AssessmentYear,
                            ConstructionYear = newProperty?.ConstructionYear,
                            PropertyTypeId = newProperty?.PropertyTypeId,
                            PropertyTypeDescription = newProperty?.PropertyTypeDescription,
                            SubTypeOfUseId = newProperty?.SubTypeOfUseId,
                            SubTypeOfUse = newProperty?.SubTypeOfUse,
                            OwnerName = newProperty?.OwnerName,
                            OccupierName = newProperty?.OccupierName,
                            Type = newProperty?.Type,
                            ConstructionTypeId = newProperty?.ConstructionTypeId,
                            ConstructionType = newProperty?.ConstructionType,
                            IsPhoto = null,
                            OldSocietyName = oldProperty?.OldSocietyName,
                            OldPropertyId = oldProperty?.Id,
                            OldWardNo = oldProperty?.OldWardNo,
                            OldPropertyNo = oldProperty?.OldPropertyNo,
                            OldPartitionNo = oldProperty?.OldPartitionNo,
                            OldOwnerName = oldProperty?.OldOwnerName,
                            OldOccupierName = oldProperty?.OldOccupierName,
                            OldRv = oldProperty?.OldRV?.ToString(),
                            OldTotalTax = oldProperty?.OldTotalTax.HasValue == true ? Convert.ToDecimal(oldProperty.OldTotalTax.Value) : null,
                            OldPropertyTax = oldProperty?.OldGeneralTax.HasValue == true ? Convert.ToDecimal(oldProperty.OldGeneralTax.Value) : null,
                            OldAddress = oldProperty?.OldAddress,
                            OldWingName = oldProperty?.OldWing,
                            OldFlatShopNo = oldProperty?.OldFlatOrShopNumber,
                            IsMatchProperty = oldProperty != null && newProperty != null,
                            Identify = oldProperty?.Identify ?? false,
                            IdentifyName = oldProperty?.IdentifyName,
                            IdentifyDate = oldProperty?.IdentifyDate,
                            IsMerge = false
                        },
                        sortSource,
                        newProperty?.WingName ?? oldProperty?.OldWing ?? string.Empty,
                        newProperty?.FlatOrShopNo ?? oldProperty?.OldFlatOrShopNumber ?? string.Empty,
                        oldProperty?.Id,
                        newProperty?.Id));
                }

                // Step 13: Final Ordering
                var orderedResults = combinedResults
                    .OrderBy(x => x.SortSource)
                    .ThenBy(x => x.SortWing)
                    .ThenBy(x => x.SortFlat)
                    .ThenBy(x => x.OldPropertyId)
                    .ThenBy(x => x.NewPropertyId)
                    .Select(x => x.Result)
                    .ToList();

                _logger.LogInformation("Retrieved {Count} property matching details for PropertyId: {PropertyId}",orderedResults.Count,propertyId);

                return orderedResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Error retrieving property matching details for PropertyId: {PropertyId}",request.PropertyId);
                throw;
            }
        }
    }
}
