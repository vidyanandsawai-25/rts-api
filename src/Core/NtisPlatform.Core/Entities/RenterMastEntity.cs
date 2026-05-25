using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// Represents renter master data in the PTIS system
/// </summary>
public class RenterMastEntity : BaseEntity, IHardDeletable
{
    public int PropertyDetailsId { get; set; }

    public double? RentMonthly { get; set; }

    public double? FinalYearlyRent { get; set; }

    public string? FinancialYear { get; set; }

    public DateTime? DurationFrom { get; set; }

    public DateTime? DurationTo { get; set; }

    public string? TaxLiability { get; set; }

    public double? NonCalculateRentMonthly { get; set; }

    public string? RenterNameEnglish { get; set; }

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
