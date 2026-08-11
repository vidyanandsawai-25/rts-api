namespace NtisPlatform.Application.DTOs.PasswordReset;

/// <summary>
/// Response to a methods-lookup request. An empty <see cref="Methods"/> list collapses several
/// distinct cases (feature disabled, account not found, account has no usable channel) into one
/// generic outcome — same enumeration-safe posture as the rest of the forgot-password flow.
/// </summary>
public class ForgotPasswordAvailableMethodsResponseDto
{
    public bool Success { get; set; } = true;
    public string? Message { get; set; }

    /// <summary>Subset of "Email", "Sms", "Authenticator" — whichever are actually usable for this account.</summary>
    public List<string> Methods { get; set; } = new();

    /// <summary>Masked email (e.g. "jo***@example.com"), present only when "Email" is in <see cref="Methods"/>.</summary>
    public string? MaskedEmail { get; set; }

    /// <summary>Masked mobile number (e.g. "*******91"), present only when "Sms" is in <see cref="Methods"/>.</summary>
    public string? MaskedMobile { get; set; }
}
