using NtisPlatform.Core.Entities;
using Xunit;

namespace NtisPlatform.Tests.Core.Entities;

public class PropertyDetailsReassessmentEntityTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        var entity = new PropertyDetailsReassessmentEntity
        {
            Id = 1,
            FloorId = 2,
            SubFloorId = 3,
            ConstructionTypeId = 4,
            TypeOfUseId = 5,
            SubTypeOfUseId = 6
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(2, entity.FloorId);
        Assert.Equal(3, entity.SubFloorId);
        Assert.Equal(4, entity.ConstructionTypeId);
        Assert.Equal(5, entity.TypeOfUseId);
        Assert.Equal(6, entity.SubTypeOfUseId);
    }
}
