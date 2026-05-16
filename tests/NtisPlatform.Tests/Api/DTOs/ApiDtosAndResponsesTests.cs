
using NtisPlatform.Api.Middleware;
using NtisPlatform.Application.DTOs.Document;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace NtisPlatform.Tests.Api.DTOs;

/// <summary>
/// Comprehensive tests for API DTOs and Middleware response models to achieve 100% coverage
/// </summary>
public class ApiDtosAndResponsesTests
{
    #region DocumentUploadFormDto Tests

    [Fact]
    public void DocumentUploadFormDto_Properties_GetSet_WorksCorrectly()
    {
        // Arrange
        var formFile = new FormFileMock();
        var dto = new DocumentUploadFormDto();

        // Act
        dto.File = formFile;
        dto.OwnerUserId = 100;
        dto.DocumentType = "Certificate";
        dto.ModuleCode = "PROPERTY";
        dto.ReferenceTableName = "PropertyCertificate";
        dto.ReferenceTableId = 1;
        dto.ReferenceTableIdGuid = Guid.NewGuid();
        dto.BindingPurpose = "MainDocument";
        dto.IsPrimaryDocument = true;
        dto.AuthModuleCode = "AUTH";
        dto.AuthReferenceId = 50;

        // Assert
        Assert.NotNull(dto.File);
        Assert.Equal(100, dto.OwnerUserId);
        Assert.Equal("Certificate", dto.DocumentType);
        Assert.Equal("PROPERTY", dto.ModuleCode);
        Assert.Equal("PropertyCertificate", dto.ReferenceTableName);
        Assert.Equal(1, dto.ReferenceTableId);
        Assert.NotEqual(Guid.Empty, dto.ReferenceTableIdGuid);
        Assert.Equal("MainDocument", dto.BindingPurpose);
        Assert.True(dto.IsPrimaryDocument);
        Assert.Equal("AUTH", dto.AuthModuleCode);
        Assert.Equal(50, dto.AuthReferenceId);
    }

    [Fact]
    public void DocumentUploadFormDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var dto = new DocumentUploadFormDto();

