using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Asset_Management;

public class AssetTypeOfUseGroupDto : BaseDtos
{
    public string TypeOfUseGroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string GroupIcon { get; set; } = string.Empty;
    public bool IsFloorWiseRateApplicable { get; set; }
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}

public class CreateAssetTypeOfUseGroupDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AssetTypeOfUseGroup_GroupCode_Required")]
    [StringLength(10, ErrorMessage = "AssetTypeOfUseGroup_GroupCode_MaxLength")]
    public string TypeOfUseGroupCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetTypeOfUseGroup_GroupName_Required")]
    [StringLength(50, ErrorMessage = "AssetTypeOfUseGroup_GroupName_MaxLength")]
    public string GroupName { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetTypeOfUseGroup_GroupIcon_Required")]
    [StringLength(50, ErrorMessage = "AssetTypeOfUseGroup_GroupIcon_MaxLength")]
    public string GroupIcon { get; set; } = string.Empty;

    public bool IsFloorWiseRateApplicable { get; set; }
}

public class UpdateAssetTypeOfUseGroupDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AssetTypeOfUseGroup_GroupCode_Required")]
    [StringLength(10, ErrorMessage = "AssetTypeOfUseGroup_GroupCode_MaxLength")]
    public string TypeOfUseGroupCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetTypeOfUseGroup_GroupName_Required")]
    [StringLength(50, ErrorMessage = "AssetTypeOfUseGroup_GroupName_MaxLength")]
    public string GroupName { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetTypeOfUseGroup_GroupIcon_Required")]
    [StringLength(50, ErrorMessage = "AssetTypeOfUseGroup_GroupIcon_MaxLength")]
    public string GroupIcon { get; set; } = string.Empty;

    public bool IsFloorWiseRateApplicable { get; set; }
}
