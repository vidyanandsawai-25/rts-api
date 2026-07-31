using System;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities.Master;

public class AssetOrganizationMasterEntity : BaseEntity, IHardDeletable
{
    public int AuthorityId { get; set; }
    public string OrganizationCode { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;

    // IHardDeletable members
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
