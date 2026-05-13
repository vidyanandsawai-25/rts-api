using NtisPlatform.Core.Interfaces;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities;

[Table("RenterDetails", Schema = "PTIS")]
public class RenterDetailEntity : BaseEntity, IHardDeletable
{
    public int PropertyDetailsId { get; set; }
    public string? AgreementId { get; set; }
    public string? IncrementFrequency { get; set; }
    public string? IncrementType { get; set; }
    public double? IncrementValue { get; set; }
    public string? IncrementMethod { get; set; }
    public DateTime? DurationFrom { get; set; }
    public DateTime? DurationTo { get; set; }
    public double? RentAmount { get; set; }
    public double? RentMonthly { get; set; }
    public double? Increment { get; set; }
    public bool? IncrementStatus { get; set; }

    // Custom increment fields
    public DateTime? CustomFromDate { get; set; }
    public DateTime? CustomToDate { get; set; }
    public string? CustomIncrementType { get; set; }
    public double? CustomIncrementValue { get; set; }
    public string? CustomMethod { get; set; }

    public bool MarkedForDeletion { get; set; } = false;
    public DateTime? MarkedForDeletionDate { get; set; }
    public virtual PropertyDetailsEntity? PropertyDetails { get; set; }
}
