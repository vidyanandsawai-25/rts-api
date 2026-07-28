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
            PolicyCodeId = 7,
            CreatedDate = new DateTime(2024, 1, 5),
            CalculationValue = 10000m,
            TaxId = 3,
            TaxAmount = 250m,
            MarkedForDeletion = false,
            MarkedForDeletionDate = null
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(9, entity.PropertyId);
        Assert.Equal(7, entity.PolicyCodeId);
        Assert.Equal(new DateTime(2024, 1, 5), entity.CreatedDate);
        Assert.Equal(10000m, entity.CalculationValue);
        Assert.Equal(3, entity.TaxId);
        Assert.Equal(250m, entity.TaxAmount);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
    }
}
