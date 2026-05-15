using NtisPlatform.Core.Entities.Master;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities.Master;

public class FlagMasterEntityTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var entity = new FlagMasterEntity
        {
            Id = 1,
            PropertyId = 99,
            Lift = true,
            IsActive = true
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(99, entity.PropertyId);
        Assert.True(entity.Lift);
        Assert.True(entity.IsActive);
    }
}
