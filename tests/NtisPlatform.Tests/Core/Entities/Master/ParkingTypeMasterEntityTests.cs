using NtisPlatform.Core.Entities.Master;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities.Master;

public class ParkingTypeMasterEntityTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var entity = new ParkingTypeMasterEntity
        {
            Id = 1,
            TypeOfUseId = 7
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(7, entity.TypeOfUseId);
    }
}
