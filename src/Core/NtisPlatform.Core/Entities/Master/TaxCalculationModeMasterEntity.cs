using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// A calculation mode available to the Dynamic Tax Register (PTIS.TaxCalculationModeMaster) —
/// the DB-driven replacement for a hardcoded VALUE_BASED/CONDITION_BASED/MASTER_BASED/HYBRID list.
///
/// The <c>Uses*Config</c> flags are what make it genuinely data-driven: code must never branch on
/// <see cref="ModeCode"/> ("is this HYBRID?") but on the capabilities ("does this mode use master
/// config?"). A new mode that reuses an existing mechanism is then a pure DB insert — it picks up
/// its dropdown entry, its configuration tab, and correct mode-change cleanup with no code change.
/// </summary>
[Table("TaxCalculationModeMaster", Schema = "PTIS")]
public class TaxCalculationModeMasterEntity : BaseEntity
{
    /// <summary>Stable machine value, also what the API/JSON exchanges (e.g. "VALUE_BASED").</summary>
    [Required]
    [Column(TypeName = "nvarchar(20)")]
    public string ModeCode { get; set; } = null!;

    /// <summary>Fallback display label. The UI prefers its own i18n string when one exists for
    /// this code, so this stays readable for API consumers and DB queries without forcing English
    /// into the screen.</summary>
    [Required]
    [Column(TypeName = "nvarchar(100)")]
    public string ModeName { get; set; } = null!;

    public int DisplayOrder { get; set; } = 0;

    /// <summary>Uses PTIS.TaxPercentageMasterRV (per-TypeOfUse percentage rows).</summary>
    public bool UsesValueConfig { get; set; }

    /// <summary>Uses PTIS.TaxConditionRule (priority-ordered condition rows).</summary>
    public bool UsesConditionConfig { get; set; }

    /// <summary>Uses PTIS.TaxMasterMapping (master key → result mappings).</summary>
    public bool UsesMasterConfig { get; set; }

    /// <summary>Uses PTIS.TaxHybridConfig (the hybrid strategy row).</summary>
    public bool UsesHybridConfig { get; set; }

    // ── Navigation ──────────────────────────────────────────────────────────────
    public ICollection<TaxMasterEntity> TaxMasters { get; set; } = new List<TaxMasterEntity>();
}
