using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.PropertyBuildingInformation;
using NtisPlatform.Core.Entities;
using System.Text.RegularExpressions;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Building-information search operations for properties.
/// </summary>
public partial class PropertyService
{
    public async Task<List<PropertyBuildingInformationDto>> SearchBuildingInformationAsync(
        List<SearchBuildingInformationDto> dtos,
        CancellationToken cancellationToken = default)
    {
        if (dtos == null || dtos.Count == 0)
        {
            return new List<PropertyBuildingInformationDto>();
        }

        // Remove invalid and duplicate ward-society combinations
        var distinctCriteria = dtos
            .Where(x => !string.IsNullOrWhiteSpace(x.OldWardNo))
            .GroupBy(x => new
            {
                OldWardNo = x.OldWardNo.Trim().ToUpperInvariant(),

                OldSocietyName = string.IsNullOrWhiteSpace(x.OldSocietyName)
                    ? string.Empty
                    : x.OldSocietyName.Trim().ToUpperInvariant(),

                x.MapId
            })
            .Select(group => group.First())
            .ToList();

        var finalResults = new List<PropertyBuildingInformationDto>();

        foreach (var dto in distinctCriteria)
        {
            var results = await SearchBuildingInformationByCriteriaAsync(
                dto,
                cancellationToken);

            finalResults.AddRange(results);
        }

        // Prevent duplicate response records and sort Wing-wise & Flat-wise
        var uniqueResults = finalResults
            .GroupBy(x => new
            {
                x.PropertyId,
                OldPropertyId = x.Id
            })
            .Select(group => group.First())
            .ToList();

        return SortBuildingInformationWingWise(uniqueResults);
    }

