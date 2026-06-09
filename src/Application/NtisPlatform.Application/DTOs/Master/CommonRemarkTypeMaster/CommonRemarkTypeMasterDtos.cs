using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.CommonRemarkTypeMaster;

public class CommonRemarkTypeMasterDtos : BaseDtos
{
    public string RemarkTypeName { get; set; } = string.Empty;
}

public class CreateCommonRemarkTypeMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "RemarkTypeName_Required")]
    [StringLength(100, ErrorMessage = "RemarkTypeName_MaxLen_100")]
    [RegularExpression(@"^[\p{IsDevanagari}A-Za-z\s.,-]+$",ErrorMessage = "RemarkTypeName_Invalid")]
    public string RemarkTypeName { get; set; } = string.Empty;
}

public class UpdateCommonRemarkTypeMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "RemarkTypeName_Required")]
    [StringLength(100, ErrorMessage = "RemarkTypeName_MaxLen_100")]
    [RegularExpression(@"^[\p{IsDevanagari}A-Za-z\s.,-]+$", ErrorMessage = "RemarkTypeName_Invalid")]
    public string RemarkTypeName { get; set; } = string.Empty;
}
