namespace NtisPlatform.Application.DTOs.TwoFactor;

/// <summary>
/// Result of successfully enabling 2FA. Recovery codes are returned exactly once — the backend
/// never returns plaintext codes again after this response.
/// </summary>
public class EnableTwoFactorResponseDto
{
    public bool IsEnabled { get; set; }
    public IReadOnlyCollection<string> RecoveryCodes { get; set; } = Array.Empty<string>();
}
