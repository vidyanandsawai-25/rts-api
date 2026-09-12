using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.OldSociety;

namespace NtisPlatform.Application.Services;

public partial class PropertyService
{
    public async Task<OldSocietyResponseDto?> GetOldSocietiesAsync(
        SearchOldSocietyDto dto,
        CancellationToken cancellationToken = default)
    {
        var wardNo = dto.WardNo.Trim();
        var searchText = dto.SearchText?.Trim();
        var numericPart = ExtractNumericPart(wardNo);
        int.TryParse(wardNo, out var directWardId);
        int.TryParse(numericPart, out var parsedWardId);

        var wardIds = await _wardRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(w =>
                w.IsActive &&
                w.WardNo != null &&
                (
                    w.WardNo.Trim() == wardNo ||
                    (!string.IsNullOrEmpty(numericPart) && w.WardNo.Trim() == numericPart) ||
                    (directWardId > 0 && w.Id == directWardId) ||
                    (parsedWardId > 0 && w.Id == parsedWardId)
                ))
            .Select(w => w.Id)
            .ToListAsync(cancellationToken);



        if (wardIds.Count == 0)
        {
            return new OldSocietyResponseDto
            {
                Data = new List<OldSocietyDto>(),
                Count = 0,
                PageNumber = dto.PageNumber,
                PageSize = dto.PageSize,
                TotalPages = 0,
                HasNext = false,
                HasPrevious = false
            };
        }

        var wardAllocations = _wardAllocationRepository.GetQueryable().AsNoTracking();
        var oldWards = _oldWardMasterRepository.GetQueryable().AsNoTracking();

        var allocatedOldWardNos = await (
            from wa in wardAllocations
            join ow in oldWards on wa.OldWardId equals ow.Id
            where wa.IsActive && ow.IsActive && ow.OldWardNo != null && wardIds.Contains(wa.WardId)
            select ow.OldWardNo!.Trim()
        ).Distinct().ToListAsync(cancellationToken);

        if (allocatedOldWardNos.Count == 0)
        {
            return new OldSocietyResponseDto
            {
                Data = new List<OldSocietyDto>(),
                Count = 0,
                PageNumber = dto.PageNumber,
                PageSize = dto.PageSize,
                TotalPages = 0,
                HasNext = false,
                HasPrevious = false
            };
        }

        var mapDetails = _propertyMapDetailRepository.GetQueryable().AsNoTracking();

        var query = _propertyOldRepository
            .GetQueryable()
            .AsNoTracking()
            .Where(x =>
                x.OldWardNo != null &&
                allocatedOldWardNos.Contains(x.OldWardNo.Trim()) &&
                x.OldSocietyName != null &&
                x.OldSocietyName.Trim() != string.Empty &&
                x.IsActive &&
                !x.MarkedForDeletion &&
                !mapDetails.Any(pmd =>
                    pmd.IsActive &&
                    pmd.PropertyIdOld == x.Id &&
                    pmd.Status != null &&
                    (pmd.Status == "ACTIVE" || pmd.Status == "Active")));

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var searchPattern = $"%{searchText}%";
            var searchWithSpacePattern = searchText.Contains("-") ? $"%{searchText.Replace("-", " ")}%" : searchPattern;

            query = query.Where(x =>
                (x.OldSocietyName != null && (
                    EF.Functions.Like(x.OldSocietyName, searchPattern) ||
                    EF.Functions.Like(x.OldSocietyName, searchWithSpacePattern)
                )) ||

                (x.OldAddress != null && (
                    EF.Functions.Like(x.OldAddress, searchPattern) ||
                    EF.Functions.Like(x.OldAddress, searchWithSpacePattern)
                )) ||

                (x.OldWing != null &&
                 EF.Functions.Like(x.OldWing, searchPattern)) ||

                (x.OldFlatOrShopNumber != null &&
                 EF.Functions.Like(x.OldFlatOrShopNumber, searchPattern)));
        }

        /*
         * Load all matching property rows first.
         *
         * Pagination must not be applied here because one society
         * may contain multiple property rows, wings and flats.
         */
        var societyData = await query
            .Select(x => new
            {
                OldSocietyName = x.OldSocietyName!,
                x.OldAddress,
                x.OldWing,
                x.OldFlatOrShopNumber
            })
            .ToListAsync(cancellationToken);

        /*
         * Group by society name so each society appears once.
         */
        var groupedSocieties = societyData
            .GroupBy(x =>
                System.Text.RegularExpressions.Regex.Replace(
                    x.OldSocietyName.Trim().ToUpperInvariant(),
                    @"\s+",
                    "").Replace(".", ""))
            .Select(group =>
            {
                var distinctWings = group
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.OldWing))
                    .Select(x =>
                        x.OldWing!
                            .Trim()
                            .ToUpperInvariant())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();

                /*
                 * Wing + FlatOrShopNumber is used because:
                 *
                 * M Wing + Flat 101
                 * S Wing + Flat 101
                 *
                 * are two different properties.
                 */
                var totalFlatOrShop = group
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(
                            x.OldFlatOrShopNumber))
                    .Select(x => new
                    {
                        Wing = string.IsNullOrWhiteSpace(x.OldWing)
                            ? string.Empty
                            : x.OldWing
                                .Trim()
                                .ToUpperInvariant(),

                        FlatOrShopNumber = x.OldFlatOrShopNumber!
                            .Trim()
                            .ToUpperInvariant()
                    })
                    .Distinct()
                    .Count();

                return new OldSocietyDto
                {
                    OldSocietyName = group
                        .Select(x =>
                            x.OldSocietyName.Trim())
                        .First(),

                    /*
                     * Address is not part of grouping because each
                     * flat can have a different address.
                     */
                    OldAddress = group
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(
                                x.OldAddress))
                        .Select(x =>
                            x.OldAddress!.Trim())
                        .FirstOrDefault(),

                    Wings = string.Join(", ", distinctWings),

                    TotalWing = distinctWings.Count,

                    TotalFlatOrShop = totalFlatOrShop
                };
            })
            .OrderBy(x => x.OldSocietyName)
            .ToList();

        var totalCount = groupedSocieties.Count;

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(
                totalCount / (double)dto.PageSize);

        /*
         * Apply pagination after grouping.
         */
        var paginatedSocieties = groupedSocieties
            .Skip((dto.PageNumber - 1) * dto.PageSize)
            .Take(dto.PageSize)
            .ToList();

        return new OldSocietyResponseDto
        {
            Data = paginatedSocieties,
            Count = totalCount,
            PageNumber = dto.PageNumber,
            PageSize = dto.PageSize,
            TotalPages = totalPages,
            HasNext = dto.PageNumber < totalPages,
            HasPrevious = dto.PageNumber > 1
        };
    }
}
