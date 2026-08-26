using Microsoft.Extensions.Options;
using NtisPlatform.Application.Options;
using NtisPlatform.Infrastructure.Services;

namespace NtisPlatform.Tests.Infrastructure.Services;

/// <summary>
/// Unit tests for TotpService. Uses a fixed timestamp throughout so assertions never depend on
/// waiting for the real 30-second TOTP window.
/// </summary>
public class TotpServiceTests
{
    private static readonly DateTimeOffset FixedTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static TotpService CreateService(int allowedDriftSteps = 1)
    {
        var options = Options.Create(new TwoFactorAuthenticationOptions
        {
            Issuer = "NtisPlatform",
            AllowedDriftSteps = allowedDriftSteps
        });
        return new TotpService(options);
    }

    [Fact]
    public void GenerateSecret_ReturnsNonEmptyBase32String()
    {
        var service = CreateService();

        var secret = service.GenerateSecret();

        Assert.NotEmpty(secret);
        Assert.All(secret, c => Assert.Contains(c, "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"));
    }

    [Fact]
    public void GenerateSecret_ReturnsDifferentValuesEachCall()
    {
        var service = CreateService();

        var first = service.GenerateSecret();
        var second = service.GenerateSecret();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void BuildAuthenticatorUri_ContainsExpectedComponents()
    {
        var service = CreateService();
        var secret = service.GenerateSecret();

        var uri = service.BuildAuthenticatorUri("NtisPlatform", "jdoe@example.com", secret);

        Assert.StartsWith("otpauth://totp/", uri);
        Assert.Contains($"secret={secret}", uri);
        Assert.Contains("issuer=NtisPlatform", uri);
        Assert.Contains("algorithm=SHA1", uri);
        Assert.Contains("digits=6", uri);
        Assert.Contains("period=30", uri);
    }

    [Fact]
    public void BuildAuthenticatorUri_UriEscapesIssuerAndAccountName()
    {
        var service = CreateService();
        var secret = service.GenerateSecret();

        var uri = service.BuildAuthenticatorUri("My Org & Co", "user name@example.com", secret);

        Assert.DoesNotContain(" ", uri.Split('?')[0]); // label portion is escaped
        Assert.Contains("issuer=My%20Org%20%26%20Co", uri);
    }

    [Fact]
    public void ValidateCode_WithCodeComputedAtSameTimestamp_ReturnsTrue()
    {
        var service = CreateService();
        var secret = service.GenerateSecret();

        var code = service.ComputeCode(secret, FixedTime);

        Assert.True(service.ValidateCode(secret, code, FixedTime));
    }

    [Fact]
    public void ValidateCode_WithWrongCode_ReturnsFalse()
    {
        var service = CreateService();
        var secret = service.GenerateSecret();

        var validCode = service.ComputeCode(secret, FixedTime);
        var wrongCode = validCode == "000000" ? "111111" : "000000";

        Assert.False(service.ValidateCode(secret, wrongCode, FixedTime));
    }

    [Fact]
    public void ValidateCode_WithinAllowedDrift_ReturnsTrue()
    {
        var service = CreateService(allowedDriftSteps: 1);
        var secret = service.GenerateSecret();

        var code = service.ComputeCode(secret, FixedTime);

        // One 30-second step later — still within the ±1 step drift window.
        Assert.True(service.ValidateCode(secret, code, FixedTime.AddSeconds(30)));
    }

    [Fact]
    public void ValidateCode_BeyondAllowedDrift_ReturnsFalse()
    {
        var service = CreateService(allowedDriftSteps: 1);
        var secret = service.GenerateSecret();

        var code = service.ComputeCode(secret, FixedTime);

        // Outside the broad device/server drift fallback window (e.g. 3 days later)
        Assert.False(service.ValidateCode(secret, code, FixedTime.AddDays(3)));
    }

    [Fact]
    public void ComputeCode_IsSixDigits()
    {
        var service = CreateService();
        var secret = service.GenerateSecret();

        var code = service.ComputeCode(secret, FixedTime);

        Assert.Equal(6, code.Length);
        Assert.All(code, c => Assert.True(char.IsDigit(c)));
    }
}