        // Assert
        Assert.Null(dto.File);
        Assert.Null(dto.OwnerUserId);
        Assert.Null(dto.DocumentType);
        Assert.Null(dto.ModuleCode);
        Assert.Null(dto.ReferenceTableName);
        Assert.Null(dto.ReferenceTableId);
        Assert.Null(dto.ReferenceTableIdGuid);
        Assert.Null(dto.BindingPurpose);
        Assert.False(dto.IsPrimaryDocument);
        Assert.Null(dto.AuthModuleCode);
        Assert.Null(dto.AuthReferenceId);
    }

    [Fact]
    public void DocumentUploadFormDto_FileProperty_HasRequiredAttribute()
    {
        // Arrange
        var property = typeof(DocumentUploadFormDto).GetProperty("File");

        // Act
        var requiredAttribute = property?.GetCustomAttributes(typeof(RequiredAttribute), false).FirstOrDefault();

        // Assert
        Assert.NotNull(requiredAttribute);
        Assert.IsType<RequiredAttribute>(requiredAttribute);
    }

    [Fact]
    public void DocumentUploadFormDto_WithIntReferenceId_SetsCorrectly()
    {
        // Arrange & Act
        var dto = new DocumentUploadFormDto
        {
            ReferenceTableId = 123,
            ReferenceTableIdGuid = null
        };

        // Assert
        Assert.Equal(123, dto.ReferenceTableId);
        Assert.Null(dto.ReferenceTableIdGuid);
    }

    [Fact]
    public void DocumentUploadFormDto_WithGuidReferenceId_SetsCorrectly()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var dto = new DocumentUploadFormDto
        {
            ReferenceTableId = null,
            ReferenceTableIdGuid = guid
        };

        // Assert
        Assert.Null(dto.ReferenceTableId);
        Assert.Equal(guid, dto.ReferenceTableIdGuid);
    }

    #endregion

    #region ErrorResponse Tests

    [Fact]
    public void ErrorResponse_Properties_GetSet_WorksCorrectly()
    {
        // Arrange & Act
        var response = new ErrorResponse
        {
            StatusCode = 400,
            Message = "Bad Request",
            Details = "Detailed error information"
        };

        // Assert
        Assert.Equal(400, response.StatusCode);
        Assert.Equal("Bad Request", response.Message);
        Assert.Equal("Detailed error information", response.Details);
    }

    [Fact]
    public void ErrorResponse_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var response = new ErrorResponse();

        // Assert
        Assert.Equal(0, response.StatusCode);
        Assert.Equal(string.Empty, response.Message);
        Assert.Null(response.Details);
    }

    [Fact]
    public void ErrorResponse_WithoutDetails_WorksCorrectly()
    {
        // Arrange & Act
        var response = new ErrorResponse
        {
            StatusCode = 500,
            Message = "Internal Server Error"
        };

        // Assert
        Assert.Equal(500, response.StatusCode);
        Assert.Equal("Internal Server Error", response.Message);
        Assert.Null(response.Details);
    }

    [Fact]
    public void ErrorResponse_WithAllProperties_SetsCorrectly()
    {
        // Arrange & Act
        var response = new ErrorResponse
        {
            StatusCode = 404,
            Message = "Not Found",
            Details = "The requested resource was not found"
        };

        // Assert
        Assert.Equal(404, response.StatusCode);
        Assert.NotEmpty(response.Message);
        Assert.NotNull(response.Details);
    }

    #endregion

    #region ValidationErrorResponse Tests

    [Fact]
    public void ValidationErrorResponse_Properties_GetSet_WorksCorrectly()
    {
        // Arrange
        var errors = new Dictionary<string, string>
        {
            { "Email", "Invalid email" },
            { "Password", "Too short" }
        };

        // Act
        var response = new ValidationErrorResponse
        {
            StatusCode = 400,
            Message = "Validation failed",
            OperationType = "Create",
            Errors = errors
        };

        // Assert
        Assert.Equal(400, response.StatusCode);
        Assert.Equal("Validation failed", response.Message);
        Assert.Equal("Create", response.OperationType);
        Assert.Equal(errors, response.Errors);
        Assert.Equal(2, response.Errors.Count);
    }

    [Fact]
    public void ValidationErrorResponse_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var response = new ValidationErrorResponse();

        // Assert
        Assert.Equal(0, response.StatusCode);
        Assert.Equal(string.Empty, response.Message);
        Assert.Equal(string.Empty, response.OperationType);
        Assert.NotNull(response.Errors);
        Assert.Empty(response.Errors);
    }

    [Fact]
    public void ValidationErrorResponse_WithEmptyErrors_WorksCorrectly()
    {
        // Arrange & Act
        var response = new ValidationErrorResponse
        {
            StatusCode = 400,
            Message = "Validation failed",
            OperationType = "Update",
            Errors = new Dictionary<string, string>()
        };

        // Assert
        Assert.Empty(response.Errors);
    }

    [Fact]
    public void ValidationErrorResponse_WithSingleError_WorksCorrectly()
    {
        // Arrange
        var errors = new Dictionary<string, string>
        {
            { "Username", "Username is required" }
        };

        // Act
        var response = new ValidationErrorResponse
        {
            StatusCode = 400,
            Message = "Validation failed",
            OperationType = "Create",
            Errors = errors
        };

        // Assert
        Assert.Single(response.Errors);
        Assert.True(response.Errors.ContainsKey("Username"));
        Assert.Equal("Username is required", response.Errors["Username"]);
    }

    [Fact]
    public void ValidationErrorResponse_WithMultipleErrors_WorksCorrectly()
    {
        // Arrange
        var errors = new Dictionary<string, string>
        {
            { "Field1", "Error1" },
            { "Field2", "Error2" },
            { "Field3", "Error3" }
        };

        // Act
        var response = new ValidationErrorResponse
        {
            StatusCode = 400,
            Message = "Multiple validation errors",
            OperationType = "Delete",
            Errors = errors
        };

        // Assert
        Assert.Equal(3, response.Errors.Count);
        Assert.Contains("Field1", response.Errors.Keys);
        Assert.Contains("Field2", response.Errors.Keys);
        Assert.Contains("Field3", response.Errors.Keys);
    }

    [Theory]
    [InlineData("Create")]
    [InlineData("Update")]
    [InlineData("Delete")]
    public void ValidationErrorResponse_WithDifferentOperationTypes_SetsCorrectly(string operationType)
    {
        // Arrange & Act
        var response = new ValidationErrorResponse
        {
            OperationType = operationType
        };

        // Assert
        Assert.Equal(operationType, response.OperationType);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void DocumentUploadFormDto_WithAllNullOptionalFields_IsValid()
    {
        // Arrange & Act
        var dto = new DocumentUploadFormDto
        {
            File = new FormFileMock()
        };

        // Assert
        Assert.NotNull(dto.File);
        Assert.Null(dto.OwnerUserId);
        Assert.Null(dto.ModuleCode);
    }

    [Fact]
    public void ValidationErrorResponse_ErrorsDictionary_CanBeModified()
    {
        // Arrange
        var response = new ValidationErrorResponse();

        // Act
        response.Errors["NewField"] = "New error";

        // Assert
        Assert.Single(response.Errors);
        Assert.Equal("New error", response.Errors["NewField"]);
    }

    [Fact]
    public void ErrorResponse_WithEmptyMessage_IsAllowed()
    {
        // Arrange & Act
        var response = new ErrorResponse
        {
            StatusCode = 500,
            Message = ""
        };

        // Assert
        Assert.Equal("", response.Message);
    }

    #endregion

    #region Helper Classes

    private class FormFileMock : IFormFile
    {
        public string ContentType => "application/pdf";
        public string ContentDisposition => "form-data";
        public IHeaderDictionary Headers => new HeaderDictionary();
        public long Length => 1024;
        public string Name => "file";
        public string FileName => "test.pdf";

        public void CopyTo(Stream target) { }
        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Stream OpenReadStream() => new MemoryStream();
    }

    #endregion
}
