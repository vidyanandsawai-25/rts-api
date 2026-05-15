using NtisPlatform.Core.Exceptions;
using Xunit;

namespace NtisPlatform.Tests.Core.Exceptions;

public class PropertyCertificateNotFoundExceptionTests
{
    [Fact]
    public void Constructor_PopulatesProperties()
    {
        var ex = new PropertyCertificateNotFoundException(3);

        Assert.Equal("PropertyCertificate", ex.EntityType);
        Assert.Equal(3, ex.EntityId);
        Assert.Equal("PROPERTY_CERTIFICATE_NOT_FOUND", ex.ErrorCode);
    }
}
