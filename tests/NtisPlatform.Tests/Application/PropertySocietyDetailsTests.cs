using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Repositories;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Comprehensive tests for Property Society Details API
/// Coverage: Repository, Service, DTOs, Entities (SocietyDetailsEntity, PropertySocietyDetailsDto, UpdatePropertySocietyDetailsDto)
/// </summary>
public class PropertySocietyDetailsTests
{
    #region PropertySocietyDetailsDto Tests

    public class PropertySocietyDetailsDtoTests
    {
        [Fact]
        public void PropertySocietyDetailsDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new PropertySocietyDetailsDto
            {
                PropertyId = 549357,
                SocietyDetailId = 100,
                WingId = 5,
                WingNo = "A",
                WingName = "West Wing",
                SocietyName = "ABC Society",
                SocietyAddress = "123 Main Street",
                SecretaryName = "John Secretary",
                ManagerName = "Jane Manager",
                LandOwnerName = "Land Owner",
                BuilderName = "Builder Corp",
                SocietyNameEnglish = "ABC Society Eng",
                SocietyAddressEnglish = "123 Main St Eng",
                SecretaryNameEnglish = "John Sec Eng",
                ManagerNameEnglish = "Jane Mgr Eng",
                LandOwnerNameEnglish = "Land Owner Eng",
                BuilderNameEnglish = "Builder Eng",
                ManagerMobileNo = "9876543210",
                SecretaryMobileNo = "8765432109",
                SocietyEmailId = "society@example.com",
                SecretaryEmailId = "secretary@example.com",
                ManagerEmailId = "manager@example.com"
            };

            Assert.Equal(549357, dto.PropertyId);
            Assert.Equal(100, dto.SocietyDetailId);
            Assert.Equal(5, dto.WingId);
            Assert.Equal("A", dto.WingNo);
            Assert.Equal("West Wing", dto.WingName);
            Assert.Equal("ABC Society", dto.SocietyName);
            Assert.Equal("123 Main Street", dto.SocietyAddress);
            Assert.Equal("John Secretary", dto.SecretaryName);
            Assert.Equal("Jane Manager", dto.ManagerName);
            Assert.Equal("Land Owner", dto.LandOwnerName);
            Assert.Equal("Builder Corp", dto.BuilderName);
            Assert.Equal("ABC Society Eng", dto.SocietyNameEnglish);
            Assert.Equal("123 Main St Eng", dto.SocietyAddressEnglish);
            Assert.Equal("John Sec Eng", dto.SecretaryNameEnglish);
            Assert.Equal("Jane Mgr Eng", dto.ManagerNameEnglish);
            Assert.Equal("Land Owner Eng", dto.LandOwnerNameEnglish);
            Assert.Equal("Builder Eng", dto.BuilderNameEnglish);
            Assert.Equal("9876543210", dto.ManagerMobileNo);
            Assert.Equal("8765432109", dto.SecretaryMobileNo);
            Assert.Equal("society@example.com", dto.SocietyEmailId);
            Assert.Equal("secretary@example.com", dto.SecretaryEmailId);
            Assert.Equal("manager@example.com", dto.ManagerEmailId);
        }

        [Fact]
        public void PropertySocietyDetailsDto_AllOptionalProperties_CanBeNull()
        {
            var dto = new PropertySocietyDetailsDto
            {
                PropertyId = 549357
            };

            Assert.Null(dto.SocietyDetailId);
            Assert.Null(dto.WingId);
            Assert.Null(dto.WingNo);
            Assert.Null(dto.WingName);
            Assert.Null(dto.SocietyName);
            Assert.Null(dto.SocietyAddress);
            Assert.Null(dto.SecretaryName);
            Assert.Null(dto.ManagerName);
            Assert.Null(dto.LandOwnerName);
            Assert.Null(dto.BuilderName);
            Assert.Null(dto.SocietyNameEnglish);
            Assert.Null(dto.SocietyAddressEnglish);
            Assert.Null(dto.SecretaryNameEnglish);
            Assert.Null(dto.ManagerNameEnglish);
            Assert.Null(dto.LandOwnerNameEnglish);
            Assert.Null(dto.BuilderNameEnglish);
            Assert.Null(dto.ManagerMobileNo);
            Assert.Null(dto.SecretaryMobileNo);
            Assert.Null(dto.SocietyEmailId);
            Assert.Null(dto.SecretaryEmailId);
            Assert.Null(dto.ManagerEmailId);
        }

