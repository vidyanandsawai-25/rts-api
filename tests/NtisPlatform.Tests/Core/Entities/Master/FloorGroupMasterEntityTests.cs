using NtisPlatform.Core.Entities.Master;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities.Master;

public class FloorGroupMasterEntityTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var entity = new FloorGroupMasterEntity
        {
            Id = 1,
            FloorGroup = "Ground",
            IsActive = true
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal("Ground", entity.FloorGroup);
    }
}
