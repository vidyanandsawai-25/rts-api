using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents old tax transaction data in the PTIS system (TransMastOld table)
/// Stores historical tax information per property, year, and tax type
/// </summary>
[Table("TransMastOld", Schema = "PTIS")]
public class TransMastOldEntity : BaseEntity
{
    [Required]
    public int PropertyId { get; set; }

    [Required]
    public int FinanceYearId { get; set; }

    [Required]
    [Column(TypeName = "char(2)")]
    public string RVorCV { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal RVorCVValue { get; set; }

    [Required]
    public int TaxId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }

    public bool MarkedForDeletion { get; set; } = false;

    public DateTime? MarkedForDeletionDate { get; set; }
}
