using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService.Data;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.CapitalValue.MasterDataProviders;

/// <summary>
/// Provider for loading and caching master data required for capital value calculations.
/// Centralizes all master data loading logic to keep CapitalValueService clean.
/// </summary>
public class CapitalValueMasterDataProvider : ICapitalValueMasterDataProvider
{
    private readonly IRepository<RateMasterForCVEntity, int> _rateRepository;
    private readonly IRepository<NatureFactorCVMasterEntity, int> _natureFactorRepository;
    private readonly IRepository<UseFactorCVMasterEntity, int> _useFactorRepository;
    private readonly IRepository<AgeFactorCVMasterEntity, int> _ageFactorRepository;
    private readonly IRepository<FloorFactorCVMasterEntity, int> _floorFactorRepository;
    private readonly IRepository<AssessmentYearRangeCVEntity, int> _assessmentYearRangeRepository;
    private readonly IRepository<TaxPercentageMasterCVEntity, int> _taxPercentageRepository;
    private readonly IRepository<TaxMasterEntity, int> _taxMasterRepository;
    private readonly IRepository<CSNDetailsEntity, int> _csnDetailsRepository;
    private readonly IRepository<PolicyConfigurationEntity, int> _ruleRepository;
    private readonly ILogger<CapitalValueMasterDataProvider> _logger;

    public CapitalValueMasterDataProvider(
        IRepository<RateMasterForCVEntity, int> rateRepository,
        IRepository<NatureFactorCVMasterEntity, int> natureFactorRepository,
        IRepository<UseFactorCVMasterEntity, int> useFactorRepository,
        IRepository<AgeFactorCVMasterEntity, int> ageFactorRepository,
        IRepository<FloorFactorCVMasterEntity, int> floorFactorRepository,
        IRepository<AssessmentYearRangeCVEntity, int> assessmentYearRangeRepository,
        IRepository<TaxPercentageMasterCVEntity, int> taxPercentageRepository,
        IRepository<TaxMasterEntity, int> taxMasterRepository,
        IRepository<CSNDetailsEntity, int> csnDetailsRepository,
        IRepository<PolicyConfigurationEntity, int> ruleRepository,
        ILogger<CapitalValueMasterDataProvider> logger)
    {
        _rateRepository = rateRepository;
        _natureFactorRepository = natureFactorRepository;
        _useFactorRepository = useFactorRepository;
        _ageFactorRepository = ageFactorRepository;
        _floorFactorRepository = floorFactorRepository;
        _assessmentYearRangeRepository = assessmentYearRangeRepository;
        _taxPercentageRepository = taxPercentageRepository;
        _taxMasterRepository = taxMasterRepository;
        _csnDetailsRepository = csnDetailsRepository;
        _ruleRepository = ruleRepository;
        _logger = logger;
    }

    /// <summary>
    /// Loads all master data required for capital value calculation
    /// </summary>
    public async Task<MasterDataContext> LoadMasterDataAsync(int moujaId, string csn, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Loading master data for MoujaId: {MoujaId}, CSN: {CSN}", moujaId, csn);

        // Load nature factors both as dictionary (for fast lookup) and as list (for ID retrieval)
        var natureFactorEntities = await _natureFactorRepository.GetQueryable()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        var natureFactorsDict = natureFactorEntities
            .GroupBy(x => (x.ConstructionTypeId, x.YearRangeCVId))
            .ToDictionary(g => g.Key, g => (decimal?)g.First().Factor);

        // Load use factors both as dictionary and as list
        var useFactorEntities = await _useFactorRepository.GetQueryable()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        var useFactorsDict = useFactorEntities
            .GroupBy(x => (x.TypeOfUseId, x.YearRangeCVId, x.SubTypeOfUseId))
            .ToDictionary(g => g.Key, g => (decimal?)g.First().Factor);

        var context = new MasterDataContext
        {
            YearRanges = await LoadYearRangesAsync(cancellationToken),
            NatureFactors = natureFactorsDict,
            NatureFactorEntities = natureFactorEntities,
            UseFactors = useFactorsDict,
            UseFactorEntities = useFactorEntities,
            AgeFactors = await LoadAgeFactorsAsync(cancellationToken),
            FloorFactors = await LoadFloorFactorsAsync(cancellationToken),
            RateMasters = await LoadRateMastersAsync(moujaId, csn, cancellationToken),
            TaxData = await LoadTaxDataAsync(cancellationToken),
            TaxTotalHead = await LoadTaxTotalHeadAsync(cancellationToken),
            AssessmentYearRule = await LoadAssessmentYearRuleAsync(cancellationToken)
        };

        _logger.LogDebug("Master data loaded successfully. YearRanges: {YearRanges}, RateMasters: {RateMasters}, TaxData: {TaxData}",
            context.YearRanges.Count, context.RateMasters.Count, context.TaxData.Count);

        return context;
    }

