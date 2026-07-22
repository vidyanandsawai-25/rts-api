using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;

public class OwningDepartmentDto : BaseDtos
{
    public string OwningDepartmentName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreateOwningDepartmentDto : CreateBaseDtos
{
    [Required(ErrorMessage = "OwningDepartment_OwningDepartmentName_Required")]
    [StringLength(200, ErrorMessage = "OwningDepartment_OwningDepartmentName_MaxLengthExceeded_200")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "OwningDepartment_OwningDepartmentName_Invalid")]
    public string? OwningDepartmentName { get; set; }

    [StringLength(500, ErrorMessage = "OwningDepartment_Description_MaxLengthExceeded_500")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "OwningDepartment_Description_Invalid")]
    public string? Description { get; set; }
}

public class UpdateOwningDepartmentDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "OwningDepartment_OwningDepartmentName_Required")]
    [StringLength(200, ErrorMessage = "OwningDepartment_OwningDepartmentName_MaxLengthExceeded_200")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "OwningDepartment_OwningDepartmentName_Invalid")]
    public string? OwningDepartmentName { get; set; }

    [StringLength(500, ErrorMessage = "OwningDepartment_Description_MaxLengthExceeded_500")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "OwningDepartment_Description_Invalid")]
    public string? Description { get; set; }
}
