using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;
using Xunit;

namespace NtisPlatform.Tests.Application.Extensions;

/// <summary>
/// Comprehensive tests for FilterExpressionBuilder and QueryableExtensions to achieve 100% code coverage
/// </summary>
public class FilterExpressionBuilderTests
{
    private class TestQueryParameters : BaseQueryParameters
    {
        [Filterable]
        [Searchable]
        [Sortable]
        public string? PropertyNo { get; set; }

        [Filterable(FilterOperator.GreaterThan)]
        [Sortable]
        public int? MinId { get; set; }

        [Filterable(FilterOperator.Contains)]
        [Searchable]
        public string? OwnerName { get; set; }
    }

    [Fact]
    public void BuildFilterExpression_NullQueryParameters_ReturnsNull()
    {
        var result = FilterExpressionBuilder.BuildFilterExpression<PropertyEntity, TestQueryParameters>(null!);

        Assert.Null(result);
    }

    [Fact]
    public void BuildFilterExpression_NoFilters_ReturnsNull()
    {
        var queryParams = new TestQueryParameters();

        var result = FilterExpressionBuilder.BuildFilterExpression<PropertyEntity, TestQueryParameters>(queryParams);

        Assert.Null(result);
    }

    [Fact]
    public void BuildFilterExpression_StringEquals_WorksCorrectly()
    {
        var queryParams = new TestQueryParameters
        {
            PropertyNo = "123"
        };

        var expression = FilterExpressionBuilder.BuildFilterExpression<PropertyEntity, TestQueryParameters>(queryParams);

        Assert.NotNull(expression);
    }

    [Fact]
    public void BuildFilterExpression_StringContains_WorksCorrectly()
    {
        var queryParams = new TestQueryParameters
        {
            OwnerName = "John"
        };

        var expression = FilterExpressionBuilder.BuildFilterExpression<PropertyEntity, TestQueryParameters>(queryParams);

        Assert.NotNull(expression);
    }

    [Fact]
    public void BuildSearchExpression_NullSearchTerm_ReturnsNull()
    {
        var queryParams = new TestQueryParameters
        {
            SearchTerm = null
        };

        var result = FilterExpressionBuilder.BuildSearchExpression<PropertyEntity, TestQueryParameters>(queryParams);

        Assert.Null(result);
    }

    [Fact]
    public void BuildSearchExpression_EmptySearchTerm_ReturnsNull()
    {
        var queryParams = new TestQueryParameters
        {
            SearchTerm = "   "
        };

        var result = FilterExpressionBuilder.BuildSearchExpression<PropertyEntity, TestQueryParameters>(queryParams);

        Assert.Null(result);
    }

    [Fact]
    public void BuildSearchExpression_WithSearchTerm_WorksCorrectly()
    {
        var queryParams = new TestQueryParameters
        {
            SearchTerm = "test"
        };

        var expression = FilterExpressionBuilder.BuildSearchExpression<PropertyEntity, TestQueryParameters>(queryParams);

        Assert.NotNull(expression);
    }

    [Fact]
    public void GetSortableFields_ReturnsCorrectFields()
    {
        var fields = FilterExpressionBuilder.GetSortableFields<TestQueryParameters>();

        Assert.NotNull(fields);
        Assert.NotEmpty(fields);
        Assert.Contains("PropertyNo", fields);
    }
}

/// <summary>
/// Comprehensive tests for QueryableExtensions to achieve 100% code coverage
/// </summary>
public class QueryableExtensionsTests
{
    private class TestQueryParameters : BaseQueryParameters
    {
        [Filterable]
        [Searchable]
        [Sortable]
        public string? PropertyNo { get; set; }

        [Filterable]
        [Sortable]
        public int? WardId { get; set; }
    }

