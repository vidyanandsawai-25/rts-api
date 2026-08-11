using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.Auth;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Api;

/// <summary>
/// Unit tests for AuthController
/// Tests API endpoints for authentication
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IMfaChallengeService> _mfaChallengeServiceMock;
    private readonly Mock<IOtpChallengeService> _otpChallengeServiceMock;
    private readonly Mock<IPasswordResetService> _passwordResetServiceMock;
    private readonly Mock<IAuthTokenIssuerService> _authTokenIssuerMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _mfaChallengeServiceMock = new Mock<IMfaChallengeService>();
        _otpChallengeServiceMock = new Mock<IOtpChallengeService>();
        _passwordResetServiceMock = new Mock<IPasswordResetService>();
        _authTokenIssuerMock = new Mock<IAuthTokenIssuerService>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(
            _authServiceMock.Object,
            _mfaChallengeServiceMock.Object,
            _otpChallengeServiceMock.Object,
            _passwordResetServiceMock.Object,
            _authTokenIssuerMock.Object,
            _userRepositoryMock.Object,
            _loggerMock.Object);
    }

    #region Login Endpoint Tests - Success

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "ValidPassword123"
        };

        var response = new LoginResponseDto
        {
            Success = true,
            Token = "mock-jwt-token",
            UserId = 1,
            Username = "testuser",
            Message = "Login successful",
            ExpiresAt = DateTime.Now.AddMinutes(60)
        };

        _authServiceMock.Setup(x => x.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResponse = Assert.IsType<LoginResponseDto>(okResult.Value);
        Assert.True(returnedResponse.Success);
        Assert.Equal("mock-jwt-token", returnedResponse.Token);
        Assert.Equal(1, returnedResponse.UserId);
        Assert.Equal("testuser", returnedResponse.Username);
    }

    #endregion

    #region Login Endpoint Tests - Invalid Credentials

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "WrongPassword"
        };

        var response = new LoginResponseDto
        {
            Success = false,
            Message = "Invalid username or password"
        };

        _authServiceMock.Setup(x => x.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult.Value);
        var messageProperty = unauthorizedResult.Value.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        var messageValue = messageProperty.GetValue(unauthorizedResult.Value) as string;
        Assert.Equal("Invalid username or password", messageValue);
    }

    [Fact]
    public async Task Login_WithInactiveUser_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "inactiveuser",
            Password = "ValidPassword123"
        };

        var response = new LoginResponseDto
        {
            Success = false,
            Message = "User account is inactive. Please contact administrator."
        };

        _authServiceMock.Setup(x => x.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult.Value);
        var messageProperty = unauthorizedResult.Value.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        var messageValue = messageProperty.GetValue(unauthorizedResult.Value) as string;
        Assert.Equal("User account is inactive. Please contact administrator.", messageValue);
    }

    [Fact]
    public async Task Login_WithLockedAccount_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "lockeduser",
            Password = "ValidPassword123"
        };

        var response = new LoginResponseDto
        {
            Success = false,
            Message = "Account is locked until 2026-03-22 15:30:00"
        };

        _authServiceMock.Setup(x => x.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorizedResult.Value);
        var messageProperty = unauthorizedResult.Value.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        var messageValue = messageProperty.GetValue(unauthorizedResult.Value) as string;
        Assert.Contains("Account is locked", messageValue);
    }

    #endregion

    #region Login Endpoint Tests - Validation

    [Fact]
    public async Task Login_WithInvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "", // Invalid - required field
            Password = "Password123"
        };

        _controller.ModelState.AddModelError("Username", "Username is required");

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
    }

    [Fact]
    public async Task Login_WithMissingPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "" // Invalid - required field
        };

        _controller.ModelState.AddModelError("Password", "Password is required");

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Login Endpoint Tests - Exception Handling

    [Fact]
    public async Task Login_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "ValidPassword123"
        };

        _authServiceMock.Setup(x => x.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection error"));

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.NotNull(statusCodeResult.Value);
        var messageProperty = statusCodeResult.Value.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        var messageValue = messageProperty.GetValue(statusCodeResult.Value) as string;
        Assert.Equal("An error occurred during login", messageValue);
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task Login_WithSuccessfulLogin_LogsInformation()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "ValidPassword123"
        };

        var response = new LoginResponseDto
        {
            Success = true,
            Token = "mock-jwt-token",
            Message = "Login successful"
        };

        _authServiceMock.Setup(x => x.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        await _controller.Login(request, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successful login for user: testuser")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Login_WithFailedLogin_LogsWarning()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "WrongPassword"
        };

        var response = new LoginResponseDto
        {
            Success = false,
            Message = "Invalid username or password"
        };

        _authServiceMock.Setup(x => x.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        await _controller.Login(request, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed login attempt for username: testuser")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Login_WithException_LogsError()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Username = "testuser",
            Password = "ValidPassword123"
        };

        var exception = new Exception("Test exception");
        _authServiceMock.Setup(x => x.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        await _controller.Login(request, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error during login for username: testuser")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region VerifyTwoFactor Endpoint Tests

    [Fact]
    public async Task VerifyTwoFactor_WithValidCode_ReturnsOkWithLoginResponse()
    {
        // Arrange
        var request = new VerifyTwoFactorRequestDto { ChallengeId = "opaque-id", Code = "123456" };
        var loginResponse = new LoginResponseDto { Success = true, Token = "jwt", RefreshToken = "rt" };

        _mfaChallengeServiceMock
            .Setup(x => x.VerifyLoginChallengeAsync("opaque-id", "123456", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MfaVerificationResult.Succeeded(loginResponse));

        // Act
        var result = await _controller.VerifyTwoFactor(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(loginResponse, okResult.Value);
    }

    [Fact]
    public async Task VerifyTwoFactor_WithInvalidCode_ReturnsUnauthorized()
    {
        // Arrange
        var request = new VerifyTwoFactorRequestDto { ChallengeId = "opaque-id", Code = "000000" };

        _mfaChallengeServiceMock
            .Setup(x => x.VerifyLoginChallengeAsync("opaque-id", "000000", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MfaVerificationResult.Failed(MfaVerificationFailureReason.InvalidCode));

        // Act
        var result = await _controller.VerifyTwoFactor(request, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task VerifyTwoFactor_WithLockedChallenge_ReturnsLocked423()
    {
        // Arrange
        var request = new VerifyTwoFactorRequestDto { ChallengeId = "opaque-id", Code = "000000" };

        _mfaChallengeServiceMock
            .Setup(x => x.VerifyLoginChallengeAsync("opaque-id", "000000", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MfaVerificationResult.Failed(MfaVerificationFailureReason.ChallengeLocked));

        // Act
        var result = await _controller.VerifyTwoFactor(request, CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status423Locked, statusResult.StatusCode);
    }

    [Fact]
    public async Task VerifyTwoFactor_WithRecoveryCode_PassesUseRecoveryCodeFlagThrough()
    {
        // Arrange
        var request = new VerifyTwoFactorRequestDto { ChallengeId = "opaque-id", Code = "ABCDE-FGHJK", UseRecoveryCode = true };

        _mfaChallengeServiceMock
            .Setup(x => x.VerifyLoginChallengeAsync("opaque-id", "ABCDE-FGHJK", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MfaVerificationResult.Succeeded(new LoginResponseDto { Success = true }));

        // Act
        var result = await _controller.VerifyTwoFactor(request, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mfaChallengeServiceMock.Verify(
            x => x.VerifyLoginChallengeAsync("opaque-id", "ABCDE-FGHJK", true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
