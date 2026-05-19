using NtisPlatform.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

 
/// <summary>
/// Represents tax master data in the PTIS system (TaxMaster table)
/// Stores tax type definitions and configurations
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

    [Required]
    public int TaxCategoryId { get; set; }

    public int DisplayOrder { get; set; } = 0;

    public bool TaxOnUnit { get; set; } = false;

    public bool AssessmentStatus { get; set; } = true;

    public bool OldTaxStatus { get; set; } = true;
}
