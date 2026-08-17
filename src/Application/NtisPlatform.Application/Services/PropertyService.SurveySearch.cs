using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Constants;
using NtisPlatform.Application.DTOs.PropertySurveySearch;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Models;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Services;

public partial class PropertyService
{
    public async Task<ApiResponse<PropertySurveySearchPaginatedResponseDto>>
        SearchSurveyPropertiesAsync(
            PropertySurveySearchQueryParameters request,
            CancellationToken cancellationToken = default)
    {
        ValidateSurveySearchRequest(request);
        NormalizeSurveySearchRequest(request);

        var result = request.Status == SurveySearchStatus.Old
            ? await SearchOldPropertiesAsync(request, cancellationToken)
            : await SearchNewPropertiesAsync(request, cancellationToken);

        return new ApiResponse<PropertySurveySearchPaginatedResponseDto>
        {
            Success = true,
            Message = result.Count > 0
                ? "Property search fetched successfully."
                : "Record not found.",
            Items = result
        };
    }

    private async Task<PropertySurveySearchPaginatedResponseDto>
        SearchNewPropertiesAsync(
            PropertySurveySearchQueryParameters request,
            CancellationToken cancellationToken)
    {
        var properties = _propertyRepository
            .GetQueryable()
            .AsNoTracking();

        var wards = _wardRepository
            .GetQueryable()
            .AsNoTracking();

        var propertyTypes = _propertyTypeRepository
            .GetQueryable()
            .AsNoTracking();

        var societies = _societyRepository
            .GetQueryable()
            .AsNoTracking();

        var query = properties.Where(property =>
            property.IsActive &&
            !property.MarkedForDeletion);

        query = query.Where(property =>
            wards.Any(ward =>
                ward.Id == property.WardId &&
                ward.IsActive &&
                ward.WardNo.ToString() == request.WardNo));

        if (request.PartitionNo != null)
        {
            query = query.Where(property =>
                property.PartitionNo == request.PartitionNo);
        }

        if (request.PropertyType == SurveyPropertyType.Apartment)
        {
            // Resolve apartment category IDs dynamically from PropertyCategoryMaster
            // instead of hardcoding ID = 6, matching the pattern used across the codebase.
            var apartmentCategoryIds = await _categoryRepository
                .GetQueryable()
                .AsNoTracking()
                .Where(category =>
                    category.IsActive &&
                    category.PropertyCategoryName.Contains(
                        CapitalValueConstants.PropertyCategory.ApartmentKeyword))
                .Select(category => category.Id)
                .ToListAsync(cancellationToken);

            query = query.Where(property =>
                property.CategoryId.HasValue &&
                apartmentCategoryIds.Contains(property.CategoryId.Value));
        }

        if (request.SearchText != null)
        {
            var search = request.SearchText;

            query = query.Where(property =>
                (property.UPICId != null &&
                 property.UPICId.Contains(search)) ||

                (property.CategoryId != null &&
                 property.CategoryId.ToString()!.Contains(search)) ||

                (property.PropertyNo != null &&
                 property.PropertyNo.Contains(search)) ||

                (property.PartitionNo != null &&
                 property.PartitionNo.Contains(search)) ||

                (property.OwnerName != null &&
                 property.OwnerName.Contains(search)) ||

                (property.OwnerNameEnglish != null &&
                 property.OwnerNameEnglish.Contains(search)) ||

                (property.OccupierName != null &&
                 property.OccupierName.Contains(search)) ||

                (property.OccupierNameEnglish != null &&
                 property.OccupierNameEnglish.Contains(search)) ||

                (property.Address != null &&
                 property.Address.Contains(search)) ||

                (property.AddressEnglish != null &&
                 property.AddressEnglish.Contains(search)) ||

                (property.MobileNo != null &&
                 property.MobileNo.Contains(search)) ||

                (property.EmailId != null &&
                 property.EmailId.Contains(search)) ||

                (property.FlatOrShopNo != null &&
                 property.FlatOrShopNo.Contains(search)) ||

                (property.FlatOrShopName != null &&
                 property.FlatOrShopName.Contains(search)) ||

                (property.FlatOrShopNoEnglish != null &&
                 property.FlatOrShopNoEnglish.Contains(search)) ||

                (property.FlatOrShopNameEnglish != null &&
                 property.FlatOrShopNameEnglish.Contains(search)) ||

                propertyTypes.Any(propertyType =>
                    propertyType.Id == property.PropertyTypeId &&
                    propertyType.IsActive &&
                    (
                        (propertyType.PropertyDescription != null &&
                         propertyType.PropertyDescription.Contains(search)) ||

                        (propertyType.Type != null &&
                         propertyType.Type.Contains(search))
                    )) ||

                societies.Any(society =>
                    society.Id == property.SocietyDetailId &&
                    society.IsActive &&
                    !society.MarkedForDeletion &&
                    (
                        (society.SocietyName != null &&
                         society.SocietyName.Contains(search)) ||

                        (society.SocietyNameEnglish != null &&
                         society.SocietyNameEnglish.Contains(search)) ||

                        (society.WingName != null &&
                         society.WingName.Contains(search))
                    )));
        }

        var resultQuery =
    from property in query

    join propertyType in propertyTypes
        on property.PropertyTypeId equals propertyType.Id
        into propertyTypeJoin

    from propertyType in propertyTypeJoin.DefaultIfEmpty()

    join ward in wards.Where(x => x.IsActive)
        on property.WardId equals ward.Id
        into wardJoin

    from ward in wardJoin.DefaultIfEmpty()

    orderby property.PropertyNo, property.Id

    select new PropertySurveySearchResponseDto
    {
        Id = property.Id,
        PropertyId = property.Id,
        Source = SurveySearchStatus.New,

        WardNo = ward != null
            ? ward.WardNo
            : null,

        PropertyNo = property.PropertyNo,
        PartitionNo = property.PartitionNo,
        UPICId = property.UPICId,
        CategoryId = property.CategoryId,

        PartType = propertyType != null
            ? propertyType.Type
            : null,

        PropertyTypeId = property.PropertyTypeId,

        PropertyDescription = propertyType != null
            ? propertyType.PropertyDescription
            : null,

        OwnerName = property.OwnerName,
        OwnerNameEnglish = property.OwnerNameEnglish,
        OccupierName = property.OccupierName,
        OccupierNameEnglish = property.OccupierNameEnglish,
        Address = property.Address,
        AddressEnglish = property.AddressEnglish,
        MobileNo = property.MobileNo,
        EmailId = property.EmailId,
        FlatOrShopNo = property.FlatOrShopNo,

        NewPropertyNo = property.PropertyNo,

        NewWardNo = ward != null
            ? ward.WardNo
            : null,

        NewPartitionNo = property.PartitionNo
    };

var results = await ApplyPagination(
        resultQuery,
        request)
    .ToListAsync(cancellationToken);

        var propertyIds = results
            .Select(x => x.PropertyId)
            .Distinct()
            .ToList();

        var oldPropertyIdsByNewPropertyId = await properties
            .Where(property =>
                propertyIds.Contains(property.Id) &&
                property.PropertyMastOldId.HasValue)
            .Select(property => new
            {
                property.Id,
                OldPropertyId = property.PropertyMastOldId!.Value
            })
            .ToDictionaryAsync(
                x => x.Id,
                x => x.OldPropertyId,
                cancellationToken);

        var oldPropertyIds = oldPropertyIdsByNewPropertyId
            .Values
            .Distinct()
            .ToList();

        var mapDetailsDict = await GetLatestMapDetailsAsync(
     oldPropertyIds,
     cancellationToken);

        foreach (var result in results)
        {
            string? status = null;

            if (result.PropertyId.HasValue &&
                oldPropertyIdsByNewPropertyId.TryGetValue(
                    result.PropertyId.Value,
                    out var oldPropertyId))
            {
                if (mapDetailsDict.TryGetValue(
                    oldPropertyId,
                    out var mapDetail))
                {
                    status = mapDetail.Status;
                }
            }

            result.Active = IsActiveMapStatus(status);
            result.Status = status;
        }

        return CreateSurveyPagedResponse(
            results,
            request.PageSize);
    }

