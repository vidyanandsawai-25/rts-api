using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class FloorGroupDto : BaseDtos
{
    public string FloorGroup { get; set; } = string.Empty;
}

public class CreateFloorGroupDto : CreateBaseDtos
{
    [Required(ErrorMessage = "FloorGroup_FloorGroup_Required")]
    [StringLength(50, ErrorMessage = "FloorGroup_FloorGroup_MaxLen_50")]
    public string FloorGroup { get; set; } = string.Empty;
}

public class UpdateFloorGroupDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "FloorGroup_FloorGroup_Required")]
    [StringLength(50, ErrorMessage = "FloorGroup_FloorGroup_MaxLen_50")]
    public string FloorGroup { get; set; } = string.Empty;
}
