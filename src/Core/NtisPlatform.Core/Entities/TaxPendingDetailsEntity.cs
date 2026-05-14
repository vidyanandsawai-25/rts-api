using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents tax pending details in the PTIS system
/// </summary>
[Table("TaxPendingDetails", Schema = "PTIS")]
public class TaxPendingDetailsEntity : BaseEntity
{
    [Required]
    public int PropertyId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PendingAmount { get; set; }
}
