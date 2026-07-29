using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Services;

public partial class PropertyService
{
    /// <summary>
    /// Merges a new property (PropertyMast) with an old property (PropertyMastOld) by:
    /// <list type="number">
    ///   <item>Validating that the PropertyMap, PropertyMastOld, and PropertyMast all exist and are active.</item>
    ///   <item>Ensuring neither the new nor the old property is already merged.</item>
    ///   <item>Confirming that no active MODIFIED PropertyMapDetail already links this pair.</item>
    ///   <item>Creating two <see cref="PropertyMapDetailEntity"/> records (sides "OLD" and "NEW").</item>
    /// </list>
    /// </summary>
    public async Task<PropertyResponse> MergePropertyAsync(PropertyMergeDto dto, CancellationToken cancellationToken = default)
    {
        var response = new PropertyResponse();
        try
        {
            var newPropertyCount = dto.PropertyIds?.Count ?? 0;
            var oldPropertyCount = dto.PropertyOldIds?.Count ?? 0;

            if (newPropertyCount == 0 || oldPropertyCount == 0)
            {
                response.Success = false;
                response.Message = "Property details are required";
                return response;
            }

            string mappingCategory;
            if (newPropertyCount == 1 && oldPropertyCount == 1)
            {
                mappingCategory = "ONE_TO_ONE";
            }
            else if (newPropertyCount > 1 && oldPropertyCount == 1)
            {
                mappingCategory = "SPLIT";
            }
            else if (newPropertyCount == 1 && oldPropertyCount > 1)
            {
                mappingCategory = "MERGE";
            }
            else
            {
                response.Success = false;
                response.Message ="Multiple old properties cannot be merged with multiple new properties";
                return response;
            }

            var propertyMapId = await _propertyMapMasterRepository.GetQueryable().AsNoTracking()
                .Where(x => x.IsActive && x.MappingCategory == mappingCategory)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (!propertyMapId.HasValue)
            {
                response.Success = false;
                response.Message =$"{mappingCategory} property mapping category not found";
                return response;
            }

            dto.PropertyMapId = propertyMapId.Value;
            return mappingCategory switch
            {
                "ONE_TO_ONE" => await MergeSingleProperty(dto, cancellationToken),
                "SPLIT"      => await MergeSplitProperty(dto, cancellationToken),
                "MERGE"      => await MergeMultipleProperty(dto, cancellationToken),
                _            => throw new InvalidOperationException(
                                $"Unsupported mapping category: {mappingCategory}")
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Merges a single old property with a single new property (optimized)
    /// </summary>
    private async Task<PropertyResponse> MergeSingleProperty(PropertyMergeDto dto, CancellationToken cancellationToken = default)
    {
        var response = new PropertyResponse();
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // ── 1. Validate input ────────────────────────────────────────────────────
            if (dto.PropertyOldIds == null || dto.PropertyOldIds.Count != 1)
            {
                response.Success = false;
                response.Message = "Single merge operation requires exactly one old propertyNo";
                return response;
            }

            if (dto.PropertyIds == null || dto.PropertyIds.Count != 1)
            {
                response.Success = false;
                response.Message = "Single merge operation requires exactly one new propertyNo";
                return response;
            }

            int propertyOldId = dto.PropertyOldIds[0];
            int propertyId = dto.PropertyIds[0];

            // ── 2. Parallel validation queries (3 queries at once) ──────────────────
            var propertyMastOld = await _propertyOldRepository
                .GetQueryable()
                .AsNoTracking()
                .Where(pm => pm.Id == propertyOldId && pm.IsActive && !pm.MarkedForDeletion)
                .Select(p => new { p.OldPropertyNo, p.Id, p.OldWardNo, p.OldPartitionNo })
                .FirstOrDefaultAsync(cancellationToken);

            var propertyMast = await (
                from pm in _repository.GetQueryable().AsNoTracking()
                join wd in _wardRepository.GetQueryable().AsNoTracking() on pm.WardId equals wd.Id
                where pm.Id == propertyId && pm.IsActive && !pm.MarkedForDeletion
                select new { pm.Id, wd.WardNo, pm.PropertyNo, pm.PartitionNo })
                .FirstOrDefaultAsync(cancellationToken);

            // ── 3. Validate results ──────────────────────────────────────────────────
            if (propertyMastOld == null)
            {
                response.Success = false;
                response.Message = "Old Property not found";
                return response;
            }

            if (propertyMast == null)
            {
                response.Success = false;
                response.Message = "New Property not found";
                return response;
            }

            // ── 4. Parse coordinates ─────────────────────────────────────────────────
            decimal? latitude = !string.IsNullOrWhiteSpace(dto.Latitude) && decimal.TryParse(dto.Latitude, out var lat) ? lat : null;
            decimal? longitude = !string.IsNullOrWhiteSpace(dto.Longitude) && decimal.TryParse(dto.Longitude, out var lon) ? lon : null;
            var now = DateTime.Now;

            var newPropertyNo = BuildPropertyNumber(propertyMast.WardNo, propertyMast.PropertyNo, propertyMast.PartitionNo);
            var oldPropertyNo = BuildPropertyNumber(propertyMastOld.OldWardNo, propertyMastOld.OldPropertyNo, propertyMastOld.OldPartitionNo);

            var existsNewMergeProperty = await _propertyMapDetailRepository
                .GetQueryable()
                .AsNoTracking()
                .Where(pm => pm.PropertyIdNew == propertyId && pm.IsActive && pm.Status == "MODIFIED" && pm.PropertySide == "NEW")
                .Select(pm => pm.PropertyNo)
                .FirstOrDefaultAsync(cancellationToken);

            var existingMerge = await (
                        from oldMap in _propertyMapDetailRepository
                            .GetQueryable()
                            .AsNoTracking()
                        join newMap in _propertyMapDetailRepository
                            .GetQueryable()
                            .AsNoTracking()
                            on oldMap.PropertyIdNew equals newMap.PropertyIdNew
                        where oldMap.PropertyIdOld == propertyOldId
                              && oldMap.IsActive
                              && oldMap.Status == "MODIFIED"
                              && oldMap.PropertySide == "OLD"
                              && newMap.IsActive
                              && newMap.Status == "MODIFIED"
                              && newMap.PropertySide == "NEW"
                        select new
                        {
                            OldPropertyNo = oldMap.PropertyNo,
                            NewPropertyNo = newMap.PropertyNo
                        })
                        .FirstOrDefaultAsync(cancellationToken);

            if (existingMerge != null)
            {
                response.Success = false;
                response.Message = $"Old property no  {existingMerge!.OldPropertyNo} already merged for new property no : {existingMerge!.NewPropertyNo}";
                return response;
            }

            // ── 5. Build PropertyMapDetail entities ──────────────────────────────────
            var mapDetails = new List<PropertyMapDetailEntity>(2);

            var oldData = CreatePropertyMergeDetail(
                           propertyMapId: dto.PropertyMapId ?? 0,
                           propertySide: "OLD",
                           propertyIdNew: propertyId,
                           propertyIdOld: propertyOldId,
                           propertyNo: oldPropertyNo,
                           remark: "Property Merged - Single Old Property",
                           latitude: latitude,
                           longitude: longitude,
                           location: dto.Location,
                           userId: dto.UserId);
            mapDetails.Add(oldData);

            if (existsNewMergeProperty != newPropertyNo)
            {
                var newData = CreatePropertyMergeDetail(
                          propertyMapId: dto.PropertyMapId ?? 0,
                          propertySide: "NEW",
                          propertyIdNew: propertyId,
                          propertyIdOld: null,
                          propertyNo: newPropertyNo,
                          remark: "Property Merged - Single New Property",
                          latitude: latitude,
                          longitude: longitude,
                          location: dto.Location,
                          userId: dto.UserId);
                mapDetails.Add(newData);
            }

            // ── 6. Execute bulk update for property (EF Core 7+) ────────────────────
            var propertyUpdateCount = await _repository
            .GetQueryable()
            .Where(pm => pm.Id == propertyId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(pm => pm.PropertyMastOldId, propertyOldId),
                cancellationToken);

            if (propertyUpdateCount == 0)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                response.Success = false;
                response.Message = "Failed to update property";
                return response;
            }

            // ── 7. Bulk insert PropertyMapDetail records ────────────────────────────
            await _propertyMapDetailRepository.AddRangeAsync(mapDetails, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // ── 8. Commit transaction ────────────────────────────────────────────────
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            response.Success = true;
            response.Message = $"Old property no {oldPropertyNo} merge successfull in new property no {newPropertyNo}";
            return response;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            response.Success = false;
            response.Message = $"Error during merge: {ex.Message}";
            return response;
        }
    }

    /// <summary>
    /// Merges one old property (PropertyMastOld) into multiple new properties (PropertyMast) for SPLIT scenario:
    /// <list type="number">
    ///   <item>Validating that exactly one old property ID and multiple new property IDs are provided.</item>
    ///   <item>Ensuring the old property exists and is active.</item>
    ///   <item>Ensuring all new properties exist and are active.</item>
    ///   <item>Confirming none of the new properties are already merged.</item>
    ///   <item>Confirming the old property is not already merged elsewhere.</item>
    ///   <item>Creating PropertyMapDetail records for OLD side (once) and NEW side (for each new property).</item>
    /// </list>
    /// </summary>
    private async Task<PropertyResponse> MergeSplitProperty(PropertyMergeDto dto, CancellationToken cancellationToken = default)
    {
        var response = new PropertyResponse();
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // ── 1. Validate input ────────────────────────────────────────────────────
            if (dto.PropertyOldIds == null || dto.PropertyOldIds.Count != 1)
            {
                response.Success = false;
                response.Message = "Split operation requires exactly one old propertyNo";
                return response;
            }

            if (dto.PropertyIds == null || dto.PropertyIds.Count < 1)
            {
                response.Success = false;
                response.Message = "At least one new propertyNo is required for the split operation";
                return response;
            }

            int propertyOldId = dto.PropertyOldIds[0];
            var propertyIds = dto.PropertyIds;

            // ── 2. Parallel validation queries (3 queries at once) ──────────────────
            var propertyMastOld = await _propertyOldRepository.GetQueryable().AsNoTracking()
                .Where(pm => pm.Id == propertyOldId && pm.IsActive && !pm.MarkedForDeletion)
                .Select(p => new { p.OldWardNo, p.OldPropertyNo, p.OldPartitionNo, p.Id })
                .FirstOrDefaultAsync(cancellationToken);

            var propertyMasts = await (
                    from pm in _repository.GetQueryable()
                    join wd in _wardRepository.GetQueryable() on pm.WardId equals wd.Id
                    where propertyIds.Contains(pm.Id) && pm.IsActive && !pm.MarkedForDeletion
                    && wd.IsActive
                    select new { pm.Id,wd.WardNo, pm.PropertyNo, pm.PartitionNo }
                ).AsNoTracking()
                .ToListAsync(cancellationToken);

            var existingMerges = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                .Where(pmd =>
                    pmd.PropertyIdNew.HasValue &&
                    propertyIds.Contains(pmd.PropertyIdNew.Value) &&
                    pmd.IsActive &&
                    pmd.Status == "MODIFIED")
                .Select(pmd => new { pmd.PropertyIdNew })
                .ToListAsync(cancellationToken);

           
            // ── 3. Validate results ──────────────────────────────────────────────────
            if (propertyMastOld == null)
            {
                response.Success = false;
                response.Message = "Old Property not found";
                return response;
            }

            if (propertyMasts.Count != propertyIds.Count())
            {
                response.Success = false;
                response.Message = "Selected new properties were not found. Please select the properties one by one and try";
                return response;
            }

            // Check if any new properties are already merged
            if (existingMerges.Any())
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                // Get detailed info only if needed for error message
                var existingMapDetails = await (
                    from pmd in _propertyMapDetailRepository.GetQueryable()
                    join prop in _repository.GetQueryable() on pmd.PropertyIdNew equals prop.Id
                    join ward in _wardRepository.GetQueryable() on prop.WardId equals ward.Id
                    join pmo in _propertyOldRepository.GetQueryable() on pmd.PropertyIdOld equals pmo.Id
                    where existingMerges.Select(e => e.PropertyIdNew).Contains(pmd.PropertyIdNew.Value)
                    select new {ward.WardNo,prop.PropertyNo,prop.PartitionNo,pmo.OldWardNo,pmo.OldPropertyNo,pmo.OldPartitionNo})
                    .AsNoTracking()
                    .Distinct()
                    .Take(5)
                    .ToListAsync(cancellationToken);

                var newPropertyNo = BuildPropertyNumbers(existingMapDetails.Select(p => (p.WardNo, p.PropertyNo, p.PartitionNo)));
                var oldPropertyNos = BuildPropertyNumbers(existingMapDetails.Select(p => (p.OldWardNo,p.OldPropertyNo,p.OldPartitionNo)));

                response.Success = false;
                response.Message = $"New properties {newPropertyNo} already merged for old properties: {oldPropertyNos}";
                return response;
            }

            // ── 4. Parse coordinates ─────────────────────────────────────────────────
            decimal? latitude = !string.IsNullOrWhiteSpace(dto.Latitude) && decimal.TryParse(dto.Latitude, out var lat) ? lat : null;
            decimal? longitude = !string.IsNullOrWhiteSpace(dto.Longitude) && decimal.TryParse(dto.Longitude, out var lon) ? lon : null;
            var now = DateTime.Now;

            // ── 5. Build PropertyMapDetail entities (pre-sized collection) ───────────
            var mapDetails = new List<PropertyMapDetailEntity>(propertyIds.Count + 1);

            var existsOldMergeProperty = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                            .Where(pm => pm.PropertyIdOld == propertyOldId && pm.IsActive && pm.Status == "MODIFIED" && pm.PropertySide == "OLD")
                            .Select(pm => pm.PropertyNo)
                            .FirstOrDefaultAsync(cancellationToken);

            // OLD side entity
            var oldPropertyNo = BuildPropertyNumber(propertyMastOld.OldWardNo, propertyMastOld.OldPropertyNo, propertyMastOld.OldPartitionNo);

            if (existsOldMergeProperty != oldPropertyNo)
            {
                var oldData = CreatePropertyMergeDetail(
                            propertyMapId: dto.PropertyMapId ?? 0,
                            propertySide: "OLD",
                            propertyIdNew: null,
                            propertyIdOld: propertyOldId,
                            propertyNo: oldPropertyNo,
                            remark: "Property Split - Old Property",
                            latitude: latitude,
                            longitude: longitude,
                            location: dto.Location,
                            userId: dto.UserId);
                mapDetails.Add(oldData);
            }
            
            // NEW side entities
            var newPropertyNos = new List<string>();
            foreach (var propertyMast in propertyMasts)
            {
                var newPropertyNo = BuildPropertyNumber(propertyMast.WardNo, propertyMast.PropertyNo, propertyMast.PartitionNo);

                var newData = CreatePropertyMergeDetail(
                            propertyMapId: dto.PropertyMapId ?? 0,
                            propertySide: "NEW",
                            propertyIdNew: propertyMast.Id,
                            propertyIdOld: propertyOldId,
                            propertyNo: newPropertyNo,
                            remark: "Property Split - New Property",
                            latitude: latitude,
                            longitude: longitude,
                            location: dto.Location,
                            userId: dto.UserId);
                mapDetails.Add(newData);
                newPropertyNos.Add(newPropertyNo);
            }


            // ── 6. Bulk insert PropertyMapDetail records ─────────────────────────────
            await _propertyMapDetailRepository.AddRangeAsync(mapDetails, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // ── 7. Commit transaction ────────────────────────────────────────────────
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            response.Success = true;
            response.Message = $"Property Split Successful old property no : {oldPropertyNo} split into  {string.Join(", ", newPropertyNos)} new properties ";
            return response;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            response.Success = false;
            response.Message = $"Error during split: {ex.Message}";
            return response;
        }
    }

    /// <summary>
    /// Merges multiple old properties (PropertyMastOld) into one new property (PropertyMast) for MERGE scenario:
    /// <list type="number">
    ///   <item>Validating that multiple old property IDs and exactly one new property ID are provided.</item>
    ///   <item>Ensuring all old properties exist and are active.</item>
    ///   <item>Ensuring the new property exists and is active.</item>
    ///   <item>Confirming the new property is not already merged.</item>
    ///   <item>Confirming none of the old properties are already merged elsewhere.</item>
    ///   <item>Creating PropertyMapDetail records for OLD side (for each old property) and NEW side (once).</item>
    /// </list>
    /// </summary>
    private async Task<PropertyResponse> MergeMultipleProperty(PropertyMergeDto dto, CancellationToken cancellationToken = default)
    {
        var response = new PropertyResponse();
        try
        {
            // ── 1. Validate input: must have multiple old properties and exactly 1 new property ─────
            if (dto.PropertyOldIds == null || dto.PropertyOldIds.Count < 1)
            {
                response.Success = false;
                response.Message = "At least one old propertyNo is required for the merge operation.";
                return response;
            }

            if (dto.PropertyIds == null || dto.PropertyIds.Count != 1)
            {
                response.Success = false;
                response.Message = "Merge operation requires exactly only one new propertyNo.";
                return response;
            }

            var propertyOldIds = dto.PropertyOldIds;
            int propertyId = dto.PropertyIds.First();

            var propertyMast = await (
                    from pm in _repository.GetQueryable()
                    join wd in _wardRepository.GetQueryable() on pm.WardId equals wd.Id
                    where pm.Id == propertyId && pm.IsActive && !pm.MarkedForDeletion
                    select new{ pm.Id,wd.WardNo,pm.PropertyNo,pm.PartitionNo})
                    .AsNoTracking()
                    .FirstOrDefaultAsync(cancellationToken);

            if (propertyMast == null)
            {
                response.Success = false;
                response.Message = "New PropertyNo not found";
                return response;
            }

            // ── 2. Validate all old properties exist and are active ─────────────────
            var propertyMastOlds = await _propertyOldRepository.GetQueryable().AsNoTracking()
                .Where(pm => propertyOldIds.Contains(pm.Id) && pm.IsActive && !pm.MarkedForDeletion)
                .Select(p => new { p.Id, p.OldWardNo, p.OldPropertyNo,p.OldPartitionNo })
                .ToListAsync(cancellationToken);

            if (propertyMastOlds.Count != propertyOldIds.Count)
            {
                response.Success = false;
                response.Message = "Selected old properties were not found. Please select the properties one by one and try again.";
                return response;
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            // ── 3. Validate all old properties were not already merged ──────
            var existingMergeDetails = await (
                from pmd in _propertyMapDetailRepository.GetQueryable()
                join prop in _repository.GetQueryable() on pmd.PropertyIdNew equals prop.Id
                join ward in _wardRepository.GetQueryable() on prop.WardId equals ward.Id
                join pmo in _propertyOldRepository.GetQueryable() on pmd.PropertyIdOld equals pmo.Id
                where pmd.PropertyIdOld.HasValue
                    && propertyOldIds.Contains(pmd.PropertyIdOld.Value)
                    && pmd.IsActive && ward.IsActive
                    && pmd.Status == "MODIFIED"
                select new{ward.WardNo,prop.PropertyNo,prop.PartitionNo,pmo.OldWardNo,pmo.OldPropertyNo,pmo.OldPartitionNo})
                .AsNoTracking()
                .Distinct()
                .Take(10)
                .ToListAsync(cancellationToken);


            if (existingMergeDetails.Any())
            {
                var newPropertyNos = BuildPropertyNumbers(existingMergeDetails.Select(p => (p.WardNo, p.PropertyNo, p.PartitionNo)));
                var oldPropertyNo = BuildPropertyNumbers(existingMergeDetails.Select(p => (p.OldWardNo, p.OldPropertyNo, p.OldPartitionNo)));

                response.Success = false;
                response.Message = $"Old properties {oldPropertyNo} already merged for new properties: {newPropertyNos}";
                return response;
            }

            var existsNewMergeProperty = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
              .Where(pm => pm.PropertyIdNew == propertyId && pm.IsActive && pm.Status == "MODIFIED" && pm.PropertySide == "NEW")
              .Select(pm => pm.PropertyNo)
              .FirstOrDefaultAsync(cancellationToken);

            // ── 4. Parse optional coordinates ───────────────────────────────────────
            decimal? latitude = null;
            decimal? longitude = null;

            if (!string.IsNullOrWhiteSpace(dto.Latitude) &&
                decimal.TryParse(dto.Latitude, out var latValue))
            {
                latitude = latValue;
            }

            if (!string.IsNullOrWhiteSpace(dto.Longitude) &&
                decimal.TryParse(dto.Longitude, out var longValue))
            {
                longitude = longValue;
            }

            // ── 5. Build PropertyMapDetail entities ────────────────────────────────
            var mapDetails = new List<PropertyMapDetailEntity>();

            // Create OLD side entities (one for each old property)
            var oldPropertyNos = new List<string>();
            foreach (var propertyMastOld in propertyMastOlds)
            {
                var oldPropertyNo = BuildPropertyNumber(propertyMastOld.OldWardNo, propertyMastOld.OldPropertyNo, propertyMastOld.OldPartitionNo);

                var oldData = CreatePropertyMergeDetail(
                             propertyMapId: dto.PropertyMapId ?? 0,
                             propertySide: "OLD",
                             propertyIdNew: propertyId,
                             propertyIdOld: propertyMastOld.Id,
                             propertyNo: oldPropertyNo,
                             remark: "Property Merge - Old Property",
                             latitude: latitude,
                             longitude: longitude,
                             location: dto.Location,
                             userId: dto.UserId);
                mapDetails.Add(oldData);
                oldPropertyNos.Add(oldPropertyNo);
            }

            var newPropertyNo = BuildPropertyNumber(propertyMast.WardNo, propertyMast.PropertyNo, propertyMast.PartitionNo);
            // Create NEW side entity (one for the new property)
            if (existsNewMergeProperty != newPropertyNo)
            {
                var newData = CreatePropertyMergeDetail(
                    propertyMapId: dto.PropertyMapId ?? 0,
                    propertySide: "NEW",
                    propertyIdNew: propertyId,
                    propertyIdOld: null,
                    propertyNo: newPropertyNo,
                    remark: "Property Merge - New Property",
                    latitude: latitude,
                    longitude: longitude,
                    location: dto.Location,
                    userId: dto.UserId);
                mapDetails.Add(newData);
            }

            // ── 6. Persist all changes in one transaction ─────────────────────────
            await _propertyMapDetailRepository.AddRangeAsync(mapDetails, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            response.Success = true;
            response.Message = $"Old properties {string.Join(", ", oldPropertyNos)} merge successfull in new property no : {newPropertyNo}";
            return response;
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            response.Success = false;
            response.Message = $"Error during merge: {ex.Message}";
            return response;
        }

    }


    /// <summary>
    /// Gets detailed merge information for specified properties including old property details
    /// </summary>
    public async Task<PropertyMergeDetailResponse> GetPropertyMergeDetailsAsync(int propertyId,CancellationToken cancellationToken = default)
    {
        var response = new PropertyMergeDetailResponse();
        try
        {
            var mergeDetails = await (
                from pm in _repository.GetQueryable()
                join pmd in _propertyMapDetailRepository.GetQueryable() on pm.Id equals pmd.PropertyIdNew
                join pmo in _propertyOldRepository.GetQueryable() on pmd.PropertyIdOld equals pmo.Id
                where pm.Id == propertyId
                    && pm.IsActive && !pm.MarkedForDeletion
                    && pmd.IsActive && pmd.Status == "MODIFIED"
                    && pmo.IsActive && !pmo.MarkedForDeletion
                select new PropertyMergeDetailDto
                {
                    Id = pm.Id,
                    WardId = pm.WardId,
                    PropertyNo = pm.PropertyNo,
                    PartitionNo = pm.PartitionNo,
                    PropertyOldId = pmo.Id,
                    OldWardNo = pmo.OldWardNo,
                    OldPropertyNo = pmo.OldPropertyNo,
                    OldPartitionNo = pmo.OldPartitionNo,
                    OldOwnerName = pmo.OldOwnerName,
                    OldMobileNo = pmo.OldMobileNo,
                    OldOccupierName = pmo.OldOccupierName,
                    OldAddress = pmo.OldAddress,
                    OldSocietyName = pmo.OldSocietyName,
                    OldRV = pmo.OldRV,
                    OldTotalTax = pmo.OldTotalTax,
                    OldPlotArea = pmo.OldPlotArea,
                    OldGeneralTax = pmo.OldGeneralTax,
                    OldConstructionYear = Convert.ToInt32(pmo.OldConstructionYear),
                    OldConstructionArea = pmo.OldConstructionArea
                })
                .Distinct()
                .ToListAsync(cancellationToken);

            if (!mergeDetails.Any())
            {
                response.Success = false;
                response.Message = "No merge details found for the specified properties";
                return response;
            }

            response.Success = true;
            response.Message = $"Found {mergeDetails.Count} merge detail(s)";
            response.Data = mergeDetails;
            return response;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Error retrieving merge details: {ex.Message}";
            return response;
        }
    }


    /// <summary>
    /// Demerges a property by:
    /// <list type="number">
    ///   <item>Validating that the property exists and is merged with the specified old properties.</item>
    ///   <item>Marking PropertyMapDetail records as CANCELLED and inactive.</item>
    ///   <item>Cleaning up any existing CANCELLED records.</item>
    /// </list>
    /// </summary>
    public async Task<PropertyResponse> DemergePropertyAsync(PropertyDemergeDto dto, CancellationToken cancellationToken = default)
    {
        var response = new PropertyResponse();
        try
        {
            var newPropertyCount = dto.PropertyIds?.Count ?? 0;
            var oldPropertyCount = dto.PropertyOldIds?.Count ?? 0;
            // ── 1. Validate input ───────────────────────────────────────────────────
            if (newPropertyCount <= 0)
            {
                response.Success = false;
                response.Message = "Invalid propertyNo";
                return response;
            }

            if (oldPropertyCount <= 0)
            {
                response.Success = false;
                response.Message = "Invalid old propertyNo";
                return response;
            }

            if (newPropertyCount == 1 && !string.Equals(dto.PropertySide, "Old", StringComparison.OrdinalIgnoreCase) && oldPropertyCount > 0)
            {
                var newPropertyId = dto.PropertyIds!.Where(id => id > 0).Distinct().Single();
                var oldPropertyIds = dto.PropertyOldIds!.Where(id => id > 0).Distinct().ToList();

                if (oldPropertyIds.Count == 0)
                {
                    response.Success = false;
                    response.Message = "Invalid old property number";
                    return response;
                }

                // Load requested OLD rows and the parent NEW row.
                var validationQuery = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                    .Where(pmd =>
                        pmd.PropertyIdNew == newPropertyId &&
                        (
                            (
                                pmd.PropertyIdOld.HasValue &&
                                oldPropertyIds.Contains(pmd.PropertyIdOld.Value)
                            )
                            ||
                            pmd.PropertySide == "NEW"
                        ) &&
                        pmd.IsActive && pmd.Status == "MODIFIED")
                    .Select(pmd => new
                    {
                        pmd.Id,
                        pmd.PropertyMapId,
                        pmd.PropertyNo,
                        pmd.PropertySide,
                        pmd.PropertyIdOld,
                        pmd.PropertyIdNew
                    })
                    .ToListAsync(cancellationToken);

                if (validationQuery.Count == 0)
                {
                    var propertyExists = await _repository.GetQueryable().AsNoTracking()
                        .AnyAsync(property => property.Id == newPropertyId && property.IsActive && !property.MarkedForDeletion, cancellationToken);

                    response.Success = false;
                    response.Message = propertyExists ? "No merge details found to demerge" : "Property not found";
                    return response;
                }

                // Validate that every requested old property has an active mapping.
                var foundOldPropertyIds = validationQuery.Where(x => x.PropertyIdOld.HasValue).Select(x => x.PropertyIdOld!.Value).ToHashSet();
                var missingOldPropertyIds = oldPropertyIds.Where(id => !foundOldPropertyIds.Contains(id)).ToList();

                if (missingOldPropertyIds.Count > 0)
                {
                    response.Success = false;
                    response.Message =$"No merge details found to demerge";
                    return response;
                }

                // Count all active MODIFIED rows for this PropertyIdNew.
                // One NEW parent row + multiple OLD rows.

                var newPropertyMappingCount = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                    .CountAsync( pmd => pmd.PropertyIdNew == newPropertyId && pmd.IsActive && pmd.Status == "MODIFIED",cancellationToken);

                /*
                 * Apply the same rule used in the old-property block:
                 * PropertyIdNew count <= 2
                 *     Update NEW and OLD rows. 
                 * PropertyIdNew count > 2
                 *     Update all rows when requested old-ID count equals:
                 *     - Total count
                 *     - Total count - 1 
                 * Otherwise:
                 *     Update only requested OLD rows.
                 */
                var shouldUpdateAllRows =
                    newPropertyMappingCount <= 2 ||
                    oldPropertyIds.Count == newPropertyMappingCount ||
                    oldPropertyIds.Count == newPropertyMappingCount - 1;

                List<int> mappingIdsToUpdate;
                if (shouldUpdateAllRows)
                {
                    mappingIdsToUpdate = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                        .Where(pmd => pmd.PropertyIdNew == newPropertyId && pmd.IsActive && pmd.Status == "MODIFIED")
                        .Select(pmd => pmd.Id)
                        .ToListAsync(cancellationToken);
                }
                else
                {
                    mappingIdsToUpdate = validationQuery
                        .Where(mapping => mapping.PropertyIdOld.HasValue && oldPropertyIds.Contains(mapping.PropertyIdOld.Value))
                        .Select(mapping => mapping.Id)
                        .Distinct().ToList();
                }

                if (mappingIdsToUpdate.Count == 0)
                {
                    response.Success = false;
                    response.Message = "No mapping records found to demerge";
                    return response;
                }

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                //  Load only the rows that will be updated.
                 
                var mappingsToUpdate = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                    .Where(pmd => mappingIdsToUpdate.Contains(pmd.Id))
                    .Select(pmd => new
                    {
                        pmd.Id,
                        pmd.PropertyMapId,
                        pmd.PropertySide,
                        pmd.PropertyIdOld,
                        pmd.PropertyIdNew
                    })
                    .ToListAsync(cancellationToken);

                var selectedPropertyMapIds = mappingsToUpdate.Select(x => x.PropertyMapId).Distinct().ToList();

                 // Find previous CANCELLED rows that could conflict with
                 // the unique key when current rows become CANCELLED.
                 
                var cancelledCandidates = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                    .Where(pmd =>
                        pmd.Status == "CANCELLED" && pmd.PropertyIdNew == newPropertyId &&
                        (
                            shouldUpdateAllRows
                            ||
                            (
                                pmd.PropertyIdOld.HasValue && oldPropertyIds.Contains(pmd.PropertyIdOld.Value)
                            )
                        ))
                    .Select(pmd => new
                    {
                        pmd.Id,
                        pmd.PropertyMapId,
                        pmd.PropertySide,
                        pmd.PropertyIdOld,
                        pmd.PropertyIdNew
                    })
                    .ToListAsync(cancellationToken);

                // Delete only exact previous CANCELLED rows.
                var cancelledIdsToDelete = cancelledCandidates
                    .Where(cancelled =>
                        mappingsToUpdate.Any(current =>
                            current.PropertySide == cancelled.PropertySide &&
                            current.PropertyIdOld == cancelled.PropertyIdOld &&
                            current.PropertyIdNew == cancelled.PropertyIdNew))
                    .Select(x => x.Id)
                    .Distinct().ToList();

                if (cancelledIdsToDelete.Count > 0)
                {
                    await _propertyMapDetailRepository.GetQueryable()
                        .Where(pmd => cancelledIdsToDelete.Contains(pmd.Id) && pmd.Status == "CANCELLED")
                        .ExecuteDeleteAsync(cancellationToken);
                }

                var now = DateTime.Now;

                // Update selected rows in one SQL query.
                var updatedCount = await _propertyMapDetailRepository.GetQueryable()
                    .Where(pmd =>
                        mappingIdsToUpdate.Contains(pmd.Id) &&
                        pmd.IsActive &&
                        pmd.Status == "MODIFIED")
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(pmd => pmd.Status, "CANCELLED")
                            .SetProperty(pmd => pmd.IsActive, false)
                            .SetProperty(pmd => pmd.IsCurrent, false)
                            .SetProperty(pmd => pmd.UpdatedBy, dto.UserId)
                            .SetProperty(pmd => pmd.UpdatedDate, now),
                        cancellationToken);

                var newPropertyNo = validationQuery
                    .Where(x => x.PropertySide == "NEW" && !string.IsNullOrWhiteSpace(x.PropertyNo))
                    .Select(x => x.PropertyNo).FirstOrDefault();

                var oldPropertyNos = validationQuery
                    .Where(x => x.PropertyIdOld.HasValue && oldPropertyIds.Contains(x.PropertyIdOld.Value) && !string.IsNullOrWhiteSpace(x.PropertyNo))
                    .Select(x => x.PropertyNo!)
                    .Distinct().ToList();

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                response.Success = true;
                response.Message = $"Old properties {string.Join(", ", oldPropertyNos)} " + $"demerged successfully from new property no: {newPropertyNo}";
                return response;
            }
            else
            {
                var oldPropertyId = dto.PropertyOldIds!.First();
                var propertyIds = dto.PropertyIds!.Where(id => id > 0).Distinct().ToList();

                var validationQuery = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                    .Where(pmd =>
                        pmd.PropertyIdOld == oldPropertyId &&
                        (
                            (
                                pmd.PropertyIdNew.HasValue && propertyIds.Contains(pmd.PropertyIdNew.Value)
                            )
                            || pmd.PropertySide == "OLD"
                        ) &&
                        pmd.IsActive && pmd.Status == "MODIFIED")
                    .Select(pmd => new
                    {
                        pmd.Id,
                        pmd.PropertyNo,
                        pmd.PropertySide,
                        pmd.PropertyIdOld,
                        pmd.PropertyIdNew
                    })
                    .ToListAsync(cancellationToken);

                if (!validationQuery.Any())
                {
                    var propertyExists = await _propertyOldRepository.GetQueryable().AsNoTracking()
                        .AnyAsync( pm => pm.Id == oldPropertyId && pm.IsActive && !pm.MarkedForDeletion, cancellationToken);

                    response.Success = false;
                    response.Message = propertyExists ? "No split details found to demerge": "Property not found";
                    return response;
                }

                //  Count all active MODIFIED rows for this old property.
                var oldPropertyMappingCount = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                    .CountAsync( pmd => pmd.PropertyIdOld == oldPropertyId && pmd.IsActive && pmd.Status == "MODIFIED", cancellationToken);

                // Rules Condition
                var shouldUpdateAllRows =
                    oldPropertyMappingCount <= 2 ||
                    propertyIds.Count == oldPropertyMappingCount ||
                    propertyIds.Count == oldPropertyMappingCount - 1;

                List<int> mappingIdsToUpdate;

                if (shouldUpdateAllRows)
                {
                    mappingIdsToUpdate = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                        .Where(pmd => pmd.PropertyIdOld == oldPropertyId && pmd.IsActive && pmd.Status == "MODIFIED")
                        .Select(pmd => pmd.Id)
                        .ToListAsync(cancellationToken);
                }
                else
                {
                    mappingIdsToUpdate = validationQuery
                        .Where(mapping => mapping.PropertyIdNew.HasValue && propertyIds.Contains(mapping.PropertyIdNew.Value))
                        .Select(mapping => mapping.Id)
                        .Distinct().ToList();
                }

                if (mappingIdsToUpdate.Count == 0)
                {
                    response.Success = false;
                    response.Message = "No mapping records found to demerge";
                    return response;
                }

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                var mappingsToUpdate = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                    .Where(pmd => mappingIdsToUpdate.Contains(pmd.Id))
                    .Select(pmd => new
                    {
                        pmd.Id,
                        pmd.PropertySide,
                        pmd.PropertyIdOld,
                        pmd.PropertyIdNew
                    })
                    .ToListAsync(cancellationToken);

                var cancelledCandidates = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                    .Where(pmd =>
                        pmd.Status == "CANCELLED" && pmd.PropertyIdOld == oldPropertyId &&
                        (
                            shouldUpdateAllRows
                            ||
                            (
                                pmd.PropertyIdNew.HasValue && propertyIds.Contains(pmd.PropertyIdNew.Value)
                            )
                        ))
                    .Select(pmd => new
                    {
                        pmd.Id,
                        pmd.PropertySide,
                        pmd.PropertyIdOld,
                        pmd.PropertyIdNew
                    })
                    .ToListAsync(cancellationToken);

                var cancelledIdsToDelete = cancelledCandidates
                    .Where(cancelled =>
                        mappingsToUpdate.Any(current => current.PropertySide == cancelled.PropertySide && current.PropertyIdOld == cancelled.PropertyIdOld && current.PropertyIdNew == cancelled.PropertyIdNew))
                    .Select(x => x.Id)
                    .Distinct().ToList();

                if (cancelledIdsToDelete.Count > 0)
                {
                    await _propertyMapDetailRepository.GetQueryable()
                        .Where(pmd => cancelledIdsToDelete.Contains(pmd.Id) && pmd.Status == "CANCELLED")
                        .ExecuteDeleteAsync(cancellationToken);
                }

                var now = DateTime.Now;

                var updatedCount = await _propertyMapDetailRepository.GetQueryable()
                    .Where(pmd =>
                        mappingIdsToUpdate.Contains(pmd.Id) &&
                        pmd.IsActive && pmd.Status == "MODIFIED")
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(pmd => pmd.Status, "CANCELLED")
                            .SetProperty(pmd => pmd.IsActive, false)
                            .SetProperty(pmd => pmd.IsCurrent, false)
                            .SetProperty(pmd => pmd.UpdatedBy, dto.UserId)
                            .SetProperty(pmd => pmd.UpdatedDate, now),
                        cancellationToken);

                var newPropertyNos = validationQuery
                    .Where(x =>
                        x.PropertyIdNew.HasValue && propertyIds.Contains(x.PropertyIdNew.Value) && !string.IsNullOrWhiteSpace(x.PropertyNo))
                    .Select(x => x.PropertyNo!)
                    .Distinct().ToList();

                var oldPropertyNo = validationQuery
                    .Where(x => x.PropertySide == "OLD" && !string.IsNullOrWhiteSpace(x.PropertyNo))
                    .Select(x => x.PropertyNo)
                    .FirstOrDefault();

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                response.Success = true;
                response.Message = $"New properties {string.Join(", ", newPropertyNos)} " + $"demerged successfully from old property no: {oldPropertyNo}";
                return response;
            }

        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            response.Success = false;
            response.Message = $"Error during demerge: {ex.Message}";
            return response;
        }
    }

    public async Task<PropertyResponse> MergeMultiplePropertyAsync(PropertyMergeMultipleDto dto, CancellationToken cancellationToken = default)
    {
        var response = new PropertyResponse();
        if (dto == null)
        {
            response.Success = false;
            response.Message = "Invalid Request";
            return response;
        }
        if (dto.PropertyIdList == null || dto.PropertyIdList.Count == 0)
        {
            response.Success = false;
            response.Message = "Invalid Request";
            return response;
        }
        if (dto.UserId <= 0)
        {
            response.Success = false;
            response.Message = "Invalid User";
            return response;
        }

        // Remove accidental duplicate exact pairs.
        var propertyPairs = dto.PropertyIdList.Select(x => new { x.PropertyOldId, x.PropertyId }).Distinct().ToList();

        // Validate positive IDs.
        var invalidPair = propertyPairs.FirstOrDefault(x =>x.PropertyOldId <= 0 || x.PropertyId <= 0);

        if (invalidPair != null)
        {
            response.Success = false;
            response.Message = "Invalid data found!! ";
            return response;
        }

        // One old property must be mapped to only one new property.
        var duplicateOldPropertyIds = propertyPairs.GroupBy(x => x.PropertyOldId).Where(group => group.Count() > 1).Select(group => group.Key).ToList();

        if (duplicateOldPropertyIds.Count > 0)
        {
            var duplicateOldProperties = await _propertyOldRepository.GetQueryable().AsNoTracking()
                       .Where(x => duplicateOldPropertyIds.Contains(x.Id) && x.IsActive && !x.MarkedForDeletion)
                       .Select(x => new { x.OldWardNo,x.OldPropertyNo,x.OldPartitionNo })
                       .ToListAsync(cancellationToken);

            var oldPropertyNos = BuildPropertyNumbers(
                duplicateOldProperties.Select(p => (p.OldWardNo ?? string.Empty,p.OldPropertyNo ?? string.Empty,p.OldPartitionNo ?? string.Empty)));

            response.Success = false;
            response.Message =$"The old property numbers are repeated: {string.Join(", ", oldPropertyNos)}";
            return response;
        }

        // One new property must receive only one old property in one-to-one merge.
        var duplicateNewPropertyIds = propertyPairs.GroupBy(x => x.PropertyId).Where(group => group.Count() > 1).Select(group => group.Key).ToList();

        if (duplicateNewPropertyIds.Count > 0)
        {
            var duplicateNewProperties = await (
                    from property in _repository.GetQueryable().AsNoTracking()
                    join ward in _wardRepository.GetQueryable().AsNoTracking()
                        on property.WardId equals ward.Id
                    where duplicateNewPropertyIds.Contains(property.Id)
                        && property.IsActive && !property.MarkedForDeletion && ward.IsActive
                    select new { ward.WardNo,property.PropertyNo,property.PartitionNo })
                    .ToListAsync(cancellationToken);

            var newPropertyNos = BuildPropertyNumbers(
                duplicateNewProperties.Select(p => (p.WardNo ?? string.Empty, p.PropertyNo ?? string.Empty, p.PartitionNo ?? string.Empty)));

            response.Success = false;
            response.Message =$"The new property numbers are repeated: {string.Join(", ", newPropertyNos)}";
            return response;
        }

        //  Parse and validate coordinates
        decimal? latitude = null;
        decimal? longitude = null;
        if (!string.IsNullOrWhiteSpace(dto.Latitude) && decimal.TryParse(dto.Latitude, out var latValue))
        {
            latitude = latValue;
        }

        if (!string.IsNullOrWhiteSpace(dto.Longitude) && decimal.TryParse(dto.Longitude, out var longValue))
        {
            longitude = longValue;
        }

        var oldPropertyIds = propertyPairs.Select(x => x.PropertyOldId).ToList();
        var newPropertyIds = propertyPairs.Select(x => x.PropertyId).ToList();

        dto.PropertyMapId = await _propertyMapMasterRepository.GetQueryable().AsNoTracking()
             .Where(x => x.IsActive && x.MappingCategory == "ONE_TO_ONE")
             .Select(x => (int?)x.Id)
             .FirstOrDefaultAsync(cancellationToken);

        var transactionStarted = false;
        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            transactionStarted = true;
            //  Fetch all old properties in one query
            var oldProperties = await _propertyOldRepository.GetQueryable().AsNoTracking()
                .Where(property => oldPropertyIds.Contains(property.Id) && property.IsActive && !property.MarkedForDeletion)
                .Select(property => new
                {
                    property.Id,
                    property.OldWardNo,
                    property.OldPropertyNo,
                    property.OldPartitionNo,
                    property.OldOwnerName
                })
                .ToListAsync(cancellationToken);

            if (oldProperties.Count != oldPropertyIds.Count)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                transactionStarted = false;

                response.Success = false;
                response.Message = $"Old properties not found ";
                return response;
            }

            //  Fetch all new properties and wards in one query
            // Do not use AsNoTracking here because these entities will be
            // updated and saved through EF change tracking.
            var newProperties = await (
                from property in _repository.GetQueryable()
                join ward in _wardRepository.GetQueryable().AsNoTracking()
                    on property.WardId equals ward.Id
                where newPropertyIds.Contains(property.Id)
                      && property.IsActive && !property.MarkedForDeletion
                select new
                {
                    Entity = property,
                    ward.WardNo
                })
                .ToListAsync(cancellationToken);

            if (newProperties.Count != newPropertyIds.Count)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                transactionStarted = false;

                response.Success = false;
                response.Message = $"New properties not found ";
                return response;
            }

