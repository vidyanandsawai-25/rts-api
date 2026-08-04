using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Asset_Management.AssetAssessmentYearRangeMasterCV;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Infrastructure.Data;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for <see cref="AssetAssessmentYearRangeMasterCVQueryParameters"/> - the filter/sort
/// surface for the assessment year range CV listing endpoint. This class was recently split out
/// of AssetAssessmentYearRangeMasterCVDtos.cs into its own file and gained a new
/// <see cref="AssetAssessmentYearRangeMasterCVQueryParameters.MarkedForDeletion"/> filter, matching
/// the AssetAgeFactorCV/AssetNatureFactorCV sibling shape - neither had any prior test coverage.
/// </summary>
public class AssetAssessmentYearRangeMasterCVQueryParametersTests
{
    [Fact]
    public void InheritsFromBaseQueryParameters()
    {
        var queryParameters = new AssetAssessmentYearRangeMasterCVQueryParameters();

        Assert.IsAssignableFrom<BaseQueryParameters>(queryParameters);
    }

    [Fact]
    public void InheritedPagingDefaults_MatchBaseQueryParameters()
    {
        var queryParameters = new AssetAssessmentYearRangeMasterCVQueryParameters();

        Assert.Equal(1, queryParameters.PageNumber);
        Assert.Equal(10, queryParameters.PageSize);
        Assert.Equal("asc", queryParameters.SortOrder);
        Assert.Equal(FilterLogic.And, queryParameters.FilterLogic);
    }

    [Fact]
    public void AllFilterableProperties_AreNullable_SoAbsenceMeansDontFilter()
    {
        var queryParameters = new AssetAssessmentYearRangeMasterCVQueryParameters();

        Assert.Null(queryParameters.FromYear);
        Assert.Null(queryParameters.ToYear);
        Assert.Null(queryParameters.IsActive);
        Assert.Null(queryParameters.MarkedForDeletion);
    }

    [Fact]
    public void CanSetAndGetEachFilterProperty()
    {
        var queryParameters = new AssetAssessmentYearRangeMasterCVQueryParameters
        {
            FromYear = 2000,
            ToYear = 2005,
            IsActive = true,
            MarkedForDeletion = false
        };

        Assert.Equal(2000, queryParameters.FromYear);
        Assert.Equal(2005, queryParameters.ToYear);
        Assert.True(queryParameters.IsActive);
        Assert.False(queryParameters.MarkedForDeletion);
    }

    [Theory]
    [InlineData(nameof(AssetAssessmentYearRangeMasterCVQueryParameters.FromYear))]
    [InlineData(nameof(AssetAssessmentYearRangeMasterCVQueryParameters.ToYear))]
    [InlineData(nameof(AssetAssessmentYearRangeMasterCVQueryParameters.IsActive))]
    public void EqualsFilterableProperties_UseDefaultEqualsOperatorAndNoEntityPropertyMapping(string propertyName)
    {
        var property = typeof(AssetAssessmentYearRangeMasterCVQueryParameters).GetProperty(propertyName);

        var filterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false)
            .Cast<FilterableAttribute>()
            .SingleOrDefault();

