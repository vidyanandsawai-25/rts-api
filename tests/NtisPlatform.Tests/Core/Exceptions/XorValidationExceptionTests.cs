using NtisPlatform.Core.Exceptions;
using Xunit;

namespace NtisPlatform.Tests.Core.Exceptions;

public class XorValidationExceptionTests
{
    [Fact]
    public void Constructor_PopulatesProperties()
    {
        var ex = new XorValidationException("ReferenceTableId", "ReferenceTableIdGuid");

        Assert.Equal("ReferenceTableId", ex.Parameter1Name);
        Assert.Equal("ReferenceTableIdGuid", ex.Parameter2Name);
        Assert.Equal("XOR_VALIDATION_FAILED", ex.ErrorCode);
    }
}
