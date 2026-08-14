using NtisPlatform.Application.DTOs.Property.ApartmentQC;
using NtisPlatform.Core.Entities;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure;

/// <summary>
/// Tests for ApartmentQCRepository.ApplyDetailPatches — pure in-memory logic,
/// no EF Core / database required. Uses a test subclass to expose the internal method.
/// </summary>
public class ApartmentQCRepositoryPatchTests
{
    private static Dictionary<int, PropertyDetailsEntity> MakeDict(params PropertyDetailsEntity[] entities)
        => entities.ToDictionary(e => e.Id);

    private static void ApplyPatches(
        Dictionary<int, PropertyDetailsEntity> dict,
        IEnumerable<UpdateApartmentQCDetailsDto> dtos,
        int updatedBy = 1)
    {
        // Instantiate via reflection so we don't need a real DbContext just for this test.
        var repo = (ApartmentQCRepository)System.Runtime.CompilerServices.RuntimeHelpers
            .GetUninitializedObject(typeof(ApartmentQCRepository));

        // Call the public method directly (it is public on the class).
        repo.ApplyDetailPatches(dict, dtos, updatedBy);
    }

    [Fact]
    public void ApplyDetailPatches_SetsFloorId_WhenProvided()
    {
        var entity = new PropertyDetailsEntity { Id = 1, FloorId = 0 };
        var dict   = MakeDict(entity);

        ApplyPatches(dict, [new UpdateApartmentQCDetailsDto { DetailId = 1, FloorId = 5 }]);

        Assert.Equal(5, entity.FloorId);
    }

    [Fact]
    public void ApplyDetailPatches_SetsConstructionTypeId_WhenProvided()
    {
        var entity = new PropertyDetailsEntity { Id = 1, ConstructionTypeId = 0 };
        var dict   = MakeDict(entity);

        ApplyPatches(dict, [new UpdateApartmentQCDetailsDto { DetailId = 1, ConstructionTypeId = 7 }]);

        Assert.Equal(7, entity.ConstructionTypeId);
    }

    [Fact]
    public void ApplyDetailPatches_SetsTypeOfUseId_WhenProvided()
    {
        var entity = new PropertyDetailsEntity { Id = 1, TypeOfUseId = 0 };
        var dict   = MakeDict(entity);

        ApplyPatches(dict, [new UpdateApartmentQCDetailsDto { DetailId = 1, TypeOfUseId = 3 }]);

        Assert.Equal(3, entity.TypeOfUseId);
    }

    [Fact]
    public void ApplyDetailPatches_SetsSubTypeOfUseId_WhenProvided()
    {
        var entity = new PropertyDetailsEntity { Id = 1, SubTypeOfUseId = null };
        var dict   = MakeDict(entity);

        ApplyPatches(dict, [new UpdateApartmentQCDetailsDto { DetailId = 1, SubTypeOfUseId = 9 }]);

        Assert.Equal(9, entity.SubTypeOfUseId);
    }

    [Fact]
    public void ApplyDetailPatches_PreservesSubTypeOfUseId_WhenTypeOfUseIdNotProvided()
    {
        var entity = new PropertyDetailsEntity { Id = 1, SubTypeOfUseId = 42 };
        var dict   = MakeDict(entity);

        // Only FloorId provided — SubTypeOfUseId must not be touched
        ApplyPatches(dict, [new UpdateApartmentQCDetailsDto { DetailId = 1, FloorId = 1 }]);

        Assert.Equal(42, entity.SubTypeOfUseId);
    }

    [Fact]
    public void ApplyDetailPatches_ClearsSubTypeOfUseId_WhenTypeOfUseIdProvidedWithoutSubType()
    {
        var entity = new PropertyDetailsEntity { Id = 1, SubTypeOfUseId = 42 };
        var dict   = MakeDict(entity);

        // TypeOfUseId changed but no SubTypeOfUseId → sub-type is reset
        ApplyPatches(dict, [new UpdateApartmentQCDetailsDto { DetailId = 1, TypeOfUseId = 3 }]);

        Assert.Null(entity.SubTypeOfUseId);
    }

    [Fact]
    public void ApplyDetailPatches_SetsConstructionYear_WhenProvided()
    {
        var entity = new PropertyDetailsEntity { Id = 1, ConstructionYear = null };
        var dict   = MakeDict(entity);

        ApplyPatches(dict, [new UpdateApartmentQCDetailsDto { DetailId = 1, ConstructionYear = "2020" }]);

        Assert.Equal("2020", entity.ConstructionYear);
    }

    [Fact]
    public void ApplyDetailPatches_SetsAssessmentYear_WhenProvided()
    {
        var entity = new PropertyDetailsEntity { Id = 1, AssessmentYear = null };
        var dict   = MakeDict(entity);

        ApplyPatches(dict, [new UpdateApartmentQCDetailsDto { DetailId = 1, AssessmentYear = "2023" }]);

        Assert.Equal("2023", entity.AssessmentYear);
    }

    [Fact]
    public void ApplyDetailPatches_StampsUpdatedByAndDate()
    {
        var entity = new PropertyDetailsEntity { Id = 1, UpdatedBy = 0, UpdatedDate = null };
        var dict   = MakeDict(entity);
        var before = DateTime.Now.AddSeconds(-1);

        ApplyPatches(dict, [new UpdateApartmentQCDetailsDto { DetailId = 1, FloorId = 1 }], updatedBy: 55);

        Assert.Equal(55, entity.UpdatedBy);
        Assert.NotNull(entity.UpdatedDate);
        Assert.True(entity.UpdatedDate >= before);
    }

    [Fact]
    public void ApplyDetailPatches_SkipsRow_WhenDetailIdNotInDictionary()
    {
        var entity = new PropertyDetailsEntity { Id = 1, FloorId = 10 };
        var dict   = MakeDict(entity);

        // DetailId 999 doesn't exist in dict
        ApplyPatches(dict, [new UpdateApartmentQCDetailsDto { DetailId = 999, FloorId = 5 }]);

        Assert.Equal(10, entity.FloorId); // untouched
    }

    [Fact]
    public void ApplyDetailPatches_MultipleRows_AppliesEachCorrectly()
    {
        var e1 = new PropertyDetailsEntity { Id = 1, FloorId = 0 };
        var e2 = new PropertyDetailsEntity { Id = 2, FloorId = 0 };
        var dict = MakeDict(e1, e2);

        ApplyPatches(dict, [
            new UpdateApartmentQCDetailsDto { DetailId = 1, FloorId = 10 },
            new UpdateApartmentQCDetailsDto { DetailId = 2, FloorId = 20 }
        ]);

        Assert.Equal(10, e1.FloorId);
        Assert.Equal(20, e2.FloorId);
    }

    [Fact]
    public void ApplyDetailPatches_DoesNotOverwriteFloorId_WhenNotProvided()
    {
        var entity = new PropertyDetailsEntity { Id = 1, FloorId = 5 };
        var dict   = MakeDict(entity);

        // Only AssessmentYear is set — FloorId should stay 5
        ApplyPatches(dict, [new UpdateApartmentQCDetailsDto { DetailId = 1, AssessmentYear = "2024" }]);

        Assert.Equal(5, entity.FloorId);
    }
}
