using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

public class RTSCertificateTemplateMasterEntity : BaseEntity, IHardDeletable
{
    public int ServiceId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public string? HeaderContent { get; set; }
    public string BodyContent { get; set; } = string.Empty;
    public string? FooterContent { get; set; }
    public string? DefaultConditionsJson { get; set; }
    public string? OfficerFieldsConfigJson { get; set; }

    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }

    public virtual RTSServiceEntity? Service { get; set; }
    public virtual ICollection<RTSIssuedCertificateEntity> IssuedCertificates { get; set; } = new List<RTSIssuedCertificateEntity>();
}
