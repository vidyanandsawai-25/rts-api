using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents transaction master data in the PTIS system
/// </summary>
public class TransMastEntity : BaseEntity, IHardDeletable
{

    public int PropertyId { get; set; }

    /// <summary>
    /// Foreign key to YearMaster (Finance Year)
    /// </summary>
    public int FinanceYearId { get; set; }

    /// <summary>
    /// Indicates whether this is Rateable Value (RV) or Capital Value (CV)
    /// </summary>

    public string CalculationType { get; set; } = string.Empty;

    /// <summary>
    /// The calculated RV or CV value for this property
    /// </summary>

    public decimal? CalculationValue { get; set; }

    /// <summary>
    /// The sum of Annual Rental Value for this property
    /// </summary>

    public decimal? CalculationAnnualValue { get; set; }

    [NotMapped]
    public string RVorCV
    {
        get => CalculationType;
        set => CalculationType = value;
    }

    [NotMapped]
    public decimal RVorCVValue
    {
        get => CalculationValue ?? 0;
        set => CalculationValue = value;
    }

    /// <summary>
    /// Foreign key to TaxMaster (type of tax)
    /// </summary>

    public int TaxId { get; set; }

    /// <summary>
    /// Calculated tax amount for this property, year, and tax type
    /// </summary>

    public decimal TaxAmount { get; set; }

    // Navigation Properties

    /// <summary>
    /// Navigation property to the associated Property
    /// </summary>
    [ForeignKey(nameof(PropertyId))]
    public virtual PropertyEntity? Property { get; set; }

    /// <summary>
    /// Navigation property to the Finance Year
    /// </summary>
    [ForeignKey(nameof(FinanceYearId))]
    public virtual YearMasterEntity? FinanceYear { get; set; }

    /// <summary>
    /// Navigation property to the Tax Master (type of tax)
    /// </summary>
    [ForeignKey(nameof(TaxId))]
    public virtual TaxMasterEntity? Tax { get; set; }

    public bool MarkedForDeletion { get; set; } = false;

    public DateTime? MarkedForDeletionDate { get; set; }
}