using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.FieldRegistry;

public class FieldRegistryDto
{
    public string SchemaName { get; set; } = string.Empty;
}

public class FieldRegistryDetailsDto
{
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
}

public class FieldRegistryTableDetailsDto
{
    public string ColumnName { get; set; } = string.Empty;
}

public class FieldRegistryDetailsQueryParameters : BaseQueryParameters
{
    [Required(ErrorMessage = "SchemaName is required")]
    public string SchemaName { get; set; } = string.Empty;
}

public class FieldRegistryTableDetailsQueryParameters : BaseQueryParameters
{
    [Required(ErrorMessage = "SchemaName is required")]
    public string SchemaName { get; set; } = string.Empty;

    [Required(ErrorMessage = "TableName is required")]
    public string TableName { get; set; } = string.Empty;
}

public class FieldRegistryQueryParameters : BaseQueryParameters
{
    public string? UpdateCode { get; set; }
    public string? UpdateName { get; set; }
    public string? ReferenceTableName { get; set; }
    public string? Category { get; set; }
    public string? FieldName { get; set; }
}

public class CreateFieldRegistryDto
{
    [Required(ErrorMessage = "UpdateCode is required")]
    [StringLength(50, ErrorMessage = "UpdateCode cannot exceed 50 characters")]
    public string UpdateCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "UpdateName is required")]
    [StringLength(200, ErrorMessage = "UpdateName cannot exceed 200 characters")]
    public string UpdateName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "UpdateNameMarathi cannot exceed 200 characters")]
    public string UpdateNameMarathi { get; set; } = string.Empty;

    [Required(ErrorMessage = "ReferenceTableName is required")]
    [StringLength(200, ErrorMessage = "ReferenceTableName cannot exceed 200 characters")]
    public string ReferenceTableName { get; set; } = string.Empty;

    [Range(1, 9999, ErrorMessage = "DisplaySequence must be between 1 and 9999")]
    public int DisplaySequence { get; set; }

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
    public string? Category { get; set; }

    public bool IsApprovalRequired { get; set; }

    public bool IsActive { get; set; } = true;

    public int? CreatedBy { get; set; }

    [Required(ErrorMessage = "At least one field configuration is required")]
    [MinLength(1, ErrorMessage = "At least one field configuration is required")]
    public List<FieldRegistryFieldConfigDto> FieldConfigs { get; set; } = new();
}

public class FieldRegistryFieldConfigDto
{
    [Required(ErrorMessage = "FieldName is required")]
    [StringLength(100, ErrorMessage = "FieldName cannot exceed 100 characters")]
    public string FieldName { get; set; } = string.Empty;

    [Required(ErrorMessage = "DisplayName is required")]
    [StringLength(200, ErrorMessage = "DisplayName cannot exceed 200 characters")]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "DisplayNameMarathi cannot exceed 200 characters")]
    public string DisplayNameMarathi { get; set; } = string.Empty;

    [Required(ErrorMessage = "ControlType is required")]
    [StringLength(50, ErrorMessage = "ControlType cannot exceed 50 characters")]
    public string ControlType { get; set; } = string.Empty;

    [Required(ErrorMessage = "DataType is required")]
    [StringLength(50, ErrorMessage = "DataType cannot exceed 50 characters")]
    public string DataType { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Placeholder cannot exceed 500 characters")]
    public string? Placeholder { get; set; }

    public bool IsRequired { get; set; }

    public int? MaxLength { get; set; }

    [StringLength(500, ErrorMessage = "ValidationRegex cannot exceed 500 characters")]
    public string? ValidationRegex { get; set; }

    [StringLength(500, ErrorMessage = "DefaultValue cannot exceed 500 characters")]
    public string? DefaultValue { get; set; }

    [StringLength(500, ErrorMessage = "BindApi cannot exceed 500 characters")]
    public string? BindApi { get; set; }
}

public class FieldRegistryResponseDto
{
    public int MasterId { get; set; }
    public string UpdateCode { get; set; } = string.Empty;
    public string UpdateName { get; set; } = string.Empty;
    public string UpdateNameMarathi { get; set; } = string.Empty;
    public string ReferenceTableName { get; set; } = string.Empty;
    public int DisplaySequence { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public bool IsApprovalRequired { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedDate { get; set; }
    public int? CreatedBy { get; set; }
    public List<FieldRegistryFieldConfigResponseDto> FieldConfigs { get; set; } = new();
}

public class FieldRegistryFieldConfigResponseDto
{
    public int Id { get; set; }
    public int BulkUpdateMasterId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DisplayNameMarathi { get; set; } = string.Empty;
    public string ControlType { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string? Placeholder { get; set; }
    public bool IsRequired { get; set; }
    public int? MaxLength { get; set; }
    public string? ValidationRegex { get; set; }
    public string? DefaultValue { get; set; }
    public int SequenceNo { get; set; }
    public bool IsReadonly { get; set; }
    public string? BindApi { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedDate { get; set; }
    public int? CreatedBy { get; set; }
}

public class UpdateFieldRegistryDto
{
    [Required(ErrorMessage = "UpdateName is required")]
    [StringLength(200, ErrorMessage = "UpdateName cannot exceed 200 characters")]
    public string UpdateName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "UpdateNameMarathi cannot exceed 200 characters")]
    public string UpdateNameMarathi { get; set; } = string.Empty;

    [Required(ErrorMessage = "ReferenceTableName is required")]
    [StringLength(200, ErrorMessage = "ReferenceTableName cannot exceed 200 characters")]
    public string ReferenceTableName { get; set; } = string.Empty;

    [Range(1, 9999, ErrorMessage = "DisplaySequence must be between 1 and 9999")]
    public int DisplaySequence { get; set; }

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
    public string? Category { get; set; }

    public bool IsApprovalRequired { get; set; }

    public bool IsActive { get; set; } = true;

    public int? UpdatedBy { get; set; }

    [Required(ErrorMessage = "At least one field configuration is required")]
    [MinLength(1, ErrorMessage = "At least one field configuration is required")]
    public List<UpdateFieldRegistryFieldConfigDto> FieldConfigs { get; set; } = new();
}

public class UpdateFieldRegistryFieldConfigDto
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "FieldName is required")]
    [StringLength(100, ErrorMessage = "FieldName cannot exceed 100 characters")]
    public string FieldName { get; set; } = string.Empty;

    [Required(ErrorMessage = "DisplayName is required")]
    [StringLength(200, ErrorMessage = "DisplayName cannot exceed 200 characters")]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "DisplayNameMarathi cannot exceed 200 characters")]
    public string DisplayNameMarathi { get; set; } = string.Empty;

    [Required(ErrorMessage = "ControlType is required")]
    [StringLength(50, ErrorMessage = "ControlType cannot exceed 50 characters")]
    public string ControlType { get; set; } = string.Empty;

    [Required(ErrorMessage = "DataType is required")]
    [StringLength(50, ErrorMessage = "DataType cannot exceed 50 characters")]
    public string DataType { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Placeholder cannot exceed 500 characters")]
    public string? Placeholder { get; set; }

    public bool IsRequired { get; set; }

    public int? MaxLength { get; set; }

    [StringLength(500, ErrorMessage = "ValidationRegex cannot exceed 500 characters")]
    public string? ValidationRegex { get; set; }

    [StringLength(500, ErrorMessage = "DefaultValue cannot exceed 500 characters")]
    public string? DefaultValue { get; set; }

    [StringLength(500, ErrorMessage = "BindApi cannot exceed 500 characters")]
    public string? BindApi { get; set; }
}
