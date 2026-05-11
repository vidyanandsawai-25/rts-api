using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;

namespace NtisPlatform.Application.DTOs.Master.RoomTypeMaster;

public class RoomTypeMasterDto : BaseDtos
{
    public string RoomTypeName { get; set; } = string.Empty;
    public string RoomTypeCode { get; set; } = string.Empty;
}

public class CreateRoomTypeMasterDto : CreateBaseDtos
{
    [Required(ErrorMessage = "RoomTypeName_Required")]
    [StringLength(100, ErrorMessage = "RoomTypeName_MaxLen_100")]
    public string RoomTypeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "RoomTypeCode_Required")]
    [StringLength(50, ErrorMessage = "RoomTypeCode_MaxLen_50")]
    public string RoomTypeCode { get; set; } = string.Empty;
}

public class UpdateRoomTypeMasterDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "RoomTypeName_Required")]
    [StringLength(100, ErrorMessage = "RoomTypeName_MaxLen_100")]
    public string RoomTypeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "RoomTypeCode_Required")]
    [StringLength(50, ErrorMessage = "RoomTypeCode_MaxLen_50")]
    public string RoomTypeCode { get; set; } = string.Empty;
}

public class RoomTypeMasterQueryParameters : BaseQueryParameters
{
    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? RoomTypeName { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    public string? RoomTypeCode { get; set; }

    [Filterable(FilterOperator.Equals)]
    public bool? IsActive { get; set; }
}
