using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NtisPlatform.Application.DTOs.Property.ApartmentQC;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Constants;
using NtisPlatform.Infrastructure.Data;

namespace NtisPlatform.Infrastructure.Repositories;

/// <summary>
/// Data-access for the ApartmentQC Search typeahead. Mirrors PropertySearchRepository's
/// per-ward in-memory cache: the first request for a ward pays one DB round trip that resolves
/// every property's Society/Unit/IndividualProperty category up front; every filtered keystroke
/// against an already-cached ward is answered entirely in memory.
/// </summary>
public sealed class ApartmentQcSearchRepository : IApartmentQcSearchRepository
{
    private const int SuggestionCacheDurationMinutes = 5;

    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;

    public ApartmentQcSearchRepository(ApplicationDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    private async Task<List<ApartmentQcSearchSuggestionDto>> GetWardLookupAsync(int wardId, CancellationToken cancellationToken)
    {
        var cacheKey = $"ApartmentQcSearch_Ward_{wardId}";

        if (_cache.TryGetValue(cacheKey, out List<ApartmentQcSearchSuggestionDto>? cached) && cached != null)
            return cached;

        var ward = await _context.WardMaster.AsNoTracking()
            .Where(w => w.Id == wardId)
            .Select(w => new { w.WardNo, w.ZoneId, ZoneNo = w.Zone != null ? w.Zone.ZoneNo : null })
            .FirstOrDefaultAsync(cancellationToken);

        // One query: every active property in the ward, left-joined against PropertyCategoryMaster
        // (to gate Society/Unit/Amenity classification to Apartment-category properties only -- a
        // non-apartment property is always IndividualProperty, even if it happens to carry a stray
        // Society/Wing link from legacy data), against PropertyTypeMaster (to detect Amenity via
        // PartType, a normalized classification independent of whether the description is in
        // English or Marathi), and against SocietyDetailsMast to find out whether this property IS
        // a society's representative property.
        var joined = await (
            from p in _context.PropertyMast.AsNoTracking()
            where p.WardId == wardId && p.IsActive && !p.MarkedForDeletion
            join cat in _context.PropertyCategoryMaster.AsNoTracking() on p.CategoryId equals cat.Id into catJoin
            from category in catJoin.DefaultIfEmpty()
            join ptm in _context.PropertyTypeMasters.AsNoTracking() on p.PropertyTypeId equals (int?)ptm.Id into ptmJoin
            from propertyType in ptmJoin.DefaultIfEmpty()
            join s in _context.SocietyDetailsMast.AsNoTracking().Where(s => s.IsActive && !s.MarkedForDeletion)
                on p.Id equals s.PropertyId into societyJoin
            from society in societyJoin.DefaultIfEmpty()
            select new
            {
                p.Id,
                p.PropertyNo,
                p.PartitionNo,
                p.UPICId,
                p.WingDetailId,
                CategoryName = category != null ? category.PropertyCategoryName : null,
                PropertyTypePartType = propertyType != null ? propertyType.PartType : null,
                RepresentativeSocietyDetailId = society != null ? (int?)society.Id : null,
                RepresentativeSocietyName = society != null ? society.SocietyName : null
            }
        ).ToListAsync(cancellationToken);

        bool IsApartmentCategory(string? categoryName) =>
            categoryName != null && PropertyCategoryConstants.ApartmentCategoryNames.Contains(categoryName);

        // Resolve each property's category up front. PropertyCategoryMaster gates everything below,
        // as a strict partition: a non-Apartment-category property is always IndividualProperty,
        // full stop, even if it happens to carry a stray Society/Wing link from legacy data. Within
        // an Apartment-category property, PartitionNo is the codebase-wide "this is a unit, not the
        // building's representative property" signal (see PropertySearchRepository/PropertyRepository/
        // every ApartmentQC workflow-stage repository's structure-vs-unit split) -- a unit whose
        // PropertyType is classified Amenity (PropertyTypeMaster.PartType = "Amenity") is its own
        // category rather than a generic Unit. An Apartment-category property with no PartitionNo is
        // Society even if no SocietyDetailsMast row points to it yet (details just haven't been
        // entered for it) -- e.g. a building's common-area/society-office property.
        var categorized = joined.Select(x => new
        {
            x.Id,
            x.PropertyNo,
            x.PartitionNo,
            x.UPICId,
            x.WingDetailId,
            x.RepresentativeSocietyDetailId,
            x.RepresentativeSocietyName,
            Category = !IsApartmentCategory(x.CategoryName)
                ? ApartmentQcSearchCategory.IndividualProperty
                : string.IsNullOrWhiteSpace(x.PartitionNo)
                    ? ApartmentQcSearchCategory.Society
                    : string.Equals(x.PropertyTypePartType, "Amenity", StringComparison.OrdinalIgnoreCase)
                        ? ApartmentQcSearchCategory.Amenity
                        : ApartmentQcSearchCategory.Unit
        }).ToList();

        // Resolve the owning society for every Unit/Amenity row that has a WingDetailId, via
        // WingDetailsMast.SocietyDetailsMastId -- a unit must carry SocietyDetailId+WingDetailId+
        // PropertyId together, not just WingDetailId, so the frontend has the full linkage without
        // a second lookup. Not every unit/amenity has a WingDetailId (a flat not yet linked to a
        // wing, or an amenity that isn't wing-specific), so this is a best-effort supplement to the
        // direct RepresentativeSocietyDetailId match, not a requirement.
        var unitWingIds = categorized
            .Where(x => (x.Category == ApartmentQcSearchCategory.Unit || x.Category == ApartmentQcSearchCategory.Amenity) && x.WingDetailId.HasValue)
            .Select(x => x.WingDetailId!.Value)
            .Distinct()
            .ToList();

        var societyByWingId = unitWingIds.Count == 0
            ? new Dictionary<int, (int SocietyDetailId, string? SocietyName)>()
            : await (
                from w in _context.WingDetailsMast.AsNoTracking()
                where unitWingIds.Contains(w.Id) && w.IsActive && !w.MarkedForDeletion
                join s in _context.SocietyDetailsMast.AsNoTracking().Where(s => s.IsActive && !s.MarkedForDeletion)
                    on w.SocietyDetailsMastId equals s.Id
                select new { WingId = w.Id, SocietyDetailId = s.Id, SocietyName = (string?)s.SocietyName }
            ).ToDictionaryAsync(x => x.WingId, x => (x.SocietyDetailId, x.SocietyName), cancellationToken);

        // Batch-fetch every wing for every society found in this ward, in one query -- WingDetailsMast
        // has no PropertyId of its own, so wings can only be resolved via their SocietyDetailsMastId.
        // A Society row may have no SocietyDetailsMast link yet (details not entered), so this must
        // filter on HasValue rather than assume every Society row carries one.
        var societyIds = categorized
            .Where(x => x.Category == ApartmentQcSearchCategory.Society && x.RepresentativeSocietyDetailId.HasValue)
            .Select(x => x.RepresentativeSocietyDetailId!.Value)
            .Distinct()
            .ToList();
        var wingsBySociety = societyIds.Count == 0
            ? new Dictionary<int, List<ApartmentQcSearchWingSummaryDto>>()
            : (await _context.WingDetailsMast.AsNoTracking()
                .Where(w => societyIds.Contains(w.SocietyDetailsMastId) && w.IsActive && !w.MarkedForDeletion)
                .Select(w => new { w.SocietyDetailsMastId, w.Id, w.WingName })
                .ToListAsync(cancellationToken))
                .GroupBy(w => w.SocietyDetailsMastId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(w => new ApartmentQcSearchWingSummaryDto
                    {
                        SocietyDetailId = g.Key,
                        WingDetailId = w.Id,
                        WingName = w.WingName
                    }).ToList());

        // Create a lookup map of PropertyNo -> (SocietyDetailId, SocietyName) for all Society-category properties in this ward
        var societyByPropertyNo = categorized
            .Where(x => x.RepresentativeSocietyDetailId.HasValue && !string.IsNullOrWhiteSpace(x.PropertyNo))
            .GroupBy(x => x.PropertyNo!)
            .ToDictionary(
                g => g.Key,
                g => (SocietyDetailId: g.First().RepresentativeSocietyDetailId!.Value, SocietyName: g.First().RepresentativeSocietyName)
            );

        string CategoryLabel(ApartmentQcSearchCategory category) => category switch
        {
            ApartmentQcSearchCategory.Society => "apartment society property",
            ApartmentQcSearchCategory.Unit => "apartment society unit property",
            ApartmentQcSearchCategory.Amenity => "apartment society amenity property",
            _ => "individual property"
        };

        var rows = categorized.Select(x =>
        {
            // Society, Unit, and Amenity resolve their society via:
            // 1. Direct match on this property row (RepresentativeSocietyDetailId)
            // 2. Wing linkage via WingDetailId (societyByWingId)
            // 3. Fallback to matching by PropertyNo against the ward's representative society property (societyByPropertyNo)
            var societyDetailId = x.RepresentativeSocietyDetailId;
            var societyName = x.RepresentativeSocietyName;

            if (x.WingDetailId.HasValue && societyByWingId.TryGetValue(x.WingDetailId.Value, out var owningSocietyByWing))
            {
                societyDetailId = owningSocietyByWing.SocietyDetailId;
                societyName = owningSocietyByWing.SocietyName;
            }

            if (!societyDetailId.HasValue && !string.IsNullOrWhiteSpace(x.PropertyNo) && societyByPropertyNo.TryGetValue(x.PropertyNo, out var owningSocietyByProp))
            {
                societyDetailId = owningSocietyByProp.SocietyDetailId;
                societyName = owningSocietyByProp.SocietyName;
            }

            return new ApartmentQcSearchSuggestionDto
            {
                PropertyId = x.Id,
                ZoneId = ward?.ZoneId ?? 0,
                ZoneNo = ward?.ZoneNo,
                WardId = wardId,
                WardNo = ward?.WardNo,
                PropertyNo = x.PropertyNo,
                PartitionNo = x.PartitionNo,
                UpicId = x.UPICId,
                DisplayLabel = x.PartitionNo == null || x.PartitionNo == string.Empty
                    ? (x.PropertyNo ?? string.Empty)
                    : $"{x.PropertyNo}-{x.PartitionNo}",
                Category = x.Category,
                CategoryLabel = CategoryLabel(x.Category),
                SocietyDetailId = x.Category == ApartmentQcSearchCategory.IndividualProperty ? null : societyDetailId,
                SocietyName = x.Category == ApartmentQcSearchCategory.IndividualProperty ? null : societyName,
                Wings = x.Category == ApartmentQcSearchCategory.Society
                    ? (x.RepresentativeSocietyDetailId.HasValue && wingsBySociety.TryGetValue(x.RepresentativeSocietyDetailId.Value, out var wings)
                        ? wings
                        : new List<ApartmentQcSearchWingSummaryDto>())
                    : null,
                WingDetailId = x.Category == ApartmentQcSearchCategory.Unit || x.Category == ApartmentQcSearchCategory.Amenity
                    ? x.WingDetailId
                    : null
            };
        }).ToList();

        _cache.Set(cacheKey, rows, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(SuggestionCacheDurationMinutes),
            Size = 1
        });

        return rows;
    }

    public async Task<List<ApartmentQcSearchSuggestionDto>> GetSuggestionsAsync(
        int wardId, string? propertyNo, string? partitionNo, int maxResults, CancellationToken cancellationToken = default)
    {
        var candidates = await GetWardLookupAsync(wardId, cancellationToken);

        IEnumerable<ApartmentQcSearchSuggestionDto> results = candidates;

        if (!string.IsNullOrWhiteSpace(propertyNo))
            results = results.Where(x => x.PropertyNo != null && x.PropertyNo.Contains(propertyNo, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(partitionNo))
            results = results.Where(x => x.PartitionNo != null && x.PartitionNo.Contains(partitionNo, StringComparison.OrdinalIgnoreCase));

        return results.Take(maxResults).ToList();
    }
}
