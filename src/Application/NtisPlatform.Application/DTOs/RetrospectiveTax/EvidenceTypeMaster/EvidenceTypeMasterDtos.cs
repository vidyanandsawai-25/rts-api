using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RetrospectiveTax.EvidenceTypeMaster;

public class EvidenceTypeMasterDto : BaseDtos
{
    public string EvidenceCode { get; set; } = string.Empty;
    public string EvidenceName { get; set; } = string.Empty;
    public bool IsCertificate { get; set; }
    public int DisplayOrder { get; set; }
}

public class CreateEvidenceTypeMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "EvidenceTypeMaster_EvidenceCode_Required")]
    [StringLength(50, ErrorMessage = "EvidenceTypeMaster_EvidenceCode_MaxLen_50")]
    public string EvidenceCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "EvidenceTypeMaster_EvidenceName_Required")]
    [StringLength(100, ErrorMessage = "EvidenceTypeMaster_EvidenceName_MaxLen_100")]
    public string EvidenceName { get; set; } = string.Empty;

    public bool IsCertificate { get; set; }
    public int DisplayOrder { get; set; }
}

public class UpdateEvidenceTypeMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "EvidenceTypeMaster_EvidenceCode_Required")]
    [StringLength(50, ErrorMessage = "EvidenceTypeMaster_EvidenceCode_MaxLen_50")]
    public string EvidenceCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "EvidenceTypeMaster_EvidenceName_Required")]
    [StringLength(100, ErrorMessage = "EvidenceTypeMaster_EvidenceName_MaxLen_100")]
    public string EvidenceName { get; set; } = string.Empty;

    public bool IsCertificate { get; set; }
    public int DisplayOrder { get; set; }
}