            var oldPropertyDictionary = oldProperties.ToDictionary(x => x.Id);
            var newPropertyDictionary = newProperties.ToDictionary(x => x.Entity.Id);

            //  Get relevant existing mappings in one query
            var existingMappingsNew = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                .Where(mapping =>
                    mapping.IsActive &&
                    mapping.Status == "MODIFIED" &&
                    (
                        (
                            mapping.PropertySide == "OLD" &&
                            (mapping.PropertyIdOld.HasValue && oldPropertyIds.Contains(mapping.PropertyIdOld.Value)
                            )) ||
                        (
                            mapping.PropertySide == "NEW" &&
                            mapping.PropertyIdNew.HasValue && newPropertyIds.Contains(mapping.PropertyIdNew.Value)
                        )
                    ))
                .Select(mapping => new
                {
                    mapping.PropertyIdOld,
                    mapping.PropertyIdNew,
                    mapping.PropertySide,
                    mapping.PropertyNo
                }).ToListAsync(cancellationToken);

            var mappingsQuery = _propertyMapDetailRepository.GetQueryable().AsNoTracking();

            // Get PropertyIdNew connected to the passed PropertyIdOld.
            var matchingNewPropertyIds = mappingsQuery
                .Where(mapping =>
                    mapping.IsActive &&
                    mapping.Status == "MODIFIED" &&
                    mapping.PropertySide == "OLD" &&
                    mapping.PropertyIdOld.HasValue &&
                    oldPropertyIds.Contains(mapping.PropertyIdOld.Value) &&
                    mapping.PropertyIdNew.HasValue)
                .Select(mapping => mapping.PropertyIdNew!.Value);

