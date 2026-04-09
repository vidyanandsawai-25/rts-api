using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.OfficeMaster;

public class OfficeDto : BaseDtos
{
    public int Id { get; set; }
    public string? OfficeCode { get; set; }
    public string? OfficeName { get; set; }
    public string? Type { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Pincode { get; set; }
    public string? Phone { get; set; }
    public string? EmailId { get; set; }
    public int? OfficeIncharge { get; set; }
    public int? DesignationMasterId { get; set; }
    public DateTime? EstablishedDate { get; set; }
}

public class CreateOfficeDto : CreateBaseDtos
{
    [Required(ErrorMessage = "OfficeCode_Required")]
    [StringLength(50, ErrorMessage = "OfficeCode_MaxLen_50")]
    public string OfficeCode { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "OfficeName_Required")]
    [StringLength(200, ErrorMessage = "OfficeName_MaxLen_200")]
    public string OfficeName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Type_Required")]
    [StringLength(100, ErrorMessage = "Type_MaxLen_100")]
    public string Type { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Address_MaxLen_500")]
    public string? Address { get; set; }
    
    [Required(ErrorMessage = "City_Required")]
    [StringLength(100, ErrorMessage = "City_MaxLen_100")]
    public string City { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Pincode_Required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Pincode_Length_6")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Pincode_Invalid_Format")]
    public string Pincode { get; set; } = string.Empty;
    
    [StringLength(20, ErrorMessage = "Phone_MaxLen_20")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "Phone_Invalid_Format")]
    public string? Phone { get; set; }
    
    [EmailAddress(ErrorMessage = "Email_Invalid")]
    [StringLength(200, ErrorMessage = "Email_MaxLen_200")]
    public string? EmailId { get; set; }
    
    public int? OfficeIncharge { get; set; }
    public int? DesignationMasterId { get; set; }
    public DateTime? EstablishedDate { get; set; }
}

public class UpdateOfficeDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "OfficeCode_Required")]
    [StringLength(50, ErrorMessage = "OfficeCode_MaxLen_50")]
    public string OfficeCode { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "OfficeName_Required")]
    [StringLength(200, ErrorMessage = "OfficeName_MaxLen_200")]
    public string OfficeName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Type_Required")]
    [StringLength(100, ErrorMessage = "Type_MaxLen_100")]
    public string Type { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Address_MaxLen_500")]
    public string? Address { get; set; }
    
    [Required(ErrorMessage = "City_Required")]
    [StringLength(100, ErrorMessage = "City_MaxLen_100")]
    public string City { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Pincode_Required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Pincode_Length_6")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Pincode_Invalid_Format")]
    public string Pincode { get; set; } = string.Empty;
    
    [StringLength(20, ErrorMessage = "Phone_MaxLen_20")]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "Phone_Invalid_Format")]
    public string? Phone { get; set; }
    
    [EmailAddress(ErrorMessage = "Email_Invalid")]
    [StringLength(200, ErrorMessage = "Email_MaxLen_200")]
    public string? EmailId { get; set; }
    
    public int? OfficeIncharge { get; set; }
    public int? DesignationMasterId { get; set; }
    public DateTime? EstablishedDate { get; set; }
}