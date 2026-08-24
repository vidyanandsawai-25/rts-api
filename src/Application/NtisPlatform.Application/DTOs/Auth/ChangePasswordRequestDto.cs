using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Auth;

/// <summary>
/// Request DTO for authenticated self-service password change.
/// </summary>
public class ChangePasswordRequestDto
{
    /// <summary>
    /// Optional username or email (used when changing password from unauthenticated login screen).
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// The user's current plain-text password.
    /// </summary>
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// The desired new password.
    /// </summary>
    [Required(ErrorMessage = "New password is required")]
    [MaxLength(128, ErrorMessage = "Password cannot exceed 128 characters")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation of the new password. Must match NewPassword.
    /// </summary>
    [Required(ErrorMessage = "Confirm password is required")]
    [Compare(nameof(NewPassword), ErrorMessage = "New password and confirmation password do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
