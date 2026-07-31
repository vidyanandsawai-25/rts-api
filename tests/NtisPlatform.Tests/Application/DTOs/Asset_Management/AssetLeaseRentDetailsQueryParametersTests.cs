using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Asset_Management.AssetLeaseRentDetails;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Infrastructure.Data;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for AssetLeaseRentDetailsQueryParameters - filter/search/sort surface for the
/// shop/tenant lease-and-rent registration listing endpoint.
/// </summary>
public class AssetLeaseRentDetailsQueryParametersTests
{
    [Fact]
    public void AssetLeaseRentDetailsQueryParameters_InheritsFromBaseQueryParameters()
    {
        var queryParameters = new AssetLeaseRentDetailsQueryParameters();

        Assert.IsAssignableFrom<BaseQueryParameters>(queryParameters);
    }

    [Theory]
    [InlineData(nameof(AssetLeaseRentDetailsQueryParameters.AssetCategoryId))]
    [InlineData(nameof(AssetLeaseRentDetailsQueryParameters.ZoneId))]
    [InlineData(nameof(AssetLeaseRentDetailsQueryParameters.AssetTypeId))]
    [InlineData(nameof(AssetLeaseRentDetailsQueryParameters.WardId))]
    [InlineData(nameof(AssetLeaseRentDetailsQueryParameters.ParentAssetId))]
    [InlineData(nameof(AssetLeaseRentDetailsQueryParameters.AssetNo))]
    [InlineData(nameof(AssetLeaseRentDetailsQueryParameters.RentStatus))]
    [InlineData(nameof(AssetLeaseRentDetailsQueryParameters.PaymentStatus))]
    public void JoinOrUndecidedProperties_HaveNoFilterableOrSearchableAttribute(string propertyName)
    {
        // AssetCategoryId/AssetTypeId/ParentAssetId/AssetNo target columns one hop away on
        // AssetMasterEntity; ZoneId/WardId are two hops away via AssetMasterEntity.Details
        // (AssetDetailsEntity). FilterExpressionBuilder/BuildSearchExpression only resolve a
        // single direct Type.GetProperty(name) call with no support for dotted/nested paths, so a
        // EntityProperty = "Asset.X" override here would throw FilterValidationException (or, for
        // [Searchable] with no override, silently never match) the moment this DTO were wired up
        // to a service.
        // RentStatus/PaymentStatus are a distinct, previously-unreported instance of the exact
        // same underlying bug: AssetLeaseRentDetailsEntity has no RentStatus or PaymentStatus
        // column at all (only WorkflowStatus), so a bare [Filterable] here would throw
        // "Property 'RentStatus'/'PaymentStatus' not found" - discovered while verifying this
        // Copilot review against the real entity, not something Copilot itself flagged.
        // All of these must NOT carry [Filterable]/[Searchable] - see the class-level comment in
        // the source for what each needs before it can be wired up (an explicit join, or - for
        // RentStatus/PaymentStatus - a product decision on what they should even mean).
        var property = typeof(AssetLeaseRentDetailsQueryParameters).GetProperty(propertyName);

        Assert.NotNull(property);
        Assert.Empty(property!.GetCustomAttributes(typeof(FilterableAttribute), false));
        Assert.Empty(property.GetCustomAttributes(typeof(SearchableAttribute), false));
    }

    [Theory]
    [InlineData(nameof(AssetLeaseRentDetailsQueryParameters.AssetId))]
    [InlineData(nameof(AssetLeaseRentDetailsQueryParameters.WorkflowStatus))]
    [InlineData(nameof(AssetLeaseRentDetailsQueryParameters.IsActive))]
    public void FilterableProperties_WithoutOverride_UseDefaultEqualsOperatorAndNoEntityPropertyMapping(string propertyName)
    {
        // Unlike the join/undecided properties above, these ARE direct properties on
        // AssetLeaseRentDetailsEntity (AssetId, WorkflowStatus, IsActive via BaseEntity), so the
        // generic [Filterable] convention resolves them correctly.
        var property = typeof(AssetLeaseRentDetailsQueryParameters).GetProperty(propertyName);

        var filterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false)
            .Cast<FilterableAttribute>()
            .SingleOrDefault();

