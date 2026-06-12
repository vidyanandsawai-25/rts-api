using NtisPlatform.Core.Entities;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Represents a tax category in the PTIS system (PTIS.TaxCategoryMaster table).
/// Classifies taxes into broad groups such as Property Tax, Cess, Education Tax, etc.
/// </summary>
/// <remarks>
/// Seed data (as of initial migration):
/// <list type="table">
///   <listheader><term>Id</term><term>CategoryCode</term><term>CategoryName</term></listheader>
///   <item><term>1</term><term>TAX</term><term>Property Tax</term></item>
///   <item><term>2</term><term>CESS</term><term>Cess</term></item>
///   <item><term>3</term><term>EDU</term><term>State Education Tax</term></item>
///   <item><term>4</term><term>EMP</term><term>State Employment Tax</term></item>
///   <item><term>5</term><term>USER</term><term>User Charges</term></item>
///   <item><term>6</term><term>PENALTY</term><term>Penalty</term></item>
/// </list>
/// <c>RateableValueService.IsEducationTax</c> matches <c>CategoryCode = "EDU"</c> and
/// <c>IsEmploymentTax</c> matches <c>CategoryCode = "EMP"</c> via the loaded navigation.
/// </remarks>
[Table("TaxCategoryMaster", Schema = "PTIS")]
public class TaxCategoryMasterEntity : BaseEntity
{
    /// <summary>Short code uniquely identifying the category (e.g. TAX, EDU, EMP).</summary>
    [Required]
    [Column(TypeName = "nvarchar(50)")]
    public string CategoryCode { get; set; } = null!;

    /// <summary>Human-readable name of the category.</summary>
    [Required]
    [Column(TypeName = "nvarchar(100)")]
    public string CategoryName { get; set; } = null!;

    // ── Navigation ──────────────────────────────────────────────────────────────
    /// <summary>Taxes that belong to this category.</summary>
    public ICollection<TaxMasterEntity> TaxMasters { get; set; } = new List<TaxMasterEntity>();
}
