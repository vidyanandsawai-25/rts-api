using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;


public class FloorDto : BaseDtos
{
    public int Id { get; set; } 
    public string FloorCode { get; set; } = string.Empty;
    public string Description { get; set; }
    public int? SequenceNo { get; set; }
    public int? MaxFloorNo { get; set; }

}
public class CreateFloorDto: CreateBaseDtos
{
	//To use .Resx file for localization 
    //[Required(ErrorMessageResourceType = typeof(Resources.ValidationMessages),ErrorMessageResourceName = "FloorID_Required")]
    //[StringLength(5,ErrorMessageResourceType = typeof(Resources.ValidationMessages),ErrorMessageResourceName = "FloorID_MaxLen_5")]
	
	//To use DB with catche for localization
    [Required(ErrorMessage = "FloorCode_Required")]
    [StringLength(5, ErrorMessage = "FloorCode_MaxLen_5")]
    public string FloorCode { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Description_MaxLen_100")]
    public string Description { get; set; }
    public int? SequenceNo { get; set; }
    public int? MaxFloorNo { get; set; }
}

public class UpdateFloorDto :UpdateBaseDtos
{
    [Required(ErrorMessage = "FloorCode_Required")]
    [StringLength(5, ErrorMessage = "FloorCode_MaxLen_5")]
    public string FloorCode { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Description_MaxLen_100")]
    public string Description { get; set; }

    public int? SequenceNo { get; set; }

    public int? MaxFloorNo { get; set; }
}
