using NtisPlatform.Application.DTOs.Master.WaterConnection;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Master.WaterConnection;

public class WaterConnectionQueryParametersTests
{
    [Fact]
    public void WaterConnection_Properties_RoundTrip()
    {
        var q = new WaterConnectionQueryParameters
        {
            PropertyId = 1,
            IsActive = true,
            ConnectionNo = "C1",
            FinanceYearId = 2024
        };

        Assert.Equal(1, q.PropertyId);
        Assert.True(q.IsActive);
        Assert.Equal("C1", q.ConnectionNo);
        Assert.Equal(2024, q.FinanceYearId);
    }

    [Fact]
    public void WaterConnectionType_Properties_RoundTrip()
    {
        var q = new WaterConnectionTypeQueryParameters { IsActive = true, ConnectionTypeName = "Domestic" };
        Assert.True(q.IsActive);
        Assert.Equal("Domestic", q.ConnectionTypeName);
    }

    [Fact]
    public void WaterConnectionSize_Properties_RoundTrip()
    {
        var q = new WaterConnectionSizeQueryParameters { IsActive = true };
        Assert.True(q.IsActive);
    }

    [Fact]
    public void WaterConnectionStatus_Properties_RoundTrip()
    {
        var q = new WaterConnectionStatusQueryParameters { IsActive = true, StatusName = "Open" };
        Assert.True(q.IsActive);
        Assert.Equal("Open", q.StatusName);
    }

    [Fact]
    public void WaterRateMaster_Properties_RoundTrip()
    {
        var q = new WaterRateMasterQueryParameters
        {
            WaterConnectionTypeId = 1,
            WaterConnectionSizeId = 2,
            FinanceYearId = 2024,
            IsActive = true
        };

        Assert.Equal(1, q.WaterConnectionTypeId);
        Assert.Equal(2, q.WaterConnectionSizeId);
        Assert.Equal(2024, q.FinanceYearId);
        Assert.True(q.IsActive);
    }

    [Fact]
    public void WaterConnectionDetails_Properties_RoundTrip()
    {
        var q = new WaterConnectionDetailsQueryParameters
        {
            WaterConnectionId = 5,
            FinanceYearId = 2024,
            IsActive = true
        };

        Assert.Equal(5, q.WaterConnectionId);
        Assert.Equal(2024, q.FinanceYearId);
        Assert.True(q.IsActive);
    }
}
