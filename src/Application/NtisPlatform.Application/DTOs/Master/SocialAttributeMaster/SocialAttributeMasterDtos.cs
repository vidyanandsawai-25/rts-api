using System.ComponentModel.DataAnnotations;
namespace NtisPlatform.Application.DTOs.Master.SocialAttributeMaster;

public class SocialAttributeDto : BaseDtos
{
    public string SocialAttributeCode { get; set; } = string.Empty;

    public string SocialAttributeName { get; set; } = string.Empty;

    public string DataType { get; set; } = string.Empty;

    public string? Unit { get; set; }

    public int? DisplayOrder { get; set; }

    public int? ParentAttributeId { get; set; }

    public bool IsRequiredWhenParentTrue { get; set; }

    public bool IsDiscountApplicable { get; set; }
}
public class CreateSocialAttributeDto : CreateBaseDtos
{
    [Required(ErrorMessage = "Attribute code is required")]
    [MinLength(3, ErrorMessage = "Attribute code must be at least 3 characters long")]
    [MaxLength(100, ErrorMessage = "Attribute code cannot exceed 100 characters")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Attribute code can only contain alphanumeric characters, underscores, and hyphens")]
    public string SocialAttributeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Attribute name is required")]
    [MinLength(2, ErrorMessage = "Attribute name must be at least 2 characters long")]
    [MaxLength(200, ErrorMessage = "Attribute name cannot exceed 200 characters")]
    [RegularExpression(
        @"^[a-zA-Z0-9\s\-\(\)&'\.]+$", ErrorMessage = "Attribute name can only contain alphanumeric characters, spaces, hyphens, parentheses, apostrophes, dots, and ampersands")]
    public string SocialAttributeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Data type is required")]
    [MaxLength(30, ErrorMessage = "Data type cannot exceed 30 characters")]
    [RegularExpression(@"^(string|int|integer|decimal|boolean|bool|datetime|date|double|float|long|guid)$",ErrorMessage = "Invalid data type. Allowed types are: string, int, integer, decimal, boolean, bool, datetime, date, double, float, long, guid")]
    public string DataType { get; set; } = string.Empty;

    [MaxLength(50, ErrorMessage = "Unit cannot exceed 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\s\/%\-\.]*$", ErrorMessage = "Unit can only contain alphanumeric characters, spaces, forward slashes, percent signs, hyphens, and dots")]
    public string? Unit { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Display order must be greater than 0 ")]
    public int? DisplayOrder { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Parent Attribute ID must be greater than 0")]
    public int? ParentAttributeId { get; set; }

    public bool IsRequiredWhenParentTrue { get; set; } = false;

    public bool IsDiscountApplicable { get; set; } = false;
}

public class UpdateSocialAttributeDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "Attribute code is required")]
    [MinLength(3, ErrorMessage = "Attribute code must be at least 3 characters long")]
    [MaxLength(100, ErrorMessage = "Attribute code cannot exceed 100 characters")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Attribute code can only contain alphanumeric characters, underscores, and hyphens")]
    public string SocialAttributeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Attribute name is required")]
    [MinLength(2, ErrorMessage = "Attribute name must be at least 2 characters long")]
    [MaxLength(200, ErrorMessage = "Attribute name cannot exceed 200 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-\(\)&'\.]+$", ErrorMessage = "Attribute name can only contain alphanumeric characters, spaces, hyphens, parentheses, apostrophes, dots, and ampersands")]
    public string SocialAttributeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Data type is required")]
    [MaxLength(30, ErrorMessage = "Data type cannot exceed 30 characters")]
    [RegularExpression(@"^(string|int|integer|decimal|boolean|bool|datetime|date|double|float|long|guid)$", ErrorMessage = "Invalid data type. Allowed types are: string, int, integer, decimal, boolean, bool, datetime, date, double, float, long, guid")]
    public string DataType { get; set; } = string.Empty;

    [MaxLength(50, ErrorMessage = "Unit cannot exceed 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\s\/\-\.]*$", ErrorMessage = "Unit can only contain alphanumeric characters, spaces, forward slashes, hyphens, and dots")]
    public string? Unit { get; set; }

    [Range(1, 10000, ErrorMessage = "Display order must be between 1 and 10000")]
    public int? DisplayOrder { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Parent Attribute ID must be greater than 0")]
    public int? ParentAttributeId { get; set; }
    public bool IsRequiredWhenParentTrue { get; set; }
    public bool IsDiscountApplicable { get; set; }
}