using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

/// <summary>
/// A reusable certificate design that can be copied into a service certificate.
/// </summary>
public class RTSCertificateCoreTemplateMasterEntity : BaseEntity, IHardDeletable
{
    public string TemplateName { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? HeaderContent { get; set; }
    public string BodyContent { get; set; } = string.Empty;
    public string? FooterContent { get; set; }
    public string? DesignJson { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}
