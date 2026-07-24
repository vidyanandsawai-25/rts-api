using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Asset_Management;

public class AssetSubZoneDetailsForCVDto : BaseDtos
{
    public int MoujaId { get; set; }
    public string SubZoneNo { get; set; } = string.Empty;
    public string SubZoneName { get; set; } = string.Empty;
    public bool MarkedForDeletion { get; set; }
    public DateTime? MarkedForDeletionDate { get; set; }
}

public class CreateAssetSubZoneDetailsForCVDto : CreateBaseDtos
{
    [Required(ErrorMessage = "AssetSubZoneDetailsForCV_MoujaId_Required")]
    public int MoujaId { get; set; }

    [Required(ErrorMessage = "AssetSubZoneDetailsForCV_SubZoneNo_Required")]
    [StringLength(20, ErrorMessage = "AssetSubZoneDetailsForCV_SubZoneNo_MaxLength")]
    public string SubZoneNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetSubZoneDetailsForCV_SubZoneName_Required")]
    [StringLength(1000, ErrorMessage = "AssetSubZoneDetailsForCV_SubZoneName_MaxLength")]
    public string SubZoneName { get; set; } = string.Empty;
}

public class UpdateAssetSubZoneDetailsForCVDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "AssetSubZoneDetailsForCV_MoujaId_Required")]
    public int MoujaId { get; set; }

    [Required(ErrorMessage = "AssetSubZoneDetailsForCV_SubZoneNo_Required")]
    [StringLength(20, ErrorMessage = "AssetSubZoneDetailsForCV_SubZoneNo_MaxLength")]
    public string SubZoneNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "AssetSubZoneDetailsForCV_SubZoneName_Required")]
    [StringLength(1000, ErrorMessage = "AssetSubZoneDetailsForCV_SubZoneName_MaxLength")]
    public string SubZoneName { get; set; } = string.Empty;
}
