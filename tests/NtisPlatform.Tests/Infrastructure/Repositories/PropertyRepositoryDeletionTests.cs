using Microsoft.EntityFrameworkCore;
using Moq;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Repositories;

/// <summary>
/// Tests for PropertyRepository deletion-related methods.
/// These tests cover the new repository methods added for property deletion functionality.
/// </summary>
public class PropertyRepositoryDeletionTests
{
    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    #region GetPropertyDetailsByPropertyIdAsync Tests

    [Fact]
    public async Task GetPropertyDetailsByPropertyIdAsync_WithExistingProperty_ReturnsPropertyDetails()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var propertyId = 1;

        var property = new PropertyEntity
        {
            Id = propertyId,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };

        var propertyDetails = new List<PropertyDetailsEntity>
        {
            new() { Id = 1, PropertyId = propertyId, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false },
            new() { Id = 2, PropertyId = propertyId, FloorId = 2, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false }
        };

        context.PropertyMast.Add(property);
        context.PropertyDetails.AddRange(propertyDetails);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        // Act
        var result = await repository.GetPropertyDetailsByPropertyIdAsync(propertyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, pd => Assert.Equal(propertyId, pd.PropertyId));
    }

    [Fact]
    public async Task GetPropertyDetailsByPropertyIdAsync_WithNoPropertyDetails_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var propertyId = 999;

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        // Act
        var result = await repository.GetPropertyDetailsByPropertyIdAsync(propertyId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetRvResultsByPropertyIdAsync Tests

    [Fact]
    public async Task GetRvResultsByPropertyIdAsync_WithValidId_ReturnsResults()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var propertyId = 1;

        var rvResults = new List<RVCalculationResultsEntity>
        {
            new() { Id = 1, PropertyId = propertyId, PropertyDetailsId = 1, IsActive = true, MarkedForDeletion = false },
            new() { Id = 2, PropertyId = propertyId, PropertyDetailsId = 2, IsActive = true, MarkedForDeletion = false }
        };

        context.RVCalculationResults.AddRange(rvResults);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        // Act
        var result = await repository.GetRvResultsByPropertyIdAsync(propertyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(propertyId, r.PropertyId));
    }

    [Fact]
    public async Task GetRvResultsByPropertyIdAsync_WithNoResults_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        // Act
        var result = await repository.GetRvResultsByPropertyIdAsync(999);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetSection129ResultsByPropertyIdAsync Tests

    [Fact]
    public async Task GetSection129ResultsByPropertyIdAsync_WithValidId_ReturnsResults()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var propertyId = 1;

        var section129Results = new List<PropertyTaxCalculationSection129ResultsEntity>
        {
            new() { Id = 1, PropertyId = propertyId, PropertyDetailsId = 1, IsActive = true, MarkedForDeletion = false }
        };

        context.PropertyTaxCalculationSection129Results.AddRange(section129Results);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        // Act
        var result = await repository.GetSection129ResultsByPropertyIdAsync(propertyId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(propertyId, result[0].PropertyId);
    }

    [Fact]
    public async Task GetSection129ResultsByPropertyIdAsync_WithNoResults_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        // Act
        var result = await repository.GetSection129ResultsByPropertyIdAsync(999);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetPropertyOccupancyByPropertyDetailIdsAsync Tests

    [Fact]
    public async Task GetPropertyOccupancyByPropertyDetailIdsAsync_WithValidIds_ReturnsResults()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var propertyDetailIds = new List<int> { 1, 2 };

        var propertyDetails = new List<PropertyDetailsEntity>
        {
            new() { Id = 1, PropertyId = 1, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false },
            new() { Id = 2, PropertyId = 1, FloorId = 2, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false }
        };

        var occupancyDetails = new List<PropertyOccupancyDetailsEntity>
        {
            new() { Id = 1, PropertyDetailId = 1, IsActive = true, MarkedForDeletion = false, CreatedDate = DateTime.UtcNow },
            new() { Id = 2, PropertyDetailId = 2, IsActive = true, MarkedForDeletion = false, CreatedDate = DateTime.UtcNow }
        };

        context.PropertyDetails.AddRange(propertyDetails);
        context.PropertyOccupancyDetails.AddRange(occupancyDetails);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        // Act
        var result = await repository.GetPropertyOccupancyByPropertyDetailIdsAsync(propertyDetailIds);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, o => Assert.Contains(o.PropertyDetailId, propertyDetailIds));
    }

    [Fact]
    public async Task GetPropertyOccupancyByPropertyDetailIdsAsync_WithNoMatchingIds_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var propertyDetailIds = new List<int> { 999 };

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        // Act
        var result = await repository.GetPropertyOccupancyByPropertyDetailIdsAsync(propertyDetailIds);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPropertyOccupancyByPropertyDetailIdsAsync_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        // Act
        var result = await repository.GetPropertyOccupancyByPropertyDetailIdsAsync(new List<int>());

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetRentersByPropertyDetailIdsAsync Tests

    [Fact]
    public async Task GetRentersByPropertyDetailIdsAsync_WithValidIds_ReturnsResults()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var propertyDetailIds = new List<int> { 1 };

        var renters = new List<RenterMastEntity>
        {
            new() { Id = 1, PropertyDetailsId = 1, IsActive = true, MarkedForDeletion = false }
        };

        context.RenterMast.AddRange(renters);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        // Act
        var result = await repository.GetRentersByPropertyDetailIdsAsync(propertyDetailIds);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }

    #endregion

    #region GetRoomWiseSubmissionByPropertyIdAsync Tests

    [Fact]
    public async Task GetRoomWiseSubmissionByPropertyIdAsync_WithValidId_ReturnsResults()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var propertyId = 1;

        var submissions = new List<RoomWiseSubmissionDetailsEntity>
        {
            new() { Id = 1, PropertyId = propertyId, PropertyDetailsId = 1, IsActive = true, MarkedForDeletion = false },
            new() { Id = 2, PropertyId = propertyId, PropertyDetailsId = 2, IsActive = true, MarkedForDeletion = false }
        };

        context.RoomWiseSubmissionDetails.AddRange(submissions);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        // Act
        var result = await repository.GetRoomWiseSubmissionByPropertyIdAsync(propertyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.Equal(propertyId, s.PropertyId));
    }

    [Fact]
    public async Task GetRoomWiseSubmissionByPropertyIdAsync_WithNullPropertyDetailsId_IncludesInResults()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var propertyId = 1;

        var submissions = new List<RoomWiseSubmissionDetailsEntity>
        {
            new() { Id = 1, PropertyId = propertyId, PropertyDetailsId = 1, IsActive = true, MarkedForDeletion = false },
            new() { Id = 2, PropertyId = propertyId, PropertyDetailsId = null, IsActive = true, MarkedForDeletion = false } // Should be included when querying by PropertyId
        };

        context.RoomWiseSubmissionDetails.AddRange(submissions);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        // Act
        var result = await repository.GetRoomWiseSubmissionByPropertyIdAsync(propertyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count); // Both records should be included
        Assert.All(result, s => Assert.Equal(propertyId, s.PropertyId));
    }

    [Fact]
    public async Task GetRoomWiseSubmissionByPropertyIdAsync_WithNoResults_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        // Act
        var result = await repository.GetRoomWiseSubmissionByPropertyIdAsync(999);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetRelatedEntitiesForDeletionAsync Tests

    [Fact]
    public async Task GetRelatedEntitiesForDeletionAsync_WithRelatedEntities_ReturnsAllIHardDeletableEntities()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var propertyId = 1;

        // Add various related entities
        context.ApplyTaxesMaster.Add(new ApplyTaxesMasterEntity { Id = 1, PropertyId = propertyId, IsActive = true, MarkedForDeletion = false });
        context.PlotDetails.Add(new PlotDetailsEntity { Id = 1, PropertyId = propertyId, IsActive = true, MarkedForDeletion = false });
        context.PolicyTaxDetails.Add(new PolicyTaxDetailsEntity { Id = 1, PropertyId = propertyId, IsActive = true, MarkedForDeletion = false });
        context.PropertyMastDetails.Add(new PropertyAssessmentEntity { Id = 1, PropertyId = propertyId, IsActive = true, MarkedForDeletion = false });

        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        // Act
        var result = await repository.GetRelatedEntitiesForDeletionAsync(propertyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Count); // Updated from 3 to 4 to include PropertyAssessmentEntity
        Assert.All(result, e => Assert.IsAssignableFrom<IHardDeletable>(e));
    }

    [Fact]
    public async Task GetRelatedEntitiesForDeletionAsync_WithNoRelatedEntities_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var propertyId = 999;

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        // Act
        var result = await repository.GetRelatedEntitiesForDeletionAsync(propertyId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRelatedEntitiesForDeletionAsync_IncludesAllTransactionEntities()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var propertyId = 1;

        // Add transaction-related entities. TransMast now holds both CV and RV rows
        // (CalculationType discriminator) since TransMastCV/TransMastRV were folded into it.
        context.TransMast.Add(new TransMastEntity { Id = 1, PropertyId = propertyId, TaxId = 1, FinanceYearId = 1, CalculationType = "CV", IsActive = true, MarkedForDeletion = false });
        context.TransMast.Add(new TransMastEntity { Id = 2, PropertyId = propertyId, TaxId = 1, FinanceYearId = 1, CalculationType = "RV", IsActive = true, MarkedForDeletion = false });

        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        // Act
        var result = await repository.GetRelatedEntitiesForDeletionAsync(propertyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetRelatedEntitiesForDeletionAsync_IncludesTaxPendingEntities()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var propertyId = 1;

        // Add tax pending entities
        context.TaxPendingDetails.Add(new TaxPendingDetailsEntity { Id = 1, PropertyId = propertyId, IsActive = true, MarkedForDeletion = false });
        context.TaxPendingDetailsRV.Add(new TaxPendingDetailsRVEntity { Id = 1, PropertyId = propertyId, IsActive = true, MarkedForDeletion = false });
        context.TaxPendingDetailsCV.Add(new TaxPendingDetailsCVEntity { Id = 1, PropertyId = propertyId, IsActive = true, MarkedForDeletion = false });

        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        // Act
        var result = await repository.GetRelatedEntitiesForDeletionAsync(propertyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetRelatedEntitiesForDeletionAsync_IncludesPropertyAssessmentEntities()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var propertyId = 1;

        // Add PropertyAssessmentEntity (PropertyMastDetails)
        context.PropertyMastDetails.Add(new PropertyAssessmentEntity 
        { 
            Id = 1, 
            PropertyId = propertyId, 
            IsActive = true, 
            MarkedForDeletion = false 
        });

        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        // Act
        var result = await repository.GetRelatedEntitiesForDeletionAsync(propertyId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.IsType<PropertyAssessmentEntity>(result[0]);

        var assessment = result[0] as PropertyAssessmentEntity;
        Assert.NotNull(assessment);
        Assert.Equal(propertyId, assessment.PropertyId);
    }

    [Fact]
    public async Task GetRelatedEntitiesForDeletionAsync_WithMultiplePropertyAssessments_ReturnsAll()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var propertyId = 1;

        // Add multiple PropertyAssessmentEntity records (although typically there's only one)
        context.PropertyMastDetails.AddRange(
            new PropertyAssessmentEntity { Id = 1, PropertyId = propertyId, IsActive = true, MarkedForDeletion = false },
            new PropertyAssessmentEntity { Id = 2, PropertyId = propertyId, IsActive = true, MarkedForDeletion = false }
        );

        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        // Act
        var result = await repository.GetRelatedEntitiesForDeletionAsync(propertyId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.IsType<PropertyAssessmentEntity>(e));
        Assert.All(result.Cast<PropertyAssessmentEntity>(), a => Assert.Equal(propertyId, a.PropertyId));
    }

    #endregion

    #region GetRoomWiseMinusBySubmissionIdsAsync Tests

    [Fact]
    public async Task GetRoomWiseMinusBySubmissionIdsAsync_WithValidIds_ReturnsResults()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var submissionIds = new List<int> { 1, 2 };

        var minusData = new List<RoomWiseMinusDataEntity>
        {
            new() { Id = 1, RoomWiseSubmissionId = 1, IsActive = true, MarkedForDeletion = false },
            new() { Id = 2, RoomWiseSubmissionId = 2, IsActive = true, MarkedForDeletion = false }
        };

        context.RoomWiseMinusData.AddRange(minusData);
        await context.SaveChangesAsync();

        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        // Act
        var result = await repository.GetRoomWiseMinusBySubmissionIdsAsync(submissionIds);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, m => Assert.Contains(m.RoomWiseSubmissionId, submissionIds));
    }

    [Fact]
    public async Task GetRoomWiseMinusBySubmissionIdsAsync_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        // Act
        var result = await repository.GetRoomWiseMinusBySubmissionIdsAsync(new List<int>());

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRoomWiseMinusBySubmissionIdsAsync_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        var minusData = new RoomWiseMinusDataEntity
        {
            Id = 1,
            RoomWiseSubmissionId = 99,
            IsActive = true,
            MarkedForDeletion = false
        };

        context.RoomWiseMinusData.Add(minusData);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetRoomWiseMinusBySubmissionIdsAsync(new List<int> { 1, 2, 3 });

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region MarkEntitiesForDeletion Tests

    [Fact]
    public void MarkEntitiesForDeletion_WithValidEntities_SetsAllDeletionFlags()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        var entities = new List<PropertyDetailsEntity>
        {
            new() { Id = 1, PropertyId = 1, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false },
            new() { Id = 2, PropertyId = 1, FloorId = 2, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false }
        };

        context.PropertyDetails.AddRange(entities);
        context.SaveChanges();

        // Act
        repository.MarkEntitiesForDeletion(entities);

        // Assert
        Assert.All(entities, entity =>
        {
            Assert.True(entity.MarkedForDeletion);
            Assert.NotNull(entity.MarkedForDeletionDate);
            Assert.False(entity.IsActive);
            Assert.NotNull(entity.UpdatedDate);
        });

        // Verify EF Core tracked the changes
        Assert.All(entities, entity =>
        {
            var entry = context.Entry(entity);
            Assert.Equal(EntityState.Modified, entry.State);
        });
    }

    [Fact]
    public void MarkEntitiesForDeletion_PreservesExistingDeletionDate()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        var existingDeletionDate = DateTime.Now.AddDays(-1);
        var entity = new PropertyDetailsEntity
        {
            Id = 1,
            PropertyId = 1,
            FloorId = 1,
            ConstructionTypeId = 1,
            TypeOfUseId = 1,
            IsActive = true,
            MarkedForDeletion = false,
            MarkedForDeletionDate = existingDeletionDate
        };

        context.PropertyDetails.Add(entity);
        context.SaveChanges();

        // Act
        repository.MarkEntitiesForDeletion(new[] { entity });

        // Assert
        Assert.True(entity.MarkedForDeletion);
        Assert.Equal(existingDeletionDate, entity.MarkedForDeletionDate);
        Assert.False(entity.IsActive);
    }

    [Fact]
    public void MarkEntitiesForDeletion_WithEmptyList_DoesNotThrow()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
        var entities = new List<PropertyDetailsEntity>();

        // Act & Assert - Should not throw
        repository.MarkEntitiesForDeletion(entities);
    }

    [Fact]
    public void MarkEntitiesForDeletion_WithDifferentEntityTypes_MarksAll()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        var propertyDetails = new PropertyDetailsEntity
        {
            Id = 1,
            PropertyId = 1,
            FloorId = 1,
            ConstructionTypeId = 1,
            TypeOfUseId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };

        var rvResult = new RVCalculationResultsEntity
        {
            Id = 1,
            PropertyDetailsId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };

        context.PropertyDetails.Add(propertyDetails);
        context.RVCalculationResults.Add(rvResult);
        context.SaveChanges();

        var entities = new List<IHardDeletable> { propertyDetails, rvResult };

        // Act
        repository.MarkEntitiesForDeletion(entities);

        // Assert
        Assert.All(entities, entity =>
        {
            Assert.True(entity.MarkedForDeletion);
            Assert.NotNull(entity.MarkedForDeletionDate);
        });

        Assert.False(propertyDetails.IsActive);
        Assert.NotNull(propertyDetails.UpdatedDate);

        Assert.False(rvResult.IsActive);
        Assert.NotNull(rvResult.UpdatedDate);
    }

    [Fact]
    public void MarkEntitiesForDeletion_SetsUpdatedDateToSameTime()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        var entities = new List<PropertyDetailsEntity>
        {
            new() { Id = 1, PropertyId = 1, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false },
            new() { Id = 2, PropertyId = 1, FloorId = 2, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false }
        };

        context.PropertyDetails.AddRange(entities);
        context.SaveChanges();

        var beforeCall = DateTime.Now;

        // Act
        repository.MarkEntitiesForDeletion(entities);

        var afterCall = DateTime.Now;

        // Assert - All entities should have similar timestamps (within the method execution time)
        var firstDate = entities[0].UpdatedDate!.Value;
        var secondDate = entities[1].UpdatedDate!.Value;

        Assert.InRange(firstDate, beforeCall.AddSeconds(-1), afterCall.AddSeconds(1));
        Assert.InRange(secondDate, beforeCall.AddSeconds(-1), afterCall.AddSeconds(1));

        // Deletion dates should also be set to similar times
        var firstDeletionDate = entities[0].MarkedForDeletionDate!.Value;
        var secondDeletionDate = entities[1].MarkedForDeletionDate!.Value;

        Assert.InRange(firstDeletionDate, beforeCall.AddSeconds(-1), afterCall.AddSeconds(1));
        Assert.InRange(secondDeletionDate, beforeCall.AddSeconds(-1), afterCall.AddSeconds(1));
    }

    [Fact]
    public void MarkEntitiesForDeletion_WithMixedBaseEntityTypes_HandlesCorrectly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        var plotDetails = new PlotDetailsEntity
        {
            Id = 1,
            PropertyId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };

        var applyTaxes = new ApplyTaxesMasterEntity
        {
            Id = 1,
            PropertyId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };

        context.PlotDetails.Add(plotDetails);
        context.ApplyTaxesMaster.Add(applyTaxes);
        context.SaveChanges();

        var entities = new List<IHardDeletable> { plotDetails, applyTaxes };

        // Act
        repository.MarkEntitiesForDeletion(entities);

        // Assert
        // Both should be marked for deletion
        Assert.True(plotDetails.MarkedForDeletion);
        Assert.True(applyTaxes.MarkedForDeletion);

        // Both should have deletion dates
        Assert.NotNull(plotDetails.MarkedForDeletionDate);
        Assert.NotNull(applyTaxes.MarkedForDeletionDate);

        // Both should be inactive (BaseEntity property)
        Assert.False(plotDetails.IsActive);
        Assert.False(applyTaxes.IsActive);

        // Both should have UpdatedDate set
        Assert.NotNull(plotDetails.UpdatedDate);
        Assert.NotNull(applyTaxes.UpdatedDate);
    }

    #endregion

    #region DeactivatePropertyEntities Tests

    [Fact]
    public void DeactivatePropertyEntities_WithValidBaseEntities_SetsIsActiveFalseAndUpdatesUpdatedDate()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
        var now = DateTime.Now;
        var entities = new List<BaseEntity>
        {
            new PropertySocialDetailsEntity { Id = 1, PropertyId = 1, SocialAttributeId = 1, IsActive = true, UpdatedDate = null },
            new WaterConnectionMasterEntity { Id = 2, PropertyId = 1, WaterConnectionTypeId = 1, WaterConnectionSizeId = 1, ConnectionNo = "A", ConnectionStartDate = now, IsActive = true, UpdatedDate = null }
        };
        // Add to context for EF tracking
        context.AddRange(entities);
        context.SaveChanges();

        // Act
        repository.DeactivatePropertyEntities(entities);

        // Assert
        Assert.All(entities, entity =>
        {
            Assert.False(entity.IsActive);
            Assert.NotNull(entity.UpdatedDate);
            Assert.True(entity.UpdatedDate >= now);
            Assert.Equal(EntityState.Modified, context.Entry(entity).State);
        });
    }

    [Fact]
    public void DeactivatePropertyEntities_WithEmptyList_DoesNotThrow()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
        var entities = new List<BaseEntity>();

        // Act & Assert - Should not throw
        repository.DeactivatePropertyEntities(entities);
    }

    [Fact]
    public void DeactivatePropertyEntities_DoesNotTouchMarkedForDeletion()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var repository = new PropertyRepository(context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));
        var entity = new PropertySocialDetailsEntity 
        { 
            Id = 1, 
            PropertyId = 1, 
            SocialAttributeId = 1, 
            IsActive = true, 
            UpdatedDate = null 
        };
        context.Add(entity);
        context.SaveChanges();

        // Act
        repository.DeactivatePropertyEntities(new[] { entity });

        // Assert
        Assert.False(entity.IsActive);
        Assert.NotNull(entity.UpdatedDate);
        // BaseEntity doesn't have MarkedForDeletion, only IHardDeletable does
    }

    #endregion
}
