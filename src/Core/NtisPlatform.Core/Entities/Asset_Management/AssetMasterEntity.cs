using System;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management;

/// <summary>
/// Minimal placeholder for AssetMasterEntity introduced in PR 7.
/// Will be expanded with full properties in PR 9.
/// </summary>
public class AssetMasterEntity : BaseEntity, IHardDeletable
{
    public bool MarkedForDeletion { get; set; } = false;
    public DateTime? MarkedForDeletionDate { get; set; }
    public int? AssetCategoryId { get; set; }
    public int? AssetTypeId { get; set; }
}
