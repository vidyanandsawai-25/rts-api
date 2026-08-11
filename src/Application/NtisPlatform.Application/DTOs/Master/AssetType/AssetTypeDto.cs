using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;

public class AssetTypeDto : BaseDtos
{
    public int AssetCategoryId { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string? TypeNameLocal { get; set; }
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string CodeFormat { get; set; } = string.Empty;
    public int LastSequence { get; set; }
    public bool IsSubUnit { get; set; }
    public bool AllowUnitRegistration { get; set; }
    public bool AllowRoomRegistration { get; set; }
    public string? AssetWardNo { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
}

public class CreateAssetTypeDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AssetType_CategoryId_Required")]
    public int? AssetCategoryId { get; set; }

    [Required(ErrorMessage = "AssetType_TypeCode_Required")]
    [StringLength(50, ErrorMessage = "AssetType_TypeCode_MaxLengthExceeded_50")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "AssetType_TypeCode_Invalid")]
    public string? TypeCode { get; set; }

    [Required(ErrorMessage = "AssetType_TypeName_Required")]
    [StringLength(200, ErrorMessage = "AssetType_TypeName_MaxLengthExceeded_200")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "AssetType_TypeName_Invalid")]
    public string? TypeName { get; set; }

    [StringLength(200, ErrorMessage = "AssetType_TypeNameLocal_MaxLengthExceeded_200")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "AssetType_TypeNameLocal_Invalid")]
    public string? TypeNameLocal { get; set; }

    [StringLength(500, ErrorMessage = "AssetType_Description_MaxLengthExceeded_500")]
    [RegularExpression(@"^$|^(?!^0+$)(?!.* {2})(?!.*[\/,.\-()&]{2,})(?!.* $)[\p{L}\p{M}\p{N}](?:[\p{L}\p{M}\p{N} \/,.\-()&]*[\p{L}\p{M}\p{N}.)])?$", ErrorMessage = "AssetType_Description_Invalid")]
    public string? Description { get; set; }

    [StringLength(100, ErrorMessage = "AssetType_Icon_MaxLengthExceeded_100")]
    [RegularExpression(@"^[\p{L}\p{N} \-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "AssetType_Icon_Invalid")]
    public string? Icon { get; set; }

    [Required(ErrorMessage = "AssetType_CodeFormat_Required")]
    [StringLength(100, ErrorMessage = "AssetType_CodeFormat_MaxLengthExceeded_100")]
    [RegularExpression(@"^[\p{L}\p{N} &\-\/\{\}\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "AssetType_CodeFormat_Invalid")]
    public string? CodeFormat { get; set; }

    public bool IsSubUnit { get; set; }
    public bool AllowUnitRegistration { get; set; }
    public bool AllowRoomRegistration { get; set; }

    [StringLength(50, ErrorMessage = "AssetType_AssetWardNo_MaxLengthExceeded_50")]
    public string? AssetWardNo { get; set; }
}

public class UpdateAssetTypeDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AssetType_CategoryId_Required")]
    public int? AssetCategoryId { get; set; }

    [Required(ErrorMessage = "AssetType_TypeCode_Required")]
    [StringLength(50, ErrorMessage = "AssetType_TypeCode_MaxLengthExceeded_50")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "AssetType_TypeCode_Invalid")]
    public string? TypeCode { get; set; }

    [Required(ErrorMessage = "AssetType_TypeName_Required")]
    [StringLength(200, ErrorMessage = "AssetType_TypeName_MaxLengthExceeded_200")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "AssetType_TypeName_Invalid")]
    public string? TypeName { get; set; }

    [StringLength(200, ErrorMessage = "AssetType_TypeNameLocal_MaxLengthExceeded_200")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "AssetType_TypeNameLocal_Invalid")]
    public string? TypeNameLocal { get; set; }

    [StringLength(500, ErrorMessage = "AssetType_Description_MaxLengthExceeded_500")]
    [RegularExpression(@"^$|^(?!^0+$)(?!.* {2})(?!.*[\/,.\-()&]{2,})(?!.* $)[\p{L}\p{M}\p{N}](?:[\p{L}\p{M}\p{N} \/,.\-()&]*[\p{L}\p{M}\p{N}.)])?$", ErrorMessage = "AssetType_Description_Invalid")]
    public string? Description { get; set; }

    [StringLength(100, ErrorMessage = "AssetType_Icon_MaxLengthExceeded_100")]
    [RegularExpression(@"^[\p{L}\p{N} \-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "AssetType_Icon_Invalid")]
    public string? Icon { get; set; }

    [Required(ErrorMessage = "AssetType_CodeFormat_Required")]
    [StringLength(100, ErrorMessage = "AssetType_CodeFormat_MaxLengthExceeded_100")]
    [RegularExpression(@"^[\p{L}\p{N} &\-\/\{\}\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "AssetType_CodeFormat_Invalid")]
    public string? CodeFormat { get; set; }

    public bool IsSubUnit { get; set; }
    public bool AllowUnitRegistration { get; set; }
    public bool AllowRoomRegistration { get; set; }

    [StringLength(50, ErrorMessage = "AssetType_AssetWardNo_MaxLengthExceeded_50")]
    public string? AssetWardNo { get; set; }
}
