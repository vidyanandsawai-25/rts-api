using NtisPlatform.Core.Exceptions;
using Xunit;

namespace NtisPlatform.Tests.Core.Exceptions;

public class DocumentBindingNotFoundExceptionTests
{
    [Fact]
    public void Constructor_PopulatesProperties()
    {
        var ex = new DocumentBindingNotFoundException(7);

        Assert.Equal("DocumentBinding", ex.EntityType);
        Assert.Equal(7, ex.EntityId);
        Assert.Equal("BINDING_NOT_FOUND", ex.ErrorCode);
    }
}
