using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.TaxEngine;

/// <summary>
/// Provides read-only master data required by the Rateable Value calculation pipeline.
/// Abstracted as an interface to allow mocking in unit tests and future alternative
/// data sources (e.g. cached, read-replica).
/// </summary>
public interface ITaxMasterDataService
{
    Task<List<TypeOfUseEntity>> GetActiveTypeOfUsesAsync();
    Task<List<SubTypeOfUseEntity>> GetActiveSubTypeOfUsesAsync();
    Task<List<PropertyCategoryEntity>> GetActivePropertyCategoriesAsync();
    Task<List<FloorEntity>> GetActiveFloorsAsync();
    Task<List<SubFloorEntity>> GetActiveSubFloorsAsync();
    Task<List<ConstructionTypeEntity>> GetActiveConstructionTypesAsync();

    /// <returns>The RateSectionId whose ward mapping covers <paramref name="wardId"/>, or 0 if none found.</returns>
    Task<int> GetRateSectionIdForWardAsync(int? wardId);
    Task<List<RateEntity>> GetRatesForSectionAsync(int rateSectionId);

    Task<List<DepreciationMasterEntity>> GetActiveDepreciationsAsync();
    Task<List<AssessmentYearRangeEntity>> GetActiveYearRangesAsync();
    Task<List<TaxMasterEntity>> GetActiveTaxesAsync();
    Task<List<TaxPercentageMasterRVEntity>> GetActiveTaxPercentagesAsync();
    Task<List<EducationTaxMasterEntity>> GetActiveEducationTaxSlabsAsync();
    Task<List<EmploymentTaxMasterEntity>> GetActiveEmploymentTaxSlabsAsync();
}
