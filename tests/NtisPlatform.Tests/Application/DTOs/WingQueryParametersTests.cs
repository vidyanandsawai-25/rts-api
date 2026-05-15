using NtisPlatform.Application.DTOs;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs;

public class WingQueryParametersTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var query = new WingQueryParameters { WingNo = "W1" };

        Assert.Equal("W1", query.WingNo);
    }
}