            // Return the corresponding NEW-side row.
            var existingMappings = await mappingsQuery
                .Where(mapping =>
                    mapping.IsActive &&
                    mapping.Status == "MODIFIED" &&
                    mapping.PropertyIdNew.HasValue &&
                    matchingNewPropertyIds.Contains(mapping.PropertyIdNew.Value))
                .Select(mapping => new
                {
                    mapping.PropertyMapId,
                    mapping.PropertyIdOld,
                    mapping.PropertyIdNew,
                    mapping.PropertySide,
                    mapping.PropertyNo
                })
                .ToListAsync(cancellationToken);

            

            //  Validate that old properties are not already merged
            var alreadyMergedOldProperties = existingMappings
                .Where(x => x.PropertySide == "OLD")
                .Select(x => x.PropertyNo!)
                .Distinct().ToList();

            var alreadyMergedNewProperties = existingMappings
                .Where(x => x.PropertySide == "NEW")
                .Select(x => x.PropertyNo!)
                .Distinct().ToList();


            if (alreadyMergedOldProperties.Count > 0)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                transactionStarted = false;

                response.Success = false;
                response.Message = $"{string.Join(", ", alreadyMergedOldProperties)} Old properties already merged into new properties : {string.Join(", ", alreadyMergedNewProperties)}";
                return response;
            }

            if (alreadyMergedNewProperties.Count > 0)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                transactionStarted = false;

                response.Success = false;
                response.Message = $"{string.Join(", ", alreadyMergedNewProperties)} New properties already merged into Old properties : {string.Join(", ", alreadyMergedOldProperties)}";
                return response;
            }

            // Existing NEW-side records are used only to avoid duplicates.
            var newSideExistingPropertyIds = existingMappingsNew.Where(x => x.PropertySide == "NEW")
                .Select(x => x.PropertyIdNew).ToHashSet();

            //  Build all map-detail records in memory
            var now = DateTime.Now;
            var propertyMapId = dto.PropertyMapId ?? 0;
            var mapDetails = new List<PropertyMapDetailEntity>(propertyPairs.Count * 2);
            var mergedPropertyMessages = new List<string>(propertyPairs.Count);

            foreach (var pair in propertyPairs)
            {
                var oldProperty = oldPropertyDictionary[pair.PropertyOldId];
                var newProperty = newPropertyDictionary[pair.PropertyId];
                var oldPropertyNo = BuildPropertyNumber(oldProperty.OldWardNo,oldProperty.OldPropertyNo,oldProperty.OldPartitionNo);
                var newPropertyNo = BuildPropertyNumber(newProperty.WardNo,newProperty.Entity.PropertyNo,newProperty.Entity.PartitionNo);

                // Copy OldOwnerName from PropertyMastOld to OwnerName in PropertyMast
                if (!string.IsNullOrWhiteSpace(oldProperty.OldOwnerName))
                {
                    newProperty.Entity.OwnerName = oldProperty.OldOwnerName.Trim();
                    newProperty.Entity.UpdatedBy = dto.UserId;
                    newProperty.Entity.UpdatedDate = now;
                }

                // OLD-side mapping is required for every pair.
                var oldData = CreatePropertyMergeDetail(
                              propertyMapId: propertyMapId,
                              propertySide: "OLD",
                              propertyIdNew: pair.PropertyId,
                              propertyIdOld: pair.PropertyOldId,
                              propertyNo: oldPropertyNo,
                              remark: "Property Merged - Multiple One To One Old Property",
                              latitude: latitude,
                              longitude: longitude,
                              location: dto.Location,
                              userId: dto.UserId);
                mapDetails.Add(oldData);

                // Add NEW-side mapping only if it does not already exist.
                if (!newSideExistingPropertyIds.Contains(pair.PropertyId))
                {
                    var newData = CreatePropertyMergeDetail(
                                propertyMapId : propertyMapId,
                                propertySide : "NEW",
                                propertyIdNew: pair.PropertyId,
                                propertyIdOld : null,
                                propertyNo : newPropertyNo,
                                remark : "Property Merged - Multiple One To One New Property",
                                latitude : latitude,
                                longitude : longitude,
                                location : dto.Location,
                                userId : dto.UserId);
                    mapDetails.Add(newData);
                    newSideExistingPropertyIds.Add(pair.PropertyId);
                }
                mergedPropertyMessages.Add($"{oldPropertyNo} -> {newPropertyNo}");
            }

            //  Add all PropertyMapDetail rows
            await _propertyMapDetailRepository.AddRangeAsync(mapDetails,cancellationToken);

            // One SaveChanges call updates all PropertyMast entities and
            // inserts all PropertyMapDetail entities.
            var affectedRecords = await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (affectedRecords <= 0)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                transactionStarted = false;
                response.Success = false;
                response.Message = "Property data not merge";
                return response;
            }

            //  Commit transaction
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            transactionStarted = false;

            response.Success = true;
            response.Message = $"properties merged successfully. " + string.Join(", ", mergedPropertyMessages);
            return response;
        }
        catch (Exception ex)
        {
            if (transactionStarted)
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            }
            response.Success = false;
            response.Message = $"Error during multiple property merge: {ex.Message}";
            return response;
        }
    }

    public async Task<PropertyResponse> DemergeMultiplePropertyAsync(PropertyDemergeMultipleDto dto,CancellationToken cancellationToken = default)
    {
        var response = new PropertyResponse();
        var transactionStarted = false;

        try
        {
            // 1. Validate request
            if (dto == null)
            {
                response.Success = false;
                response.Message = "Invalid request";
                return response;
            }

            if (dto.PropertyIdList == null ||
                dto.PropertyIdList.Count == 0)
            {
                response.Success = false;
                response.Message = "At least one property pair is required";
                return response;
            }

            if (dto.UserId <= 0)
            {
                response.Success = false;
                response.Message = "Invalid user";
                return response;
            }

            // 2. Remove exact duplicate pairs
            var propertyPairs = dto.PropertyIdList
                .Select(x => new PropertyDemergePair
                {
                    PropertyOldId = x.PropertyOldId,
                    PropertyId = x.PropertyId
                })
                .DistinctBy(x => new
                {
                    x.PropertyOldId,
                    x.PropertyId
                }).ToList();

            // 3. Validate property IDs
            var invalidPair = propertyPairs.FirstOrDefault(x => x.PropertyOldId <= 0 || x.PropertyId <= 0);

            if (invalidPair != null)
            {
                response.Success = false;
                response.Message = "Invalid property data found";
                return response;
            }

            var oldPropertyIds = propertyPairs.Select(x => x.PropertyOldId).Distinct().ToList();
            var newPropertyIds = propertyPairs.Select(x => x.PropertyId).Distinct().ToList();

           // 4. Get old-property numbers from PropertyMastOld.
           
            var oldProperties = await _propertyOldRepository.GetQueryable().AsNoTracking()
                .Where(x => oldPropertyIds.Contains(x.Id) && x.IsActive && !x.MarkedForDeletion)
                .Select(x => new
                {
                    x.Id,
                    x.OldWardNo,
                    x.OldPropertyNo,
                    x.OldPartitionNo
                })
                .ToListAsync(cancellationToken);

            var oldPropertyNumberDictionary = oldProperties
                .ToDictionary( x => x.Id,x => BuildPropertyNumber(x.OldWardNo,x.OldPropertyNo,x.OldPartitionNo));

            //  5. Get new-property numbers from PropertyMast and WardMaster.
            
            var newProperties = await (
                from property in _repository.GetQueryable().AsNoTracking()
                join ward in _wardRepository.GetQueryable().AsNoTracking()
                    on property.WardId equals ward.Id
                where newPropertyIds.Contains(property.Id)
                      && property.IsActive && !property.MarkedForDeletion
                      && ward.IsActive
                select new
                {
                    property.Id,
                    ward.WardNo,
                    property.PropertyNo,
                    property.PartitionNo
                })
                .ToListAsync(cancellationToken);

            var newPropertyNumberDictionary = newProperties
                .ToDictionary( x => x.Id, x => BuildPropertyNumber(x.WardNo,x.PropertyNo,x.PartitionNo));

            // 6. Load mapping records related to requested IDs. *
             // This query is used for count checking and selecting
             // active MODIFIED records.
             
            var allMappingRecords = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                .Where(pmd =>
                    (
                        (
                            pmd.PropertyIdOld.HasValue && oldPropertyIds.Contains(pmd.PropertyIdOld.Value)
                        )
                        ||
                        (
                            pmd.PropertyIdNew.HasValue && newPropertyIds.Contains(pmd.PropertyIdNew.Value)
                        )
                    )
                    && pmd.Status == "MODIFIED")
                .Select(pmd => new PropertyMappingSelection
                {
                    Id = pmd.Id,
                    PropertyMapId = pmd.PropertyMapId,
                    PropertySide = pmd.PropertySide,
                    PropertyIdOld = pmd.PropertyIdOld,
                    PropertyIdNew = pmd.PropertyIdNew,
                    PropertyNo = pmd.PropertyNo,
                    Status = pmd.Status,
                    IsActive = pmd.IsActive
                })
                .ToListAsync(cancellationToken);

            if (allMappingRecords.Count == 0)
            {
                response.Success = false;
                response.Message = "No property merging details found";
                return response;
            }

            // Only active MODIFIED records can be cancelled
            var activeModifiedMappings = allMappingRecords.Where(x => x.IsActive && x.Status == "MODIFIED").ToList();

            if (activeModifiedMappings.Count == 0)
            {
                response.Success = false;
                response.Message = "No property merging details found to demerge";
                return response;
            }

            /*
             * 7. Select mapping records to update.
             * If old-count or new-count is greater than 2:
             *     Update only exact matching old/new row.
             * If both counts are 2 or less:
             *     Update both related rows.
             */
            var selectedMappingDictionary =
                new Dictionary<int, PropertyMappingSelection>();

            var missingPairs = new List<string>();
            var operationDetails = new List<string>();

            foreach (var pair in propertyPairs)
            {
                var oldPropertyIdCount = allMappingRecords.Count(x =>x.PropertyIdOld == pair.PropertyOldId);
                var newPropertyIdCount = allMappingRecords.Count(x =>x.PropertyIdNew == pair.PropertyId);

                List<PropertyMappingSelection> pairMappings;

                if (oldPropertyIdCount > 2 || newPropertyIdCount > 2)
                {
                    // Select only the exact matching row
                    pairMappings = activeModifiedMappings
                        .Where(x => x.PropertyIdOld == pair.PropertyOldId && x.PropertyIdNew == pair.PropertyId)
                        .ToList();
                }
                else
                {
                    // Select both related rows
                    pairMappings = activeModifiedMappings
                        .Where(x =>
                            (
                                x.PropertyIdOld == pair.PropertyOldId &&
                                (
                                    !x.PropertyIdNew.HasValue || x.PropertyIdNew == pair.PropertyId
                                )
                            )
                            ||
                            (
                                x.PropertyIdNew == pair.PropertyId &&
                                (
                                    !x.PropertyIdOld.HasValue || x.PropertyIdOld == pair.PropertyOldId
                                )
                            ))
                        .ToList();
                }

                if (pairMappings.Count == 0)
                {
                    oldPropertyNumberDictionary.TryGetValue(pair.PropertyOldId,out var missingOldPropertyNo);
                    newPropertyNumberDictionary.TryGetValue(pair.PropertyId,out var missingNewPropertyNo);
                    var missingOldDisplay = !string.IsNullOrWhiteSpace(missingOldPropertyNo) ? missingOldPropertyNo: pair.PropertyOldId.ToString();
                    var missingNewDisplay = !string.IsNullOrWhiteSpace(missingNewPropertyNo) ? missingNewPropertyNo : pair.PropertyId.ToString();
                    missingPairs.Add( $"{missingOldDisplay} -> {missingNewDisplay}");
                    continue;
                }

                oldPropertyNumberDictionary.TryGetValue(pair.PropertyOldId,out var oldPropertyNo);
                newPropertyNumberDictionary.TryGetValue(pair.PropertyId,out var newPropertyNo);
                var oldPropertyDisplay = !string.IsNullOrWhiteSpace(oldPropertyNo) ? oldPropertyNo : pair.PropertyOldId.ToString();
                var newPropertyDisplay = !string.IsNullOrWhiteSpace(newPropertyNo) ? newPropertyNo : pair.PropertyId.ToString();
                operationDetails.Add($"{oldPropertyDisplay} -> {newPropertyDisplay}");

                foreach (var mapping in pairMappings)
                {
                    selectedMappingDictionary[mapping.Id] = mapping;
                }
            }

            if (missingPairs.Count > 0)
            {
                response.Success = false;
                response.Message = $"Property merging details not found for property no : " + $"{string.Join(", ", missingPairs)}";
                return response;
            }

            if (selectedMappingDictionary.Count == 0)
            {
                response.Success = false;
                response.Message = "No property merging records found to demerge";
                return response;
            }

            var selectedMappings = selectedMappingDictionary.Values.ToList();
            var selectedMappingIds = selectedMappings.Select(x => x.Id).ToList();

            // 8. Begin transaction
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            transactionStarted = true;

            // 9. Find previous CANCELLED records that could cause
            // a unique-key conflict.
            
            var selectedPropertyMapIds = selectedMappings.Select(x => x.PropertyMapId).Distinct().ToList();

            var selectedOldIds = selectedMappings.Where(x => x.PropertyIdOld.HasValue)
                .Select(x => x.PropertyIdOld!.Value).Distinct().ToList();

            var selectedNewIds = selectedMappings.Where(x => x.PropertyIdNew.HasValue)
                .Select(x => x.PropertyIdNew!.Value).Distinct().ToList();

            var cancelledCandidates = await _propertyMapDetailRepository.GetQueryable().AsNoTracking()
                .Where(pmd =>
                    pmd.Status == "CANCELLED" &&
                    selectedPropertyMapIds.Contains(pmd.PropertyMapId) &&
                    (
                        (
                            pmd.PropertyIdOld.HasValue &&
                            selectedOldIds.Contains(pmd.PropertyIdOld.Value)
                        )
                        ||
                        (
                            pmd.PropertyIdNew.HasValue &&
                            selectedNewIds.Contains(pmd.PropertyIdNew.Value)
                        )
                    ))
                .Select(pmd => new PropertyMappingSelection
                {
                    Id = pmd.Id,
                    PropertyMapId = pmd.PropertyMapId,
                    PropertySide = pmd.PropertySide,
                    PropertyIdOld = pmd.PropertyIdOld,
                    PropertyIdNew = pmd.PropertyIdNew,
                    PropertyNo = pmd.PropertyNo,
                    Status = pmd.Status,
                    IsActive = pmd.IsActive
                })
                .ToListAsync(cancellationToken);

            /*
             * Keep only CANCELLED records matching the exact
             * unique-key values of selected MODIFIED rows.
             */
            var cancelledMappingIds = cancelledCandidates
                .Where(cancelled =>
                    selectedMappings.Any(current =>
                        current.PropertyMapId == cancelled.PropertyMapId &&
                        current.PropertySide == cancelled.PropertySide &&
                        current.PropertyIdOld == cancelled.PropertyIdOld &&
                        current.PropertyIdNew == cancelled.PropertyIdNew))
                .Select(x => x.Id)
                .Distinct().ToList();

            var deletedCount = 0;

            if (cancelledMappingIds.Count > 0)
            {
                deletedCount = await _propertyMapDetailRepository.GetQueryable()
                    .Where(pmd => cancelledMappingIds.Contains(pmd.Id) && pmd.Status == "CANCELLED")
                    .ExecuteDeleteAsync(cancellationToken);
            }

            var now = DateTime.Now;
            // 10. Update selected records
            var updatedCount = await _propertyMapDetailRepository.GetQueryable()
                .Where(pmd =>
                    selectedMappingIds.Contains(pmd.Id) && pmd.IsActive &&
                    pmd.Status == "MODIFIED")
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(pmd => pmd.Status,"CANCELLED")
                        .SetProperty(pmd => pmd.IsActive,false)
                        .SetProperty(pmd => pmd.IsCurrent,false)
                        .SetProperty(pmd => pmd.UpdatedBy,dto.UserId)
                        .SetProperty(pmd => pmd.UpdatedDate,now),
                    cancellationToken);

            // 11. Commit transaction
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            transactionStarted = false;

            // 12. Return property numbers instead of IDs
            response.Success = true;
            response.Message =$"Properties demerged successfully. " +$"{string.Join(", ", operationDetails)}";
            return response;
        }
        catch (Exception ex)
        {
            if (transactionStarted)
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            }
            response.Success = false;
            response.Message = $"Error during multiple property demerge: " + $"{ex.Message}";
            return response;
        }
    }
    public async Task<PagedResults<PropertyUnMergeResponseDto>> GetUnMergePropertyDetailsAsync(UnMergePropertydetailDto request, CancellationToken cancellationToken)
    {
        var result = new PagedResults<PropertyUnMergeResponseDto>
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        var propertyKey = await (
            from pm in _repository.GetQueryable().AsNoTracking()
            join societyTemp in _societyRepository.GetQueryable().AsNoTracking()
                on pm.SocietyDetailId equals societyTemp.Id 
                into societyGroup
            from propertySociety in societyGroup.DefaultIfEmpty()
            where pm.Id == request.PropertyId && pm.IsActive && !pm.MarkedForDeletion
            select new { pm.WardId,pm.PropertyNo,SocietyName = propertySociety != null? propertySociety.SocietyName : null })
            .FirstOrDefaultAsync(cancellationToken);

        if (propertyKey is null)
        {
            return new PagedResults<PropertyUnMergeResponseDto>();
        }

        var query =
            from pm in _repository.GetQueryable()
                .AsNoTracking()
            join ward in _wardRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                on pm.WardId equals ward.Id
            join societyTemp in _societyRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                on pm.SocietyDetailId equals societyTemp.Id
                into societyGroup
            from society in societyGroup.DefaultIfEmpty()
            join propertyTypeTemp in _propertyTypeRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                on pm.PropertyTypeId equals propertyTypeTemp.Id
                into propertyTypeGroup
            from propertyType in propertyTypeGroup.DefaultIfEmpty()
            join wingTemp in _wingMasterRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                on society.WingId equals wingTemp.Id
                into wingGroup
            from wing in wingGroup.DefaultIfEmpty()
            where
                pm.WardId == propertyKey.WardId &&
                pm.PropertyNo == propertyKey.PropertyNo &&
                pm.IsActive && !pm.MarkedForDeletion &&
                pm.PartitionNo != null &&
                pm.PartitionNo != string.Empty &&
                (wing == null || pm.PartitionNo != wing.WingNo) &&
                (propertyType == null || propertyType.PartType != "Amenity") &&
                (string.IsNullOrWhiteSpace(request.WingName) || (society.WingName ?? string.Empty).Trim() == request.WingName.Trim()) &&
                !_propertyMapDetailRepository.GetQueryable()
                    .Any(map => map.PropertyIdNew == pm.Id && map.IsActive && map.Status == "MODIFIED")

            select new PropertyUnMergeResponseDto
            {
                PropertyId = pm.Id,
                WardNo = ward.WardNo,
                PropertyNo = pm.PropertyNo,
                PartitionNo = pm.PartitionNo,
                OwnerName = pm.OwnerName,
                OccupierName = pm.OccupierName,
                Address = pm.Address,
                MobileNo = pm.MobileNo,
                Type = pm.Type,
                SocietyName = propertyKey.SocietyName,
                WingName = society != null ? society.WingName : null,
                FlatOrShopName = pm.FlatOrShopName,
                FlatOrShopNo = pm.FlatOrShopNo,
                PropertyTypeDescription = propertyType != null ? propertyType.PropertyDescription : null,
                BHK = _assessmentRepository.GetQueryable()
                    .Where(detail => detail.PropertyId == pm.Id && detail.IsActive && !detail.MarkedForDeletion)
                    .OrderByDescending(detail => detail.Id)
                    .Select(detail => detail.BHK)
                    .FirstOrDefault(),
            };

        result.TotalCount = await query.CountAsync(cancellationToken);
        var orderedQuery = query.OrderBy(x => x.PropertyId);
        if (request.PageSize <= 0)
        {
            result.PageNumber = 1;
            result.PageSize = result.TotalCount;
            result.Items = await orderedQuery.ToListAsync(cancellationToken);
        }
        else
        {
            result.Items = await orderedQuery
              .Skip((request.PageNumber - 1) * request.PageSize)
              .Take(request.PageSize)
              .ToListAsync(cancellationToken);
        }
        return result;
    }

    public async Task<PagedResults<OldPropertyUnMergeResponseDto>> GetUnMergeOldPropertyDetailsAsync(UnMergePropertydetailDto request,CancellationToken cancellationToken)
    {
        var result = new PagedResults<OldPropertyUnMergeResponseDto>
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        var propertyMapQuery = _propertyMapDetailRepository.GetQueryable().AsNoTracking();
        var oldPropertyQuery = _propertyOldRepository.GetQueryable().AsNoTracking();

        // get Society name.
        var societyQuery =( from map in propertyMapQuery
                            join oldProperty in oldPropertyQuery
                                on map.PropertyIdOld equals oldProperty.Id
                            where map.PropertyIdNew == request.PropertyId
                                  && map.IsActive && map.Status == "ACTIVE"
                                  && oldProperty.OldSocietyName != null
                            select oldProperty.OldSocietyName
                        ).Distinct();

        var query = from oldProperty in oldPropertyQuery
            join societyName in societyQuery
                on oldProperty.OldSocietyName equals societyName
            where !propertyMapQuery.Any(map => map.PropertyIdOld == oldProperty.Id && map.IsActive && map.Status == "MODIFIED") &&
               (string.IsNullOrWhiteSpace(request.WingName) || (oldProperty.OldWing ?? string.Empty).Trim() == request.WingName.Trim())

            select new OldPropertyUnMergeResponseDto
            {
                PropertyOldId = oldProperty.Id,
                OldWardNo = oldProperty.OldWardNo,
                OldPropertyNo = oldProperty.OldPropertyNo,
                OldPartitionNo = oldProperty.OldPartitionNo,
                OldOwnerName = oldProperty.OldOwnerName,
                OldOccupierName = oldProperty.OldOccupierName,
                OldAddress = oldProperty.OldAddress,
                OldFlatOrShopNumber = oldProperty.OldFlatOrShopNumber,
                OldWing = oldProperty.OldWing,
                OldSocietyName = oldProperty.OldSocietyName,
                OldRV = oldProperty.OldRV,
                OldGeneralTax = oldProperty.OldGeneralTax,
                OldTotalTax = oldProperty.OldTotalTax,
                OldConstructionYear = Convert.ToInt32(oldProperty.OldConstructionYear),
                OldConstructionArea = oldProperty.OldConstructionArea,
                OldUseType = oldProperty.OldUseType,
                OldMobileNo = oldProperty.OldMobileNo
            };
       
        result.TotalCount = await query.CountAsync(cancellationToken);
        var orderedQuery = query.OrderBy(x => x.PropertyOldId);

        if (request.PageSize <= 0)
        {
            result.PageNumber = 1;
            result.PageSize = result.TotalCount;
            result.Items = await orderedQuery.ToListAsync(cancellationToken);
        }
        else
        {
            result.Items = await orderedQuery
               .Skip((request.PageNumber - 1) * request.PageSize)
               .Take(request.PageSize)
               .ToListAsync(cancellationToken);
        }
        return result;
    }

    private static string BuildPropertyNumber(params string?[] propertyNumberParts)
    {
        return string.Join("-", propertyNumberParts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));
    }
    private static string BuildPropertyNumbers(IEnumerable<(string WardNo, string PropertyNo, string PartitionNo)> properties)
    {
        return string.Join(", ",properties.Select(x => BuildPropertyNumber(x.WardNo,x.PropertyNo,x.PartitionNo)).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
    }
    private static PropertyMapDetailEntity CreatePropertyMergeDetail(int propertyMapId,string propertySide,int? propertyIdNew,int? propertyIdOld,
                                        string propertyNo,string remark,decimal? latitude,decimal? longitude,string? location,int userId)
    {
        DateTime createdDate = DateTime.Now;
        return new PropertyMapDetailEntity
        {
            PropertyMapId = propertyMapId,
            PropertySide = propertySide,
            PropertyIdNew = propertyIdNew,
            PropertyIdOld = propertyIdOld,
            PropertyNo = propertyNo,
            Status = "MODIFIED",
            IsCurrent = true,
            Remark = remark,
            Latitude = latitude,
            Longitude = longitude,
            Location = location,
            IsActive = true,
            CreatedBy = userId,
            CreatedDate = createdDate
        };
    }
}