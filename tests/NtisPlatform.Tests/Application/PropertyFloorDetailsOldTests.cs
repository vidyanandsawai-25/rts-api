using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Tests for Property Floor Details Old API-related entities and DTOs,
/// including update models and associated application behavior covered in this file.
/// </summary>
public class PropertyFloorDetailsOldTests
{
    #region PropertyDetailsOldEntity Tests

    public class PropertyDetailsOldEntityTests
    {
        [Fact]
        public void PropertyDetailsOldEntity_AllProperties_GetSet_WorksCorrectly()
        {
            var now = DateTime.Now;
            var entity = new PropertyDetailsOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldFloorId = 5,
                OldSubFloorId = 12,
                OldConstructionYear = "2015",
                OldAssessmentYear = "2020",
                OldConstructionTypeId = 2,
                OldTypeOfUseId = 3,
                OldSubTypeOfUseId = 7,
                OldCarpetAreaSqMeter = 111.48,
                OldCarpetAreaSqFeet = 1200.50,
                OldBuiltupAreaSqMeter = 130.50,
                OldBuiltupAreaSqFeet = 1400.75,
                MarkedForDeletion = false,
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = now,
                UpdatedBy = 2,
                UpdatedDate = now
            };

            Assert.Equal(1, entity.Id);
            Assert.Equal(549357, entity.PropertyId);
            Assert.Equal(5, entity.OldFloorId);
            Assert.Equal(12, entity.OldSubFloorId);
            Assert.Equal("2015", entity.OldConstructionYear);
            Assert.Equal("2020", entity.OldAssessmentYear);
            Assert.Equal(2, entity.OldConstructionTypeId);
            Assert.Equal(3, entity.OldTypeOfUseId);
            Assert.Equal(7, entity.OldSubTypeOfUseId);
            Assert.Equal(111.48, entity.OldCarpetAreaSqMeter);
            Assert.Equal(1200.50, entity.OldCarpetAreaSqFeet);
            Assert.Equal(130.50, entity.OldBuiltupAreaSqMeter);
            Assert.Equal(1400.75, entity.OldBuiltupAreaSqFeet);
            Assert.False(entity.MarkedForDeletion);
            Assert.True(entity.IsActive);
        }

        [Fact]
        public void PropertyDetailsOldEntity_OptionalFields_CanBeNull()
        {
            var entity = new PropertyDetailsOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                IsActive = true,
                MarkedForDeletion = false
            };

