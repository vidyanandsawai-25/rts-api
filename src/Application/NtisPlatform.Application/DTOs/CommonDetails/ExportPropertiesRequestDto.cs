using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.CommonDetails;

public class ExportPropertiesRequestDto
{
    public int? WardId { get; set; }
    public string? FromPropertyNo { get; set; }
    public string? ToPropertyNo { get; set; }
    public string? PropertyNo { get; set; }
    public string? Wing { get; set; }

    [Required(ErrorMessage = "CommonDetails_UpdateCode_Required")]
    public string UpdateCode { get; set; } = string.Empty;
}
