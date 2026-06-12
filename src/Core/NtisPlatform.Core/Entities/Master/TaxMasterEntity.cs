using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Represents a tax type in the PTIS system (PTIS.TaxMaster table).
/// Classification (Education / Employment / General) is determined at runtime by reading
/// <see cref="TaxCategoryMasterEntity"/>.<see cref="TaxCategoryMasterEntity.CategoryCode"/>
/// via the <c>PTIS.TaxCategoryMaster</c> foreign key — no enum or extra column required.
/// </summary>
[Table("TaxMaster", Schema = "PTIS")]
public class TaxMasterEntity : BaseEntity
{
    [Required]
    [Column(TypeName = "nvarchar(20)")]
    public string TaxCode { get; set; } = null!;

    [Required]
    [Column(TypeName = "nvarchar(200)")]
    public string TaxName { get; set; } = null!;

    [Column(TypeName = "nvarchar(200)")]
    public string? TaxNameAlias { get; set; }

    /// <summary>FK → <see cref="TaxCategoryMasterEntity"/>.</summary>
    [Required]
    public int TaxCategoryId { get; set; }

    public int DisplayOrder { get; set; } = 0;

    public bool TaxOnUnit { get; set; } = false;

    public bool AssessmentStatus { get; set; } = true;

    public bool OldTaxStatus { get; set; } = true;

    // ── Navigation ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Category this tax belongs to.
    /// Load with <c>.Include(t =&gt; t.TaxCategoryMaster)</c> to access
    /// <see cref="TaxCategoryMasterEntity.CategoryCode"/> for classification logic.
    /// </summary>
    public TaxCategoryMasterEntity? TaxCategoryMaster { get; set; }

    public ICollection<TaxPercentageMasterCVEntity> TaxPercentageMasterCV { get; set; } = new List<TaxPercentageMasterCVEntity>();
    public ICollection<PolicyTaxDetailsEntity> PolicyTaxDetails { get; set; } = new List<PolicyTaxDetailsEntity>();
    public ICollection<PolicyTaxDetailsCVEntity> PolicyTaxDetailsCV { get; set; } = new List<PolicyTaxDetailsCVEntity>();
}
