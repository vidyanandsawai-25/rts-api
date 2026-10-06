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
    public string? DashboardUrl { get; set; }
    public string? MahaITTokenUrl { get; set; }
    public string? MahaITPushUrl { get; set; }
    public string? MahaITClientSecretKey { get; set; }
    public string? MahaITDepartmentCode { get; set; }
    public int? Division { get; set; }
    public int? District { get; set; }
    public int? Taluka { get; set; }
}
