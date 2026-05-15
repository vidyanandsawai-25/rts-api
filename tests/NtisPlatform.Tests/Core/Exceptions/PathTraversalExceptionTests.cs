using NtisPlatform.Core.Exceptions;
using Xunit;

namespace NtisPlatform.Tests.Core.Exceptions;

public class PathTraversalExceptionTests
{
    [Fact]
    public void Constructor_PopulatesProperties()
    {
        var ex = new PathTraversalException("../etc/passwd");

        Assert.Equal("../etc/passwd", ex.AttemptedPath);
        Assert.Equal("PATH_TRAVERSAL_DETECTED", ex.ErrorCode);
        Assert.Contains("../etc/passwd", ex.Message);
    }
}
