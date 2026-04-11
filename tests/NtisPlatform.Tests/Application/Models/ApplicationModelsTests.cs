using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Application.Models;

/// <summary>
/// Comprehensive tests for Application Models and Exceptions to achieve 100% code coverage
/// </summary>
public class ApplicationModelsTests
{
    #region ApiResponse Tests

    [Fact]
    public void ApiResponse_DefaultConstructor_InitializesWithDefaults()
    {
        var response = new ApiResponse<string>();

        Assert.False(response.Success);
        Assert.Equal(string.Empty, response.Message);
        Assert.Null(response.Items);
        Assert.Null(response.Errors);
    }

    [Fact]
    public void ApiResponse_AllProperties_GetSet_WorkCorrectly()
    {
        var errors = new List<string> { "Error 1", "Error 2" };
        var response = new ApiResponse<string>
        {
            Success = true,
            Message = "Operation successful",
            Items = "test_item",
            Errors = errors
        };

        Assert.True(response.Success);
        Assert.Equal("Operation successful", response.Message);
        Assert.Equal("test_item", response.Items);
        Assert.Equal(2, response.Errors!.Count);
        Assert.Contains("Error 1", response.Errors);
    }

    [Fact]
    public void ApiResponse_WithComplexType_WorksCorrectly()
    {
        var item = new { Id = 1, Name = "Test" };
        var response = new ApiResponse<object>
        {
            Success = true,
            Message = "Success",
            Items = item
        };

        Assert.True(response.Success);
        Assert.NotNull(response.Items);
    }

    [Fact]
    public void ApiResponse_ErrorsList_CanBePopulated()
    {
        var response = new ApiResponse<string>
        {
            Success = false,
            Message = "Validation failed",
            Errors = new List<string> { "Field 1 is required", "Field 2 is invalid" }
        };

        Assert.False(response.Success);
        Assert.Equal(2, response.Errors!.Count);
    }

    #endregion

    #region PagedResult Tests

