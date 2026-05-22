using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;

public class AssetCategoryDto : BaseDtos
{
    public string? CategoryName { get; set; }
    public string? CategoryCode { get; set; }
    public string? Description { get; set; }
}

public class CreateAssetCategoryDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AssetCategory_CategoryName_Required")]
    [StringLength(200, ErrorMessage = "AssetCategory_CategoryName_MaxLengthExceeded_200")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "AssetCategory_CategoryName_Invalid")]
    public string? CategoryName { get; set; }

    [Required(ErrorMessage = "AssetCategory_CategoryCode_Required")]
    [StringLength(100, ErrorMessage = "AssetCategory_CategoryCode_MaxLengthExceeded_100")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "AssetCategory_CategoryCode_Invalid")]
    public string? CategoryCode { get; set; }

    [StringLength(500, ErrorMessage = "AssetCategory_Description_MaxLengthExceeded_500")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "AssetCategory_Description_Invalid")]
    public string? Description { get; set; }
}

public class UpdateAssetCategoryDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AssetCategory_CategoryName_Required")]
    [StringLength(200, ErrorMessage = "AssetCategory_CategoryName_MaxLengthExceeded_200")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "AssetCategory_CategoryName_Invalid")]
    public string? CategoryName { get; set; }

    [Required(ErrorMessage = "AssetCategory_CategoryCode_Required")]
    [StringLength(100, ErrorMessage = "AssetCategory_CategoryCode_MaxLengthExceeded_100")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "AssetCategory_CategoryCode_Invalid")]
    public string? CategoryCode { get; set; }

    [StringLength(500, ErrorMessage = "AssetCategory_Description_MaxLengthExceeded_500")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "AssetCategory_Description_Invalid")]
    public string? Description { get; set; }
}