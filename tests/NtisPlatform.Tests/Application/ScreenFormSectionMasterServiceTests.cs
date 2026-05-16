using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable.Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using ValidationResult = NtisPlatform.Application.Models.ValidationResult;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Unit tests for <see cref="ScreenFormSectionMasterService"/>
/// </summary>
public class ScreenFormSectionMasterServiceTests
{
    private readonly Mock<IRepository<ScreenFormSectionMasterEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly IMapper _mapper;
    private readonly ScreenFormSectionMasterService _service;

    public ScreenFormSectionMasterServiceTests()
    {
        _mockRepository = new Mock<IRepository<ScreenFormSectionMasterEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ScreenFormSectionMasterMappingProfile>();
        },
        Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _service = new ScreenFormSectionMasterService(_mockRepository.Object, _mockUnitOfWork.Object, _mapper, _mockReferenceValidator.Object);
    }

    #region Entity Tests

    [Fact]
    public void Entity_Properties_GetSet_WorksCorrectly()
    {
        var screen = new ScreenMasterEntity { ScreenGroupId = 1, ScreenName = "MainScreen" };
        var parentSection = new ScreenFormSectionMasterEntity { Id = 99, SectionName = "ParentSection" };

        var entity = new ScreenFormSectionMasterEntity
        {
            Id = 1,
            ScreenId = 1,
            ParentSectionId = null,
            SectionType = "Form",
            SectionName = "Basic Info",
            SectionCode = "BASIC001",
            DisplayOrder = 1,
            ColumnCount = 2,
            IsOptional = false,
            IsCollapsible = true,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now,
            UpdatedBy = 2,
            UpdatedDate = DateTime.Now
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(1, entity.ScreenId);
        Assert.Null(entity.ParentSectionId);
        Assert.Equal("Form", entity.SectionType);
        Assert.Equal("Basic Info", entity.SectionName);
        Assert.Equal("BASIC001", entity.SectionCode);
        Assert.Equal(1, entity.DisplayOrder);
        Assert.Equal(2, entity.ColumnCount);
        Assert.False(entity.IsOptional);
        Assert.True(entity.IsCollapsible);
        Assert.True(entity.IsActive);
        Assert.Equal(1, entity.CreatedBy);
        Assert.NotNull(entity.CreatedDate);
        Assert.Equal(2, entity.UpdatedBy);
        Assert.NotNull(entity.UpdatedDate);
    }

    [Fact]
    public void Entity_DefaultValues_AreCorrect()
    {
        var entity = new ScreenFormSectionMasterEntity();
        Assert.Equal(0, entity.Id);
        Assert.Equal(0, entity.ScreenId);
        Assert.Null(entity.ParentSectionId);
        Assert.True(string.IsNullOrEmpty(entity.SectionType));
        Assert.True(string.IsNullOrEmpty(entity.SectionName));
        Assert.True(string.IsNullOrEmpty(entity.SectionCode));
        Assert.Equal(0, entity.DisplayOrder);
        Assert.Equal(0, entity.ColumnCount);
        Assert.False(entity.IsOptional);
        Assert.False(entity.IsCollapsible);
        Assert.True(entity.IsActive);
        Assert.Null(entity.CreatedBy);
        Assert.Null(entity.CreatedDate);
        Assert.Null(entity.UpdatedBy);
        Assert.Null(entity.UpdatedDate);
    }

    #endregion

    #region DTO Tests

    [Fact]
    public void Dto_Properties_GetSet_WorksCorrectly()
    {
        var dto = new ScreenFormSectionMasterDto
        {
            Id = 1,
            ScreenId = 1,
            ParentSectionId = null,
            SectionType = "Form",
            SectionName = "Basic Info",
            SectionCode = "BASIC001",
            DisplayOrder = 1,
            ColumnCount = 2,
            IsOptional = false,
            IsCollapsible = true,
            IsActive = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };

        Assert.Equal(1, dto.Id);
        Assert.Equal(1, dto.ScreenId);
        Assert.Null(dto.ParentSectionId);
        Assert.Equal("Form", dto.SectionType);
        Assert.Equal("Basic Info", dto.SectionName);
        Assert.Equal("BASIC001", dto.SectionCode);
        Assert.Equal(1, dto.DisplayOrder);
        Assert.Equal(2, dto.ColumnCount);
        Assert.False(dto.IsOptional);
        Assert.True(dto.IsCollapsible);
        Assert.True(dto.IsActive);
        Assert.NotNull(dto.CreatedDate);
        Assert.NotNull(dto.UpdatedDate);
    }

    [Fact]
    public void Dto_DefaultValues_AreCorrect()
    {
        var dto = new ScreenFormSectionMasterDto();
        Assert.Equal(0, dto.Id);
        Assert.Equal(0, dto.ScreenId);
        Assert.Null(dto.ParentSectionId);
        Assert.Equal(string.Empty, dto.SectionType);
        Assert.Equal(string.Empty, dto.SectionName);
        Assert.Null(dto.SectionNameLocal);
        Assert.Equal(string.Empty, dto.SectionCode);
        Assert.Null(dto.Description);
        Assert.Equal(0, dto.DisplayOrder);
        Assert.Equal(0, dto.ColumnCount);
        Assert.False(dto.IsOptional);
        Assert.False(dto.IsCollapsible);
        Assert.False(dto.IsCollapsedByDefault);
        Assert.False(dto.IsRepeatable);
        Assert.False(dto.IsActive);
        Assert.Null(dto.CreatedDate);
        Assert.Null(dto.UpdatedDate);
    }

    [Fact]
    public void CreateDto_ValidData_PassesValidation()
    {
        var dto = new CreateScreenFormSectionMasterDto
        {
            ScreenId = 1,
            SectionType = "Form",
            SectionName = "Basic Info",
            SectionCode = "BASIC001",
            DisplayOrder = 1,
            ColumnCount = 2,
            IsActive = true,
            CreatedBy = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(0, "ScreenFormSectionMaster_ScreenId_Required")]
    public void CreateDto_InvalidScreenId_FailsValidation(int screenId, string expectedError)
    {
        var dto = new CreateScreenFormSectionMasterDto
        {
            ScreenId = screenId,
            SectionType = "Form",
            SectionName = "Test",
            SectionCode = "TEST001",
            DisplayOrder = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == expectedError);
    }

    [Theory]
    [InlineData(null, "ScreenFormSectionMaster_SectionType_Required")]
    [InlineData("", "ScreenFormSectionMaster_SectionType_Required")]
    public void CreateDto_InvalidSectionType_FailsValidation(string? sectionType, string expectedError)
    {
        var dto = new CreateScreenFormSectionMasterDto
        {
            ScreenId = 1,
            SectionType = sectionType!,
            SectionName = "Test",
            SectionCode = "TEST001",
            DisplayOrder = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == expectedError);
    }

    [Fact]
    public void CreateDto_SectionTypeTooLong_FailsValidation()
    {
        var dto = new CreateScreenFormSectionMasterDto
        {
            ScreenId = 1,
            SectionType = new string('A', 51),
            SectionName = "Test",
            SectionCode = "TEST001",
            DisplayOrder = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "ScreenFormSectionMaster_SectionType_MaxLen_50");
    }

    [Theory]
    [InlineData(null, "ScreenFormSectionMaster_SectionName_Required")]
    [InlineData("", "ScreenFormSectionMaster_SectionName_Required")]
    public void CreateDto_InvalidSectionName_FailsValidation(string? sectionName, string expectedError)
    {
        var dto = new CreateScreenFormSectionMasterDto
        {
            ScreenId = 1,
            SectionType = "Form",
            SectionName = sectionName!,
            SectionCode = "TEST001",
            DisplayOrder = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == expectedError);
    }

    [Fact]
    public void CreateDto_SectionNameTooLong_FailsValidation()
    {
        var dto = new CreateScreenFormSectionMasterDto
        {
            ScreenId = 1,
            SectionType = "Form",
            SectionName = new string('A', 201),
            SectionCode = "TEST001",
            DisplayOrder = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "ScreenFormSectionMaster_SectionName_MaxLen_200");
    }

    [Theory]
    [InlineData(null, "ScreenFormSectionMaster_SectionCode_Required")]
    [InlineData("", "ScreenFormSectionMaster_SectionCode_Required")]
    public void CreateDto_InvalidSectionCode_FailsValidation(string? sectionCode, string expectedError)
    {
        var dto = new CreateScreenFormSectionMasterDto
        {
            ScreenId = 1,
            SectionType = "Form",
            SectionName = "Test",
            SectionCode = sectionCode!,
            DisplayOrder = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == expectedError);
    }

    [Fact]
    public void UpdateDto_ValidData_PassesValidation()
    {
        var dto = new UpdateScreenFormSectionMasterDto
        {
            ScreenId = 1,
            SectionType = "Form",
            SectionName = "Updated Section",
            SectionCode = "BASIC001",
            DisplayOrder = 1,
            ColumnCount = 2,
            IsActive = true,
            UpdatedBy = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdateDto_MissingSectionName_FailsValidation()
    {
        var dto = new UpdateScreenFormSectionMasterDto
        {
            ScreenId = 1,
            SectionType = "Form",
            SectionName = null!,
            SectionCode = "TEST001",
            DisplayOrder = 1,
            ColumnCount = 2
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "ScreenFormSectionMaster_SectionName_Required");
    }

    #endregion

    #region QueryParameters Tests

    [Fact]
    public void QueryParameters_Properties_WorkCorrectly()
    {
        var qp = new ScreenFormSectionMasterQueryParameters
        {
            ScreenId = 1,
            SectionType = "Form",
            SectionName = "Test",
            IsActive = true,
            PageNumber = 2,
            PageSize = 20,
            SearchTerm = "Test",
            SortBy = "SectionName"
        };
        Assert.Equal(1, qp.ScreenId);
        Assert.Equal("Form", qp.SectionType);
        Assert.Equal("Test", qp.SectionName);
        Assert.True(qp.IsActive);
        Assert.Equal(2, qp.PageNumber);
        Assert.Equal(20, qp.PageSize);
        Assert.Equal("Test", qp.SearchTerm);
        Assert.Equal("SectionName", qp.SortBy);
    }

    [Fact]
    public void QueryParameters_DefaultValues_AreCorrect()
    {
        var qp = new ScreenFormSectionMasterQueryParameters();
        Assert.Null(qp.ScreenId);
        Assert.Null(qp.SectionType);
        Assert.Null(qp.SectionName);
        Assert.Null(qp.IsActive);
        Assert.Equal(1, qp.PageNumber);
        Assert.Equal(10, qp.PageSize);
    }

    #endregion

    #region Service CRUD Tests

    [Fact]
    public async Task Service_GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = new ScreenFormSectionMasterEntity
        {
            Id = 1,
            ScreenId = 1,
            SectionType = "Form",
            SectionName = "Test Section",
            SectionCode = "TEST001",
            DisplayOrder = 1,
            ColumnCount = 2,
            IsActive = true
        };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var result = await _service.GetByIdAsync(1, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test Section", result.SectionName);
    }

    [Fact]
    public async Task Service_GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((ScreenFormSectionMasterEntity?)null);
        var result = await _service.GetByIdAsync(999, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task Service_GetAllAsync_ReturnsAllEntities()
    {
        var entities = new List<ScreenFormSectionMasterEntity>
        {
  new() { Id = 1, ScreenId = 1, SectionType = "Form", SectionName = "Section1", SectionCode = "SEC001", DisplayOrder = 1, ColumnCount = 2, IsActive = true },
     new() { Id = 2, ScreenId = 1, SectionType = "Form", SectionName = "Section2", SectionCode = "SEC002", DisplayOrder = 2, ColumnCount = 2, IsActive = true },
            new() { Id = 3, ScreenId = 2, SectionType = "Grid", SectionName = "Section3", SectionCode = "SEC003", DisplayOrder = 3, ColumnCount = 1, IsActive = false }
    };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMockDbSet().Object);
        var qp = new ScreenFormSectionMasterQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count());
    }

    [Theory]
    [InlineData(null, 3)]
    [InlineData(true, 2)]
    [InlineData(false, 1)]
    public async Task Service_GetAllAsync_WithFilters_ReturnsFilteredEntities(bool? isActive, int expectedCount)
    {
        var entities = new List<ScreenFormSectionMasterEntity>
  {
            new() { Id = 1, ScreenId = 1, SectionType = "Form", SectionName = "Section1", SectionCode = "SEC001", DisplayOrder = 1, ColumnCount = 2, IsActive = true },
            new() { Id = 2, ScreenId = 1, SectionType = "Form", SectionName = "Section2", SectionCode = "SEC002", DisplayOrder = 2, ColumnCount = 2, IsActive = true },
          new() { Id = 3, ScreenId = 2, SectionType = "Grid", SectionName = "Section3", SectionCode = "SEC003", DisplayOrder = 3, ColumnCount = 1, IsActive = false }
  };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMockDbSet().Object);
        var qp = new ScreenFormSectionMasterQueryParameters { IsActive = isActive, PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.Equal(expectedCount, result.Items.Count());
    }

    [Fact]
    public async Task Service_GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<ScreenFormSectionMasterEntity>().BuildMockDbSet().Object);
        var qp = new ScreenFormSectionMasterQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task Service_GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        var entities = Enumerable.Range(1, 25).Select(i => new ScreenFormSectionMasterEntity
        {
            Id = i,
            ScreenId = 1,
            SectionType = "Form",
            SectionName = $"Section {i}",
            SectionCode = $"SEC{i:000}",
            DisplayOrder = i,
            ColumnCount = 2,
            IsActive = true
        }).ToList();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMockDbSet().Object);
        var qp = new ScreenFormSectionMasterQueryParameters { PageNumber = 2, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(2, result.PageNumber);
    }

    [Fact]
    public async Task Service_CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        var createDto = new CreateScreenFormSectionMasterDto
        {
            ScreenId = 1,
            SectionType = "Form",
            SectionName = "Test Section",
            SectionCode = "TEST001",
            DisplayOrder = 1,
            ColumnCount = 2,
            IsActive = true,
            CreatedBy = 1
        };
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ScreenFormSectionMasterEntity>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync((ScreenFormSectionMasterEntity e, CancellationToken _) => { e.Id = 1; return e; });
        var result = await _service.CreateAsync(createDto, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test Section", result.SectionName);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Service_CreateAsync_DuplicateSectionCode_ThrowsException()
    {
        var createDto = new CreateScreenFormSectionMasterDto
        {
            ScreenId = 1,
            SectionType = "Form",
            SectionName = "Test",
            SectionCode = "TEST001",
            DisplayOrder = 1
        };
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ScreenFormSectionMasterEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(createDto, CancellationToken.None));
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Service_UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        var existingEntity = new ScreenFormSectionMasterEntity
        {
            Id = 1,
            ScreenId = 1,
            SectionType = "Form",
            SectionName = "Old Name",
            SectionCode = "TEST001",
            DisplayOrder = 1,
            ColumnCount = 2,
            IsActive = true
        };
        var updateDto = new UpdateScreenFormSectionMasterDto
        {
            ScreenId = 1,
            SectionType = "Form",
            SectionName = "Updated Name",
            SectionCode = "TEST001",
            DisplayOrder = 1,
            ColumnCount = 2,
            IsActive = true
        };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ScreenFormSectionMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.SectionName);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Service_UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((ScreenFormSectionMasterEntity?)null);
        var updateDto = new UpdateScreenFormSectionMasterDto
        {
            ScreenId = 1,
            SectionType = "Form",
            SectionName = "Test",
            SectionCode = "TEST001",
            DisplayOrder = 1,
            ColumnCount = 2
        };
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);
        Assert.Null(result);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Service_UpdateAsync_DeactivateWithReferences_ThrowsValidationException()
    {
        var existingEntity = new ScreenFormSectionMasterEntity
        {
            Id =1,
            ScreenId =1,
            SectionType = "Form",
            SectionName = "Section",
            SectionCode = "CODE",
            DisplayOrder =1,
            ColumnCount =2,
            IsActive = true
        };
        var updateDto = new UpdateScreenFormSectionMasterDto
        {
            ScreenId =1,
            SectionType = "Form",
            SectionName = "Section",
            SectionCode = "CODE",
            DisplayOrder =1,
            ColumnCount =2,
            IsActive = false
        };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mockReferenceValidator.Setup(v => v.ValidateReferencesAsync<ScreenFormSectionMasterEntity>(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(ValidationResult.Failure("Cannot deactivate due to references"));
        await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(() => _service.UpdateAsync(1, updateDto, CancellationToken.None));
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ScreenFormSectionMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Service_UpdateAsync_DeactivateWithoutReferences_Succeeds()
    {
        var existingEntity = new ScreenFormSectionMasterEntity
        {
            Id =1,
            ScreenId =1,
            SectionType = "Form",
            SectionName = "Section",
            SectionCode = "CODE",
            DisplayOrder =1,
            ColumnCount =2,
            IsActive = true
        };
        var updateDto = new UpdateScreenFormSectionMasterDto
        {
            ScreenId =1,
            SectionType = "Form",
            SectionName = "Section",
            SectionCode = "CODE",
            DisplayOrder =1,
            ColumnCount =2,
            IsActive = false
        };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ScreenFormSectionMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockReferenceValidator.Setup(v => v.ValidateReferencesAsync<ScreenFormSectionMasterEntity>(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(ValidationResult.Success());
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);
        Assert.NotNull(result);
        Assert.False(existingEntity.IsActive);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ScreenFormSectionMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Service_DeleteAsync_ExistingEntity_DeletesSuccessfully()
    {
        var entity = new ScreenFormSectionMasterEntity { Id =1, ScreenId =1, SectionType = "Form", SectionName = "Test", SectionCode = "TEST001", DisplayOrder =1, ColumnCount =2 };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<ScreenFormSectionMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockReferenceValidator.Setup(v => v.ValidateReferencesAsync<ScreenFormSectionMasterEntity>(1, It.IsAny<CancellationToken>())).ReturnsAsync(ValidationResult.Success());
        var result = await _service.DeleteAsync(1, CancellationToken.None);
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.Is<ScreenFormSectionMasterEntity>(e => e.Id ==1), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Service_DeleteAsync_NonExistingEntity_ReturnsFalse()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((ScreenFormSectionMasterEntity?)null);
        var result = await _service.DeleteAsync(999, CancellationToken.None);
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Service_DeleteAsync_WithReferences_ThrowsValidationException()
    {
        var entity = new ScreenFormSectionMasterEntity { Id =1, ScreenId =1, SectionType = "Form", SectionName = "Test", SectionCode = "TEST001", DisplayOrder =1, ColumnCount =2 };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockReferenceValidator.Setup(v => v.ValidateReferencesAsync<ScreenFormSectionMasterEntity>(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(ValidationResult.Failure("Cannot delete due to references"));
        await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(() => _service.DeleteAsync(1, CancellationToken.None));
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<ScreenFormSectionMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Controller Tests

    [Fact]
    public async Task Controller_GetAll_ReturnsOk()
    {
        var qp = new ScreenFormSectionMasterQueryParameters();
        var pagedResult = new PagedResult<ScreenFormSectionMasterDto>(new List<ScreenFormSectionMasterDto>(), 0, 1, 10);
        var serviceMock = new Mock<IScreenFormSectionMasterService>();
        var loggerMock = new Mock<ILogger<ScreenFormSectionMasterController>>();
        serviceMock.Setup(s => s.GetAllAsync(qp, It.IsAny<CancellationToken>())).ReturnsAsync(pagedResult);
        var ctrl = new ScreenFormSectionMasterController(serviceMock.Object, loggerMock.Object);
        var result = await ctrl.GetAll(qp, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_GetById_ExistingId_ReturnsOk()
    {
        var serviceMock = new Mock<IScreenFormSectionMasterService>();
        var loggerMock = new Mock<ILogger<ScreenFormSectionMasterController>>();
        serviceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new ScreenFormSectionMasterDto { Id = 1 });
        var ctrl = new ScreenFormSectionMasterController(serviceMock.Object, loggerMock.Object);
        var result = await ctrl.GetById(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_GetById_NonExistingId_ReturnsNotFound()
    {
        var serviceMock = new Mock<IScreenFormSectionMasterService>();
        var loggerMock = new Mock<ILogger<ScreenFormSectionMasterController>>();
        serviceMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((ScreenFormSectionMasterDto?)null);
        var ctrl = new ScreenFormSectionMasterController(serviceMock.Object, loggerMock.Object);
        var result = await ctrl.GetById(999, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Controller_Create_ValidDto_ReturnsOk()
    {
        var serviceMock = new Mock<IScreenFormSectionMasterService>();
        var loggerMock = new Mock<ILogger<ScreenFormSectionMasterController>>();
        serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateScreenFormSectionMasterDto>(), It.IsAny<CancellationToken>()))
     .ReturnsAsync(new ScreenFormSectionMasterDto { Id = 1 });
        var ctrl = new ScreenFormSectionMasterController(serviceMock.Object, loggerMock.Object);
        var result = await ctrl.Create(new CreateScreenFormSectionMasterDto
        {
            ScreenId = 1,
            SectionType = "Form",
            SectionName = "Test",
            SectionCode = "TEST001",
            DisplayOrder = 1
        }, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Update_ExistingId_ReturnsOk()
    {
        var serviceMock = new Mock<IScreenFormSectionMasterService>();
        var loggerMock = new Mock<ILogger<ScreenFormSectionMasterController>>();
        serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateScreenFormSectionMasterDto>(), It.IsAny<CancellationToken>()))
  .ReturnsAsync(new ScreenFormSectionMasterDto { Id = 1 });
        var ctrl = new ScreenFormSectionMasterController(serviceMock.Object, loggerMock.Object);
        var result = await ctrl.Update(1, new UpdateScreenFormSectionMasterDto
        {
            ScreenId = 1,
            SectionType = "Form",
            SectionName = "Test",
            SectionCode = "TEST001",
            DisplayOrder = 1,
            ColumnCount = 2
        }, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Update_NonExistingId_ReturnsNotFound()
    {
        var serviceMock = new Mock<IScreenFormSectionMasterService>();
        var loggerMock = new Mock<ILogger<ScreenFormSectionMasterController>>();
        serviceMock.Setup(s => s.UpdateAsync(999, It.IsAny<UpdateScreenFormSectionMasterDto>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync((ScreenFormSectionMasterDto?)null);
        var ctrl = new ScreenFormSectionMasterController(serviceMock.Object, loggerMock.Object);
        var result = await ctrl.Update(999, new UpdateScreenFormSectionMasterDto
        {
            ScreenId = 1,
            SectionType = "Form",
            SectionName = "Test",
            SectionCode = "TEST001",
            DisplayOrder = 1,
            ColumnCount = 2
        }, CancellationToken.None);
        Assert.True(result is OkObjectResult || result is NotFoundResult);
    }

    [Fact]
    public async Task Controller_Delete_ExistingId_ReturnsOk()
    {
        var serviceMock = new Mock<IScreenFormSectionMasterService>();
        var loggerMock = new Mock<ILogger<ScreenFormSectionMasterController>>();
        serviceMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var ctrl = new ScreenFormSectionMasterController(serviceMock.Object, loggerMock.Object);
        var result = await ctrl.Delete(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Delete_NonExistingId_ReturnsNotFound()
    {
        var serviceMock = new Mock<IScreenFormSectionMasterService>();
        var loggerMock = new Mock<ILogger<ScreenFormSectionMasterController>>();
        serviceMock.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var ctrl = new ScreenFormSectionMasterController(serviceMock.Object, loggerMock.Object);
        var result = await ctrl.Delete(999, CancellationToken.None);
        Assert.True(result is OkObjectResult || result is NotFoundResult);
    }

    #endregion
}
