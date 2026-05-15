using NtisPlatform.Core.Exceptions;
using Xunit;

namespace NtisPlatform.Tests.Core.Exceptions;

public class FileSizeLimitExceededExceptionTests
{
    [Fact]
    public void Constructor_PopulatesProperties()
    {
        var ex = new FileSizeLimitExceededException("big.pdf", 2_000_000, 1_000_000);

        Assert.Equal("big.pdf", ex.FileName);
        Assert.Equal(2_000_000, ex.FileSize);
        Assert.Equal(1_000_000, ex.MaxFileSize);
        Assert.Equal("FILE_SIZE_EXCEEDED", ex.ErrorCode);
    }
}
