using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;

public class OwnershipTypeDto : BaseDtos
{
    public string? OwnershipTypeName { get; set; }
    public string? Description { get; set; }
}

public class CreateOwnershipTypeDto : CreateBaseDtos
{
    [Required(ErrorMessage = "OwnershipType_OwnershipTypeName_Required")]
    [StringLength(200, ErrorMessage = "OwnershipType_OwnershipTypeName_MaxLengthExceeded_200")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "OwnershipType_OwnershipTypeName_Invalid")]
    public string? OwnershipTypeName { get; set; }

    [StringLength(500, ErrorMessage = "OwnershipType_Description_MaxLengthExceeded_500")]
    [RegularExpression(@"^$|^(?!^0+$)(?!.* {2})(?!.*[\/,.\-()&]{2,})(?!.* $)[\p{L}\p{M}\p{N}](?:[\p{L}\p{M}\p{N} \/,.\-()&]*[\p{L}\p{M}\p{N}.)])?$", ErrorMessage = "OwnershipType_Description_Invalid")]
    public string? Description { get; set; }
}

public class UpdateOwnershipTypeDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "OwnershipType_OwnershipTypeName_Required")]
    [StringLength(200, ErrorMessage = "OwnershipType_OwnershipTypeName_MaxLengthExceeded_200")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "OwnershipType_OwnershipTypeName_Invalid")]
    public string? OwnershipTypeName { get; set; }

    [StringLength(500, ErrorMessage = "OwnershipType_Description_MaxLengthExceeded_500")]
    [RegularExpression(@"^$|^(?!^0+$)(?!.* {2})(?!.*[\/,.\-()&]{2,})(?!.* $)[\p{L}\p{M}\p{N}](?:[\p{L}\p{M}\p{N} \/,.\-()&]*[\p{L}\p{M}\p{N}.)])?$", ErrorMessage = "OwnershipType_Description_Invalid")]
    public string? Description { get; set; }
}