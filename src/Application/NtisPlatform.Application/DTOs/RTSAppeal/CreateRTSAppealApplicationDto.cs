using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.RTSAppeal;

/// <summary>
/// Request payload for submitting an RTS First or Second Appeal.
/// Inherits from CreateBaseDtos for standardized audit and active state handling.
/// </summary>
public class CreateRTSAppealApplicationDto : CreateBaseDtos
{
    [Required(ErrorMessage = "Application number is required.")]
    [StringLength(100)]
    public string ApplicationNo { get; set; } = string.Empty;

    public int ApplicationId { get; set; }

    [StringLength(100)]
    public string AppealNo { get; set; } = string.Empty;

    [StringLength(50)]
    public string AppealLevel { get; set; } = string.Empty;

    [Required(ErrorMessage = "Appeal type is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid appeal type.")]
    public int AppealTypeId { get; set; }

    [StringLength(200)]
    public string? ApplicantName { get; set; }

    [Required(ErrorMessage = "Mobile number is required.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Mobile number must be a valid 10-digit number.")]
    public string MobileNo { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [StringLength(150)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Reason for Appeal (अपीलाचे कारण) is required.")]
    [StringLength(1000, ErrorMessage = "Reason for appeal cannot exceed 1000 characters.")]
    public string ReasonForAppeal { get; set; } = string.Empty;

    /// <summary>
    /// Mandatory reason explaining the delay when filing between 31 and 90 days (विलंब माफीचे कारण).
    /// </summary>
    [StringLength(1000, ErrorMessage = "Delay justification cannot exceed 1000 characters.")]
    public string? DelayJustification { get; set; }
}
