using NtisPlatform.Application.Common;
using Xunit;

namespace NtisPlatform.Tests.Application.Common;

public class GuardTests
{
    [Fact]
    public void AgainstNull_ReturnsArgument_WhenNotNull()
    {
        var value = new object();
        Assert.Same(value, Guard.AgainstNull(value));
    }

    [Fact]
    public void AgainstNull_Throws_WhenNull()
    {
        object? value = null;
        Assert.Throws<ArgumentNullException>(() => Guard.AgainstNull(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AgainstNullOrWhiteSpace_Throws_WhenInvalid(string? value)
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrWhiteSpace(value));
    }

    [Fact]
    public void AgainstNullOrWhiteSpace_ReturnsArgument_WhenValid()
    {
        Assert.Equal("ok", Guard.AgainstNullOrWhiteSpace("ok"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AgainstNullOrEmpty_String_Throws_WhenInvalid(string? value)
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrEmpty(value));
    }

    [Fact]
    public void AgainstNullOrEmpty_String_ReturnsArgument_WhenValid()
    {
        Assert.Equal(" ", Guard.AgainstNullOrEmpty(" "));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AgainstNegativeOrZero_Int_Throws_WhenInvalid(int value)
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstNegativeOrZero(value));
    }

    [Fact]
    public void AgainstNegativeOrZero_Int_Returns_WhenPositive()
    {
        Assert.Equal(1, Guard.AgainstNegativeOrZero(1));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void AgainstNegativeOrZero_Long_Throws_WhenInvalid(long value)
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstNegativeOrZero(value));
    }

    [Fact]
    public void AgainstNegativeOrZero_Long_Returns_WhenPositive()
    {
        Assert.Equal(2L, Guard.AgainstNegativeOrZero(2L));
    }

    [Fact]
    public void AgainstNegative_Throws_WhenNegative()
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstNegative(-1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void AgainstNegative_Returns_WhenZeroOrPositive(int value)
    {
        Assert.Equal(value, Guard.AgainstNegative(value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void AgainstOutOfRange_Throws_WhenOutsideRange(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Guard.AgainstOutOfRange(value, 1, 10));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void AgainstOutOfRange_Returns_WhenInsideRange(int value)
    {
        Assert.Equal(value, Guard.AgainstOutOfRange(value, 1, 10));
    }

    [Fact]
    public void AgainstInvalidLength_Throws_WhenTooShort()
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidLength("ab", 3, 5));
    }

    [Fact]
    public void AgainstInvalidLength_Throws_WhenTooLong()
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidLength("abcdef", 3, 5));
    }

    [Fact]
    public void AgainstInvalidLength_Returns_WhenWithin()
    {
        Assert.Equal("abcd", Guard.AgainstInvalidLength("abcd", 3, 5));
    }

    [Fact]
    public void AgainstExceedingLength_Throws_WhenTooLong()
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstExceedingLength("abcdef", 3));
    }

    [Fact]
    public void AgainstExceedingLength_Returns_WhenWithin()
    {
        Assert.Equal("abc", Guard.AgainstExceedingLength("abc", 3));
    }

    [Fact]
    public void AgainstEmptyGuid_Throws_WhenEmpty()
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstEmptyGuid(Guid.Empty));
    }

    [Fact]
    public void AgainstEmptyGuid_Returns_WhenValid()
    {
        var g = Guid.NewGuid();
        Assert.Equal(g, Guard.AgainstEmptyGuid(g));
    }

    [Fact]
    public void AgainstNullOrEmpty_Collection_Throws_WhenNull()
    {
        IEnumerable<int>? collection = null;
        Assert.Throws<ArgumentNullException>(() => Guard.AgainstNullOrEmpty(collection));
    }

    [Fact]
    public void AgainstNullOrEmpty_Collection_Throws_WhenEmpty()
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstNullOrEmpty(Array.Empty<int>()));
    }

    [Fact]
    public void AgainstNullOrEmpty_Collection_Returns_WhenNonEmpty()
    {
        var list = new[] { 1, 2 };
        Assert.Same(list, Guard.AgainstNullOrEmpty(list));
    }

    [Fact]
    public void AgainstInvalidStream_Throws_WhenNull()
    {
        Stream? stream = null;
        Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidStream(stream));
    }

    [Fact]
    public void AgainstInvalidStream_Throws_WhenNotReadable()
    {
        using var stream = new NonReadableStream();
        Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidStream(stream));
    }

    [Fact]
    public void AgainstInvalidStream_Returns_WhenReadable()
    {
        using var stream = new MemoryStream();
        Assert.Same(stream, Guard.AgainstInvalidStream(stream));
    }

    [Theory]
    [InlineData("no-at-or-dot")]
    [InlineData("a@b")]
    [InlineData("a.b")]
    public void AgainstInvalidEmailFormat_Throws_WhenInvalid(string value)
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidEmailFormat(value));
    }

    [Fact]
    public void AgainstInvalidEmailFormat_Returns_WhenValid()
    {
        Assert.Equal("a@b.com", Guard.AgainstInvalidEmailFormat("a@b.com"));
    }

    [Fact]
    public void AgainstInvalidFileExtension_Throws_WhenExtensionMissing()
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidFileExtension("file", new[] { ".pdf" }));
    }

    [Fact]
    public void AgainstInvalidFileExtension_Throws_WhenExtensionNotAllowed()
    {
        Assert.Throws<ArgumentException>(() => Guard.AgainstInvalidFileExtension("file.exe", new[] { ".pdf" }));
    }

    [Fact]
    public void AgainstInvalidFileExtension_Returns_WhenAllowed()
    {
        Assert.Equal("file.PDF", Guard.AgainstInvalidFileExtension("file.PDF", new[] { ".pdf" }));
    }

    [Fact]
    public void Against_Throws_WhenConditionTrue()
    {
        Assert.Throws<ArgumentException>(() => Guard.Against(true, "boom"));
    }

    [Fact]
    public void Against_DoesNotThrow_WhenConditionFalse()
    {
        Guard.Against(false, "ok");
    }

    [Fact]
    public void ValidateAll_Throws_WhenAnyConditionTrue()
    {
        var ex = Assert.Throws<ArgumentException>(() => Guard.ValidateAll(
            (true, "one"),
            (false, "two"),
            (true, "three")));

        Assert.Contains("one", ex.Message);
        Assert.Contains("three", ex.Message);
        Assert.DoesNotContain("two", ex.Message);
    }

    [Fact]
    public void ValidateAll_DoesNotThrow_WhenAllConditionsFalse()
    {
        Guard.ValidateAll(
            (false, "one"),
            (false, "two"));
    }

    private sealed class NonReadableStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 0;
        public override long Position { get => 0; set { } }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => 0;
        public override long Seek(long offset, SeekOrigin origin) => 0;
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
    }
}
