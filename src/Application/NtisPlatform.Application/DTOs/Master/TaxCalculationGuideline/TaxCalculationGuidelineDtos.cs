using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.TaxCalculationGuideline;

public class TaxCalculationGuidelineDto : BaseDtos
{
    public string GuidelineCode { get; set; } = string.Empty;
    public string GuidelineName { get; set; } = string.Empty;
    public string? Description { get; set; }

    public bool EnableCertificateBasedTax { get; set; }
    public bool ApplyOnlyProtectedCertificateTypes { get; set; }
    public byte FinancialYearStartMonth { get; set; }
    public byte FinancialYearStartDay { get; set; }

    public string DatePriority1 { get; set; } = string.Empty;
    public string DatePriority2 { get; set; } = string.Empty;
    public string DatePriority3 { get; set; } = string.Empty;
    public string DatePriority4 { get; set; } = string.Empty;

    public bool EnableCCToOCSplit { get; set; }
    public int IgnoreCCToOCIfWithinValue { get; set; }
    public string IgnoreCCToOCIfWithinType { get; set; } = string.Empty;
    public decimal CCPeriodMultiplier { get; set; }
    public decimal OCPeriodMultiplier { get; set; }

    public string ElectricBillDateRule { get; set; } = string.Empty;
    public int ElectricBillAddMonths { get; set; }
    public decimal ElectricBillMultiplier { get; set; }

    public string NoDateRule { get; set; } = string.Empty;
    public int LookbackYears { get; set; }
    public decimal DefaultRetrospectiveMultiplier { get; set; }

    public string FloorCertificatePriority { get; set; } = string.Empty;
    public bool EnableCurrentYearProration { get; set; }
    public string ProrationMethod { get; set; } = string.Empty;
    public string TaxPersistenceMode { get; set; } = string.Empty;

    public string? PolicyReferenceNo { get; set; }
    public DateTime? PolicyReferenceDate { get; set; }
    public string? PolicyApprovedBy { get; set; }
    public string? Remark { get; set; }
}

