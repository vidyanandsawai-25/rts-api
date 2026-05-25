using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

public class PropertyAssessmentDetailsEntity : BaseEntity, IHardDeletable
{
    public int PropertyId { get; set; }
    public bool MarkedForDeletion { get; set; } = false;
    public DateTime? MarkedForDeletionDate { get; set; }
    public virtual PropertyEntity? PropertyMast { get; set; }
}