        Assert.NotNull(filterable);
        Assert.Equal(FilterOperator.Equals, filterable!.Operator);
        Assert.Null(filterable.EntityProperty);
    }

    [Theory]
    [InlineData(nameof(AssetLeaseRentDetailsQueryParameters.TenantName))]
    [InlineData(nameof(AssetLeaseRentDetailsQueryParameters.ShopName))]
    public void TenantNameAndShopName_HaveContainsFilterableSearchableAndSortableAttributes(string propertyName)
    {
        var property = typeof(AssetLeaseRentDetailsQueryParameters).GetProperty(propertyName);

        var filterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false)
            .Cast<FilterableAttribute>()
            .SingleOrDefault();
        var hasSearchable = property?.GetCustomAttributes(typeof(SearchableAttribute), false).Any();
        var hasSortable = property?.GetCustomAttributes(typeof(SortableAttribute), false).Any();

        Assert.NotNull(filterable);
        Assert.Equal(FilterOperator.Contains, filterable!.Operator);
        Assert.True(hasSearchable);
        Assert.True(hasSortable);
    }

    [Fact]
    public void AssetNo_HasNoSortableAttribute_Either()
    {
        // AssetNo was never Sortable in the source (join-dependent columns aren't exposed for
        // sorting here) - unaffected by the Filterable/Searchable removal above.
        var property = typeof(AssetLeaseRentDetailsQueryParameters).GetProperty(nameof(AssetLeaseRentDetailsQueryParameters.AssetNo));

        var hasSortable = property?.GetCustomAttributes(typeof(SortableAttribute), false).Any();

        Assert.False(hasSortable);
    }

    [Fact]
    public void FromDate_IsFilterableWithGreaterThanOrEqual_MappedToLeaseStartDate()
    {
        var property = typeof(AssetLeaseRentDetailsQueryParameters).GetProperty(nameof(AssetLeaseRentDetailsQueryParameters.FromDate));

        var filterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false)
            .Cast<FilterableAttribute>()
            .SingleOrDefault();

        Assert.NotNull(filterable);
        Assert.Equal(FilterOperator.GreaterThanOrEqual, filterable!.Operator);
        Assert.Equal("LeaseStartDate", filterable.EntityProperty);
    }

    [Fact]
    public void ToDate_IsFilterableWithLessThanOrEqual_MappedToLeaseEndDate()
    {
        var property = typeof(AssetLeaseRentDetailsQueryParameters).GetProperty(nameof(AssetLeaseRentDetailsQueryParameters.ToDate));

        var filterable = property?.GetCustomAttributes(typeof(FilterableAttribute), false)
            .Cast<FilterableAttribute>()
            .SingleOrDefault();

        Assert.NotNull(filterable);
        Assert.Equal(FilterOperator.LessThanOrEqual, filterable!.Operator);
        Assert.Equal("LeaseEndDate", filterable.EntityProperty);
    }

    [Fact]
    public void AllFilterableProperties_AreNullable()
    {
        // Every [Filterable] property must be nullable so that its absence means "don't filter."
        var queryParameters = new AssetLeaseRentDetailsQueryParameters();

        Assert.Null(queryParameters.AssetCategoryId);
        Assert.Null(queryParameters.ZoneId);
        Assert.Null(queryParameters.AssetTypeId);
        Assert.Null(queryParameters.WardId);
        Assert.Null(queryParameters.ParentAssetId);
        Assert.Null(queryParameters.AssetId);
        Assert.Null(queryParameters.RentStatus);
        Assert.Null(queryParameters.WorkflowStatus);
        Assert.Null(queryParameters.PaymentStatus);
        Assert.Null(queryParameters.TenantName);
        Assert.Null(queryParameters.ShopName);
        Assert.Null(queryParameters.AssetNo);
        Assert.Null(queryParameters.FromDate);
        Assert.Null(queryParameters.ToDate);
        Assert.Null(queryParameters.IsActive);
    }

    [Fact]
    public void CanSetAndGetEachFilterProperty()
    {
        var queryParameters = new AssetLeaseRentDetailsQueryParameters
        {
            AssetCategoryId = 1,
            ZoneId = 2,
            AssetTypeId = 3,
            WardId = 4,
            ParentAssetId = 5,
            AssetId = 6,
            RentStatus = "Active",
            WorkflowStatus = "Approved",
            PaymentStatus = "Paid",
            TenantName = "John",
            ShopName = "Corner Shop",
            AssetNo = "AST-001",
            FromDate = new DateTime(2026, 1, 1),
            ToDate = new DateTime(2026, 12, 31),
            IsActive = true
        };

        Assert.Equal(1, queryParameters.AssetCategoryId);
        Assert.Equal(2, queryParameters.ZoneId);
        Assert.Equal(3, queryParameters.AssetTypeId);
        Assert.Equal(4, queryParameters.WardId);
        Assert.Equal(5, queryParameters.ParentAssetId);
        Assert.Equal(6, queryParameters.AssetId);
        Assert.Equal("Active", queryParameters.RentStatus);
        Assert.Equal("Approved", queryParameters.WorkflowStatus);
        Assert.Equal("Paid", queryParameters.PaymentStatus);
        Assert.Equal("John", queryParameters.TenantName);
        Assert.Equal("Corner Shop", queryParameters.ShopName);
        Assert.Equal("AST-001", queryParameters.AssetNo);
        Assert.Equal(new DateTime(2026, 1, 1), queryParameters.FromDate);
        Assert.Equal(new DateTime(2026, 12, 31), queryParameters.ToDate);
        Assert.True(queryParameters.IsActive);
    }

    [Fact]
    public void InheritedPagingDefaults_MatchBaseQueryParameters()
    {
        var queryParameters = new AssetLeaseRentDetailsQueryParameters();

        Assert.Equal(1, queryParameters.PageNumber);
        Assert.Equal(10, queryParameters.PageSize);
        Assert.Equal("asc", queryParameters.SortOrder);
        Assert.Equal(FilterLogic.And, queryParameters.FilterLogic);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Integration tests against a real ApplicationDbContext (EF Core InMemory
    // provider - the established pattern for FilterExpressionBuilder/ApplyFilters
    // coverage in this repo; see FilterExpressionBuilderTests.cs and
    // FilterExpressionBuilderComplexTests.cs), proving the fix actually resolves
    // filters through EF Core rather than in memory, and that the previously
    // broken properties no longer throw FilterValidationException.
    // ─────────────────────────────────────────────────────────────────────────

    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static AssetLeaseRentDetailsEntity MakeLease(
        int id, int assetId, string tenantName, string workflowStatus, DateTime leaseStart, DateTime? leaseEnd = null, bool isActive = true) =>
        new()
        {
            Id = id,
            AssetId = assetId,
            TenantName = tenantName,
            TenantMobile = "9999999999",
            SecurityDeposit = 1000m,
            WorkflowStatus = workflowStatus,
            LeaseStartDate = leaseStart,
            LeaseEndDate = leaseEnd,
            IsActive = isActive
        };

    [Fact]
    public async Task ApplyFilters_WithNoFilters_ReturnsAllRows()
    {
        await using var context = CreateInMemoryContext();
        context.AssetLeaseRentDetails.AddRange(
            MakeLease(1, 10, "John Doe", "Approved", new DateTime(2026, 1, 1)),
            MakeLease(2, 20, "Jane Smith", "Pending", new DateTime(2026, 2, 1)));
        await context.SaveChangesAsync();

        var result = context.AssetLeaseRentDetails.AsQueryable()
            .ApplyFilters(new AssetLeaseRentDetailsQueryParameters())
            .ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ApplyFilters_ByAssetId_ReturnsMatchingRow()
    {
        await using var context = CreateInMemoryContext();
        context.AssetLeaseRentDetails.AddRange(
            MakeLease(1, 10, "John Doe", "Approved", new DateTime(2026, 1, 1)),
            MakeLease(2, 20, "Jane Smith", "Pending", new DateTime(2026, 2, 1)));
        await context.SaveChangesAsync();

        var result = context.AssetLeaseRentDetails.AsQueryable()
            .ApplyFilters(new AssetLeaseRentDetailsQueryParameters { AssetId = 10 })
            .ToList();

        Assert.Single(result);
        Assert.Equal(10, result[0].AssetId);
    }

    [Fact]
    public async Task ApplyFilters_ByWorkflowStatus_ReturnsMatchingRow()
    {
        await using var context = CreateInMemoryContext();
        context.AssetLeaseRentDetails.AddRange(
            MakeLease(1, 10, "John Doe", "Approved", new DateTime(2026, 1, 1)),
            MakeLease(2, 20, "Jane Smith", "Pending", new DateTime(2026, 2, 1)));
        await context.SaveChangesAsync();

        var result = context.AssetLeaseRentDetails.AsQueryable()
            .ApplyFilters(new AssetLeaseRentDetailsQueryParameters { WorkflowStatus = "Pending" })
            .ToList();

        Assert.Single(result);
        Assert.Equal("Jane Smith", result[0].TenantName);
    }

    [Fact]
    public async Task ApplyFilters_ByTenantNameContains_IsCaseInsensitive()
    {
        await using var context = CreateInMemoryContext();
        context.AssetLeaseRentDetails.AddRange(
            MakeLease(1, 10, "John Doe", "Approved", new DateTime(2026, 1, 1)),
            MakeLease(2, 20, "Jane Smith", "Pending", new DateTime(2026, 2, 1)));
        await context.SaveChangesAsync();

        var result = context.AssetLeaseRentDetails.AsQueryable()
            .ApplyFilters(new AssetLeaseRentDetailsQueryParameters { TenantName = "john" })
            .ToList();

        Assert.Single(result);
        Assert.Equal("John Doe", result[0].TenantName);
    }

    [Fact]
    public async Task ApplyFilters_ByFromDateAndToDate_ReturnsRowsWithinLeaseRange()
    {
        // FromDate maps to LeaseStartDate >= FromDate; ToDate maps to LeaseEndDate <= ToDate (both
        // independently, not an overlap check) - LeaseEndDate must be non-null for the ToDate
        // comparison to pass at all, since FilterExpressionBuilder guards nullable comparisons
        // with HasValue (a null LeaseEndDate always fails the ToDate filter).
        await using var context = CreateInMemoryContext();
        context.AssetLeaseRentDetails.AddRange(
            MakeLease(1, 10, "John Doe", "Approved", new DateTime(2026, 1, 1), new DateTime(2026, 2, 1)),
            MakeLease(2, 20, "Jane Smith", "Pending", new DateTime(2026, 6, 1), new DateTime(2026, 8, 1)),
            MakeLease(3, 30, "Bob Lee", "Approved", new DateTime(2026, 12, 1), new DateTime(2026, 12, 15)));
        await context.SaveChangesAsync();

        var result = context.AssetLeaseRentDetails.AsQueryable()
            .ApplyFilters(new AssetLeaseRentDetailsQueryParameters
            {
                FromDate = new DateTime(2026, 3, 1),
                ToDate = new DateTime(2026, 9, 1)
            })
            .ToList();

        Assert.Single(result);
        Assert.Equal(20, result[0].AssetId);
    }

    [Fact]
    public async Task ApplyFilters_CombiningMultipleFilters_AppliesAndLogic()
    {
        await using var context = CreateInMemoryContext();
        context.AssetLeaseRentDetails.AddRange(
            MakeLease(1, 10, "John Doe", "Approved", new DateTime(2026, 1, 1)),
            MakeLease(2, 10, "John Doe", "Pending", new DateTime(2026, 2, 1)));
        await context.SaveChangesAsync();

        var result = context.AssetLeaseRentDetails.AsQueryable()
            .ApplyFilters(new AssetLeaseRentDetailsQueryParameters { AssetId = 10, WorkflowStatus = "Approved" })
            .ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task ApplyFilters_NullFilterValues_AreIgnored()
    {
        await using var context = CreateInMemoryContext();
        context.AssetLeaseRentDetails.AddRange(
            MakeLease(1, 10, "John Doe", "Approved", new DateTime(2026, 1, 1)),
            MakeLease(2, 20, "Jane Smith", "Pending", new DateTime(2026, 2, 1)));
        await context.SaveChangesAsync();

        var result = context.AssetLeaseRentDetails.AsQueryable()
            .ApplyFilters(new AssetLeaseRentDetailsQueryParameters { AssetId = null, WorkflowStatus = null })
            .ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ApplyFilters_WithAllJoinDependentPropertiesPopulated_DoesNotThrow_AndIgnoresThem()
    {
        // The core regression test for this fix: before the fix, populating any one of
        // AssetCategoryId/ZoneId/AssetTypeId/WardId/ParentAssetId/AssetNo/RentStatus/PaymentStatus
        // would make BuildFilterExpression throw FilterValidationException ("Property '...' not
        // found on entity type 'AssetLeaseRentDetailsEntity'"). Now that they carry no
        // [Filterable] attribute, ApplyFilters must silently ignore them (they're not part of the
        // generic filter surface) while still correctly honoring the one real, direct-property
        // filter (AssetId) alongside them.
        await using var context = CreateInMemoryContext();
        context.AssetLeaseRentDetails.AddRange(
            MakeLease(1, 10, "John Doe", "Approved", new DateTime(2026, 1, 1)),
            MakeLease(2, 20, "Jane Smith", "Pending", new DateTime(2026, 2, 1)));
        await context.SaveChangesAsync();

        var queryParameters = new AssetLeaseRentDetailsQueryParameters
        {
            AssetCategoryId = 999,
            ZoneId = 999,
            AssetTypeId = 999,
            WardId = 999,
            ParentAssetId = 999,
            AssetNo = "DOES-NOT-EXIST",
            RentStatus = "SomeStatus",
            PaymentStatus = "SomeStatus",
            AssetId = 10
        };

        var exception = Record.Exception(() =>
            context.AssetLeaseRentDetails.AsQueryable().ApplyFilters(queryParameters).ToList());

        Assert.Null(exception);

        var result = context.AssetLeaseRentDetails.AsQueryable().ApplyFilters(queryParameters).ToList();
        Assert.Single(result);
        Assert.Equal(10, result[0].AssetId);
    }
}
