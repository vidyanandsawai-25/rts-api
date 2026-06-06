using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;

public class SubZoneDetailsForCVDto : BaseDtos
{
  
    public int MoujaId { get; set; }
    public string SubZoneNo { get; set; } = string.Empty;
    public string SubZoneName { get; set; } = string.Empty;

    // Optional: Include Mouja info for display
    public string? MoujaName { get; set; }
}

public class CreateSubZoneDetailsForCVDto : CreateBaseDtos
{
    [Required(ErrorMessage = "SubZoneDetailsForCV_MoujaId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "SubZoneDetailsForCV_MoujaId_Invalid")]
    public int MoujaId { get; set; }

    [Required(ErrorMessage = "SubZoneDetailsForCV_SubZoneNo_Required")]
    [StringLength(20, ErrorMessage = "SubZoneDetailsForCV_SubZoneNo_MaxLen_20")]
    public string SubZoneNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "SubZoneDetailsForCV_SubZoneName_Required")]
    [StringLength(1000, ErrorMessage = "SubZoneDetailsForCV_SubZoneName_MaxLen_1000")]
    public string SubZoneName { get; set; } = string.Empty;
}

public class UpdateSubZoneDetailsForCVDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "SubZoneDetailsForCV_MoujaId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "SubZoneDetailsForCV_MoujaId_Invalid")]
    public int MoujaId { get; set; }

    [Required(ErrorMessage = "SubZoneDetailsForCV_SubZoneNo_Required")]
    [StringLength(20, ErrorMessage = "SubZoneDetailsForCV_SubZoneNo_MaxLen_20")]
    public string SubZoneNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "SubZoneDetailsForCV_SubZoneName_Required")]
    [StringLength(1000, ErrorMessage = "SubZoneDetailsForCV_SubZoneName_MaxLen_1000")]
    public string SubZoneName { get; set; } = string.Empty;
}