        [Fact]
        public void PropertySocietyDetailsDto_DefaultConstructor_InitializesCorrectly()
        {
            var dto = new PropertySocietyDetailsDto();

            Assert.Null(dto.PropertyId);
            Assert.Null(dto.SocietyDetailId);
        }
    }

    #endregion

    #region UpdatePropertySocietyDetailsDto Tests

    public class UpdatePropertySocietyDetailsDtoTests
    {
        private static IList<ValidationResult> Validate(object model)
        {
            var results = new List<ValidationResult>();
            var ctx = new ValidationContext(model, serviceProvider: null, items: null);
            Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
            return results;
        }

        [Fact]
        public void UpdatePropertySocietyDetailsDto_AllProperties_GetSet_WorksCorrectly()
        {
            var dto = new UpdatePropertySocietyDetailsDto
            {
                WingId = 5,
                WingName = "West Wing",
                SocietyName = "ABC Society",
                SocietyAddress = "123 Main Street",
                SecretaryName = "John Secretary",
                ManagerName = "Jane Manager",
                LandOwnerName = "Land Owner",
                BuilderName = "Builder Corp",
                SocietyNameEnglish = "ABC Society Eng",
                SocietyAddressEnglish = "123 Main St Eng",
                SecretaryNameEnglish = "John Sec Eng",
                ManagerNameEnglish = "Jane Mgr Eng",
                LandOwnerNameEnglish = "Land Owner Eng",
                BuilderNameEnglish = "Builder Eng",
                ManagerMobileNo = "9876543210",
                SecretaryMobileNo = "8765432109",
                SocietyEmailId = "society@example.com",
                SecretaryEmailId = "secretary@example.com",
                ManagerEmailId = "manager@example.com"
            };

            Assert.Equal(5, dto.WingId);
            Assert.Equal("West Wing", dto.WingName);
            Assert.Equal("ABC Society", dto.SocietyName);
            Assert.Equal("123 Main Street", dto.SocietyAddress);
            Assert.Equal("John Secretary", dto.SecretaryName);
            Assert.Equal("Jane Manager", dto.ManagerName);
            Assert.Equal("Land Owner", dto.LandOwnerName);
            Assert.Equal("Builder Corp", dto.BuilderName);
            Assert.Equal("ABC Society Eng", dto.SocietyNameEnglish);
            Assert.Equal("123 Main St Eng", dto.SocietyAddressEnglish);
            Assert.Equal("John Sec Eng", dto.SecretaryNameEnglish);
            Assert.Equal("Jane Mgr Eng", dto.ManagerNameEnglish);
            Assert.Equal("Land Owner Eng", dto.LandOwnerNameEnglish);
            Assert.Equal("Builder Eng", dto.BuilderNameEnglish);
            Assert.Equal("9876543210", dto.ManagerMobileNo);
            Assert.Equal("8765432109", dto.SecretaryMobileNo);
            Assert.Equal("society@example.com", dto.SocietyEmailId);
            Assert.Equal("secretary@example.com", dto.SecretaryEmailId);
            Assert.Equal("manager@example.com", dto.ManagerEmailId);
        }

        [Fact]
        public void UpdatePropertySocietyDetailsDto_AllOptional_PassesValidation()
        {
            var dto = new UpdatePropertySocietyDetailsDto();

            var results = Validate(dto);
            Assert.Empty(results);
        }

        [Fact]
        public void UpdatePropertySocietyDetailsDto_ValidData_PassesValidation()
        {
            var dto = new UpdatePropertySocietyDetailsDto
            {
                WingId = 5,
                SocietyName = "ABC Society",
                ManagerMobileNo = "9876543210",
                SocietyEmailId = "society@example.com"
            };

            var results = Validate(dto);
            Assert.Empty(results);
        }

        [Fact]
        public void UpdatePropertySocietyDetailsDto_InvalidWingId_FailsValidation()
        {
            var dto = new UpdatePropertySocietyDetailsDto
            {
                WingId = 0
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("WingId must be greater than 0"));
        }

        [Fact]
        public void UpdatePropertySocietyDetailsDto_ExceedMaxLengthWingName_FailsValidation()
        {
            var dto = new UpdatePropertySocietyDetailsDto
            {
                WingName = new string('A', 31)
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("WingName") && r.ErrorMessage.Contains("30"));
        }

        [Fact]
        public void UpdatePropertySocietyDetailsDto_ExceedMaxLengthSocietyName_FailsValidation()
        {
            var dto = new UpdatePropertySocietyDetailsDto
            {
                SocietyName = new string('B', 501)
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("SocietyName") && r.ErrorMessage.Contains("500"));
        }

        [Fact]
        public void UpdatePropertySocietyDetailsDto_ExceedMaxLength200Chars_FailsValidation()
        {
            var dto = new UpdatePropertySocietyDetailsDto
            {
                SocietyAddress = new string('C', 201),
                SecretaryName = new string('D', 201),
                ManagerName = new string('E', 201)
            };

            var results = Validate(dto);
            Assert.True(results.Count >= 3);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("SocietyAddress") && r.ErrorMessage.Contains("200"));
            Assert.Contains(results, r => r.ErrorMessage!.Contains("SecretaryName") && r.ErrorMessage.Contains("200"));
            Assert.Contains(results, r => r.ErrorMessage!.Contains("ManagerName") && r.ErrorMessage.Contains("200"));
        }

        [Fact]
        public void UpdatePropertySocietyDetailsDto_InvalidMobileNo_FailsValidation()
        {
            var dto = new UpdatePropertySocietyDetailsDto
            {
                ManagerMobileNo = "abc123xyz"
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("ManagerMobileNo") && r.ErrorMessage.Contains("invalid characters"));
        }

        [Fact]
        public void UpdatePropertySocietyDetailsDto_ValidMobileNo_PassesValidation()
        {
            var dto = new UpdatePropertySocietyDetailsDto
            {
                ManagerMobileNo = "9876543210",
                SecretaryMobileNo = "+91-987654321"
            };

            var results = Validate(dto);
            Assert.Empty(results);
        }

        [Fact]
        public void UpdatePropertySocietyDetailsDto_InvalidEmail_FailsValidation()
        {
            var dto = new UpdatePropertySocietyDetailsDto
            {
                SocietyEmailId = "invalid-email"
            };

            var results = Validate(dto);
            Assert.NotEmpty(results);
            Assert.Contains(results, r => r.ErrorMessage!.Contains("SocietyEmailId") && r.ErrorMessage.Contains("valid email"));
        }

        [Fact]
        public void UpdatePropertySocietyDetailsDto_ValidEmails_PassValidation()
        {
            var dto = new UpdatePropertySocietyDetailsDto
            {
                SocietyEmailId = "society@example.com",
                SecretaryEmailId = "secretary@example.com",
                ManagerEmailId = "manager@example.com"
            };

            var results = Validate(dto);
            Assert.Empty(results);
        }

        [Fact]
        public void UpdatePropertySocietyDetailsDto_MaxLengthExactValues_PassValidation()
        {
            var dto = new UpdatePropertySocietyDetailsDto
            {
                WingName = new string('A', 30),
                SocietyName = new string('B', 500),
                SocietyAddress = new string('C', 200),
                ManagerMobileNo = new string('1', 13),
                SocietyEmailId = "a@b.co"
            };

            var results = Validate(dto);
            Assert.Empty(results);
        }
    }

    #endregion

    #region SocietyDetailsEntity Tests

    public class SocietyDetailsEntityTests
    {
        [Fact]
        public void SocietyDetailsEntity_AllProperties_GetSet_WorksCorrectly()
        {
            var now = DateTime.Now;
            var entity = new SocietyDetailsEntity
            {
                Id = 1,
                PropertyId = 549357,
                WingId = 5,
                WingName = "West Wing",
                SocietyName = "ABC Society",
                SocietyAddress = "123 Main Street",
                SecretaryName = "John Secretary",
                ManagerName = "Jane Manager",
                LandOwnerName = "Land Owner",
                BuilderName = "Builder Corp",
                SecretaryNameEnglish = "John Sec Eng",
                SocietyNameEnglish = "ABC Society Eng",
                SocietyAddressEnglish = "123 Main St Eng",
                ManagerNameEnglish = "Jane Mgr Eng",
                LandOwnerNameEnglish = "Land Owner Eng",
                BuilderNameEnglish = "Builder Eng",
                ManagerMobileNo = "9876543210",
                SecretaryMobileNo = "8765432109",
                SocietyEmailId = "society@example.com",
                SecretaryEmailId = "secretary@example.com",
                ManagerEmailId = "manager@example.com",
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
            Assert.Equal(5, entity.WingId);
            Assert.Equal("West Wing", entity.WingName);
            Assert.Equal("ABC Society", entity.SocietyName);
            Assert.Equal("123 Main Street", entity.SocietyAddress);
            Assert.Equal("John Secretary", entity.SecretaryName);
            Assert.Equal("Jane Manager", entity.ManagerName);
            Assert.Equal("Land Owner", entity.LandOwnerName);
            Assert.Equal("Builder Corp", entity.BuilderName);
            Assert.Equal("John Sec Eng", entity.SecretaryNameEnglish);
            Assert.Equal("ABC Society Eng", entity.SocietyNameEnglish);
            Assert.Equal("123 Main St Eng", entity.SocietyAddressEnglish);
            Assert.Equal("Jane Mgr Eng", entity.ManagerNameEnglish);
            Assert.Equal("Land Owner Eng", entity.LandOwnerNameEnglish);
            Assert.Equal("Builder Eng", entity.BuilderNameEnglish);
            Assert.Equal("9876543210", entity.ManagerMobileNo);
            Assert.Equal("8765432109", entity.SecretaryMobileNo);
            Assert.Equal("society@example.com", entity.SocietyEmailId);
            Assert.Equal("secretary@example.com", entity.SecretaryEmailId);
            Assert.Equal("manager@example.com", entity.ManagerEmailId);
            Assert.False(entity.MarkedForDeletion);
            Assert.Null(entity.MarkedForDeletionDate);
            Assert.True(entity.IsActive);
            Assert.Equal(1, entity.CreatedBy);
            Assert.Equal(2, entity.UpdatedBy);
        }

        [Fact]
        public void SocietyDetailsEntity_OptionalFields_CanBeNull()
        {
            var entity = new SocietyDetailsEntity
            {
                Id = 1,
                IsActive = true,
                MarkedForDeletion = false
            };

            Assert.Null(entity.PropertyId);
            Assert.Null(entity.WingId);
            Assert.Null(entity.WingName);
            Assert.Null(entity.SocietyName);
            Assert.Null(entity.SocietyAddress);
            Assert.Null(entity.SecretaryName);
            Assert.Null(entity.ManagerName);
            Assert.Null(entity.LandOwnerName);
            Assert.Null(entity.BuilderName);
            Assert.Null(entity.SecretaryNameEnglish);
            Assert.Null(entity.SocietyNameEnglish);
            Assert.Null(entity.SocietyAddressEnglish);
            Assert.Null(entity.ManagerNameEnglish);
            Assert.Null(entity.LandOwnerNameEnglish);
            Assert.Null(entity.BuilderNameEnglish);
            Assert.Null(entity.ManagerMobileNo);
            Assert.Null(entity.SecretaryMobileNo);
            Assert.Null(entity.SocietyEmailId);
            Assert.Null(entity.SecretaryEmailId);
            Assert.Null(entity.ManagerEmailId);
            Assert.Null(entity.MarkedForDeletionDate);
        }

        [Fact]
        public void SocietyDetailsEntity_InheritsFromBaseEntity()
        {
            var entity = new SocietyDetailsEntity();
            Assert.IsAssignableFrom<BaseEntity>(entity);
        }

        [Fact]
        public void SocietyDetailsEntity_DefaultValues_SetCorrectly()
        {
            var entity = new SocietyDetailsEntity();

            Assert.Equal(0, entity.Id);
            Assert.False(entity.MarkedForDeletion);
            Assert.True(entity.IsActive);
        }

        [Fact]
        public void SocietyDetailsEntity_MarkedForDeletion_GetSet_WorksCorrectly()
        {
            var entity = new SocietyDetailsEntity
            {
                MarkedForDeletion = true,
                MarkedForDeletionDate = DateTime.Now
            };

            Assert.True(entity.MarkedForDeletion);
            Assert.NotNull(entity.MarkedForDeletionDate);
        }
    }

    #endregion

    #region PropertyRepository SocietyDetails Tests

    public class PropertyRepositorySocietyDetailsTests
    {
        [Fact]
        public async Task GetSocietyDetailsAsync_PropertyNotFound_ReturnsNull()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var repository = new PropertyRepository(context);

            var result = await repository.GetSocietyDetailsAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetSocietyDetailsAsync_PropertyExistsNoSociety_ReturnsEmptyDto()
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
                SocietyDetailId = null,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetSocietyDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            Assert.Null(result.SocietyDetailId);
        }

        [Fact]
        public async Task GetSocietyDetailsAsync_WithSocietyDetails_ReturnsCompleteDto()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var wing = new WingEntity { Id = 5, WingNo = "A", IsActive = true };
            var society = new SocietyDetailsEntity
            {
                Id = 100,
                PropertyId = 549357,
                WingId = 5,
                WingName = "West Wing",
                SocietyName = "ABC Society",
                IsActive = true,
                MarkedForDeletion = false
            };

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                SocietyDetailId = 100,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.WingEntity.Add(wing);
            context.SocietyDetailsMast.Add(society);
            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var result = await repository.GetSocietyDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            Assert.Equal(100, result.SocietyDetailId);
            Assert.Equal(5, result.WingId);
            Assert.Equal("A", result.WingNo);
            Assert.Equal("West Wing", result.WingName);
            Assert.Equal("ABC Society", result.SocietyName);
        }

        [Fact]
        public async Task UpdateSocietyDetailsAsync_PropertyNotFound_ReturnsNull()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var repository = new PropertyRepository(context);

            var dto = new UpdatePropertySocietyDetailsDto
            {
                SocietyName = "Test"
            };

            var result = await repository.UpdateSocietyDetailsAsync(999, dto);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateSocietyDetailsAsync_InvalidWingId_ThrowsException()
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
            var dto = new UpdatePropertySocietyDetailsDto
            {
                WingId = 999
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repository.UpdateSocietyDetailsAsync(549357, dto));

            Assert.Contains("Wing with ID 999 does not exist or is inactive", exception.Message);
        }

        [Fact]
        public async Task UpdateSocietyDetailsAsync_NoSocietyExists_CreatesNewSociety()
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
                SocietyDetailId = null,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertySocietyDetailsDto
            {
                SocietyName = "New Society",
                ManagerMobileNo = "9876543210"
            };

            var result = await repository.UpdateSocietyDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("New Society", result.SocietyName);
            Assert.Equal("9876543210", result.ManagerMobileNo);

            var societyCount = await context.SocietyDetailsMast.CountAsync();
            Assert.Equal(1, societyCount);
        }

        [Fact]
        public async Task UpdateSocietyDetailsAsync_SocietyExists_UpdatesSociety()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);

            var society = new SocietyDetailsEntity
            {
                Id = 100,
                PropertyId = 549357,
                SocietyName = "Old Society",
                IsActive = true,
                MarkedForDeletion = false
            };

            var property = new PropertyEntity
            {
                Id = 549357,
                WardId = 79,
                TaxZoneId = 10,
                SocietyDetailId = 100,
                IsActive = true,
                MarkedForDeletion = false
            };

            context.SocietyDetailsMast.Add(society);
            context.PropertyMast.Add(property);
            await context.SaveChangesAsync();

            var repository = new PropertyRepository(context);
            var dto = new UpdatePropertySocietyDetailsDto
            {
                SocietyName = "Updated Society",
                WingName = "New Wing"
            };

            var result = await repository.UpdateSocietyDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("Updated Society", result.SocietyName);
            Assert.Equal("New Wing", result.WingName);

            var societyCount = await context.SocietyDetailsMast.CountAsync();
            Assert.Equal(1, societyCount);
        }

        [Fact]
        public async Task UpdateSocietyDetailsAsync_UpdatesAllFields()
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
            var dto = new UpdatePropertySocietyDetailsDto
            {
                WingName = "Wing A",
                SocietyName = "Society Name",
                SocietyAddress = "Society Address",
                SecretaryName = "Secretary",
                ManagerName = "Manager",
                LandOwnerName = "Land Owner",
                BuilderName = "Builder",
                SocietyNameEnglish = "Society Eng",
                SocietyAddressEnglish = "Address Eng",
                SecretaryNameEnglish = "Secretary Eng",
                ManagerNameEnglish = "Manager Eng",
                LandOwnerNameEnglish = "Land Owner Eng",
                BuilderNameEnglish = "Builder Eng",
                ManagerMobileNo = "9876543210",
                SecretaryMobileNo = "8765432109",
                SocietyEmailId = "society@test.com",
                SecretaryEmailId = "secretary@test.com",
                ManagerEmailId = "manager@test.com"
            };

            var result = await repository.UpdateSocietyDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("Wing A", result.WingName);
            Assert.Equal("Society Name", result.SocietyName);
            Assert.Equal("Society Address", result.SocietyAddress);
            Assert.Equal("Secretary", result.SecretaryName);
            Assert.Equal("Manager", result.ManagerName);
            Assert.Equal("Land Owner", result.LandOwnerName);
            Assert.Equal("Builder", result.BuilderName);
            Assert.Equal("Society Eng", result.SocietyNameEnglish);
            Assert.Equal("Address Eng", result.SocietyAddressEnglish);
            Assert.Equal("Secretary Eng", result.SecretaryNameEnglish);
            Assert.Equal("Manager Eng", result.ManagerNameEnglish);
            Assert.Equal("Land Owner Eng", result.LandOwnerNameEnglish);
            Assert.Equal("Builder Eng", result.BuilderNameEnglish);
            Assert.Equal("9876543210", result.ManagerMobileNo);
            Assert.Equal("8765432109", result.SecretaryMobileNo);
            Assert.Equal("society@test.com", result.SocietyEmailId);
            Assert.Equal("secretary@test.com", result.SecretaryEmailId);
            Assert.Equal("manager@test.com", result.ManagerEmailId);
        }
    }

    #endregion

    #region PropertyService SocietyDetails Tests

    public class PropertyServiceSocietyDetailsTests
    {
        [Fact]
        public async Task GetSocietyDetailsAsync_CallsRepository()
        {
            var mockRepo = new Mock<IRepository<PropertyEntity, int>>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockMapper = new Mock<IMapper>();
            var mockPropertyRepo = new Mock<IPropertyRepository>();
            var mockLogger = new Mock<ILogger<PropertyService>>();

            var expectedDto = new PropertySocietyDetailsDto
            {
                PropertyId = 549357,
                SocietyDetailId = 100,
                SocietyName = "ABC Society"
            };

            mockPropertyRepo
                .Setup(r => r.GetSocietyDetailsAsync(549357, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedDto);

            var mockFeatureFlags = new Mock<IOptions<FeatureFlagsOptions>>();
            mockFeatureFlags.Setup(f => f.Value).Returns(new FeatureFlagsOptions
            {
                AllowPropertyDeletionWithoutPaymentValidation = true
            });
            var service = new PropertyService(mockRepo.Object, mockUnitOfWork.Object, mockMapper.Object, mockPropertyRepo.Object, mockLogger.Object, mockFeatureFlags.Object);

            var result = await service.GetSocietyDetailsAsync(549357);

            Assert.NotNull(result);
            Assert.Equal(549357, result.PropertyId);
            Assert.Equal(100, result.SocietyDetailId);
            Assert.Equal("ABC Society", result.SocietyName);
            mockPropertyRepo.Verify(r => r.GetSocietyDetailsAsync(549357, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateSocietyDetailsAsync_CallsRepositoryAndReturnsResult()
        {
            var mockRepo = new Mock<IRepository<PropertyEntity, int>>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockMapper = new Mock<IMapper>();
            var mockPropertyRepo = new Mock<IPropertyRepository>();
            var mockLogger = new Mock<ILogger<PropertyService>>();

            var dto = new UpdatePropertySocietyDetailsDto
            {
                SocietyName = "Updated Society"
            };

            var expectedResult = new PropertySocietyDetailsDto
            {
                PropertyId = 549357,
                SocietyName = "Updated Society"
            };

            mockPropertyRepo
                .Setup(r => r.UpdateSocietyDetailsAsync(549357, dto, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            var mockFeatureFlags = new Mock<IOptions<FeatureFlagsOptions>>();
            mockFeatureFlags.Setup(f => f.Value).Returns(new FeatureFlagsOptions
            {
                AllowPropertyDeletionWithoutPaymentValidation = true
            });
            var service = new PropertyService(mockRepo.Object, mockUnitOfWork.Object, mockMapper.Object, mockPropertyRepo.Object, mockLogger.Object, mockFeatureFlags.Object);

            var result = await service.UpdateSocietyDetailsAsync(549357, dto);

            Assert.NotNull(result);
            Assert.Equal("Updated Society", result.SocietyName);
            mockPropertyRepo.Verify(r => r.UpdateSocietyDetailsAsync(549357, dto, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    #endregion
}
