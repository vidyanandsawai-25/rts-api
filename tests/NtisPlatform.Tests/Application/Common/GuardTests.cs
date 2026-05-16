using NtisPlatform.Application.Common;
using Xunit;

namespace NtisPlatform.Tests.Application.Common;

/// <summary>
/// Comprehensive tests for Guard class to achieve 100% line and branch coverage
/// </summary>
public class GuardTests
{
    #region AgainstNull Tests

    [Fact]
    public void AgainstNull_WithNonNullValue_ReturnsValue()
    {
        // Arrange
        var value = "test";

        // Act
        var result = Guard.AgainstNull(value);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void AgainstNull_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        string? value = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => Guard.AgainstNull(value));
        Assert.Contains("cannot be null", exception.Message);
    }

    #endregion

    #region AgainstNullOrWhiteSpace Tests

    [Fact]
    public void AgainstNullOrWhiteSpace_WithValidString_ReturnsValue()
    {
        // Arrange
        var value = "test";

        // Act
        var result = Guard.AgainstNullOrWhiteSpace(value);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void AgainstNullOrWhiteSpace_WithNull_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrWhiteSpace(null!));
        Assert.Contains("cannot be null or whitespace", exception.Message);
    }

    [Fact]
    public void AgainstNullOrWhiteSpace_WithEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrWhiteSpace(""));
        Assert.Contains("cannot be null or whitespace", exception.Message);
    }

    [Fact]
    public void AgainstNullOrWhiteSpace_WithWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrWhiteSpace("   "));
        Assert.Contains("cannot be null or whitespace", exception.Message);
    }

    #endregion

    #region AgainstNullOrEmpty Tests

    [Fact]
    public void AgainstNullOrEmpty_WithValidString_ReturnsValue()
    {
        // Arrange
        var value = "test";

        // Act
        var result = Guard.AgainstNullOrEmpty(value);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void AgainstNullOrEmpty_WithNull_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrEmpty(null!));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    public void AgainstNullOrEmpty_WithEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrEmpty(""));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    public void AgainstNullOrEmpty_WithWhitespace_ReturnsValue()
    {
        // Arrange
        var value = "   ";

        // Act
        var result = Guard.AgainstNullOrEmpty(value);

        // Assert
        Assert.Equal(value, result);
    }

    #endregion

    #region AgainstNegativeOrZero - Int Tests

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public void AgainstNegativeOrZero_Int_WithPositiveValue_ReturnsValue(int value)
    {
        // Act
        var result = Guard.AgainstNegativeOrZero(value);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void AgainstNegativeOrZero_Int_WithZero_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.AgainstNegativeOrZero(0));
        Assert.Contains("must be greater than zero", exception.Message);
    }

    [Fact]
    public void AgainstNegativeOrZero_Int_WithNegativeValue_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.AgainstNegativeOrZero(-1));
        Assert.Contains("must be greater than zero", exception.Message);
    }

    #endregion

    #region AgainstNegativeOrZero - Long Tests

    [Theory]
    [InlineData(1L)]
    [InlineData(100L)]
    [InlineData(long.MaxValue)]
    public void AgainstNegativeOrZero_Long_WithPositiveValue_ReturnsValue(long value)
    {
        // Act
        var result = Guard.AgainstNegativeOrZero(value);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void AgainstNegativeOrZero_Long_WithZero_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.AgainstNegativeOrZero(0L));
        Assert.Contains("must be greater than zero", exception.Message);
    }

    [Fact]
    public void AgainstNegativeOrZero_Long_WithNegativeValue_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.AgainstNegativeOrZero(-1L));
        Assert.Contains("must be greater than zero", exception.Message);
    }

    #endregion

    #region AgainstNegative Tests

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    public void AgainstNegative_WithNonNegativeValue_ReturnsValue(int value)
    {
        // Act
        var result = Guard.AgainstNegative(value);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void AgainstNegative_WithNegativeValue_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.AgainstNegative(-1));
        Assert.Contains("cannot be negative", exception.Message);
    }

    #endregion

    #region AgainstOutOfRange Tests

    [Theory]
    [InlineData(5, 1, 10)]
    [InlineData(1, 1, 10)]
    [InlineData(10, 1, 10)]
    public void AgainstOutOfRange_WithValueInRange_ReturnsValue(int value, int min, int max)
    {
        // Act
        var result = Guard.AgainstOutOfRange(value, min, max);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void AgainstOutOfRange_WithValueBelowMinimum_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.AgainstOutOfRange(0, 1, 10));
        Assert.Contains("must be between", exception.Message);
    }

    [Fact]
    public void AgainstOutOfRange_WithValueAboveMaximum_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.AgainstOutOfRange(11, 1, 10));
        Assert.Contains("must be between", exception.Message);
    }

    #endregion

    #region AgainstInvalidLength Tests

    [Theory]
    [InlineData("test", 1, 10)]
    [InlineData("a", 1, 10)]
    [InlineData("1234567890", 1, 10)]
    public void AgainstInvalidLength_WithValidLength_ReturnsValue(string value, int minLength, int maxLength)
    {
        // Act
        var result = Guard.AgainstInvalidLength(value, minLength, maxLength);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void AgainstInvalidLength_WithTooShortString_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidLength("ab", 3, 10));
        Assert.Contains("length must be between", exception.Message);
    }

    [Fact]
    public void AgainstInvalidLength_WithTooLongString_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidLength("12345678901", 1, 10));
        Assert.Contains("length must be between", exception.Message);
    }

    [Fact]
    public void AgainstInvalidLength_WithNullString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidLength(null!, 1, 10));
    }

    #endregion

    #region AgainstExceedingLength Tests

    [Theory]
    [InlineData("test", 10)]
    [InlineData("a", 10)]
    [InlineData("1234567890", 10)]
    public void AgainstExceedingLength_WithValidLength_ReturnsValue(string value, int maxLength)
    {
        // Act
        var result = Guard.AgainstExceedingLength(value, maxLength);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact]
    public void AgainstExceedingLength_WithTooLongString_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.AgainstExceedingLength("12345678901", 10));
        Assert.Contains("cannot exceed", exception.Message);
        Assert.Contains("10 characters", exception.Message);
    }

    [Fact]
    public void AgainstExceedingLength_WithNullString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Guard.AgainstExceedingLength(null!, 10));
    }

    #endregion

    #region AgainstEmptyGuid Tests

    [Fact]
    public void AgainstEmptyGuid_WithValidGuid_ReturnsValue()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var result = Guard.AgainstEmptyGuid(guid);

        // Assert
        Assert.Equal(guid, result);
    }

    [Fact]
    public void AgainstEmptyGuid_WithEmptyGuid_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.AgainstEmptyGuid(Guid.Empty));
        Assert.Contains("cannot be an empty GUID", exception.Message);
    }

    #endregion

    #region AgainstNullOrEmpty - Collection Tests

    [Fact]
    public void AgainstNullOrEmpty_Collection_WithValidCollection_ReturnsValue()
    {
        // Arrange
        var collection = new List<int> { 1, 2, 3 };

        // Act
        var result = Guard.AgainstNullOrEmpty(collection);

        // Assert
        Assert.Equal(collection, result);
    }

    [Fact]
    public void AgainstNullOrEmpty_Collection_WithNullCollection_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Guard.AgainstNullOrEmpty<int>(null!));
    }

    [Fact]
    public void AgainstNullOrEmpty_Collection_WithEmptyCollection_ThrowsArgumentException()
    {
        // Arrange
        var collection = new List<int>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrEmpty(collection));
        Assert.Contains("cannot be empty", exception.Message);
    }

    #endregion

    #region AgainstInvalidStream Tests

    [Fact]
    public void AgainstInvalidStream_WithReadableStream_ReturnsValue()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        var result = Guard.AgainstInvalidStream(stream);

        // Assert
        Assert.Equal(stream, result);
    }

    [Fact]
    public void AgainstInvalidStream_WithNullStream_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidStream(null!));
        Assert.Contains("File is required", exception.Message);
    }

    [Fact]
    public void AgainstInvalidStream_WithNonReadableStream_ThrowsArgumentException()
    {
        // Arrange
        var stream = new NonReadableStream();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidStream(stream));
        Assert.Contains("must be readable", exception.Message);
    }

    #endregion

    #region AgainstInvalidEmailFormat Tests

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("test.user@domain.co.uk")]
    [InlineData("name+tag@example.org")]
    public void AgainstInvalidEmailFormat_WithValidEmail_ReturnsValue(string email)
    {
        // Act
        var result = Guard.AgainstInvalidEmailFormat(email);

        // Assert
        Assert.Equal(email, result);
    }

    [Fact]
    public void AgainstInvalidEmailFormat_WithoutAtSymbol_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidEmailFormat("userexample.com"));
        Assert.Contains("not a valid email format", exception.Message);
    }

    [Fact]
    public void AgainstInvalidEmailFormat_WithoutDot_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidEmailFormat("user@example"));
        Assert.Contains("not a valid email format", exception.Message);
    }

    [Fact]
    public void AgainstInvalidEmailFormat_WithNullOrWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidEmailFormat(null!));
        Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidEmailFormat(""));
        Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidEmailFormat("   "));
    }

    #endregion

    #region AgainstInvalidFileExtension Tests

    [Theory]
    [InlineData("document.pdf", new[] { ".pdf", ".doc", ".docx" })]
    [InlineData("image.JPG", new[] { ".jpg", ".png", ".gif" })]
    [InlineData("file.TXT", new[] { ".txt", ".csv" })]
    public void AgainstInvalidFileExtension_WithValidExtension_ReturnsValue(string fileName, string[] validExtensions)
    {
        // Act
        var result = Guard.AgainstInvalidFileExtension(fileName, validExtensions);

        // Assert
        Assert.Equal(fileName, result);
    }

    [Fact]
    public void AgainstInvalidFileExtension_WithInvalidExtension_ThrowsArgumentException()
    {
        // Arrange
        var fileName = "document.exe";
        var validExtensions = new[] { ".pdf", ".doc", ".docx" };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            Guard.AgainstInvalidFileExtension(fileName, validExtensions));
        Assert.Contains("invalid extension", exception.Message);
        Assert.Contains("Allowed extensions", exception.Message);
    }

    [Fact]
    public void AgainstInvalidFileExtension_WithoutExtension_ThrowsArgumentException()
    {
        // Arrange
        var fileName = "document";
        var validExtensions = new[] { ".pdf", ".doc" };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            Guard.AgainstInvalidFileExtension(fileName, validExtensions));
        Assert.Contains("invalid extension", exception.Message);
    }

    [Fact]
    public void AgainstInvalidFileExtension_WithNullFileName_ThrowsArgumentException()
    {
        // Arrange
        var validExtensions = new[] { ".pdf", ".doc" };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidFileExtension(null!, validExtensions));
    }

    [Fact]
    public void AgainstInvalidFileExtension_WithEmptyValidExtensions_ThrowsArgumentException()
    {
        // Arrange
        var fileName = "document.pdf";
        var validExtensions = Array.Empty<string>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidFileExtension(fileName, validExtensions));
    }

    [Fact]
    public void AgainstInvalidFileExtension_WithNullValidExtensions_ThrowsArgumentException()
    {
        // Arrange
        var fileName = "document.pdf";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Guard.AgainstInvalidFileExtension(fileName, null!));
    }

    [Fact]
    public void AgainstInvalidFileExtension_CaseInsensitive_ReturnsValue()
    {
        // Arrange
        var fileName = "DOCUMENT.PDF";
        var validExtensions = new[] { ".pdf", ".doc" };

        // Act
        var result = Guard.AgainstInvalidFileExtension(fileName, validExtensions);

        // Assert
        Assert.Equal(fileName, result);
    }

    #endregion

    #region Helper Classes

    private class NonReadableStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 0;
        public override long Position { get; set; }

        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => 0;
        public override long Seek(long offset, SeekOrigin origin) => 0;
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
    }

    #endregion
}
