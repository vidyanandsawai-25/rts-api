using NtisPlatform.Core.Exceptions;
using Xunit;

namespace NtisPlatform.Tests.Core.Exceptions;

public class ChecksumMismatchExceptionTests
{
    [Fact]
    public void Constructor_PopulatesProperties()
    {
        var ex = new ChecksumMismatchException("doc.pdf", "abc", "xyz");

        Assert.Equal("doc.pdf", ex.FileName);
        Assert.Equal("abc", ex.ExpectedChecksum);
        Assert.Equal("xyz", ex.ActualChecksum);
        Assert.Equal("CHECKSUM_MISMATCH", ex.ErrorCode);
    }
}
