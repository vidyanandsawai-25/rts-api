using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;

/// <summary>One row of the Dynamic Tax Register read-only grid.</summary>
public class DynamicTaxRegisterRowDto
{
    public int TaxId { get; set; }
    public string? TaxName { get; set; }
    /// <summary>Regional-language name. Preferred over TaxName wherever a tax is shown to the
    /// public (see RateableValueService / PropertyOldDetailsRepository, which both fall back
    /// TaxNameAlias ?? TaxName).</summary>
    public string? TaxNameAlias { get; set; }
    public string? TaxCode { get; set; }
    public string CalculationMode { get; set; } = "VALUE_BASED";
    public int? RuleDefinitionId { get; set; }
    public string? RuleName { get; set; }
    public string? RuleCategory { get; set; }
    public string? Source { get; set; }
    /// <summary>Overall tax status — ACTIVE / DEACTIVE (from IsActive).</summary>
    public string Status { get; set; } = "DEACTIVE";
    public bool AssessmentStatus { get; set; }
    public bool OldTaxStatus { get; set; }
    public string? RuleSummary { get; set; }
}

/// <summary>A selectable tax category for the Add-Tax dropdown — sourced from
/// PTIS.TaxCategoryMaster (active rows only, EDU/EMP excluded).</summary>
public class TaxCategoryOptionDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

/// <summary>Hero stat-card counts per calculation mode.</summary>
public class DynamicTaxRegisterStatsDto
{
    public int ValueBased { get; set; }
    public int ConditionBased { get; set; }
    public int MasterBased { get; set; }
    public int Hybrid { get; set; }
    public int Total { get; set; }
}

/// <summary>Filters for the register list.</summary>
public class DynamicTaxRegisterQueryParameters
{
    public string? Search { get; set; }
    public string? Mode { get; set; }
    /// <summary>ACTIVE | DEACTIVE.</summary>
    public string? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>Payload for the General-tab "Save Settings" action.</summary>
public class UpdateTaxRegisterSettingsRequest
{
    /// <summary>ACTIVE | DEACTIVE (mapped to IsActive). Validated against exactly these two values —
    /// any other value (including a typo) is rejected.</summary>
    [Required]
    public string? Status { get; set; }

    /// <summary>Null means "not supplied — leave unchanged", matching TaxName/TaxNameAlias's
    /// convention. Was previously a non-nullable bool, so an omitted field silently reset it to
    /// false for any caller that didn't send it explicitly.</summary>
    public bool? AssessmentStatus { get; set; }

    /// <summary>Null means "not supplied — leave unchanged" — see AssessmentStatus.</summary>
    public bool? OldTaxStatus { get; set; }

    /// <summary>Editable Tax Name (identity). Null/blank leaves the stored name unchanged.</summary>
    public string? TaxName { get; set; }

    /// <summary>Regional-language name. Unlike TaxName this is optional and clearable, so the
    /// semantics differ deliberately: null means "not supplied — leave as-is", while an empty
    /// string means "the admin cleared it" and stores NULL.</summary>
    [StringLength(200, ErrorMessage = "DynamicTaxRegister_TaxNameAlias_MaxLengthExceeded_200")]
    public string? TaxNameAlias { get; set; }

    /// <summary>VALUE_BASED | CONDITION_BASED | MASTER_BASED | HYBRID. Deliberately nullable with
    /// NO default — a non-null default would let a payload that simply omits the field silently
    /// mean VALUE_BASED, which (given the mode-change cleanup below) would wipe a tax's
    /// condition/master/hybrid configuration. Blank/unknown values are rejected both here and
    /// again in the service (defence in depth — this one is safety-critical).</summary>
    public string? CalculationMode { get; set; }

    public int? RuleDefinitionId { get; set; }
    public int? UpdatedBy { get; set; }

    /// <summary>The CalculationMode the caller believed this tax was in when it rendered its
    /// confirmation. When supplied and it does not match the stored mode, the update is rejected
    /// with 409 rather than acting on a stale view (the caller may be about to destroy config it
    /// never warned the user about).</summary>
    public string? ExpectedCurrentMode { get; set; }

    /// <summary>Explicit opt-in to deleting the abandoned mode's configuration rows. Changing
    /// CalculationMode without this is rejected with 409 — deletion is never implicit.</summary>
    public bool ConfirmModeChangeCleanup { get; set; }
}

/// <summary>Per-tax configuration row counts, so the UI can name exactly what a mode change
/// would delete before asking the admin to confirm it.</summary>
public class TaxConfigSummaryDto
{
    public int TaxId { get; set; }
    public int ValueRowCount { get; set; }
    public int ConditionRowCount { get; set; }
    public int MasterMappingCount { get; set; }
    public bool HasHybridConfig { get; set; }
}

/// <summary>Payload for creating a new tax from the "Add Tax" action.</summary>
public class CreateTaxRegisterRequest
{
    [Required]
    [StringLength(200)]
    public string TaxName { get; set; } = null!;

    /// <summary>Optional regional-language name (e.g. Marathi/Hindi).</summary>
    [StringLength(200, ErrorMessage = "DynamicTaxRegister_TaxNameAlias_MaxLengthExceeded_200")]
    public string? TaxNameAlias { get; set; }

    [Required]
    [StringLength(20)]
    public string TaxCode { get; set; } = null!;

    /// <summary>FK → PTIS.TaxCategoryMaster (e.g. 1 = Property Tax).</summary>
    [Range(1, int.MaxValue, ErrorMessage = "DynamicTaxRegister_TaxCategoryId_Invalid")]
    public int TaxCategoryId { get; set; }

    /// <summary>VALUE_BASED | CONDITION_BASED | MASTER_BASED | HYBRID.</summary>
    [Required]
    public string CalculationMode { get; set; } = "VALUE_BASED";

    public int? RuleDefinitionId { get; set; }

    /// <summary>ACTIVE | DEACTIVE (mapped to IsActive).</summary>
    public string Status { get; set; } = "ACTIVE";

    public bool AssessmentStatus { get; set; } = true;
    public bool OldTaxStatus { get; set; } = false;
    public int? CreatedBy { get; set; }
}
