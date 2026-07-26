using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master;

public class AssetConditionMasterDto : BaseDtos
{
    public string ConditionCategory { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string ConditionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? ConditionFactor { get; set; }
    public int? DisplayOrder { get; set; }
}

public class CreateAssetConditionMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AssetConditionMaster_ConditionCategory_Required")]
    [StringLength(20, ErrorMessage = "AssetConditionMaster_ConditionCategory_MaxLengthExceeded_20")]
    public string ConditionCategory { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetConditionMaster_CategoryId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AssetConditionMaster_CategoryId_Invalid")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "AssetConditionMaster_ConditionName_Required")]
    [StringLength(100, ErrorMessage = "AssetConditionMaster_ConditionName_MaxLengthExceeded_100")]
    public string ConditionName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "AssetConditionMaster_Description_MaxLengthExceeded_500")]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0.0001", "99999999999999.9999", ErrorMessage = "AssetConditionMaster_ConditionFactor_Range")]
    public decimal? ConditionFactor { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "AssetConditionMaster_DisplayOrder_Invalid")]
    public int? DisplayOrder { get; set; }
}

public class UpdateAssetConditionMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AssetConditionMaster_ConditionCategory_Required")]
    [StringLength(20, ErrorMessage = "AssetConditionMaster_ConditionCategory_MaxLengthExceeded_20")]
    public string ConditionCategory { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetConditionMaster_CategoryId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AssetConditionMaster_CategoryId_Invalid")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "AssetConditionMaster_ConditionName_Required")]
    [StringLength(100, ErrorMessage = "AssetConditionMaster_ConditionName_MaxLengthExceeded_100")]
    public string ConditionName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "AssetConditionMaster_Description_MaxLengthExceeded_500")]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0.0001", "99999999999999.9999", ErrorMessage = "AssetConditionMaster_ConditionFactor_Range")]
    public decimal? ConditionFactor { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "AssetConditionMaster_DisplayOrder_Invalid")]
    public int? DisplayOrder { get; set; }
}
