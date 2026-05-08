using Microsoft.Extensions.Configuration;
using Moq;
using NtisPlatform.Api.Helpers;
using Xunit;

namespace NtisPlatform.Tests.Api.Helpers;

/// <summary>
/// Comprehensive tests for FileValidationHelper to achieve 100% code coverage
/// </summary>
public class FileValidationHelperTests
{
    private readonly FileValidationHelper _helper;

    public FileValidationHelperTests()
    {
        var mockConfiguration = new Mock<IConfiguration>();
        var mockSection = new Mock<IConfigurationSection>();
        mockSection.Setup(s => s.Value).Returns((string?)null);
        mockConfiguration.Setup(c => c.GetSection(It.IsAny<string>())).Returns(mockSection.Object);
        _helper = new FileValidationHelper(mockConfiguration.Object);
    }

    #region IsValidFileType - Valid Files Tests

    [Fact]
    public void IsValidFileType_WithValidPdf_ReturnsTrue()
    {
        // Act
        var result = _helper.IsValidFileType("application/pdf", "document.pdf");

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("image/jpeg", "photo.jpg")]
    [InlineData("image/jpeg", "photo.jpeg")]
    [InlineData("image/png", "image.png")]
    [InlineData("image/gif", "animation.gif")]
    [InlineData("image/bmp", "picture.bmp")]
    [InlineData("image/tiff", "scan.tiff")]
    [InlineData("image/tiff", "scan.tif")]
    [InlineData("image/webp", "modern.webp")]
    public void IsValidFileType_WithValidImages_ReturnsTrue(string contentType, string fileName)
    {
        // Act
        var result = _helper.IsValidFileType(contentType, fileName);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("application/msword", "old.doc")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "new.docx")]
    [InlineData("application/vnd.ms-excel", "old.xls")]
    [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "new.xlsx")]
    [InlineData("application/vnd.ms-powerpoint", "old.ppt")]
    [InlineData("application/vnd.openxmlformats-officedocument.presentationml.presentation", "new.pptx")]
    public void IsValidFileType_WithValidOfficeDocuments_ReturnsTrue(string contentType, string fileName)
    {
        // Act
        var result = _helper.IsValidFileType(contentType, fileName);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidFileType_WithValidTextFile_ReturnsTrue()
    {
        // Act
        var result = _helper.IsValidFileType("text/plain", "readme.txt");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidFileType_WithCaseInsensitiveExtension_ReturnsTrue()
    {
        // Act
        var result = _helper.IsValidFileType("application/pdf", "Document.PDF");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidFileType_WithCaseInsensitiveMimeType_ReturnsTrue()
    {
        // Act
        var result = _helper.IsValidFileType("Application/PDF", "document.pdf");

        // Assert
        Assert.True(result);
    }

    #endregion

    #region IsValidFileType - Invalid Files Tests

    [Fact]
    public void IsValidFileType_WithNullContentType_ReturnsFalse()
    {
        // Act
        var result = _helper.IsValidFileType(null!, "document.pdf");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidFileType_WithEmptyContentType_ReturnsFalse()
    {
        // Act
        var result = _helper.IsValidFileType("", "document.pdf");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidFileType_WithWhitespaceContentType_ReturnsFalse()
    {
        // Act
        var result = _helper.IsValidFileType("   ", "document.pdf");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidFileType_WithNullFileName_ReturnsFalse()
    {
        // Act
        var result = _helper.IsValidFileType("application/pdf", null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidFileType_WithEmptyFileName_ReturnsFalse()
    {
        // Act
        var result = _helper.IsValidFileType("application/pdf", "");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidFileType_WithWhitespaceFileName_ReturnsFalse()
    {
        // Act
        var result = _helper.IsValidFileType("application/pdf", "   ");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidFileType_WithFileNameWithoutExtension_ReturnsFalse()
    {
        // Act
        var result = _helper.IsValidFileType("application/pdf", "documentwithoutextension");

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("application/x-msdownload", "virus.exe")]
    [InlineData("application/javascript", "script.js")]
    [InlineData("application/x-sh", "script.sh")]
    [InlineData("application/x-powershell", "script.ps1")]
    [InlineData("application/zip", "archive.zip")]
    [InlineData("application/x-rar-compressed", "archive.rar")]
    [InlineData("application/x-7z-compressed", "archive.7z")]
    public void IsValidFileType_WithDangerousFileTypes_ReturnsFalse(string contentType, string fileName)
    {
        // Act
        var result = _helper.IsValidFileType(contentType, fileName);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidFileType_WithValidExtensionButInvalidMimeType_ReturnsFalse()
    {
        // Act - Valid .pdf extension but wrong MIME type
        var result = _helper.IsValidFileType("application/octet-stream", "document.pdf");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidFileType_WithValidMimeTypeButInvalidExtension_ReturnsFalse()
    {
        // Act - Valid PDF MIME but wrong extension
        var result = _helper.IsValidFileType("application/pdf", "document.exe");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetInvalidFileTypeMessage Tests

    [Fact]
    public void GetInvalidFileTypeMessage_ReturnsNonEmptyMessage()
    {
        // Act
        var message = _helper.GetInvalidFileTypeMessage();

        // Assert
        Assert.NotNull(message);
        Assert.NotEmpty(message);
        Assert.Contains("Invalid file type", message);
    }

    [Fact]
    public void GetInvalidFileTypeMessage_ContainsAllowedFormats()
    {
        // Act
        var message = _helper.GetInvalidFileTypeMessage();

        // Assert
        Assert.Contains(".pdf", message);
        Assert.Contains(".jpg", message);
        Assert.Contains(".doc", message);
    }

    #endregion
}
