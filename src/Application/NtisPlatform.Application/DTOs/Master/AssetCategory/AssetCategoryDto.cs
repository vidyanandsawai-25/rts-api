using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;

public class AssetCategoryDto : BaseDtos
{
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ValuationType { get; set; } = "GENERIC";
    public bool IsMovable { get; set; }
    public bool HasFloorDetails { get; set; }
    public bool HasInventory { get; set; }
    public bool IsInventoryMandatory { get; set; }
    public bool HasLegalCompliance { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}

public class CreateAssetCategoryDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AssetCategory_CategoryName_Required")]
    [StringLength(200, ErrorMessage = "AssetCategory_CategoryName_MaxLengthExceeded_200")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "AssetCategory_CategoryName_Invalid")]
    public string CategoryName { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetCategory_CategoryCode_Required")]
    [StringLength(100, ErrorMessage = "AssetCategory_CategoryCode_MaxLengthExceeded_100")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "AssetCategory_CategoryCode_Invalid")]
    public string CategoryCode { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "AssetCategory_Description_MaxLengthExceeded_500")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "AssetCategory_Description_Invalid")]
    public string? Description { get; set; }

    [StringLength(20, ErrorMessage = "AssetCategory_ValuationType_MaxLengthExceeded_20")]
    public string ValuationType { get; set; } = "GENERIC";

    public bool IsMovable { get; set; } = false;
    public bool HasFloorDetails { get; set; } = false;
    public bool HasInventory { get; set; } = false;
    public bool IsInventoryMandatory { get; set; } = false;
    public bool HasLegalCompliance { get; set; } = false;
}

public class UpdateAssetCategoryDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AssetCategory_CategoryName_Required")]
    [StringLength(200, ErrorMessage = "AssetCategory_CategoryName_MaxLengthExceeded_200")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "AssetCategory_CategoryName_Invalid")]
    public string CategoryName { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetCategory_CategoryCode_Required")]
    [StringLength(100, ErrorMessage = "AssetCategory_CategoryCode_MaxLengthExceeded_100")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "AssetCategory_CategoryCode_Invalid")]
    public string CategoryCode { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "AssetCategory_Description_MaxLengthExceeded_500")]
    [RegularExpression(@"^[\p{L}\p{N} \.,&\-\u0900-\u097F\u0980-\u09FF]*$", ErrorMessage = "AssetCategory_Description_Invalid")]
    public string? Description { get; set; }

    [StringLength(20, ErrorMessage = "AssetCategory_ValuationType_MaxLengthExceeded_20")]
    public string ValuationType { get; set; } = "GENERIC";

    public bool IsMovable { get; set; } = false;
    public bool HasFloorDetails { get; set; } = false;
    public bool HasInventory { get; set; } = false;
    public bool IsInventoryMandatory { get; set; } = false;
    public bool HasLegalCompliance { get; set; } = false;
}
