using NtisPlatform.Core.Exceptions;
using Xunit;

namespace NtisPlatform.Tests.Core.Exceptions;

public class CertificateTypeNotFoundExceptionTests
{
    [Fact]
    public void Constructor_PopulatesProperties()
    {
        var ex = new CertificateTypeNotFoundException(5);

        Assert.Equal("CertificateType", ex.EntityType);
        Assert.Equal(5, ex.EntityId);
        Assert.Equal("CERTIFICATE_TYPE_NOT_FOUND", ex.ErrorCode);
    }
}