    private async Task<List<AssessmentYearRangeCVEntity>> LoadYearRangesAsync(CancellationToken cancellationToken)
    {
        return await _assessmentYearRangeRepository.GetQueryable()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);
    }

     
    private async Task<List<AgeFactorCVMasterEntity>> LoadAgeFactorsAsync(CancellationToken cancellationToken)
    {
        return await _ageFactorRepository.GetQueryable()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<(int FloorId, int YearRangeCVId), FloorFactorCVMasterEntity>> LoadFloorFactorsAsync(CancellationToken cancellationToken)
    {
        var factors = await _floorFactorRepository.GetQueryable()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);

        return factors
            .GroupBy(x => (x.FloorId, x.YearRangeCVId))
            .ToDictionary(g => g.Key, g => g.First());
    }

    private async Task<List<RateMasterForCVEntity>> LoadRateMastersAsync(int moujaId, string csn, CancellationToken cancellationToken)
    {
        var rateMasters = await (
            from csnDetail in _csnDetailsRepository.GetQueryable().Where(x => x.IsActive)
            join rm in _rateRepository.GetQueryable().Where(x => x.IsActive)
                on csnDetail.RateCVMasterId equals rm.Id
            where csnDetail.MoujaId == moujaId && csnDetail.CSN == csn
            select rm
        ).ToListAsync(cancellationToken);

        if (!rateMasters.Any())
        {
            throw new CSNRateMappingNotFoundException(moujaId, csn);
        }

        return rateMasters;
    }

    private async Task<List<TaxPercentageMasterCVEntity>> LoadTaxDataAsync(CancellationToken cancellationToken)
    {
        return await _taxPercentageRepository.GetQueryable()
            .Include(x => x.TaxMaster)
            .Where(x => x.IsActive && x.TaxMaster!.IsActive && x.TaxMaster.TaxName != "TaxTotal")
            .ToListAsync(cancellationToken);
    }

    private async Task<TaxMasterEntity> LoadTaxTotalHeadAsync(CancellationToken cancellationToken)
    {
        var taxTotal = await _taxMasterRepository.GetQueryable()
            .Where(x => x.IsActive && x.TaxName == "TaxTotal")
            .FirstOrDefaultAsync(cancellationToken);

        if (taxTotal == null)
        {
            throw new MasterDataNotFoundException("TaxTotal", "TaxName = 'TaxTotal'");
        }

        return taxTotal;
    }

    private async Task<PolicyConfigurationEntity?> LoadAssessmentYearRuleAsync(CancellationToken cancellationToken)
    {
        return await _ruleRepository.GetQueryable()
            .Where(x => x.IsActive && x.PolicyCode == "AssessmentYear")
            .FirstOrDefaultAsync(cancellationToken);
    }
}

/// <summary>
/// Container for all master data required for CV calculation
/// </summary>
/// 

public class MasterDataContext
{
    public List<AssessmentYearRangeCVEntity> YearRanges { get; set; } = new();
    public Dictionary<(int ConstructionTypeId, int YearRangeCVId), decimal?> NatureFactors { get; set; } = new();
    public Dictionary<(int TypeOfUseId, int YearRangeCVId, int SubTypeOfUseId), decimal?> UseFactors { get; set; } = new();
    public List<AgeFactorCVMasterEntity> AgeFactors { get; set; } = new();
    public Dictionary<(int FloorId, int YearRangeCVId), FloorFactorCVMasterEntity> FloorFactors { get; set; } = new();
    public List<RateMasterForCVEntity> RateMasters { get; set; } = new();
    public List<TaxPercentageMasterCVEntity> TaxData { get; set; } = new();
    public TaxMasterEntity TaxTotalHead { get; set; } = null!;
    public PolicyConfigurationEntity? AssessmentYearRule { get; set; }

    // Entity lists for ID lookup (needed for storing IDs instead of values)
    public List<NatureFactorCVMasterEntity>? NatureFactorEntities { get; set; }
    public List<UseFactorCVMasterEntity>? UseFactorEntities { get; set; }
}

