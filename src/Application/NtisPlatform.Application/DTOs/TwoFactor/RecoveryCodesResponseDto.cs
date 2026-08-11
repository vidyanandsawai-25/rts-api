namespace NtisPlatform.Application.DTOs.TwoFactor;

/// <summary>
/// Newly generated recovery codes, returned exactly once.
/// </summary>
public class RecoveryCodesResponseDto
{
    public IReadOnlyCollection<string> RecoveryCodes { get; set; } = Array.Empty<string>();
}
