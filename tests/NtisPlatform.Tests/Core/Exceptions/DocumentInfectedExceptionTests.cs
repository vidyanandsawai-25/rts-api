using NtisPlatform.Core.Exceptions;
using Xunit;

namespace NtisPlatform.Tests.Core.Exceptions;

public class DocumentInfectedExceptionTests
{
    [Fact]
    public void Constructor_PopulatesProperties()
    {
        var guid = Guid.NewGuid();

        var ex = new DocumentInfectedException(guid);

        Assert.Equal(guid, ex.DocumentGuid);
        Assert.Equal("DOCUMENT_INFECTED", ex.ErrorCode);
    }
}