    [Fact]
    public async Task ApplyFilters_WithFilters_AppliesCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.PropertyMast.Add(new PropertyEntity { Id = 1, PropertyNo = "123", WardId = 79, TaxZoneId = 10, IsActive = true });
        context.PropertyMast.Add(new PropertyEntity { Id = 2, PropertyNo = "456", WardId = 80, TaxZoneId = 10, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new TestQueryParameters
        {
            WardId = 79
        };

        var result = context.PropertyMast.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Single(result);
        Assert.Equal("123", result[0].PropertyNo);
    }

    [Fact]
    public async Task ApplyFilters_NoFilters_ReturnsAll()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.PropertyMast.Add(new PropertyEntity { Id = 1, WardId = 79, TaxZoneId = 10, IsActive = true });
        context.PropertyMast.Add(new PropertyEntity { Id = 2, WardId = 80, TaxZoneId = 10, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new TestQueryParameters();

        var result = context.PropertyMast.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ApplySearch_WithSearchTerm_AppliesCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.PropertyMast.Add(new PropertyEntity { Id = 1, PropertyNo = "ABC123", WardId = 79, TaxZoneId = 10, IsActive = true });
        context.PropertyMast.Add(new PropertyEntity { Id = 2, PropertyNo = "XYZ789", WardId = 80, TaxZoneId = 10, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new TestQueryParameters
        {
            SearchTerm = "ABC"
        };

        var result = context.PropertyMast.AsQueryable().ApplySearch(queryParams).ToList();

        Assert.Single(result);
        Assert.Equal("ABC123", result[0].PropertyNo);
    }

    [Fact]
    public async Task ApplySearch_NoSearchTerm_ReturnsAll()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.PropertyMast.Add(new PropertyEntity { Id = 1, WardId = 79, TaxZoneId = 10, IsActive = true });
        context.PropertyMast.Add(new PropertyEntity { Id = 2, WardId = 80, TaxZoneId = 10, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new TestQueryParameters();

        var result = context.PropertyMast.AsQueryable().ApplySearch(queryParams).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ApplySort_WithValidSortBy_SortsCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.PropertyMast.Add(new PropertyEntity { Id = 2, PropertyNo = "ZZZ", WardId = 79, TaxZoneId = 10, IsActive = true });
        context.PropertyMast.Add(new PropertyEntity { Id = 1, PropertyNo = "AAA", WardId = 80, TaxZoneId = 10, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new TestQueryParameters
        {
            SortBy = "PropertyNo",
            SortOrder = "asc"
        };

        var result = context.PropertyMast.AsQueryable().ApplySort(queryParams).ToList();

        Assert.Equal("AAA", result[0].PropertyNo);
        Assert.Equal("ZZZ", result[1].PropertyNo);
    }

    [Fact]
    public async Task ApplySort_Descending_SortsCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.PropertyMast.Add(new PropertyEntity { Id = 1, PropertyNo = "AAA", WardId = 79, TaxZoneId = 10, IsActive = true });
        context.PropertyMast.Add(new PropertyEntity { Id = 2, PropertyNo = "ZZZ", WardId = 80, TaxZoneId = 10, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new TestQueryParameters
        {
            SortBy = "PropertyNo",
            SortOrder = "desc"
        };

        var result = context.PropertyMast.AsQueryable().ApplySort(queryParams).ToList();

        Assert.Equal("ZZZ", result[0].PropertyNo);
        Assert.Equal("AAA", result[1].PropertyNo);
    }

    [Fact]
    public async Task ApplySort_NoSortBy_AppliesDefaultIdOrdering()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        // Add entities with Ids in non-sequential order
        context.PropertyMast.Add(new PropertyEntity { Id = 3, PropertyNo = "C", WardId = 1, TaxZoneId = 1, IsActive = true });
        context.PropertyMast.Add(new PropertyEntity { Id = 1, PropertyNo = "A", WardId = 1, TaxZoneId = 1, IsActive = true });
        context.PropertyMast.Add(new PropertyEntity { Id = 2, PropertyNo = "B", WardId = 1, TaxZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new TestQueryParameters
        {
            SortBy = null
        };

        var result = context.PropertyMast.AsQueryable().ApplySort(queryParams).ToList();

        // Assert default ordering by Id (ascending)
        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
        Assert.Equal(3, result[2].Id);
    }

    [Fact]
    public void ApplySort_InvalidSortBy_ThrowsException()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var queryParams = new TestQueryParameters
        {
            SortBy = "InvalidField"
        };

        Assert.Throws<FilterValidationException>(() => context.PropertyMast.AsQueryable().ApplySort(queryParams));
    }

    [Fact]
    public async Task ToPagedResultAsync_ReturnsCorrectPagination()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        for (int i = 1; i <= 50; i++)
        {
            context.PropertyMast.Add(new PropertyEntity
            {
                Id = i,
                WardId = 1,
                TaxZoneId = 1,
                PropertyNo = $"P{i}",
                IsActive = true
            });
        }
        await context.SaveChangesAsync();

        var query = context.PropertyMast.AsQueryable();
        var result = await query.ToPagedResultAsync(2, 10);

        Assert.Equal(50, result.TotalCount);
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(5, result.TotalPages);
    }

    [Fact]
    public async Task ApplyPaginationAsync_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        for (int i = 1; i <= 25; i++)
        {
            context.PropertyMast.Add(new PropertyEntity
            {
                Id = i,
                WardId = 1,
                TaxZoneId = 1,
                IsActive = true
            });
        }
        await context.SaveChangesAsync();

        var queryParams = new TestQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        var query = context.PropertyMast.AsQueryable();
        var result = await query.ApplyPaginationAsync(queryParams);

        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(3, result.TotalPages);
    }
}

/// <summary>
/// Additional tests for complex filter scenarios using real entities
/// </summary>
public class FilterExpressionBuilderAdvancedTests
{
    private class WardQueryParameters : BaseQueryParameters
    {
        [Filterable(FilterOperator.StartsWith)]
        [Sortable]
        public string? WardNo { get; set; }

        [Filterable(FilterOperator.Contains)]
        public string? Description { get; set; }

        [Filterable]
        public int? ZoneId { get; set; }
    }

    [Fact]
    public async Task BuildFilterExpression_StringStartsWith_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 2, WardNo = "E001", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new WardQueryParameters
        {
            WardNo = "W"
        };

        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Single(result);
        Assert.Equal("W001", result[0].WardNo);
    }

    [Fact]
    public async Task BuildFilterExpression_StringContains_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "W001", Description = "North Ward", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 2, WardNo = "W002", Description = "South Ward", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new WardQueryParameters
        {
            Description = "North"
        };

        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Single(result);
        Assert.Equal("North Ward", result[0].Description);
    }

    [Fact]
    public async Task BuildFilterExpression_IntEquals_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "W001", ZoneId = 5, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 2, WardNo = "W002", ZoneId = 10, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new WardQueryParameters
        {
            ZoneId = 5
        };

        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Single(result);
        Assert.Equal(5, result[0].ZoneId);
    }

    [Fact]
    public async Task ApplySort_CaseInsensitive_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "ZZZ", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 2, WardNo = "AAA", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new WardQueryParameters
        {
            SortBy = "wardno",
            SortOrder = "asc"
        };

        var result = context.WardMaster.AsQueryable().ApplySort(queryParams).ToList();

        Assert.Equal("AAA", result[0].WardNo);
    }
}