            Assert.Null(entity.OldFloorId);
            Assert.Null(entity.OldSubFloorId);
            Assert.Null(entity.OldConstructionYear);
            Assert.Null(entity.OldAssessmentYear);
            Assert.Null(entity.OldConstructionTypeId);
            Assert.Null(entity.OldTypeOfUseId);
            Assert.Null(entity.OldSubTypeOfUseId);
            Assert.Null(entity.OldCarpetAreaSqMeter);
            Assert.Null(entity.OldCarpetAreaSqFeet);
            Assert.Null(entity.OldBuiltupAreaSqMeter);
            Assert.Null(entity.OldBuiltupAreaSqFeet);
        }

        [Fact]
        public void PropertyDetailsOldEntity_InheritsFromBaseEntity()
        {
            var entity = new PropertyDetailsOldEntity();
            Assert.IsAssignableFrom<BaseEntity>(entity);
        }

        [Fact]
        public void PropertyDetailsOldEntity_DefaultValues_SetCorrectly()
        {
            var entity = new PropertyDetailsOldEntity();

            Assert.Equal(0, entity.Id);
            Assert.Equal(0, entity.PropertyId);
            Assert.False(entity.MarkedForDeletion);
            Assert.True(entity.IsActive);
        }
    }

    #endregion

    #region PropertyDetailsOldDto Tests

    public class PropertyDetailsOldDtoTests
    {
        [Fact]
        public void PropertyDetailsOldDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new PropertyDetailsOldDto
            {
                Id = 1,
                PropertyId = 549357,
                OldFloorId = 5,
                FloorDescription = "First Floor",
                OldSubFloorId = 12,
                SubFloorDescription = "Section A",
                OldConstructionYear = "2015",
                ConstructionYearValue = 2015,
                OldAssessmentYear = "2020",
                AssessmentYearValue = 2020,
                OldConstructionTypeId = 2,
                ConstructionTypeDescription = "RCC",
                OldTypeOfUseId = 3,
                TypeOfUseDescription = "Residential",
                OldSubTypeOfUseId = 7,
                SubTypeOfUseDescription = "Apartment",
                OldCarpetAreaSqMeter = 111.48,
                OldCarpetAreaSqFeet = 1200.50,
                OldBuiltupAreaSqMeter = 130.50,
                OldBuiltupAreaSqFeet = 1400.75,
                MarkedForDeletion = false
            };

            Assert.Equal(1, dto.Id);
            Assert.Equal(549357, dto.PropertyId);
            Assert.Equal(5, dto.OldFloorId);
            Assert.Equal("First Floor", dto.FloorDescription);
            Assert.Equal(12, dto.OldSubFloorId);
            Assert.Equal("Section A", dto.SubFloorDescription);
            Assert.Equal("2015", dto.OldConstructionYear);
            Assert.Equal(2015, dto.ConstructionYearValue);
            Assert.Equal("2020", dto.OldAssessmentYear);
            Assert.Equal(2020, dto.AssessmentYearValue);
            Assert.Equal(2, dto.OldConstructionTypeId);
            Assert.Equal("RCC", dto.ConstructionTypeDescription);
            Assert.Equal(3, dto.OldTypeOfUseId);
            Assert.Equal("Residential", dto.TypeOfUseDescription);
            Assert.Equal(7, dto.OldSubTypeOfUseId);
            Assert.Equal("Apartment", dto.SubTypeOfUseDescription);
            Assert.Equal(111.48, dto.OldCarpetAreaSqMeter);
            Assert.Equal(1200.50, dto.OldCarpetAreaSqFeet);
            Assert.Equal(130.50, dto.OldBuiltupAreaSqMeter);
            Assert.Equal(1400.75, dto.OldBuiltupAreaSqFeet);
            Assert.False(dto.MarkedForDeletion);
        }

        [Fact]
        public void PropertyDetailsOldDto_OptionalProperties_CanBeNull()
        {
            var dto = new PropertyDetailsOldDto
            {
                Id = 1,
                PropertyId = 549357
            };

            Assert.Null(dto.OldFloorId);
            Assert.Null(dto.FloorDescription);
            Assert.Null(dto.OldSubFloorId);
            Assert.Null(dto.SubFloorDescription);
            Assert.Null(dto.OldConstructionYear);
            Assert.Null(dto.ConstructionYearValue);
            Assert.Null(dto.OldAssessmentYear);
            Assert.Null(dto.AssessmentYearValue);
            Assert.Null(dto.OldConstructionTypeId);
            Assert.Null(dto.ConstructionTypeDescription);
            Assert.Null(dto.OldTypeOfUseId);
            Assert.Null(dto.TypeOfUseDescription);
            Assert.Null(dto.OldSubTypeOfUseId);
            Assert.Null(dto.SubTypeOfUseDescription);
            Assert.Null(dto.OldCarpetAreaSqMeter);
            Assert.Null(dto.OldCarpetAreaSqFeet);
            Assert.Null(dto.OldBuiltupAreaSqMeter);
            Assert.Null(dto.OldBuiltupAreaSqFeet);
        }
    }

    #endregion

    #region UpdatePropertyDetailsOldDto Tests

    public class UpdatePropertyDetailsOldDtoTests
    {
        [Fact]
        public void UpdatePropertyDetailsOldDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new UpdatePropertyDetailsOldDto
            {
                Id = 1,
                OldFloorId = 5,
                OldSubFloorId = 12,
                OldConstructionYear = "2015",
                OldAssessmentYear = "2020",
                OldConstructionTypeId = 2,
                OldTypeOfUseId = 3,
                OldSubTypeOfUseId = 7,
                OldCarpetAreaSqMeter = 111.48,
                OldCarpetAreaSqFeet = 1200.50,
                OldBuiltupAreaSqMeter = 130.50,
                OldBuiltupAreaSqFeet = 1400.75
            };

            Assert.Equal(1, dto.Id);
            Assert.Equal(5, dto.OldFloorId);
            Assert.Equal(12, dto.OldSubFloorId);
            Assert.Equal("2015", dto.OldConstructionYear);
            Assert.Equal("2020", dto.OldAssessmentYear);
            Assert.Equal(2, dto.OldConstructionTypeId);
            Assert.Equal(3, dto.OldTypeOfUseId);
            Assert.Equal(7, dto.OldSubTypeOfUseId);
            Assert.Equal(111.48, dto.OldCarpetAreaSqMeter);
            Assert.Equal(1200.50, dto.OldCarpetAreaSqFeet);
            Assert.Equal(130.50, dto.OldBuiltupAreaSqMeter);
            Assert.Equal(1400.75, dto.OldBuiltupAreaSqFeet);
        }

        [Fact]
        public void UpdatePropertyDetailsOldDto_NullId_ForInsert()
        {
            var dto = new UpdatePropertyDetailsOldDto
            {
                Id = null,  // null = INSERT
                OldFloorId = 5,
                OldConstructionYear = "2020"
            };

            Assert.Null(dto.Id);
            Assert.Equal(5, dto.OldFloorId);
        }

        [Fact]
        public void UpdatePropertyDetailsOldDto_WithId_ForUpdate()
        {
            var dto = new UpdatePropertyDetailsOldDto
            {
                Id = 15,  // existing Id = UPDATE
                OldFloorId = 5,
                OldConstructionYear = "2020"
            };

            Assert.Equal(15, dto.Id);
            Assert.Equal(5, dto.OldFloorId);
        }

        [Fact]
        public void UpdatePropertyDetailsOldDto_AllOptionalFields_CanBeNull()
        {
            var dto = new UpdatePropertyDetailsOldDto
            {
                Id = 1
            };

            Assert.Null(dto.OldFloorId);
            Assert.Null(dto.OldSubFloorId);
            Assert.Null(dto.OldConstructionYear);
            Assert.Null(dto.OldAssessmentYear);
            Assert.Null(dto.OldConstructionTypeId);
            Assert.Null(dto.OldTypeOfUseId);
            Assert.Null(dto.OldSubTypeOfUseId);
            Assert.Null(dto.OldCarpetAreaSqMeter);
            Assert.Null(dto.OldCarpetAreaSqFeet);
            Assert.Null(dto.OldBuiltupAreaSqMeter);
            Assert.Null(dto.OldBuiltupAreaSqFeet);
        }
    }

    #endregion

    #region UpdatePropertyDetailsOldListDto Tests

    public class UpdatePropertyDetailsOldListDtoTests
    {
        [Fact]
        public void UpdatePropertyDetailsOldListDto_FloorDetails_InitializesAsEmptyList()
        {
            var dto = new UpdatePropertyDetailsOldListDto();
            Assert.NotNull(dto.FloorDetails);
            Assert.Empty(dto.FloorDetails);
        }

        [Fact]
        public void UpdatePropertyDetailsOldListDto_CanAddMultipleFloorDetails()
        {
            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new() { Id = 1, OldFloorId = 5, OldConstructionYear = "2020" },
                    new() { Id = null, OldFloorId = 6, OldConstructionYear = "2021" }
                }
            };

            Assert.Equal(2, dto.FloorDetails.Count);
            Assert.Equal(1, dto.FloorDetails[0].Id);
            Assert.Null(dto.FloorDetails[1].Id);
        }

        [Fact]
        public void UpdatePropertyDetailsOldListDto_FloorDetailsRequired_ValidationAttribute()
        {
            var dto = new UpdatePropertyDetailsOldListDto();
            var requiredAttr = typeof(UpdatePropertyDetailsOldListDto)
                .GetProperty(nameof(UpdatePropertyDetailsOldListDto.FloorDetails))
                ?.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), false);

            Assert.NotNull(requiredAttr);
            Assert.NotEmpty(requiredAttr);

            // Verify FloorDetails cannot be null at runtime (protected by Required attribute)
            Assert.NotNull(dto.FloorDetails);
        }
    }

    #endregion

    #region PropertyDetailsOldListDto Tests

    public class PropertyDetailsOldListDtoTests
    {
        [Fact]
        public void PropertyDetailsOldListDto_FloorDetails_InitializesAsEmptyList()
        {
            var dto = new PropertyDetailsOldListDto();
            Assert.NotNull(dto.FloorDetails);
            Assert.Empty(dto.FloorDetails);
        }

        [Fact]
        public void PropertyDetailsOldListDto_PropertyId_GetSet_WorksCorrectly()
        {
            var dto = new PropertyDetailsOldListDto
            {
                PropertyId = 549357,
                FloorDetails = new List<PropertyDetailsOldDto>
                {
                    new() { Id = 1, PropertyId = 549357, OldFloorId = 5 }
                }
            };

            Assert.Equal(549357, dto.PropertyId);
            Assert.Single(dto.FloorDetails);
        }
    }

    #endregion

    #region Repository Tests - GetFloorDetailsOldAsync

    public class GetFloorDetailsOldTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task GetFloorDetailsOldAsync_PropertyDoesNotExist_ReturnsNull()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            var result = await repository.GetFloorDetailsOldAsync(999999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetFloorDetailsOldAsync_PropertyExistsButNoFloorDetails_ReturnsEmptyList()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity
            {
                Id = 549357,
                PropertyNo = "P001",
                IsActive = true,
                MarkedForDeletion = false
            });
            await context.SaveChangesAsync();

            var result = await repository.GetFloorDetailsOldAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            Assert.Empty(result.FloorDetails);
        }

        [Fact]
        public async Task GetFloorDetailsOldAsync_PropertyWithFloorDetails_ReturnsWithJoinedData()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            // Setup property
            context.PropertyMast.Add(new PropertyEntity
            {
                Id = 549357,
                PropertyNo = "P001",
                IsActive = true,
                MarkedForDeletion = false
            });

            // Setup master data
            var floor = new FloorEntity
            {
                Id = 5,
                FloorCode = "1",
                Description = "First Floor",
                IsActive = true
            };
            context.FloorEntity.Add(floor);

            var subFloor = new SubFloorEntity
            {
                Id = 12,
                SubFloorCode = "A",
                Description = "Section A",
                IsActive = true
            };
            context.SubFloorEntity.Add(subFloor);

            var constructionType = new ConstructionTypeEntity
            {
                Id = 2,
                ConstructionCode = "RCC",
                Description = "RCC Construction",
                IsActive = true
            };
            context.ConstructionTypeEntity.Add(constructionType);

            var typeOfUse = new TypeOfUseEntity { Id = 3, TypeOfUseCode = "R", Description = "Residential", Type = "R", TypeOfUseGroupId = 1, IsActive = true };
            context.TypeOfUse.Add(typeOfUse);

            var subTypeOfUse = new SubTypeOfUseEntity { Id = 7, Description = "Apartment", TypeOfUseId = 3, IsActive = true };
            context.SubTypeOfUse.Add(subTypeOfUse);

            // Setup floor details old
            context.PropertyDetailsOld.Add(new PropertyDetailsOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldFloorId = 5,
                OldSubFloorId = 12,
                OldConstructionYear = "2015",
                OldAssessmentYear = "2020",
                OldConstructionTypeId = 2,
                OldTypeOfUseId = 3,
                OldSubTypeOfUseId = 7,
                OldCarpetAreaSqMeter = 111.48,
                OldCarpetAreaSqFeet = 1200.50,
                OldBuiltupAreaSqMeter = 130.50,
                OldBuiltupAreaSqFeet = 1400.75,
                IsActive = true,
                MarkedForDeletion = false
            });

            await context.SaveChangesAsync();

            var result = await repository.GetFloorDetailsOldAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            Assert.Single(result.FloorDetails);

            var detail = result.FloorDetails[0];
            Assert.Equal(1, detail.Id);
            Assert.Equal(5, detail.OldFloorId);
            Assert.Equal("First Floor", detail.FloorDescription);
            Assert.Equal(12, detail.OldSubFloorId);
            Assert.Equal("Section A", detail.SubFloorDescription);
            Assert.Equal("2015", detail.OldConstructionYear);
            Assert.Equal(2015, detail.ConstructionYearValue);
            Assert.Equal("2020", detail.OldAssessmentYear);
            Assert.Equal(2020, detail.AssessmentYearValue);
            Assert.Equal(2, detail.OldConstructionTypeId);
            Assert.Equal("RCC Construction", detail.ConstructionTypeDescription);
            Assert.Equal(3, detail.OldTypeOfUseId);
            Assert.Equal("Residential", detail.TypeOfUseDescription);
            Assert.Equal(7, detail.OldSubTypeOfUseId);
            Assert.Equal("Apartment", detail.SubTypeOfUseDescription);
            Assert.Equal(111.48, detail.OldCarpetAreaSqMeter);
            Assert.Equal(1200.50, detail.OldCarpetAreaSqFeet);
            Assert.Equal(130.50, detail.OldBuiltupAreaSqMeter);
            Assert.Equal(1400.75, detail.OldBuiltupAreaSqFeet);
        }

        [Fact]
        public async Task GetFloorDetailsOldAsync_MultipleFloorDetails_ReturnsAllOrdered()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity
            {
                Id = 549357,
                PropertyNo = "P001",
                IsActive = true,
                MarkedForDeletion = false
            });

            // Add floor master data
            context.FloorEntity.Add(new FloorEntity { Id = 5, FloorCode = "1", Description = "First Floor", IsActive = true });
            context.FloorEntity.Add(new FloorEntity { Id = 6, FloorCode = "2", Description = "Second Floor", IsActive = true });

            // Add multiple floor details - inserted out of order
            context.PropertyDetailsOld.Add(new PropertyDetailsOldEntity
            {
                Id = 3,
                PropertyId = 549357,
                OldFloorId = 6,
                OldConstructionYear = "2021",
                IsActive = true,
                MarkedForDeletion = false
            });

            context.PropertyDetailsOld.Add(new PropertyDetailsOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldFloorId = 5,
                OldConstructionYear = "2020",
                IsActive = true,
                MarkedForDeletion = false
            });

            await context.SaveChangesAsync();

            var result = await repository.GetFloorDetailsOldAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(2, result.FloorDetails.Count);
            // Verify ordered by Id
            Assert.Equal(1, result.FloorDetails[0].Id);
            Assert.Equal(3, result.FloorDetails[1].Id);
        }

        [Fact]
        public async Task GetFloorDetailsOldAsync_InactiveFloorDetails_NotReturned()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity
            {
                Id = 549357,
                PropertyNo = "P001",
                IsActive = true,
                MarkedForDeletion = false
            });

            // Active record
            context.PropertyDetailsOld.Add(new PropertyDetailsOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldFloorId = 5,
                IsActive = true,
                MarkedForDeletion = false
            });

            // Inactive record
            context.PropertyDetailsOld.Add(new PropertyDetailsOldEntity
            {
                Id = 2,
                PropertyId = 549357,
                OldFloorId = 6,
                IsActive = false,
                MarkedForDeletion = false
            });

            await context.SaveChangesAsync();

            var result = await repository.GetFloorDetailsOldAsync(549357);

            Assert.NotNull(result);
            Assert.Single(result.FloorDetails);
            Assert.Equal(1, result.FloorDetails[0].Id);
        }

        [Fact]
        public async Task GetFloorDetailsOldAsync_MarkedForDeletion_NotReturned()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity
            {
                Id = 549357,
                PropertyNo = "P001",
                IsActive = true,
                MarkedForDeletion = false
            });

            // Not marked for deletion
            context.PropertyDetailsOld.Add(new PropertyDetailsOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldFloorId = 5,
                IsActive = true,
                MarkedForDeletion = false
            });

            // Marked for deletion
            context.PropertyDetailsOld.Add(new PropertyDetailsOldEntity
            {
                Id = 2,
                PropertyId = 549357,
                OldFloorId = 6,
                IsActive = true,
                MarkedForDeletion = true
            });

            await context.SaveChangesAsync();

            var result = await repository.GetFloorDetailsOldAsync(549357);

            Assert.NotNull(result);
            Assert.Single(result.FloorDetails);
            Assert.Equal(1, result.FloorDetails[0].Id);
        }

        [Fact]
        public async Task GetFloorDetailsOldAsync_InvalidConstructionYear_ParsesAsNull()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity
            {
                Id = 549357,
                PropertyNo = "P001",
                IsActive = true,
                MarkedForDeletion = false
            });

            context.PropertyDetailsOld.Add(new PropertyDetailsOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldConstructionYear = "ABCD",  // Invalid year
                IsActive = true,
                MarkedForDeletion = false
            });

            await context.SaveChangesAsync();

            var result = await repository.GetFloorDetailsOldAsync(549357);

            Assert.NotNull(result);
            Assert.Single(result.FloorDetails);
            Assert.Equal("ABCD", result.FloorDetails[0].OldConstructionYear);
            Assert.Null(result.FloorDetails[0].ConstructionYearValue);  // Failed to parse
        }

        [Fact]
        public async Task GetFloorDetailsOldAsync_InactiveMasterData_JoinsReturnNull()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity
            {
                Id = 549357,
                PropertyNo = "P001",
                IsActive = true,
                MarkedForDeletion = false
            });

            // Inactive floor
            context.FloorEntity.Add(new FloorEntity
            {
                Id = 5,
                FloorCode = "1",
                Description = "First Floor",
                IsActive = false  // Inactive
            });

            context.PropertyDetailsOld.Add(new PropertyDetailsOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldFloorId = 5,
                IsActive = true,
                MarkedForDeletion = false
            });

            await context.SaveChangesAsync();

            var result = await repository.GetFloorDetailsOldAsync(549357);

            Assert.NotNull(result);
            Assert.Single(result.FloorDetails);
            Assert.Equal(5, result.FloorDetails[0].OldFloorId);
            Assert.Null(result.FloorDetails[0].FloorDescription);  // Inactive floor not joined
        }
    }

    #endregion

    #region Repository Tests - UpdateFloorDetailsOldAsync

    public class UpdateFloorDetailsOldTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_PropertyDoesNotExist_ReturnsNull()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new() { Id = null, OldFloorId = 5, OldConstructionYear = "2020" }
                }
            };

            var result = await repository.UpdateFloorDetailsOldAsync(999999, dto);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_InsertNewRecord_Success()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            // Setup property
            context.PropertyMast.Add(new PropertyEntity
            {
                Id = 549357,
                PropertyNo = "P001",
                IsActive = true,
                MarkedForDeletion = false
            });

            // Setup master data
            context.FloorEntity.Add(new FloorEntity { Id = 5, FloorCode = "1", Description = "First Floor", IsActive = true });
            context.ConstructionTypeEntity.Add(new ConstructionTypeEntity { Id = 2, ConstructionCode = "RCC", Description = "RCC", IsActive = true });
            context.TypeOfUse.Add(new TypeOfUseEntity { Id = 3, TypeOfUseCode = "R", Description = "Residential", Type = "R", TypeOfUseGroupId = 1, IsActive = true });

            await context.SaveChangesAsync();

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new()
                    {
                        Id = null,  // INSERT
                        OldFloorId = 5,
                        OldConstructionYear = "2020",
                        OldConstructionTypeId = 2,
                        OldTypeOfUseId = 3,
                        OldCarpetAreaSqMeter = 100.0
                    }
                }
            };

            var result = await repository.UpdateFloorDetailsOldAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Single(result.FloorDetails);
            var detail = result.FloorDetails[0];
            Assert.NotEqual(0, detail.Id);  // Id was auto-generated
            Assert.Equal(5, detail.OldFloorId);
            Assert.Equal("First Floor", detail.FloorDescription);
            Assert.Equal("2020", detail.OldConstructionYear);
            Assert.Equal(2020, detail.ConstructionYearValue);
            Assert.Equal(2, detail.OldConstructionTypeId);
            Assert.Equal(3, detail.OldTypeOfUseId);
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_UpdateExistingRecord_Success()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            // Setup
            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            context.FloorEntity.Add(new FloorEntity { Id = 5, FloorCode = "1", Description = "First Floor", IsActive = true });
            context.FloorEntity.Add(new FloorEntity { Id = 6, FloorCode = "2", Description = "Second Floor", IsActive = true });

            // Existing record
            context.PropertyDetailsOld.Add(new PropertyDetailsOldEntity
            {
                Id = 10,
                PropertyId = 549357,
                OldFloorId = 5,
                OldConstructionYear = "2019",
                OldCarpetAreaSqMeter = 90.0,
                IsActive = true,
                MarkedForDeletion = false
            });

            await context.SaveChangesAsync();

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new()
                    {
                        Id = 10,  // UPDATE existing
                        OldFloorId = 6,  // Changed
                        OldConstructionYear = "2020",  // Changed
                        OldCarpetAreaSqMeter = 100.0  // Changed
                    }
                }
            };

            var result = await repository.UpdateFloorDetailsOldAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Single(result.FloorDetails);
            var detail = result.FloorDetails[0];
            Assert.Equal(10, detail.Id);
            Assert.Equal(6, detail.OldFloorId);  // Updated
            Assert.Equal("Second Floor", detail.FloorDescription);
            Assert.Equal("2020", detail.OldConstructionYear);  // Updated
            Assert.Equal(100.0, detail.OldCarpetAreaSqMeter);  // Updated
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_MixedInsertAndUpdate_Success()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            context.FloorEntity.Add(new FloorEntity { Id = 5, FloorCode = "1", Description = "First Floor", IsActive = true });
            context.FloorEntity.Add(new FloorEntity { Id = 6, FloorCode = "2", Description = "Second Floor", IsActive = true });

            // Existing record
            context.PropertyDetailsOld.Add(new PropertyDetailsOldEntity
            {
                Id = 10,
                PropertyId = 549357,
                OldFloorId = 5,
                OldConstructionYear = "2019",
                IsActive = true,
                MarkedForDeletion = false
            });

            await context.SaveChangesAsync();

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new() { Id = 10, OldFloorId = 5, OldConstructionYear = "2020" },  // UPDATE
                    new() { Id = null, OldFloorId = 6, OldConstructionYear = "2021" }  // INSERT
                }
            };

            var result = await repository.UpdateFloorDetailsOldAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal(2, result.FloorDetails.Count);

            var updated = result.FloorDetails.FirstOrDefault(d => d.Id == 10);
            Assert.NotNull(updated);
            Assert.Equal("2020", updated.OldConstructionYear);

            var inserted = result.FloorDetails.FirstOrDefault(d => d.Id != 10);
            Assert.NotNull(inserted);
            Assert.Equal(6, inserted.OldFloorId);
            Assert.Equal("2021", inserted.OldConstructionYear);
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_SoftDeleteRecordsNotInList_Success()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            context.FloorEntity.Add(new FloorEntity { Id = 5, FloorCode = "1", Description = "First Floor", IsActive = true });
            context.FloorEntity.Add(new FloorEntity { Id = 6, FloorCode = "2", Description = "Second Floor", IsActive = true });

            // Existing records
            context.PropertyDetailsOld.Add(new PropertyDetailsOldEntity
            {
                Id = 10,
                PropertyId = 549357,
                OldFloorId = 5,
                IsActive = true,
                MarkedForDeletion = false
            });

            context.PropertyDetailsOld.Add(new PropertyDetailsOldEntity
            {
                Id = 11,
                PropertyId = 549357,
                OldFloorId = 6,
                IsActive = true,
                MarkedForDeletion = false
            });

            await context.SaveChangesAsync();

            // Only send record Id=10, record Id=11 should be soft deleted
            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new() { Id = 10, OldFloorId = 5, OldConstructionYear = "2020" }
                }
            };

            var result = await repository.UpdateFloorDetailsOldAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Single(result.FloorDetails);  // Only active record returned
            Assert.Equal(10, result.FloorDetails[0].Id);

            // Verify record 11 is marked for deletion
            var deletedRecord = await context.PropertyDetailsOld.FirstOrDefaultAsync(pd => pd.Id == 11);
            Assert.NotNull(deletedRecord);
            Assert.True(deletedRecord.MarkedForDeletion);
            Assert.NotNull(deletedRecord.MarkedForDeletionDate);
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_InvalidFloorId_ThrowsException()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            await context.SaveChangesAsync();

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new() { Id = null, OldFloorId = 999, OldConstructionYear = "2020" }  // Invalid Floor Id
                }
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repository.UpdateFloorDetailsOldAsync(549357, dto));

            Assert.Contains("Invalid or inactive Floor ID", exception.Message);
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_InvalidConstructionTypeId_ThrowsException()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            context.FloorEntity.Add(new FloorEntity { Id = 5, FloorCode = "1", Description = "First Floor", IsActive = true });
            await context.SaveChangesAsync();

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new() { Id = null, OldFloorId = 5, OldConstructionTypeId = 999 }  // Invalid ConstructionType Id
                }
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repository.UpdateFloorDetailsOldAsync(549357, dto));

            Assert.Contains("Invalid or inactive ConstructionType ID", exception.Message);
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_InvalidTypeOfUseId_ThrowsException()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            context.FloorEntity.Add(new FloorEntity { Id = 5, FloorCode = "1", Description = "First Floor", IsActive = true });
            await context.SaveChangesAsync();

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new() { Id = null, OldFloorId = 5, OldTypeOfUseId = 999 }  // Invalid TypeOfUse Id
                }
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repository.UpdateFloorDetailsOldAsync(549357, dto));

            Assert.Contains("Invalid or inactive TypeOfUse ID", exception.Message);
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_InvalidSubFloorId_ThrowsException()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            context.FloorEntity.Add(new FloorEntity { Id = 5, FloorCode = "1", Description = "First Floor", IsActive = true });
            await context.SaveChangesAsync();

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new() { Id = null, OldFloorId = 5, OldSubFloorId = 999 }  // Invalid SubFloor Id
                }
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repository.UpdateFloorDetailsOldAsync(549357, dto));

            Assert.Contains("Invalid or inactive SubFloor ID", exception.Message);
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_InvalidSubTypeOfUseId_ThrowsException()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            context.FloorEntity.Add(new FloorEntity { Id = 5, FloorCode = "1", Description = "First Floor", IsActive = true });
            await context.SaveChangesAsync();

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new() { Id = null, OldFloorId = 5, OldSubTypeOfUseId = 999 }  // Invalid SubTypeOfUse Id
                }
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repository.UpdateFloorDetailsOldAsync(549357, dto));

            Assert.Contains("Invalid or inactive SubTypeOfUse ID", exception.Message);
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_UpdateNonExistentRecord_ThrowsException()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            context.FloorEntity.Add(new FloorEntity { Id = 5, FloorCode = "1", Description = "First Floor", IsActive = true });
            await context.SaveChangesAsync();

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new() { Id = 999, OldFloorId = 5 }  // Non-existent Id
                }
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repository.UpdateFloorDetailsOldAsync(549357, dto));

            Assert.Contains("PropertyDetailsOld record with ID 999 not found", exception.Message);
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_EmptyList_SoftDeletesAllRecords()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });

            context.PropertyDetailsOld.Add(new PropertyDetailsOldEntity
            {
                Id = 10,
                PropertyId = 549357,
                OldFloorId = 5,
                IsActive = true,
                MarkedForDeletion = false
            });

            await context.SaveChangesAsync();

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>()  // Empty list
            };

            var result = await repository.UpdateFloorDetailsOldAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Empty(result.FloorDetails);

            var deletedRecord = await context.PropertyDetailsOld.FirstOrDefaultAsync(pd => pd.Id == 10);
            Assert.NotNull(deletedRecord);
            Assert.True(deletedRecord.MarkedForDeletion);
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_InactiveFloorMaster_ThrowsException()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            context.FloorEntity.Add(new FloorEntity { Id = 5, FloorCode = "1", Description = "First Floor", IsActive = false });  // Inactive
            await context.SaveChangesAsync();

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new() { Id = null, OldFloorId = 5 }
                }
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repository.UpdateFloorDetailsOldAsync(549357, dto));

            Assert.Contains("Invalid or inactive Floor ID", exception.Message);
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_AllFieldsUpdate_Success()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            context.FloorEntity.Add(new FloorEntity { Id = 5, FloorCode = "1", Description = "First Floor", IsActive = true });
            context.SubFloorEntity.Add(new SubFloorEntity { Id = 12, SubFloorCode = "A", Description = "Section A", IsActive = true });
            context.ConstructionTypeEntity.Add(new ConstructionTypeEntity { Id = 2, ConstructionCode = "RCC", Description = "RCC", IsActive = true });
            context.TypeOfUse.Add(new TypeOfUseEntity { Id = 3, TypeOfUseCode = "R", Description = "Residential", Type = "R", TypeOfUseGroupId = 1, IsActive = true });
            context.SubTypeOfUse.Add(new SubTypeOfUseEntity { Id = 7, Description = "Apartment", TypeOfUseId = 3, IsActive = true });

            context.PropertyDetailsOld.Add(new PropertyDetailsOldEntity
            {
                Id = 10,
                PropertyId = 549357,
                OldFloorId = 5,
                IsActive = true,
                MarkedForDeletion = false
            });

            await context.SaveChangesAsync();

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new()
                    {
                        Id = 10,
                        OldFloorId = 5,
                        OldSubFloorId = 12,
                        OldConstructionYear = "2020",
                        OldAssessmentYear = "2021",
                        OldConstructionTypeId = 2,
                        OldTypeOfUseId = 3,
                        OldSubTypeOfUseId = 7,
                        OldCarpetAreaSqMeter = 100.0,
                        OldCarpetAreaSqFeet = 1075.0,
                        OldBuiltupAreaSqMeter = 120.0,
                        OldBuiltupAreaSqFeet = 1290.0
                    }
                }
            };

            var result = await repository.UpdateFloorDetailsOldAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Single(result.FloorDetails);
            var detail = result.FloorDetails[0];
            Assert.Equal(10, detail.Id);
            Assert.Equal(5, detail.OldFloorId);
            Assert.Equal(12, detail.OldSubFloorId);
            Assert.Equal("2020", detail.OldConstructionYear);
            Assert.Equal("2021", detail.OldAssessmentYear);
            Assert.Equal(2, detail.OldConstructionTypeId);
            Assert.Equal(3, detail.OldTypeOfUseId);
            Assert.Equal(7, detail.OldSubTypeOfUseId);
            Assert.Equal(100.0, detail.OldCarpetAreaSqMeter);
            Assert.Equal(1075.0, detail.OldCarpetAreaSqFeet);
            Assert.Equal(120.0, detail.OldBuiltupAreaSqMeter);
            Assert.Equal(1290.0, detail.OldBuiltupAreaSqFeet);
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_MultipleInvalidIds_ThrowsExceptionWithAllIds()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            await context.SaveChangesAsync();

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new() { Id = null, OldFloorId = 888 },
                    new() { Id = null, OldFloorId = 999 }
                }
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repository.UpdateFloorDetailsOldAsync(549357, dto));

            Assert.Contains("Invalid or inactive Floor ID", exception.Message);
            Assert.Contains("888", exception.Message);
            Assert.Contains("999", exception.Message);
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_NullOptionalFields_Success()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            await context.SaveChangesAsync();

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new()
                    {
                        Id = null,
                        OldFloorId = null,
                        OldSubFloorId = null,
                        OldConstructionYear = null,
                        OldAssessmentYear = null,
                        OldConstructionTypeId = null,
                        OldTypeOfUseId = null,
                        OldSubTypeOfUseId = null,
                        OldCarpetAreaSqMeter = null,
                        OldCarpetAreaSqFeet = null,
                        OldBuiltupAreaSqMeter = null,
                        OldBuiltupAreaSqFeet = null
                    }
                }
            };

            var result = await repository.UpdateFloorDetailsOldAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Single(result.FloorDetails);
            var detail = result.FloorDetails[0];
            Assert.Null(detail.OldFloorId);
            Assert.Null(detail.OldSubFloorId);
            Assert.Null(detail.OldConstructionYear);
            Assert.Null(detail.OldAssessmentYear);
        }
    }

    #endregion

    #region Integration Tests - End to End

    public class FloorDetailsOldIntegrationTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task FullCRUD_Workflow_Success()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            // Setup
            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            context.FloorEntity.Add(new FloorEntity { Id = 5, FloorCode = "1", Description = "First Floor", IsActive = true });
            context.FloorEntity.Add(new FloorEntity { Id = 6, FloorCode = "2", Description = "Second Floor", IsActive = true });
            context.ConstructionTypeEntity.Add(new ConstructionTypeEntity { Id = 2, ConstructionCode = "RCC", Description = "RCC", IsActive = true });
            context.TypeOfUse.Add(new TypeOfUseEntity { Id = 3, TypeOfUseCode = "R", Description = "Residential", Type = "R", TypeOfUseGroupId = 1, IsActive = true });
            await context.SaveChangesAsync();

            // Step 1: GET - Should be empty
            var getResult1 = await repository.GetFloorDetailsOldAsync(549357);
            Assert.NotNull(getResult1);
            Assert.Empty(getResult1.FloorDetails);

            // Step 2: INSERT first record
            var insertDto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new() { Id = null, OldFloorId = 5, OldConstructionYear = "2020", OldConstructionTypeId = 2, OldTypeOfUseId = 3 }
                }
            };
            var insertResult = await repository.UpdateFloorDetailsOldAsync(549357, insertDto);
            Assert.NotNull(insertResult);
            Assert.Single(insertResult.FloorDetails);
            var insertedId = insertResult.FloorDetails[0].Id;

            // Step 3: GET - Should have 1 record
            var getResult2 = await repository.GetFloorDetailsOldAsync(549357);
            Assert.NotNull(getResult2);
            Assert.Single(getResult2.FloorDetails);
            Assert.Equal("2020", getResult2.FloorDetails[0].OldConstructionYear);

            // Step 4: UPDATE existing and INSERT new
            var updateDto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new() { Id = insertedId, OldFloorId = 6, OldConstructionYear = "2021" },  // UPDATE
                    new() { Id = null, OldFloorId = 5, OldConstructionYear = "2022" }  // INSERT
                }
            };
            var updateResult = await repository.UpdateFloorDetailsOldAsync(549357, updateDto);
            Assert.NotNull(updateResult);
            Assert.Equal(2, updateResult.FloorDetails.Count);

            // Step 5: GET - Should have 2 records
            var getResult3 = await repository.GetFloorDetailsOldAsync(549357);
            Assert.NotNull(getResult3);
            Assert.Equal(2, getResult3.FloorDetails.Count);

            // Step 6: DELETE one record (by not including it)
            var deleteDto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new() { Id = insertedId, OldFloorId = 6, OldConstructionYear = "2021" }  // Keep only this one
                }
            };
            var deleteResult = await repository.UpdateFloorDetailsOldAsync(549357, deleteDto);
            Assert.NotNull(deleteResult);
            Assert.Single(deleteResult.FloorDetails);

            // Step 7: GET - Should have 1 active record
            var getResult4 = await repository.GetFloorDetailsOldAsync(549357);
            Assert.NotNull(getResult4);
            Assert.Single(getResult4.FloorDetails);
            Assert.Equal(insertedId, getResult4.FloorDetails[0].Id);
        }

        [Fact]
        public async Task BulkValidation_MultipleRecordsWithMixedValidAndInvalidIds_ThrowsWithAllInvalidIds()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            context.FloorEntity.Add(new FloorEntity { Id = 5, FloorCode = "1", Description = "First Floor", IsActive = true });
            await context.SaveChangesAsync();

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new() { Id = null, OldFloorId = 5, OldConstructionTypeId = 999 },  // Invalid ConstructionTypeId
                    new() { Id = null, OldFloorId = 5, OldConstructionTypeId = 888 }   // Invalid ConstructionTypeId
                }
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repository.UpdateFloorDetailsOldAsync(549357, dto));

            Assert.Contains("Invalid or inactive ConstructionType ID", exception.Message);
            Assert.Contains("999", exception.Message);
            Assert.Contains("888", exception.Message);
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_ValidAssessmentYearParsing_Success()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            await context.SaveChangesAsync();

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new() { Id = null, OldAssessmentYear = "2022" }
                }
            };

            var result = await repository.UpdateFloorDetailsOldAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Single(result.FloorDetails);
            Assert.Equal("2022", result.FloorDetails[0].OldAssessmentYear);
            Assert.Equal(2022, result.FloorDetails[0].AssessmentYearValue);
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_InvalidAssessmentYear_ParsesAsNull()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            await context.SaveChangesAsync();

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new() { Id = null, OldAssessmentYear = "XXXX" }
                }
            };

            var result = await repository.UpdateFloorDetailsOldAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Single(result.FloorDetails);
            Assert.Equal("XXXX", result.FloorDetails[0].OldAssessmentYear);
            Assert.Null(result.FloorDetails[0].AssessmentYearValue);
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_EmptyConstructionYear_ParsesAsNull()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            await context.SaveChangesAsync();

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new() { Id = null, OldConstructionYear = "" }
                }
            };

            var result = await repository.UpdateFloorDetailsOldAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Single(result.FloorDetails);
            Assert.Equal("", result.FloorDetails[0].OldConstructionYear);
            Assert.Null(result.FloorDetails[0].ConstructionYearValue);
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_AllBuiltupAreaFields_Success()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            await context.SaveChangesAsync();

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new()
                    {
                        Id = null,
                        OldBuiltupAreaSqMeter = 150.0,
                        OldBuiltupAreaSqFeet = 1614.59
                    }
                }
            };

            var result = await repository.UpdateFloorDetailsOldAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Single(result.FloorDetails);
            Assert.Equal(150.0, result.FloorDetails[0].OldBuiltupAreaSqMeter);
            Assert.Equal(1614.59, result.FloorDetails[0].OldBuiltupAreaSqFeet);
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_UpdateWithZeroId_InsertsNewRecord()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            await context.SaveChangesAsync();

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new() { Id = 0, OldConstructionYear = "2020" }
                }
            };

            var result = await repository.UpdateFloorDetailsOldAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Single(result.FloorDetails);
            Assert.NotEqual(0, result.FloorDetails[0].Id);
        }

        [Fact]
        public async Task GetFloorDetailsOldAsync_WithAllMasterJoins_ReturnsAllDescriptions()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            context.FloorEntity.Add(new FloorEntity { Id = 5, FloorCode = "1", Description = "First Floor", IsActive = true });
            context.SubFloorEntity.Add(new SubFloorEntity { Id = 12, SubFloorCode = "A", Description = "Section A", IsActive = true });
            context.ConstructionTypeEntity.Add(new ConstructionTypeEntity { Id = 2, ConstructionCode = "RCC", Description = "RCC Construction", IsActive = true });
            context.TypeOfUse.Add(new TypeOfUseEntity { Id = 3, TypeOfUseCode = "R", Description = "Residential", Type = "R", TypeOfUseGroupId = 1, IsActive = true });
            context.SubTypeOfUse.Add(new SubTypeOfUseEntity { Id = 7, Description = "Apartment", TypeOfUseId = 3, IsActive = true });

            context.PropertyDetailsOld.Add(new PropertyDetailsOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldFloorId = 5,
                OldSubFloorId = 12,
                OldConstructionTypeId = 2,
                OldTypeOfUseId = 3,
                OldSubTypeOfUseId = 7,
                IsActive = true,
                MarkedForDeletion = false
            });

            await context.SaveChangesAsync();

            var result = await repository.GetFloorDetailsOldAsync(549357);

            Assert.NotNull(result);
            Assert.Single(result.FloorDetails);
            var detail = result.FloorDetails[0];
            Assert.Equal("First Floor", detail.FloorDescription);
            Assert.Equal("Section A", detail.SubFloorDescription);
            Assert.Equal("RCC Construction", detail.ConstructionTypeDescription);
            Assert.Equal("Residential", detail.TypeOfUseDescription);
            Assert.Equal("Apartment", detail.SubTypeOfUseDescription);
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_WithCancellationToken_PassesTokenCorrectly()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            await context.SaveChangesAsync();

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new() { Id = null, OldConstructionYear = "2020" }
                }
            };

            using var cts = new CancellationTokenSource();
            var result = await repository.UpdateFloorDetailsOldAsync(549357, dto, cts.Token);

            Assert.NotNull(result);
            Assert.Single(result.FloorDetails);
        }

        [Fact]
        public async Task GetFloorDetailsOldAsync_WithCancellationToken_PassesTokenCorrectly()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            await context.SaveChangesAsync();

            using var cts = new CancellationTokenSource();
            var result = await repository.GetFloorDetailsOldAsync(549357, cts.Token);

            Assert.NotNull(result);
            Assert.Empty(result.FloorDetails);
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_UpdateAllOptionalFieldsToNull_Success()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            context.PropertyDetailsOld.Add(new PropertyDetailsOldEntity
            {
                Id = 10,
                PropertyId = 549357,
                OldFloorId = 5,
                OldSubFloorId = 12,
                OldConstructionYear = "2019",
                OldAssessmentYear = "2020",
                OldConstructionTypeId = 2,
                OldTypeOfUseId = 3,
                OldSubTypeOfUseId = 7,
                OldCarpetAreaSqMeter = 100.0,
                OldCarpetAreaSqFeet = 1075.0,
                OldBuiltupAreaSqMeter = 120.0,
                OldBuiltupAreaSqFeet = 1290.0,
                IsActive = true,
                MarkedForDeletion = false
            });

            await context.SaveChangesAsync();

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new()
                    {
                        Id = 10,
                        OldFloorId = null,
                        OldSubFloorId = null,
                        OldConstructionYear = null,
                        OldAssessmentYear = null,
                        OldConstructionTypeId = null,
                        OldTypeOfUseId = null,
                        OldSubTypeOfUseId = null,
                        OldCarpetAreaSqMeter = null,
                        OldCarpetAreaSqFeet = null,
                        OldBuiltupAreaSqMeter = null,
                        OldBuiltupAreaSqFeet = null
                    }
                }
            };

            var result = await repository.UpdateFloorDetailsOldAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Single(result.FloorDetails);
            var detail = result.FloorDetails[0];
            Assert.Null(detail.OldFloorId);
            Assert.Null(detail.OldSubFloorId);
            Assert.Null(detail.OldConstructionYear);
            Assert.Null(detail.OldAssessmentYear);
            Assert.Null(detail.OldConstructionTypeId);
            Assert.Null(detail.OldTypeOfUseId);
            Assert.Null(detail.OldSubTypeOfUseId);
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_MultipleRecordsSoftDelete_UpdatesDeletionDate()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            context.PropertyDetailsOld.Add(new PropertyDetailsOldEntity { Id = 1, PropertyId = 549357, IsActive = true, MarkedForDeletion = false });
            context.PropertyDetailsOld.Add(new PropertyDetailsOldEntity { Id = 2, PropertyId = 549357, IsActive = true, MarkedForDeletion = false });
            context.PropertyDetailsOld.Add(new PropertyDetailsOldEntity { Id = 3, PropertyId = 549357, IsActive = true, MarkedForDeletion = false });

            await context.SaveChangesAsync();

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new() { Id = 1, OldConstructionYear = "2020" }
                }
            };

            var beforeUpdate = DateTime.Now;
            var result = await repository.UpdateFloorDetailsOldAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Single(result.FloorDetails);

            var deletedRecords = await context.PropertyDetailsOld
                .Where(pd => pd.PropertyId == 549357 && pd.MarkedForDeletion)
                .ToListAsync();

            Assert.Equal(2, deletedRecords.Count);
            Assert.All(deletedRecords, r =>
            {
                Assert.True(r.MarkedForDeletion);
                Assert.NotNull(r.MarkedForDeletionDate);
                Assert.True(r.MarkedForDeletionDate >= beforeUpdate);
                Assert.NotNull(r.UpdatedDate);
            });
        }

        [Fact]
        public async Task GetFloorDetailsOldAsync_NullFloorIds_HandlesGracefully()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            context.PropertyDetailsOld.Add(new PropertyDetailsOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldFloorId = null,
                OldSubFloorId = null,
                OldConstructionTypeId = null,
                OldTypeOfUseId = null,
                OldSubTypeOfUseId = null,
                IsActive = true,
                MarkedForDeletion = false
            });

            await context.SaveChangesAsync();

            var result = await repository.GetFloorDetailsOldAsync(549357);

            Assert.NotNull(result);
            Assert.Single(result.FloorDetails);
            var detail = result.FloorDetails[0];
            Assert.Null(detail.OldFloorId);
            Assert.Null(detail.FloorDescription);
            Assert.Null(detail.OldSubFloorId);
            Assert.Null(detail.SubFloorDescription);
            Assert.Null(detail.OldConstructionTypeId);
            Assert.Null(detail.ConstructionTypeDescription);
            Assert.Null(detail.OldTypeOfUseId);
            Assert.Null(detail.TypeOfUseDescription);
            Assert.Null(detail.OldSubTypeOfUseId);
            Assert.Null(detail.SubTypeOfUseDescription);
        }

        [Fact]
        public async Task GetFloorDetailsOldAsync_EmptyAssessmentYear_ParsesAsNull()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            context.PropertyDetailsOld.Add(new PropertyDetailsOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                OldConstructionYear = "",
                OldAssessmentYear = "",
                IsActive = true,
                MarkedForDeletion = false
            });

            await context.SaveChangesAsync();

            var result = await repository.GetFloorDetailsOldAsync(549357);

            Assert.NotNull(result);
            Assert.Single(result.FloorDetails);
            Assert.Null(result.FloorDetails[0].ConstructionYearValue);
            Assert.Null(result.FloorDetails[0].AssessmentYearValue);
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_UpdateWithAllFieldsSet_Success()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            context.FloorEntity.Add(new FloorEntity { Id = 5, FloorCode = "1", Description = "First Floor", IsActive = true });
            context.SubFloorEntity.Add(new SubFloorEntity { Id = 12, SubFloorCode = "A", Description = "Section A", IsActive = true });
            context.ConstructionTypeEntity.Add(new ConstructionTypeEntity { Id = 2, ConstructionCode = "RCC", Description = "RCC", IsActive = true });
            context.TypeOfUse.Add(new TypeOfUseEntity { Id = 3, TypeOfUseCode = "R", Description = "Residential", Type = "R", TypeOfUseGroupId = 1, IsActive = true });
            context.SubTypeOfUse.Add(new SubTypeOfUseEntity { Id = 7, Description = "Apartment", TypeOfUseId = 3, IsActive = true });

            context.PropertyDetailsOld.Add(new PropertyDetailsOldEntity
            {
                Id = 10,
                PropertyId = 549357,
                IsActive = true,
                MarkedForDeletion = false
            });

            await context.SaveChangesAsync();

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new()
                    {
                        Id = 10,
                        OldFloorId = 5,
                        OldSubFloorId = 12,
                        OldConstructionYear = "2020",
                        OldAssessmentYear = "2021",
                        OldConstructionTypeId = 2,
                        OldTypeOfUseId = 3,
                        OldSubTypeOfUseId = 7,
                        OldCarpetAreaSqMeter = 100.0,
                        OldCarpetAreaSqFeet = 1075.0,
                        OldBuiltupAreaSqMeter = 120.0,
                        OldBuiltupAreaSqFeet = 1290.0
                    }
                }
            };

            var result = await repository.UpdateFloorDetailsOldAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Single(result.FloorDetails);
            var detail = result.FloorDetails[0];
            Assert.Equal(10, detail.Id);
            Assert.Equal(5, detail.OldFloorId);
            Assert.Equal(12, detail.OldSubFloorId);
            Assert.Equal("2020", detail.OldConstructionYear);
            Assert.Equal(2020, detail.ConstructionYearValue);
            Assert.Equal("2021", detail.OldAssessmentYear);
            Assert.Equal(2021, detail.AssessmentYearValue);
            Assert.Equal(2, detail.OldConstructionTypeId);
            Assert.Equal(3, detail.OldTypeOfUseId);
            Assert.Equal(7, detail.OldSubTypeOfUseId);
            Assert.Equal(100.0, detail.OldCarpetAreaSqMeter);
            Assert.Equal(1075.0, detail.OldCarpetAreaSqFeet);
            Assert.Equal(120.0, detail.OldBuiltupAreaSqMeter);
            Assert.Equal(1290.0, detail.OldBuiltupAreaSqFeet);
        }

        [Fact]
        public async Task GetFloorDetailsOldAsync_WithMarkedForDeletionDateSet_ReturnsDate()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            var deletionDate = new DateTime(2024, 1, 15);
            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            context.PropertyDetailsOld.Add(new PropertyDetailsOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                IsActive = true,
                MarkedForDeletion = false,
                MarkedForDeletionDate = deletionDate
            });

            await context.SaveChangesAsync();

            var result = await repository.GetFloorDetailsOldAsync(549357);

            Assert.NotNull(result);
            Assert.Single(result.FloorDetails);
            Assert.Equal(deletionDate, result.FloorDetails[0].MarkedForDeletionDate);
        }

        [Fact]
        public async Task UpdateFloorDetailsOldAsync_DuplicateIdInRequest_LastUpdateWins()
        {
            using var context = CreateInMemoryContext();
            var repository = new PropertyRepository(context);

            context.PropertyMast.Add(new PropertyEntity { Id = 549357, PropertyNo = "P001", IsActive = true, MarkedForDeletion = false });
            context.PropertyDetailsOld.Add(new PropertyDetailsOldEntity
            {
                Id = 10,
                PropertyId = 549357,
                OldConstructionYear = "2019",
                IsActive = true,
                MarkedForDeletion = false
            });

            await context.SaveChangesAsync();

            var dto = new UpdatePropertyDetailsOldListDto
            {
                FloorDetails = new List<UpdatePropertyDetailsOldDto>
                {
                    new() { Id = 10, OldConstructionYear = "2020" },
                    new() { Id = 10, OldConstructionYear = "2021" }
                }
            };

            var result = await repository.UpdateFloorDetailsOldAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Single(result.FloorDetails);
            Assert.Equal("2021", result.FloorDetails[0].OldConstructionYear);
        }
    }

    #endregion
}

