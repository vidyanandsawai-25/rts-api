using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Report;

// ─── Read DTO ────────────────────────────────────────────────────────────────

public class ReportDefinitionDto : BaseDtos
{
    public string ReportCode { get; set; } = string.Empty;
    public string ReportName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TemplateFile { get; set; } = string.Empty;
    public string DataProviderCode { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

// ─── Create DTO ──────────────────────────────────────────────────────────────

public class CreateReportDefinitionDto : CreateBaseDtos
{
    [Required(ErrorMessage = "Report_ReportCode_Required")]
    [StringLength(100, ErrorMessage = "Report_ReportCode_MaxLen_100")]
    public string ReportCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Report_ReportName_Required")]
    [StringLength(200, ErrorMessage = "Report_ReportName_MaxLen_200")]
    public string ReportName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Report_Category_MaxLen_100")]
    public string Category { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Report_Description_MaxLen_500")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Report_TemplateFile_Required")]
    [StringLength(200, ErrorMessage = "Report_TemplateFile_MaxLen_200")]
    public string TemplateFile { get; set; } = string.Empty;

    [Required(ErrorMessage = "Report_DataProviderCode_Required")]
    [StringLength(100, ErrorMessage = "Report_DataProviderCode_MaxLen_100")]
    public string DataProviderCode { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}

// ─── Update DTO ──────────────────────────────────────────────────────────────

public class UpdateReportDefinitionDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "Report_ReportCode_Required")]
    [StringLength(100, ErrorMessage = "Report_ReportCode_MaxLen_100")]
    public string ReportCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Report_ReportName_Required")]
    [StringLength(200, ErrorMessage = "Report_ReportName_MaxLen_200")]
    public string ReportName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Report_Category_MaxLen_100")]
    public string Category { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Report_Description_MaxLen_500")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Report_TemplateFile_Required")]
    [StringLength(200, ErrorMessage = "Report_TemplateFile_MaxLen_200")]
    public string TemplateFile { get; set; } = string.Empty;

    [Required(ErrorMessage = "Report_DataProviderCode_Required")]
    [StringLength(100, ErrorMessage = "Report_DataProviderCode_MaxLen_100")]
    public string DataProviderCode { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}

// ─── Query Parameters ─────────────────────────────────────────────────────────

public class ReportDefinitionQueryParameters : BaseQueryParameters
{
    [Filterable]
    [Searchable]
    [Sortable]
    public string? ReportCode { get; set; }

    [Filterable(FilterOperator.Contains)]
    [Searchable]
    [Sortable]
    public string? ReportName { get; set; }

    [Filterable]
    [Searchable]
    [Sortable]
    public string? Category { get; set; }

    [Filterable]
    [Sortable]
    public bool? IsActive { get; set; }
}

// ─── Async report request (from ntis-ui) ─────────────────────────────────────

/// <summary>Submit a report for async generation. Returns a request id to poll.</summary>
public class ReportRequestSubmitDto
{
    [Required(ErrorMessage = "Report_ReportCode_Required")]
    [StringLength(100, ErrorMessage = "Report_ReportCode_MaxLen_100")]
    public string ReportCode { get; set; } = string.Empty;

    public Dictionary<string, string> Parameters { get; set; } = new();
}

/// <summary>Result of submitting a report request.</summary>
public class ReportRequestSubmitResultDto
{
    public Guid ReportRequestId { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>Status projection returned to the UI while it polls.</summary>
public class ReportRequestStatusDto
{
    public Guid ReportRequestId { get; set; }
    public string ReportCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime? StartedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? ErrorMessage { get; set; }
    public bool DownloadAvailable { get; set; }
    public int RequestedByUserId { get; set; }
}

/// <summary>Body sent by the ntis-report worker after writing a terminal status to the queue DB,
/// so the platform hub can push the change to the report owner's browser.</summary>
public class WorkerNotifyDto
{
    public Guid ReportRequestId { get; set; }
    public string Status { get; set; } = string.Empty;
}

// ─── Report Parameter Definition DTOs ────────────────────────────────────────

public class ReportParameterDefinitionDto : BaseDtos
{
    public int ReportDefinitionId { get; set; }
    public string ParameterKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string ParameterType { get; set; } = string.Empty;
    public string? CascadeFromKey { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
}

public class CreateReportParameterDefinitionDto : CreateBaseDtos
{
    [Required(ErrorMessage = "ReportParam_ReportDefinitionId_Required")]
    public int ReportDefinitionId { get; set; }

    [Required(ErrorMessage = "ReportParam_ParameterKey_Required")]
    [StringLength(100, ErrorMessage = "ReportParam_ParameterKey_MaxLen")]
    public string ParameterKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "ReportParam_Label_Required")]
    [StringLength(200, ErrorMessage = "ReportParam_Label_MaxLen")]
    public string Label { get; set; } = string.Empty;

    [Required(ErrorMessage = "ReportParam_ParameterType_Required")]
    [StringLength(50, ErrorMessage = "ReportParam_ParameterType_MaxLen")]
    public string ParameterType { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "ReportParam_CascadeFromKey_MaxLen")]
    public string? CascadeFromKey { get; set; }

    public bool IsRequired { get; set; } = true;
    public int SortOrder { get; set; }
}

public class UpdateReportParameterDefinitionDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "ReportParam_ParameterKey_Required")]
    [StringLength(100, ErrorMessage = "ReportParam_ParameterKey_MaxLen")]
    public string ParameterKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "ReportParam_Label_Required")]
    [StringLength(200, ErrorMessage = "ReportParam_Label_MaxLen")]
    public string Label { get; set; } = string.Empty;

    [Required(ErrorMessage = "ReportParam_ParameterType_Required")]
    [StringLength(50, ErrorMessage = "ReportParam_ParameterType_MaxLen")]
    public string ParameterType { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "ReportParam_CascadeFromKey_MaxLen")]
    public string? CascadeFromKey { get; set; }

    public bool IsRequired { get; set; } = true;
    public int SortOrder { get; set; }
}

public class ReportParameterDefinitionQueryParameters : BaseQueryParameters
{
    [Filterable]
    public int? ReportDefinitionId { get; set; }

    [Filterable]
    [Searchable]
    [Sortable]
    public string? ParameterKey { get; set; }

    [Filterable]
    public string? ParameterType { get; set; }

    [Filterable]
    public bool? IsActive { get; set; }
}
