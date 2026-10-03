using System.ComponentModel.DataAnnotations.Schema;

namespace NtisPlatform.Core.Entities.Master;

public class RTSAapleSarkarCredentialEntity : BaseEntity
{
    public int IntegrationId { get; set; } = 1;
    public int UlbId { get; set; }
    public int UlbDistrict { get; set; }
    public string ClientCode { get; set; } = string.Empty;
    public string ChecksumKey { get; set; } = string.Empty;
    public string EncryptionKey { get; set; } = string.Empty;
    public string EncryptionIV { get; set; } = string.Empty;
    public string? ServiceUrl { get; set; }
    public string? PortalBaseUrl { get; set; }
}
