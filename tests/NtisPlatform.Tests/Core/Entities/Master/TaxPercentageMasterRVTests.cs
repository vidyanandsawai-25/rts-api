using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities.Master;

public class TaxPercentageMasterRVTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var entity = new TaxPercentageMasterRVEntity
        {
            Id = 1,
            YearRangeRVId = 4,
            TypeOfUseId = 6
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(4, entity.YearRangeRVId);
        Assert.Equal(6, entity.TypeOfUseId);
    }
}
