using NtisPlatform.Application.DTOs.PasswordReset;

namespace NtisPlatform.Application.Interfaces;

/// <summary>
/// Canonical set of forgot-password OTP delivery methods, as returned by
/// <see cref="IPasswordResetService.GetAvailableMethodsAsync"/> and accepted by
/// <see cref="ForgotPasswordRequestDto.Method"/>.
/// </summary>
public static class ForgotPasswordMethodNames
{
    public const string Email = "Email";
    public const string Sms = "Sms";
    public const string Authenticator = "Authenticator";
}

/// <summary>
/// Self-service forgot-password flow: request an OTP, verify it for a short-lived reset token,
/// then use that token to set a new password. Gated end-to-end by the <c>2FALOGINFORFPASS</c>
/// SECURITY_AUTH config flag — when off, the flow reports itself unavailable rather than sending
/// an OTP, so organizations that require admin-assisted resets aren't exposed to a bypass path.
/// </summary>
public interface IPasswordResetService
{
    /// <summary>
    /// Looks up which OTP delivery methods (<see cref="ForgotPasswordMethodNames"/>) are actually
    /// usable for the given account, so the client can offer only real choices.
    /// </summary>
    Task<ForgotPasswordAvailableMethodsResponseDto> GetAvailableMethodsAsync(ForgotPasswordAvailableMethodsRequestDto request, CancellationToken cancellationToken = default);

    Task<ForgotPasswordResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default);

    Task<VerifyForgotPasswordOtpResponseDto> VerifyForgotPasswordOtpAsync(VerifyForgotPasswordOtpRequestDto request, CancellationToken cancellationToken = default);

    Task<ResetPasswordResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default);
}
