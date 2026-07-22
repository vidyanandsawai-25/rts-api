using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// GST/tax rate master. Maps to the [AMS].[GSTMaster] table.
/// A rate is applicable for demands whose reference date falls within
/// <see cref="EffectiveFromDate"/> .. <see cref="EffectiveToDate"/>.
/// </summary>
public class GSTMasterEntity : BaseEntity, IHardDeletable
{
    public string TaxCode { get; set; } = string.Empty;
    public string TaxName { get; set; } = string.Empty;
    public decimal TaxPercentage { get; set; }

    public DateTime EffectiveFromDate { get; set; }
    public DateTime? EffectiveToDate { get; set; }

    // IHardDeletable properties
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