    private async Task<PropertySurveySearchPaginatedResponseDto>
        SearchOldPropertiesAsync(
            PropertySurveySearchQueryParameters request,
            CancellationToken cancellationToken)
    {

        var allocatedOldWardNumbers =
        await GetAllocatedOldWardNumbersAsync(
        request,
        cancellationToken);

        if (allocatedOldWardNumbers.Count == 0)
        {
            return EmptySurveyResponse();
        }
        if (request.PropertyType == SurveyPropertyType.Apartment)
        {
            return await SearchOldPropertiesGroupedBySocietyAsync(
                request,
                cancellationToken);
        }

        var oldPropertyQuery = BuildOldPropertyQuery(
    request,
    allocatedOldWardNumbers,
    requireSociety: false);

        var oldResultQuery = oldPropertyQuery
    .OrderBy(property => property.OldSocietyName)
    .ThenBy(property => property.OldPropertyNo)
    .Select(property => new PropertySurveySearchResponseDto
    {
        Id = property.Id,
        PropertyId = null,
        Source = SurveySearchStatus.Old,

        WardNo = request.WardNo,
        OldWardNo = property.OldWardNo,

        PropertyNo = property.OldPropertyNo,
        PartitionNo = property.OldPartitionNo,
        PropertyTypeId = property.OldPropertyTypeId,

        OwnerName = property.OldOwnerName,
        OwnerNameEnglish = property.OldOwnerNameEnglish,
        OccupierName = property.OldOccupierName,
        OccupierNameEnglish = property.OldOccupierNameEnglish,

        Address = property.OldAddress,
        AddressEnglish = property.OldAddressEnglish,

        MobileNo = property.OldMobileNo,
        EmailId = property.OldEmailId,

        SocietyName = property.OldSocietyName,
        Wing = property.OldWing,
        FlatOrShopNo = property.OldFlatOrShopNumber,

        OldRV = (double?)property.OldRV,
        OldTotalTax = (double?)property.OldTotalTax,
        OldAssessmentYear = property.OldAssessmentYear,
        OldFloor = property.OldFloor,
        TotalArea = (double?)property.OldConstructionArea
    });

    var results = await ApplyPagination(
        oldResultQuery,
        request)
    .ToListAsync(cancellationToken);

        // Extract unique society names from the paginated results
        var societyNamesInPage = results
            .Select(r => r.SocietyName ?? string.Empty)
            .Distinct()
            .ToList();

        // Compute aggregates only for societies present in this page
        var societyAggregates = await oldPropertyQuery
            .Where(property => societyNamesInPage.Contains(property.OldSocietyName ?? string.Empty))
            .GroupBy(property => property.OldSocietyName)
            .Select(group => new PropertySocietyGroupedResponseDto
            {
                SocietyName = group.Key,

                TotalWingCount = group
                    .Select(property => property.OldWing)
                    .Distinct()
                    .Count(),

                TotalFlatShopCount = group
                    .Select(property => property.OldFlatOrShopNumber)
                    .Distinct()
                    .Count(),

               
            })
            .ToListAsync(cancellationToken);

        var societyAggregateDictionary = societyAggregates
            .GroupBy(x => x.SocietyName ?? string.Empty)
            .ToDictionary(
                group => group.Key,
                group => group.First());

        var oldPropertyIds = results
            .Select(result => result.Id)
            .Distinct()
            .ToList();

        var oldPropertyMetadata = await oldPropertyQuery
            .Where(property =>
                oldPropertyIds.Contains(property.Id))
            .Select(property => new
            {
                property.Id,
                property.OldConstructionYear
            })
            .ToDictionaryAsync(
                x => x.Id,
                cancellationToken);

        var mapDetailsDict = await GetLatestMapDetailsAsync(
            oldPropertyIds,
            cancellationToken);

        var mappedNewPropertyIds = mapDetailsDict
            .Values
            .Where(x => x.PropertyIdNew.HasValue)
            .Select(x => x.PropertyIdNew!.Value)
            .Distinct()
            .ToList();

        var newPropertiesByMappedId =
            await GetNewPropertiesByPropertyIdAsync(
                mappedNewPropertyIds,
                cancellationToken);

        var newPropertiesByOldId =
            await GetNewPropertiesByOldPropertyIdAsync(
                oldPropertyIds,
                cancellationToken);

        foreach (var result in results)
        {
            mapDetailsDict.TryGetValue(
                result.Id,
                out var mapDetail);

            societyAggregateDictionary.TryGetValue(
                result.SocietyName ?? string.Empty,
                out var aggregate);

            oldPropertyMetadata.TryGetValue(
                result.Id,
                out var metadata);

            var newProperty = ResolveMappedNewProperty(
                result.Id,
                mapDetail.PropertyIdNew,
                mapDetail.Status,
                newPropertiesByMappedId,
                newPropertiesByOldId);

            result.OldConstructionYear =
                ParseNullableInt(metadata?.OldConstructionYear);

            result.TotalWingCount =
                aggregate?.TotalWingCount ?? 0;

            result.TotalFlatShopCount =
                aggregate?.TotalFlatShopCount ?? 0;

           

            result.Active = IsActiveMapStatus(mapDetail.Status);
            result.Status = mapDetail.Status;

            result.NewPropertyNo = newProperty?.PropertyNo;
            result.NewWardNo = newProperty?.WardNo;
            result.NewPartitionNo = newProperty?.PartitionNo;
        }

        var mappedResults = results
            .OrderByDescending(result =>
                IsStatus(
                    result.Status,
                    PropertyMapStatus.Draft))
            .ThenBy(result => result.SocietyName)
            .ThenBy(result => result.PropertyNo)
            .ToList();

        return CreateSurveyPagedResponse(
            mappedResults,
            request.PageSize);
    }

