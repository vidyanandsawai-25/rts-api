using FluentAssertions;
using NtisPlatform.Core.Entities;
using Xunit;

namespace NtisPlatform.Tests.Core;

/// <summary>
/// Unit tests for ConfigValueMasterEntity
/// </summary>
public class ConfigValueMasterEntityTests
{
    [Fact]
    public void ConfigValueMasterEntity_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var configValue = new ConfigValueMasterEntity();

        // Assert
        Assert.Equal(0, configValue.ConfigValueId);
        Assert.Equal(0, configValue.ConfigKeyId);
        Assert.Null(configValue.DepartmentId);
        Assert.Null(configValue.ModuleId);
        Assert.Null(configValue.Value);
        Assert.True(configValue.IsActive); // BaseEntity defaults to true
    }

    [Fact]
    public void ConfigValueMasterEntity_CanSetConfigValueId()
    {
        // Arrange
        var configValue = new ConfigValueMasterEntity();

        // Act
        configValue.ConfigValueId = 100;

        // Assert
        Assert.Equal(100, configValue.ConfigValueId);
    }

    [Fact]
    public void ConfigValueMasterEntity_CanSetConfigKeyId()
    {
        // Arrange
        var configValue = new ConfigValueMasterEntity();

        // Act
        configValue.ConfigKeyId = 50;

        // Assert
        Assert.Equal(50, configValue.ConfigKeyId);
    }

    [Fact]
    public void ConfigValueMasterEntity_CanSetDepartmentId()
    {
        // Arrange
        var configValue = new ConfigValueMasterEntity();

        // Act
        configValue.DepartmentId = 10;

        // Assert
        Assert.Equal(10, configValue.DepartmentId);
    }

    [Fact]
    public void ConfigValueMasterEntity_CanSetModuleId()
    {
        // Arrange
        var configValue = new ConfigValueMasterEntity();

        // Act
        configValue.ModuleId = 20;

        // Assert
        configValue.ModuleId.Should().Be(20);
    }

    [Fact]
    public void ConfigValueMasterEntity_CanSetValue()
    {
        // Arrange
        var configValue = new ConfigValueMasterEntity();

        // Act
        configValue.Value = "Test Configuration Value";

        // Assert
        configValue.Value.Should().Be("Test Configuration Value");
    }

    [Fact]
    public void ConfigValueMasterEntity_CanSetIsActive()
    {
        // Arrange
        var configValue = new ConfigValueMasterEntity();

        // Act
        configValue.IsActive = true;

        // Assert
        configValue.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ConfigValueMasterEntity_InheritsFromBaseEntity()
    {
        // Arrange & Act
        var configValue = new ConfigValueMasterEntity();

        // Assert
        configValue.Should().BeAssignableTo<BaseEntity>();
    }

    [Fact]
    public void ConfigValueMasterEntity_AllPropertiesCanBeSet()
    {
        // Arrange
        var configValue = new ConfigValueMasterEntity();
        var now = DateTime.UtcNow;

        // Act
        configValue.ConfigValueId = 1;
        configValue.ConfigKeyId = 5;
        configValue.DepartmentId = 10;
        configValue.ModuleId = 15;
        configValue.Value = "Sample Value";
        configValue.IsActive = true;
        configValue.CreatedBy = 100;
        configValue.CreatedDate = now;
        configValue.UpdatedBy = 200;
        configValue.UpdatedDate = now.AddDays(1);

        // Assert
        configValue.ConfigValueId.Should().Be(1);
        configValue.ConfigKeyId.Should().Be(5);
        configValue.DepartmentId.Should().Be(10);
        configValue.ModuleId.Should().Be(15);
        configValue.Value.Should().Be("Sample Value");
        configValue.IsActive.Should().BeTrue();
        configValue.CreatedBy.Should().Be(100);
        configValue.CreatedDate.Should().Be(now);
        configValue.UpdatedBy.Should().Be(200);
        configValue.UpdatedDate.Should().Be(now.AddDays(1));
    }

    [Fact]
    public void ConfigValueMasterEntity_CanSetNavigationProperties()
    {
        // Arrange
        var configValue = new ConfigValueMasterEntity();
        var configKey = new ConfigKeyMasterEntity { ConfigKeyId = 5, ConfigCode = "TEST_KEY" };
        var department = new DepartmentMasterEntity { DepartmentId = 10, DepartmentCode = "DEPT001" };
        var module = new ModuleMasterEntity { ModuleId = 15, ModuleName = "Test Module" };

        // Act
        configValue.ConfigKey = configKey;
        configValue.Department = department;
        configValue.Module = module;

        // Assert
        configValue.ConfigKey.Should().NotBeNull();
        configValue.ConfigKey.ConfigKeyId.Should().Be(5);
        configValue.Department.Should().NotBeNull();
        configValue.Department.DepartmentId.Should().Be(10);
        configValue.Module.Should().NotBeNull();
        configValue.Module.ModuleId.Should().Be(15);
    }

    [Fact]
    public void ConfigValueMasterEntity_DepartmentIdCanBeNull()
    {
        // Arrange
        var configValue = new ConfigValueMasterEntity
        {
            ConfigKeyId = 5,
            Value = "Test",
            DepartmentId = null
        };

        // Assert
        configValue.DepartmentId.Should().BeNull();
    }

    [Fact]
    public void ConfigValueMasterEntity_ModuleIdCanBeNull()
    {
        // Arrange
        var configValue = new ConfigValueMasterEntity
        {
            ConfigKeyId = 5,
            Value = "Test",
            ModuleId = null
        };

        // Assert
        configValue.ModuleId.Should().BeNull();
    }

    [Fact]
    public void ConfigValueMasterEntity_ValueCanBeNull()
    {
        // Arrange
        var configValue = new ConfigValueMasterEntity
        {
            ConfigKeyId = 5,
            Value = null
        };

        // Assert
        configValue.Value.Should().BeNull();
    }

    [Fact]
    public void ConfigValueMasterEntity_CanHandleLongValue()
    {
        // Arrange
        var configValue = new ConfigValueMasterEntity();
        var longValue = new string('A', 500); // Max length from database schema

        // Act
        configValue.Value = longValue;

        // Assert
        configValue.Value.Should().Be(longValue);
        configValue.Value.Length.Should().Be(500);
    }
}
