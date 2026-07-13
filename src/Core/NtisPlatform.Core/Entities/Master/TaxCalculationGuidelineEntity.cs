namespace NtisPlatform.Core.Entities.Master;

public class TaxCalculationGuidelineEntity : BaseEntity
{
    public string GuidelineCode { get; set; } = string.Empty;
    public string GuidelineName { get; set; } = string.Empty;
    public string? Description { get; set; }

    public bool EnableCertificateBasedTax { get; set; } = true;
    public bool ApplyOnlyProtectedCertificateTypes { get; set; } = true;
    public byte FinancialYearStartMonth { get; set; } = 4;
    public byte FinancialYearStartDay { get; set; } = 1;

    public string DatePriority1 { get; set; } = "RETROSPECTIVE";
    public string DatePriority2 { get; set; } = "ELECTRIC_BILL";
    public string DatePriority3 { get; set; } = "CC";
    public string DatePriority4 { get; set; } = "OC";

    public bool EnableCCToOCSplit { get; set; } = true;
    public int IgnoreCCToOCIfWithinValue { get; set; } = 0;
    public string IgnoreCCToOCIfWithinType { get; set; } = "MONTHS";
    public decimal CCPeriodMultiplier { get; set; } = 1.0000m;
    public decimal OCPeriodMultiplier { get; set; } = 1.0000m;

    public string ElectricBillDateRule { get; set; } = "NO_TAX";
    public int ElectricBillAddMonths { get; set; } = 0;
    public decimal ElectricBillMultiplier { get; set; } = 1.0000m;

    public string NoDateRule { get; set; } = "DEFAULT_RETROSPECTIVE";
    public int LookbackYears { get; set; } = 5;
    public decimal DefaultRetrospectiveMultiplier { get; set; } = 1.0000m;

    public string FloorCertificatePriority { get; set; } = "PROPERTY_OVERRIDES_FLOOR";
    public bool EnableCurrentYearProration { get; set; } = true;
    public string ProrationMethod { get; set; } = "FULL_YEAR";
    public string TaxPersistenceMode { get; set; } = "PROPERTY_AGGREGATED";

    public string? PolicyReferenceNo { get; set; }
    public DateTime? PolicyReferenceDate { get; set; }
    public string? PolicyApprovedBy { get; set; }
    public string? Remark { get; set; }
}
