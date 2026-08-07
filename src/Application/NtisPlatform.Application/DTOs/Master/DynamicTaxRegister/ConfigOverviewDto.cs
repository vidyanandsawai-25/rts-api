using System.Collections.Generic;

namespace NtisPlatform.Application.DTOs.Master;

/// <summary>
/// Read-only aggregate for the "Show Config" overview modal — a bird's-eye view of every
/// tax's configuration, grouped by calculation mode. Each tax appears in exactly one bucket
/// (Value / Condition / Master / Hybrid) by its <c>TaxMaster.CalculationMode</c>. Purely a
/// reporting projection over existing tables; never touches billing.
/// </summary>
public class ConfigOverviewDto
{
    public ValueOverviewDto Value { get; set; } = new();
    public List<ConditionOverviewRowDto> Condition { get; set; } = new();
    public List<MasterOverviewRowDto> Master { get; set; } = new();
    public HybridOverviewDto Hybrid { get; set; } = new();
}

/// <summary>A tax identity used as a column header in the value pivot.</summary>
public class OverviewTaxDto
{
    public int TaxId { get; set; }
    public string? TaxName { get; set; }
    public string? TaxCode { get; set; }
}

/// <summary>
/// Value-based cross-tax pivot: <see cref="Taxes"/> are the dynamic columns (one per
/// value-based tax), <see cref="Rows"/> are one per (TypeOfUse × year-range) with a
/// TaxId→percentage map.
/// </summary>
public class ValueOverviewDto
{
    public List<OverviewTaxDto> Taxes { get; set; } = new();
    public List<ValueOverviewRowDto> Rows { get; set; } = new();
}

/// <summary>One pivot row: a type-of-use + assessment-year-range, with each value-based tax's percentage.</summary>
public class ValueOverviewRowDto
{
    public int TypeOfUseId { get; set; }
    public string? TypeOfUseCode { get; set; }
    public string? Description { get; set; }
    /// <summary>User group / classification (TypeOfUse.Type, e.g. "R-Residential").</summary>
    public string? Type { get; set; }
    public int YearRangeRVId { get; set; }
    public string YearRangeLabel { get; set; } = string.Empty;
    /// <summary>TaxId → percentage for this type-of-use + year. Serializes as {"4": 12.0}.</summary>
    public Dictionary<int, decimal> Percentages { get; set; } = new();
}

/// <summary>One condition rule row for the overview (Condition tab, or a Hybrid tax's condition side).</summary>
public class ConditionOverviewRowDto
{
    public int TaxId { get; set; }
    public string? TaxName { get; set; }
    public string? TaxCode { get; set; }
    public int SortOrder { get; set; }
    public List<TaxConditionItemDto> Conditions { get; set; } = new();
    public string ResultMode { get; set; } = "FIXED";
    public string ResultBase { get; set; } = "NONE";
    public decimal ResultValue { get; set; }
    /// <summary>Set for "PER_UNIT" rows — without it the overview cannot label the row's effect
    /// ("150 per Toilet Count") and would fall back to describing it as a flat amount.</summary>
    public string? UnitFieldId { get; set; }

    /// <summary>Set for "OTHER_TAX" rows — the referenced tax's display name, resolved server-side
    /// because the overview tables receive no tax list to look it up from. Without it the effect
    /// reads "Percent 200% of another tax" instead of naming the tax.</summary>
    public string? ReferenceTaxName { get; set; }

    public bool IsActive { get; set; }

    /// <summary>When true, this row halts evaluation if it matches — rows below it (by SortOrder)
    /// never run. See TaxConditionRuleEntity.StopFurtherProcessing.</summary>
    public bool StopFurtherProcessing { get; set; }

    /// <summary>PROPERTY_BASED | BUILDING_BASED — descriptive classification only. See
    /// TaxConditionRuleEntity.IsBuildingBased.</summary>
    public string AssessmentBasis { get; set; } = "PROPERTY_BASED";
    public int? AssessmentYearRangeId { get; set; }
    public string? YearRangeLabel { get; set; }
}

