using NtisPlatform.Core.Exceptions;
using Xunit;

namespace NtisPlatform.Tests.Core.Exceptions;

public class InvalidFileNameExceptionTests
{
    [Fact]
    public void Constructor_PopulatesProperties()
    {
        var ex = new InvalidFileNameException("../bad.txt", "path traversal");

        Assert.Equal("../bad.txt", ex.FileName);
        Assert.Equal("INVALID_FILE_NAME", ex.ErrorCode);
        Assert.Contains("path traversal", ex.Message);
    }
}
