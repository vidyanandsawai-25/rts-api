using NtisPlatform.Core.Exceptions;
using Xunit;

namespace NtisPlatform.Tests.Core.Exceptions;

public class FileStorageExceptionTests
{
    [Fact]
    public void Constructor_WithoutInner_PopulatesProperties()
    {
        var ex = new FileStorageException("disk full", "/tmp/x");

        Assert.Equal("/tmp/x", ex.FilePath);
        Assert.Equal("FILE_STORAGE_ERROR", ex.ErrorCode);
        Assert.Null(ex.InnerException);
    }

    [Fact]
    public void Constructor_WithInner_PopulatesInnerException()
    {
        var inner = new InvalidOperationException("io");
        var ex = new FileStorageException("disk full", "/tmp/x", inner);

        Assert.Same(inner, ex.InnerException);
        Assert.Equal("/tmp/x", ex.FilePath);
    }

    [Fact]
    public void Constructor_AllowsNullFilePath()
    {
        var ex = new FileStorageException("oops");

        Assert.Null(ex.FilePath);
    }
}
