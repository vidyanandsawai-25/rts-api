using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.PropertyBuildingInformation;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Building-information search operations for properties.
/// </summary>
public partial class PropertyService
{
    /// <summary>
    /// Searches old-property building information using ward number,
    /// optional society name and optional property map.
    /// </summary>
    public async Task<PagedResult<PropertyBuildingInformationDto>>
        SearchBuildingInformationAsync(
            BuildingInformationQueryParameters queryParameters,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        var oldWardNo = queryParameters.OldWardNo?.Trim();

        if (string.IsNullOrWhiteSpace(oldWardNo))
        {
            throw new InvalidOperationException(
                "BuildingInformation_OldWardNo_Required");
        }

        var oldSocietyName =
            string.IsNullOrWhiteSpace(queryParameters.OldSocietyName)
                ? null
                : queryParameters.OldSocietyName.Trim();

        var pageNumber = queryParameters.PageNumber <= 0
            ? 1
            : queryParameters.PageNumber;

        var pageSize = queryParameters.PageSize <= 0
            ? 10
            : queryParameters.PageSize;

        /*
         * Start from PropertyMastOld.
         *
         * This ensures old-property records are returned even when there is
         * no related PropertyMast record.
         */
        var oldPropertyQuery = _propertyOldRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                !x.MarkedForDeletion &&
                x.OldWardNo == oldWardNo);

        if (oldSocietyName != null)
        {
            oldPropertyQuery = oldPropertyQuery.Where(x =>
                x.OldSocietyName != null &&
                x.OldSocietyName.Contains(oldSocietyName));
        }

        /*
         * When MapId is supplied, return only old properties belonging
         * to that property map.
         */
        if (queryParameters.MapId is > 0)
        {
            var mapId = queryParameters.MapId.Value;

            var mappedOldPropertyIds = _propertyMapDetailRepository
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.PropertyMapId == mapId &&
                    x.PropertyIdOld.HasValue &&
                    x.IsActive)
                .Select(x => x.PropertyIdOld!.Value)
                .Distinct();

            oldPropertyQuery = oldPropertyQuery.Where(x =>
                mappedOldPropertyIds.Contains(x.Id));
        }

        var propertyQuery = _repository
            .GetQueryable()
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                !x.MarkedForDeletion);

        var societyQuery = _societyRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                !x.MarkedForDeletion);

        var roomWiseQuery = _roomWiseRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                !x.MarkedForDeletion);

        var mapDetailQuery = _propertyMapDetailRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                x.PropertyIdOld.HasValue);

        var query =
            from oldProperty in oldPropertyQuery

            join property in propertyQuery
                on oldProperty.Id equals property.PropertyMastOldId
                into propertyGroup
            from property in propertyGroup.DefaultIfEmpty()

            join society in societyQuery
                on (property == null ? null : property.SocietyDetailId) equals (int?)society.Id
                into societyGroup
            from society in societyGroup.DefaultIfEmpty()

            let roomWiseDetail = roomWiseQuery
                .Where(x =>
                    property != null &&
                    x.PropertyId == property.Id)
                .OrderBy(x => x.Id)
                .FirstOrDefault()

            let latestMapDetail = mapDetailQuery
                .Where(x =>
                    x.PropertyIdOld == oldProperty.Id)
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault()

            select new PropertyBuildingInformationDto
            {
                PropertyId = property != null
       ? property.Id
       : 0,

                Id = oldProperty.Id,
                OldPropertyNo = oldProperty.OldPropertyNo,
                OldWing = oldProperty.OldWing,
                OldFlatOrShopNumber = oldProperty.OldFlatOrShopNumber,
                OldPropertyTypeId = oldProperty.OldPropertyTypeId,
                OldOwnerName = oldProperty.OldOwnerName,
                OldMobileNo = oldProperty.OldMobileNo,

                OldRV = oldProperty.OldRV.HasValue
       ? (decimal?)Convert.ToDecimal(oldProperty.OldRV.Value)
       : null,

                OldTotalTax = oldProperty.OldTotalTax.HasValue
       ? (decimal?)Convert.ToDecimal(oldProperty.OldTotalTax.Value)
       : null,

                BuilderName = society != null
       ? society.BuilderName
       : null,

                BuilderNameEnglish = society != null
       ? society.BuilderNameEnglish
       : null,

                BuilderMobileNo = society != null
       ? society.BuilderMobileNo
       : null,

                BuilderMobileNoRemarkId = society != null
       ? society.BuilderMobileNoRemarkId
       : null,

                AreaSqMtr = roomWiseDetail != null &&
                roomWiseDetail.AreaSqMtr.HasValue
       ? (decimal?)Convert.ToDecimal(roomWiseDetail.AreaSqMtr.Value)
       : null,

                TotalAreaSqMtr = roomWiseDetail != null &&
                     roomWiseDetail.TotalAreaSqMtr.HasValue
       ? (decimal?)Convert.ToDecimal(roomWiseDetail.TotalAreaSqMtr.Value)
       : null,

                Identify =
       latestMapDetail != null &&
       latestMapDetail.Status != null &&
       (
           latestMapDetail.Status == "DRAFT" ||
           latestMapDetail.Status == "ACTIVE"
       )
            };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.OldPropertyNo)
            .ThenBy(x => x.OldWing)
            .ThenBy(x => x.OldFlatOrShopNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return CreateBuildingInformationPage(
            items,
            totalCount,
            pageNumber,
            pageSize);
    }

    private static PagedResult<PropertyBuildingInformationDto>
        CreateBuildingInformationPage(
            List<PropertyBuildingInformationDto> items,
            int totalCount,
            int pageNumber,
            int pageSize)
    {
        return new PagedResult<PropertyBuildingInformationDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}