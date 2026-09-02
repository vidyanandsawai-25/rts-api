using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

public class RTSIssuedCertificateEntity : BaseEntity, IHardDeletable
{
    public Guid CertificateGuid { get; set; } = Guid.NewGuid();
    public string CertificateNo { get; set; } = string.Empty;
    public int ApplicationId { get; set; }
    public int ServiceId { get; set; }
    public int CertificateServiceId { get; set; }
    public string? OfficerInputsJson { get; set; }
    public string MergedHtmlContent { get; set; } = string.Empty;
    public string? QrCodePayload { get; set; }
    public int IssuedByUserId { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public bool IsDigitallySigned { get; set; } = true;
    public string? DigitalSignatureInfo { get; set; }

    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }

    public virtual RTSApplicationDetailsEntity? Application { get; set; }
    public virtual RTSServiceEntity? Service { get; set; }
    public virtual RTSServiceCertificateMasterEntity? CertificateService { get; set; }
    public virtual UserEntity? IssuedByUser { get; set; }
}
