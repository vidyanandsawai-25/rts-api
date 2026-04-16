using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.PropertyCertificateType;

public class PropertyCertificateTypeDto : BaseDtos
{
    public int Id { get; set; }
    public string CertificateTypeName { get; set; } = string.Empty;
    public string CertificateTypeCode { get; set; } = string.Empty;
    public string FieldCode { get; set; } = string.Empty;
    public string SectionCode { get; set; } = string.Empty;
    public string DocumentTypeCode { get; set; } = string.Empty;
    public string? DisplayLabel { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsMandatory { get; set; }
}

public class CreatePropertyCertificateTypeDto : CreateBaseDtos
{
    [Required(ErrorMessage = "CertificateTypeName_Required")]
    [StringLength(100, ErrorMessage = "CertificateTypeName_MaxLen_100")]
    public string CertificateTypeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "CertificateTypeCode_Required")]
    [StringLength(50, ErrorMessage = "CertificateTypeCode_MaxLen_50")]
    public string CertificateTypeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "FieldCode_Required")]
    [StringLength(100, ErrorMessage = "FieldCode_MaxLen_100")]
    public string FieldCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "SectionCode_Required")]
    [StringLength(100, ErrorMessage = "SectionCode_MaxLen_100")]
    public string SectionCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "DocumentTypeCode_Required")]
    [StringLength(50, ErrorMessage = "DocumentTypeCode_MaxLen_50")]
    public string DocumentTypeCode { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "DisplayLabel_MaxLen_200")]
    public string? DisplayLabel { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsMandatory { get; set; }
}

public class UpdatePropertyCertificateTypeDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "CertificateTypeName_Required")]
    [StringLength(100, ErrorMessage = "CertificateTypeName_MaxLen_100")]
    public string CertificateTypeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "CertificateTypeCode_Required")]
    [StringLength(50, ErrorMessage = "CertificateTypeCode_MaxLen_50")]
    public string CertificateTypeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "FieldCode_Required")]
    [StringLength(100, ErrorMessage = "FieldCode_MaxLen_100")]
    public string FieldCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "SectionCode_Required")]
    [StringLength(100, ErrorMessage = "SectionCode_MaxLen_100")]
    public string SectionCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "DocumentTypeCode_Required")]
    [StringLength(50, ErrorMessage = "DocumentTypeCode_MaxLen_50")]
    public string DocumentTypeCode { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "DisplayLabel_MaxLen_200")]
    public string? DisplayLabel { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsMandatory { get; set; }
}
