using System.ComponentModel.DataAnnotations;
using Xunit;
using NtisPlatform.Application.DTOs.Range;

namespace NtisPlatform.Tests.Application.DTOs.Range;

public class RangeCreateRequestDtosTests
{
    private class TestCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    [Fact]
    public void RangeCreateRequest_WithValidProperties_SetsCorrectly()
    {
        // Arrange
        var template = new TestCreateDto { Name = "Test", Value = 100 };

        // Act
        var request = new RangeCreateRequest<TestCreateDto>
        {
            RangeFrom = "1",
            RangeTo = "10",
            Prefix = "Item-",
            Suffix = "-End",
            Template = template,
            StartSequenceNo = 5
        };

        // Assert
        Assert.Equal("1", request.RangeFrom);
        Assert.Equal("10", request.RangeTo);
        Assert.Equal("Item-", request.Prefix);
        Assert.Equal("-End", request.Suffix);
        Assert.Equal(template, request.Template);
        Assert.Equal(5, request.StartSequenceNo);
    }

    [Fact]
    public void RangeCreateRequest_WithAlphabeticRange_WorksCorrectly()
    {
        // Arrange
        var template = new TestCreateDto { Name = "Alpha", Value = 50 };

        // Act
        var request = new RangeCreateRequest<TestCreateDto>
        {
            RangeFrom = "A",
            RangeTo = "Z",
            Prefix = "Letter-",
            Suffix = null,
            Template = template
        };

        // Assert
        Assert.Equal("A", request.RangeFrom);
        Assert.Equal("Z", request.RangeTo);
        Assert.Equal("Letter-", request.Prefix);
        Assert.Null(request.Suffix);
    }

    [Fact]
    public void RangeCreateRequest_WithNullPrefixAndSuffix_WorksCorrectly()
    {
        // Arrange
        var template = new TestCreateDto { Name = "Null Test", Value = 25 };

        // Act
        var request = new RangeCreateRequest<TestCreateDto>
        {
            RangeFrom = "100",
            RangeTo = "200",
            Prefix = null,
            Suffix = null,
            Template = template
        };

        // Assert
        Assert.Null(request.Prefix);
        Assert.Null(request.Suffix);
    }

    [Fact]
    public void RangeCreateRequest_DefaultStartSequenceNo_IsOne()
    {
        // Arrange & Act
        var request = new RangeCreateRequest<TestCreateDto>
        {
            RangeFrom = "1",
            RangeTo = "5",
            Template = new TestCreateDto()
        };

        // Assert
        Assert.Equal(1, request.StartSequenceNo);
    }

    [Fact]
    public void RangeCreateRequest_WithEmptyStrings_WorksCorrectly()
    {
        // Arrange
        var template = new TestCreateDto();

        // Act
        var request = new RangeCreateRequest<TestCreateDto>
        {
            RangeFrom = string.Empty,
            RangeTo = string.Empty,
            Prefix = "",
            Suffix = "",
            Template = template
        };

        // Assert
        Assert.Equal(string.Empty, request.RangeFrom);
        Assert.Equal(string.Empty, request.RangeTo);
        Assert.Equal("", request.Prefix);
        Assert.Equal("", request.Suffix);
    }

    [Fact]
    public void RangeCreateRequest_ValidatesRequiredFields()
    {
        // Arrange
        var request = new RangeCreateRequest<TestCreateDto>
        {
            RangeFrom = "",
            RangeTo = "",
            Template = null!
        };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RangeCreateRequest<TestCreateDto>.RangeFrom)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RangeCreateRequest<TestCreateDto>.RangeTo)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RangeCreateRequest<TestCreateDto>.Template)));
    }

    [Fact]
    public void RangeCreateRequest_WithLargeRange_SetsCorrectly()
    {
        // Arrange
        var template = new TestCreateDto { Name = "Large Range", Value = 999 };

        // Act
        var request = new RangeCreateRequest<TestCreateDto>
        {
            RangeFrom = "1000",
            RangeTo = "9999",
            Template = template,
            StartSequenceNo = 100
        };

        // Assert
        Assert.Equal("1000", request.RangeFrom);
        Assert.Equal("9999", request.RangeTo);
        Assert.Equal(100, request.StartSequenceNo);
    }
}
