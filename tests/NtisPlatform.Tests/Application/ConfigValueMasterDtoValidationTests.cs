using NtisPlatform.Application.DTOs.Master.ConfigValueMaster;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Unit tests for ConfigValueMaster DTOs validation
/// </summary>
public class ConfigValueMasterDtoValidationTests
{
    #region ConfigValueMasterDto Tests

    [Fact]
    public void ConfigValueMasterDto_AllProperties_CanBeSet()
    {
        // Arrange & Act
        var dto = new ConfigValueMasterDto
        {
            Id = 1,
            ConfigKeyId = 10,
            DepartmentId = 5,
            ModuleId = 3,
            Value = "Test Configuration Value",
            IsActive = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.Equal(10, dto.ConfigKeyId);
        Assert.Equal(5, dto.DepartmentId);
        Assert.Equal(3, dto.ModuleId);
        Assert.Equal("Test Configuration Value", dto.Value);
        Assert.True(dto.IsActive);
    }

    [Fact]
    public void ConfigValueMasterDto_DefaultValues()
    {
        // Arrange & Act
        var dto = new ConfigValueMasterDto();

        // Assert
        Assert.Equal(0, dto.Id);
        Assert.Equal(0, dto.ConfigKeyId);
        Assert.Null(dto.DepartmentId);
        Assert.Null(dto.ModuleId);
        Assert.Null(dto.Value);
        Assert.False(dto.IsActive);
    }

    [Fact]
    public void ConfigValueMasterDto_OptionalFieldsCanBeNull()
    {
        // Arrange & Act
        var dto = new ConfigValueMasterDto
        {
            Id = 1,
            ConfigKeyId = 10,
            DepartmentId = null,
            ModuleId = null,
            Value = null,
            IsActive = true
        };

        // Assert
        Assert.Null(dto.DepartmentId);
        Assert.Null(dto.ModuleId);
        Assert.Null(dto.Value);
    }

    #endregion

    #region CreateConfigValueMasterDto Tests

    [Fact]
    public void CreateConfigValueMasterDto_WithValidData_IsValid()
    {
        // Arrange
        var dto = new CreateConfigValueMasterDto
        {
            ConfigKeyId = 10,
            DepartmentId = 5,
            ModuleId = 3,
            Value = "New Configuration Value",
            IsActive = true,
            CreatedBy = 100 
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.Empty(validationResults);
    }

    [Fact]
    public void CreateConfigValueMasterDto_WithZeroConfigKeyId_IsInvalid()
    {
        // Arrange
        var dto = new CreateConfigValueMasterDto
        {
            ConfigKeyId = 0,
            DepartmentId = 5,
            ModuleId = 3,
            Value = "Valid Value",
            IsActive = true,
            CreatedBy = 100
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, r =>
            r.MemberNames.Contains(nameof(CreateConfigValueMasterDto.ConfigKeyId)));
    }

    [Fact]
    public void CreateConfigValueMasterDto_WithNegativeConfigKeyId_IsInvalid()
    {
        // Arrange
        var dto = new CreateConfigValueMasterDto
        {
            ConfigKeyId = -1,
            DepartmentId = 5,
            ModuleId = 3,
            Value = "Valid Value",
            IsActive = true,
            CreatedBy = 100
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, r =>
            r.MemberNames.Contains(nameof(CreateConfigValueMasterDto.ConfigKeyId)));
    }

    [Fact]
    public void CreateConfigValueMasterDto_WithTooLongValue_IsInvalid()
    {
        // Arrange
        var dto = new CreateConfigValueMasterDto
        {
            ConfigKeyId = 10,
            DepartmentId = 5,
            ModuleId = 3,
            Value = new string('a', 501),
            IsActive = true,
            CreatedBy = 100
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, r =>
            r.MemberNames.Contains(nameof(CreateConfigValueMasterDto.Value)));
    }

    #endregion

    #region UpdateConfigValueMasterDto Tests

    [Fact]
    public void UpdateConfigValueMasterDto_WithValidData_IsValid()
    {
        // Arrange
        var dto = new UpdateConfigValueMasterDto
        {
            ConfigKeyId = 10,
            DepartmentId = 5,
            ModuleId = 3,
            Value = "Updated Configuration Value",
            IsActive = true,
            UpdatedBy = 200
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.Empty(validationResults);
    }

    #endregion

    #region ConfigValueMasterQueryParameters Tests

    [Fact]
    public void ConfigValueMasterQueryParameters_AllPropertiesCanBeSet()
    {
        // Arrange & Act
        var queryParams = new ConfigValueMasterQueryParameters
        {
            ConfigKeyId = 10,
            DepartmentId = 5,
            ModuleId = 3,
            Value = "Search Value",
            IsActive = true,
            PageNumber = 1,
            PageSize = 20,
            SortBy = "ConfigKeyId",
            SortOrder = "asc"
        };

        // Assert
        Assert.Equal(10, queryParams.ConfigKeyId);
        Assert.Equal(5, queryParams.DepartmentId);
        Assert.Equal(3, queryParams.ModuleId);
        Assert.Equal("Search Value", queryParams.Value);
        Assert.True(queryParams.IsActive);
        Assert.Equal(1, queryParams.PageNumber);
        Assert.Equal(20, queryParams.PageSize);
    }

    [Fact]
    public void ConfigValueMasterQueryParameters_DefaultValues()
    {
        // Arrange & Act
        var queryParams = new ConfigValueMasterQueryParameters();

        // Assert
        Assert.Null(queryParams.ConfigKeyId);
        Assert.Null(queryParams.DepartmentId);
        Assert.Null(queryParams.ModuleId);
        Assert.Null(queryParams.Value);
        Assert.Null(queryParams.IsActive);
    }

    [Fact]
    public void ConfigValueMasterQueryParameters_FilterByConfigKeyIdOnly()
    {
        // Arrange & Act
        var queryParams = new ConfigValueMasterQueryParameters
        {
            ConfigKeyId = 10
        };

        // Assert
        Assert.Equal(10, queryParams.ConfigKeyId);
        Assert.Null(queryParams.DepartmentId);
        Assert.Null(queryParams.ModuleId);
    }

    [Fact]
    public void ConfigValueMasterQueryParameters_FilterByDepartmentOnly()
    {
        // Arrange & Act
        var queryParams = new ConfigValueMasterQueryParameters
        {
            DepartmentId = 5
        };

        // Assert
        Assert.Equal(5, queryParams.DepartmentId);
        Assert.Null(queryParams.ConfigKeyId);
        Assert.Null(queryParams.ModuleId);
    }

    [Fact]
    public void ConfigValueMasterQueryParameters_FilterByModuleOnly()
    {
        // Arrange & Act
        var queryParams = new ConfigValueMasterQueryParameters
        {
            ModuleId = 3
        };

        // Assert
        Assert.Equal(3, queryParams.ModuleId);
        Assert.Null(queryParams.ConfigKeyId);
        Assert.Null(queryParams.DepartmentId);
    }

    [Fact]
    public void ConfigValueMasterQueryParameters_AllFiltersCanBeCombined()
    {
        // Arrange & Act
        var queryParams = new ConfigValueMasterQueryParameters
        {
            ConfigKeyId = 10,
            DepartmentId = 5,
            ModuleId = 3,
            Value = "Test",
            IsActive = true
        };

        // Assert
        Assert.Equal(10, queryParams.ConfigKeyId);
        Assert.Equal(5, queryParams.DepartmentId);
        Assert.Equal(3, queryParams.ModuleId);
        Assert.Equal("Test", queryParams.Value);
        Assert.True(queryParams.IsActive);
    }

    #endregion

    #region Helper Methods

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }

    #endregion
    [Fact]
    public void UpdateConfigValueMasterDto_WithZeroConfigKeyId_IsInvalid()
    {
        // Arrange
        var dto = new UpdateConfigValueMasterDto
        {
            ConfigKeyId = 0,
            DepartmentId = 5,
            ModuleId = 3,
            Value = "Valid Value",
            IsActive = true,
            UpdatedBy = 100
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, r =>
            r.MemberNames.Contains(nameof(UpdateConfigValueMasterDto.ConfigKeyId)));
    }

    [Fact]
    public void UpdateConfigValueMasterDto_WithNegativeConfigKeyId_IsInvalid()
    {
        // Arrange
        var dto = new UpdateConfigValueMasterDto
        {
            ConfigKeyId = -1,
            DepartmentId = 5,
            ModuleId = 3,
            Value = "Valid Value",
            IsActive = true,
            UpdatedBy = 100
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, r =>
            r.MemberNames.Contains(nameof(UpdateConfigValueMasterDto.ConfigKeyId)));
    }

    [Fact]
    public void UpdateConfigValueMasterDto_WithTooLongValue_IsInvalid()
    {
        // Arrange
        var dto = new UpdateConfigValueMasterDto
        {
            ConfigKeyId = 10,
            DepartmentId = 5,
            ModuleId = 3,
            Value = new string('a', 501),
            IsActive = true,
            UpdatedBy = 100
        };

        // Act
        var validationResults = ValidateModel(dto);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, r =>
            r.MemberNames.Contains(nameof(UpdateConfigValueMasterDto.Value)));
    }
}