    private async Task<PropertySurveySearchPaginatedResponseDto>
    SearchOldPropertiesGroupedBySocietyAsync(
        PropertySurveySearchQueryParameters request,
        CancellationToken cancellationToken)
    {

        var allocatedOldWardNumbers =
       await GetAllocatedOldWardNumbersAsync(
           request,
           cancellationToken);

        if (allocatedOldWardNumbers.Count == 0)
        {
            return EmptySurveyResponse();
        }

        var matchingPropertiesQuery = BuildOldPropertyQuery(
    request,
    allocatedOldWardNumbers,
    requireSociety: true);

        var matchedSocietyNames = await matchingPropertiesQuery
            .Select(property => property.OldSocietyName)
            .Where(societyName => societyName != null)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (matchedSocietyNames.Count == 0)
        {
            return EmptySurveyResponse();
        }

        var allSocietyPropertiesQuery = _propertyOldRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(property =>
                property.IsActive &&
                !property.MarkedForDeletion &&
                property.OldWardNo != null &&
                allocatedOldWardNumbers.Contains(property.OldWardNo) &&
                matchedSocietyNames.Contains(
                    property.OldSocietyName));

        var societyGroups = await allSocietyPropertiesQuery
            .GroupBy(property => property.OldSocietyName)
            .Select(group => new PropertySurveySearchResponseDto
            {
                SocietyName = group.Key,
                Source = SurveySearchStatus.Old,
                WardNo = request.WardNo,

                TotalWingCount = group
                    .Select(property => property.OldWing)
                    .Distinct()
                    .Count(),

                TotalFlatShopCount = group
                    .Select(property =>
                        property.OldFlatOrShopNumber)
                    .Distinct()
                    .Count(),

                Id = group
                    .OrderBy(property =>
                        property.OldPartitionNo == null ||
                        property.OldPartitionNo == string.Empty
                            ? 0
                            : 1)
                    .ThenBy(property => property.OldPropertyNo)
                    .Select(property => property.Id)
                    .FirstOrDefault(),

                OldWardNo = group
                    .OrderBy(property =>
                        property.OldPartitionNo == null ||
                        property.OldPartitionNo == string.Empty
                            ? 0
                            : 1)
                    .ThenBy(property => property.OldPropertyNo)
                    .Select(property => property.OldWardNo)
                    .FirstOrDefault(),

                PropertyNo = group
                    .OrderBy(property =>
                        property.OldPartitionNo == null ||
                        property.OldPartitionNo == string.Empty
                            ? 0
                            : 1)
                    .ThenBy(property => property.OldPropertyNo)
                    .Select(property => property.OldPropertyNo)
                    .FirstOrDefault(),

                PartitionNo = group
                    .OrderBy(property =>
                        property.OldPartitionNo == null ||
                        property.OldPartitionNo == string.Empty
                            ? 0
                            : 1)
                    .ThenBy(property => property.OldPropertyNo)
                    .Select(property => property.OldPartitionNo)
                    .FirstOrDefault(),

                PropertyTypeId = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldPropertyTypeId)
                    .FirstOrDefault(),

                OwnerName = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldOwnerName)
                    .FirstOrDefault(),

                OwnerNameEnglish = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldOwnerNameEnglish)
                    .FirstOrDefault(),

