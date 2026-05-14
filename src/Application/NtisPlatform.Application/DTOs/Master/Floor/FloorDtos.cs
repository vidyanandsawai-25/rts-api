using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;


public class FloorDto : BaseDtos
{
    public string FloorCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? SequenceNo { get; set; }
    public int? MaxFloorNo { get; set; }

}
public class CreateFloorDto: CreateBaseDtos
{
	//To use .Resx file for localization
    //[Required(ErrorMessageResourceType = typeof(Resources.ValidationMessages),ErrorMessageResourceName = "FloorID_Required")]
    //[StringLength(5,ErrorMessageResourceType = typeof(Resources.ValidationMessages),ErrorMessageResourceName = "FloorID_MaxLen_5")]

	//To use DB with catche for localization

    // AllowEmptyStrings = true: range-create passes empty template values;
    // the service transformer overwrites FloorCode with the generated range value.
    [Required(AllowEmptyStrings = true, ErrorMessage = "FloorCode_Required")]
    [StringLength(10, ErrorMessage = "FloorCode_MaxLen_10")]
    public string FloorCode { get; set; } = string.Empty;

    // AllowEmptyStrings = true: range-create passes empty template description;
    // the service transformer auto-generates description from the range value.
    [Required(AllowEmptyStrings = true, ErrorMessage = "Floor_Description_Required")]
    [StringLength(100, ErrorMessage = "Description_MaxLen_100")]
    public string Description { get; set; }
    public int? SequenceNo { get; set; }
    public int? MaxFloorNo { get; set; }
}

public class UpdateFloorDto :UpdateBaseDtos
{
    [Required(ErrorMessage = "FloorCode_Required")]
    [StringLength(10, ErrorMessage = "FloorCode_MaxLen_10")]
    public string FloorCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Floor_Description_Required")]
    [StringLength(100, ErrorMessage = "Description_MaxLen_100")]
    public string Description { get; set; }

    public int? SequenceNo { get; set; }

    public int? MaxFloorNo { get; set; }
}
