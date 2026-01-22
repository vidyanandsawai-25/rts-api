using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs;


public class FloorDto 
{
    public string FloorID { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? SequenceNo { get; set; }
    public string? DescriptionEnglish { get; set; }
    public int? MaxFloorNo { get; set; }
    public bool IsActive { get; set; }

    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

}
public class CreateFloorDto
{
	//To use .Resx file for localization 
    //[Required(ErrorMessageResourceType = typeof(Resources.ValidationMessages),ErrorMessageResourceName = "FloorID_Required")]
    //[StringLength(5,ErrorMessageResourceType = typeof(Resources.ValidationMessages),ErrorMessageResourceName = "FloorID_MaxLen_5")]
	
	//To use DB with catche for localization
    [Required(ErrorMessage = "FloorID_Required")]
    [StringLength(5, ErrorMessage = "FloorID_MaxLen_5")]
    public string FloorID { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Description_MaxLen_100")]
    public string? Description { get; set; }

    public int? SequenceNo { get; set; }

    [StringLength(100, ErrorMessage = "DescriptionEnglish_MaxLen_100")]
    public string? DescriptionEnglish { get; set; }

    public int? MaxFloorNo { get; set; }
    public bool IsActive { get; set; }
    public int? CreatedBy { get; set; }
}

public class UpdateFloorDto
{
    [Required(ErrorMessage = "FloorID_Required")]
    [StringLength(5, ErrorMessage = "FloorID_MaxLen_5")]
    public string FloorID { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Description_MaxLen_100")]
    public string? Description { get; set; }

    public int? SequenceNo { get; set; }

    [StringLength(100, ErrorMessage = "DescriptionEnglish_MaxLen_100")]
    public string? DescriptionEnglish { get; set; }

    public int? MaxFloorNo { get; set; }
    public bool IsActive { get; set; }
    public int? UpdatedBy { get; set; }
}
