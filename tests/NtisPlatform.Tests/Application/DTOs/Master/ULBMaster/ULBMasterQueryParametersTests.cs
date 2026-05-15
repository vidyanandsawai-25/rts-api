using NtisPlatform.Application.DTOs.Master.ULBMaster;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Master.ULBMaster;

public class ULBMasterQueryParametersTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var q = new ULBMasterQueryParameters
        {
            UlbCode = "ULB01",
            UlbName = "Pune",
            UlbNameLocal = "पुणे",
            UlbTypeId = (byte)2,
            IsActive = true,
            EmailId = "a@b.com",
            MobileNo = "12345",
            ContactPersonName = "Person",
            State = "MH",
            District = "Pune",
            PinCode = "411001",
            PartnerName = "Partner",
            PMName = "PM",
            PMEmailId = "pm@x.com",
            LicenceType = "Full",
            SupportType = "Premium"
        };

        Assert.Equal("ULB01", q.UlbCode);
        Assert.Equal("Pune", q.UlbName);
        Assert.Equal("पुणे", q.UlbNameLocal);
        Assert.Equal((byte)2, q.UlbTypeId);
        Assert.True(q.IsActive);
        Assert.Equal("a@b.com", q.EmailId);
        Assert.Equal("12345", q.MobileNo);
        Assert.Equal("Person", q.ContactPersonName);
        Assert.Equal("MH", q.State);
        Assert.Equal("Pune", q.District);
        Assert.Equal("411001", q.PinCode);
        Assert.Equal("Partner", q.PartnerName);
        Assert.Equal("PM", q.PMName);
        Assert.Equal("pm@x.com", q.PMEmailId);
        Assert.Equal("Full", q.LicenceType);
        Assert.Equal("Premium", q.SupportType);
    }
}
