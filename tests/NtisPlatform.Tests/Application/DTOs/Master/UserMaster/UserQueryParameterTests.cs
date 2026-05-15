using NtisPlatform.Application.DTOs.Master.UserMaster;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Master.UserMaster;

public class UserQueryParameterTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var query = new UserQueryParameter
        {
            UserName = "alice",
            FirstName = "Alice",
            MiddleName = "M",
            LastName = "Liddell",
            MobileNo = "555-1234"
        };

        Assert.Equal("alice", query.UserName);
        Assert.Equal("Alice", query.FirstName);
        Assert.Equal("M", query.MiddleName);
        Assert.Equal("Liddell", query.LastName);
        Assert.Equal("555-1234", query.MobileNo);
    }
}