        Assert.NotNull(filterable);
        Assert.Equal(FilterOperator.Equals, filterable!.Operator);
        Assert.Null(filterable.EntityProperty);
    }

    [Theory]
    [InlineData(nameof(AssetAssessmentYearRangeMasterCVQueryParameters.FromYear))]
    [InlineData(nameof(AssetAssessmentYearRangeMasterCVQueryParameters.ToYear))]
    [InlineData(nameof(AssetAssessmentYearRangeMasterCVQueryParameters.IsActive))]
    [InlineData(nameof(AssetAssessmentYearRangeMasterCVQueryParameters.MarkedForDeletion))]
    public void AllFourProperties_AreSortable(string propertyName)
    {
        var property = typeof(AssetAssessmentYearRangeMasterCVQueryParameters).GetProperty(propertyName);

        var hasSortable = property?.GetCustomAttributes(typeof(SortableAttribute), false).Any();

        Assert.True(hasSortable);
    }

    [Fact]
    public void MarkedForDeletion_IsFilterableWithDefaultEqualsOperator()
    {
        // Unlike FromYear/ToYear/IsActive, MarkedForDeletion uses the bare [Filterable] ctor
        // (no explicit FilterOperator argument) - the default still resolves to Equals.
        var property = typeof(AssetAssessmentYearRangeMasterCVQueryParameters)
            .GetProperty(nameof(AssetAssessmentYearRangeMasterCVQueryParameters.MarkedForDeletion));

        var filterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false)
            .Cast<FilterableAttribute>()
            .SingleOrDefault();

        Assert.NotNull(filterable);
        Assert.Equal(FilterOperator.Equals, filterable!.Operator);
        Assert.Null(filterable.EntityProperty);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Integration tests against a real ApplicationDbContext (EF Core InMemory provider) -
    // proving every filter actually resolves through EF Core, per the established pattern in
    // AssetLeaseRentDetailsQueryParametersTests.cs.
    // ─────────────────────────────────────────────────────────────────────────

    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static AssetAssessmentYearRangeMasterCVEntity MakeRange(
        int id, int fromYear, int toYear, bool isActive = true, bool markedForDeletion = false) =>
        new()
        {
            Id = id,
            FromYear = fromYear,
            ToYear = toYear,
            IsActive = isActive,
            MarkedForDeletion = markedForDeletion
        };

    [Fact]
    public async Task ApplyFilters_WithNoFilters_ReturnsAllRows()
    {
        await using var context = CreateInMemoryContext();
        context.AssetAssessmentYearRangeMasterCV.AddRange(
            MakeRange(1, 2000, 2005),
            MakeRange(2, 2006, 2010));
        await context.SaveChangesAsync();

        var result = context.AssetAssessmentYearRangeMasterCV.AsQueryable()
            .ApplyFilters(new AssetAssessmentYearRangeMasterCVQueryParameters())
            .ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ApplyFilters_ByFromYear_ReturnsMatchingRow()
    {
        await using var context = CreateInMemoryContext();
        context.AssetAssessmentYearRangeMasterCV.AddRange(
            MakeRange(1, 2000, 2005),
            MakeRange(2, 2006, 2010));
        await context.SaveChangesAsync();

        var result = context.AssetAssessmentYearRangeMasterCV.AsQueryable()
            .ApplyFilters(new AssetAssessmentYearRangeMasterCVQueryParameters { FromYear = 2006 })
            .ToList();

        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
    }

    [Fact]
    public async Task ApplyFilters_ByToYear_ReturnsMatchingRow()
    {
        await using var context = CreateInMemoryContext();
        context.AssetAssessmentYearRangeMasterCV.AddRange(
            MakeRange(1, 2000, 2005),
            MakeRange(2, 2006, 2010));
        await context.SaveChangesAsync();

        var result = context.AssetAssessmentYearRangeMasterCV.AsQueryable()
            .ApplyFilters(new AssetAssessmentYearRangeMasterCVQueryParameters { ToYear = 2005 })
            .ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task ApplyFilters_ByIsActive_ExcludesInactiveRows()
    {
        await using var context = CreateInMemoryContext();
        context.AssetAssessmentYearRangeMasterCV.AddRange(
            MakeRange(1, 2000, 2005, isActive: true),
            MakeRange(2, 2006, 2010, isActive: false));
        await context.SaveChangesAsync();

        var result = context.AssetAssessmentYearRangeMasterCV.AsQueryable()
            .ApplyFilters(new AssetAssessmentYearRangeMasterCVQueryParameters { IsActive = true })
            .ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task ApplyFilters_ByMarkedForDeletion_ReturnsOnlyMarkedRows()
    {
        await using var context = CreateInMemoryContext();
        context.AssetAssessmentYearRangeMasterCV.AddRange(
            MakeRange(1, 2000, 2005, markedForDeletion: false),
            MakeRange(2, 2006, 2010, markedForDeletion: true));
        await context.SaveChangesAsync();

        var result = context.AssetAssessmentYearRangeMasterCV.AsQueryable()
            .ApplyFilters(new AssetAssessmentYearRangeMasterCVQueryParameters { MarkedForDeletion = true })
            .ToList();

        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
    }

    [Fact]
    public async Task ApplyFilters_CombiningFromYearAndIsActive_AppliesAndLogic()
    {
        await using var context = CreateInMemoryContext();
        context.AssetAssessmentYearRangeMasterCV.AddRange(
            MakeRange(1, 2000, 2005, isActive: true),
            MakeRange(2, 2000, 2005, isActive: false));
        await context.SaveChangesAsync();

        var result = context.AssetAssessmentYearRangeMasterCV.AsQueryable()
            .ApplyFilters(new AssetAssessmentYearRangeMasterCVQueryParameters { FromYear = 2000, IsActive = true })
            .ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task ApplyFilters_NullFilterValues_AreIgnored()
    {
        await using var context = CreateInMemoryContext();
        context.AssetAssessmentYearRangeMasterCV.AddRange(
            MakeRange(1, 2000, 2005),
            MakeRange(2, 2006, 2010));
        await context.SaveChangesAsync();

        var result = context.AssetAssessmentYearRangeMasterCV.AsQueryable()
            .ApplyFilters(new AssetAssessmentYearRangeMasterCVQueryParameters
            {
                FromYear = null,
                ToYear = null,
                IsActive = null,
                MarkedForDeletion = null
            })
            .ToList();

        Assert.Equal(2, result.Count);
    }
}
