using NtisPlatform.Core.Entities.Master;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities.Master;

public class TaxPercentageMasterCVTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var entity = new TaxPercentageMasterCV
        {
            Id = 1,
            YearRangeCVId = 3,
            TypeOfUseId = 5
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(3, entity.YearRangeCVId);
        Assert.Equal(5, entity.TypeOfUseId);
    }
}
