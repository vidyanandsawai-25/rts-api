using NtisPlatform.Core.Entities;

namespace NtisPlatform.Application.Interfaces.TaxEngine;

/// <summary>
/// Applies the RV rule engine to a single property detail and returns the
/// adjusted rate, or <c>null</c> when no rules matched or rule execution failed.
/// Extracted from <c>RateableValueService</c> to satisfy the Single Responsibility Principle.
/// </summary>
public interface IRVRuleApplicator
{
    /// <summary>
    /// Runs all applicable RV rules for the given detail and returns the final
    /// adjusted rate.  Returns <c>null</c> when the rule engine is skipped (e.g. invalid
    /// floor/type-of-use), no rules matched, or an exception occurred (fail-open).
    /// </summary>
    Task<decimal?> GetAdjustedRateAsync(
        PropertyDetailsEntity detail,
        TypeOfUseEntity detailTypeOfUse,
        PropertyEntity property,
        PropertyAssessmentEntity? propertyAssessment,
        bool hasLift,
        int constructionYearValue,
        int financeYear,
        int yearRangeRVId,
        decimal masterRatePerUnit);
}
