using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.PropertyNumberDetails;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services;

public class PropertyNumberDetailsService : IPropertyNumberDetailsService
{
    private readonly ILogger<PropertyNumberDetailsService> _logger;
    private readonly IRepository<PropertyEntity, int> _propertyRepository;
    private readonly IRepository<PropertyCategoryEntity, int> _propertyCategoryRepository;
    private readonly IRepository<WardEntity, int> _wardRepository;
   
    public PropertyNumberDetailsService(
        ILogger<PropertyNumberDetailsService> logger,
        IRepository<PropertyEntity, int> propertyRepository,
        IRepository<PropertyCategoryEntity, int> propertyCategoryRepository,
        IRepository<WardEntity, int> wardRepository)
    {
        _logger = logger;
        _propertyRepository = propertyRepository;
        _propertyCategoryRepository = propertyCategoryRepository;
        _wardRepository = wardRepository;
    }

    public async Task<PropertyNumberDetailsDto?> GetAsync(int propertyId,string filter,CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Get Property Number Details. PropertyId: {PropertyId}, Filter: {Filter}",propertyId,filter);

        filter = filter?.Trim() ?? string.Empty;
        var isCurrentFilter = filter.Equals("Current", StringComparison.OrdinalIgnoreCase);
        var isNextFilter = filter.Equals("Next", StringComparison.OrdinalIgnoreCase);
        var isPreviousFilter = filter.Equals("Previous", StringComparison.OrdinalIgnoreCase) || filter.Equals("Privious", StringComparison.OrdinalIgnoreCase);

        // GET CURRENT PROPERTY
        var current = await (
            from p in _propertyRepository.GetQueryable().AsNoTracking()
            join w in _wardRepository.GetQueryable().AsNoTracking()
                on p.WardId equals w.Id
            join c in _propertyCategoryRepository.GetQueryable().AsNoTracking()
                on p.CategoryId equals c.Id
            where p.Id == propertyId
                  && p.IsActive
                  && p.MarkedForDeletion != true
                  && w.IsActive
                  && c.IsActive

            select new
            {
                p.Id,
                p.WardId,
                w.WardNo,
                p.PropertyNo,
                p.PartitionNo,
                p.FlatOrShopNo,
                p.WingDetailId,
                Category = c.PropertyCategoryName
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (current == null)
            return null;

        // CURRENT
        if (isCurrentFilter)
        {
            return new PropertyNumberDetailsDto
            {
                WardId = current.WardId,
                WardNo = current.WardNo ?? string.Empty,
                PropertyNo = current.PropertyNo ?? string.Empty,
                PartitionNo = current.PartitionNo ?? string.Empty,
                Category = current.Category ?? string.Empty,
                FlatOrShopNo = current.FlatOrShopNo ?? string.Empty,
                WingDetailId = current.WingDetailId,
                PropertyType = "Current"
            };
        }

        if (!isNextFilter && !isPreviousFilter)
            return null;

        var currentPropertyNo = current.PropertyNo?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(currentPropertyNo))
            return null;

        var isApartment = current.Category is "Apartment" or "Multi Commercial Apartment";

        // CACHE MAIN PROPERTY LIST This is loaded only when we actually need to move from one PropertyNo to another PropertyNo.
        List<PropertyNumberDetailsDto>? mainPropertiesCache = null;

        async Task<List<PropertyNumberDetailsDto>> GetMainPropertiesAsync()
        {
            if (mainPropertiesCache != null)
                return mainPropertiesCache;

            mainPropertiesCache = await (
                from p in _propertyRepository.GetQueryable().AsNoTracking()
                join w in _wardRepository.GetQueryable().AsNoTracking()
                    on p.WardId equals w.Id
                join c in _propertyCategoryRepository.GetQueryable().AsNoTracking()
                    on p.CategoryId equals c.Id
                where p.WardId == current.WardId
                      && p.IsActive
                      && p.MarkedForDeletion != true
                      && w.IsActive
                      && c.IsActive
                      // Main property only
                      && (p.PartitionNo == null ||
                          p.PartitionNo == "")
                      && p.PropertyNo != null
                      && p.PropertyNo != ""

                select new PropertyNumberDetailsDto
                {
                    WardId = p.WardId,
                    WardNo = w.WardNo ?? string.Empty,
                    PropertyNo = p.PropertyNo ?? string.Empty,
                    PartitionNo = p.PartitionNo ?? string.Empty,
                    Category = c.PropertyCategoryName ?? string.Empty,
                    FlatOrShopNo = p.FlatOrShopNo ?? string.Empty,
                    WingDetailId = p.WingDetailId
                })
                .ToListAsync(cancellationToken);

            return mainPropertiesCache;
        }

        // NATURAL PROPERTY NUMBER PARSER 47   => Number = 47, Suffix = "" 47A,  => Number = 47, Suffix = "A",  47B  => Number = 47, Suffix = "B"
        static (int Number, string Suffix) ParsePropertyNumber(string propertyNo)
        {
            propertyNo = propertyNo.Trim();
            var numericLength = 0;

            while (numericLength < propertyNo.Length && char.IsDigit(propertyNo[numericLength]))
            {
                numericLength++;
            }

            var numericPart = numericLength > 0 ? propertyNo[..numericLength] : string.Empty;
            var suffix = numericLength < propertyNo.Length ? propertyNo[numericLength..].Trim() : string.Empty;
            var number = int.TryParse(numericPart, out var parsedNumber) ? parsedNumber : int.MaxValue;
            return (number, suffix);
        }

        // GET NEXT / PREVIOUS MAIN PROPERTY
        async Task<PropertyNumberDetailsDto?> GetAdjacentMainPropertyAsync(bool getNext)
        {
            var mainProperties = await GetMainPropertiesAsync();
            if (mainProperties.Count == 0)
                return null;

            var orderedProperties = mainProperties
                .Select(x =>
                {
                    var rawPropertyNo = x.PropertyNo.Trim();
                    var parsed = ParsePropertyNumber(rawPropertyNo);
                    return new
                    {
                        Property = x,
                        Number = parsed.Number,
                        Suffix = parsed.Suffix,
                        RawPropertyNo = rawPropertyNo
                    };
                })
                .OrderBy(x => x.Number)
                .ThenBy(x => string.IsNullOrEmpty(x.Suffix) ? 0 : 1)
                .ThenBy(x => x.Suffix, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var currentIndex = orderedProperties.FindIndex(x =>
                string.Equals(x.RawPropertyNo,currentPropertyNo,StringComparison.OrdinalIgnoreCase));

            if (currentIndex < 0)
                return null;

            // NEXT MAIN PROPERTY
            if (getNext)
            {
                var nextIndex = currentIndex + 1;
                if (nextIndex >= orderedProperties.Count)
                    return null;

                var next = orderedProperties[nextIndex].Property;
                next.PropertyType = "Next";
                return next;
            }

            // PREVIOUS MAIN PROPERTY
            var previousIndex = currentIndex - 1;
            if (previousIndex < 0)
                return null;

            var previous = orderedProperties[previousIndex].Property;
            previous.PropertyType = "Previous";
            return previous;
        }

        // APARTMENT
        if (isApartment)
        {
            var apartmentQuery =
                from p in _propertyRepository.GetQueryable().AsNoTracking()
                join w in _wardRepository.GetQueryable().AsNoTracking()
                    on p.WardId equals w.Id
                join c in _propertyCategoryRepository.GetQueryable().AsNoTracking()
                    on p.CategoryId equals c.Id
                where p.WardId == current.WardId
                      && p.PropertyNo == current.PropertyNo
                      && (current.WingDetailId == null ||
                          p.WingDetailId == current.WingDetailId)
                      && p.IsActive
                      && p.MarkedForDeletion != true
                      && w.IsActive
                      && c.IsActive
                      && p.FlatOrShopNo != null
                      && p.FlatOrShopNo != ""
                select new PropertyNumberDetailsDto
                {
                    WardId = p.WardId,
                    WardNo = w.WardNo ?? string.Empty,
                    PropertyNo = p.PropertyNo ?? string.Empty,
                    PartitionNo = p.PartitionNo ?? string.Empty,
                    Category = c.PropertyCategoryName ?? string.Empty,
                    FlatOrShopNo = p.FlatOrShopNo ?? string.Empty,
                    WingDetailId = p.WingDetailId
                };

            // CURRENT IS MAIN APARTMENT PROPERTY
            if (string.IsNullOrWhiteSpace(current.FlatOrShopNo))
            {
                if (isPreviousFilter)
                {
                    return await GetAdjacentMainPropertyAsync(getNext: false);
                }

                // NEXT
                if (isNextFilter)
                {
                    var firstFlat = await apartmentQuery
                        .OrderBy(x => Convert.ToInt32(x.FlatOrShopNo))
                        .ThenBy(x => x.PartitionNo)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (firstFlat != null)
                    {
                        firstFlat.PropertyType = "Next";
                        return firstFlat;
                    }

                    // No flats available in this apartment. Move directly to next PropertyNo.
                    return await GetAdjacentMainPropertyAsync(getNext: true);
                }
                return null;
            }

            // CURRENT IS APARTMENT FLAT
            var currentFlat = int.TryParse(current.FlatOrShopNo,out var flatNo) ? flatNo : 0;

            // APARTMENT NEXT
            if (isNextFilter)
            {
                var nextFlat = await apartmentQuery
                    .Where(x => Convert.ToInt32(x.FlatOrShopNo) > currentFlat)
                    .OrderBy(x => Convert.ToInt32(x.FlatOrShopNo))
                    .ThenBy(x => x.PartitionNo)
                    .FirstOrDefaultAsync(cancellationToken);

                // NEXT FLAT EXISTS
                if (nextFlat != null)
                {
                    nextFlat.PropertyType = "Next";
                    return nextFlat;
                }
                return await GetAdjacentMainPropertyAsync(getNext: true);
            }

            // APARTMENT PREVIOUS
            if (isPreviousFilter)
            {
                var previousFlat = await apartmentQuery
                    .Where(x => Convert.ToInt32(x.FlatOrShopNo) < currentFlat)

                    .OrderByDescending(x => Convert.ToInt32(x.FlatOrShopNo))
                    .ThenByDescending(x => x.PartitionNo)
                    .FirstOrDefaultAsync(cancellationToken);

                // PREVIOUS FLAT EXISTS
                if (previousFlat != null)
                {
                    previousFlat.PropertyType = "Previous";
                    return previousFlat;
                }
                return new PropertyNumberDetailsDto
                {
                    WardId = current.WardId,
                    WardNo = current.WardNo ?? string.Empty,
                    PropertyNo = current.PropertyNo ?? string.Empty,
                    PartitionNo = string.Empty,
                    Category = current.Category ?? string.Empty,
                    FlatOrShopNo = string.Empty,
                    WingDetailId = current.WingDetailId,
                    PropertyType = "Previous"
                };
            }
            return null;
        }

        // NORMAL / INDIVIDUAL
        if (isNextFilter)
        {
            return await GetAdjacentMainPropertyAsync(getNext: true);
        }
        if (isPreviousFilter)
        {
            return await GetAdjacentMainPropertyAsync(getNext: false);
        }
        return null;
    }
}
