using NtisPlatform.Application.Enums;
using Xunit;

namespace NtisPlatform.Tests.Application.Enums;

public class EnumsTests
{
    [Fact]
    public void FilterOperator_HasExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)FilterOperator.Equals);
        Assert.Equal(1, (int)FilterOperator.Contains);
        Assert.Equal(2, (int)FilterOperator.StartsWith);
        Assert.Equal(3, (int)FilterOperator.EndsWith);
        Assert.Equal(4, (int)FilterOperator.GreaterThan);
        Assert.Equal(5, (int)FilterOperator.LessThan);
        Assert.Equal(6, (int)FilterOperator.GreaterThanOrEqual);
        Assert.Equal(7, (int)FilterOperator.LessThanOrEqual);
        Assert.Equal(8, (int)FilterOperator.In);
        Assert.Equal(9, (int)FilterOperator.IsNull);
        Assert.Equal(10, (int)FilterOperator.IsNotNull);
        Assert.Equal(11, (int)FilterOperator.NotIn);
        Assert.Equal(12, (int)FilterOperator.NotEquals);
        Assert.Equal(13, (int)FilterOperator.Between);
        Assert.Equal(14, (int)FilterOperator.Top);
    }

    [Fact]
    public void FilterOperator_CanBeConvertedToString()
    {
        // Act & Assert
        Assert.Equal("Equals", FilterOperator.Equals.ToString());
        Assert.Equal("Contains", FilterOperator.Contains.ToString());
        Assert.Equal("GreaterThan", FilterOperator.GreaterThan.ToString());
    }

    [Fact]
    public void FilterOperator_CanBeParsed()
    {
        // Act
        var equals = Enum.Parse<FilterOperator>("Equals");
        var contains = Enum.Parse<FilterOperator>("Contains");

        // Assert
        Assert.Equal(FilterOperator.Equals, equals);
        Assert.Equal(FilterOperator.Contains, contains);
    }

    [Theory]
    [InlineData(FilterOperator.Equals)]
    [InlineData(FilterOperator.Contains)]
    [InlineData(FilterOperator.StartsWith)]
    [InlineData(FilterOperator.EndsWith)]
    [InlineData(FilterOperator.GreaterThan)]
    [InlineData(FilterOperator.LessThan)]
    [InlineData(FilterOperator.GreaterThanOrEqual)]
    [InlineData(FilterOperator.LessThanOrEqual)]
    [InlineData(FilterOperator.In)]
    [InlineData(FilterOperator.IsNull)]
    [InlineData(FilterOperator.IsNotNull)]
    [InlineData(FilterOperator.NotIn)]
    [InlineData(FilterOperator.NotEquals)]
    [InlineData(FilterOperator.Between)]
    [InlineData(FilterOperator.Top)]
    public void FilterOperator_AllValuesDefined(FilterOperator op)
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(FilterOperator), op));
    }

    [Fact]
    public void FilterLogic_HasExpectedValues()
    {
        // Arrange
        var andValue = (int)FilterLogic.And;
        var orValue = (int)FilterLogic.Or;

        // Assert
        Assert.Equal(0, andValue);
        Assert.Equal(1, orValue);
    }

    [Fact]
    public void FilterLogic_CanBeConvertedToString()
    {
        // Act & Assert
        Assert.Equal("And", FilterLogic.And.ToString());
        Assert.Equal("Or", FilterLogic.Or.ToString());
    }

    [Fact]
    public void FilterLogic_CanBeParsed()
    {
        // Act
        var and = Enum.Parse<FilterLogic>("And");
        var or = Enum.Parse<FilterLogic>("Or");

        // Assert
        Assert.Equal(FilterLogic.And, and);
        Assert.Equal(FilterLogic.Or, or);
    }

    [Fact]
    public void OperationType_HasExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)OperationType.Create);
        Assert.Equal(1, (int)OperationType.Update);
        Assert.Equal(2, (int)OperationType.Delete);
    }

    [Fact]
    public void OperationType_CanBeConvertedToString()
    {
        // Act & Assert
        Assert.Equal("Create", OperationType.Create.ToString());
        Assert.Equal("Update", OperationType.Update.ToString());
        Assert.Equal("Delete", OperationType.Delete.ToString());
    }

    [Theory]
    [InlineData(OperationType.Create)]
    [InlineData(OperationType.Update)]
    [InlineData(OperationType.Delete)]
    public void OperationType_AllValuesDefined(OperationType op)
    {
        // Assert
        Assert.True(Enum.IsDefined(typeof(OperationType), op));
    }
}
