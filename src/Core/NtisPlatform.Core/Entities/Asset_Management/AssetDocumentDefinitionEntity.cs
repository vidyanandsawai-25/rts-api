using System;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Asset_Management;

public class AssetDocumentDefinitionEntity : BaseEntity, IHardDeletable
{
    public int? AssetCategoryId { get; set; }
    public int? AssetTypeId { get; set; }
    public string DocumentCode { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
    public int? DisplayOrder { get; set; }

    public virtual AssetCategoryEntity? AssetCategory { get; set; }
    public virtual AssetTypeEntity? AssetType { get; set; }

    // IHardDeletable members
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
