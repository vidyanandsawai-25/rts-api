using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.CommonDetails;

public class BulkUpdateRequestDto
{
    [Required(ErrorMessage = "BulkUpdate_UpdateCode_Required")]
    public string UpdateCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "BulkUpdate_PropertyIds_Required")]
    [MinLength(1, ErrorMessage = "BulkUpdate_PropertyIds_Required")]
    public List<long> PropertyIds { get; set; } = [];

    [Required(ErrorMessage = "BulkUpdate_UpdateData_Required")]
    [MinLength(1, ErrorMessage = "BulkUpdate_UpdateData_Required")]
    public Dictionary<string, object?> UpdateData { get; set; } = [];

    public string? Remarks { get; set; }
}