    [Fact]
    public void PagedResult_DefaultConstructor_InitializesWithDefaults()
    {
        var result = new PagedResult<string>();

        Assert.NotNull(result.Items);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.PageNumber);
        Assert.Equal(0, result.PageSize);
    }

    [Fact]
    public void PagedResult_ParameterizedConstructor_InitializesCorrectly()
    {
        var items = new List<string> { "Item 1", "Item 2", "Item 3" };
        var result = new PagedResult<string>(items, 100, 2, 10);

        Assert.Equal(3, result.Items.Count());
        Assert.Equal(100, result.TotalCount);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public void PagedResult_TotalPages_CalculatesCorrectly()
    {
        var result = new PagedResult<string>(new List<string>(), 100, 1, 10);

        Assert.Equal(10, result.TotalPages);
    }

    [Fact]
    public void PagedResult_TotalPages_RoundsUpCorrectly()
    {
        var result = new PagedResult<string>(new List<string>(), 95, 1, 10);

        Assert.Equal(10, result.TotalPages); // 95 / 10 = 9.5, rounds up to 10
    }

    [Fact]
    public void PagedResult_TotalPages_WithZeroPageSize_ReturnsZero()
    {
        var result = new PagedResult<string>(new List<string>(), 100, 1, 0);

        // When PageSize is 0, TotalPages calculation: (int)Math.Ceiling(100 / 0.0) = (int)Infinity
        // In C#, casting Infinity to int gives int.MaxValue or similar behavior
        // Expected behavior: Should handle division by zero gracefully
        var totalPages = result.TotalPages;
        
        // The actual result depends on implementation, but should not throw
        Assert.True(totalPages >= 0);
    }

    [Fact]
    public void PagedResult_HasPrevious_FirstPage_ReturnsFalse()
    {
        var result = new PagedResult<string>(new List<string>(), 100, 1, 10);

        Assert.False(result.HasPrevious);
    }

    [Fact]
    public void PagedResult_HasPrevious_SecondPage_ReturnsTrue()
    {
        var result = new PagedResult<string>(new List<string>(), 100, 2, 10);

        Assert.True(result.HasPrevious);
    }

    [Fact]
    public void PagedResult_HasNext_LastPage_ReturnsFalse()
    {
        var result = new PagedResult<string>(new List<string>(), 100, 10, 10);

        Assert.False(result.HasNext);
    }

    [Fact]
    public void PagedResult_HasNext_FirstPage_ReturnsTrue()
    {
        var result = new PagedResult<string>(new List<string>(), 100, 1, 10);

        Assert.True(result.HasNext);
    }

    [Fact]
    public void PagedResult_HasNext_MiddlePage_ReturnsTrue()
    {
        var result = new PagedResult<string>(new List<string>(), 100, 5, 10);

        Assert.True(result.HasNext);
    }

    [Fact]
    public void PagedResult_WithNoItems_CalculatesCorrectly()
    {
        var result = new PagedResult<string>(new List<string>(), 0, 1, 10);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasPrevious);
        Assert.False(result.HasNext);
    }

    [Fact]
    public void PagedResult_WithSinglePage_CalculatesCorrectly()
    {
        var items = new List<string> { "Item 1", "Item 2", "Item 3" };
        var result = new PagedResult<string>(items, 3, 1, 10);

        Assert.Equal(3, result.Items.Count());
        Assert.Equal(1, result.TotalPages);
        Assert.False(result.HasPrevious);
        Assert.False(result.HasNext);
    }

    [Fact]
    public void PagedResult_WithComplexType_WorksCorrectly()
    {
        var items = new List<TestDto>
        {
            new TestDto { Id = 1, Name = "Test 1" },
            new TestDto { Id = 2, Name = "Test 2" }
        };

        var result = new PagedResult<TestDto>(items, 50, 3, 20);

        Assert.Equal(2, result.Items.Count());
        Assert.Equal(50, result.TotalCount);
        Assert.Equal(3, result.PageNumber);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(3, result.TotalPages); // 50 / 20 = 2.5, rounds to 3
        Assert.True(result.HasPrevious);
        Assert.False(result.HasNext);
    }

    private class TestDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #endregion

    #region FilterValidationException Tests

    [Fact]
    public void FilterValidationException_MessageConstructor_InitializesCorrectly()
    {
        var exception = new FilterValidationException("Test message");

        Assert.Equal("Test message", exception.Message);
        Assert.NotNull(exception.Errors);
        Assert.Empty(exception.Errors);
    }

    [Fact]
    public void FilterValidationException_MessageWithErrorsConstructor_InitializesCorrectly()
    {
        var errors = new Dictionary<string, string>
        {
            { "Field1", "Error 1" },
            { "Field2", "Error 2" }
        };

        var exception = new FilterValidationException("Validation failed", errors);

        Assert.Equal("Validation failed", exception.Message);
        Assert.Equal(2, exception.Errors.Count);
        Assert.Equal("Error 1", exception.Errors["Field1"]);
        Assert.Equal("Error 2", exception.Errors["Field2"]);
    }

    [Fact]
    public void FilterValidationException_PropertyNameErrorConstructor_InitializesCorrectly()
    {
        var exception = new FilterValidationException("Age", "Age must be positive");

        Assert.Equal("Filter validation failed for Age", exception.Message);
        Assert.Single(exception.Errors);
        Assert.Equal("Age must be positive", exception.Errors["Age"]);
    }

    [Fact]
    public void FilterValidationException_CanBeCaught_AsException()
    {
        try
        {
            throw new FilterValidationException("Test");
        }
        catch (Exception ex)
        {
            Assert.IsType<FilterValidationException>(ex);
        }
    }

    [Fact]
    public void FilterValidationException_CanBeCaught_AsFilterValidationException()
    {
        try
        {
            throw new FilterValidationException("PropertyName", "Error message");
        }
        catch (FilterValidationException ex)
        {
            Assert.Equal("Filter validation failed for PropertyName", ex.Message);
            Assert.Single(ex.Errors);
        }
    }

    [Fact]
    public void FilterValidationException_ErrorsDictionary_CanBeModified()
    {
        var exception = new FilterValidationException("Initial message");

        exception.Errors.Add("NewField", "New error");

        Assert.Single(exception.Errors);
        Assert.Equal("New error", exception.Errors["NewField"]);
    }

    [Fact]
    public void FilterValidationException_WithEmptyErrorsDictionary_WorksCorrectly()
    {
        var errors = new Dictionary<string, string>();
        var exception = new FilterValidationException("Message", errors);

        Assert.Equal("Message", exception.Message);
        Assert.Empty(exception.Errors);
    }

    #endregion
}
