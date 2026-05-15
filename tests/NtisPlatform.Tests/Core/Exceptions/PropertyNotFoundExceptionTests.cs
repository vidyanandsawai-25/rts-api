using NtisPlatform.Core.Exceptions;
using Xunit;

namespace NtisPlatform.Tests.Core.Exceptions;

public class PropertyNotFoundExceptionTests
{
    [Fact]
    public void Constructor_PopulatesProperties()
    {
        var ex = new PropertyNotFoundException(7);

        Assert.Equal("Property", ex.EntityType);
        Assert.Equal(7, ex.EntityId);
        Assert.Equal("PROPERTY_NOT_FOUND", ex.ErrorCode);
    }
}
