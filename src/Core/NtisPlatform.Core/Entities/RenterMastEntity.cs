using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents renter master data in the PTIS system
/// </summary>
[Table("RenterMast", Schema = "PTIS")]
public class RenterMastEntity : BaseEntity
{
    [Required]
    public int PropertyDetailsId { get; set; }

    [Column(TypeName = "float")]
    public double? RentMonthly { get; set; }

    [Column(TypeName = "float")]
    public double? FinalYearlyRent { get; set; }

    [Column(TypeName = "nvarchar(4)")]
    public string? FinancialYear { get; set; }

    public DateTime? DurationFrom { get; set; }

    public DateTime? DurationTo { get; set; }

    [Column(TypeName = "nvarchar(20)")]
    public string? TaxLiability { get; set; }

    [Column(TypeName = "float")]
    public double? NonCalculateRentMonthly { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string? RenterNameEnglish { get; set; }

    [Column(TypeName = "nvarchar(500)")]
    public string? RenterName { get; set; }

    public DateTime? AgreementDate { get; set; }

    public DateTime? AgreementFromDate { get; set; }

    public DateTime? AgreementToDate { get; set; }

    /// <summary>
    /// Indicates whether the entity is marked for deletion
    /// </summary>
    public bool MarkedForDeletion { get; set; } = false;

    /// <summary>
    /// Date when marked for deletion
    /// </summary>
    public DateTime? MarkedForDeletionDate { get; set; }

    // Navigation property
    /// <summary>
    /// Navigation property to PropertyDetails
    /// </summary>
    [ForeignKey(nameof(PropertyDetailsId))]
    public virtual PropertyDetailsEntity? PropertyDetails { get; set; }
}
