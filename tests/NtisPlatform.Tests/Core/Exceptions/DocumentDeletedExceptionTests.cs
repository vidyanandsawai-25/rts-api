using NtisPlatform.Core.Exceptions;
using Xunit;

namespace NtisPlatform.Tests.Core.Exceptions;

public class DocumentDeletedExceptionTests
{
    [Fact]
    public void Constructor_PopulatesProperties()
    {
        var guid = Guid.NewGuid();

        var ex = new DocumentDeletedException(guid);

        Assert.Equal(guid, ex.DocumentGuid);
        Assert.Equal("DOCUMENT_DELETED", ex.ErrorCode);
        Assert.Contains(guid.ToString(), ex.Message);
    }
}
