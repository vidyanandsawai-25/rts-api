using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Comprehensive tests for Property Old Taxes Details API and Related Entities/DTOs
/// Coverage: Repository, Service, DTOs, Entities (TransMastOldEntity, PropertyOldTaxesDetailsDto, UpdatePropertyOldTaxesDetailsDto)
/// Follows the same pattern as PropertyOldDetailsTests
/// </summary>
public class PropertyOldTaxesDetailsTests
{
    #region TransMastOldEntity Tests

    public class TransMastOldEntityTests
    {
        [Fact]
        public void TransMastOldEntity_AllProperties_GetSet_WorksCorrectly()
        {
            var now = DateTime.Now;
            var entity = new TransMastOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                FinanceYearId = 100,
                RVorCV = "RV",
                RVorCVValue = 75000.75m,
                TaxId = 5,
                TaxAmount = 5000.50m,
                MarkedForDeletion = false,
                MarkedForDeletionDate = null,
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = now,
                UpdatedBy = 2,
                UpdatedDate = now
            };

            Assert.Equal(1, entity.Id);
            Assert.Equal(549357, entity.PropertyId);
            Assert.Equal(100, entity.FinanceYearId);
            Assert.Equal("RV", entity.RVorCV);
            Assert.Equal(75000.75m, entity.RVorCVValue);
            Assert.Equal(5, entity.TaxId);
            Assert.Equal(5000.50m, entity.TaxAmount);
            Assert.False(entity.MarkedForDeletion);
            Assert.Null(entity.MarkedForDeletionDate);
            Assert.True(entity.IsActive);
            Assert.Equal(1, entity.CreatedBy);
            Assert.Equal(2, entity.UpdatedBy);
        }

        [Fact]
        public void TransMastOldEntity_InheritsFromBaseEntity()
        {
            var entity = new TransMastOldEntity();
            Assert.IsAssignableFrom<BaseEntity>(entity);
        }

        [Fact]
        public void TransMastOldEntity_DefaultValues_SetCorrectly()
        {
            var entity = new TransMastOldEntity();

            Assert.Equal(0, entity.Id);
            Assert.Equal(0, entity.PropertyId);
            Assert.Equal(0, entity.FinanceYearId);
            Assert.Equal(0, entity.TaxId);
            Assert.Equal(0m, entity.RVorCVValue);
            Assert.Equal(0m, entity.TaxAmount);
            Assert.False(entity.MarkedForDeletion);
            Assert.True(entity.IsActive);
        }

        [Fact]
        public void TransMastOldEntity_RVorCV_AcceptsValidValues()
        {
            var entity1 = new TransMastOldEntity { RVorCV = "RV" };
            var entity2 = new TransMastOldEntity { RVorCV = "CV" };
            var entity3 = new TransMastOldEntity { RVorCV = "XX" };

            Assert.Equal("RV", entity1.RVorCV);
            Assert.Equal("CV", entity2.RVorCV);
            Assert.Equal("XX", entity3.RVorCV);
        }

        [Fact]
        public void TransMastOldEntity_DecimalValues_StoresPrecision()
        {
            var entity = new TransMastOldEntity
            {
                RVorCVValue = 123456.78m,
                TaxAmount = 9876.54m
            };

            Assert.Equal(123456.78m, entity.RVorCVValue);
            Assert.Equal(9876.54m, entity.TaxAmount);
        }

        [Fact]
        public void TransMastOldEntity_MarkedForDeletionDate_CanBeNull()
        {
            var entity = new TransMastOldEntity
            {
                MarkedForDeletion = true,
                MarkedForDeletionDate = null
            };

            Assert.True(entity.MarkedForDeletion);
            Assert.Null(entity.MarkedForDeletionDate);
        }

        [Fact]
        public void TransMastOldEntity_MarkedForDeletionDate_CanBeSet()
        {
            var now = DateTime.Now;
            var entity = new TransMastOldEntity
            {
                MarkedForDeletion = true,
                MarkedForDeletionDate = now
            };

            Assert.True(entity.MarkedForDeletion);
            Assert.Equal(now, entity.MarkedForDeletionDate);
        }

        [Fact]
        public void TransMastOldEntity_ZeroDecimalValues_WorksCorrectly()
        {
            var entity = new TransMastOldEntity
            {
                RVorCVValue = 0.00m,
                TaxAmount = 0.00m
            };

            Assert.Equal(0.00m, entity.RVorCVValue);
            Assert.Equal(0.00m, entity.TaxAmount);
        }

