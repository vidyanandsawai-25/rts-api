using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class TypeOfUseGroupCVDto : BaseDtos
{

    public string TypeOfUseGroupCVCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string GroupIcon { get; set; } = string.Empty;
    public bool IsFloorWiseRateApplicable { get; set; }
}

public class CreateTypeOfUseGroupCVDto : CreateBaseDtos
{
    [Required(ErrorMessage = "TypeOfUseGroupCV_Code_Required")]
    [StringLength(50, ErrorMessage = "TypeOfUseGroupCV_Code_MaxLen_50")]
    public string TypeOfUseGroupCVCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "TypeOfUseGroupCV_GroupName_Required")]
    [StringLength(100, ErrorMessage = "TypeOfUseGroupCV_GroupName_MaxLen_100")]
    public string GroupName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "TypeOfUseGroupCV_GroupIcon_MaxLen_100")]
    public string GroupIcon { get; set; } = string.Empty;

    public bool IsFloorWiseRateApplicable { get; set; }
}

public class UpdateTypeOfUseGroupCVDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "TypeOfUseGroupCV_Code_Required")]
    [StringLength(50, ErrorMessage = "TypeOfUseGroupCV_Code_MaxLen_50")]
    public string TypeOfUseGroupCVCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "TypeOfUseGroupCV_GroupName_Required")]
    [StringLength(100, ErrorMessage = "TypeOfUseGroupCV_GroupName_MaxLen_100")]
    public string GroupName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "TypeOfUseGroupCV_GroupIcon_MaxLen_100")]
    public string GroupIcon { get; set; } = string.Empty;

    public bool IsFloorWiseRateApplicable { get; set; }
}
