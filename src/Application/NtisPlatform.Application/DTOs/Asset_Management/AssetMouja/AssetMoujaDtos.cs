using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Asset_Management;

public class AssetMoujaDto : BaseDtos
{
    public string MoujaNo { get; set; } = string.Empty;
    public string MoujaName { get; set; } = string.Empty;
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}

public class CreateAssetMoujaDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AssetMouja_MoujaNo_Required")]
    [StringLength(20, ErrorMessage = "AssetMouja_MoujaNo_MaxLength")]
    public string MoujaNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetMouja_MoujaName_Required")]
    [StringLength(100, ErrorMessage = "AssetMouja_MoujaName_MaxLength")]
    public string MoujaName { get; set; } = string.Empty;
}

public class UpdateAssetMoujaDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AssetMouja_MoujaNo_Required")]
    [StringLength(20, ErrorMessage = "AssetMouja_MoujaNo_MaxLength")]
    public string MoujaNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetMouja_MoujaName_Required")]
    [StringLength(100, ErrorMessage = "AssetMouja_MoujaName_MaxLength")]
    public string MoujaName { get; set; } = string.Empty;
}
