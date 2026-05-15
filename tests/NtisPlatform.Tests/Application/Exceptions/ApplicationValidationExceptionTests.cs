using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using Xunit;

namespace NtisPlatform.Tests.Application.Exceptions;

/// <summary>
/// Comprehensive tests for ValidationException to achieve 100% line and branch coverage
/// </summary>
public class ApplicationValidationExceptionTests
{
    #region Constructor Tests - Message and OperationType

    [Fact]
    public void Constructor_WithMessageAndOperationType_SetsProperties()
    {
        // Arrange & Act
        var exception = new ValidationException("Validation failed", OperationType.Create);

        // Assert
        Assert.Equal("Validation failed", exception.Message);
        Assert.Equal(OperationType.Create, exception.OperationType);
        Assert.NotNull(exception.Errors);
        Assert.Empty(exception.Errors);
    }

    [Theory]
    [InlineData(OperationType.Create)]
    [InlineData(OperationType.Update)]
    [InlineData(OperationType.Delete)]
    public void Constructor_WithDifferentOperationTypes_SetsCorrectly(OperationType operationType)
    {
        // Arrange & Act
        var exception = new ValidationException("Test message", operationType);

        // Assert
        Assert.Equal(operationType, exception.OperationType);
    }

    #endregion

    #region Constructor Tests - Message, Errors Dictionary, and OperationType

    [Fact]
    public void Constructor_WithErrorsDictionary_SetsProperties()
    {
        // Arrange
        var errors = new Dictionary<string, string>
        {
            { "Email", "Invalid email format" },
            { "Password", "Password too short" }
        };

        // Act
        var exception = new ValidationException("Multiple validation errors", errors, OperationType.Create);

        // Assert
        Assert.Equal("Multiple validation errors", exception.Message);
        Assert.Equal(OperationType.Create, exception.OperationType);
        Assert.Equal(errors, exception.Errors);
        Assert.Equal(2, exception.Errors.Count);
    }

    [Fact]
    public void Constructor_WithEmptyErrorsDictionary_SetsEmptyErrors()
    {
        // Arrange
        var errors = new Dictionary<string, string>();

        // Act
        var exception = new ValidationException("No errors", errors, OperationType.Update);

        // Assert
        Assert.Empty(exception.Errors);
        Assert.Same(errors, exception.Errors);
    }

    [Fact]
    public void Constructor_WithSingleErrorInDictionary_WorksCorrectly()
    {
        // Arrange
        var errors = new Dictionary<string, string>
        {
            { "Username", "Username is required" }
        };

        // Act
        var exception = new ValidationException("Validation failed", errors, OperationType.Create);

        // Assert
        Assert.Single(exception.Errors);
        Assert.True(exception.Errors.ContainsKey("Username"));
        Assert.Equal("Username is required", exception.Errors["Username"]);
    }

    #endregion

    #region Constructor Tests - Single Property Error

    [Fact]
    public void Constructor_WithSinglePropertyError_SetsProperties()
    {
        // Arrange & Act
        var exception = new ValidationException("Email", "Invalid email format", OperationType.Create);

        // Assert
        Assert.Contains("Validation failed for Email", exception.Message);
        Assert.Contains("Invalid email format", exception.Message);
        Assert.Equal(OperationType.Create, exception.OperationType);
        Assert.Single(exception.Errors);
        Assert.True(exception.Errors.ContainsKey("Email"));
        Assert.Equal("Invalid email format", exception.Errors["Email"]);
    }

    [Fact]
    public void Constructor_WithSinglePropertyError_FormatsMessageCorrectly()
    {
        // Arrange & Act
        var exception = new ValidationException("PropertyName", "Error description", OperationType.Update);

        // Assert
        Assert.Equal("Validation failed for PropertyName: Error description", exception.Message);
    }

    [Theory]
    [InlineData("Email", "Invalid format", OperationType.Create)]
    [InlineData("Password", "Too short", OperationType.Update)]
    [InlineData("Username", "Already exists", OperationType.Delete)]
    public void Constructor_WithVariousPropertyErrors_SetsCorrectly(
        string propertyName, 
        string errorMessage, 
        OperationType operationType)
    {
        // Arrange & Act
        var exception = new ValidationException(propertyName, errorMessage, operationType);

        // Assert
        Assert.Equal(propertyName, exception.Errors.Keys.First());
        Assert.Equal(errorMessage, exception.Errors[propertyName]);
        Assert.Equal(operationType, exception.OperationType);
    }

