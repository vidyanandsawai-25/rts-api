using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Repositories;

/// <summary>
/// Comprehensive tests for PropertyRepository tax details methods
/// Tests GetTaxDetailsAsync and GetTaxDetailsCVAsync
/// </summary>
public class PropertyRepositoryTaxDetailsTests
{
    #region GetTaxDetailsAsync Tests

    [Fact]
    public async Task GetTaxDetailsAsync_PropertyDoesNotExist_ReturnsNull()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsAsync(999999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTaxDetailsAsync_PropertyIsInactive_ReturnsNull()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity
        {
            Id = 1,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = false, // Inactive
            MarkedForDeletion = false
        };

        context.PropertyMast.Add(property);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsAsync(1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTaxDetailsAsync_PropertyMarkedForDeletion_ReturnsNull()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity
        {
            Id = 1,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = true,
            MarkedForDeletion = true // Marked for deletion
        };

        context.PropertyMast.Add(property);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsAsync(1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTaxDetailsAsync_WithSinglePolicyAndMultipleTaxes_ReturnsCorrectGroupedData()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity
        {
            Id = 1,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };

        var tax1 = new TaxMasterEntity { Id = 1, TaxName = "Property Tax", TaxCode = "PROP", DisplayOrder = 1, IsActive = true };
        var tax2 = new TaxMasterEntity { Id = 2, TaxName = "Water Tax", TaxCode = "WATER", DisplayOrder = 2, IsActive = true };
        var tax3 = new TaxMasterEntity { Id = 3, TaxName = "Sewerage Tax", TaxCode = "SEWERAGE", DisplayOrder = 3, IsActive = true };

        var policyTax1 = new PolicyTaxDetailsEntity
        {
            Id = 1,
            PropertyId = 1,
            PolicyCode = "POL2024",
            TaxId = 1,
            TaxAmount = 1000.50m,
            IsActive = true,
            MarkedForDeletion = false
        };

        var policyTax2 = new PolicyTaxDetailsEntity
        {
            Id = 2,
            PropertyId = 1,
            PolicyCode = "POL2024",
            TaxId = 2,
            TaxAmount = 500.25m,
            IsActive = true,
            MarkedForDeletion = false
        };

        var policyTax3 = new PolicyTaxDetailsEntity
        {
            Id = 3,
            PropertyId = 1,
            PolicyCode = "POL2024",
            TaxId = 3,
            TaxAmount = 300.00m,
            IsActive = true,
            MarkedForDeletion = false
        };

        context.PropertyMast.Add(property);
        context.TaxMaster.AddRange(tax1, tax2, tax3);
        context.PolicyTaxDetails.AddRange(policyTax1, policyTax2, policyTax3);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.PropertyId);
        Assert.Single(result.Policies);

        var policy = result.Policies[0];
        Assert.Equal("POL2024", policy.PolicyCode);
        Assert.Equal(3, policy.TaxAmounts.Count);
        Assert.Equal(1000.50m, policy.TaxAmounts.Single(t => t.TaxName == "Property Tax").TaxAmount);
        Assert.Equal(500.25m, policy.TaxAmounts.Single(t => t.TaxName == "Water Tax").TaxAmount);
        Assert.Equal(300.00m, policy.TaxAmounts.Single(t => t.TaxName == "Sewerage Tax").TaxAmount);
        Assert.Equal(1800.75m, policy.TaxTotal);
    }

    [Fact]
    public async Task GetTaxDetailsAsync_WithMultiplePolicies_ReturnsCorrectGroupedData()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity
        {
            Id = 1,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };

        var tax1 = new TaxMasterEntity { Id = 1, TaxName = "Property Tax", TaxCode = "PROP", DisplayOrder = 1, IsActive = true };
        var tax2 = new TaxMasterEntity { Id = 2, TaxName = "Water Tax", TaxCode = "WATER", DisplayOrder = 2, IsActive = true };

        // Policy 2023
        var policyTax1 = new PolicyTaxDetailsEntity
        {
            Id = 1,
            PropertyId = 1,
            PolicyCode = "POL2023",
            TaxId = 1,
            TaxAmount = 900.00m,
            IsActive = true,
            MarkedForDeletion = false
        };

        var policyTax2 = new PolicyTaxDetailsEntity
        {
            Id = 2,
            PropertyId = 1,
            PolicyCode = "POL2023",
            TaxId = 2,
            TaxAmount = 450.00m,
            IsActive = true,
            MarkedForDeletion = false
        };

        // Policy 2024
        var policyTax3 = new PolicyTaxDetailsEntity
        {
            Id = 3,
            PropertyId = 1,
            PolicyCode = "POL2024",
            TaxId = 1,
            TaxAmount = 1000.00m,
            IsActive = true,
            MarkedForDeletion = false
        };

        var policyTax4 = new PolicyTaxDetailsEntity
        {
            Id = 4,
            PropertyId = 1,
            PolicyCode = "POL2024",
            TaxId = 2,
            TaxAmount = 500.00m,
            IsActive = true,
            MarkedForDeletion = false
        };

        context.PropertyMast.Add(property);
        context.TaxMaster.AddRange(tax1, tax2);
        context.PolicyTaxDetails.AddRange(policyTax1, policyTax2, policyTax3, policyTax4);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.PropertyId);
        Assert.Equal(2, result.Policies.Count);

