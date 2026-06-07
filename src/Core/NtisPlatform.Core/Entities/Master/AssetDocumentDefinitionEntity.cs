using System;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Master;

public class AssetDocumentDefinitionEntity : BaseEntity, IHardDeletable
{
    public int AssetCategoryId { get; set; }
    public int? AssetTypeId { get; set; }
    public string DocumentCode { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
    public int MaxFileSizeMB { get; set; }
    public string AllowedExtensions { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    // IHardDeletable members
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
