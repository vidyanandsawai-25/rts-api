using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.LockUnlock;
using NtisPlatform.Application.Helpers;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

/// <summary>
/// Application service for reading Excel property lists and matching them against Property records and PropertyScreenLocks.
/// Employs CommonDetails architecture patterns (ExcelImportHelper parsing, hardcoded column validation, clean data flows).
/// </summary>
public class PropertyLockExcelService : IPropertyLockExcelService
{
    private readonly IRepository<WardEntity> _wardRepo;
    private readonly IRepository<ZoneEntity> _zoneRepo;
    private readonly IRepository<PropertyEntity> _propertyRepo;
    private readonly IRepository<PropertyScreenLockEntity> _propertyScreenLockRepo;
    private readonly IRepository<ScreenMasterEntity> _screenMasterRepo;
    private readonly ILogger<PropertyLockExcelService> _logger;

    public PropertyLockExcelService(
        IRepository<WardEntity> wardRepo,
        IRepository<ZoneEntity> zoneRepo,
        IRepository<PropertyEntity> propertyRepo,
        IRepository<PropertyScreenLockEntity> propertyScreenLockRepo,
        IRepository<ScreenMasterEntity> screenMasterRepo,
        ILogger<PropertyLockExcelService> logger)
    {
        _wardRepo = wardRepo;
        _zoneRepo = zoneRepo;
        _propertyRepo = propertyRepo;
        _propertyScreenLockRepo = propertyScreenLockRepo;
        _screenMasterRepo = screenMasterRepo;
        _logger = logger;
    }

    public async Task<PropertyLockExcelPagedResultDto> GetPropertyLocksByExcelFileAsync(
        Stream fileStream, int pageNumber, int pageSize, string? searchTerm = null, CancellationToken ct = default)
    {
        if (fileStream == null || fileStream.Length == 0)
        {
            throw new ArgumentException("Please upload a valid Excel file.");
        }

        var (headers, excelRows) = ExcelImportHelper.Read(fileStream);

        var headerSet = new HashSet<string>(headers.Where(h => !string.IsNullOrWhiteSpace(h)), StringComparer.OrdinalIgnoreCase);

        // Exact 4 fixed column names
        var requiredColumns = new[] { "ZoneNo", "WardNo", "PropertyNo", "PartitionNo" };
        var missingColumns = requiredColumns.Where(col => !headerSet.Contains(col)).ToList();

        if (missingColumns.Count > 0)
        {
            throw new ArgumentException($"Missing required column(s): {string.Join(", ", missingColumns)}.");
        }

        var rows = new List<ExcelPropertyRow>();
        foreach (var row in excelRows)
        {
            var zoneVal = row.Cells.GetValueOrDefault("ZoneNo")?.Trim();
            var wardVal = row.Cells.GetValueOrDefault("WardNo")?.Trim();
            var propVal = row.Cells.GetValueOrDefault("PropertyNo")?.Trim();
            var partVal = row.Cells.GetValueOrDefault("PartitionNo")?.Trim();

            if (string.IsNullOrWhiteSpace(zoneVal) || string.IsNullOrWhiteSpace(wardVal) || string.IsNullOrWhiteSpace(propVal))
                continue;

            rows.Add(new ExcelPropertyRow
            {
                ZoneNo = zoneVal,
                WardNo = wardVal,
                PropertyNo = propVal,
                PartitionNo = string.IsNullOrWhiteSpace(partVal) ? null : partVal
            });
        }

        var normalizedPageNumber = pageNumber <= 0 ? 1 : pageNumber;
        var normalizedPageSize = pageSize == 0 ? 10 : pageSize;
        var cleanSearchTerm = string.Equals(searchTerm?.Trim(), "string", StringComparison.OrdinalIgnoreCase) ? null : searchTerm?.Trim();

        var request = new SearchByExcelRequestDto
        {
            Rows = rows,
            PageNumber = normalizedPageNumber,
            PageSize = normalizedPageSize,
            SearchTerm = cleanSearchTerm
        };

        return await GetPropertyLocksByExcelRowsAsync(request, ct);
    }

