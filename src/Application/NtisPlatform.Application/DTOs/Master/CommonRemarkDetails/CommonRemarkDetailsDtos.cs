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
    [Range(1, int.MaxValue, ErrorMessage = "RemarkTypeId_Invalid")]
    public int RemarkTypeId { get; set; }

    [Required(ErrorMessage = "RemarkText_Required")]
    [StringLength(300, ErrorMessage = "RemarkText_MaxLen_300")]
    [RegularExpression(@"^[\p{IsDevanagari}A-Za-z0-9\s.,-]+$", ErrorMessage = "Remark_Invalid")]
    public string Remark { get; set; } = string.Empty;
}

public class UpdateCommonRemarkDetailsDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "RemarkTypeId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "RemarkTypeId_Invalid")]
    public int RemarkTypeId { get; set; }

    [Required(ErrorMessage = "RemarkText_Required")]
    [StringLength(300, ErrorMessage = "RemarkText_MaxLen_300")]
    [RegularExpression(@"^[\p{IsDevanagari}A-Za-z0-9\s.,-]+$",ErrorMessage = "Remark_Invalid")]
    public string Remark { get; set; } = string.Empty;
}