                OccupierName = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldOccupierName)
                    .FirstOrDefault(),

                OccupierNameEnglish = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldOccupierNameEnglish)
                    .FirstOrDefault(),

                Address = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldAddress)
                    .FirstOrDefault(),

                AddressEnglish = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldAddressEnglish)
                    .FirstOrDefault(),

                MobileNo = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldMobileNo)
                    .FirstOrDefault(),

                EmailId = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldEmailId)
                    .FirstOrDefault(),

                Wing = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldWing)
                    .FirstOrDefault(),

                FlatOrShopNo = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldFlatOrShopNumber)
                    .FirstOrDefault(),

                OldRV = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => (double?)property.OldRV)
                    .FirstOrDefault(),

                OldTotalTax = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => (double?)property.OldTotalTax)
                    .FirstOrDefault(),

                OldAssessmentYear = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldAssessmentYear)
                    .FirstOrDefault(),

                OldFloor = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldFloor)
                    .FirstOrDefault(),

                TotalArea = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property =>
                        (double?)property.OldConstructionArea)
                    .FirstOrDefault()
            })
            .OrderBy(group => group.SocietyName)
            .Skip(GetSkip(request))
            .Take(request.PageSize + 1)
            .ToListAsync(cancellationToken);

        var hasNext = societyGroups.Count > request.PageSize;

        var data = hasNext
            ? societyGroups.Take(request.PageSize).ToList()
            : societyGroups;

        var oldPropertyIds = data
            .Select(group => group.Id)
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        var oldPropertyMetadata = await allSocietyPropertiesQuery
            .Where(property =>
                oldPropertyIds.Contains(property.Id))
            .Select(property => new
            {
                property.Id,
                property.OldConstructionYear
            })
            .ToDictionaryAsync(
                x => x.Id,
                cancellationToken);

        var mapDetailsDict = await GetLatestMapDetailsAsync(
            oldPropertyIds,
            cancellationToken);

        var mappedNewPropertyIds = mapDetailsDict
            .Values
            .Where(x => x.PropertyIdNew.HasValue)
            .Select(x => x.PropertyIdNew!.Value)
            .Distinct()
            .ToList();

        var newPropertiesByMappedId =
            await GetNewPropertiesByPropertyIdAsync(
                mappedNewPropertyIds,
                cancellationToken);

        var newPropertiesByOldId =
            await GetNewPropertiesByOldPropertyIdAsync(
                oldPropertyIds,
                cancellationToken);

        foreach (var result in data)
        {
            mapDetailsDict.TryGetValue(
                result.Id,
                out var mapDetail);

            oldPropertyMetadata.TryGetValue(
                result.Id,
                out var metadata);

            var newProperty = ResolveMappedNewProperty(
                result.Id,
                mapDetail.PropertyIdNew,
                mapDetail.Status,
                newPropertiesByMappedId,
                newPropertiesByOldId);

            result.OldConstructionYear =
                ParseNullableInt(metadata?.OldConstructionYear);

            result.Active = IsActiveMapStatus(mapDetail.Status);
            result.Status = mapDetail.Status;

            result.NewPropertyNo = newProperty?.PropertyNo;
            result.NewWardNo = newProperty?.WardNo;
            result.NewPartitionNo = newProperty?.PartitionNo;
        }

        var mappedData = data
            .OrderByDescending(result =>
                IsStatus(
                    result.Status,
                    PropertyMapStatus.Draft))
            .ThenBy(result => result.SocietyName)
            .ThenBy(result => result.PropertyNo)
            .ToList();

        return new PropertySurveySearchPaginatedResponseDto
        {
            Data = mappedData,
            Count = mappedData.Count,
            HasNext = hasNext
        };
    }

    public async Task<PropertySocietyGroupedPaginatedResponseDto>
        SearchPropertiesBySocietyAsync(
            PropertySurveySearchQueryParameters request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.Status = NormalizeUpper(
            request.Status,
            SurveySearchStatus.New);

        if (request.Status != SurveySearchStatus.Old ||
            string.IsNullOrWhiteSpace(request.WardNo))
        {
            return EmptySocietyResponse();
        }

        if (request.PageNumber <= 0 ||
            request.PageSize <= 0)
        {
            return EmptySocietyResponse();
        }

        request.WardNo = request.WardNo.Trim();

        request.SearchText = ResolveSearchText(
            request.SearchText,
            request.SearchTerm);

        var numericWardNo =
            ExtractNumericPart(request.WardNo);

        if (string.IsNullOrWhiteSpace(numericWardNo))
        {
            return EmptySocietyResponse();
        }

        var oldPropertyQuery = _propertyOldRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(property =>
                property.IsActive &&
                !property.MarkedForDeletion &&
                property.OldWardNo == numericWardNo &&
                property.OldSocietyName != null &&
                property.OldSocietyName != string.Empty);

        oldPropertyQuery = ApplyOldPropertySearch(
            oldPropertyQuery,
            request.SearchText);

        var societyGroups = await oldPropertyQuery
            .GroupBy(property => property.OldSocietyName)
            .Select(group => new PropertySocietyGroupedResponseDto
            {
                SocietyName = group.Key,
                OldWardNo = numericWardNo,
                WardNo = request.WardNo,
                Source = SurveySearchStatus.Old,

                TotalWingCount = group
                    .Select(property => property.OldWing)
                    .Where(wing =>
                        wing != null &&
                        wing != string.Empty)
                    .Distinct()
                    .Count(),

                TotalFlatShopCount = group
                    .Select(property =>
                        property.OldFlatOrShopNumber)
                    .Where(flat =>
                        flat != null &&
                        flat != string.Empty)
                    .Distinct()
                    .Count(),

                TotalProperties = group.Count(),

                TotalRV = group.Sum(property =>
                    (double?)(property.OldRV ?? 0)) ?? 0,

                TotalTax = group.Sum(property =>
                    (double?)(property.OldTotalTax ?? 0)) ?? 0,

                TotalArea = group.Sum(property =>
                    (double?)(property.OldConstructionArea ?? 0)) ?? 0,

                Id = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => (int?)property.Id)
                    .FirstOrDefault(),

                PropertyId = 0,

                PropertyNo = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldPropertyNo)
                    .FirstOrDefault(),

                PartitionNo = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldPartitionNo)
                    .FirstOrDefault(),

                PropertyTypeId = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldPropertyTypeId)
                    .FirstOrDefault(),

                OwnerName = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldOwnerName)
                    .FirstOrDefault(),

                OwnerNameEnglish = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldOwnerNameEnglish)
                    .FirstOrDefault(),

                OccupierName = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldOccupierName)
                    .FirstOrDefault(),

                OccupierNameEnglish = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldOccupierNameEnglish)
                    .FirstOrDefault(),

                Address = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldAddress)
                    .FirstOrDefault(),

                AddressEnglish = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldAddressEnglish)
                    .FirstOrDefault(),

                MobileNo = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldMobileNo)
                    .FirstOrDefault(),

                EmailId = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldEmailId)
                    .FirstOrDefault(),

                Wing = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldWing)
                    .FirstOrDefault(),

                FlatOrShopNo = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => property.OldFlatOrShopNumber)
                    .FirstOrDefault(),

                OldRV = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => (double?)property.OldRV)
                    .FirstOrDefault(),

                OldTotalTax = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property => (double?)property.OldTotalTax)
                    .FirstOrDefault(),

                SampleTotalArea = group
                    .OrderBy(property => property.OldPropertyNo)
                    .Select(property =>
                        (double?)property.OldConstructionArea)
                    .FirstOrDefault()
            })
            .OrderBy(group => group.SocietyName)
            .Skip(GetSkip(request))
            .Take(request.PageSize + 1)
            .ToListAsync(cancellationToken);

        var hasNext =
            societyGroups.Count > request.PageSize;

        var data = hasNext
            ? societyGroups.Take(request.PageSize).ToList()
            : societyGroups;

        return new PropertySocietyGroupedPaginatedResponseDto
        {
            Data = data,
            Count = data.Count,
            HasNext = hasNext
        };
    }

    private IQueryable<PropertyMastOldEntity> BuildOldPropertyQuery(
     PropertySurveySearchQueryParameters request,
     IReadOnlyCollection<string> allocatedOldWardNumbers,
     bool requireSociety)
    {
        var wardNoExact = request.WardNo!;
        var wardNoNumeric =
            ExtractNumericPart(wardNoExact);

        var query = _propertyOldRepository
     .GetQueryable()
     .AsNoTracking()
     .Where(property =>
         property.IsActive &&
         !property.MarkedForDeletion &&
         property.OldWardNo != null &&
         allocatedOldWardNumbers.Contains(property.OldWardNo));

        if (requireSociety)
        {
            query = query.Where(property =>
                property.OldSocietyName != null &&
                property.OldSocietyName != string.Empty);
        }

        return ApplyOldPropertySearch(
            query,
            request.SearchText);
    }

    private static IQueryable<PropertyMastOldEntity>
        ApplyOldPropertySearch(
            IQueryable<PropertyMastOldEntity> query,
            string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return query;
        }

        return query.Where(property =>
            (property.OldSocietyName != null &&
             property.OldSocietyName.Contains(search)) ||

            (property.OldWing != null &&
             property.OldWing.Contains(search)) ||

            (property.OldFlatOrShopNumber != null &&
             property.OldFlatOrShopNumber.Contains(search)) ||

            (property.OldOwnerName != null &&
             property.OldOwnerName.Contains(search)) ||

            (property.OldOwnerNameEnglish != null &&
             property.OldOwnerNameEnglish.Contains(search)) ||

            (property.OldOccupierName != null &&
             property.OldOccupierName.Contains(search)) ||

            (property.OldOccupierNameEnglish != null &&
             property.OldOccupierNameEnglish.Contains(search)) ||

            (property.OldAddress != null &&
             property.OldAddress.Contains(search)) ||

            (property.OldAddressEnglish != null &&
             property.OldAddressEnglish.Contains(search)) ||

            (property.OldMobileNo != null &&
             property.OldMobileNo.Contains(search)) ||

            (property.OldEmailId != null &&
             property.OldEmailId.Contains(search)) ||

            (property.OldPropertyNo != null &&
             property.OldPropertyNo.Contains(search)) ||

            (property.OldPartitionNo != null &&
             property.OldPartitionNo.Contains(search)) ||

            (property.OldEgovNo != null &&
             property.OldEgovNo.Contains(search)) ||

            (property.OldPlotNo != null &&
             property.OldPlotNo.Contains(search)) ||

            (property.OldCSN != null &&
             property.OldCSN.Contains(search)));
    }

    private async Task<Dictionary<int, (int? PropertyIdNew, string? Status)>>
        GetLatestMapDetailsAsync(
            IReadOnlyCollection<int> oldPropertyIds,
            CancellationToken cancellationToken)
    {
        if (oldPropertyIds.Count == 0)
        {
            return new Dictionary<int, (int?, string?)>();
        }

        var mapDetails = await _propertyMapDetailRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(mapDetail =>
                mapDetail.PropertyIdOld.HasValue &&
                oldPropertyIds.Contains(
                    mapDetail.PropertyIdOld.Value))
            .GroupBy(mapDetail =>
                mapDetail.PropertyIdOld!.Value)
            .Select(group => new
            {
                PropertyOldId = group.Key,

                LatestMapDetail = group
                    .OrderByDescending(mapDetail =>
                        mapDetail.CreatedDate)
                    .Select(mapDetail => new
                    {
                        mapDetail.PropertyIdNew,
                        mapDetail.Status
                    })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return mapDetails.ToDictionary(
            detail => detail.PropertyOldId,
            detail => (detail.LatestMapDetail?.PropertyIdNew, detail.LatestMapDetail?.Status));
    }

    private async Task<Dictionary<int, PropertySurveySearchResponseDto>>
        GetNewPropertiesByPropertyIdAsync(
            IReadOnlyCollection<int> propertyIds,
            CancellationToken cancellationToken)
    {
        if (propertyIds.Count == 0)
        {
            return new Dictionary<int, PropertySurveySearchResponseDto>();
        }

        var properties = await BuildNewPropertyReferenceQuery()
    .Where(property =>
        property.PropertyId.HasValue &&
        propertyIds.Contains(property.PropertyId.Value))
    .ToListAsync(cancellationToken);

        return properties
            .Where(property => property.PropertyId.HasValue)
            .GroupBy(property => property.PropertyId.Value)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(property =>
                        property.PropertyId)
                    .First());
    }

    private async Task<Dictionary<int, PropertySurveySearchResponseDto>>
    GetNewPropertiesByOldPropertyIdAsync(
        IReadOnlyCollection<int> oldPropertyIds,
        CancellationToken cancellationToken)
    {
        if (oldPropertyIds.Count == 0)
        {
            return new Dictionary<int, PropertySurveySearchResponseDto>();
        }

        // Step 1:
        // Resolve Old PropertyId -> New PropertyId
        // using PTIS.PropertyMapDetail.
        var mappings = await _propertyMapDetailRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(mapDetail =>
                mapDetail.IsActive &&
                mapDetail.PropertyIdOld.HasValue &&
                oldPropertyIds.Contains(
                    mapDetail.PropertyIdOld.Value) &&
                mapDetail.PropertyIdNew.HasValue)
            .GroupBy(mapDetail =>
                mapDetail.PropertyIdOld!.Value)
            .Select(group => new
            {
                OldPropertyId = group.Key,

                NewPropertyId = group
                    .OrderByDescending(mapDetail =>
                        mapDetail.CreatedDate)
                    .Select(mapDetail =>
                        mapDetail.PropertyIdNew)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var newPropertyIds = mappings
            .Where(x => x.NewPropertyId.HasValue)
            .Select(x => x.NewPropertyId!.Value)
            .Distinct()
            .ToList();

        if (newPropertyIds.Count == 0)
        {
            return new Dictionary<int, PropertySurveySearchResponseDto>();
        }

        // Step 2:
        // Fetch actual NEW property details from PropertyMast.
        var newProperties = await BuildNewPropertyReferenceQuery()
            .Where(property =>
                property.PropertyId.HasValue &&
                newPropertyIds.Contains(
                    property.PropertyId.Value))
            .ToListAsync(cancellationToken);

        var newPropertyDictionary = newProperties
            .Where(property =>
                property.PropertyId.HasValue)
            .GroupBy(property =>
                property.PropertyId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(property =>
                        property.PropertyId)
                    .First());

        // Step 3:
        // Build:
        // OldPropertyId -> New Property DTO
        return mappings
            .Where(mapping =>
                mapping.NewPropertyId.HasValue &&
                newPropertyDictionary.ContainsKey(
                    mapping.NewPropertyId.Value))
            .ToDictionary(
                mapping => mapping.OldPropertyId,
                mapping =>
                    newPropertyDictionary[
                        mapping.NewPropertyId!.Value]);
    }

    private async Task<List<string>> GetAllocatedOldWardNumbersAsync(
    PropertySurveySearchQueryParameters request,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.WardNo) || !request.UserId.HasValue)
        {
            return [];
        }

        var wardNo = request.WardNo.Trim();

        // Step 1: Resolve current WardNo -> WardId(s)
        var wardIds = await _wardRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(ward =>
                ward.IsActive &&
                ward.WardNo != null &&
                ward.WardNo == wardNo)
            .Select(ward => ward.Id)
            .ToListAsync(cancellationToken);

        if (wardIds.Count == 0)
        {
            return [];
        }

        // Step 2: Get OldWardId(s) allocated to this user + current ward
        var oldWardIds = await _wardAllocationRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(allocation =>
                allocation.UserId == request.UserId.Value &&
                allocation.IsActive &&
                wardIds.Contains(allocation.WardId) &&
                allocation.OldWardId.HasValue)
            .Select(allocation =>
                allocation.OldWardId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (oldWardIds.Count == 0)
        {
            return [];
        }

        // Step 3: Resolve OldWardId -> OldWardNo
        return await _oldWardMasterRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(oldWard =>
                oldWardIds.Contains(oldWard.Id) &&
                oldWard.IsActive &&
                oldWard.OldWardNo != null)
            .Select(oldWard => oldWard.OldWardNo!)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private IQueryable<PropertySurveySearchResponseDto>
        BuildNewPropertyReferenceQuery()
    {
        var properties = _propertyRepository
            .GetQueryable()
            .AsNoTracking();

        var wards = _wardRepository
            .GetQueryable()
            .AsNoTracking();

        return
            from property in properties

            join ward in wards.Where(x => x.IsActive)
                on property.WardId equals ward.Id
                into wardJoin

            from ward in wardJoin.DefaultIfEmpty()

            where property.IsActive &&
                  !property.MarkedForDeletion

            select new PropertySurveySearchResponseDto
            {
                Id = property.Id,
                PropertyId = property.Id,

                PropertyNo = property.PropertyNo,
                PartitionNo = property.PartitionNo,

                WardNo = ward != null
                    ? ward.WardNo
                    : null
            };
    }

    private static PropertySurveySearchResponseDto?
        ResolveMappedNewProperty(
            int oldPropertyId,
            int? mappedNewPropertyId,
            string? status,
            IReadOnlyDictionary<int, PropertySurveySearchResponseDto>
                propertiesByMappedId,
            IReadOnlyDictionary<int, PropertySurveySearchResponseDto>
                propertiesByOldId)
    {
        if (IsStatus(
                status,
                PropertyMapStatus.Active) &&
            mappedNewPropertyId.HasValue &&
            propertiesByMappedId.TryGetValue(
                mappedNewPropertyId.Value,
                out var activeProperty))
        {
            return activeProperty;
        }

        if (IsStatus(
                status,
                PropertyMapStatus.Modified) &&
            propertiesByOldId.TryGetValue(
                oldPropertyId,
                out var modifiedProperty))
        {
            return modifiedProperty;
        }

        return null;
    }

    private static void ValidateSurveySearchRequest(
        PropertySurveySearchQueryParameters request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.WardNo))
        {
            throw new PropertyValidationException(
                "WardNo is required.");
        }

        if (request.PageNumber <= 0)
        {
            throw new PropertyValidationException(
                "PageNumber must be greater than zero.");
        }

        if (request.PageSize == -1)
        {
            throw new PropertyValidationException(
                "PageSize must be greater than zero.");
        }
    }

    private static void NormalizeSurveySearchRequest(
        PropertySurveySearchQueryParameters request)
    {
        request.WardNo = request.WardNo!.Trim();

        request.Status = NormalizeUpper(
            request.Status,
            SurveySearchStatus.New);

        request.PropertyType = NormalizeUpper(
            request.PropertyType,
            SurveyPropertyType.All);

        request.SearchText = ResolveSearchText(
            request.SearchText,
            request.SearchTerm);

        request.PartitionNo =
            NormalizeOptionalValue(request.PartitionNo);
    }

    private static PropertySurveySearchPaginatedResponseDto
    CreateSurveyPagedResponse(
        List<PropertySurveySearchResponseDto> items,
        int pageSize)
{
    if (pageSize == -1)
    {
        return new PropertySurveySearchPaginatedResponseDto
        {
            Data = items,
            Count = items.Count,
            HasNext = false
        };
    }

    var hasNext = items.Count > pageSize;

    var data = hasNext
        ? items.Take(pageSize).ToList()
        : items;

    return new PropertySurveySearchPaginatedResponseDto
    {
        Data = data,
        Count = data.Count,
        HasNext = hasNext
    };
}

    private static PropertySurveySearchPaginatedResponseDto
        EmptySurveyResponse()
    {
        return new PropertySurveySearchPaginatedResponseDto
        {
            Data = [],
            Count = 0,
            HasNext = false
        };
    }

    private static PropertySocietyGroupedPaginatedResponseDto
        EmptySocietyResponse()
    {
        return new PropertySocietyGroupedPaginatedResponseDto
        {
            Data = [],
            Count = 0,
            HasNext = false
        };
    }

    private static bool IsActiveMapStatus(string? status)
    {
        return IsStatus(
                   status,
                   PropertyMapStatus.Draft) ||
               IsStatus(
                   status,
                   PropertyMapStatus.Active);
    }

    private static bool IsStatus(
        string? status,
        string expectedStatus)
    {
        return string.Equals(
            status,
            expectedStatus,
            StringComparison.OrdinalIgnoreCase);
    }

    private static int GetSkip(
        PropertySurveySearchQueryParameters request)
    {
        if (request.PageSize == -1)
        {
            return 0;
        }

        var skip =
            ((long)request.PageNumber - 1L) *
            request.PageSize;

        if (skip > int.MaxValue)
        {
            throw new PropertyValidationException(
                "PageNumber/PageSize combination is too large.");
        }

        return (int)skip;
    }

    private static IQueryable<T> ApplyPagination<T>(
    IQueryable<T> query,
    PropertySurveySearchQueryParameters request)
    {
        if (request.PageSize == -1)
        {
            return query;
        }

        return query
            .Skip(GetSkip(request))
            .Take(request.PageSize + 1);
    }

    private static string NormalizeUpper(
        string? value,
        string defaultValue)
    {
        return string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : value.Trim().ToUpperInvariant();
    }

    private static string? NormalizeOptionalValue(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string? ResolveSearchText(
        string? searchText,
        string? searchTerm)
    {
        var value = !string.IsNullOrWhiteSpace(searchText)
            ? searchText
            : searchTerm;

        return NormalizeOptionalValue(value);
    }

    private static int? ParseNullableInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return int.TryParse(
            value.Trim(),
            out var parsedValue)
                ? parsedValue
                : null;
    }

    private static string ExtractNumericPart(string wardNo)
    {
        return string.IsNullOrWhiteSpace(wardNo)
            ? string.Empty
            : new string(
                wardNo
                    .Where(char.IsDigit)
                    .ToArray());
    }
}