using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class OwnerTitleDto : BaseDtos
{
    public int Id { get; set; }
    public string OwnerTitle { get; set; } = string.Empty;
}

public class CreateOwnerTitleDto : CreateBaseDtos
{
    [Required(ErrorMessage = "OwnerTitle_Name_Required")]
    [StringLength(30, ErrorMessage = "OwnerTitle_Name_MaxLen_30")]
    public string OwnerTitle { get; set; } = string.Empty;
}

public class UpdateOwnerTitleDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "OwnerTitle_Name_Required")]
    [StringLength(30, ErrorMessage = "OwnerTitle_Name_MaxLen_30")]
    public string OwnerTitle { get; set; } = string.Empty;
}
