using NtisPlatform.Core.Entities;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities;

public class PolicyTaxDetailsEntityTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var entity = new PolicyTaxDetailsEntity
        {
            Id = 1,
            PropertyId = 9,
            PolicyCode = "POL001",
            PolicyDate = new DateTime(2024, 1, 5),
            PolicyYear = 2024,
            PolicyReason = "Renewal",
            PolicyRVorCVvalue = 10000m,
            TaxId = 3,
            TaxAmount = 250m,
            MarkedForDeletion = false,
            MarkedForDeletionDate = null
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(9, entity.PropertyId);
        Assert.Equal("POL001", entity.PolicyCode);
        Assert.Equal(new DateTime(2024, 1, 5), entity.PolicyDate);
        Assert.Equal((short?)2024, entity.PolicyYear);
        Assert.Equal("Renewal", entity.PolicyReason);
        Assert.Equal(10000m, entity.PolicyRVorCVvalue);
        Assert.Equal(3, entity.TaxId);
        Assert.Equal(250m, entity.TaxAmount);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
    }
}