        [Fact]
        public void TransMastOldEntity_LargeDecimalValues_WorksCorrectly()
        {
            var entity = new TransMastOldEntity
            {
                RVorCVValue = 999999999999.99m,
                TaxAmount = 999999999999.99m
            };

            Assert.Equal(999999999999.99m, entity.RVorCVValue);
            Assert.Equal(999999999999.99m, entity.TaxAmount);
        }
    }

    #endregion

    #region PropertyOldTaxesDetailsDto Tests

    public class PropertyOldTaxesDetailsDtoTests
    {
        [Fact]
        public void PropertyOldTaxesDetailsDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new PropertyOldTaxesDetailsDto
            {
                PropertyId = 549357,
                TaxYears = new List<OldTaxYearDto>
                {
                    new OldTaxYearDto
                    {
                        FinanceYearId = 100,
                        Year = 2023,
                        YearCode = "2023-24",
                        RVorCV = "RV",
                        RVorCVValue = 75000.75m,
                        Taxes = new List<TaxDetailDto>
                        {
                            new TaxDetailDto { TaxId = 1, TaxName = "General Tax", TaxAmount = 5000m },
                            new TaxDetailDto { TaxId = 2, TaxName = "Water Tax", TaxAmount = 1000m }
                        },
                        TaxTotal = 6000m,
                        Interest = 500m,
                        NetTotal = 6500m
                    }
                }
            };

            Assert.Equal(549357, dto.PropertyId);
            Assert.NotEmpty(dto.TaxYears);
            Assert.Equal(100, dto.TaxYears[0].FinanceYearId);
            Assert.Equal(2023, dto.TaxYears[0].Year);
            Assert.Equal("2023-24", dto.TaxYears[0].YearCode);
            Assert.Equal("RV", dto.TaxYears[0].RVorCV);
            Assert.Equal(75000.75m, dto.TaxYears[0].RVorCVValue);
            Assert.Equal(2, dto.TaxYears[0].Taxes.Count);
            Assert.Equal(6000m, dto.TaxYears[0].TaxTotal);
            Assert.Equal(500m, dto.TaxYears[0].Interest);
            Assert.Equal(6500m, dto.TaxYears[0].NetTotal);
        }

        [Fact]
        public void PropertyOldTaxesDetailsDto_DefaultConstructor_InitializesEmptyList()
        {
            var dto = new PropertyOldTaxesDetailsDto();

            Assert.Equal(0, dto.PropertyId);
            Assert.NotNull(dto.TaxYears);
            Assert.Empty(dto.TaxYears);
        }

        [Fact]
        public void OldTaxYearDto_DefaultConstructor_InitializesEmptyTaxList()
        {
            var dto = new OldTaxYearDto();

            Assert.Equal(0, dto.FinanceYearId);
            Assert.Equal(0, dto.Year);
            Assert.Null(dto.YearCode);
            Assert.Null(dto.RVorCV);
            Assert.Null(dto.RVorCVValue);
            Assert.NotNull(dto.Taxes);
            Assert.Empty(dto.Taxes);
            Assert.Equal(0m, dto.TaxTotal);
            Assert.Equal(0m, dto.Interest);
            Assert.Equal(0m, dto.NetTotal);
        }

        [Fact]
        public void TaxDetailDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new TaxDetailDto
            {
                TaxId = 5,
                TaxName = "Special Tax",
                TaxAmount = 2500.50m
            };

            Assert.Equal(5, dto.TaxId);
            Assert.Equal("Special Tax", dto.TaxName);
            Assert.Equal(2500.50m, dto.TaxAmount);
        }

        [Fact]
        public void TaxDetailDto_DefaultConstructor_InitializesCorrectly()
        {
            var dto = new TaxDetailDto();

            Assert.Equal(0, dto.TaxId);
            Assert.Null(dto.TaxName);
            Assert.Equal(0m, dto.TaxAmount);
        }

        [Fact]
        public void OldTaxYearDto_MultipleYears_CanBeAddedToList()
        {
            var dto = new PropertyOldTaxesDetailsDto
            {
                PropertyId = 549357,
                TaxYears = new List<OldTaxYearDto>
                {
                    new OldTaxYearDto { FinanceYearId = 100, Year = 2023 },
                    new OldTaxYearDto { FinanceYearId = 99, Year = 2022 },
                    new OldTaxYearDto { FinanceYearId = 98, Year = 2021 }
                }
            };

            Assert.Equal(3, dto.TaxYears.Count);
            Assert.Equal(2023, dto.TaxYears[0].Year);
            Assert.Equal(2022, dto.TaxYears[1].Year);
            Assert.Equal(2021, dto.TaxYears[2].Year);
        }

        [Fact]
        public void OldTaxYearDto_EmptyTaxesList_IsValid()
        {
            var dto = new OldTaxYearDto
            {
                FinanceYearId = 100,
                Year = 2023,
                Taxes = new List<TaxDetailDto>()
            };

            Assert.Empty(dto.Taxes);
            Assert.Equal(0m, dto.TaxTotal);
            Assert.Equal(0m, dto.Interest);
            Assert.Equal(0m, dto.NetTotal);
        }

        [Fact]
        public void OldTaxYearDto_CalculatedFields_SetCorrectly()
        {
            var dto = new OldTaxYearDto
            {
                TaxTotal = 10000m,
                Interest = 1000m,
                NetTotal = 11000m
            };

            Assert.Equal(10000m, dto.TaxTotal);
            Assert.Equal(1000m, dto.Interest);
            Assert.Equal(11000m, dto.NetTotal);
        }

        [Fact]
        public void PropertyOldTaxesDetailsDto_OptionalFields_CanBeNull()
        {
            var dto = new PropertyOldTaxesDetailsDto
            {
                PropertyId = 549357,
                TaxYears = new List<OldTaxYearDto>
                {
                    new OldTaxYearDto
                    {
                        FinanceYearId = 100,
                        Year = 2023,
                        YearCode = null,
                        RVorCV = null,
                        RVorCVValue = null
                    }
                }
            };

            Assert.Null(dto.TaxYears[0].YearCode);
            Assert.Null(dto.TaxYears[0].RVorCV);
            Assert.Null(dto.TaxYears[0].RVorCVValue);
        }
    }

    #endregion

    #region UpdatePropertyOldTaxesDetailsDto Tests

    public class UpdatePropertyOldTaxesDetailsDtoTests
    {
        [Fact]
        public void UpdatePropertyOldTaxesDetailsDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new UpdatePropertyOldTaxesDetailsDto
            {
                TaxYears = new List<UpdateOldTaxYearDto>
                {
                    new UpdateOldTaxYearDto
                    {
                        FinanceYearId = 100,
                        RVorCV = "RV",
                        RVorCVValue = 75000.75m,
                        Taxes = new List<UpdateTaxDetailDto>
                        {
                            new UpdateTaxDetailDto { TaxId = 1, TaxAmount = 5000m },
                            new UpdateTaxDetailDto { TaxId = 2, TaxAmount = 1000m }
                        }
                    }
                }
            };

            Assert.NotEmpty(dto.TaxYears);
            Assert.Equal(100, dto.TaxYears[0].FinanceYearId);
            Assert.Equal("RV", dto.TaxYears[0].RVorCV);
            Assert.Equal(75000.75m, dto.TaxYears[0].RVorCVValue);
            Assert.Equal(2, dto.TaxYears[0].Taxes.Count);
        }

        [Fact]
        public void UpdatePropertyOldTaxesDetailsDto_DefaultConstructor_InitializesEmptyList()
        {
            var dto = new UpdatePropertyOldTaxesDetailsDto();

            Assert.NotNull(dto.TaxYears);
            Assert.Empty(dto.TaxYears);
        }

        [Fact]
        public void UpdateOldTaxYearDto_DefaultConstructor_InitializesEmptyTaxList()
        {
            var dto = new UpdateOldTaxYearDto();

            Assert.Equal(0, dto.FinanceYearId);
            Assert.Null(dto.RVorCV);
            Assert.Null(dto.RVorCVValue);
            Assert.NotNull(dto.Taxes);
            Assert.Empty(dto.Taxes);
        }

        [Fact]
        public void UpdateTaxDetailDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new UpdateTaxDetailDto
            {
                TaxId = 10,
                TaxAmount = 7500.25m
            };

            Assert.Equal(10, dto.TaxId);
            Assert.Equal(7500.25m, dto.TaxAmount);
        }

        [Fact]
        public void UpdateTaxDetailDto_DefaultConstructor_InitializesCorrectly()
        {
            var dto = new UpdateTaxDetailDto();

            Assert.Equal(0, dto.TaxId);
            Assert.Equal(0m, dto.TaxAmount);
        }

        [Fact]
        public void UpdateOldTaxYearDto_OptionalFields_CanBeNull()
        {
            var dto = new UpdateOldTaxYearDto
            {
                FinanceYearId = 100,
                RVorCV = null,
                RVorCVValue = null
            };

            Assert.Equal(100, dto.FinanceYearId);
            Assert.Null(dto.RVorCV);
            Assert.Null(dto.RVorCVValue);
        }

        [Fact]
        public void UpdatePropertyOldTaxesDetailsDto_MultipleYears_CanBeAdded()
        {
            var dto = new UpdatePropertyOldTaxesDetailsDto
            {
                TaxYears = new List<UpdateOldTaxYearDto>
                {
                    new UpdateOldTaxYearDto { FinanceYearId = 100 },
                    new UpdateOldTaxYearDto { FinanceYearId = 99 },
                    new UpdateOldTaxYearDto { FinanceYearId = 98 }
                }
            };

            Assert.Equal(3, dto.TaxYears.Count);
            Assert.Equal(100, dto.TaxYears[0].FinanceYearId);
            Assert.Equal(99, dto.TaxYears[1].FinanceYearId);
            Assert.Equal(98, dto.TaxYears[2].FinanceYearId);
        }

        [Fact]
        public void UpdateOldTaxYearDto_EmptyTaxesList_IsValid()
        {
            var dto = new UpdateOldTaxYearDto
            {
                FinanceYearId = 100,
                Taxes = new List<UpdateTaxDetailDto>()
            };

            Assert.Empty(dto.Taxes);
        }

        [Fact]
        public void UpdateTaxDetailDto_ZeroAmount_IsValid()
        {
            var dto = new UpdateTaxDetailDto
            {
                TaxId = 5,
                TaxAmount = 0m
            };

            Assert.Equal(0m, dto.TaxAmount);
        }

        [Fact]
        public void UpdateTaxDetailDto_NegativeAmount_CanBeSet()
        {
            var dto = new UpdateTaxDetailDto
            {
                TaxId = 5,
                TaxAmount = -1000m
            };

            Assert.Equal(-1000m, dto.TaxAmount);
        }

        [Fact]
        public void UpdateOldTaxYearDto_RVorCV_AcceptsValidValues()
        {
            var dto1 = new UpdateOldTaxYearDto { RVorCV = "RV" };
            var dto2 = new UpdateOldTaxYearDto { RVorCV = "CV" };

            Assert.Equal("RV", dto1.RVorCV);
            Assert.Equal("CV", dto2.RVorCV);
        }

        [Fact]
        public void UpdateOldTaxYearDto_LargeDecimalValue_WorksCorrectly()
        {
            var dto = new UpdateOldTaxYearDto
            {
                RVorCVValue = 999999999999.99m
            };

            Assert.Equal(999999999999.99m, dto.RVorCVValue);
        }
    }

    #endregion

    #region PropertyRepository OldTaxesDetails Tests

    public class PropertyRepositoryOldTaxesDetailsTests
    {
        [Fact]
        public async Task GetOldTaxesDetailsAsync_PropertyNotFound_ReturnsNull()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var repository = new PropertyRepository(context);

            var result = await repository.GetOldTaxesDetailsAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetOldTaxesDetailsAsync_PropertyExistsButNoOldTaxes_ReturnsEmptyDto()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetOldTaxesDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            Assert.Empty(result.TaxYears);
        }

        [Fact]
        public async Task GetOldTaxesDetailsAsync_NoOldTaxesConfigured_ReturnsEmptyDto()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            // Add a tax without OldTaxStatus = true
            var tax = new TaxMasterEntity
            {
                Id = 1,
                TaxCode = "GT",
                TaxName = "General Tax",
                TaxNameAlias = "GT",
                TaxCategoryId = 1,
                OldTaxStatus = false, // Not an old tax
                IsActive = true
            };

            context.PropertyMast.Add(property);
            context.TaxMaster.Add(tax);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetOldTaxesDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            Assert.Empty(result.TaxYears);
        }

        [Fact]
        public async Task GetOldTaxesDetailsAsync_WithOldTaxesAndTransactions_ReturnsPopulatedDto()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var year = new YearMasterEntity
            {
                Id = 100,
                Year = 2023,
                YearCode = "2023-24",
                IsActive = true
            };

            var tax1 = new TaxMasterEntity
            {
                Id = 1,
                TaxCode = "GT",
                TaxName = "General Tax",
                TaxNameAlias = "GT",
                TaxCategoryId = 1,
                DisplayOrder = 1,
                OldTaxStatus = true,
                IsActive = true
            };

            var tax2 = new TaxMasterEntity
            {
                Id = 2,
                TaxCode = "WT",
                TaxName = "Water Tax",
                TaxNameAlias = "WT",
                TaxCategoryId = 1,
                DisplayOrder = 2,
                OldTaxStatus = true,
                IsActive = true
            };

            var trans1 = new TransMastOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                FinanceYearId = 100,
                TaxId = 1,
                TaxAmount = 5000m,
                RVorCV = "RV",
                RVorCVValue = 75000m,
                IsActive = true,
                MarkedForDeletion = false
            };

            var trans2 = new TransMastOldEntity
            {
                Id = 2,
                PropertyId = 549357,
                FinanceYearId = 100,
                TaxId = 2,
                TaxAmount = 1000m,
                RVorCV = "RV",
                RVorCVValue = 75000m,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            context.YearMaster.Add(year);
            context.TaxMaster.Add(tax1);
            context.TaxMaster.Add(tax2);
            context.TransMastOld.Add(trans1);
            context.TransMastOld.Add(trans2);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetOldTaxesDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            Assert.Single(result.TaxYears);
            Assert.Equal(100, result.TaxYears[0].FinanceYearId);
            Assert.Equal(2023, result.TaxYears[0].Year);
            Assert.Equal("2023-24", result.TaxYears[0].YearCode);
            Assert.Equal("RV", result.TaxYears[0].RVorCV);
            Assert.Equal(75000m, result.TaxYears[0].RVorCVValue);
            Assert.Equal(2, result.TaxYears[0].Taxes.Count);
            Assert.Equal(6000m, result.TaxYears[0].TaxTotal);
            Assert.Equal(0m, result.TaxYears[0].Interest);
            Assert.Equal(6000m, result.TaxYears[0].NetTotal);
        }

        [Fact]
        public async Task GetOldTaxesDetailsAsync_WithInterestTax_CalculatesCorrectly()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var year = new YearMasterEntity
            {
                Id = 100,
                Year = 2023,
                YearCode = "2023-24",
                IsActive = true
            };

            var tax1 = new TaxMasterEntity
            {
                Id = 1,
                TaxCode = "GT",
                TaxName = "General Tax",
                TaxNameAlias = "GT",
                TaxCategoryId = 1,
                DisplayOrder = 1,
                OldTaxStatus = true,
                IsActive = true
            };

            var taxInterest = new TaxMasterEntity
            {
                Id = 2,
                TaxCode = "INT",
                TaxName = "Interest",
                TaxNameAlias = "INT",
                TaxCategoryId = 1,
                DisplayOrder = 2,
                OldTaxStatus = true,
                IsActive = true
            };

            var trans1 = new TransMastOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                FinanceYearId = 100,
                TaxId = 1,
                TaxAmount = 5000m,
                RVorCV = "RV",
                RVorCVValue = 75000m,
                IsActive = true,
                MarkedForDeletion = false
            };

            var trans2 = new TransMastOldEntity
            {
                Id = 2,
                PropertyId = 549357,
                FinanceYearId = 100,
                TaxId = 2,
                TaxAmount = 500m,
                RVorCV = "RV",
                RVorCVValue = 75000m,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            context.YearMaster.Add(year);
            context.TaxMaster.Add(tax1);
            context.TaxMaster.Add(taxInterest);
            context.TransMastOld.Add(trans1);
            context.TransMastOld.Add(trans2);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetOldTaxesDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Single(result.TaxYears);
            Assert.Equal(5000m, result.TaxYears[0].TaxTotal); // General Tax only
            Assert.Equal(500m, result.TaxYears[0].Interest); // Interest separated
            Assert.Equal(5500m, result.TaxYears[0].NetTotal); // TaxTotal + Interest
        }

        [Fact]
        public async Task GetOldTaxesDetailsAsync_MultipleYears_ReturnsDescendingOrder()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var year1 = new YearMasterEntity { Id = 100, Year = 2023, YearCode = "2023-24", IsActive = true };
            var year2 = new YearMasterEntity { Id = 99, Year = 2022, YearCode = "2022-23", IsActive = true };
            var year3 = new YearMasterEntity { Id = 98, Year = 2021, YearCode = "2021-22", IsActive = true };

            var tax = new TaxMasterEntity
            {
                Id = 1,
                TaxCode = "GT",
                TaxName = "General Tax",
                TaxCategoryId = 1,
                DisplayOrder = 1,
                OldTaxStatus = true,
                IsActive = true
            };

            var trans1 = new TransMastOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                FinanceYearId = 100,
                TaxId = 1,
                TaxAmount = 5000m,
                RVorCV = "RV",
                RVorCVValue = 75000m,
                IsActive = true,
                MarkedForDeletion = false
            };

            var trans2 = new TransMastOldEntity
            {
                Id = 2,
                PropertyId = 549357,
                FinanceYearId = 99,
                TaxId = 1,
                TaxAmount = 4500m,
                RVorCV = "RV",
                RVorCVValue = 70000m,
                IsActive = true,
                MarkedForDeletion = false
            };

            var trans3 = new TransMastOldEntity
            {
                Id = 3,
                PropertyId = 549357,
                FinanceYearId = 98,
                TaxId = 1,
                TaxAmount = 4000m,
                RVorCV = "RV",
                RVorCVValue = 65000m,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            context.YearMaster.AddRange(year1, year2, year3);
            context.TaxMaster.Add(tax);
            context.TransMastOld.AddRange(trans1, trans2, trans3);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetOldTaxesDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(3, result.TaxYears.Count);
            Assert.Equal(2023, result.TaxYears[0].Year); // Descending order
            Assert.Equal(2022, result.TaxYears[1].Year);
            Assert.Equal(2021, result.TaxYears[2].Year);
        }

        [Fact]
        public async Task GetOldTaxesDetailsAsync_InactiveTransactions_NotIncluded()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var year = new YearMasterEntity { Id = 100, Year = 2023, YearCode = "2023-24", IsActive = true };
            var tax = new TaxMasterEntity
            {
                Id = 1,
                TaxCode = "GT",
                TaxName = "General Tax",
                TaxCategoryId = 1,
                DisplayOrder = 1,
                OldTaxStatus = true,
                IsActive = true
            };

            var trans1 = new TransMastOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                FinanceYearId = 100,
                TaxId = 1,
                TaxAmount = 5000m,
                RVorCV = "RV",
                RVorCVValue = 75000m,
                IsActive = false, // Inactive
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            context.YearMaster.Add(year);
            context.TaxMaster.Add(tax);
            context.TransMastOld.Add(trans1);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetOldTaxesDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Empty(result.TaxYears); // Inactive transactions should not be included
        }

        [Fact]
        public async Task GetOldTaxesDetailsAsync_MarkedForDeletionTransactions_NotIncluded()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var year = new YearMasterEntity { Id = 100, Year = 2023, YearCode = "2023-24", IsActive = true };
            var tax = new TaxMasterEntity
            {
                Id = 1,
                TaxCode = "GT",
                TaxName = "General Tax",
                TaxCategoryId = 1,
                DisplayOrder = 1,
                OldTaxStatus = true,
                IsActive = true
            };

            var trans1 = new TransMastOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                FinanceYearId = 100,
                TaxId = 1,
                TaxAmount = 5000m,
                RVorCV = "RV",
                RVorCVValue = 75000m,
                IsActive = true,
                MarkedForDeletion = true // Marked for deletion
            };

            context.PropertyMast.Add(property);
            context.YearMaster.Add(year);
            context.TaxMaster.Add(tax);
            context.TransMastOld.Add(trans1);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetOldTaxesDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Empty(result.TaxYears); // Marked for deletion should not be included
        }

        [Fact]
        public async Task GetOldTaxesDetailsAsync_TaxWithNoTransactions_ShowsZeroAmount()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var year = new YearMasterEntity { Id = 100, Year = 2023, YearCode = "2023-24", IsActive = true };

            var tax1 = new TaxMasterEntity
            {
                Id = 1,
                TaxCode = "GT",
                TaxName = "General Tax",
                TaxCategoryId = 1,
                DisplayOrder = 1,
                OldTaxStatus = true,
                IsActive = true
            };

            var tax2 = new TaxMasterEntity
            {
                Id = 2,
                TaxCode = "WT",
                TaxName = "Water Tax",
                TaxCategoryId = 1,
                DisplayOrder = 2,
                OldTaxStatus = true,
                IsActive = true
            };

            // Only transaction for tax1
            var trans1 = new TransMastOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                FinanceYearId = 100,
                TaxId = 1,
                TaxAmount = 5000m,
                RVorCV = "RV",
                RVorCVValue = 75000m,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            context.YearMaster.Add(year);
            context.TaxMaster.Add(tax1);
            context.TaxMaster.Add(tax2);
            context.TransMastOld.Add(trans1);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetOldTaxesDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Single(result.TaxYears);
            Assert.Equal(2, result.TaxYears[0].Taxes.Count);
            Assert.Equal(5000m, result.TaxYears[0].Taxes[0].TaxAmount); // General Tax
            Assert.Equal(0m, result.TaxYears[0].Taxes[1].TaxAmount); // Water Tax (no transaction)
        }

        [Fact]
        public async Task GetOldTaxesDetailsAsync_TaxesOrderedByDisplayOrder_ReturnsCorrectOrder()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var year = new YearMasterEntity { Id = 100, Year = 2023, YearCode = "2023-24", IsActive = true };

            var tax1 = new TaxMasterEntity
            {
                Id = 1,
                TaxCode = "ZT",
                TaxName = "Z Tax",
                TaxCategoryId = 1,
                DisplayOrder = 3,
                OldTaxStatus = true,
                IsActive = true
            };

            var tax2 = new TaxMasterEntity
            {
                Id = 2,
                TaxCode = "AT",
                TaxName = "A Tax",
                TaxCategoryId = 1,
                DisplayOrder = 1,
                OldTaxStatus = true,
                IsActive = true
            };

            var tax3 = new TaxMasterEntity
            {
                Id = 3,
                TaxCode = "MT",
                TaxName = "M Tax",
                TaxCategoryId = 1,
                DisplayOrder = 2,
                OldTaxStatus = true,
                IsActive = true
            };

            context.PropertyMast.Add(property);
            context.YearMaster.Add(year);
            context.TaxMaster.AddRange(tax1, tax2, tax3);
            
            // Add transactions for all taxes so they appear in the result
            context.TransMastOld.Add(new TransMastOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                FinanceYearId = 100,
                TaxId = 1,
                TaxAmount = 100m,
                RVorCV = "RV",
                RVorCVValue = 10000m,
                IsActive = true,
                MarkedForDeletion = false
            });
            context.TransMastOld.Add(new TransMastOldEntity
            {
                Id = 2,
                PropertyId = 549357,
                FinanceYearId = 100,
                TaxId = 2,
                TaxAmount = 200m,
                RVorCV = "RV",
                RVorCVValue = 10000m,
                IsActive = true,
                MarkedForDeletion = false
            });
            context.TransMastOld.Add(new TransMastOldEntity
            {
                Id = 3,
                PropertyId = 549357,
                FinanceYearId = 100,
                TaxId = 3,
                TaxAmount = 300m,
                RVorCV = "RV",
                RVorCVValue = 10000m,
                IsActive = true,
                MarkedForDeletion = false
            });
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetOldTaxesDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(3, result.TaxYears[0].Taxes.Count);
            Assert.Equal("A Tax", result.TaxYears[0].Taxes[0].TaxName); // DisplayOrder 1
            Assert.Equal("M Tax", result.TaxYears[0].Taxes[1].TaxName); // DisplayOrder 2
            Assert.Equal("Z Tax", result.TaxYears[0].Taxes[2].TaxName); // DisplayOrder 3
        }

        [Fact]
        public async Task GetOldTaxesDetailsAsync_UsesTaxNameAlias_WhenAvailable()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var year = new YearMasterEntity { Id = 100, Year = 2023, YearCode = "2023-24", IsActive = true };

            var tax1 = new TaxMasterEntity
            {
                Id = 1,
                TaxCode = "GT",
                TaxName = "General Tax",
                TaxNameAlias = "GT Alias", // Has alias
                TaxCategoryId = 1,
                DisplayOrder = 1,
                OldTaxStatus = true,
                IsActive = true
            };

            var tax2 = new TaxMasterEntity
            {
                Id = 2,
                TaxCode = "WT",
                TaxName = "Water Tax",
                TaxNameAlias = null, // No alias
                TaxCategoryId = 1,
                DisplayOrder = 2,
                OldTaxStatus = true,
                IsActive = true
            };

            context.PropertyMast.Add(property);
            context.YearMaster.Add(year);
            context.TaxMaster.AddRange(tax1, tax2);
            
            // Add transactions so taxes appear in the result
            context.TransMastOld.Add(new TransMastOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                FinanceYearId = 100,
                TaxId = 1,
                TaxAmount = 100m,
                RVorCV = "RV",
                RVorCVValue = 10000m,
                IsActive = true,
                MarkedForDeletion = false
            });
            context.TransMastOld.Add(new TransMastOldEntity
            {
                Id = 2,
                PropertyId = 549357,
                FinanceYearId = 100,
                TaxId = 2,
                TaxAmount = 200m,
                RVorCV = "RV",
                RVorCVValue = 10000m,
                IsActive = true,
                MarkedForDeletion = false
            });
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetOldTaxesDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(2, result.TaxYears[0].Taxes.Count);
            Assert.Equal("GT Alias", result.TaxYears[0].Taxes[0].TaxName); // Uses alias
            Assert.Equal("Water Tax", result.TaxYears[0].Taxes[1].TaxName); // Uses TaxName
        }

        [Fact]
        public async Task UpdateOldTaxesDetailsAsync_PropertyNotFound_ReturnsNull()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var repository = new PropertyRepository(context);

            var dto = new UpdatePropertyOldTaxesDetailsDto
            {
                TaxYears = new List<UpdateOldTaxYearDto>
                {
                    new UpdateOldTaxYearDto { FinanceYearId = 100 }
                }
            };

            var result = await repository.UpdateOldTaxesDetailsAsync(999, dto);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateOldTaxesDetailsAsync_InvalidFinanceYear_ThrowsException()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldTaxesDetailsDto
            {
                TaxYears = new List<UpdateOldTaxYearDto>
                {
                    new UpdateOldTaxYearDto { FinanceYearId = 999 } // Invalid year
                }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await repository.UpdateOldTaxesDetailsAsync(549357, dto));
        }

        [Fact]
        public async Task UpdateOldTaxesDetailsAsync_InvalidTaxId_ThrowsException()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var year = new YearMasterEntity { Id = 100, Year = 2023, IsActive = true };

            context.PropertyMast.Add(property);
            context.YearMaster.Add(year);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldTaxesDetailsDto
            {
                TaxYears = new List<UpdateOldTaxYearDto>
                {
                    new UpdateOldTaxYearDto
                    {
                        FinanceYearId = 100,
                        Taxes = new List<UpdateTaxDetailDto>
                        {
                            new UpdateTaxDetailDto { TaxId = 999, TaxAmount = 5000m } // Invalid tax
                        }
                    }
                }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await repository.UpdateOldTaxesDetailsAsync(549357, dto));
        }

        [Fact]
        public async Task UpdateOldTaxesDetailsAsync_TaxNotOldTax_ThrowsException()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var year = new YearMasterEntity { Id = 100, Year = 2023, IsActive = true };
            var tax = new TaxMasterEntity
            {
                Id = 1,
                TaxCode = "GT",
                TaxName = "General Tax",
                TaxCategoryId = 1,
                OldTaxStatus = false, // Not an old tax
                IsActive = true
            };

            context.PropertyMast.Add(property);
            context.YearMaster.Add(year);
            context.TaxMaster.Add(tax);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldTaxesDetailsDto
            {
                TaxYears = new List<UpdateOldTaxYearDto>
                {
                    new UpdateOldTaxYearDto
                    {
                        FinanceYearId = 100,
                        Taxes = new List<UpdateTaxDetailDto>
                        {
                            new UpdateTaxDetailDto { TaxId = 1, TaxAmount = 5000m }
                        }
                    }
                }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await repository.UpdateOldTaxesDetailsAsync(549357, dto));
        }

        [Fact]
        public async Task UpdateOldTaxesDetailsAsync_RVorCVTooLong_ThrowsException()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var year = new YearMasterEntity { Id = 100, Year = 2023, IsActive = true };
            var tax = new TaxMasterEntity
            {
                Id = 1,
                TaxCode = "GT",
                TaxName = "General Tax",
                TaxCategoryId = 1,
                OldTaxStatus = true,
                IsActive = true
            };

            context.PropertyMast.Add(property);
            context.YearMaster.Add(year);
            context.TaxMaster.Add(tax);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldTaxesDetailsDto
            {
                TaxYears = new List<UpdateOldTaxYearDto>
                {
                    new UpdateOldTaxYearDto
                    {
                        FinanceYearId = 100,
                        RVorCV = "TOOLONG", // More than 2 characters
                        Taxes = new List<UpdateTaxDetailDto>
                        {
                            new UpdateTaxDetailDto { TaxId = 1, TaxAmount = 5000m }
                        }
                    }
                }
            };

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await repository.UpdateOldTaxesDetailsAsync(549357, dto));
        }

        [Fact]
        public async Task UpdateOldTaxesDetailsAsync_NoExistingTransactions_InsertsNewRecords()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var year = new YearMasterEntity { Id = 100, Year = 2023, YearCode = "2023-24", IsActive = true };
            var tax1 = new TaxMasterEntity
            {
                Id = 1,
                TaxCode = "GT",
                TaxName = "General Tax",
                TaxCategoryId = 1,
                DisplayOrder = 1,
                OldTaxStatus = true,
                IsActive = true
            };
            var tax2 = new TaxMasterEntity
            {
                Id = 2,
                TaxCode = "WT",
                TaxName = "Water Tax",
                TaxCategoryId = 1,
                DisplayOrder = 2,
                OldTaxStatus = true,
                IsActive = true
            };

            context.PropertyMast.Add(property);
            context.YearMaster.Add(year);
            context.TaxMaster.AddRange(tax1, tax2);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldTaxesDetailsDto
            {
                TaxYears = new List<UpdateOldTaxYearDto>
                {
                    new UpdateOldTaxYearDto
                    {
                        FinanceYearId = 100,
                        RVorCV = "RV",
                        RVorCVValue = 75000m,
                        Taxes = new List<UpdateTaxDetailDto>
                        {
                            new UpdateTaxDetailDto { TaxId = 1, TaxAmount = 5000m },
                            new UpdateTaxDetailDto { TaxId = 2, TaxAmount = 1000m }
                        }
                    }
                }
            };

            var result = await repository.UpdateOldTaxesDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Single(result.TaxYears);
            Assert.Equal(2, result.TaxYears[0].Taxes.Count);
            Assert.Equal(5000m, result.TaxYears[0].Taxes[0].TaxAmount);
            Assert.Equal(1000m, result.TaxYears[0].Taxes[1].TaxAmount);

            // Verify INSERT happened
            var transCount = await context.TransMastOld.CountAsync();
            Assert.Equal(2, transCount);
        }

        [Fact]
        public async Task UpdateOldTaxesDetailsAsync_ExistingTransactions_UpdatesRecords()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var year = new YearMasterEntity { Id = 100, Year = 2023, YearCode = "2023-24", IsActive = true };
            var tax = new TaxMasterEntity
            {
                Id = 1,
                TaxCode = "GT",
                TaxName = "General Tax",
                TaxCategoryId = 1,
                DisplayOrder = 1,
                OldTaxStatus = true,
                IsActive = true
            };

            var existingTrans = new TransMastOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                FinanceYearId = 100,
                TaxId = 1,
                TaxAmount = 1000m, // Old amount
                RVorCV = "CV", // Old value
                RVorCVValue = 50000m, // Old value
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            context.YearMaster.Add(year);
            context.TaxMaster.Add(tax);
            context.TransMastOld.Add(existingTrans);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldTaxesDetailsDto
            {
                TaxYears = new List<UpdateOldTaxYearDto>
                {
                    new UpdateOldTaxYearDto
                    {
                        FinanceYearId = 100,
                        RVorCV = "RV", // New value
                        RVorCVValue = 75000m, // New value
                        Taxes = new List<UpdateTaxDetailDto>
                        {
                            new UpdateTaxDetailDto { TaxId = 1, TaxAmount = 5000m } // New amount
                        }
                    }
                }
            };

            var result = await repository.UpdateOldTaxesDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal(5000m, result.TaxYears[0].Taxes[0].TaxAmount);
            Assert.Equal("RV", result.TaxYears[0].RVorCV);
            Assert.Equal(75000m, result.TaxYears[0].RVorCVValue);

            // Verify UPDATE happened (still 1 record)
            var transCount = await context.TransMastOld.CountAsync();
            Assert.Equal(1, transCount);
        }

        [Fact]
        public async Task UpdateOldTaxesDetailsAsync_MultipleYears_ProcessesAllYears()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var year1 = new YearMasterEntity { Id = 100, Year = 2023, YearCode = "2023-24", IsActive = true };
            var year2 = new YearMasterEntity { Id = 99, Year = 2022, YearCode = "2022-23", IsActive = true };
            var tax = new TaxMasterEntity
            {
                Id = 1,
                TaxCode = "GT",
                TaxName = "General Tax",
                TaxCategoryId = 1,
                DisplayOrder = 1,
                OldTaxStatus = true,
                IsActive = true
            };

            context.PropertyMast.Add(property);
            context.YearMaster.AddRange(year1, year2);
            context.TaxMaster.Add(tax);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldTaxesDetailsDto
            {
                TaxYears = new List<UpdateOldTaxYearDto>
                {
                    new UpdateOldTaxYearDto
                    {
                        FinanceYearId = 100,
                        RVorCV = "RV",
                        RVorCVValue = 75000m,
                        Taxes = new List<UpdateTaxDetailDto>
                        {
                            new UpdateTaxDetailDto { TaxId = 1, TaxAmount = 5000m }
                        }
                    },
                    new UpdateOldTaxYearDto
                    {
                        FinanceYearId = 99,
                        RVorCV = "RV",
                        RVorCVValue = 70000m,
                        Taxes = new List<UpdateTaxDetailDto>
                        {
                            new UpdateTaxDetailDto { TaxId = 1, TaxAmount = 4500m }
                        }
                    }
                }
            };

            var result = await repository.UpdateOldTaxesDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal(2, result.TaxYears.Count);

            // Verify both years were inserted
            var transCount = await context.TransMastOld.CountAsync();
            Assert.Equal(2, transCount);
        }

        [Fact]
        public async Task UpdateOldTaxesDetailsAsync_DefaultsRVorCVToRV_WhenNotProvided()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var year = new YearMasterEntity { Id = 100, Year = 2023, YearCode = "2023-24", IsActive = true };
            var tax = new TaxMasterEntity
            {
                Id = 1,
                TaxCode = "GT",
                TaxName = "General Tax",
                TaxCategoryId = 1,
                DisplayOrder = 1,
                OldTaxStatus = true,
                IsActive = true
            };

            context.PropertyMast.Add(property);
            context.YearMaster.Add(year);
            context.TaxMaster.Add(tax);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldTaxesDetailsDto
            {
                TaxYears = new List<UpdateOldTaxYearDto>
                {
                    new UpdateOldTaxYearDto
                    {
                        FinanceYearId = 100,
                        RVorCV = null, // Not provided
                        RVorCVValue = null, // Not provided
                        Taxes = new List<UpdateTaxDetailDto>
                        {
                            new UpdateTaxDetailDto { TaxId = 1, TaxAmount = 5000m }
                        }
                    }
                }
            };

            var result = await repository.UpdateOldTaxesDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("RV", result.TaxYears[0].RVorCV); // Defaulted to "RV"
            Assert.Equal(0m, result.TaxYears[0].RVorCVValue); // Defaulted to 0
        }

        [Fact]
        public async Task UpdateOldTaxesDetailsAsync_MixedInsertAndUpdate_ProcessesCorrectly()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var year = new YearMasterEntity { Id = 100, Year = 2023, YearCode = "2023-24", IsActive = true };
            var tax1 = new TaxMasterEntity
            {
                Id = 1,
                TaxCode = "GT",
                TaxName = "General Tax",
                TaxCategoryId = 1,
                DisplayOrder = 1,
                OldTaxStatus = true,
                IsActive = true
            };
            var tax2 = new TaxMasterEntity
            {
                Id = 2,
                TaxCode = "WT",
                TaxName = "Water Tax",
                TaxCategoryId = 1,
                DisplayOrder = 2,
                OldTaxStatus = true,
                IsActive = true
            };

            // Existing transaction for tax1 only
            var existingTrans = new TransMastOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                FinanceYearId = 100,
                TaxId = 1,
                TaxAmount = 1000m,
                RVorCV = "RV",
                RVorCVValue = 50000m,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            context.YearMaster.Add(year);
            context.TaxMaster.AddRange(tax1, tax2);
            context.TransMastOld.Add(existingTrans);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldTaxesDetailsDto
            {
                TaxYears = new List<UpdateOldTaxYearDto>
                {
                    new UpdateOldTaxYearDto
                    {
                        FinanceYearId = 100,
                        RVorCV = "RV",
                        RVorCVValue = 75000m,
                        Taxes = new List<UpdateTaxDetailDto>
                        {
                            new UpdateTaxDetailDto { TaxId = 1, TaxAmount = 5000m }, // UPDATE
                            new UpdateTaxDetailDto { TaxId = 2, TaxAmount = 1000m }  // INSERT
                        }
                    }
                }
            };

            var result = await repository.UpdateOldTaxesDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal(2, result.TaxYears[0].Taxes.Count);

            // Verify 1 UPDATE + 1 INSERT = 2 total records
            var transCount = await context.TransMastOld.CountAsync();
            Assert.Equal(2, transCount);
        }

        [Fact]
        public async Task UpdateOldTaxesDetailsAsync_WithCancellationToken_PassesTokenCorrectly()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var year = new YearMasterEntity { Id = 100, Year = 2023, IsActive = true };
            var tax = new TaxMasterEntity
            {
                Id = 1,
                TaxCode = "GT",
                TaxName = "General Tax",
                TaxCategoryId = 1,
                OldTaxStatus = true,
                IsActive = true
            };

            context.PropertyMast.Add(property);
            context.YearMaster.Add(year);
            context.TaxMaster.Add(tax);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldTaxesDetailsDto
            {
                TaxYears = new List<UpdateOldTaxYearDto>
                {
                    new UpdateOldTaxYearDto
                    {
                        FinanceYearId = 100,
                        Taxes = new List<UpdateTaxDetailDto>
                        {
                            new UpdateTaxDetailDto { TaxId = 1, TaxAmount = 5000m }
                        }
                    }
                }
            };
            var cts = new CancellationTokenSource();

            var result = await repository.UpdateOldTaxesDetailsAsync(549357, dto, cts.Token);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetOldTaxesDetailsAsync_WithCancellationToken_PassesTokenCorrectly()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var cts = new CancellationTokenSource();

            var result = await repository.GetOldTaxesDetailsAsync(549357, cts.Token);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task UpdateOldTaxesDetailsAsync_InactiveProperty_ReturnsNull()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = false, // Inactive
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldTaxesDetailsDto();

            var result = await repository.UpdateOldTaxesDetailsAsync(549357, dto);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateOldTaxesDetailsAsync_MarkedForDeletionProperty_ReturnsNull()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = true // Marked for deletion
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldTaxesDetailsDto();

            var result = await repository.UpdateOldTaxesDetailsAsync(549357, dto);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetOldTaxesDetailsAsync_InactiveProperty_ReturnsNull()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = false, // Inactive
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetOldTaxesDetailsAsync(549357);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetOldTaxesDetailsAsync_MarkedForDeletionProperty_ReturnsNull()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = true // Marked for deletion
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetOldTaxesDetailsAsync(549357);

            Assert.Null(result);
        }
    }

    #endregion

    #region PropertyService OldTaxesDetails Tests

    public class PropertyServiceOldTaxesDetailsTests
    {
        [Fact]
        public async Task GetOldTaxesDetailsAsync_CallsRepository()
        {
            var mockRepo = new Mock<IRepository<PropertyEntity, int>>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockMapper = new Mock<IMapper>();
            var mockPropertyRepo = new Mock<IPropertyRepository>();

            var expectedDto = new PropertyOldTaxesDetailsDto
            {
                PropertyId = 549357,
                TaxYears = new List<OldTaxYearDto>
                {
                    new OldTaxYearDto { FinanceYearId = 100, Year = 2023 }
                }
            };

            mockPropertyRepo
                .Setup(r => r.GetOldTaxesDetailsAsync(549357, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedDto);

            var service = new PropertyService(mockRepo.Object, mockUnitOfWork.Object, mockMapper.Object, mockPropertyRepo.Object);

            var result = await service.GetOldTaxesDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            Assert.Single(result.TaxYears);
            mockPropertyRepo.Verify(r => r.GetOldTaxesDetailsAsync(549357, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetOldTaxesDetailsAsync_PropertyNotFound_ReturnsNull()
        {
            var mockRepo = new Mock<IRepository<PropertyEntity, int>>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockMapper = new Mock<IMapper>();
            var mockPropertyRepo = new Mock<IPropertyRepository>();

            mockPropertyRepo
                .Setup(r => r.GetOldTaxesDetailsAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((PropertyOldTaxesDetailsDto?)null);

            var service = new PropertyService(mockRepo.Object, mockUnitOfWork.Object, mockMapper.Object, mockPropertyRepo.Object);

            var result = await service.GetOldTaxesDetailsAsync(999);

            Assert.Null(result);
            mockPropertyRepo.Verify(r => r.GetOldTaxesDetailsAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateOldTaxesDetailsAsync_CallsRepositoryAndReturnsResult()
        {
            var mockRepo = new Mock<IRepository<PropertyEntity, int>>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockMapper = new Mock<IMapper>();
            var mockPropertyRepo = new Mock<IPropertyRepository>();

            var dto = new UpdatePropertyOldTaxesDetailsDto
            {
                TaxYears = new List<UpdateOldTaxYearDto>
                {
                    new UpdateOldTaxYearDto { FinanceYearId = 100 }
                }
            };

            var expectedResult = new PropertyOldTaxesDetailsDto
            {
                PropertyId = 549357,
                TaxYears = new List<OldTaxYearDto>
                {
                    new OldTaxYearDto { FinanceYearId = 100, Year = 2023 }
                }
            };

            mockPropertyRepo
                .Setup(r => r.UpdateOldTaxesDetailsAsync(549357, dto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            var service = new PropertyService(mockRepo.Object, mockUnitOfWork.Object, mockMapper.Object, mockPropertyRepo.Object);

            var result = await service.UpdateOldTaxesDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            mockPropertyRepo.Verify(r => r.UpdateOldTaxesDetailsAsync(549357, dto, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateOldTaxesDetailsAsync_PropertyNotFound_ReturnsNull()
        {
            var mockRepo = new Mock<IRepository<PropertyEntity, int>>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockMapper = new Mock<IMapper>();
            var mockPropertyRepo = new Mock<IPropertyRepository>();

            var dto = new UpdatePropertyOldTaxesDetailsDto();

            mockPropertyRepo
                .Setup(r => r.UpdateOldTaxesDetailsAsync(999, dto, It.IsAny<CancellationToken>()))
                .ReturnsAsync((PropertyOldTaxesDetailsDto?)null);

            var service = new PropertyService(mockRepo.Object, mockUnitOfWork.Object, mockMapper.Object, mockPropertyRepo.Object);

            var result = await service.UpdateOldTaxesDetailsAsync(999, dto);

            Assert.Null(result);
            mockPropertyRepo.Verify(r => r.UpdateOldTaxesDetailsAsync(999, dto, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateOldTaxesDetailsAsync_WithCancellationToken_PassesTokenToRepository()
        {
            var mockRepo = new Mock<IRepository<PropertyEntity, int>>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockMapper = new Mock<IMapper>();
            var mockPropertyRepo = new Mock<IPropertyRepository>();

            var dto = new UpdatePropertyOldTaxesDetailsDto();
            var expectedResult = new PropertyOldTaxesDetailsDto { PropertyId = 549357 };
            var cts = new CancellationTokenSource();

            mockPropertyRepo
                .Setup(r => r.UpdateOldTaxesDetailsAsync(549357, dto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            var service = new PropertyService(mockRepo.Object, mockUnitOfWork.Object, mockMapper.Object, mockPropertyRepo.Object);

            var result = await service.UpdateOldTaxesDetailsAsync(549357, dto, cts.Token);

            Assert.NotNull(result);
            mockPropertyRepo.Verify(r => r.UpdateOldTaxesDetailsAsync(549357, dto, cts.Token), Times.Once);
        }

        [Fact]
        public async Task GetOldTaxesDetailsAsync_WithCancellationToken_PassesTokenToRepository()
        {
            var mockRepo = new Mock<IRepository<PropertyEntity, int>>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockMapper = new Mock<IMapper>();
            var mockPropertyRepo = new Mock<IPropertyRepository>();

            var expectedDto = new PropertyOldTaxesDetailsDto { PropertyId = 549357 };
            var cts = new CancellationTokenSource();

            mockPropertyRepo
                .Setup(r => r.GetOldTaxesDetailsAsync(549357, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedDto);

            var service = new PropertyService(mockRepo.Object, mockUnitOfWork.Object, mockMapper.Object, mockPropertyRepo.Object);

            var result = await service.GetOldTaxesDetailsAsync(549357, cts.Token);

            Assert.NotNull(result);
            mockPropertyRepo.Verify(r => r.GetOldTaxesDetailsAsync(549357, cts.Token), Times.Once);
        }

        [Fact]
        public async Task GetOldTaxesDetailsAsync_ExistingProperty_ReturnsCompleteDto()
        {
            var mockRepo = new Mock<IRepository<PropertyEntity, int>>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockMapper = new Mock<IMapper>();
            var mockPropertyRepo = new Mock<IPropertyRepository>();

            var expectedDto = new PropertyOldTaxesDetailsDto
            {
                PropertyId = 549357,
                TaxYears = new List<OldTaxYearDto>
                {
                    new OldTaxYearDto
                    {
                        FinanceYearId = 100,
                        Year = 2023,
                        YearCode = "2023-24",
                        RVorCV = "RV",
                        RVorCVValue = 75000m,
                        Taxes = new List<TaxDetailDto>
                        {
                            new TaxDetailDto { TaxId = 1, TaxName = "General Tax", TaxAmount = 5000m },
                            new TaxDetailDto { TaxId = 2, TaxName = "Water Tax", TaxAmount = 1000m }
                        },
                        TaxTotal = 6000m,
                        Interest = 500m,
                        NetTotal = 6500m
                    }
                }
            };

            mockPropertyRepo
                .Setup(r => r.GetOldTaxesDetailsAsync(549357, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedDto);

            var service = new PropertyService(mockRepo.Object, mockUnitOfWork.Object, mockMapper.Object, mockPropertyRepo.Object);

            var result = await service.GetOldTaxesDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            Assert.Single(result.TaxYears);
            Assert.Equal(2023, result.TaxYears[0].Year);
            Assert.Equal("2023-24", result.TaxYears[0].YearCode);
            Assert.Equal(2, result.TaxYears[0].Taxes.Count);
            Assert.Equal(6000m, result.TaxYears[0].TaxTotal);
            Assert.Equal(500m, result.TaxYears[0].Interest);
            Assert.Equal(6500m, result.TaxYears[0].NetTotal);
        }
    }

    #endregion

    #region Property Old Taxes Details Validation Tests

    public class PropertyOldTaxesDetailsValidationTests
    {
        [Fact]
        public async Task UpdateOldTaxesDetailsAsync_DuplicateTaxIdInSameYear_ThrowsException()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var year = new YearMasterEntity { Id = 100, Year = 2023, IsActive = true };
            var tax = new TaxMasterEntity
            {
                Id = 1,
                TaxCode = "GT",
                TaxName = "General Tax",
                TaxCategoryId = 1,
                OldTaxStatus = true,
                IsActive = true
            };

            context.PropertyMast.Add(property);
            context.YearMaster.Add(year);
            context.TaxMaster.Add(tax);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldTaxesDetailsDto
            {
                TaxYears = new List<UpdateOldTaxYearDto>
                {
                    new UpdateOldTaxYearDto
                    {
                        FinanceYearId = 100,
                        Taxes = new List<UpdateTaxDetailDto>
                        {
                            new UpdateTaxDetailDto { TaxId = 1, TaxAmount = 5000m },
                            new UpdateTaxDetailDto { TaxId = 1, TaxAmount = 3000m } // Duplicate!
                        }
                    }
                }
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await repository.UpdateOldTaxesDetailsAsync(549357, dto));

            Assert.Contains("Duplicate TaxId(s) found", exception.Message);
            Assert.Contains("year 100", exception.Message);
            Assert.Contains(": 1", exception.Message); // TaxId format in message
        }

        [Fact]
        public async Task UpdateOldTaxesDetailsAsync_EmptyStringRVorCV_DefaultsToRV()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var year = new YearMasterEntity { Id = 100, Year = 2023, YearCode = "2023-24", IsActive = true };
            var tax = new TaxMasterEntity
            {
                Id = 1,
                TaxCode = "GT",
                TaxName = "General Tax",
                TaxCategoryId = 1,
                DisplayOrder = 1,
                OldTaxStatus = true,
                IsActive = true
            };

            context.PropertyMast.Add(property);
            context.YearMaster.Add(year);
            context.TaxMaster.Add(tax);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldTaxesDetailsDto
            {
                TaxYears = new List<UpdateOldTaxYearDto>
                {
                    new UpdateOldTaxYearDto
                    {
                        FinanceYearId = 100,
                        RVorCV = "   ", // Empty/whitespace
                        RVorCVValue = 75000m,
                        Taxes = new List<UpdateTaxDetailDto>
                        {
                            new UpdateTaxDetailDto { TaxId = 1, TaxAmount = 5000m }
                        }
                    }
                }
            };

            var result = await repository.UpdateOldTaxesDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("RV", result.TaxYears[0].RVorCV); // Should default to "RV"
        }

        [Fact]
        public async Task UpdateOldTaxesDetailsAsync_WhitespaceInRVorCV_TrimsCorrectly()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var year = new YearMasterEntity { Id = 100, Year = 2023, YearCode = "2023-24", IsActive = true };
            var tax = new TaxMasterEntity
            {
                Id = 1,
                TaxCode = "GT",
                TaxName = "General Tax",
                TaxCategoryId = 1,
                DisplayOrder = 1,
                OldTaxStatus = true,
                IsActive = true
            };

            context.PropertyMast.Add(property);
            context.YearMaster.Add(year);
            context.TaxMaster.Add(tax);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldTaxesDetailsDto
            {
                TaxYears = new List<UpdateOldTaxYearDto>
                {
                    new UpdateOldTaxYearDto
                    {
                        FinanceYearId = 100,
                        RVorCV = "  CV  ", // Whitespace around value
                        RVorCVValue = 75000m,
                        Taxes = new List<UpdateTaxDetailDto>
                        {
                            new UpdateTaxDetailDto { TaxId = 1, TaxAmount = 5000m }
                        }
                    }
                }
            };

            var result = await repository.UpdateOldTaxesDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("CV", result.TaxYears[0].RVorCV); // Should be trimmed
        }

        [Fact]
        public async Task UpdateOldTaxesDetailsAsync_RVorCVTooLongAfterTrim_ThrowsExceptionWithDetails()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var year = new YearMasterEntity { Id = 100, Year = 2023, IsActive = true };
            var tax = new TaxMasterEntity
            {
                Id = 1,
                TaxCode = "GT",
                TaxName = "General Tax",
                TaxCategoryId = 1,
                OldTaxStatus = true,
                IsActive = true
            };

            context.PropertyMast.Add(property);
            context.YearMaster.Add(year);
            context.TaxMaster.Add(tax);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldTaxesDetailsDto
            {
                TaxYears = new List<UpdateOldTaxYearDto>
                {
                    new UpdateOldTaxYearDto
                    {
                        FinanceYearId = 100,
                        RVorCV = "TOOLONG",
                        Taxes = new List<UpdateTaxDetailDto>
                        {
                            new UpdateTaxDetailDto { TaxId = 1, TaxAmount = 5000m }
                        }
                    }
                }
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await repository.UpdateOldTaxesDetailsAsync(549357, dto));

            Assert.Contains("RVorCV must be 2 characters or less", exception.Message);
            Assert.Contains("'TOOLONG'", exception.Message);
            Assert.Contains("7 characters", exception.Message);
        }

        [Fact]
        public async Task UpdateOldTaxesDetailsAsync_MultipleYears_UpdatesAllYearsCorrectly()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            var year1 = new YearMasterEntity { Id = 100, Year = 2023, YearCode = "2023-24", IsActive = true };
            var year2 = new YearMasterEntity { Id = 99, Year = 2022, YearCode = "2022-23", IsActive = true };
            var year3 = new YearMasterEntity { Id = 98, Year = 2021, YearCode = "2021-22", IsActive = true };

            var tax1 = new TaxMasterEntity
            {
                Id = 1,
                TaxCode = "GT",
                TaxName = "General Tax",
                TaxCategoryId = 1,
                DisplayOrder = 1,
                OldTaxStatus = true,
                IsActive = true
            };

            var tax2 = new TaxMasterEntity
            {
                Id = 2,
                TaxCode = "WT",
                TaxName = "Water Tax",
                TaxCategoryId = 1,
                DisplayOrder = 2,
                OldTaxStatus = true,
                IsActive = true
            };

            // Add some existing transactions
            var trans1 = new TransMastOldEntity
            {
                Id = 1,
                PropertyId = 549357,
                FinanceYearId = 100,
                TaxId = 1,
                TaxAmount = 1000m,
                RVorCV = "RV",
                RVorCVValue = 50000m,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            context.YearMaster.AddRange(year1, year2, year3);
            context.TaxMaster.AddRange(tax1, tax2);
            context.TransMastOld.Add(trans1);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertyOldTaxesDetailsDto
            {
                TaxYears = new List<UpdateOldTaxYearDto>
                {
                    new UpdateOldTaxYearDto
                    {
                        FinanceYearId = 100,
                        RVorCV = "RV",
                        RVorCVValue = 75000m,
                        Taxes = new List<UpdateTaxDetailDto>
                        {
                            new UpdateTaxDetailDto { TaxId = 1, TaxAmount = 5000m },
                            new UpdateTaxDetailDto { TaxId = 2, TaxAmount = 1000m }
                        }
                    },
                    new UpdateOldTaxYearDto
                    {
                        FinanceYearId = 99,
                        RVorCV = "RV",
                        RVorCVValue = 70000m,
                        Taxes = new List<UpdateTaxDetailDto>
                        {
                            new UpdateTaxDetailDto { TaxId = 1, TaxAmount = 4500m },
                            new UpdateTaxDetailDto { TaxId = 2, TaxAmount = 900m }
                        }
                    },
                    new UpdateOldTaxYearDto
                    {
                        FinanceYearId = 98,
                        RVorCV = "RV",
                        RVorCVValue = 65000m,
                        Taxes = new List<UpdateTaxDetailDto>
                        {
                            new UpdateTaxDetailDto { TaxId = 1, TaxAmount = 4000m },
                            new UpdateTaxDetailDto { TaxId = 2, TaxAmount = 800m }
                        }
                    }
                }
            };

            var result = await repository.UpdateOldTaxesDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal(3, result.TaxYears.Count);

            // Verify all data was processed correctly
            Assert.Equal(5000m, result.TaxYears[0].Taxes[0].TaxAmount);
            Assert.Equal(4500m, result.TaxYears[1].Taxes[0].TaxAmount);
            Assert.Equal(4000m, result.TaxYears[2].Taxes[0].TaxAmount);
        }

        [Fact]
        public async Task GetOldTaxesDetailsAsync_LargeDataset_ReturnsAllDataCorrectly()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);

            // Create 5 years
            var years = new List<YearMasterEntity>();
            for (int i = 0; i < 5; i++)
            {
                years.Add(new YearMasterEntity
                {
                    Id = 100 + i,
                    Year = 2023 - i,
                    YearCode = $"{2023 - i}-{2024 - i}",
                    IsActive = true
                });
            }
            context.YearMaster.AddRange(years);

            // Create 10 different taxes
            var taxes = new List<TaxMasterEntity>();
            for (int i = 1; i <= 10; i++)
            {
                taxes.Add(new TaxMasterEntity
                {
                    Id = i,
                    TaxCode = $"T{i}",
                    TaxName = $"Tax {i}",
                    TaxCategoryId = 1,
                    DisplayOrder = i,
                    OldTaxStatus = true,
                    IsActive = true
                });
            }
            context.TaxMaster.AddRange(taxes);

            // Create transactions for each year and tax (5 years x 10 taxes = 50 transactions)
            var transactions = new List<TransMastOldEntity>();
            int transId = 1;
            foreach (var year in years)
            {
                foreach (var tax in taxes)
                {
                    transactions.Add(new TransMastOldEntity
                    {
                        Id = transId++,
                        PropertyId = 549357,
                        FinanceYearId = year.Id,
                        TaxId = tax.Id,
                        TaxAmount = 100m * tax.Id,
                        RVorCV = "RV",
                        RVorCVValue = 50000m + (year.Year * 1000),
                        IsActive = true,
                        MarkedForDeletion = false
                    });
                }
            }
            context.TransMastOld.AddRange(transactions);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetOldTaxesDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(5, result.TaxYears.Count);
            Assert.All(result.TaxYears, year => Assert.Equal(10, year.Taxes.Count));

            // Verify data integrity
            var firstYear = result.TaxYears[0];
            Assert.Equal(5500m, firstYear.TaxTotal); // Sum of tax 1-10 (100+200+...+1000 = 5500)
        }
    }

    #endregion
}