public class CreateTaxCalculationGuidelineDto : CreateBaseDtos
{
    [Required(ErrorMessage = "TaxCalculationGuideline_GuidelineCode_Required")]
    [StringLength(50, ErrorMessage = "TaxCalculationGuideline_GuidelineCode_MaxLen_50")]
    public string GuidelineCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "TaxCalculationGuideline_GuidelineName_Required")]
    [StringLength(150, ErrorMessage = "TaxCalculationGuideline_GuidelineName_MaxLen_150")]
    public string GuidelineName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "TaxCalculationGuideline_Description_MaxLen_500")]
    public string? Description { get; set; }

    public bool EnableCertificateBasedTax { get; set; } = true;
    public bool ApplyOnlyProtectedCertificateTypes { get; set; } = true;

    [Range(1, 12, ErrorMessage = "TaxCalculationGuideline_FinancialYearStartMonth_Range_1_12")]
    public byte FinancialYearStartMonth { get; set; } = 4;

    [Range(1, 31, ErrorMessage = "TaxCalculationGuideline_FinancialYearStartDay_Range_1_31")]
    public byte FinancialYearStartDay { get; set; } = 1;

    [Required]
    [RegularExpression("^(RETROSPECTIVE|ELECTRIC_BILL|CC|OC)$", ErrorMessage = "TaxCalculationGuideline_DatePriority_Invalid")]
    public string DatePriority1 { get; set; } = "RETROSPECTIVE";

    [Required]
    [RegularExpression("^(RETROSPECTIVE|ELECTRIC_BILL|CC|OC)$", ErrorMessage = "TaxCalculationGuideline_DatePriority_Invalid")]
    public string DatePriority2 { get; set; } = "ELECTRIC_BILL";

    [Required]
    [RegularExpression("^(RETROSPECTIVE|ELECTRIC_BILL|CC|OC)$", ErrorMessage = "TaxCalculationGuideline_DatePriority_Invalid")]
    public string DatePriority3 { get; set; } = "CC";

    [Required]
    [RegularExpression("^(RETROSPECTIVE|ELECTRIC_BILL|CC|OC)$", ErrorMessage = "TaxCalculationGuideline_DatePriority_Invalid")]
    public string DatePriority4 { get; set; } = "OC";

    public bool EnableCCToOCSplit { get; set; } = true;
    public int IgnoreCCToOCIfWithinValue { get; set; } = 0;

    [Required]
    [RegularExpression("^(YEARS|MONTHS|DAYS)$", ErrorMessage = "TaxCalculationGuideline_IgnoreCCToOCIfWithinType_Invalid")]
    [StringLength(10, ErrorMessage = "TaxCalculationGuideline_IgnoreCCToOCIfWithinType_MaxLen_10")]
    public string IgnoreCCToOCIfWithinType { get; set; } = "MONTHS";

    public decimal CCPeriodMultiplier { get; set; } = 1.0000m;
    public decimal OCPeriodMultiplier { get; set; } = 1.0000m;

    [Required]
    [RegularExpression("^(NO_TAX|ADD_MONTHS|FROM_FY_START|EXACT_DATE)$", ErrorMessage = "TaxCalculationGuideline_ElectricBillDateRule_Invalid")]
    [StringLength(30, ErrorMessage = "TaxCalculationGuideline_ElectricBillDateRule_MaxLen_30")]
    public string ElectricBillDateRule { get; set; } = "NO_TAX";

    public int ElectricBillAddMonths { get; set; } = 0;
    public decimal ElectricBillMultiplier { get; set; } = 1.0000m;

    [Required]
    [RegularExpression("^(ASSESSMENT_YEAR|CONSTRUCTION_YEAR|NO_TAX|DEFAULT_RETROSPECTIVE)$", ErrorMessage = "TaxCalculationGuideline_NoDateRule_Invalid")]
    [StringLength(30, ErrorMessage = "TaxCalculationGuideline_NoDateRule_MaxLen_30")]
    public string NoDateRule { get; set; } = "DEFAULT_RETROSPECTIVE";

    public int LookbackYears { get; set; } = 5;
    public decimal DefaultRetrospectiveMultiplier { get; set; } = 1.0000m;

    [Required]
    [RegularExpression("^(PROPERTY_OVERRIDES_FLOOR|FLOOR_OVERRIDES_PROPERTY)$", ErrorMessage = "TaxCalculationGuideline_FloorCertificatePriority_Invalid")]
    [StringLength(30, ErrorMessage = "TaxCalculationGuideline_FloorCertificatePriority_MaxLen_30")]
    public string FloorCertificatePriority { get; set; } = "PROPERTY_OVERRIDES_FLOOR";

    public bool EnableCurrentYearProration { get; set; } = true;

    [Required]
    [RegularExpression("^(FULL_YEAR|MONTHLY|DAILY)$", ErrorMessage = "TaxCalculationGuideline_ProrationMethod_Invalid")]
    [StringLength(20, ErrorMessage = "TaxCalculationGuideline_ProrationMethod_MaxLen_20")]
    public string ProrationMethod { get; set; } = "FULL_YEAR";

    [Required]
    [RegularExpression("^(FLOOR_LEDGER|PROPERTY_AGGREGATED)$", ErrorMessage = "TaxCalculationGuideline_TaxPersistenceMode_Invalid")]
    [StringLength(30, ErrorMessage = "TaxCalculationGuideline_TaxPersistenceMode_MaxLen_30")]
    public string TaxPersistenceMode { get; set; } = "PROPERTY_AGGREGATED";

    [StringLength(100, ErrorMessage = "TaxCalculationGuideline_PolicyReferenceNo_MaxLen_100")]
    public string? PolicyReferenceNo { get; set; }

    public DateTime? PolicyReferenceDate { get; set; }

    [StringLength(150, ErrorMessage = "TaxCalculationGuideline_PolicyApprovedBy_MaxLen_150")]
    public string? PolicyApprovedBy { get; set; }

    [StringLength(500, ErrorMessage = "TaxCalculationGuideline_Remark_MaxLen_500")]
    public string? Remark { get; set; }
}

public class UpdateTaxCalculationGuidelineDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "TaxCalculationGuideline_GuidelineCode_Required")]
    [StringLength(50, ErrorMessage = "TaxCalculationGuideline_GuidelineCode_MaxLen_50")]
    public string GuidelineCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "TaxCalculationGuideline_GuidelineName_Required")]
    [StringLength(150, ErrorMessage = "TaxCalculationGuideline_GuidelineName_MaxLen_150")]
    public string GuidelineName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "TaxCalculationGuideline_Description_MaxLen_500")]
    public string? Description { get; set; }

    public bool EnableCertificateBasedTax { get; set; } = true;
    public bool ApplyOnlyProtectedCertificateTypes { get; set; } = true;

    [Range(1, 12, ErrorMessage = "TaxCalculationGuideline_FinancialYearStartMonth_Range_1_12")]
    public byte FinancialYearStartMonth { get; set; } = 4;

    [Range(1, 31, ErrorMessage = "TaxCalculationGuideline_FinancialYearStartDay_Range_1_31")]
    public byte FinancialYearStartDay { get; set; } = 1;

    [Required]
    [RegularExpression("^(RETROSPECTIVE|ELECTRIC_BILL|CC|OC)$", ErrorMessage = "TaxCalculationGuideline_DatePriority_Invalid")]
    public string DatePriority1 { get; set; } = "RETROSPECTIVE";

    [Required]
    [RegularExpression("^(RETROSPECTIVE|ELECTRIC_BILL|CC|OC)$", ErrorMessage = "TaxCalculationGuideline_DatePriority_Invalid")]
    public string DatePriority2 { get; set; } = "ELECTRIC_BILL";

    [Required]
    [RegularExpression("^(RETROSPECTIVE|ELECTRIC_BILL|CC|OC)$", ErrorMessage = "TaxCalculationGuideline_DatePriority_Invalid")]
    public string DatePriority3 { get; set; } = "CC";

    [Required]
    [RegularExpression("^(RETROSPECTIVE|ELECTRIC_BILL|CC|OC)$", ErrorMessage = "TaxCalculationGuideline_DatePriority_Invalid")]
    public string DatePriority4 { get; set; } = "OC";

    public bool EnableCCToOCSplit { get; set; } = true;
    public int IgnoreCCToOCIfWithinValue { get; set; } = 0;

    [Required]
    [RegularExpression("^(YEARS|MONTHS|DAYS)$", ErrorMessage = "TaxCalculationGuideline_IgnoreCCToOCIfWithinType_Invalid")]
    [StringLength(10, ErrorMessage = "TaxCalculationGuideline_IgnoreCCToOCIfWithinType_MaxLen_10")]
    public string IgnoreCCToOCIfWithinType { get; set; } = "MONTHS";

    public decimal CCPeriodMultiplier { get; set; } = 1.0000m;
    public decimal OCPeriodMultiplier { get; set; } = 1.0000m;

    [Required]
    [RegularExpression("^(NO_TAX|ADD_MONTHS|FROM_FY_START|EXACT_DATE)$", ErrorMessage = "TaxCalculationGuideline_ElectricBillDateRule_Invalid")]
    [StringLength(30, ErrorMessage = "TaxCalculationGuideline_ElectricBillDateRule_MaxLen_30")]
    public string ElectricBillDateRule { get; set; } = "NO_TAX";

    public int ElectricBillAddMonths { get; set; } = 0;
    public decimal ElectricBillMultiplier { get; set; } = 1.0000m;

    [Required]
    [RegularExpression("^(ASSESSMENT_YEAR|CONSTRUCTION_YEAR|NO_TAX|DEFAULT_RETROSPECTIVE)$", ErrorMessage = "TaxCalculationGuideline_NoDateRule_Invalid")]
    [StringLength(30, ErrorMessage = "TaxCalculationGuideline_NoDateRule_MaxLen_30")]
    public string NoDateRule { get; set; } = "DEFAULT_RETROSPECTIVE";

    public int LookbackYears { get; set; } = 5;
    public decimal DefaultRetrospectiveMultiplier { get; set; } = 1.0000m;

    [Required]
    [RegularExpression("^(PROPERTY_OVERRIDES_FLOOR|FLOOR_OVERRIDES_PROPERTY)$", ErrorMessage = "TaxCalculationGuideline_FloorCertificatePriority_Invalid")]
    [StringLength(30, ErrorMessage = "TaxCalculationGuideline_FloorCertificatePriority_MaxLen_30")]
    public string FloorCertificatePriority { get; set; } = "PROPERTY_OVERRIDES_FLOOR";

    public bool EnableCurrentYearProration { get; set; } = true;

    [Required]
    [RegularExpression("^(FULL_YEAR|MONTHLY|DAILY)$", ErrorMessage = "TaxCalculationGuideline_ProrationMethod_Invalid")]
    [StringLength(20, ErrorMessage = "TaxCalculationGuideline_ProrationMethod_MaxLen_20")]
    public string ProrationMethod { get; set; } = "FULL_YEAR";

    [Required]
    [RegularExpression("^(FLOOR_LEDGER|PROPERTY_AGGREGATED)$", ErrorMessage = "TaxCalculationGuideline_TaxPersistenceMode_Invalid")]
    [StringLength(30, ErrorMessage = "TaxCalculationGuideline_TaxPersistenceMode_MaxLen_30")]
    public string TaxPersistenceMode { get; set; } = "PROPERTY_AGGREGATED";

    [StringLength(100, ErrorMessage = "TaxCalculationGuideline_PolicyReferenceNo_MaxLen_100")]
    public string? PolicyReferenceNo { get; set; }

    public DateTime? PolicyReferenceDate { get; set; }

    [StringLength(150, ErrorMessage = "TaxCalculationGuideline_PolicyApprovedBy_MaxLen_150")]
    public string? PolicyApprovedBy { get; set; }

    [StringLength(500, ErrorMessage = "TaxCalculationGuideline_Remark_MaxLen_500")]
    public string? Remark { get; set; }
}
