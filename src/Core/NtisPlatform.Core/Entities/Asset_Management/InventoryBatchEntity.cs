using System;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management;

/// <summary>
/// Minimal placeholder for InventoryBatchEntity introduced in InventoryDocument PR.
/// Will be expanded with full properties when merging the full Asset Inventory API.
/// </summary>
public class InventoryBatchEntity : BaseEntity, IHardDeletable
{
    public bool MarkedForDeletion { get; set; } = false;
    public DateTime? MarkedForDeletionDate { get; set; }
}
