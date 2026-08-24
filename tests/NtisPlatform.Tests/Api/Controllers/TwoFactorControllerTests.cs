using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.TwoFactor;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Tests.Api.Controllers;

/// <summary>
/// Unit tests for TwoFactorController — HTTP status mapping and authorization boundary
/// (every action operates on the caller's own user id from the NameIdentifier claim; there is
/// no request parameter through which another user's id could be supplied).
/// </summary>
public class TwoFactorControllerTests
{
    private readonly Mock<ITwoFactorAuthenticationService> _serviceMock = new();
    private readonly TwoFactorController _controller;

    public TwoFactorControllerTests()
    {
        _controller = new TwoFactorController(_serviceMock.Object, new Mock<ILogger<TwoFactorController>>().Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, "42") }, "test"))
                }
            }
        };
    }

    [Fact]
    public async Task GetStatus_ReturnsOkWithStatusForCurrentUser()
    {
        _serviceMock.Setup(x => x.GetStatusAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TwoFactorStatusResponseDto { IsEnabled = true, RecoveryCodesRemaining = 5, HasAuthenticatorKey = true });

        var result = await _controller.GetStatus(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<TwoFactorStatusResponseDto>(ok.Value);
        Assert.True(dto.IsEnabled);
        _serviceMock.Verify(x => x.GetStatusAsync(42, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Setup_WhenSuccessful_ReturnsOk()
    {
        _serviceMock.Setup(x => x.BeginSetupAsync(42, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TwoFactorOperationResult<TwoFactorSetupResponseDto>.Succeeded(new TwoFactorSetupResponseDto()));

        var result = await _controller.Setup(CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Setup_WhenAlreadyEnabled_ReturnsConflict()
    {
        _serviceMock.Setup(x => x.BeginSetupAsync(42, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TwoFactorOperationResult<TwoFactorSetupResponseDto>.Failed(TwoFactorOperationError.AlreadyEnabled));

        var result = await _controller.Setup(CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Setup_WhenNoEmailOnFile_ReturnsConflict()
    {
        // Regression test: Setup used to hardcode every failure as "already enabled" — now maps
        // via the shared error switch, so a distinct failure reason gets a distinct message.
        _serviceMock.Setup(x => x.BeginSetupAsync(42, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TwoFactorOperationResult<TwoFactorSetupResponseDto>.Failed(TwoFactorOperationError.EmailNotOnFile));

        var result = await _controller.Setup(CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Enable_WithValidCode_ReturnsOkWithPendingEmailVerification()
    {
        // Self-service "enable" no longer enables immediately: a valid TOTP code only unlocks a
        // one-time code emailed to the caller's own registered address.
        var response = new TwoFactorEmailVerificationPendingResponseDto { MaskedEmail = "jd***@example.com" };
        _serviceMock.Setup(x => x.EnableAsync(42, "123456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TwoFactorOperationResult<TwoFactorEmailVerificationPendingResponseDto>.Succeeded(response));

        var result = await _controller.Enable(new EnableTwoFactorRequestDto { Code = "123456" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
    }

    [Fact]
    public async Task Enable_WithInvalidCode_ReturnsBadRequest()
    {
        _serviceMock.Setup(x => x.EnableAsync(42, "000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TwoFactorOperationResult<TwoFactorEmailVerificationPendingResponseDto>.Failed(TwoFactorOperationError.InvalidCode));

        var result = await _controller.Enable(new EnableTwoFactorRequestDto { Code = "000000" }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Enable_WithMissingModelState_ReturnsBadRequest()
    {
        _controller.ModelState.AddModelError("Code", "Code is required");

        var result = await _controller.Enable(new EnableTwoFactorRequestDto { Code = "" }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        _serviceMock.Verify(x => x.EnableAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmEmail_WithValidCode_ReturnsOkWithRecoveryCodes()
    {
        var response = new EnableTwoFactorResponseDto { IsEnabled = true, RecoveryCodes = new[] { "AAAAA-BBBBB" } };
        _serviceMock.Setup(x => x.ConfirmEnableAsync(42, "482913", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TwoFactorOperationResult<EnableTwoFactorResponseDto>.Succeeded(response));

        var result = await _controller.ConfirmEmail(new EnableTwoFactorRequestDto { Code = "482913" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
    }

    [Fact]
    public async Task ConfirmEmail_WithInvalidCode_ReturnsBadRequest()
    {
        _serviceMock.Setup(x => x.ConfirmEnableAsync(42, "000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TwoFactorOperationResult<EnableTwoFactorResponseDto>.Failed(TwoFactorOperationError.InvalidCode));

        var result = await _controller.ConfirmEmail(new EnableTwoFactorRequestDto { Code = "000000" }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ConfirmEmail_WithMissingModelState_ReturnsBadRequest()
    {
        _controller.ModelState.AddModelError("Code", "Code is required");

        var result = await _controller.ConfirmEmail(new EnableTwoFactorRequestDto { Code = "" }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        _serviceMock.Verify(x => x.ConfirmEnableAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Disable_WhenSuccessful_ReturnsNoContent()
    {
        _serviceMock.Setup(x => x.DisableAsync(42, "123456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TwoFactorOperationResult<bool>.Succeeded(true));

        var result = await _controller.Disable(new TwoFactorCodeRequestDto { Code = "123456" }, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Disable_WhenNotEnabled_ReturnsConflict()
    {
        _serviceMock.Setup(x => x.DisableAsync(42, "123456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TwoFactorOperationResult<bool>.Failed(TwoFactorOperationError.NotEnabled));

        var result = await _controller.Disable(new TwoFactorCodeRequestDto { Code = "123456" }, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task Reset_WhenSuccessful_ReturnsOkWithNewSetupPayload()
    {
        var setup = new TwoFactorSetupResponseDto { AuthenticatorUri = "otpauth://totp/reset" };
        _serviceMock.Setup(x => x.ResetAsync(42, "123456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TwoFactorOperationResult<TwoFactorSetupResponseDto>.Succeeded(setup));

        var result = await _controller.Reset(new TwoFactorCodeRequestDto { Code = "123456" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(setup, ok.Value);
    }

    [Fact]
    public async Task RegenerateRecoveryCodes_WhenSuccessful_ReturnsOk()
    {
        var response = new RecoveryCodesResponseDto { RecoveryCodes = new[] { "CCCCC-DDDDD" } };
        _serviceMock.Setup(x => x.RegenerateRecoveryCodesAsync(42, "123456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TwoFactorOperationResult<RecoveryCodesResponseDto>.Succeeded(response));

        var result = await _controller.RegenerateRecoveryCodes(new TwoFactorCodeRequestDto { Code = "123456" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
    }
}
