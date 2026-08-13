using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Api.Controllers;
using NtisPlatform.Application.DTOs.Master.UserMaster;
using NtisPlatform.Application.DTOs.TwoFactor;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Tests.Api;

/// <summary>
/// Unit tests for UserController
/// Tests CRUD API endpoints for user management using extension methods
/// </summary>
public class UserControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ITwoFactorAuthenticationService> _twoFactorServiceMock;
    private readonly Mock<ILogger<UserController>> _loggerMock;
    private readonly Mock<IUserScreenAccessService> _userScreenAccessServiceMock;
    private readonly UserController _controller;

    public UserControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _twoFactorServiceMock = new Mock<ITwoFactorAuthenticationService>();
        _loggerMock = new Mock<ILogger<UserController>>();
        _userScreenAccessServiceMock = new Mock<IUserScreenAccessService>();
        _controller = new UserController(
            _userServiceMock.Object,
            _twoFactorServiceMock.Object,
            _loggerMock.Object,
            _userScreenAccessServiceMock.Object);
    }

    #region GetAll Endpoint Tests

    [Fact]
    public async Task GetAll_WithValidParameters_ReturnsOkWithPagedResult()
    {
        // Arrange
        var queryParameters = new UserQueryParameter
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "test"
        };

        var pagedResult = new PagedResult<UserDto>
        {
            Items = new List<UserDto>
            {
                new() { Id = 1, UserName = "testuser1", FirstName = "Test", LastName = "User1" },
                new() { Id = 2, UserName = "testuser2", FirstName = "Test", LastName = "User2" }
            },
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        _userServiceMock.Setup(x => x.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<PagedResult<UserDto>>(okResult.Value);
        Assert.Equal(2, returnedResult.Items.Count());
        Assert.Equal(2, returnedResult.TotalCount);
    }

    [Fact]
    public async Task GetAll_WithEmptyResult_ReturnsOkWithEmptyList()
    {
        // Arrange
        var queryParameters = new UserQueryParameter
        {
            PageNumber = 1,
            PageSize = 10
        };

        var pagedResult = new PagedResult<UserDto>
        {
            Items = new List<UserDto>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };

        _userServiceMock.Setup(x => x.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParameters, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResult = Assert.IsType<PagedResult<UserDto>>(okResult.Value);
        Assert.Empty(returnedResult.Items);
    }

    [Fact]
    public async Task GetAll_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var queryParameters = new UserQueryParameter();

        _userServiceMock.Setup(x => x.GetAllAsync(queryParameters, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAll(queryParameters, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<UserDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("error occurred", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region GetById Endpoint Tests

    [Fact]
    public async Task GetById_WithExistingUser_ReturnsOkWithUser()
    {
        // Arrange
        var userId = 1;
        var userDto = new UserDto
        {
            Id = userId,
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            MobileNo = "1234567890",
            Departments = new List<UserDepartmentAllocationDto>(),
            ModuleAccess = new List<UserModuleAllocationDto>(),
            RoleAllocations = new List<UserRoleAllocationDto>()
        };

        _userServiceMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userDto);

        // Act
        var result = await _controller.GetById(userId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedUser = Assert.IsType<UserDto>(okResult.Value);
        Assert.Equal(userId, returnedUser.Id);
        Assert.Equal("testuser", returnedUser.UserName);
    }

    [Fact]
    public async Task GetById_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var userId = 999;

        _userServiceMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserDto?)null);

        // Act
        var result = await _controller.GetById(userId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var userId = 1;

        _userServiceMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetById(userId, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<UserDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
    }

    #endregion

    #region Create Endpoint Tests

    [Fact]
    public async Task Create_WithValidData_ReturnsOkWithApiResponse()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            UserName = "newuser",
            FirstName = "New",
            LastName = "User",
            UserCode = "NEW001",
            Email = "newuser@example.com",
            MobileNo = "9876543210",
            CreatedBy = 1
        };

        var createdUser = new UserDto
        {
            Id = 1,
            UserName = "newuser",
            FirstName = "New",
            LastName = "User",
            UserCode = "NEW001",
            Email = "newuser@example.com",
            MobileNo = "9876543210",
            Departments = new List<UserDepartmentAllocationDto>(),
            ModuleAccess = new List<UserModuleAllocationDto>(),
            RoleAllocations = new List<UserRoleAllocationDto>()
        };

        _userServiceMock.Setup(x => x.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<UserDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Contains("inserted successfully", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(apiResponse.Items);
        Assert.Equal(1, apiResponse.Items.Id);
        Assert.Equal("newuser", apiResponse.Items.UserName);
    }

    [Fact]
    public async Task Create_WithDuplicateUsername_ReturnsConflict()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            UserName = "existinguser",
            CreatedBy = 1
        };

        _userServiceMock.Setup(x => x.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Username 'existinguser' is already taken (duplicate key violation)"));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<UserDto>>(conflictResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("already exists", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            UserName = "testuser",
            CreatedBy = 1
        };

        _userServiceMock.Setup(x => x.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection error"));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<UserDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
    }

    #endregion

    #region Update Endpoint Tests

    [Fact]
    public async Task Update_WithValidData_ReturnsOkWithApiResponse()
    {
        // Arrange
        var userId = 1;
        var updateDto = new UpdateUserDto
        {
            UserName = "updateduser",
            FirstName = "Updated",
            LastName = "User",
            UserCode = "UPD001",
            Email = "updated@example.com",
            UpdatedBy = 1
        };

        var updatedUser = new UserDto
        {
            Id = userId,
            UserName = "updateduser",
            FirstName = "Updated",
            LastName = "User",
            UserCode = "UPD001",
            Email = "updated@example.com",
            Departments = new List<UserDepartmentAllocationDto>(),
            ModuleAccess = new List<UserModuleAllocationDto>(),
            RoleAllocations = new List<UserRoleAllocationDto>()
        };

        _userServiceMock.Setup(x => x.UpdateAsync(userId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedUser);

        // Act
        var result = await _controller.Update(userId, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<UserDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.Contains("updated successfully", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(apiResponse.Items);
        Assert.Equal(userId, apiResponse.Items.Id);
    }

    [Fact]
    public async Task Update_WithNonExistentUser_ReturnsOkWithFailureResponse()
    {
        // Arrange
        var userId = 999;
        var updateDto = new UpdateUserDto
        {
            UserName = "nonexistent",
            UpdatedBy = 1
        };

        _userServiceMock.Setup(x => x.UpdateAsync(userId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserDto?)null);

        // Act
        var result = await _controller.Update(userId, updateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<UserDto>>(okResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("not found", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Update_WithDuplicateUsername_ReturnsConflict()
    {
        // Arrange
        var userId = 1;
        var updateDto = new UpdateUserDto
        {
            UserName = "existinguser",
            UpdatedBy = 1
        };

        _userServiceMock.Setup(x => x.UpdateAsync(userId, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Username 'existinguser' is already taken (unique constraint violation)"));

        // Act
        var result = await _controller.Update(userId, updateDto, CancellationToken.None);

        // Assert
        var conflictResult = Assert.IsType<ConflictObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<UserDto>>(conflictResult.Value);
        Assert.False(apiResponse.Success);
        Assert.Contains("already exists", apiResponse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Update_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var userId = 1;
        var updateDto = new UpdateUserDto
        {
            UserName = "testuser",
            UpdatedBy = 1
        };

        _userServiceMock.Setup(x => x.UpdateAsync(userId, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection error"));

        // Act
        var result = await _controller.Update(userId, updateDto, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<UserDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
    }

    #endregion

    #region Delete Endpoint Tests

    [Fact]
    public async Task Delete_WithExistingUser_ReturnsOkWithSuccessResponse()
    {
        // Arrange
        var userId = 1;
        var deleteDto = new DeleteUserDto { UpdatedBy = 1 };

        _userServiceMock.Setup(x => x.DeleteAsync(userId, It.IsAny<DeleteUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(userId, deleteDto, CancellationToken.None);

        // Assert
        var noContentResult = Assert.IsType<NoContentResult>(result);
        Assert.Equal(204, noContentResult.StatusCode);

        _userServiceMock.Verify(x => x.DeleteAsync(userId, It.IsAny<DeleteUserDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WithNonExistentUser_ReturnsOkWithFailureResponse()
    {
        // Arrange
        var userId = 999;
        var deleteDto = new DeleteUserDto { UpdatedBy = 1 };

        _userServiceMock.Setup(x => x.DeleteAsync(userId, It.IsAny<DeleteUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(userId, deleteDto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);

        _userServiceMock.Verify(x => x.DeleteAsync(userId, It.IsAny<DeleteUserDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var userId = 1;
        var deleteDto = new DeleteUserDto { UpdatedBy = 1 };

        _userServiceMock.Setup(x => x.DeleteAsync(userId, It.IsAny<DeleteUserDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.Delete(userId, deleteDto, CancellationToken.None));
    }

    #endregion

    #region Security Admin Endpoints Tests

    [Fact]
    public async Task Lock_WithExistingUser_ReturnsOkWithSecurityStatus()
    {
        // Arrange
        var userId = 1;
        var deactivateDto = new DeactivateUserDto { UpdatedBy = 1 };
        var expectedResponse = new UserSecurityStatusDto
        {
            Id = userId,
            UserName = "testuser",
            IsActive = false,
            MustChangePassword = true
        };

        _userServiceMock.Setup(x => x.DeactivateUserAsync(userId, deactivateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Lock(userId, deactivateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserSecurityStatusDto>(okResult.Value);
        Assert.Equal(userId, response.Id);
        Assert.False(response.IsActive);
    }

    [Fact]
    public async Task Lock_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var userId = 999;
        var deactivateDto = new DeactivateUserDto { UpdatedBy = 1 };

        _userServiceMock.Setup(x => x.DeactivateUserAsync(userId, deactivateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSecurityStatusDto?)null);

        // Act
        var result = await _controller.Lock(userId, deactivateDto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Unlock_WithExistingUser_ReturnsOkWithSecurityStatus()
    {
        // Arrange
        var userId = 1;
        var activateDto = new ActivateUserDto { UpdatedBy = 1 };
        var expectedResponse = new UserSecurityStatusDto
        {
            Id = userId,
            UserName = "testuser",
            IsActive = true,
            MustChangePassword = false
        };

        _userServiceMock.Setup(x => x.ActivateUserAsync(userId, activateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Unlock(userId, activateDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserSecurityStatusDto>(okResult.Value);
        Assert.Equal(userId, response.Id);
        Assert.True(response.IsActive);
    }

    [Fact]
    public async Task Unlock_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var userId = 999;
        var activateDto = new ActivateUserDto { UpdatedBy = 1 };

        _userServiceMock.Setup(x => x.ActivateUserAsync(userId, activateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSecurityStatusDto?)null);

        // Act
        var result = await _controller.Unlock(userId, activateDto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ResetPassword_WithExistingUser_ReturnsOkWithSecurityStatus()
    {
        // Arrange
        var userId = 1;
        var resetDto = new ResetPasswordDto { UpdatedBy = 1 };
        var expectedResponse = new UserSecurityStatusDto
        {
            Id = userId,
            UserName = "testuser",
            IsActive = true,
            MustChangePassword = true
        };

        _userServiceMock.Setup(x => x.ResetPasswordAsync(userId, resetDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ResetPassword(userId, resetDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserSecurityStatusDto>(okResult.Value);
        Assert.Equal(userId, response.Id);
        Assert.True(response.MustChangePassword);
    }

    [Fact]
    public async Task ResetPassword_WithNonExistentUser_ReturnsNotFound()
    {
        // Arrange
        var userId = 999;
        var resetDto = new ResetPasswordDto { UpdatedBy = 1 };

        _userServiceMock.Setup(x => x.ResetPasswordAsync(userId, resetDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSecurityStatusDto?)null);

        // Act
        var result = await _controller.ResetPassword(userId, resetDto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region Admin-Assisted Two-Factor Endpoint Tests

    [Fact]
    public async Task BeginTwoFactorSetupForUser_WhenNotEnabled_ReturnsOkWithSetupResponse()
    {
        // Arrange
        var userId = 1006;
        var expectedResponse = new TwoFactorSetupResponseDto
        {
            SharedKey = "J4GO 3LE2 ER7P X6YH GEUZ JGUJ BHG5 2ZEP",
            AuthenticatorUri = "otpauth://totp/NtisPlatform:mangesh?secret=ABC&issuer=NtisPlatform",
            Issuer = "NtisPlatform",
            AccountName = "mangesh"
        };

        _twoFactorServiceMock
            .Setup(x => x.BeginSetupAsync(userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TwoFactorOperationResult<TwoFactorSetupResponseDto>.Succeeded(expectedResponse));

        // Act
        var result = await _controller.BeginTwoFactorSetupForUser(userId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TwoFactorSetupResponseDto>(okResult.Value);
        Assert.Equal(expectedResponse.SharedKey, response.SharedKey);
    }

    [Fact]
    public async Task BeginTwoFactorSetupForUser_WhenAlreadyEnabled_ReturnsConflict()
    {
        // Arrange
        var userId = 1;
        _twoFactorServiceMock
            .Setup(x => x.BeginSetupAsync(userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TwoFactorOperationResult<TwoFactorSetupResponseDto>.Failed(TwoFactorOperationError.AlreadyEnabled));

        // Act
        var result = await _controller.BeginTwoFactorSetupForUser(userId, CancellationToken.None);

        // Assert
        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task BeginTwoFactorSetupForUser_WhenUserNotFound_ReturnsNotFound_NotConflict()
    {
        // Arrange — regression test: the setup endpoint used to report EVERY failure (including
        // an unknown/invalid target id) as "already enabled", which was actively misleading.
        var userId = 999999;
        _twoFactorServiceMock
            .Setup(x => x.BeginSetupAsync(userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TwoFactorOperationResult<TwoFactorSetupResponseDto>.Failed(TwoFactorOperationError.UserNotFound));

        // Act
        var result = await _controller.BeginTwoFactorSetupForUser(userId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task BeginTwoFactorSetupForUser_RequiresEmailOnFile()
    {
        // Arrange — regression test: admin-assisted setup must fail fast when the target has no
        // email on file, since the enrollment can never be completed without one.
        var userId = 1006;
        _twoFactorServiceMock
            .Setup(x => x.BeginSetupAsync(userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TwoFactorOperationResult<TwoFactorSetupResponseDto>.Failed(TwoFactorOperationError.EmailNotOnFile));

        // Act
        var result = await _controller.BeginTwoFactorSetupForUser(userId, CancellationToken.None);

        // Assert
        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task EnableTwoFactorForUser_WithValidTotpCode_ReturnsOkWithPendingEmailVerification()
    {
        // Arrange — admin-assisted "enable" no longer enables immediately: a valid TOTP code
        // only unlocks a one-time code emailed to the target's registered address.
        var userId = 1006;
        var request = new EnableTwoFactorRequestDto { Code = "123456" };
        var expectedResponse = new TwoFactorEmailVerificationPendingResponseDto { MaskedEmail = "ma***@example.com" };

        _twoFactorServiceMock
            .Setup(x => x.EnableAsync(userId, request.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TwoFactorOperationResult<TwoFactorEmailVerificationPendingResponseDto>.Succeeded(expectedResponse));

        // Act
        var result = await _controller.EnableTwoFactorForUser(userId, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TwoFactorEmailVerificationPendingResponseDto>(okResult.Value);
        Assert.Equal(expectedResponse.MaskedEmail, response.MaskedEmail);
    }

    [Fact]
    public async Task EnableTwoFactorForUser_WithInvalidTotpCode_ReturnsUnauthorized()
    {
        // Arrange
        var userId = 1006;
        var request = new EnableTwoFactorRequestDto { Code = "000000" };

        _twoFactorServiceMock
            .Setup(x => x.EnableAsync(userId, request.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TwoFactorOperationResult<TwoFactorEmailVerificationPendingResponseDto>.Failed(TwoFactorOperationError.InvalidCode));

        // Act
        var result = await _controller.EnableTwoFactorForUser(userId, request, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task EnableTwoFactorForUser_WithInvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var userId = 1006;
        var request = new EnableTwoFactorRequestDto { Code = "123456" };
        _controller.ModelState.AddModelError("Code", "Code is required");

        // Act
        var result = await _controller.EnableTwoFactorForUser(userId, request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ConfirmTwoFactorEmailForUser_WithValidCode_ReturnsOkWithRecoveryCodes()
    {
        // Arrange
        var userId = 1006;
        var request = new EnableTwoFactorRequestDto { Code = "482913" };
        var expectedResponse = new EnableTwoFactorResponseDto
        {
            IsEnabled = true,
            RecoveryCodes = new[] { "ABCDE-12345", "FGHJK-67890" }
        };

        _twoFactorServiceMock
            .Setup(x => x.ConfirmEnableAsync(userId, request.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TwoFactorOperationResult<EnableTwoFactorResponseDto>.Succeeded(expectedResponse));

        // Act
        var result = await _controller.ConfirmTwoFactorEmailForUser(userId, request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<EnableTwoFactorResponseDto>(okResult.Value);
        Assert.True(response.IsEnabled);
        Assert.Equal(2, response.RecoveryCodes.Count);
    }

    [Fact]
    public async Task ConfirmTwoFactorEmailForUser_WithInvalidCode_ReturnsUnauthorized()
    {
        // Arrange
        var userId = 1006;
        var request = new EnableTwoFactorRequestDto { Code = "000000" };

        _twoFactorServiceMock
            .Setup(x => x.ConfirmEnableAsync(userId, request.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TwoFactorOperationResult<EnableTwoFactorResponseDto>.Failed(TwoFactorOperationError.InvalidCode));

        // Act
        var result = await _controller.ConfirmTwoFactorEmailForUser(userId, request, CancellationToken.None);

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task ConfirmTwoFactorEmailForUser_WithInvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var userId = 1006;
        var request = new EnableTwoFactorRequestDto { Code = "" };
        _controller.ModelState.AddModelError("Code", "Code is required");

        // Act
        var result = await _controller.ConfirmTwoFactorEmailForUser(userId, request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region Password Security Tests

    [Fact]
    public async Task Create_NeverReturnsPasswordHash()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            UserName = "secureuser",
            FirstName = "Secure",
            LastName = "User",
            CreatedBy = 1
        };

        var createdUser = new UserDto
        {
            Id = 1,
            UserName = "secureuser",
            FirstName = "Secure",
            LastName = "User",
            Departments = new List<UserDepartmentAllocationDto>(),
            ModuleAccess = new List<UserModuleAllocationDto>(),
            RoleAllocations = new List<UserRoleAllocationDto>()
        };

        _userServiceMock.Setup(x => x.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<UserDto>>(okResult.Value);

        // Verify PasswordHash property doesn't exist on UserDto
        var dtoType = apiResponse.Items!.GetType();
        var passwordProperty = dtoType.GetProperty("PasswordHash");
        Assert.Null(passwordProperty); // UserDto should not have PasswordHash property
    }

    [Fact]
    public async Task GetById_NeverReturnsPasswordHash()
    {
        // Arrange
        var userDto = new UserDto
        {
            Id = 1,
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            Departments = new List<UserDepartmentAllocationDto>(),
            ModuleAccess = new List<UserModuleAllocationDto>(),
            RoleAllocations = new List<UserRoleAllocationDto>()
        };

        _userServiceMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userDto);

        // Act
        var result = await _controller.GetById(1, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedUser = Assert.IsType<UserDto>(okResult.Value);

        // Verify PasswordHash is not exposed
        var dtoType = returnedUser.GetType();
        var passwordProperty = dtoType.GetProperty("PasswordHash");
        Assert.Null(passwordProperty);
    }

    [Fact]
    public async Task ResetPassword_DoesNotReturnNewPassword()
    {
        // Arrange
        var userId = 1;
        var resetDto = new ResetPasswordDto { UpdatedBy = 1 };
        var expectedResponse = new UserSecurityStatusDto
        {
            Id = userId,
            UserName = "testuser",
            IsActive = true,
            MustChangePassword = true
        };

        _userServiceMock.Setup(x => x.ResetPasswordAsync(userId, resetDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ResetPassword(userId, resetDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<UserSecurityStatusDto>(okResult.Value);

        // Verify password/hash is never returned
        var securityStatusType = response.GetType();
        var passwordProperty = securityStatusType.GetProperty("Password");
        var passwordHashProperty = securityStatusType.GetProperty("PasswordHash");
        Assert.Null(passwordProperty);
        Assert.Null(passwordHashProperty);
    }

    #endregion

    #region Transactional Behavior Tests

    [Fact]
    public async Task Create_WhenAllocationSaveFails_PropagatesException()
    {
        // Arrange - Simulates failure after user is saved but during allocation save
        var createDto = new CreateUserDto
        {
            UserName = "testuser",
            FirstName = "Test",
            LastName = "User",
            CreatedBy = 1,
            Departments = new List<UserDepartmentAllocationCreateDto>
            {
                new() { DepartmentId = 1, IsActive = true }
            }
        };

        // Simulate exception during allocation save (would roll back transaction)
        _userServiceMock.Setup(x => x.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Failed to save department allocations"));

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<UserDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
    }

    [Fact]
    public async Task Update_WhenAllocationPatchFails_PropagatesException()
    {
        // Arrange
        var userId = 1;
        var updateDto = new UpdateUserDto
        {
            UserName = "updateduser",
            UpdatedBy = 1,
            Departments = new List<UserDepartmentAllocationCreateDto>
            {
                new() { DepartmentId = 2, IsActive = true }
            }
        };

        // Simulate exception during allocation patch
        _userServiceMock.Setup(x => x.UpdateAsync(userId, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Allocation update failed - transaction rolled back"));

        // Act
        var result = await _controller.Update(userId, updateDto, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    #endregion

    #region Concurrent Modification Tests

    [Fact]
    public async Task Update_WithConcurrentModification_ReturnsInternalServerError()
    {
        // Arrange
        var userId = 1;
        var updateDto = new UpdateUserDto
        {
            UserName = "updateduser",
            UpdatedBy = 1
        };

        _userServiceMock.Setup(x => x.UpdateAsync(userId, updateDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("The record was modified by another user"));

        // Act
        var result = await _controller.Update(userId, updateDto, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var apiResponse = Assert.IsType<ApiResponse<UserDto>>(statusCodeResult.Value);
        Assert.False(apiResponse.Success);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task GetAll_WithNegativePageNumber_HandlesGracefully()
    {
        // Arrange
        var queryParameters = new UserQueryParameter
        {
            PageNumber = -1, // Invalid
            PageSize = 10
        };

        var pagedResult = new PagedResult<UserDto>
        {
            Items = new List<UserDto>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };

        _userServiceMock.Setup(x => x.GetAllAsync(It.IsAny<UserQueryParameter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAll(queryParameters, CancellationToken.None);

        // Assert - Should handle gracefully (extension may normalize)
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WithNegativeId_ReturnsOkWithFailure()
    {
        // Arrange
        var userId = -1;
        var deleteDto = new DeleteUserDto { UpdatedBy = 1 };

        _userServiceMock.Setup(x => x.DeleteAsync(userId, It.IsAny<DeleteUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(userId, deleteDto, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);

        _userServiceMock.Verify(x => x.DeleteAsync(userId, It.IsAny<DeleteUserDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithNullAllocations_CreatesUserWithoutAllocations()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            UserName = "minimaluser",
            FirstName = "Minimal",
            LastName = "User",
            CreatedBy = 1,
            Departments = null, // Explicitly null
            ModuleAccess = null,
            RoleAllocations = null
        };

        var createdUser = new UserDto
        {
            Id = 1,
            UserName = "minimaluser",
            FirstName = "Minimal",
            LastName = "User",
            Departments = new List<UserDepartmentAllocationDto>(),
            ModuleAccess = new List<UserModuleAllocationDto>(),
            RoleAllocations = new List<UserRoleAllocationDto>()
        };

        _userServiceMock.Setup(x => x.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _controller.Create(createDto, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiResponse = Assert.IsType<ApiResponse<UserDto>>(okResult.Value);
        Assert.True(apiResponse.Success);
        Assert.NotNull(apiResponse.Items);
        Assert.NotNull(apiResponse.Items.Departments);
        Assert.Empty(apiResponse.Items.Departments);
    }

    #endregion
}

// Custom exception class for testing
public class DbUpdateException : Exception
{
    public DbUpdateException(string message) : base(message) { }
}

public class DbUpdateConcurrencyException : DbUpdateException
{
    public DbUpdateConcurrencyException(string message) : base(message) { }
}
