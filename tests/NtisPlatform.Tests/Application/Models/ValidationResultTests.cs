using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Application.Models;

/// <summary>
/// Comprehensive tests for ValidationResult to achieve 100% line and branch coverage
/// </summary>
public class ValidationResultTests
{
    #region Success Tests

    [Fact]
    public void Success_ReturnsValidResult()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    #endregion

    #region Failure Tests - Single Error

    [Fact]
    public void Failure_WithPropertyNameAndMessage_CreatesResult()
    {
        // Act
        var result = ValidationResult.Failure("Email", "Invalid email format");

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Email", result.Errors[0].PropertyName);
        Assert.Equal("Invalid email format", result.Errors[0].ErrorMessage);
    }

    [Fact]
    public void Failure_WithMessageOnly_CreatesResult()
    {
        // Act
        var result = ValidationResult.Failure("General validation error");

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal(string.Empty, result.Errors[0].PropertyName);
        Assert.Equal("General validation error", result.Errors[0].ErrorMessage);
    }

    #endregion

    #region Failure Tests - Multiple Errors

    [Fact]
    public void Failure_WithMultipleErrors_CreatesResult()
    {
        // Arrange
        var errors = new[]
        {
            new ValidationError("Email", "Invalid email format"),
            new ValidationError("Password", "Password too short"),
            new ValidationError("Username", "Username already exists")
        };

        // Act
        var result = ValidationResult.Failure(errors);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(3, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
        Assert.Contains(result.Errors, e => e.PropertyName == "Password");
        Assert.Contains(result.Errors, e => e.PropertyName == "Username");
    }

    [Fact]
    public void Failure_WithDictionary_CreatesResult()
    {
        // Arrange
        var errorDict = new Dictionary<string, string>
        {
            { "Email", "Invalid format" },
            { "Phone", "Invalid phone number" }
        };

        // Act
        var result = ValidationResult.Failure(errorDict);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void Failure_WithEmptyErrorCollection_CreatesValidResult()
    {
        // Arrange
        var errors = Array.Empty<ValidationError>();

        // Act
        var result = ValidationResult.Failure(errors);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    #endregion

    #region IsValid Tests

    [Fact]
    public void IsValid_WithNoErrors_ReturnsTrue()
    {
        // Arrange
        var result = ValidationResult.Success();

        // Act
        var isValid = result.IsValid;

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_WithErrors_ReturnsFalse()
    {
        // Arrange
        var result = ValidationResult.Failure("Email", "Invalid");

        // Act
        var isValid = result.IsValid;

        // Assert
        Assert.False(isValid);
    }

    #endregion

    #region ToDictionary Tests

    [Fact]
    public void ToDictionary_WithSingleError_ReturnsCorrectDictionary()
    {
        // Arrange
        var result = ValidationResult.Failure("Email", "Invalid email");

        // Act
        var dict = result.ToDictionary();

        // Assert
        Assert.Single(dict);
        Assert.True(dict.ContainsKey("Email"));
        Assert.Equal("Invalid email", dict["Email"]);
    }

    [Fact]
    public void ToDictionary_WithMultipleErrorsForSameProperty_CombinesMessages()
    {
        // Arrange
        var errors = new[]
        {
            new ValidationError("Email", "Invalid format"),
            new ValidationError("Email", "Email required"),
            new ValidationError("Password", "Too short")
        };
        var result = ValidationResult.Failure(errors);

        // Act
        var dict = result.ToDictionary();

        // Assert
        Assert.Equal(2, dict.Count);
        Assert.Contains("Invalid format", dict["Email"]);
        Assert.Contains("Email required", dict["Email"]);
        Assert.Contains(";", dict["Email"]);
        Assert.Equal("Too short", dict["Password"]);
    }

    [Fact]
    public void ToDictionary_WithGeneralError_UsesGeneralKey()
    {
        // Arrange
        var result = ValidationResult.Failure("General error message");

        // Act
        var dict = result.ToDictionary();

        // Assert
        Assert.Single(dict);
        Assert.True(dict.ContainsKey("General"));
        Assert.Equal("General error message", dict["General"]);
    }

    [Fact]
    public void ToDictionary_WithEmptyPropertyName_UsesGeneralKey()
    {
        // Arrange
        var errors = new[]
        {
            new ValidationError("", "Error 1"),
            new ValidationError("", "Error 2")
        };
        var result = ValidationResult.Failure(errors);

        // Act
        var dict = result.ToDictionary();

        // Assert
        Assert.Single(dict);
        Assert.True(dict.ContainsKey("General"));
        Assert.Contains("Error 1", dict["General"]);
        Assert.Contains("Error 2", dict["General"]);
    }

    [Fact]
    public void ToDictionary_WithNoErrors_ReturnsEmptyDictionary()
    {
        // Arrange
        var result = ValidationResult.Success();

        // Act
        var dict = result.ToDictionary();

        // Assert
        Assert.Empty(dict);
    }

    #endregion

    #region ValidationError Tests

    [Fact]
    public void ValidationError_WithPropertyAndMessage_SetsCorrectly()
    {
        // Act
        var error = new ValidationError("FieldName", "Error message");

        // Assert
        Assert.Equal("FieldName", error.PropertyName);
        Assert.Equal("Error message", error.ErrorMessage);
    }

    [Fact]
    public void ValidationError_WithEmptyPropertyName_SetsCorrectly()
    {
        // Act
        var error = new ValidationError("", "Error message");

        // Assert
        Assert.Equal("", error.PropertyName);
        Assert.Equal("Error message", error.ErrorMessage);
    }

    [Fact]
    public void ValidationError_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var error1 = new ValidationError("Field", "Message");
        var error2 = new ValidationError("Field", "Message");
        var error3 = new ValidationError("Field", "Different");

        // Act & Assert
        Assert.Equal(error1, error2);
        Assert.NotEqual(error1, error3);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Failure_WithNullPropertyName_AllowsNullAndCreatesError()
    {
        // Act - ValidationResult.Failure allows null property names
        var result = ValidationResult.Failure(null!, "Error message");

        // Assert - Null property name is allowed and treated as a general error
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Null(result.Errors[0].PropertyName);
        Assert.Equal("Error message", result.Errors[0].ErrorMessage);
    }

    [Fact]
    public void ToDictionary_WithMixedPropertyNames_GroupsCorrectly()
    {
        // Arrange
        var errors = new[]
        {
            new ValidationError("Email", "Invalid"),
            new ValidationError("Password", "Too short"),
            new ValidationError("Email", "Required"),
            new ValidationError("", "General error")
        };
        var result = ValidationResult.Failure(errors);

        // Act
        var dict = result.ToDictionary();

        // Assert
        Assert.Equal(3, dict.Count);
        Assert.True(dict.ContainsKey("Email"));
        Assert.True(dict.ContainsKey("Password"));
        Assert.True(dict.ContainsKey("General"));
    }

    #endregion
}
