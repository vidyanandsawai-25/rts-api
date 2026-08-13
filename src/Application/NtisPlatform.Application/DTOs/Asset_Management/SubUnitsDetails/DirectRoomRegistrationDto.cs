using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;

namespace NtisPlatform.Application.DTOs.Asset_Management.SubUnitsDetails;

public class DirectRoomRegistrationDto
{
    [Required(ErrorMessage = "AMS_DirectRoomRegistration_ParentAssetId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_DirectRoomRegistration_ParentAssetId_Required")]
    public int ParentAssetId { get; set; }

    [Required(ErrorMessage = "AMS_DirectRoomRegistration_FloorId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_DirectRoomRegistration_FloorId_Required")]
    public int FloorId { get; set; }

    public int? DepartmentId { get; set; }

    public RentInformationDto? RentInformation { get; set; }

    [Required(ErrorMessage = "AMS_DirectRoomRegistration_PropertyGroups_Required")]
    public List<PropertyGroupDto> PropertyGroups { get; set; } = new();
}

public class PropertyGroupDto
{
    [Required(ErrorMessage = "AMS_PropertyGroup_ConstructionYear_Required")]
    public string ConstructionYear { get; set; } = string.Empty;

    [Required(ErrorMessage = "AMS_PropertyGroup_ConstructionTypeId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_PropertyGroup_ConstructionTypeId_Required")]
    public int ConstructionTypeId { get; set; }

    [Required(ErrorMessage = "AMS_PropertyGroup_TypeOfUseId_Required")]
    [Range(1, int.MaxValue, ErrorMessage = "AMS_PropertyGroup_TypeOfUseId_Required")]
    public int TypeOfUseId { get; set; }

    public int? SubTypeOfUseId { get; set; }

    [Required(ErrorMessage = "AMS_PropertyGroup_Rooms_Required")]
    public List<RoomDetailDto> Rooms { get; set; } = new();
}
