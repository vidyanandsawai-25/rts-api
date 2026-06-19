using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService.Data;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.CapitalValue.DataLoader;

/// <summary>
/// Implementation of property data loading.
/// Encapsulates all repository access and EF Core queries.
/// </summary>
public class PropertyDataLoader : IPropertyDataLoader
{
    private readonly IRepository<PropertyEntity, int> _propertyRepository;
    private readonly IRepository<PropertyDetailsEntity, int> _propertyDetailsRepository;
    private readonly IRepository<FlagMasterEntity, int> _flagRepository;
    private readonly IRepository<YearMasterEntity, int> _yearMasterRepository;
    private readonly ILogger<PropertyDataLoader> _logger;

    public PropertyDataLoader(
        IRepository<PropertyEntity, int> propertyRepository,
        IRepository<PropertyDetailsEntity, int> propertyDetailsRepository,
        IRepository<FlagMasterEntity, int> flagRepository,
        IRepository<YearMasterEntity, int> yearMasterRepository,
        ILogger<PropertyDataLoader> logger)
    {
        _propertyRepository = propertyRepository;
        _propertyDetailsRepository = propertyDetailsRepository;
        _flagRepository = flagRepository;
        _yearMasterRepository = yearMasterRepository;
        _logger = logger;
    }

    public async Task<PropertyEntity> LoadPropertyAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        var property = await _propertyRepository.GetQueryable()
            .Where(x => x.Id == propertyId && !x.MarkedForDeletion && x.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (property == null)
            throw new PropertyDataNotFoundException(propertyId);

        if (!property.MoujaId.HasValue)
            throw new PropertyDataNotFoundException(propertyId, "MoujaId is required");

        return property;
    }

    public async Task<List<PropertyDetailsEntity>> LoadPropertyDetailsAsync( int propertyId, int? propertyDetailsId = null, CancellationToken cancellationToken = default)
    {
        IQueryable<PropertyDetailsEntity> query = _propertyDetailsRepository.GetQueryable()
            .Where(x => x.IsActive && !x.MarkedForDeletion && x.PropertyId == propertyId)
            .Include(x => x.Floor)
            .Include(x => x.SubFloor)
            .Include(x => x.ConstructionType)
            .Include(x => x.TypeOfUse!)
                .ThenInclude(x => x.TypeOfUseGroupCV)
            .Include(x => x.SubTypeOfUse)
            .OrderBy(x => x.Id); // Ensure consistent ordering by ID

        if (propertyDetailsId.HasValue && propertyDetailsId.Value != 0)
            query = query.Where(x => x.Id == propertyDetailsId.Value);

        var details = await query.ToListAsync(cancellationToken);

        //if (!details.Any())
        //    throw new PropertyDetailsNotFoundException(propertyId, propertyDetailsId ?? 0);

        return details;
    }

    public async Task<bool> HasActiveDetailsAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _propertyDetailsRepository.GetQueryable()
            .Where(x => x.PropertyId == propertyId && !x.MarkedForDeletion && x.IsActive)
            .AnyAsync(cancellationToken);
    }

    public async Task<bool> LoadLiftFlagAsync(int propertyId, CancellationToken cancellationToken = default)
    {
        return await _flagRepository.GetQueryable()
            .Where(x => x.PropertyId == propertyId && x.IsActive)
            .Select(x => x.Lift)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<YearMasterEntity> LoadFinanceYearAsync(int? specificYear = null, CancellationToken cancellationToken = default)
    {
        var query = _yearMasterRepository.GetQueryable().Where(x => x.IsActive);

        if (specificYear.HasValue)
            query = query.Where(x => x.Year == specificYear.Value);

        var financeYear = await query.FirstOrDefaultAsync(cancellationToken);

        if (financeYear == null)
            throw new FinanceYearNotFoundException(specificYear);

        return financeYear;
    }



}
