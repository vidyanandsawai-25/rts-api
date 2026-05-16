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
/// Comprehensive tests for FilterExpressionBuilder complex methods to achieve 100% line coverage
/// Specifically targeting: CanConvertTypes, ConvertCollectionElements, BuildStringExpression, BuildInExpression, BuildComparisonExpression
/// </summary>
public class FilterExpressionBuilderComplexTests
{
    #region BuildInExpression Tests

    private class InFilterQueryParameters : BaseQueryParameters
    {
        [Filterable(FilterOperator.In, EntityProperty = "Id")]
        public List<int>? IdList { get; set; }

        [Filterable(FilterOperator.NotIn, EntityProperty = "Id")]
        public List<int>? ExcludedIds { get; set; }

        [Filterable(FilterOperator.In, EntityProperty = "PropertyNo")]
        public List<string>? PropertyNos { get; set; }

        [Filterable(FilterOperator.NotIn, EntityProperty = "PropertyNo")]
        public List<string>? ExcludedPropertyNos { get; set; }

        [Filterable(FilterOperator.In, EntityProperty = "Id")]
        public List<long>? LongIds { get; set; }
    }

    [Fact]
    public async Task BuildInExpression_WithIntCollection_FiltersCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 2, WardNo = "W002", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 3, WardNo = "W003", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new InFilterQueryParameters
        {
            IdList = new List<int> { 1, 3 }
        };

        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, w => w.Id == 1);
        Assert.Contains(result, w => w.Id == 3);
    }

    [Fact]
    public async Task BuildInExpression_WithStringCollection_FiltersCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.PropertyMast.Add(new PropertyEntity { Id = 1, PropertyNo = "PROP001", WardId = 1, TaxZoneId = 1, IsActive = true });
        context.PropertyMast.Add(new PropertyEntity { Id = 2, PropertyNo = "PROP002", WardId = 1, TaxZoneId = 1, IsActive = true });
        context.PropertyMast.Add(new PropertyEntity { Id = 3, PropertyNo = "PROP003", WardId = 1, TaxZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new InFilterQueryParameters
        {
            PropertyNos = new List<string> { "PROP001", "PROP003" }
        };

        var result = context.PropertyMast.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.PropertyNo == "PROP001");
        Assert.Contains(result, p => p.PropertyNo == "PROP003");
    }

    [Fact]
    public async Task BuildInExpression_CaseInsensitiveStringComparison_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.PropertyMast.Add(new PropertyEntity { Id = 1, PropertyNo = "PROP001", WardId = 1, TaxZoneId = 1, IsActive = true });
        context.PropertyMast.Add(new PropertyEntity { Id = 2, PropertyNo = "PROP002", WardId = 1, TaxZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new InFilterQueryParameters
        {
            PropertyNos = new List<string> { "prop001" } // lowercase
        };

        var result = context.PropertyMast.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Single(result);
        Assert.Equal("PROP001", result[0].PropertyNo);
    }

    [Fact]
    public async Task BuildInExpression_WithEmptyCollection_SkipsFilter_ReturnsAllResults()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new InFilterQueryParameters
        {
            IdList = new List<int>() // empty collection
        };

        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        // Empty IN filter should be skipped, returning all results
        Assert.Single(result);
    }

    [Fact]
    public async Task BuildNotInExpression_FiltersCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 2, WardNo = "W002", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 3, WardNo = "W003", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new InFilterQueryParameters
        {
            ExcludedIds = new List<int> { 2 }
        };

        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, w => w.Id == 2);
    }

    [Fact]
    public async Task BuildNotInExpression_WithEmptyCollection_ReturnsAllResults()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new InFilterQueryParameters
        {
            ExcludedIds = new List<int>() // empty collection
        };

        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        // Empty NOT IN filter should be skipped, returning all results
        Assert.Single(result);
    }

    [Fact]
    public async Task BuildNotInExpression_WithStringCollection_FiltersCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.PropertyMast.Add(new PropertyEntity { Id = 1, PropertyNo = "PROP001", WardId = 1, TaxZoneId = 1, IsActive = true });
        context.PropertyMast.Add(new PropertyEntity { Id = 2, PropertyNo = "PROP002", WardId = 1, TaxZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new InFilterQueryParameters
        {
            ExcludedPropertyNos = new List<string> { "PROP002" }
        };

        var result = context.PropertyMast.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Single(result);
        Assert.Equal("PROP001", result[0].PropertyNo);
    }

    [Fact]
    public async Task BuildInExpression_WithTypeMismatch_ConvertsTypes()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        // Add entities with Id property (int type)
        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 2, WardNo = "W002", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        // Query with long values (convertible to int)
        var queryParams = new InFilterQueryParameters
        {
            LongIds = new List<long> { 1L, 2L }
        };

        // This should work due to type conversion
        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Equal(2, result.Count);
    }

    #endregion

    #region BuildComparisonExpression Tests

    private class ComparisonQueryParameters : BaseQueryParameters
    {
        [Filterable(FilterOperator.GreaterThan, EntityProperty = "Id")]
        public int? MinId { get; set; }

        [Filterable(FilterOperator.LessThan, EntityProperty = "Id")]
        public int? MaxId { get; set; }

        [Filterable(FilterOperator.GreaterThanOrEqual, EntityProperty = "Id")]
        public int? MinIdInclusive { get; set; }

        [Filterable(FilterOperator.LessThanOrEqual, EntityProperty = "Id")]
        public int? MaxIdInclusive { get; set; }

        [Filterable(FilterOperator.NotEquals, EntityProperty = "Id")]
        public int? ExcludeId { get; set; }

        [Filterable(FilterOperator.GreaterThan, EntityProperty = "Id")]
        public int? NullableMinId { get; set; }
    }

    [Fact]
    public async Task BuildComparisonExpression_GreaterThan_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 5, WardNo = "W005", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new ComparisonQueryParameters
        {
            MinId = 3
        };

        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Single(result);
        Assert.Equal(5, result[0].Id);
    }

    [Fact]
    public async Task BuildComparisonExpression_LessThan_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 5, WardNo = "W005", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new ComparisonQueryParameters
        {
            MaxId = 3
        };

        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task BuildComparisonExpression_GreaterThanOrEqual_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 3, WardNo = "W003", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 5, WardNo = "W005", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new ComparisonQueryParameters
        {
            MinIdInclusive = 3
        };

        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, w => w.Id == 3);
        Assert.Contains(result, w => w.Id == 5);
    }

    [Fact]
    public async Task BuildComparisonExpression_LessThanOrEqual_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 3, WardNo = "W003", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 5, WardNo = "W005", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new ComparisonQueryParameters
        {
            MaxIdInclusive = 3
        };

        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, w => w.Id == 1);
        Assert.Contains(result, w => w.Id == 3);
    }

    [Fact]
    public async Task BuildComparisonExpression_NotEquals_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 2, WardNo = "W002", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new ComparisonQueryParameters
        {
            ExcludeId = 1
        };

        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
    }

    #endregion

    #region BuildStringExpression Complex Tests

    private class StringExpressionQueryParameters : BaseQueryParameters
    {
        [Filterable(FilterOperator.Contains, EntityProperty = "Description")]
        public string? ContainsText { get; set; }

        [Filterable(FilterOperator.StartsWith, EntityProperty = "WardNo")]
        public string? StartsWithText { get; set; }

        [Filterable(FilterOperator.EndsWith, EntityProperty = "WardNo")]
        public string? EndsWithText { get; set; }

        [Filterable(FilterOperator.Equals, EntityProperty = "WardNo")]
        public string? ExactMatch { get; set; }

        [Filterable(FilterOperator.NotEquals, EntityProperty = "WardNo")]
        public string? NotExactMatch { get; set; }

        [Filterable(FilterOperator.GreaterThan, EntityProperty = "WardNo")]
        public string? NumericGreaterThan { get; set; }

        [Filterable(FilterOperator.LessThan, EntityProperty = "WardNo")]
        public string? NumericLessThan { get; set; }

        [Filterable(FilterOperator.GreaterThanOrEqual, EntityProperty = "WardNo")]
        public string? NumericGreaterOrEqual { get; set; }

        [Filterable(FilterOperator.LessThanOrEqual, EntityProperty = "WardNo")]
        public string? NumericLessOrEqual { get; set; }

        [Filterable(FilterOperator.GreaterThan, EntityProperty = "WardNo")]
        public string? AlphaGreaterThan { get; set; }
    }

    [Fact]
    public async Task BuildStringExpression_Contains_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "W001", Description = "North Ward", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 2, WardNo = "W002", Description = "South Ward", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new StringExpressionQueryParameters
        {
            ContainsText = "North"
        };

        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Single(result);
        Assert.Contains("North", result[0].Description);
    }

    [Fact]
    public async Task BuildStringExpression_StartsWith_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 2, WardNo = "E001", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new StringExpressionQueryParameters
        {
            StartsWithText = "W"
        };

        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Single(result);
        Assert.StartsWith("W", result[0].WardNo);
    }

    [Fact]
    public async Task BuildStringExpression_EndsWith_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 2, WardNo = "E002", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new StringExpressionQueryParameters
        {
            EndsWithText = "001"
        };

        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Single(result);
        Assert.EndsWith("001", result[0].WardNo);
    }

    [Fact]
    public async Task BuildStringExpression_Equals_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 2, WardNo = "W002", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new StringExpressionQueryParameters
        {
            ExactMatch = "W001"
        };

        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Single(result);
        Assert.Equal("W001", result[0].WardNo);
    }

    [Fact]
    public async Task BuildStringExpression_NotEquals_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "W001", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 2, WardNo = "W002", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new StringExpressionQueryParameters
        {
            NotExactMatch = "W001"
        };

        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Single(result);
        Assert.Equal("W002", result[0].WardNo);
    }

    [Fact]
    public async Task BuildStringExpression_NumericGreaterThan_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "100", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 2, WardNo = "200", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 3, WardNo = "50", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new StringExpressionQueryParameters
        {
            NumericGreaterThan = "100"
        };

        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Single(result);
        Assert.Equal("200", result[0].WardNo);
    }

    [Fact]
    public async Task BuildStringExpression_NumericLessThan_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "100", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 2, WardNo = "200", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 3, WardNo = "50", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new StringExpressionQueryParameters
        {
            NumericLessThan = "100"
        };

        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Single(result);
        Assert.Equal("50", result[0].WardNo);
    }

    [Fact]
    public async Task BuildStringExpression_NumericGreaterOrEqual_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "100", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 2, WardNo = "200", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 3, WardNo = "50", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new StringExpressionQueryParameters
        {
            NumericGreaterOrEqual = "100"
        };

        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, w => w.WardNo == "100");
        Assert.Contains(result, w => w.WardNo == "200");
    }

    [Fact]
    public async Task BuildStringExpression_NumericLessOrEqual_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "100", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 2, WardNo = "200", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 3, WardNo = "50", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new StringExpressionQueryParameters
        {
            NumericLessOrEqual = "100"
        };

        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, w => w.WardNo == "100");
        Assert.Contains(result, w => w.WardNo == "50");
    }

    [Fact]
    public async Task BuildStringExpression_AlphabeticComparison_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "AAA", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 2, WardNo = "BBB", ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 3, WardNo = "CCC", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new StringExpressionQueryParameters
        {
            AlphaGreaterThan = "BBB"
        };

        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Single(result);
        Assert.Equal("CCC", result[0].WardNo);
    }

    #endregion

    #region IsNull and IsNotNull Tests

    private class NullCheckQueryParameters : BaseQueryParameters
    {
        [Filterable(FilterOperator.IsNull, EntityProperty = "Description")]
        public bool? DescriptionIsNull { get; set; }

        [Filterable(FilterOperator.IsNotNull, EntityProperty = "Description")]
        public bool? DescriptionIsNotNull { get; set; }
    }

    [Fact]
    public async Task BuildNullCheckExpression_IsNull_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "W001", Description = null, ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 2, WardNo = "W002", Description = "Test", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new NullCheckQueryParameters
        {
            DescriptionIsNull = true
        };

        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Single(result);
        Assert.Null(result[0].Description);
    }

    [Fact]
    public async Task BuildNullCheckExpression_IsNotNull_WorksCorrectly()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "W001", Description = null, ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 2, WardNo = "W002", Description = "Test", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new NullCheckQueryParameters
        {
            DescriptionIsNotNull = true
        };

        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        Assert.Single(result);
        Assert.NotNull(result[0].Description);
    }

    [Fact]
    public async Task BuildNullCheckExpression_WhenFlagIsFalse_SkipsFilter()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        context.WardMaster.Add(new WardEntity { Id = 1, WardNo = "W001", Description = null, ZoneId = 1, IsActive = true });
        context.WardMaster.Add(new WardEntity { Id = 2, WardNo = "W002", Description = "Test", ZoneId = 1, IsActive = true });
        await context.SaveChangesAsync();

        var queryParams = new NullCheckQueryParameters
        {
            DescriptionIsNull = false // Flag is false, should skip filter
        };

        var result = context.WardMaster.AsQueryable().ApplyFilters(queryParams).ToList();

        // Should return all records since filter is skipped
        Assert.Equal(2, result.Count);
    }

    #endregion
}