    public async Task<PropertyLockExcelPagedResultDto> GetPropertyLocksByExcelRowsAsync(
        SearchByExcelRequestDto request, CancellationToken ct = default)
    {
        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize == 0 ? 10 : request.PageSize;

        if (request.Rows == null || request.Rows.Count == 0)
        {
            return new PropertyLockExcelPagedResultDto(new List<PropertyLockRowDto>(), 0, pageNumber, pageSize, 0);
        }

        var cleanRows = request.Rows
            .Where(r => !string.IsNullOrWhiteSpace(r.ZoneNo) && !string.IsNullOrWhiteSpace(r.WardNo) && !string.IsNullOrWhiteSpace(r.PropertyNo))
            .Select(r => new
            {
                ZoneNo = r.ZoneNo.Trim(),
                WardNo = r.WardNo.Trim(),
                PropertyNo = r.PropertyNo.Trim(),
                PartitionNo = string.IsNullOrWhiteSpace(r.PartitionNo) ? null : r.PartitionNo.Trim()
            })
            .ToList();

        if (cleanRows.Count == 0)
        {
            return new PropertyLockExcelPagedResultDto(new List<PropertyLockRowDto>(), 0, pageNumber, pageSize, 0);
        }

        var wardLookups = await (from w in _wardRepo.GetQueryable().AsNoTracking()
                                 join z in _zoneRepo.GetQueryable().AsNoTracking() on w.ZoneId equals z.Id
                                 where w.IsActive && z.IsActive
                                 select new { WardId = w.Id, ZoneNo = z.ZoneNo, WardNo = w.WardNo })
                                 .ToListAsync(ct);

        var wardByZoneAndNo = wardLookups
            .Where(w => !string.IsNullOrWhiteSpace(w.ZoneNo) && !string.IsNullOrWhiteSpace(w.WardNo))
            .GroupBy(w => $"{w.ZoneNo.Trim().ToUpperInvariant()}_{w.WardNo.Trim().ToUpperInvariant()}")
            .ToDictionary(g => g.Key, g => g.First());

        var wardByNoOnly = wardLookups
            .Where(w => !string.IsNullOrWhiteSpace(w.WardNo))
            .GroupBy(w => w.WardNo.Trim().ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        var wardIdPropertySpecs = cleanRows
            .Select(r =>
            {
                var key = $"{r.ZoneNo.ToUpperInvariant()}_{r.WardNo.ToUpperInvariant()}";
                var wardInfo = wardByZoneAndNo.GetValueOrDefault(key) ?? wardByNoOnly.GetValueOrDefault(r.WardNo.ToUpperInvariant());
                return new
                {
                    WardId = wardInfo?.WardId ?? 0,
                    ZoneNo = wardInfo?.ZoneNo ?? r.ZoneNo,
                    WardNo = wardInfo?.WardNo ?? r.WardNo,
                    r.PropertyNo,
                    r.PartitionNo
                };
            })
            .ToList();

        if (wardIdPropertySpecs.Count == 0)
        {
            return new PropertyLockExcelPagedResultDto(new List<PropertyLockRowDto>(), 0, pageNumber, pageSize, 0);
        }

        // Deduplicate rows while preserving first-seen Excel file order
        var totalRawCount = wardIdPropertySpecs.Count;
        var seenSpecs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var distinctSpecs = wardIdPropertySpecs
            .Where(s => seenSpecs.Add($"{s.ZoneNo.Trim()}|{s.WardNo.Trim()}|{s.PropertyNo.Trim()}|{(s.PartitionNo ?? string.Empty).Trim()}"))
            .ToList();
        var duplicateCount = totalRawCount - distinctSpecs.Count;

        // Apply SearchTerm filter in memory over the Excel specs
        var searchFilter = request.SearchTerm?.Trim();
        var filteredSpecs = distinctSpecs;
        if (!string.IsNullOrWhiteSpace(searchFilter) && !string.Equals(searchFilter, "string", StringComparison.OrdinalIgnoreCase))
        {
            filteredSpecs = filteredSpecs.Where(p =>
            {
                var propertyDisplay = string.IsNullOrEmpty(p.PartitionNo)
                    ? $"{p.WardNo}-{p.PropertyNo}"
                    : $"{p.WardNo}-{p.PropertyNo}-{p.PartitionNo}";

                return p.WardNo.Contains(searchFilter, StringComparison.OrdinalIgnoreCase) ||
                       p.PropertyNo.Contains(searchFilter, StringComparison.OrdinalIgnoreCase) ||
                       (p.PartitionNo != null && p.PartitionNo.Contains(searchFilter, StringComparison.OrdinalIgnoreCase)) ||
                       p.ZoneNo.Contains(searchFilter, StringComparison.OrdinalIgnoreCase) ||
                       propertyDisplay.Contains(searchFilter, StringComparison.OrdinalIgnoreCase);
            }).ToList();
        }

        var totalCount = filteredSpecs.Count;
        if (totalCount == 0)
        {
            return new PropertyLockExcelPagedResultDto(new List<PropertyLockRowDto>(), 0, pageNumber, pageSize, duplicateCount);
        }

        var effectivePageSize = pageSize == -1 ? totalCount : (pageSize <= 0 ? 10 : pageSize);
        var effectivePageNumber = pageNumber <= 0 ? 1 : pageNumber;
        var skip = pageSize == -1 ? 0 : (effectivePageNumber - 1) * effectivePageSize;

        // Preserve exact Excel file order
        var pagedSpecs = (pageSize == -1 ? filteredSpecs : filteredSpecs.Skip(skip).Take(effectivePageSize)).ToList();

        if (pagedSpecs.Count == 0)
        {
            return new PropertyLockExcelPagedResultDto(new List<PropertyLockRowDto>(), totalCount, effectivePageNumber, effectivePageSize, duplicateCount);
        }

        // Query PropertyEntity for the paged specs
        var pagedWardIds = pagedSpecs.Where(x => x.WardId > 0).Select(x => x.WardId).Distinct().ToList();
        var pagedPropertyNos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var spec in pagedSpecs)
        {
            if (string.IsNullOrWhiteSpace(spec.PropertyNo)) continue;
            var trimmed = spec.PropertyNo.Trim();
            pagedPropertyNos.Add(trimmed);

            var parts = trimmed.Split('-');
            if (parts.Length > 1)
            {
                pagedPropertyNos.Add(parts[^1].Trim());
                if (parts.Length > 2)
                {
                    pagedPropertyNos.Add($"{parts[^2].Trim()}-{parts[^1].Trim()}");
                }
            }
        }

        var propNosList = pagedPropertyNos.ToList();

        var candidateQuery = _propertyRepo.GetQueryable()
            .AsNoTracking()
            .Where(pm => pm.PropertyNo != null && propNosList.Contains(pm.PropertyNo) && pm.IsActive && !pm.MarkedForDeletion)
            .Select(pm => new { pm.Id, pm.WardId, pm.PropertyNo, pm.PartitionNo });

        var candidates = pagedWardIds.Count > 0
            ? await candidateQuery.Where(pm => pagedWardIds.Contains(pm.WardId)).ToListAsync(ct)
            : await candidateQuery.ToListAsync(ct);

        if (candidates.Count == 0 && pagedWardIds.Count > 0)
        {
            candidates = await candidateQuery.ToListAsync(ct);
        }

        // Fast candidate lookup
        var candidatesByWardAndProp = candidates
            .Where(c => !string.IsNullOrWhiteSpace(c.PropertyNo))
            .GroupBy(c => (c.WardId, c.PropertyNo!.Trim().ToUpperInvariant()))
            .ToDictionary(g => g.Key, g => g.ToList());

        var matchedItems = new List<PropertyLockRowDto>();
        var propertyIds = new List<int>();

        foreach (var spec in pagedSpecs)
        {
            var specPropTrimmed = spec.PropertyNo.Trim();
            var specPropUpper = specPropTrimmed.ToUpperInvariant();

            int matchedPropertyId = 0;
            string matchedPropNo = spec.PropertyNo;
            string matchedPartNo = spec.PartitionNo ?? string.Empty;

            candidatesByWardAndProp.TryGetValue((spec.WardId, specPropUpper), out var candidateBucket);

            var availableCandidates = (spec.WardId > 0 && candidateBucket != null)
                ? candidateBucket
                : candidates.Where(c =>
                    !string.IsNullOrWhiteSpace(c.PropertyNo) &&
                    (string.Equals(c.PropertyNo.Trim(), specPropTrimmed, StringComparison.OrdinalIgnoreCase) ||
                     (specPropTrimmed.Contains('-') && string.Equals(c.PropertyNo.Trim(), specPropTrimmed.Split('-')[^1].Trim(), StringComparison.OrdinalIgnoreCase)))
                  ).ToList();

            if (availableCandidates.Count > 0)
            {
                var isSpecPartitionEmpty = string.IsNullOrWhiteSpace(spec.PartitionNo) || spec.PartitionNo.Trim() == "0" || spec.PartitionNo.Trim() == "-";
                var matchedCandidate = availableCandidates.FirstOrDefault(pm =>
                {
                    if (isSpecPartitionEmpty)
                    {
                        return string.IsNullOrWhiteSpace(pm.PartitionNo) || pm.PartitionNo.Trim() == "0" || pm.PartitionNo.Trim() == "-";
                    }
                    else
                    {
                        return string.Equals(pm.PartitionNo?.Trim(), spec.PartitionNo?.Trim(), StringComparison.OrdinalIgnoreCase);
                    }
                }) ?? availableCandidates.First();

                if (matchedCandidate != null)
                {
                    matchedPropertyId = matchedCandidate.Id;
                    matchedPropNo = matchedCandidate.PropertyNo ?? spec.PropertyNo;
                    matchedPartNo = matchedCandidate.PartitionNo ?? spec.PartitionNo ?? string.Empty;
                    propertyIds.Add(matchedCandidate.Id);
                }
            }

            matchedItems.Add(new PropertyLockRowDto
            {
                PropertyId = matchedPropertyId,
                WardId = spec.WardId,
                WardNo = spec.WardNo,
                PropertyNo = matchedPropNo,
                PartitionNo = matchedPartNo,
                Property = string.IsNullOrEmpty(matchedPartNo)
                    ? $"{spec.WardNo}-{matchedPropNo}"
                    : $"{spec.WardNo}-{matchedPropNo}-{matchedPartNo}",
                IsLocked = false,
                LockedScreens = new List<LockableScreenDto>()
            });
        }

        // Fetch locks only for matched paged properties
        if (propertyIds.Count > 0)
        {
            var locksByProperty = await GetLockedScreensByPropertyAsync(propertyIds, ct);
            foreach (var item in matchedItems)
            {
                if (item.PropertyId > 0 && locksByProperty.TryGetValue(item.PropertyId, out var screens) && screens.Count > 0)
                {
                    item.IsLocked = true;
                    item.LockedScreens = screens;
                }
            }
        }

        return new PropertyLockExcelPagedResultDto(matchedItems, totalCount, effectivePageNumber, effectivePageSize, duplicateCount);
    }

    private async Task<Dictionary<int, List<LockableScreenDto>>> GetLockedScreensByPropertyAsync(List<int> propertyIds, CancellationToken ct)
    {
        var locks = await _propertyScreenLockRepo.GetQueryable()
            .AsNoTracking()
            .Where(l => propertyIds.Contains(l.PropertyId) && l.IsLocked && !l.MarkedForDeletion)
            .Join(_screenMasterRepo.GetQueryable().AsNoTracking(), l => l.LockableScreenId, s => s.Id,
                (l, s) => new
                {
                    l.PropertyId,
                    Screen = new LockableScreenDto
                    {
                        Id = s.Id,
                        ScreenCode = s.ScreenCode ?? string.Empty,
                        ScreenName = s.ScreenName ?? string.Empty,
                        ScreenNameLocal = s.ScreenNameLocal,
                        DisplayOrder = s.DisplayOrder,
                    },
                })
            .ToListAsync(ct);

        return locks
            .GroupBy(x => x.PropertyId)
            .ToDictionary(g => g.Key, g => g
                .Select(x => x.Screen)
                .OrderBy(s => s.DisplayOrder ?? int.MaxValue)
                .ThenBy(s => s.ScreenName)
                .ToList());
    }
}
