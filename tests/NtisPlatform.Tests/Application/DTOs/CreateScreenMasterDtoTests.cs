using NtisPlatform.Application.DTOs;
using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Tests.Application.DTOs;

/// <summary>
/// Comprehensive tests for CreateScreenMasterDto to achieve 100% code coverage
/// </summary>
public class CreateScreenMasterDtoTests
{
    private static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void CreateScreenMasterDto_AllProperties_GetSet_WorksCorrectly()
    {
        var dto = new CreateScreenMasterDto
        {
            ScreenGroupId = 5,
            DepartmentId = 2,
            ModuleId = 10,
            ScreenCode = "SCR001",
            ScreenName = "Dashboard",
            ScreenNameLocal = "????????",
            ScreenIcon = "dashboard-icon",
            RoutePath = "/dashboard",
            IsMenu = true,
            IsAuthenticationRequired = true,
            DisplayOrder = 1,
            IsActive = true,
            CreatedBy = 100
        };

        Assert.Equal(5, dto.ScreenGroupId);
        Assert.Equal(2, dto.DepartmentId);
        Assert.Equal(10, dto.ModuleId);
        Assert.Equal("SCR001", dto.ScreenCode);
        Assert.Equal("Dashboard", dto.ScreenName);
        Assert.Equal("????????", dto.ScreenNameLocal);
        Assert.Equal("dashboard-icon", dto.ScreenIcon);
        Assert.Equal("/dashboard", dto.RoutePath);
        Assert.True(dto.IsMenu);
        Assert.True(dto.IsAuthenticationRequired);
        Assert.Equal(1, dto.DisplayOrder);
        Assert.True(dto.IsActive);
        Assert.Equal(100, dto.CreatedBy);
    }

    [Fact]
    public void CreateScreenMasterDto_ValidData_PassesValidation()
    {
        var dto = new CreateScreenMasterDto
        {
            ScreenGroupId = 5,
            ScreenCode = "SCR001",
            ScreenName = "Dashboard"
        };

        var results = Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void CreateScreenMasterDto_DefaultValues()
    {
        var dto = new CreateScreenMasterDto();

        Assert.Equal(0, dto.ScreenGroupId);
        Assert.True(dto.IsMenu);
        Assert.True(dto.IsAuthenticationRequired);
        Assert.Equal(string.Empty, dto.ScreenCode);
        Assert.Equal(string.Empty, dto.ScreenName);
    }

    [Fact]
    public void CreateScreenMasterDto_MissingScreenCode_FailsValidation()
    {
        var dto = new CreateScreenMasterDto
        {
            ScreenGroupId = 5,
            ScreenCode = string.Empty,
            ScreenName = "Dashboard"
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "ScreenCode_Required");
    }

    [Fact]
    public void CreateScreenMasterDto_ExceedMaxLengthScreenCode_FailsValidation()
    {
        var dto = new CreateScreenMasterDto
        {
            ScreenGroupId = 5,
            ScreenCode = new string('A', 51),
            ScreenName = "Dashboard"
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage == "ScreenCode_MaxLen_50");
    }

    [Fact]
    public void UpdateScreenMasterDto_AllProperties_GetSet_WorksCorrectly()
    {
        var dto = new UpdateScreenMasterDto
        {
            ScreenGroupId = 6,
            DepartmentId = 3,
            ModuleId = 11,
            ScreenCode = "SCR002",
            ScreenName = "Reports",
            ScreenNameLocal = "???????",
            ScreenIcon = "report-icon",
            RoutePath = "/reports",
            IsMenu = false,
            IsAuthenticationRequired = true,
            DisplayOrder = 2,
            IsActive = true,
            UpdatedBy = 200
        };

        Assert.Equal(6, dto.ScreenGroupId);
        Assert.Equal(3, dto.DepartmentId);
        Assert.Equal(11, dto.ModuleId);
        Assert.Equal("SCR002", dto.ScreenCode);
        Assert.Equal("Reports", dto.ScreenName);
        Assert.Equal("???????", dto.ScreenNameLocal);
        Assert.Equal("report-icon", dto.ScreenIcon);
        Assert.Equal("/reports", dto.RoutePath);
        Assert.False(dto.IsMenu);
        Assert.True(dto.IsAuthenticationRequired);
        Assert.Equal(2, dto.DisplayOrder);
        Assert.True(dto.IsActive);
        Assert.Equal(200, dto.UpdatedBy);
    }

    [Fact]
    public void UpdateScreenMasterDto_DefaultValues()
    {
        var dto = new UpdateScreenMasterDto();

        Assert.False(dto.IsMenu);
        Assert.False(dto.IsAuthenticationRequired);
    }
}
