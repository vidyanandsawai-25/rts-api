using NtisPlatform.Core.Exceptions;
using Xunit;

namespace NtisPlatform.Tests.Core.Exceptions;

public class DocumentExpiredExceptionTests
{
    [Fact]
    public void Constructor_PopulatesProperties()
    {
        var guid = Guid.NewGuid();
        var expiry = new DateTime(2024, 6, 1);

        var ex = new DocumentExpiredException(guid, expiry);

        Assert.Equal(guid, ex.DocumentGuid);
        Assert.Equal(expiry, ex.ExpiryDate);
        Assert.Equal("DOCUMENT_EXPIRED", ex.ErrorCode);
        Assert.Contains("2024-06-01", ex.Message);
    }
}
