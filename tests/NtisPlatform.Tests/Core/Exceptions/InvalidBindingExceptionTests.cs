using NtisPlatform.Core.Exceptions;
using Xunit;

namespace NtisPlatform.Tests.Core.Exceptions;

public class InvalidBindingExceptionTests
{
    [Fact]
    public void Constructor_PopulatesProperties()
    {
        var ex = new InvalidBindingException("bad", "PROP", "Property");

        Assert.Equal("PROP", ex.ModuleCode);
        Assert.Equal("Property", ex.ReferenceTableName);
        Assert.Equal("INVALID_BINDING", ex.ErrorCode);
    }

    [Fact]
    public void Constructor_AllowsNullOptionalParams()
    {
        var ex = new InvalidBindingException("bad");

        Assert.Null(ex.ModuleCode);
        Assert.Null(ex.ReferenceTableName);
    }
}
