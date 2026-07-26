using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Master;

/// <summary>
/// Penalty rule master. Maps to the [AMS].[PenaltyRuleMaster] table.
/// Drives late-fee calculation on overdue lease-rent demands.
/// </summary>
public class PenaltyRuleMasterEntity : BaseEntity, IHardDeletable
{
    public string PenaltyCode { get; set; } = string.Empty;
    public string PenaltyName { get; set; } = string.Empty;

    /// <summary>Percentage | FlatAmount | PerDay.</summary>
    public string CalculationType { get; set; } = string.Empty;

    public decimal PenaltyValue { get; set; }
    public int GracePeriodDays { get; set; }

    // IHardDeletable properties
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
