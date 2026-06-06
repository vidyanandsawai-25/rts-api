using NtisPlatform.Application.DTOs;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs;

public class FloorGroupQueryParametersTests
{
    [Fact]
    public void Properties_RoundTrip()
    {
        // Arrange & Act
        var query = new FloorGroupQueryParameters { FloorGroup = "Ground Floor" };

        // Assert
        Assert.Equal("Ground Floor", query.FloorGroup);
    }

    [Fact]
    public void FloorGroup_CanBeNull()
    {
        // Arrange & Act
        var query = new FloorGroupQueryParameters { FloorGroup = null };

        // Assert
        Assert.Null(query.FloorGroup);
    }

    [Fact]
    public void FloorGroup_CanBeEmpty()
    {
        // Arrange & Act
        var query = new FloorGroupQueryParameters { FloorGroup = "" };

        // Assert
        Assert.Equal("", query.FloorGroup);
    }

    [Fact]
    public void DefaultConstructor_InitializesWithNull()
    {
        // Arrange & Act
        var query = new FloorGroupQueryParameters();

        // Assert
        Assert.Null(query.FloorGroup);
    }

    [Fact]
    public void InheritsFromBaseQueryParameters()
    {
        // Arrange & Act
        var query = new FloorGroupQueryParameters();

        // Assert
        Assert.IsAssignableFrom<NtisPlatform.Application.DTOs.Queries.BaseQueryParameters>(query);
    }
}