    private async Task<List<PropertyBuildingInformationDto>> SearchBuildingInformationByCriteriaAsync(
        SearchBuildingInformationDto dto,
        CancellationToken cancellationToken = default)
    {
        // Step 1: If MapId is provided (and greater than 0), filter by PropertyMapDetail first
        List<int> filteredPropertyMastOldIds = new List<int>();

        if (dto.MapId.HasValue && dto.MapId.Value > 0)
        {
            // Get PropertyMastOld IDs from PropertyMapDetail filtered by MapId
            var mapDetailsFiltered = await _propertyMapDetailRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.PropertyMapId == dto.MapId.Value &&
                           x.PropertyIdOld.HasValue &&
                           x.IsActive)
                .Select(x => x.PropertyIdOld!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (mapDetailsFiltered.Count == 0)
                return new List<PropertyBuildingInformationDto>();

            filteredPropertyMastOldIds = mapDetailsFiltered;
        }

        if (string.IsNullOrWhiteSpace(dto.OldWardNo))
            return new List<PropertyBuildingInformationDto>();

        var oldWardNo = dto.OldWardNo.Trim();

        // Step 2: Query PropertyMastOld to find properties by OldWardNo
        var propertyMastOldQuery = _propertyOldRepository.GetQueryable()
            .AsNoTracking()
            .Where(x => x.OldWardNo != null &&
                        (x.OldWardNo == oldWardNo || x.OldWardNo.Trim() == oldWardNo) &&
                        x.IsActive &&
                        !x.MarkedForDeletion);

        // Apply MapId filter if provided and greater than 0
        if (dto.MapId.HasValue && dto.MapId.Value > 0 && filteredPropertyMastOldIds.Count > 0)
        {
            propertyMastOldQuery = propertyMastOldQuery.Where(x => filteredPropertyMastOldIds.Contains(x.Id));
        }

        var propertyMastOldRecords = await propertyMastOldQuery.ToListAsync(cancellationToken);

        if (propertyMastOldRecords.Count == 0)
            return new List<PropertyBuildingInformationDto>();

        // Step 3: Filter by OldSocietyName in PropertyMastOld if provided
        if (!string.IsNullOrWhiteSpace(dto.OldSocietyName))
        {
            var searchTerm = dto.OldSocietyName.Trim();

            propertyMastOldRecords = propertyMastOldRecords
                .Where(x => x.OldSocietyName != null &&
                           x.OldSocietyName.Trim().Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (propertyMastOldRecords.Count == 0)
                return new List<PropertyBuildingInformationDto>();
        }

        // Step 4: Get PropertyMastOldIds
        var propertyMastOldIds = propertyMastOldRecords.Select(x => x.Id).ToList();

        // Step 5: Get PropertyMapDetail records for these Old Property IDs
        var propertyMapDetails = await _propertyMapDetailRepository.GetQueryable()
            .AsNoTracking()
            .Where(x => x.PropertyIdOld.HasValue && propertyMastOldIds.Contains(x.PropertyIdOld.Value) && x.IsActive)
            .GroupBy(x => x.PropertyIdOld)
            .Select(g => new
            {
                PropertyOldId = g.Key,
                MapDetail = g.OrderByDescending(x => x.CreatedDate).ThenByDescending(x => x.Id).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var propertyMapDict = propertyMapDetails
            .Where(x => x.MapDetail != null && x.PropertyOldId.HasValue)
            .ToDictionary(x => x.PropertyOldId!.Value, x => x.MapDetail!);

        // Step 6: Identify mapped new property IDs (PropertyMast.Id)
        var mappedNewPropertyIds = propertyMapDict.Values
            .Where(x => x.PropertyIdNew.HasValue && x.PropertyIdNew.Value > 0)
            .Select(x => x.PropertyIdNew!.Value)
            .Distinct()
            .ToList();

        // Step 7: Fetch linked PropertyMast records (if mapped)
        var properties = mappedNewPropertyIds.Count > 0
            ? await _repository.GetQueryable()
                .AsNoTracking()
                .Where(p => mappedNewPropertyIds.Contains(p.Id) && p.IsActive && !p.MarkedForDeletion)
                .ToListAsync(cancellationToken)
            : new List<PropertyEntity>();

        var propertyDict = properties.ToDictionary(p => p.Id);

        // Step 8: Fetch SocietyDetailsMast records for mapped properties
        var societies = mappedNewPropertyIds.Count > 0
            ? await _societyRepository.GetQueryable()
                .AsNoTracking()
                .Where(s => s.PropertyId.HasValue && mappedNewPropertyIds.Contains(s.PropertyId.Value) && s.IsActive && !s.MarkedForDeletion)
                .ToListAsync(cancellationToken)
            : new List<SocietyDetailsEntity>();

        var societyDict = societies
            .Where(s => s.PropertyId.HasValue)
            .GroupBy(s => s.PropertyId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        // Step 9: Fetch RoomWiseSubmissionDetails records for mapped properties
        var roomWiseDetailsList = mappedNewPropertyIds.Count > 0
            ? await _roomWiseRepository.GetQueryable()
                .AsNoTracking()
                .Where(x => x.PropertyId.HasValue && mappedNewPropertyIds.Contains(x.PropertyId.Value) && x.IsActive && !x.MarkedForDeletion)
                .ToListAsync(cancellationToken)
            : new List<RoomWiseSubmissionDetailsEntity>();

        var roomWiseDetailsDict = roomWiseDetailsList
            .Where(x => x.PropertyId.HasValue)
            .GroupBy(x => x.PropertyId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).First());

        // Step 10: Build result list
        var results = new List<PropertyBuildingInformationDto>();

        foreach (var propertyMastOld in propertyMastOldRecords)
        {
            var hasMapDetail = propertyMapDict.TryGetValue(propertyMastOld.Id, out var mapDetail);
            bool identify = hasMapDetail && mapDetail != null && mapDetail.Status != null &&
                           (mapDetail.Status.Equals("DRAFT", StringComparison.OrdinalIgnoreCase) ||
                            mapDetail.Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase));

            int newPropertyId = 0;
            SocietyDetailsEntity? society = null;
            RoomWiseSubmissionDetailsEntity? roomWiseDetails = null;

            if (hasMapDetail && mapDetail?.PropertyIdNew != null && mapDetail.PropertyIdNew.Value > 0)
            {
                if (propertyDict.TryGetValue(mapDetail.PropertyIdNew.Value, out var propEntity))
                {
                    newPropertyId = propEntity.Id;
                    societyDict.TryGetValue(newPropertyId, out society);
                    roomWiseDetailsDict.TryGetValue(newPropertyId, out roomWiseDetails);
                }
            }

            results.Add(new PropertyBuildingInformationDto
            {
                PropertyId = newPropertyId,

                // From PropertyMastOld
                Id = propertyMastOld.Id,
                OldPropertyNo = propertyMastOld.OldPropertyNo,
                OldWing = propertyMastOld.OldWing,
                OldFlatOrShopNumber = propertyMastOld.OldFlatOrShopNumber,
                OldPropertyTypeId = propertyMastOld.OldPropertyTypeId,
                OldOwnerName = propertyMastOld.OldOwnerName,
                OldMobileNo = propertyMastOld.OldMobileNo,
                OldRV = propertyMastOld.OldRV.HasValue ? (decimal?)propertyMastOld.OldRV.Value : null,
                OldTotalTax = propertyMastOld.OldTotalTax.HasValue ? (decimal?)propertyMastOld.OldTotalTax.Value : null,
                SocietyName = society?.SocietyName ?? propertyMastOld.OldSocietyName,

                // From SocietyDetailsMast (if mapped PropertyMast exists)
                BuilderName = society?.BuilderName,
                BuilderNameEnglish = society?.BuilderNameEnglish,
                BuilderMobileNo = society?.BuilderMobileNo,
                BuilderMobileNoRemarkId = society?.BuilderMobileNoRemarkId,

                // From RoomWiseSubmissionDetails (if mapped PropertyMast exists)
                AreaSqMtr = roomWiseDetails?.AreaSqMtr.HasValue == true ? (decimal)roomWiseDetails.AreaSqMtr.Value : null,
                TotalAreaSqMtr = roomWiseDetails?.TotalAreaSqMtr.HasValue == true ? (decimal)roomWiseDetails.TotalAreaSqMtr.Value : null,

                // From PropertyMapDetail
                Identify = identify
            });
        }

        return results;
    }

    private static List<PropertyBuildingInformationDto> SortBuildingInformationWingWise(List<PropertyBuildingInformationDto> items)
    {
        if (items == null || items.Count == 0)
        {
            return new List<PropertyBuildingInformationDto>();
        }

        return items
            .Select(x => new
            {
                Item = x,
                HasWing = !string.IsNullOrWhiteSpace(x.OldWing),
                WingName = !string.IsNullOrWhiteSpace(x.OldWing) ? x.OldWing.Trim().ToUpperInvariant() : string.Empty,
                FlatInfo = ParseFlatNumberInfo(x.OldFlatOrShopNumber)
            })
            .OrderBy(x => x.HasWing ? 0 : 1)
            .ThenBy(x => x.WingName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.FlatInfo.Group)
            .ThenBy(x => x.FlatInfo.Number)
            .ThenBy(x => x.FlatInfo.Raw, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Item)
            .ToList();
    }

    private static (int Group, int Number, string Raw) ParseFlatNumberInfo(string? flatOrShopNumber)
    {
        if (string.IsNullOrWhiteSpace(flatOrShopNumber))
        {
            return (1, int.MaxValue, string.Empty);
        }

        string raw = flatOrShopNumber.Trim();

        // Direct integer match e.g. "1", "12", "101"
        if (int.TryParse(raw, out int directNum))
        {
            return (0, directNum, raw);
        }

        // Extract leading or contained numeric value e.g. "1A", "Flat 5"
        var match = Regex.Match(raw, @"\d+");
        if (match.Success && int.TryParse(match.Value, out int extractedNum))
        {
            return (0, extractedNum, raw);
        }

        // Purely non-numeric string
        return (1, int.MaxValue, raw);
    }
}