using NtisPlatform.Core.Exceptions;
using Xunit;

namespace NtisPlatform.Tests.Core.Exceptions;

public class InvalidFileExceptionTests
{
    [Fact]
    public void Constructor_PopulatesProperties()
    {
        var ex = new InvalidFileException("file.txt", "too large");

        Assert.Equal("file.txt", ex.FileName);
        Assert.Equal("too large", ex.Reason);
        Assert.Equal("INVALID_FILE", ex.ErrorCode);
        Assert.Contains("file.txt", ex.Message);
        Assert.Contains("too large", ex.Message);
    }
}
