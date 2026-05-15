using NtisPlatform.Core.Exceptions;
using Xunit;

namespace NtisPlatform.Tests.Core.Exceptions;

public class DocumentNotFoundExceptionTests
{
    [Fact]
    public void Constructor_WithInt_PopulatesProperties()
    {
        var ex = new DocumentNotFoundException(42);

        Assert.Equal("Document", ex.EntityType);
        Assert.Equal(42, ex.EntityId);
        Assert.Equal("DOCUMENT_NOT_FOUND", ex.ErrorCode);
        Assert.Contains("42", ex.Message);
    }

    [Fact]
    public void Constructor_WithGuid_PopulatesProperties()
    {
        var guid = Guid.NewGuid();

        var ex = new DocumentNotFoundException(guid);

        Assert.Equal("Document", ex.EntityType);
        Assert.Equal(guid, ex.EntityId);
        Assert.Equal("DOCUMENT_NOT_FOUND", ex.ErrorCode);
    }
}
