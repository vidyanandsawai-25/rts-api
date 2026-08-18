using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleDateCondition;

namespace NtisPlatform.Application.Constants.RetrospectiveTax;

/// <summary>
/// Static option list for the "Compare evidence dates" dropdown (RetrospectiveRuleDateCondition.
/// ComparatorCode). Kept in one place so the API contract for this field (code sent to the
/// server + label shown to the user + which extra input the form needs) can't drift out of sync
/// between backend and frontend.
/// </summary>
public static class RetrospectiveRuleDateConditionOptions
{
    public static IReadOnlyList<RetrospectiveRuleDateConditionOptionDto> ComparatorCodes { get; } = new[]
    {
        new RetrospectiveRuleDateConditionOptionDto { Code = "NONE", Label = "No date comparison", RequiredInput = "NONE" },
        new RetrospectiveRuleDateConditionOptionDto { Code = "ELECTRICITY_BEFORE_CC", Label = "Electricity date before CC date", RequiredInput = "NONE" },
        new RetrospectiveRuleDateConditionOptionDto { Code = "ELECTRICITY_AFTER_CC", Label = "Electricity date after CC date", RequiredInput = "NONE" },
        new RetrospectiveRuleDateConditionOptionDto { Code = "ELECTRICITY_BEFORE_CUTOFF", Label = "Electricity date before cutoff date", RequiredInput = "CUTOFF_DATE" },
        new RetrospectiveRuleDateConditionOptionDto { Code = "ELECTRICITY_AFTER_CUTOFF", Label = "Electricity date after cutoff date", RequiredInput = "CUTOFF_DATE" },
        new RetrospectiveRuleDateConditionOptionDto { Code = "OC_OLDER_THAN_ALLOWED_PERIOD", Label = "OC date older than allowed period", RequiredInput = "YEARS" },
        new RetrospectiveRuleDateConditionOptionDto { Code = "OC_WITHIN_ALLOWED_PERIOD", Label = "OC date within allowed period", RequiredInput = "YEARS" },
    };
}
