using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Integration;

/// <summary>
/// Integration tests for Property Deletion functionality.
/// Tests full cascade deletion logic with realistic database scenarios.
/// Uses in-memory database to simulate real database behavior without external dependencies.
/// </summary>
[Trait("Category", "Integration")]
public class PropertyDeletionIntegrationTests : IAsyncLifetime
{
    private ApplicationDbContext? _context;
    private PropertyRepository? _repository;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"PropertyDeletionIntegrationTests_{Guid.NewGuid()}")
            .EnableSensitiveDataLogging()
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new PropertyRepository(_context, Mock.Of<IFinanceYearProvider>(p => p.GetCurrentFinanceYear() == 2026));

        // Ensure database is created
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }
    }

    #region Full Cascade Deletion Tests

    /// <summary>
    /// Tests deletion of a property with ALL possible related entities.
    /// This is the most comprehensive test covering the entire deletion cascade.
    /// </summary>
    [Fact]
    public async Task DeleteProperty_WithAllRelatedEntities_CascadesCorrectly()
    {
        // Arrange: Create a property with ALL related entities
        var propertyId = 1;
        var property = await CreateCompletePropertyWithAllRelations(propertyId);

        // Verify all entities were created
        var initialPropertyDetailsCount = await _context!.PropertyDetails.CountAsync(pd => pd.PropertyId == propertyId);
        var initialRvResultsCount = await _context.RVCalculationResults.CountAsync(r => r.PropertyId == propertyId);
        var initialSection129Count = await _context.PropertyTaxCalculationSection129Results.CountAsync(r => r.PropertyId == propertyId);
        var initialRoomWiseCount = await _context.RoomWiseSubmissionDetails.CountAsync(r => r.PropertyId == propertyId);
        var initialRelatedCount = await _context.ApplyTaxesMaster.CountAsync(a => a.PropertyId == propertyId);

        Assert.True(initialPropertyDetailsCount > 0);
        Assert.True(initialRvResultsCount > 0);
        Assert.True(initialSection129Count > 0);
        Assert.True(initialRoomWiseCount > 0);
        Assert.True(initialRelatedCount > 0);

        // Act: Perform cascade deletion
        await ExecuteCascadeDeletion(propertyId);

        // Assert: Verify ALL related entities are marked for deletion
        var propertyDetails = await _context.PropertyDetails.Where(pd => pd.PropertyId == propertyId).ToListAsync();
        Assert.All(propertyDetails, pd =>
        {
            Assert.True(pd.MarkedForDeletion);
            Assert.NotNull(pd.MarkedForDeletionDate);
            Assert.False(pd.IsActive);
        });

        var rvResults = await _context.RVCalculationResults.Where(r => r.PropertyId == propertyId).ToListAsync();
        Assert.All(rvResults, rv =>
        {
            Assert.True(rv.MarkedForDeletion);
            Assert.NotNull(rv.MarkedForDeletionDate);
            Assert.False(rv.IsActive);
        });

        var section129Results = await _context.PropertyTaxCalculationSection129Results.Where(r => r.PropertyId == propertyId).ToListAsync();
        Assert.All(section129Results, s =>
        {
            Assert.True(s.MarkedForDeletion);
            Assert.NotNull(s.MarkedForDeletionDate);
            Assert.False(s.IsActive);
        });

        var roomWiseSubmissions = await _context.RoomWiseSubmissionDetails.Where(r => r.PropertyId == propertyId).ToListAsync();
        Assert.All(roomWiseSubmissions, rw =>
        {
            Assert.True(rw.MarkedForDeletion);
            Assert.NotNull(rw.MarkedForDeletionDate);
            Assert.False(rw.IsActive);
        });

        var relatedEntities = await _context.ApplyTaxesMaster.Where(a => a.PropertyId == propertyId).ToListAsync();
        Assert.All(relatedEntities, e =>
        {
            Assert.True(e.MarkedForDeletion);
            Assert.NotNull(e.MarkedForDeletionDate);
            Assert.False(e.IsActive);
        });

        // Verify PropertyAssessmentEntity (PropertyMastDetails) is marked for deletion
        var assessments = await _context.PropertyMastDetails.Where(a => a.PropertyId == propertyId).ToListAsync();
        Assert.All(assessments, a =>
        {
            Assert.True(a.MarkedForDeletion);
            Assert.NotNull(a.MarkedForDeletionDate);
            Assert.False(a.IsActive);
        });
    }

    /// <summary>
    /// Tests that child entities (PropertyDetails-level) are correctly cascaded.
    /// This includes entities that reference PropertyDetailsId.
    /// </summary>
    [Fact]
    public async Task DeleteProperty_WithPropertyDetailsChildren_CascadesRenters()
    {
        // Arrange
        var propertyId = 2;
        var property = new PropertyEntity
        {
            Id = propertyId,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context!.PropertyMast.Add(property);

        var propertyDetails = new List<PropertyDetailsEntity>
        {
            new() { Id = 1, PropertyId = propertyId, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false },
            new() { Id = 2, PropertyId = propertyId, FloorId = 2, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false }
        };
        _context.PropertyDetails.AddRange(propertyDetails);

        var renters = new List<RenterMastEntity>
        {
            new() { Id = 1, PropertyDetailsId = 1, IsActive = true, MarkedForDeletion = false },
            new() { Id = 2, PropertyDetailsId = 2, IsActive = true, MarkedForDeletion = false }
        };
        _context.RenterMast.AddRange(renters);

        await _context.SaveChangesAsync();

        // Act: Execute cascade deletion
        await ExecuteCascadeDeletion(propertyId);

        // Assert: Verify PropertyDetails and their children are marked
        var deletedPropertyDetails = await _context.PropertyDetails.Where(pd => pd.PropertyId == propertyId).ToListAsync();
        Assert.All(deletedPropertyDetails, pd => Assert.True(pd.MarkedForDeletion));

        var deletedRenters = await _context.RenterMast.Where(r => deletedPropertyDetails.Select(pd => pd.Id).Contains(r.PropertyDetailsId)).ToListAsync();
        Assert.All(deletedRenters, r => Assert.True(r.MarkedForDeletion));
    }

    /// <summary>
    /// Tests that RoomWiseMinusData (child of RoomWiseSubmissionDetails) is correctly cascaded.
    /// This tests the two-level cascade: Property -> RoomWiseSubmission -> RoomWiseMinusData
    /// </summary>
    [Fact]
    public async Task DeleteProperty_WithRoomWiseMinusData_CascadesTwoLevels()
    {
        // Arrange
        var propertyId = 3;
        var property = new PropertyEntity
        {
            Id = propertyId,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context!.PropertyMast.Add(property);

        var roomWiseSubmissions = new List<RoomWiseSubmissionDetailsEntity>
        {
            new() { Id = 1, PropertyId = propertyId, PropertyDetailsId = 1, IsActive = true, MarkedForDeletion = false },
            new() { Id = 2, PropertyId = propertyId, PropertyDetailsId = 2, IsActive = true, MarkedForDeletion = false }
        };
        _context.RoomWiseSubmissionDetails.AddRange(roomWiseSubmissions);

        var roomWiseMinusData = new List<RoomWiseMinusDataEntity>
        {
            new() { Id = 1, RoomWiseSubmissionId = 1, IsActive = true, MarkedForDeletion = false },
            new() { Id = 2, RoomWiseSubmissionId = 1, IsActive = true, MarkedForDeletion = false },
            new() { Id = 3, RoomWiseSubmissionId = 2, IsActive = true, MarkedForDeletion = false }
        };
        _context.RoomWiseMinusData.AddRange(roomWiseMinusData);

        await _context.SaveChangesAsync();

        // Act: Execute cascade deletion
        await ExecuteCascadeDeletion(propertyId);

        // Assert: Verify two-level cascade worked
        var deletedRoomWise = await _context.RoomWiseSubmissionDetails.Where(r => r.PropertyId == propertyId).ToListAsync();
        Assert.All(deletedRoomWise, r => Assert.True(r.MarkedForDeletion));

        var deletedMinusData = await _context.RoomWiseMinusData.Where(m => deletedRoomWise.Select(r => r.Id).Contains(m.RoomWiseSubmissionId)).ToListAsync();
        Assert.All(deletedMinusData, m => Assert.True(m.MarkedForDeletion));
    }

    #endregion

    #region Orphan Prevention Tests

    /// <summary>
    /// Tests that deletion does not leave orphaned records in the database.
    /// Verifies that all entities with foreign keys to the property are handled.
    /// </summary>
    [Fact]
    public async Task DeleteProperty_DoesNotLeaveOrphanedRecords()
    {
        // Arrange
        var propertyId = 4;
        await CreateCompletePropertyWithAllRelations(propertyId);

        // Count all related entities before deletion
        var propertyDetailIds = await _context!.PropertyDetails.Where(pd => pd.PropertyId == propertyId).Select(pd => pd.Id).ToListAsync();

        // Act: Execute cascade deletion
        await ExecuteCascadeDeletion(propertyId);

        // Assert: Check for orphaned records (entities with FK to deleted property but not marked for deletion)
        var orphanedRvResults = await _context.RVCalculationResults
            .Where(r => r.PropertyId == propertyId && !r.MarkedForDeletion)
            .ToListAsync();
        Assert.Empty(orphanedRvResults);

        var orphanedSection129 = await _context.PropertyTaxCalculationSection129Results
            .Where(r => r.PropertyId == propertyId && !r.MarkedForDeletion)
            .ToListAsync();
        Assert.Empty(orphanedSection129);

        var orphanedRoomWise = await _context.RoomWiseSubmissionDetails
            .Where(r => r.PropertyId == propertyId && !r.MarkedForDeletion)
            .ToListAsync();
        Assert.Empty(orphanedRoomWise);

        var orphanedRenters = await _context.RenterMast
            .Where(r => propertyDetailIds.Contains(r.PropertyDetailsId) && !r.MarkedForDeletion)
            .ToListAsync();
        Assert.Empty(orphanedRenters);

        // Verify no orphaned PropertyAssessmentEntity (PropertyMastDetails)
        var orphanedAssessments = await _context.PropertyMastDetails
            .Where(a => a.PropertyId == propertyId && !a.MarkedForDeletion)
            .ToListAsync();
        Assert.Empty(orphanedAssessments);
    }

    /// <summary>
    /// Tests that RoomWiseMinusData records don't become orphaned when their parent submissions are deleted.
    /// This is a specific test for the two-level cascade orphan scenario.
    /// </summary>
    [Fact]
    public async Task DeleteProperty_RoomWiseMinusData_NoOrphans()
    {
        // Arrange
        var propertyId = 5;
        var property = new PropertyEntity
        {
            Id = propertyId,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context!.PropertyMast.Add(property);

        var submission = new RoomWiseSubmissionDetailsEntity
        {
            Id = 1,
            PropertyId = propertyId,
            PropertyDetailsId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.RoomWiseSubmissionDetails.Add(submission);

        // Add 10 RoomWiseMinusData records
        for (int i = 1; i <= 10; i++)
        {
            _context.RoomWiseMinusData.Add(new RoomWiseMinusDataEntity
            {
                Id = i,
                RoomWiseSubmissionId = 1,
                IsActive = true,
                MarkedForDeletion = false
            });
        }

        await _context.SaveChangesAsync();

        // Act: Execute cascade deletion
        await ExecuteCascadeDeletion(propertyId);

        // Assert: No orphaned RoomWiseMinusData
        var orphanedMinusData = await _context.RoomWiseMinusData
            .Where(m => m.RoomWiseSubmissionId == 1 && !m.MarkedForDeletion)
            .ToListAsync();
        Assert.Empty(orphanedMinusData);

        // Verify all were marked for deletion
        var markedMinusData = await _context.RoomWiseMinusData
            .Where(m => m.RoomWiseSubmissionId == 1)
            .ToListAsync();
        Assert.Equal(10, markedMinusData.Count);
        Assert.All(markedMinusData, m => Assert.True(m.MarkedForDeletion));
    }

    /// <summary>
    /// Tests that PropertyAssessmentEntity (PropertyMastDetails) is correctly cascaded when property is deleted.
    /// PropertyAssessmentEntity is a critical entity that stores assessment data for the property.
    /// </summary>
    [Fact]
    public async Task DeleteProperty_WithPropertyAssessmentEntity_MarksForDeletion()
    {
        // Arrange
        var propertyId = 7;
        var property = new PropertyEntity
        {
            Id = propertyId,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context!.PropertyMast.Add(property);

        // Add PropertyAssessmentEntity (typically there's only one per property)
        var assessment = new PropertyAssessmentEntity
        {
            Id = 1,
            PropertyId = propertyId,
            OwnerTypeId = 1,
            AdharCardNo = "123456789012",
            NoOfResidentialToilets = 2,
            NoOfCommercialToilets = 1,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context.PropertyMastDetails.Add(assessment);

        await _context.SaveChangesAsync();

        // Verify the assessment was created
        var initialAssessmentCount = await _context.PropertyMastDetails.CountAsync(a => a.PropertyId == propertyId);
        Assert.Equal(1, initialAssessmentCount);

        // Act: Execute cascade deletion
        await ExecuteCascadeDeletion(propertyId);

        // Assert: Verify PropertyAssessmentEntity is marked for deletion
        var deletedAssessment = await _context.PropertyMastDetails
            .Where(a => a.PropertyId == propertyId)
            .FirstOrDefaultAsync();

        Assert.NotNull(deletedAssessment);
        Assert.True(deletedAssessment.MarkedForDeletion);
        Assert.NotNull(deletedAssessment.MarkedForDeletionDate);
        Assert.False(deletedAssessment.IsActive);
        Assert.NotNull(deletedAssessment.UpdatedDate);
    }

    /// <summary>
    /// Tests that multiple PropertyAssessmentEntity records (if they exist) are all marked for deletion.
    /// While typically there's only one, the system should handle multiple records gracefully.
    /// </summary>
    [Fact]
    public async Task DeleteProperty_WithMultiplePropertyAssessments_MarksAllForDeletion()
    {
        // Arrange
        var propertyId = 8;
        var property = new PropertyEntity
        {
            Id = propertyId,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context!.PropertyMast.Add(property);

        // Add multiple PropertyAssessmentEntity records
        for (int i = 1; i <= 3; i++)
        {
            _context.PropertyMastDetails.Add(new PropertyAssessmentEntity
            {
                Id = i,
                PropertyId = propertyId,
                IsActive = true,
                MarkedForDeletion = false
            });
        }

        await _context.SaveChangesAsync();

        // Verify assessments were created
        var initialCount = await _context.PropertyMastDetails.CountAsync(a => a.PropertyId == propertyId);
        Assert.Equal(3, initialCount);

        // Act: Execute cascade deletion
        await ExecuteCascadeDeletion(propertyId);

        // Assert: Verify all PropertyAssessmentEntity records are marked
        var deletedAssessments = await _context.PropertyMastDetails
            .Where(a => a.PropertyId == propertyId)
            .ToListAsync();

        Assert.Equal(3, deletedAssessments.Count);
        Assert.All(deletedAssessments, a =>
        {
            Assert.True(a.MarkedForDeletion);
            Assert.NotNull(a.MarkedForDeletionDate);
            Assert.False(a.IsActive);
            Assert.NotNull(a.UpdatedDate);
        });
    }

    #endregion

    #region Performance Tests

    /// <summary>
    /// Tests deletion performance with a property that has a large number of related records.
    /// This simulates a real-world scenario with hundreds of related entities.
    /// </summary>
    [Fact]
    public async Task DeleteProperty_WithLargeNumberOfRelatedRecords_CompletesInReasonableTime()
    {
        // Arrange: Create a property with 100 property details and related entities
        var propertyId = 6;
        var property = new PropertyEntity
        {
            Id = propertyId,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context!.PropertyMast.Add(property);

        // Create 100 property details
        for (int i = 1; i <= 100; i++)
        {
            var propertyDetail = new PropertyDetailsEntity
            {
                Id = i,
                PropertyId = propertyId,
                FloorId = 1,
                ConstructionTypeId = 1,
                TypeOfUseId = 1,
                IsActive = true,
                MarkedForDeletion = false
            };
            _context.PropertyDetails.Add(propertyDetail);

            // Add 2 RV results per property detail
            _context.RVCalculationResults.Add(new RVCalculationResultsEntity
            {
                Id = i * 2 - 1,
                PropertyId = propertyId,
                PropertyDetailsId = i,
                IsActive = true,
                MarkedForDeletion = false
            });
            _context.RVCalculationResults.Add(new RVCalculationResultsEntity
            {
                Id = i * 2,
                PropertyId = propertyId,
                PropertyDetailsId = i,
                IsActive = true,
                MarkedForDeletion = false
            });

        }

        await _context.SaveChangesAsync();

        // Total entities: 1 property + 100 property details + 200 RV results = 301 entities

        // Act: Execute cascade deletion and measure time
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await ExecuteCascadeDeletion(propertyId);
        stopwatch.Stop();

        // Assert: Deletion should complete within reasonable time (10 seconds for integration test with sequential queries)
        Assert.True(stopwatch.ElapsedMilliseconds < 10000, $"Deletion took {stopwatch.ElapsedMilliseconds}ms, expected < 10000ms");

        // Verify all entities were marked
        var markedPropertyDetails = await _context.PropertyDetails.CountAsync(pd => pd.PropertyId == propertyId && pd.MarkedForDeletion);
        Assert.Equal(100, markedPropertyDetails);

        var markedRvResults = await _context.RVCalculationResults.CountAsync(r => r.PropertyId == propertyId && r.MarkedForDeletion);
        Assert.Equal(200, markedRvResults);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a complete property with all possible related entities for comprehensive testing.
    /// </summary>
    private async Task<PropertyEntity> CreateCompletePropertyWithAllRelations(int propertyId)
    {
        var property = new PropertyEntity
        {
            Id = propertyId,
            WardId = 1,
            TaxZoneId = 1,
            IsActive = true,
            MarkedForDeletion = false
        };
        _context!.PropertyMast.Add(property);

        // Add PropertyDetails (3 floors)
        var propertyDetails = new List<PropertyDetailsEntity>
        {
            new() { Id = propertyId * 10 + 1, PropertyId = propertyId, FloorId = 1, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false },
            new() { Id = propertyId * 10 + 2, PropertyId = propertyId, FloorId = 2, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false },
            new() { Id = propertyId * 10 + 3, PropertyId = propertyId, FloorId = 3, ConstructionTypeId = 1, TypeOfUseId = 1, IsActive = true, MarkedForDeletion = false }
        };
        _context.PropertyDetails.AddRange(propertyDetails);

        // Add RV Results
        foreach (var pd in propertyDetails)
        {
            _context.RVCalculationResults.Add(new RVCalculationResultsEntity
            {
                Id = pd.Id,
                PropertyId = propertyId,
                PropertyDetailsId = pd.Id,
                IsActive = true,
                MarkedForDeletion = false
            });
        }

        // Add Section129 Results
        foreach (var pd in propertyDetails)
        {
            _context.PropertyTaxCalculationSection129Results.Add(new PropertyTaxCalculationSection129ResultsEntity
            {
                Id = pd.Id,
                PropertyId = propertyId,
                PropertyDetailsId = pd.Id,
                IsActive = true,
                MarkedForDeletion = false
            });
        }

        // Add Renters
        foreach (var pd in propertyDetails)
        {
            _context.RenterMast.Add(new RenterMastEntity
            {
                Id = pd.Id,
                PropertyDetailsId = pd.Id,
                IsActive = true,
                MarkedForDeletion = false
            });
        }

        // Add RoomWise Submissions
        foreach (var pd in propertyDetails)
        {
            _context.RoomWiseSubmissionDetails.Add(new RoomWiseSubmissionDetailsEntity
            {
                Id = pd.Id,
                PropertyId = propertyId,
                PropertyDetailsId = pd.Id,
                IsActive = true,
                MarkedForDeletion = false
            });

            // Add RoomWise Minus Data (child of RoomWise Submissions)
            _context.RoomWiseMinusData.Add(new RoomWiseMinusDataEntity
            {
                Id = pd.Id,
                RoomWiseSubmissionId = pd.Id,
                IsActive = true,
                MarkedForDeletion = false
            });
        }

        // Add Related Entities (property-level)
        _context.ApplyTaxesMaster.Add(new ApplyTaxesMasterEntity
        {
            Id = propertyId,
            PropertyId = propertyId,
            IsActive = true,
            MarkedForDeletion = false
        });

        _context.PlotDetails.Add(new PlotDetailsEntity
        {
            Id = propertyId,
            PropertyId = propertyId,
            IsActive = true,
            MarkedForDeletion = false
        });

        _context.PolicyTaxDetails.Add(new PolicyTaxDetailsEntity
        {
            Id = propertyId,
            PropertyId = propertyId,
            IsActive = true,
            MarkedForDeletion = false
        });

        _context.TransMast.Add(new TransMastEntity
        {
            Id = propertyId,
            PropertyId = propertyId,
            TaxId = 1,
            FinanceYearId = 1,
            IsActive = true,
            MarkedForDeletion = false
        });

        // Add PropertyAssessmentEntity (PropertyMastDetails)
        _context.PropertyMastDetails.Add(new PropertyAssessmentEntity
        {
            Id = propertyId,
            PropertyId = propertyId,
            IsActive = true,
            MarkedForDeletion = false
        });

        await _context.SaveChangesAsync();

        return property;
    }

    /// <summary>
    /// Executes cascade deletion logic (simulates PropertyService.MarkPropertyDetailsAndRelatedAsync + MarkRelatedEntitiesForDeletionAsync)
    /// Uses sequential queries to avoid DbContext concurrency issues.
    /// </summary>
    private async Task ExecuteCascadeDeletion(int propertyId)
    {
        // Step 1: Get and mark property details
        var propertyDetails = await _repository!.GetPropertyDetailsByPropertyIdAsync(propertyId);
        var propertyDetailIds = propertyDetails.Select(x => x.Id).ToList();
        _repository.MarkEntitiesForDeletion(propertyDetails);

        // Step 2: Always query PropertyId-based entities (these use PropertyId, not PropertyDetailsId)
        var rvResults = await _repository.GetRvResultsByPropertyIdAsync(propertyId);
        _repository.MarkEntitiesForDeletion(rvResults);

        var section129Results = await _repository.GetSection129ResultsByPropertyIdAsync(propertyId);
        _repository.MarkEntitiesForDeletion(section129Results);

        var roomWiseSubmissions = await _repository.GetRoomWiseSubmissionByPropertyIdAsync(propertyId);
        _repository.MarkEntitiesForDeletion(roomWiseSubmissions);

        // Handle RoomWiseMinusData (two-level cascade)
        if (roomWiseSubmissions.Count > 0)
        {
            var roomWiseSubmissionIds = roomWiseSubmissions.Select(x => x.Id).ToList();
            var roomWiseMinusData = await _repository.GetRoomWiseMinusBySubmissionIdsAsync(roomWiseSubmissionIds);
            _repository.MarkEntitiesForDeletion(roomWiseMinusData);
        }

        // Step 3: Conditionally query PropertyDetailsId-based entities (only if PropertyDetails exist)
        if (propertyDetailIds.Count > 0)
        {
            var renters = await _repository.GetRentersByPropertyDetailIdsAsync(propertyDetailIds);
            _repository.MarkEntitiesForDeletion(renters);
        }

        // Step 4: Mark related entities (mimics MarkRelatedEntitiesForDeletionAsync)
        var relatedEntities = await _repository.GetRelatedEntitiesForDeletionAsync(propertyId);
        _repository.MarkEntitiesForDeletion(relatedEntities);

        // Step 5: Save changes
        await _context!.SaveChangesAsync();
    }

    #endregion
}
