using NtisPlatform.Application.DTOs;

namespace NtisPlatform.Tests.Application.DTOs;

/// <summary>
/// Unit tests for BaseDtos, CreateBaseDtos, and UpdateBaseDtos to ensure 100% code coverage
/// </summary>
public class BaseDtosTests
{
    [Fact]
    public void BaseDtos_AllProperties_GetSet_WorksCorrectly()
    {
        var now = DateTime.Now;
        var dto = new BaseDtos
        {
            Id = 100,
            IsActive = true,
            CreatedDate = now,
            UpdatedDate = now.AddHours(1)
        };

        Assert.Equal(100, dto.Id);
        Assert.True(dto.IsActive);
        Assert.Equal(now, dto.CreatedDate);
        Assert.Equal(now.AddHours(1), dto.UpdatedDate);
    }

    [Fact]
    public void BaseDtos_DefaultValues_SetCorrectly()
    {
        var dto = new BaseDtos();

        Assert.Equal(0, dto.Id);
        Assert.False(dto.IsActive);
        Assert.Null(dto.CreatedDate);
        Assert.Null(dto.UpdatedDate);
    }

    [Fact]
    public void BaseDtos_DateProperties_CanBeNull()
    {
        var dto = new BaseDtos
        {
            Id = 1,
            IsActive = true,
            CreatedDate = null,
            UpdatedDate = null
        };

        Assert.Null(dto.CreatedDate);
        Assert.Null(dto.UpdatedDate);
    }

    [Fact]
    public void BaseDtos_IsActive_BothValues_WorkCorrectly()
    {
        var dto1 = new BaseDtos { IsActive = true };
        var dto2 = new BaseDtos { IsActive = false };

        Assert.True(dto1.IsActive);
        Assert.False(dto2.IsActive);
    }
}

public class CreateBaseDtosTests
{
    [Fact]
    public void CreateBaseDtos_AllProperties_GetSet_WorksCorrectly()
    {
        var dto = new CreateBaseDtos
        {
            IsActive = true,
            CreatedBy = 100
        };

        Assert.True(dto.IsActive);
        Assert.Equal(100, dto.CreatedBy);
    }

    [Fact]
    public void CreateBaseDtos_DefaultValues_SetCorrectly()
    {
        var dto = new CreateBaseDtos();

        Assert.False(dto.IsActive);
        Assert.Null(dto.CreatedBy);
    }

    [Fact]
    public void CreateBaseDtos_CreatedBy_CanBeNull()
    {
        var dto = new CreateBaseDtos
        {
            IsActive = true,
            CreatedBy = null
        };

        Assert.Null(dto.CreatedBy);
    }

    [Fact]
    public void CreateBaseDtos_IsActive_BothValues_WorkCorrectly()
    {
        var dto1 = new CreateBaseDtos { IsActive = true };
        var dto2 = new CreateBaseDtos { IsActive = false };

        Assert.True(dto1.IsActive);
        Assert.False(dto2.IsActive);
    }

    [Fact]
    public void CreateBaseDtos_CreatedBy_PositiveValue_WorksCorrectly()
    {
        var dto = new CreateBaseDtos
        {
            CreatedBy = 12345
        };

        Assert.Equal(12345, dto.CreatedBy);
    }
}

public class UpdateBaseDtosTests
{
    [Fact]
    public void UpdateBaseDtos_AllProperties_GetSet_WorksCorrectly()
    {
        var dto = new UpdateBaseDtos
        {
            IsActive = true,
            UpdatedBy = 200
        };

        Assert.True(dto.IsActive);
        Assert.Equal(200, dto.UpdatedBy);
    }

    [Fact]
    public void UpdateBaseDtos_DefaultValues_SetCorrectly()
    {
        var dto = new UpdateBaseDtos();

        Assert.False(dto.IsActive);
        Assert.Null(dto.UpdatedBy);
    }

    [Fact]
    public void UpdateBaseDtos_UpdatedBy_CanBeNull()
    {
        var dto = new UpdateBaseDtos
        {
            IsActive = true,
            UpdatedBy = null
        };

        Assert.Null(dto.UpdatedBy);
    }

    [Fact]
    public void UpdateBaseDtos_IsActive_BothValues_WorkCorrectly()
    {
        var dto1 = new UpdateBaseDtos { IsActive = true };
        var dto2 = new UpdateBaseDtos { IsActive = false };

        Assert.True(dto1.IsActive);
        Assert.False(dto2.IsActive);
    }

    [Fact]
    public void UpdateBaseDtos_UpdatedBy_PositiveValue_WorksCorrectly()
    {
        var dto = new UpdateBaseDtos
        {
            UpdatedBy = 99999
        };

        Assert.Equal(99999, dto.UpdatedBy);
    }

    [Fact]
    public void UpdateBaseDtos_UpdatedBy_ZeroValue_WorksCorrectly()
    {
        var dto = new UpdateBaseDtos
        {
            UpdatedBy = 0
        };

        Assert.Equal(0, dto.UpdatedBy);
    }
}
