using NtisPlatform.Core.Exceptions;
using Xunit;

namespace NtisPlatform.Tests.Core.Exceptions;

public class InvalidFileTypeExceptionTests
{
    [Fact]
    public void Constructor_PopulatesProperties()
    {
        var ex = new InvalidFileTypeException("virus.exe", "application/x-msdownload", ".exe");

        Assert.Equal("virus.exe", ex.FileName);
        Assert.Equal("application/x-msdownload", ex.ContentType);
        Assert.Equal(".exe", ex.FileExtension);
        Assert.Equal("INVALID_FILE_TYPE", ex.ErrorCode);
    }
}
