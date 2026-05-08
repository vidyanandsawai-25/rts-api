using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.CommonRemarkDetails;

public class CommonRemarkDetailsDtos : BaseDtos
{
    public int RemarkTypeId { get; set; }
    public string Remark { get; set; } = string.Empty;
}

public class CreateCommonRemarkDetailsDto : CreateBaseDtos
{
    [Required(ErrorMessage = "RemarkTypeId_Required")]
    public int RemarkTypeId { get; set; }

    [Required(ErrorMessage = "RemarkText_Required")]
    [StringLength(300, ErrorMessage = "RemarkText_MaxLen_300")]
    public string Remark { get; set; } = string.Empty;
}

public class UpdateCommonRemarkDetailsDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "RemarkTypeId_Required")]
    public int RemarkTypeId { get; set; }

    [Required(ErrorMessage = "RemarkText_Required")]
    [StringLength(300, ErrorMessage = "RemarkText_MaxLen_300")]
    public string Remark { get; set; } = string.Empty;
}
