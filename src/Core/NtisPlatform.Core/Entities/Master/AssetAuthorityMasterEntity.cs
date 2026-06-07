using System;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Master;

public class AssetAuthorityMasterEntity : BaseEntity, IHardDeletable
{
    public string AuthorityCode { get; set; } = string.Empty;
    public string AuthorityName { get; set; } = string.Empty;
    public string? State { get; set; }

    // IHardDeletable members
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
