using System.Collections.Generic;
using System.Linq;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Asset_Management.AssetMaster;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for AssetMasterQueryParameters - filtering/sorting/searching surface for the AssetMaster
/// list endpoint. ZoneId, WardId, and Address are intentionally untagged (applied via join on
/// AssetDetails in the service, per the source comments), so they must NOT carry [Filterable].
/// </summary>
public class AssetMasterQueryParametersTests
{
    [Fact]
    public void AssetMasterQueryParameters_InheritsFromBaseQueryParameters()
    {
        var queryParameters = new AssetMasterQueryParameters();

        Assert.IsAssignableFrom<BaseQueryParameters>(queryParameters);
    }

    [Fact]
    public void DepartmentId_HasFilterableEqualsAndSortableAttributes()
    {
        var property = typeof(AssetMasterQueryParameters).GetProperty(nameof(AssetMasterQueryParameters.DepartmentId));

        var filterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false).Cast<FilterableAttribute>().SingleOrDefault();
        var sortable = property?.GetCustomAttributes(typeof(SortableAttribute), false).Any();

        Assert.NotNull(filterable);
        Assert.Equal(FilterOperator.Equals, filterable!.Operator);
        Assert.True(sortable);
    }

    [Fact]
    public void AssetNo_HasFilterableContainsSearchableAndSortableAttributes()
    {
        var property = typeof(AssetMasterQueryParameters).GetProperty(nameof(AssetMasterQueryParameters.AssetNo));

        var filterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false).Cast<FilterableAttribute>().SingleOrDefault();
        var searchable = property?.GetCustomAttributes(typeof(SearchableAttribute), false).Any();
        var sortable = property?.GetCustomAttributes(typeof(SortableAttribute), false).Any();

        Assert.NotNull(filterable);
        Assert.Equal(FilterOperator.Contains, filterable!.Operator);
        Assert.True(searchable);
        Assert.True(sortable);
    }

    [Fact]
    public void AssetName_HasFilterableContainsSearchableAndSortableAttributes()
    {
        var property = typeof(AssetMasterQueryParameters).GetProperty(nameof(AssetMasterQueryParameters.AssetName));

        var filterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false).Cast<FilterableAttribute>().SingleOrDefault();
        var searchable = property?.GetCustomAttributes(typeof(SearchableAttribute), false).Any();
        var sortable = property?.GetCustomAttributes(typeof(SortableAttribute), false).Any();

        Assert.NotNull(filterable);
        Assert.Equal(FilterOperator.Contains, filterable!.Operator);
        Assert.True(searchable);
        Assert.True(sortable);
    }

    [Fact]
    public void AssetCategoryId_HasFilterableInWithEntityPropertyAndSortableAttributes()
    {
        var property = typeof(AssetMasterQueryParameters).GetProperty(nameof(AssetMasterQueryParameters.AssetCategoryId));

        var filterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false).Cast<FilterableAttribute>().SingleOrDefault();
        var sortable = property?.GetCustomAttributes(typeof(SortableAttribute), false).Any();

        Assert.NotNull(filterable);
        Assert.Equal(FilterOperator.In, filterable!.Operator);
        Assert.Equal("AssetCategoryId", filterable.EntityProperty);
        Assert.True(sortable);
    }

    [Fact]
    public void AssetTypeId_HasFilterableInWithEntityPropertyAndSortableAttributes()
    {
        var property = typeof(AssetMasterQueryParameters).GetProperty(nameof(AssetMasterQueryParameters.AssetTypeId));

        var filterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false).Cast<FilterableAttribute>().SingleOrDefault();
        var sortable = property?.GetCustomAttributes(typeof(SortableAttribute), false).Any();

        Assert.NotNull(filterable);
        Assert.Equal(FilterOperator.In, filterable!.Operator);
        Assert.Equal("AssetTypeId", filterable.EntityProperty);
        Assert.True(sortable);
    }

    [Fact]
    public void ParentAssetId_HasFilterableEqualsAndSortableAttributes()
    {
        var property = typeof(AssetMasterQueryParameters).GetProperty(nameof(AssetMasterQueryParameters.ParentAssetId));

        var filterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false).Cast<FilterableAttribute>().SingleOrDefault();
        var sortable = property?.GetCustomAttributes(typeof(SortableAttribute), false).Any();

        Assert.NotNull(filterable);
        Assert.Equal(FilterOperator.Equals, filterable!.Operator);
        Assert.True(sortable);
    }

    [Fact]
    public void OccupancyStatus_HasFilterableContainsAndSortableAttributes_ButNotSearchable()
    {
        var property = typeof(AssetMasterQueryParameters).GetProperty(nameof(AssetMasterQueryParameters.OccupancyStatus));

        var filterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false).Cast<FilterableAttribute>().SingleOrDefault();
        var searchable = property?.GetCustomAttributes(typeof(SearchableAttribute), false).Any();
        var sortable = property?.GetCustomAttributes(typeof(SortableAttribute), false).Any();

        Assert.NotNull(filterable);
        Assert.Equal(FilterOperator.Contains, filterable!.Operator);
        Assert.False(searchable);
        Assert.True(sortable);
    }

    [Fact]
    public void IsActive_HasFilterableEqualsAndSortableAttributes()
    {
        var property = typeof(AssetMasterQueryParameters).GetProperty(nameof(AssetMasterQueryParameters.IsActive));

        var filterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false).Cast<FilterableAttribute>().SingleOrDefault();
        var sortable = property?.GetCustomAttributes(typeof(SortableAttribute), false).Any();

        Assert.NotNull(filterable);
        Assert.Equal(FilterOperator.Equals, filterable!.Operator);
        Assert.True(sortable);
    }

    [Fact]
    public void OwnershipType_HasFilterableContainsAndSortableAttributes()
    {
        var property = typeof(AssetMasterQueryParameters).GetProperty(nameof(AssetMasterQueryParameters.OwnershipType));

        var filterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false).Cast<FilterableAttribute>().SingleOrDefault();
        var sortable = property?.GetCustomAttributes(typeof(SortableAttribute), false).Any();

        Assert.NotNull(filterable);
        Assert.Equal(FilterOperator.Contains, filterable!.Operator);
        Assert.True(sortable);
    }

    [Theory]
    [InlineData(nameof(AssetMasterQueryParameters.ZoneId))]
    [InlineData(nameof(AssetMasterQueryParameters.WardId))]
    [InlineData(nameof(AssetMasterQueryParameters.Address))]
    public void JoinAppliedProperties_HaveNoFilterableSortableOrSearchableAttributes(string propertyName)
    {
        // ZoneId/WardId/Address are documented as "applied via join on AssetDetails in the
        // service" - they intentionally carry none of the three query attributes.
        var property = typeof(AssetMasterQueryParameters).GetProperty(propertyName);

        var hasFilterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false).Any();
        var hasSortable = property?.GetCustomAttributes(typeof(SortableAttribute), false).Any();
        var hasSearchable = property?.GetCustomAttributes(typeof(SearchableAttribute), false).Any();

        Assert.False(hasFilterable);
        Assert.False(hasSortable);
        Assert.False(hasSearchable);
    }

    [Fact]
    public void AssetLife_HasSortableAttribute_ButNotFilterable()
    {
        var property = typeof(AssetMasterQueryParameters).GetProperty(nameof(AssetMasterQueryParameters.AssetLife));

        var hasFilterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false).Any();
        var hasSortable = property?.GetCustomAttributes(typeof(SortableAttribute), false).Any();

        Assert.False(hasFilterable);
        Assert.True(hasSortable);
    }

    [Fact]
    public void CapitalValue_HasSortableAttribute_ButNotFilterable()
    {
        var property = typeof(AssetMasterQueryParameters).GetProperty(nameof(AssetMasterQueryParameters.CapitalValue));

        var hasFilterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false).Any();
        var hasSortable = property?.GetCustomAttributes(typeof(SortableAttribute), false).Any();

        Assert.False(hasFilterable);
        Assert.True(hasSortable);
    }

    [Fact]
    public void AllProperties_DefaultToNull()
    {
        var queryParameters = new AssetMasterQueryParameters();

        Assert.Null(queryParameters.DepartmentId);
        Assert.Null(queryParameters.AssetNo);
        Assert.Null(queryParameters.AssetName);
        Assert.Null(queryParameters.AssetCategoryId);
        Assert.Null(queryParameters.AssetTypeId);
        Assert.Null(queryParameters.ParentAssetId);
        Assert.Null(queryParameters.OccupancyStatus);
        Assert.Null(queryParameters.IsActive);
        Assert.Null(queryParameters.OwnershipType);
        Assert.Null(queryParameters.ZoneId);
        Assert.Null(queryParameters.WardId);
        Assert.Null(queryParameters.Address);
        Assert.Null(queryParameters.AssetLife);
        Assert.Null(queryParameters.CapitalValue);
    }

    [Fact]
    public void CanSetAndGetEachProperty()
    {
        var queryParameters = new AssetMasterQueryParameters
        {
            DepartmentId = 1,
            AssetNo = "AST-001",
            AssetName = "Building A",
            AssetCategoryId = new() { 1, 2 },
            AssetTypeId = new() { 3, 4 },
            ParentAssetId = 5,
            OccupancyStatus = "Occupied",
            IsActive = true,
            OwnershipType = "Owned",
            ZoneId = 6,
            WardId = 7,
            Address = "123 Main St",
            AssetLife = 30,
            CapitalValue = 100000m
        };

        Assert.Equal(1, queryParameters.DepartmentId);
        Assert.Equal("AST-001", queryParameters.AssetNo);
        Assert.Equal("Building A", queryParameters.AssetName);
        Assert.Equal(new List<int> { 1, 2 }, queryParameters.AssetCategoryId);
        Assert.Equal(new List<int> { 3, 4 }, queryParameters.AssetTypeId);
        Assert.Equal(5, queryParameters.ParentAssetId);
        Assert.Equal("Occupied", queryParameters.OccupancyStatus);
        Assert.True(queryParameters.IsActive);
        Assert.Equal("Owned", queryParameters.OwnershipType);
        Assert.Equal(6, queryParameters.ZoneId);
        Assert.Equal(7, queryParameters.WardId);
        Assert.Equal("123 Main St", queryParameters.Address);
        Assert.Equal(30, queryParameters.AssetLife);
        Assert.Equal(100000m, queryParameters.CapitalValue);
    }
}
