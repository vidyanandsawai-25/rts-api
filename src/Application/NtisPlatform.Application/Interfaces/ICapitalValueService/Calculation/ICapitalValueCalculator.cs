using NtisPlatform.Application.DTOs.CapitalValue;
using NtisPlatform.Application.Services.CapitalValue.MasterDataProviders;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService.Calculation;

/// <summary>
/// Result of capital value calculation containing both DTOs and factor entity references
/// </summary>
public class CapitalValueCalculationResult
{
    public CapitalValueDto Result { get; set; } = null!;
    public AssessmentYearRangeCVEntity YearRange { get; set; } = null!;
    public RateMasterForCVEntity RateMaster { get; set; } = null!;

    // Factor entities for ID storage
    public FloorFactorCVMasterEntity? FloorFactorEntity { get; set; }
    public AgeFactorCVMasterEntity? AgeFactorEntity { get; set; }
    public NatureFactorCVMasterEntity? NatureFactorEntity { get; set; }
    public UseFactorCVMasterEntity? UseFactorEntity { get; set; }
}

/// <summary>
/// Abstraction for CV calculation engine.
/// Allows for different calculation strategies or mocking in tests.
/// </summary>
public interface ICapitalValueCalculator
{
    /// <summary>
    /// Calculates capital value and taxes for a single property detail.
    /// Returns factor entities for ID storage instead of just values.
    /// </summary>
    CapitalValueCalculationResult Calculate(
        PropertyDetailsEntity propertyDetail,
        MasterDataContext masterData,
        bool hasLift,
        int propertyId,
        int moujaId,
        string csn,
        decimal? ruleAdjustedRate = null);
}
