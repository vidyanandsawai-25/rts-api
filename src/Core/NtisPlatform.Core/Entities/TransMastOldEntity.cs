using NtisPlatform.Core.Entities.Master;
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
    public int PropertyMastOldId { get; set; }

    [Required]
    public int FinanceYearId { get; set; }

    [Required]
    [Column(TypeName = "char(2)")]
    public string CalculationType { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal CalculationValue { get; set; }

    [NotMapped]
    public string RVorCV
    {
        get => CalculationType;
        set => CalculationType = value;
    }

    [NotMapped]
    public decimal RVorCVValue
    {
        get => CalculationValue;
        set => CalculationValue = value;
    }

    [Required]
    public int TaxId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }

    public bool MarkedForDeletion { get; set; } = false;

    public DateTime? MarkedForDeletionDate { get; set; }

    [ForeignKey(nameof(TaxId))]
    public virtual TaxMasterEntity? TaxMaster { get; set; }

}