/// <summary>One master-mapping row for the overview (Master tab, or a Hybrid tax's master side).</summary>
public class MasterOverviewRowDto
{
    public int TaxId { get; set; }
    public string? TaxName { get; set; }
    public string? TaxCode { get; set; }
    /// <summary>Which master this mapping is keyed against (PropertyType / OwnerType / TypeOfUse),
    /// from the linked rule's AttachedReference. Null when the row has no linked rule.</summary>
    public string? MasterName { get; set; }
    public string MasterKey { get; set; } = string.Empty;
    public string? DisplayValue { get; set; }
    public string ResultMode { get; set; } = "FIXED";
    public string ResultBase { get; set; } = "NONE";
    public decimal ResultValue { get; set; }
    public int AssessmentYearRangeId { get; set; }
    public string? YearRangeLabel { get; set; }
}

/// <summary>Hybrid taxes carry both condition rules and master mappings.</summary>
public class HybridOverviewDto
{
    public List<ConditionOverviewRowDto> Condition { get; set; } = new();
    public List<MasterOverviewRowDto> Master { get; set; } = new();
}

/// <summary>
/// Server-side filter + pagination request for a single "Show Config" section. The overview is
/// requested one tab (section) at a time so the client only ever receives one page — see
/// <see cref="ConfigOverviewTab"/> for the section keys. Only the filters relevant to the
/// requested <see cref="Tab"/> are honoured; the rest are ignored.
/// </summary>
public class ConfigOverviewQueryParameters
{
    /// <summary>Which section to page — see <see cref="ConfigOverviewTab"/>. Defaults to Value.</summary>
    public string? Tab { get; set; }
    public int PageNumber { get; set; } = 1;
    /// <summary>Page size; <c>-1</c> returns every row (mirrors the register feature's convention).</summary>
    public int PageSize { get; set; } = 25;

    // ── Value-tab filters ──
    /// <summary>AssessmentYearRange id (TaxPercentageMasterRV.YearRangeRVId).</summary>
    public int? YearRangeRVId { get; set; }
    /// <summary>Restrict the pivot to a TypeOfUseGroup ("Type" filter).</summary>
    public int? TypeOfUseGroupId { get; set; }
    /// <summary>Restrict the pivot to a single TypeOfUse ("Description" filter).</summary>
    public int? TypeOfUseId { get; set; }

    // ── Master-tab filters ──
    /// <summary>Restrict master mappings to a single tax.</summary>
    public int? TaxId { get; set; }
    /// <summary>Restrict master mappings to a resolved master name (PropertyType / OwnerType / TypeOfUse).</summary>
    public string? MasterName { get; set; }
}

/// <summary>Canonical <see cref="ConfigOverviewQueryParameters.Tab"/> values. The Hybrid tab is
/// split into two independently-paged sections because its UI shows two separate tables.</summary>
public static class ConfigOverviewTab
{
    public const string Value = "value";
    public const string Condition = "condition";
    public const string Master = "master";
    public const string HybridCondition = "hybridCondition";
    public const string HybridMaster = "hybridMaster";
}

/// <summary>
/// One page of a single config-overview section. Exactly one row list is populated, chosen by the
/// requested <see cref="Tab"/>: Value → <see cref="ValueRows"/> (+ <see cref="ValueTaxes"/> column
/// headers), Condition/HybridCondition → <see cref="ConditionRows"/>, Master/HybridMaster →
/// <see cref="MasterRows"/>. <see cref="TotalCount"/> is the full (post-filter) row count for the paginator.
/// </summary>
public class ConfigOverviewPageDto
{
    public string Tab { get; set; } = ConfigOverviewTab.Value;
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    /// <summary>Value pivot column headers (all value-based taxes) — populated only for the Value tab.</summary>
    public List<OverviewTaxDto> ValueTaxes { get; set; } = new();
    public List<ValueOverviewRowDto> ValueRows { get; set; } = new();
    public List<ConditionOverviewRowDto> ConditionRows { get; set; } = new();
    public List<MasterOverviewRowDto> MasterRows { get; set; } = new();
}