        var policy2023 = result.Policies.FirstOrDefault(p => p.PolicyCode == "POL2023");
        Assert.NotNull(policy2023);
        Assert.Equal(2, policy2023.TaxAmounts.Count);
        Assert.Equal(900.00m, policy2023.TaxAmounts.First(t => t.TaxName == "Property Tax").TaxAmount);
        Assert.Equal(450.00m, policy2023.TaxAmounts.First(t => t.TaxName == "Water Tax").TaxAmount);

        var policy2024 = result.Policies.FirstOrDefault(p => p.PolicyCode == "POL2024");
        Assert.NotNull(policy2024);
        Assert.Equal(2, policy2024.TaxAmounts.Count);
        Assert.Equal(1000.00m, policy2024.TaxAmounts.First(t => t.TaxName == "Property Tax").TaxAmount);
        Assert.Equal(500.00m, policy2024.TaxAmounts.First(t => t.TaxName == "Water Tax").TaxAmount);
    }

    [Fact]
    public async Task GetTaxDetailsAsync_WithDuplicateTaxNamesSamePolicy_SumsTaxAmounts()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity
        {
            Id = 1,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };

        var tax1 = new TaxMasterEntity { Id = 1, TaxName = "Property Tax", TaxCode = "PROP", DisplayOrder = 1, IsActive = true };

        // Same PolicyCode and TaxName - should be summed
        var policyTax1 = new PolicyTaxDetailsEntity
        {
            Id = 1,
            PropertyId = 1,
            PolicyCode = "POL2024",
            TaxId = 1,
            TaxAmount = 500.00m,
            IsActive = true,
            MarkedForDeletion = false
        };

        var policyTax2 = new PolicyTaxDetailsEntity
        {
            Id = 2,
            PropertyId = 1,
            PolicyCode = "POL2024",
            TaxId = 1,
            TaxAmount = 300.00m,
            IsActive = true,
            MarkedForDeletion = false
        };

        context.PropertyMast.Add(property);
        context.TaxMaster.Add(tax1);
        context.PolicyTaxDetails.AddRange(policyTax1, policyTax2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Policies);
        Assert.Equal(800.00m, result.Policies[0].TaxAmounts.First(t => t.TaxName == "Property Tax").TaxAmount);
    }

    [Fact]
    public async Task GetTaxDetailsAsync_WithInactiveTaxDetails_ExcludesThem()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity
        {
            Id = 1,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };

        var tax1 = new TaxMasterEntity { Id = 1, TaxName = "Property Tax", TaxCode = "PROP", DisplayOrder = 1, IsActive = true };
        var tax2 = new TaxMasterEntity { Id = 2, TaxName = "Water Tax", TaxCode = "WATER", DisplayOrder = 2, IsActive = true };

        var activeTax = new PolicyTaxDetailsEntity
        {
            Id = 1,
            PropertyId = 1,
            PolicyCode = "POL2024",
            TaxId = 1,
            TaxAmount = 1000.00m,
            IsActive = true,
            MarkedForDeletion = false
        };

        var inactiveTax = new PolicyTaxDetailsEntity
        {
            Id = 2,
            PropertyId = 1,
            PolicyCode = "POL2024",
            TaxId = 2,
            TaxAmount = 500.00m,
            IsActive = false, // Inactive
            MarkedForDeletion = false
        };

        context.PropertyMast.Add(property);
        context.TaxMaster.AddRange(tax1, tax2);
        context.PolicyTaxDetails.AddRange(activeTax, inactiveTax);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Policies);
        Assert.Single(result.Policies[0].TaxAmounts);
        Assert.True(result.Policies[0].TaxAmounts.Any(t => t.TaxName == "Property Tax"));
        Assert.False(result.Policies[0].TaxAmounts.Any(t => t.TaxName == "Water Tax"));
    }

    [Fact]
    public async Task GetTaxDetailsAsync_WithMarkedForDeletionTaxDetails_ReturnsNull()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity
        {
            Id = 1,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };

        var tax1 = new TaxMasterEntity { Id = 1, TaxName = "Property Tax", TaxCode = "PROP", DisplayOrder = 1, IsActive = true };

        var deletedTax = new PolicyTaxDetailsEntity
        {
            Id = 1,
            PropertyId = 1,
            PolicyCode = "POL2024",
            TaxId = 1,
            TaxAmount = 1000.00m,
            IsActive = true,
            MarkedForDeletion = true // Marked for deletion
        };

        context.PropertyMast.Add(property);
        context.TaxMaster.Add(tax1);
        context.PolicyTaxDetails.Add(deletedTax);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsAsync(1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTaxDetailsAsync_WithNullTaxAmount_DefaultsToZero()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity
        {
            Id = 1,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };

        var tax1 = new TaxMasterEntity { Id = 1, TaxName = "Property Tax", TaxCode = "PROP", DisplayOrder = 1, IsActive = true };

        var policyTax = new PolicyTaxDetailsEntity
        {
            Id = 1,
            PropertyId = 1,
            PolicyCode = "POL2024",
            TaxId = 1,
            TaxAmount = null, // Null amount
            IsActive = true,
            MarkedForDeletion = false
        };

        context.PropertyMast.Add(property);
        context.TaxMaster.Add(tax1);
        context.PolicyTaxDetails.Add(policyTax);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Policies);
        Assert.Equal(0m, result.Policies[0].TaxAmounts.First(t => t.TaxName == "Property Tax").TaxAmount);
    }

    [Fact]
    public async Task GetTaxDetailsAsync_WithTaxesHavingDisplayOrder_ReturnsTaxes()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity
        {
            Id = 1,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };

        // Create taxes with specific display orders
        var tax1 = new TaxMasterEntity { Id = 1, TaxName = "Sewerage Tax", TaxCode = "SEWERAGE", DisplayOrder = 3, IsActive = true };
        var tax2 = new TaxMasterEntity { Id = 2, TaxName = "Property Tax", TaxCode = "PROP", DisplayOrder = 1, IsActive = true };
        var tax3 = new TaxMasterEntity { Id = 3, TaxName = "Water Tax", TaxCode = "WATER", DisplayOrder = 2, IsActive = true };

        var policyTax1 = new PolicyTaxDetailsEntity
        {
            Id = 1,
            PropertyId = 1,
            PolicyCode = "POL2024",
            TaxId = 1,
            TaxAmount = 300.00m,
            IsActive = true,
            MarkedForDeletion = false
        };

        var policyTax2 = new PolicyTaxDetailsEntity
        {
            Id = 2,
            PropertyId = 1,
            PolicyCode = "POL2024",
            TaxId = 2,
            TaxAmount = 1000.00m,
            IsActive = true,
            MarkedForDeletion = false
        };

        var policyTax3 = new PolicyTaxDetailsEntity
        {
            Id = 3,
            PropertyId = 1,
            PolicyCode = "POL2024",
            TaxId = 3,
            TaxAmount = 500.00m,
            IsActive = true,
            MarkedForDeletion = false
        };

        context.PropertyMast.Add(property);
        context.TaxMaster.AddRange(tax1, tax2, tax3);
        context.PolicyTaxDetails.AddRange(policyTax1, policyTax2, policyTax3);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsAsync(1);

        // Assert
        Assert.NotNull(result);
        var taxNames = result.Policies[0].TaxAmounts.Select(t => t.TaxName).ToList();
        // Verify all taxes are present
        Assert.Contains("Property Tax", taxNames);
        Assert.Contains("Water Tax", taxNames);
        Assert.Contains("Sewerage Tax", taxNames);
    }

    #endregion

    #region GetTaxDetailsCVAsync Tests

    [Fact]
    public async Task GetTaxDetailsCVAsync_PropertyDoesNotExist_ReturnsNull()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);
        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsCVAsync(999999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTaxDetailsCVAsync_PropertyIsInactive_ReturnsNull()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity
        {
            Id = 1,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = false,
            MarkedForDeletion = false
        };

        context.PropertyMast.Add(property);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsCVAsync(1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTaxDetailsCVAsync_PropertyMarkedForDeletion_ReturnsNull()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity
        {
            Id = 1,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = true,
            MarkedForDeletion = true
        };

        context.PropertyMast.Add(property);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsCVAsync(1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTaxDetailsCVAsync_WithSinglePolicyAndMultipleTaxes_ReturnsCorrectGroupedData()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity
        {
            Id = 1,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };

        var tax1 = new TaxMasterEntity { Id = 1, TaxName = "Capital Value Tax", TaxCode = "CV", DisplayOrder = 1, IsActive = true };
        var tax2 = new TaxMasterEntity { Id = 2, TaxName = "Education Cess", TaxCode = "EDU", DisplayOrder = 2, IsActive = true };

        var policyTaxCV1 = new PolicyTaxDetailsCVEntity
        {
            Id = 1,
            PropertyId = 1,
            PolicyCode = "POLCV2024",
            TaxId = 1,
            TaxAmount = 2000.50m,
            IsActive = true,
            MarkedForDeletion = false
        };

        var policyTaxCV2 = new PolicyTaxDetailsCVEntity
        {
            Id = 2,
            PropertyId = 1,
            PolicyCode = "POLCV2024",
            TaxId = 2,
            TaxAmount = 750.25m,
            IsActive = true,
            MarkedForDeletion = false
        };

        context.PropertyMast.Add(property);
        context.TaxMaster.AddRange(tax1, tax2);
        context.PolicyTaxDetailsCV.AddRange(policyTaxCV1, policyTaxCV2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsCVAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.PropertyId);
        Assert.Single(result.Policies);

        var policy = result.Policies[0];
        Assert.Equal("POLCV2024", policy.PolicyCode);
        Assert.Equal(2, policy.TaxAmounts.Count);
        Assert.Equal(2000.50m, policy.TaxAmounts.First(t => t.TaxName == "Capital Value Tax").TaxAmount);
        Assert.Equal(750.25m, policy.TaxAmounts.First(t => t.TaxName == "Education Cess").TaxAmount);
        Assert.Equal(2750.75m, policy.TaxTotal);
    }

    [Fact]
    public async Task GetTaxDetailsCVAsync_WithMultiplePolicies_ReturnsCorrectGroupedData()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity
        {
            Id = 1,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };

        var tax1 = new TaxMasterEntity { Id = 1, TaxName = "Capital Value Tax", TaxCode = "CV", DisplayOrder = 1, IsActive = true };

        // Policy CV 2023
        var policyTaxCV1 = new PolicyTaxDetailsCVEntity
        {
            Id = 1,
            PropertyId = 1,
            PolicyCode = "POLCV2023",
            TaxId = 1,
            TaxAmount = 1800.00m,
            IsActive = true,
            MarkedForDeletion = false
        };

        // Policy CV 2024
        var policyTaxCV2 = new PolicyTaxDetailsCVEntity
        {
            Id = 2,
            PropertyId = 1,
            PolicyCode = "POLCV2024",
            TaxId = 1,
            TaxAmount = 2000.00m,
            IsActive = true,
            MarkedForDeletion = false
        };

        context.PropertyMast.Add(property);
        context.TaxMaster.Add(tax1);
        context.PolicyTaxDetailsCV.AddRange(policyTaxCV1, policyTaxCV2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsCVAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.PropertyId);
        Assert.Equal(2, result.Policies.Count);

        var policy2023 = result.Policies.FirstOrDefault(p => p.PolicyCode == "POLCV2023");
        Assert.NotNull(policy2023);
        Assert.Equal(1800.00m, policy2023.TaxAmounts.First(t => t.TaxName == "Capital Value Tax").TaxAmount);

        var policy2024 = result.Policies.FirstOrDefault(p => p.PolicyCode == "POLCV2024");
        Assert.NotNull(policy2024);
        Assert.Equal(2000.00m, policy2024.TaxAmounts.First(t => t.TaxName == "Capital Value Tax").TaxAmount);
    }

    [Fact]
    public async Task GetTaxDetailsCVAsync_WithInactiveTaxDetails_ExcludesThem()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity
        {
            Id = 1,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };

        var tax1 = new TaxMasterEntity { Id = 1, TaxName = "Capital Value Tax", TaxCode = "CV", DisplayOrder = 1, IsActive = true };

        var activeTax = new PolicyTaxDetailsCVEntity
        {
            Id = 1,
            PropertyId = 1,
            PolicyCode = "POLCV2024",
            TaxId = 1,
            TaxAmount = 2000.00m,
            IsActive = true,
            MarkedForDeletion = false
        };

        var inactiveTax = new PolicyTaxDetailsCVEntity
        {
            Id = 2,
            PropertyId = 1,
            PolicyCode = "POLCV2024",
            TaxId = 1,
            TaxAmount = 500.00m,
            IsActive = false, // Inactive
            MarkedForDeletion = false
        };

        context.PropertyMast.Add(property);
        context.TaxMaster.Add(tax1);
        context.PolicyTaxDetailsCV.AddRange(activeTax, inactiveTax);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsCVAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Policies);
        Assert.Equal(2000.00m, result.Policies[0].TaxAmounts.First(t => t.TaxName == "Capital Value Tax").TaxAmount);
    }

    [Fact]
    public async Task GetTaxDetailsCVAsync_WithMarkedForDeletion_ReturnsNull()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity
        {
            Id = 1,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };

        var tax1 = new TaxMasterEntity { Id = 1, TaxName = "Capital Value Tax", TaxCode = "CV", DisplayOrder = 1, IsActive = true };

        var deletedTax = new PolicyTaxDetailsCVEntity
        {
            Id = 1,
            PropertyId = 1,
            PolicyCode = "POLCV2024",
            TaxId = 1,
            TaxAmount = 2000.00m,
            IsActive = true,
            MarkedForDeletion = true // Marked for deletion
        };

        context.PropertyMast.Add(property);
        context.TaxMaster.Add(tax1);
        context.PolicyTaxDetailsCV.Add(deletedTax);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsCVAsync(1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTaxDetailsCVAsync_WithDuplicateTaxNamesSamePolicy_SumsTaxAmounts()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ApplicationDbContext(options);

        var property = new PropertyEntity
        {
            Id = 1,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };

        var tax1 = new TaxMasterEntity { Id = 1, TaxName = "Capital Value Tax", TaxCode = "CV", DisplayOrder = 1, IsActive = true };

        // Same PolicyCode and TaxName - should be summed
        var policyTaxCV1 = new PolicyTaxDetailsCVEntity
        {
            Id = 1,
            PropertyId = 1,
            PolicyCode = "POLCV2024",
            TaxId = 1,
            TaxAmount = 1000.00m,
            IsActive = true,
            MarkedForDeletion = false
        };

        var policyTaxCV2 = new PolicyTaxDetailsCVEntity
        {
            Id = 2,
            PropertyId = 1,
            PolicyCode = "POLCV2024",
            TaxId = 1,
            TaxAmount = 1000.00m,
            IsActive = true,
            MarkedForDeletion = false
        };

        context.PropertyMast.Add(property);
        context.TaxMaster.Add(tax1);
        context.PolicyTaxDetailsCV.AddRange(policyTaxCV1, policyTaxCV2);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context);

        // Act
        var result = await repository.GetTaxDetailsCVAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Policies);
        Assert.Equal(2000.00m, result.Policies[0].TaxAmounts.First(t => t.TaxName == "Capital Value Tax").TaxAmount);
    }

    #endregion
}
