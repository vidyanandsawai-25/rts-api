using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class OwnerTypeDto : BaseDtos
{
    public int Id { get; set; }
    public string OwnerType { get; set; } = string.Empty;
}

public class CreateOwnerTypeDto : CreateBaseDtos
{
    [Required(ErrorMessage = "OwnerType_Name_Required")]
    [StringLength(30, ErrorMessage = "OwnerType_Name_MaxLen_30")]
    public string OwnerType { get; set; } = string.Empty;
}

public class UpdateOwnerTypeDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "OwnerType_Name_Required")]
    [StringLength(30, ErrorMessage = "OwnerType_Name_MaxLen_30")]
    public string OwnerType { get; set; } = string.Empty;
}