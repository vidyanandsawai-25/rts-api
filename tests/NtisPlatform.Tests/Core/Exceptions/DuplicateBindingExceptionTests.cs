using NtisPlatform.Core.Exceptions;
using Xunit;

namespace NtisPlatform.Tests.Core.Exceptions;

public class DuplicateBindingExceptionTests
{
    [Fact]
    public void Constructor_PopulatesProperties()
    {
        var ex = new DuplicateBindingException(11, "Property", 99);

        Assert.Equal(11, ex.DocumentId);
        Assert.Equal("Property", ex.ReferenceTableName);
        Assert.Equal(99, ex.ReferenceId);
        Assert.Equal("DUPLICATE_BINDING", ex.ErrorCode);
    }
}
