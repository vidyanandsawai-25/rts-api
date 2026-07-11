using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.PropertyCertificateType;

public class PropertyCertificateTypeDto : BaseDtos
{
    public int Id { get; set; }
    public string CertificateTypeName { get; set; } = string.Empty;
    public string CertificateTypeCode { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsProtected { get; set; }
}

public class CreatePropertyCertificateTypeDto : CreateBaseDtos
{
    [Required(ErrorMessage = "CertificateTypeName_Required")]
    [StringLength(100, ErrorMessage = "CertificateTypeName_MaxLen_100")]
    public string CertificateTypeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "CertificateTypeCode_Required")]
    [StringLength(50, ErrorMessage = "CertificateTypeCode_MaxLen_50")]
    public string CertificateTypeCode { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }
}

public class UpdatePropertyCertificateTypeDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "CertificateTypeName_Required")]
    [StringLength(100, ErrorMessage = "CertificateTypeName_MaxLen_100")]
    public string CertificateTypeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "CertificateTypeCode_Required")]
    [StringLength(50, ErrorMessage = "CertificateTypeCode_MaxLen_50")]
    public string CertificateTypeCode { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }
}
