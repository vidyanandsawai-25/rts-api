using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.Auth;
using NtisPlatform.Application.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Api.Controllers;

/// <summary>
/// Complete AuthController tests to achieve 100% code coverage
/// </summary>
public class AuthControllerComprehensiveTests
{
    #region Refresh Endpoint Tests

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsOk()
    {
        var mockService = new Mock<IAuthService>();
        var mockLogger = new Mock<ILogger<AuthController>>();
        var controller = new AuthController(mockService.Object, mockLogger.Object);

        var request = new RefreshTokenRequestDto { RefreshToken = "valid_refresh_token" };
        var response = new RefreshTokenResponseDto
        {
            Success = true,
            Token = "new_access_token",
            RefreshToken = "new_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        mockService.Setup(s => s.RefreshTokenAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await controller.Refresh(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResponse = Assert.IsType<RefreshTokenResponseDto>(okResult.Value);
        Assert.True(returnedResponse.Success);
        Assert.Equal("new_access_token", returnedResponse.Token);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_ReturnsUnauthorized()
    {
        var mockService = new Mock<IAuthService>();
        var mockLogger = new Mock<ILogger<AuthController>>();
        var controller = new AuthController(mockService.Object, mockLogger.Object);

        var request = new RefreshTokenRequestDto { RefreshToken = "invalid_token" };
        var response = new RefreshTokenResponseDto
        {
            Success = false,
            Message = "Invalid refresh token"
        };

        mockService.Setup(s => s.RefreshTokenAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await controller.Refresh(request, CancellationToken.None);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult.Value);
    }

    [Fact]
    public async Task Refresh_WithInvalidModelState_ReturnsBadRequest()
    {
        var mockService = new Mock<IAuthService>();
        var mockLogger = new Mock<ILogger<AuthController>>();
        var controller = new AuthController(mockService.Object, mockLogger.Object);

        var request = new RefreshTokenRequestDto { RefreshToken = "" };
        controller.ModelState.AddModelError("RefreshToken", "RefreshToken is required");

        var result = await controller.Refresh(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Refresh_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        var mockService = new Mock<IAuthService>();
        var mockLogger = new Mock<ILogger<AuthController>>();
        var controller = new AuthController(mockService.Object, mockLogger.Object);

        var request = new RefreshTokenRequestDto { RefreshToken = "token" };

        mockService.Setup(s => s.RefreshTokenAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        var result = await controller.Refresh(request, CancellationToken.None);

        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithOperationCanceledException_Rethrows()
    {
        var mockService = new Mock<IAuthService>();
        var mockLogger = new Mock<ILogger<AuthController>>();
        var controller = new AuthController(mockService.Object, mockLogger.Object);

        var request = new RefreshTokenRequestDto { RefreshToken = "token" };

        mockService.Setup(s => s.RefreshTokenAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => controller.Refresh(request, CancellationToken.None));
    }

    #endregion

    #region ValidateSession Endpoint Tests

    [Fact]
    public async Task ValidateSession_WithValidToken_ReturnsOk()
    {
        var mockService = new Mock<IAuthService>();
        var mockLogger = new Mock<ILogger<AuthController>>();
        var controller = new AuthController(mockService.Object, mockLogger.Object);

        var request = new ValidateSessionRequestDto { AccessToken = "valid_token" };
        var response = new ValidateSessionResponseDto
        {
            IsValid = true,
            UserId = 1,
            Username = "testuser"
        };

        mockService.Setup(s => s.ValidateSessionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await controller.ValidateSession(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResponse = Assert.IsType<ValidateSessionResponseDto>(okResult.Value);
        Assert.True(returnedResponse.IsValid);
        Assert.Equal(1, returnedResponse.UserId);
    }

    [Fact]
    public async Task ValidateSession_WithInvalidToken_ReturnsOkWithInvalidFlag()
    {
        var mockService = new Mock<IAuthService>();
        var mockLogger = new Mock<ILogger<AuthController>>();
        var controller = new AuthController(mockService.Object, mockLogger.Object);

        var request = new ValidateSessionRequestDto { AccessToken = "invalid_token" };
        var response = new ValidateSessionResponseDto
        {
            IsValid = false,
            Message = "Token expired"
        };

        mockService.Setup(s => s.ValidateSessionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await controller.ValidateSession(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResponse = Assert.IsType<ValidateSessionResponseDto>(okResult.Value);
        Assert.False(returnedResponse.IsValid);
    }

    [Fact]
    public async Task ValidateSession_WithInvalidModelState_ReturnsBadRequest()
    {
        var mockService = new Mock<IAuthService>();
        var mockLogger = new Mock<ILogger<AuthController>>();
        var controller = new AuthController(mockService.Object, mockLogger.Object);

        var request = new ValidateSessionRequestDto { AccessToken = "" };
        controller.ModelState.AddModelError("AccessToken", "AccessToken is required");

        var result = await controller.ValidateSession(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ValidateSession_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        var mockService = new Mock<IAuthService>();
        var mockLogger = new Mock<ILogger<AuthController>>();
        var controller = new AuthController(mockService.Object, mockLogger.Object);

        var request = new ValidateSessionRequestDto { AccessToken = "token" };

        mockService.Setup(s => s.ValidateSessionAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        var result = await controller.ValidateSession(request, CancellationToken.None);

        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task ValidateSession_WithOperationCanceledException_Rethrows()
    {
        var mockService = new Mock<IAuthService>();
        var mockLogger = new Mock<ILogger<AuthController>>();
        var controller = new AuthController(mockService.Object, mockLogger.Object);

        var request = new ValidateSessionRequestDto { AccessToken = "token" };

        mockService.Setup(s => s.ValidateSessionAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => controller.ValidateSession(request, CancellationToken.None));
    }

    #endregion

    #region Logout Endpoint Tests

    [Fact]
    public async Task Logout_WithValidToken_ReturnsOk()
    {
        var mockService = new Mock<IAuthService>();
        var mockLogger = new Mock<ILogger<AuthController>>();
        var controller = new AuthController(mockService.Object, mockLogger.Object);

        var request = new LogoutRequestDto { RefreshToken = "refresh_token" };
        var response = new LogoutResponseDto
        {
            Success = true,
            Message = "Logged out successfully"
        };

        mockService.Setup(s => s.LogoutAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await controller.Logout(request, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResponse = Assert.IsType<LogoutResponseDto>(okResult.Value);
        Assert.True(returnedResponse.Success);
    }

    [Fact]
    public async Task Logout_WithInvalidToken_ReturnsBadRequest()
    {
        var mockService = new Mock<IAuthService>();
        var mockLogger = new Mock<ILogger<AuthController>>();
        var controller = new AuthController(mockService.Object, mockLogger.Object);

        var request = new LogoutRequestDto { RefreshToken = "invalid_token" };
        var response = new LogoutResponseDto
        {
            Success = false,
            Message = "Invalid refresh token"
        };

        mockService.Setup(s => s.LogoutAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await controller.Logout(request, CancellationToken.None);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Logout_WithInvalidModelState_ReturnsBadRequest()
    {
        var mockService = new Mock<IAuthService>();
        var mockLogger = new Mock<ILogger<AuthController>>();
        var controller = new AuthController(mockService.Object, mockLogger.Object);

        var request = new LogoutRequestDto { RefreshToken = "" };
        controller.ModelState.AddModelError("RefreshToken", "RefreshToken is required");

        var result = await controller.Logout(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Logout_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        var mockService = new Mock<IAuthService>();
        var mockLogger = new Mock<ILogger<AuthController>>();
        var controller = new AuthController(mockService.Object, mockLogger.Object);

        var request = new LogoutRequestDto { RefreshToken = "token" };

        mockService.Setup(s => s.LogoutAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        var result = await controller.Logout(request, CancellationToken.None);

        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task Logout_WithOperationCanceledException_Rethrows()
    {
        var mockService = new Mock<IAuthService>();
        var mockLogger = new Mock<ILogger<AuthController>>();
        var controller = new AuthController(mockService.Object, mockLogger.Object);

        var request = new LogoutRequestDto { RefreshToken = "token" };

        mockService.Setup(s => s.LogoutAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => controller.Logout(request, CancellationToken.None));
    }

    #endregion

    #region Login OperationCanceledException Test

    [Fact]
    public async Task Login_WithOperationCanceledException_Rethrows()
    {
        var mockService = new Mock<IAuthService>();
        var mockLogger = new Mock<ILogger<AuthController>>();
        var controller = new AuthController(mockService.Object, mockLogger.Object);

        var request = new LoginRequestDto { Username = "testuser", Password = "password" };

        mockService.Setup(s => s.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => controller.Login(request, CancellationToken.None));
    }

    #endregion
}