    #endregion

    #region Exception Inheritance Tests

    [Fact]
    public void ValidationException_InheritsFromException()
    {
        // Arrange & Act
        var exception = new ValidationException("Test", OperationType.Create);

        // Assert
        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Fact]
    public void ValidationException_CanBeCaughtAsException()
    {
        // Arrange
        Exception? caughtException = null;

        try
        {
            throw new ValidationException("Test", OperationType.Create);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert
        Assert.NotNull(caughtException);
        Assert.IsType<ValidationException>(caughtException);
    }

    #endregion

    #region Errors Dictionary Modification Tests

    [Fact]
    public void Errors_CanBeModifiedAfterCreation()
    {
        // Arrange
        var exception = new ValidationException("Test", OperationType.Create);

        // Act
        exception.Errors["NewProperty"] = "New error";

        // Assert
        Assert.Single(exception.Errors);
        Assert.True(exception.Errors.ContainsKey("NewProperty"));
    }

    [Fact]
    public void Errors_PreservesOriginalDictionary()
    {
        // Arrange
        var originalErrors = new Dictionary<string, string>
        {
            { "Field1", "Error1" }
        };

        // Act
        var exception = new ValidationException("Test", originalErrors, OperationType.Create);
        exception.Errors["Field2"] = "Error2";

        // Assert
        Assert.Equal(2, exception.Errors.Count);
        Assert.Equal(2, originalErrors.Count); // Original also modified (same reference)
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_WithEmptyStrings_WorksCorrectly()
    {
        // Arrange & Act
        var exception = new ValidationException("", "", OperationType.Create);

        // Assert
        Assert.Contains("Validation failed for :", exception.Message);
        Assert.Single(exception.Errors);
    }

    [Fact]
    public void Constructor_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange & Act
        var exception = new ValidationException(
            "Field@Name",
            "Error with special chars: <>&\"'",
            OperationType.Create);

        // Assert
        Assert.Contains("Field@Name", exception.Message);
        Assert.Contains("Error with special chars: <>&\"'", exception.Message);
    }

    [Fact]
    public void Constructor_WithVeryLongStrings_HandlesCorrectly()
    {
        // Arrange
        var longPropertyName = new string('A', 1000);
        var longErrorMessage = new string('B', 10000);

        // Act
        var exception = new ValidationException(longPropertyName, longErrorMessage, OperationType.Update);

        // Assert
        Assert.Contains(longPropertyName, exception.Errors.Keys);
        Assert.Equal(longErrorMessage, exception.Errors[longPropertyName]);
    }

    #endregion

    #region Multiple Exceptions Scenarios

    [Fact]
    public void MultipleExceptions_WithDifferentOperationTypes_CanBeDistinguished()
    {
        // Arrange
        var createException = new ValidationException("Create error", OperationType.Create);
        var updateException = new ValidationException("Update error", OperationType.Update);
        var deleteException = new ValidationException("Delete error", OperationType.Delete);

        // Assert
        Assert.Equal(OperationType.Create, createException.OperationType);
        Assert.Equal(OperationType.Update, updateException.OperationType);
        Assert.Equal(OperationType.Delete, deleteException.OperationType);
    }

    [Fact]
    public void MultipleErrors_CanBeAccessedIndividually()
    {
        // Arrange
        var errors = new Dictionary<string, string>
        {
            { "Field1", "Error1" },
            { "Field2", "Error2" },
            { "Field3", "Error3" }
        };

        // Act
        var exception = new ValidationException("Multiple errors", errors, OperationType.Create);

        // Assert
        Assert.Equal(3, exception.Errors.Count);
        Assert.Equal("Error1", exception.Errors["Field1"]);
        Assert.Equal("Error2", exception.Errors["Field2"]);
        Assert.Equal("Error3", exception.Errors["Field3"]);
    }

    #endregion
}
