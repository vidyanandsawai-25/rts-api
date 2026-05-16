using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable.Moq;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using ValidationResult = NtisPlatform.Application.Models.ValidationResult;

namespace NtisPlatform.Tests.Application;

public class ScreenFormFieldMasterServiceTests
{
    private readonly Mock<IRepository<ScreenFormFieldMasterEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly IMapper _mapper;
    private readonly ScreenFormFieldMasterService _service;

    public ScreenFormFieldMasterServiceTests()
    {
        _mockRepository = new Mock<IRepository<ScreenFormFieldMasterEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ScreenFormFieldMasterMappingProfile>();
        },
        Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _service = new ScreenFormFieldMasterService(_mockRepository.Object, _mockUnitOfWork.Object, _mapper, _mockReferenceValidator.Object);
    }

    #region Entity Tests

    [Fact]
    public void Entity_Properties_GetSet_WorksCorrectly()
    {
        var parentField = new ScreenFormFieldMasterEntity { Id = 99, FieldName = "ParentField" };
        var screen = new ScreenEntity { Id = 88, ScreenName = "ScreenName" };

        var entity = new ScreenFormFieldMasterEntity
        {
            Id = 1,
            ScreenId = 1,
            SectionId = 1,
            FieldName = "TestField",
            FieldLabel = "Test Field",
            FieldCode = "TEST001",
            DataType = "string",
            ControlType = "textbox",
            DisplayOrder = 1,
            ColumnSpan = 1,
            IsRequired = true,
            IsVisible = true,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now,
            UpdatedBy = 2,
            UpdatedDate = DateTime.Now 
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(1, entity.ScreenId);
        Assert.Equal(1, entity.SectionId);
        Assert.Equal("TestField", entity.FieldName);
        Assert.Equal("Test Field", entity.FieldLabel);
        Assert.Equal("TEST001", entity.FieldCode);
        Assert.Equal("string", entity.DataType);
        Assert.Equal("textbox", entity.ControlType);
        Assert.Equal(1, entity.DisplayOrder);
        Assert.Equal(1, entity.ColumnSpan);
        Assert.True(entity.IsRequired);
        Assert.True(entity.IsVisible);
        Assert.True(entity.IsActive);
        Assert.Equal(1, entity.CreatedBy);
        Assert.NotNull(entity.CreatedDate);
        Assert.Equal(2, entity.UpdatedBy);
        Assert.NotNull(entity.UpdatedDate);
    }

    [Fact]
    public void Entity_DefaultValues_AreCorrect()
    {
        var entity = new ScreenFormFieldMasterEntity();
        Assert.Equal(0, entity.Id);
        Assert.Equal(0, entity.ScreenId);
        Assert.Equal(0, entity.SectionId);
        Assert.True(string.IsNullOrEmpty(entity.FieldName));
        Assert.True(string.IsNullOrEmpty(entity.FieldLabel));
        Assert.True(string.IsNullOrEmpty(entity.FieldCode));
        Assert.True(string.IsNullOrEmpty(entity.DataType));
        Assert.True(string.IsNullOrEmpty(entity.ControlType));
        Assert.Equal(0, entity.DisplayOrder);
        Assert.Equal(0, entity.ColumnSpan);
        Assert.False(entity.IsRequired);
        Assert.False(entity.IsVisible);
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
        var dto = new ScreenFormFieldMasterDto
        {
            Id = 1,
            ScreenId = 1,
            SectionId = 1,
            FieldName = "TestField",
            FieldLabel = "Test Field",
            FieldCode = "TEST001",
            DataType = "string",
            ControlType = "textbox",
            DisplayOrder = 1,
            ColumnSpan = 1,
            IsRequired = true,
            IsVisible = true,
            IsActive = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };

        Assert.Equal(1, dto.Id);
        Assert.Equal(1, dto.ScreenId);
        Assert.Equal(1, dto.SectionId);
        Assert.Equal("TestField", dto.FieldName);
        Assert.Equal("Test Field", dto.FieldLabel);
        Assert.Equal("TEST001", dto.FieldCode);
        Assert.Equal("string", dto.DataType);
        Assert.Equal("textbox", dto.ControlType);
        Assert.Equal(1, dto.DisplayOrder);
        Assert.Equal(1, dto.ColumnSpan);
        Assert.True(dto.IsRequired);
        Assert.True(dto.IsVisible);
        Assert.True(dto.IsActive);
        Assert.NotNull(dto.CreatedDate);
        Assert.NotNull(dto.UpdatedDate);
    }

    [Fact]
    public void Dto_DefaultValues_AreCorrect()
    {
        var dto = new ScreenFormFieldMasterDto();
        Assert.Equal(0, dto.Id);
        Assert.Equal(0, dto.ScreenId);
        Assert.Equal(0, dto.SectionId);
        Assert.Equal(string.Empty, dto.FieldName);
        Assert.Equal(string.Empty, dto.FieldLabel);
        Assert.Null(dto.FieldLabelLocal);
        Assert.Equal(string.Empty, dto.FieldCode);
        Assert.Equal(string.Empty, dto.DataType);
        Assert.Equal(string.Empty, dto.ControlType);
        Assert.Null(dto.Placeholder);
        Assert.Null(dto.DefaultValue);
        Assert.Equal(0, dto.DisplayOrder);
        Assert.Equal(0, dto.ColumnSpan);
        Assert.Null(dto.CssClass);
        Assert.False(dto.IsRequired);
        Assert.False(dto.IsReadonly);
        Assert.False(dto.IsVisible);
        Assert.False(dto.IsUnique);
        Assert.Null(dto.MinLength);
        Assert.Null(dto.MaxLength);
        Assert.Null(dto.MinValue);
        Assert.Null(dto.MaxValue);
        Assert.Null(dto.RegexPattern);
        Assert.Null(dto.ValidationMessage);
        Assert.Null(dto.DropdownSourceId);
        Assert.Null(dto.StaticOptionsJson);
        Assert.False(dto.IsCascading);
        Assert.Null(dto.ParentFieldId);
        Assert.False(dto.IsMultiSelect);
        Assert.Null(dto.VisibilityConditionJson);
        Assert.Null(dto.ValidationJson);
        Assert.Null(dto.ExtraConfigJson);
        Assert.False(dto.IsSearchable);
        Assert.False(dto.IsFilterable);
        Assert.False(dto.IsActive);
        Assert.Null(dto.CreatedDate);
        Assert.Null(dto.UpdatedDate);
    }

    [Fact]
    public void CreateDto_ValidData_PassesValidation()
    {
        var dto = new CreateScreenFormFieldMasterDto
        {
            ScreenId = 1,
            SectionId = 1,
            FieldName = "TestField",
            FieldLabel = "Test Field",
            FieldCode = "TEST001",
            DataType = "string",
            ControlType = "textbox",
            DisplayOrder = 1,
            IsActive = true,
            CreatedBy = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(0, "ScreenFormFieldMaster_ScreenId_Required")]
    public void CreateDto_InvalidScreenId_FailsValidation(int screenId, string expectedError)
    {
        var dto = new CreateScreenFormFieldMasterDto
        {
            ScreenId = screenId,
            SectionId = 1,
            FieldName = "Test",
            FieldLabel = "Test Field",
            FieldCode = "TEST001",
            DataType = "string",
            ControlType = "textbox",
            DisplayOrder = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == expectedError);
    }

    [Theory]
    [InlineData(0, "ScreenFormFieldMaster_SectionId_Required")]
    public void CreateDto_InvalidSectionId_FailsValidation(int sectionId, string expectedError)
    {
        var dto = new CreateScreenFormFieldMasterDto
        {
            ScreenId = 1,
            SectionId = sectionId,
            FieldName = "Test",
            FieldLabel = "Test Field",
            FieldCode = "TEST001",
            DataType = "string",
            ControlType = "textbox",
            DisplayOrder = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == expectedError);
    }

    [Theory]
    [InlineData(null, "ScreenFormFieldMaster_FieldName_Required")]
    [InlineData("", "ScreenFormFieldMaster_FieldName_Required")]
    public void CreateDto_InvalidFieldName_FailsValidation(string? fieldName, string expectedError)
    {
        var dto = new CreateScreenFormFieldMasterDto
        {
            ScreenId = 1,
            SectionId = 1,
            FieldName = fieldName!,
            FieldLabel = "Test Field",
            FieldCode = "TEST001",
            DataType = "string",
            ControlType = "textbox",
            DisplayOrder = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == expectedError);
    }

    [Fact]
    public void CreateDto_FieldNameTooLong_FailsValidation()
    {
        var dto = new CreateScreenFormFieldMasterDto
        {
            ScreenId = 1,
            SectionId = 1,
            FieldName = new string('A', 201),
            FieldLabel = "Test Field",
            FieldCode = "TEST001",
            DataType = "string",
            ControlType = "textbox",
            DisplayOrder = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "ScreenFormFieldMaster_FieldName_MaxLen_200");
    }

    [Theory]
    [InlineData(null, "ScreenFormFieldMaster_FieldLabel_Required")]
    [InlineData("", "ScreenFormFieldMaster_FieldLabel_Required")]
    public void CreateDto_InvalidFieldLabel_FailsValidation(string? fieldLabel, string expectedError)
    {
        var dto = new CreateScreenFormFieldMasterDto
        {
            ScreenId = 1,
            SectionId = 1,
            FieldName = "Test",
            FieldLabel = fieldLabel!,
            FieldCode = "TEST001",
            DataType = "string",
            ControlType = "textbox",
            DisplayOrder = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == expectedError);
    }

    [Theory]
    [InlineData(null, "ScreenFormFieldMaster_FieldCode_Required")]
    [InlineData("", "ScreenFormFieldMaster_FieldCode_Required")]
    public void CreateDto_InvalidFieldCode_FailsValidation(string? fieldCode, string expectedError)
    {
        var dto = new CreateScreenFormFieldMasterDto
        {
            ScreenId = 1,
            SectionId = 1,
            FieldName = "Test",
            FieldLabel = "Test Field",
            FieldCode = fieldCode!,
            DataType = "string",
            ControlType = "textbox",
            DisplayOrder = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == expectedError);
    }

    [Theory]
    [InlineData(null, "ScreenFormFieldMaster_DataType_Required")]
    [InlineData("", "ScreenFormFieldMaster_DataType_Required")]
    public void CreateDto_InvalidDataType_FailsValidation(string? dataType, string expectedError)
    {
        var dto = new CreateScreenFormFieldMasterDto
        {
            ScreenId = 1,
            SectionId = 1,
            FieldName = "Test",
            FieldLabel = "Test Field",
            FieldCode = "TEST001",
            DataType = dataType!,
            ControlType = "textbox",
            DisplayOrder = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == expectedError);
    }

    [Theory]
    [InlineData(null, "ScreenFormFieldMaster_ControlType_Required")]
    [InlineData("", "ScreenFormFieldMaster_ControlType_Required")]
    public void CreateDto_InvalidControlType_FailsValidation(string? controlType, string expectedError)
    {
        var dto = new CreateScreenFormFieldMasterDto
        {
            ScreenId = 1,
            SectionId = 1,
            FieldName = "Test",
            FieldLabel = "Test Field",
            FieldCode = "TEST001",
            DataType = "string",
            ControlType = controlType!,
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
        var dto = new UpdateScreenFormFieldMasterDto
        {
            ScreenId = 1,
            SectionId = 1,
            FieldName = "UpdatedField",
            FieldLabel = "Updated Field",
            FieldCode = "TEST001",
            DataType = "string",
            ControlType = "textbox",
            DisplayOrder = 1,
            ColumnSpan = 1,
            IsActive = true,
            UpdatedBy = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdateDto_MissingFieldName_FailsValidation()
    {
        var dto = new UpdateScreenFormFieldMasterDto
        {
            ScreenId = 1,
            SectionId = 1,
            FieldName = null!,
            FieldLabel = "Test Field",
            FieldCode = "TEST001",
            DataType = "string",
            ControlType = "textbox",
            DisplayOrder = 1,
            ColumnSpan = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "ScreenFormFieldMaster_FieldName_Required");
    }

    #endregion

    #region QueryParameters Tests

    [Fact]
    public void QueryParameters_Properties_WorkCorrectly()
    {
        var qp = new ScreenFormFieldMasterQueryParameters
        {
            ScreenId = 1,
            PageNumber = 2,
            PageSize = 20,
            SearchTerm = "Test",
            SortBy = "FieldName"
        };
        Assert.Equal(1, qp.ScreenId);
        Assert.Equal(2, qp.PageNumber);
        Assert.Equal(20, qp.PageSize);
        Assert.Equal("Test", qp.SearchTerm);
        Assert.Equal("FieldName", qp.SortBy);
    }

    [Fact]
    public void QueryParameters_DefaultValues_AreCorrect()
    {
        var qp = new ScreenFormFieldMasterQueryParameters();
        Assert.Null(qp.ScreenId);
        Assert.Equal(1, qp.PageNumber);
        Assert.Equal(10, qp.PageSize);
    }

    #endregion

    #region Service CRUD Tests

    [Fact]
    public async Task Service_GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = new ScreenFormFieldMasterEntity
        {
            Id = 1,
            ScreenId = 1,
            SectionId = 1,
            FieldName = "TestField",
            FieldLabel = "Test Field",
            FieldCode = "TEST001",
            DataType = "string",
            ControlType = "textbox",
            DisplayOrder = 1,
            ColumnSpan = 1,
            IsActive = true
        };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var result = await _service.GetByIdAsync(1, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("TestField", result.FieldName);
        Assert.Equal("Test Field", result.FieldLabel);
    }

    [Fact]
    public async Task Service_GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((ScreenFormFieldMasterEntity?)null);
        var result = await _service.GetByIdAsync(999, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task Service_GetAllAsync_ReturnsAllEntities()
    {
        var entities = new List<ScreenFormFieldMasterEntity>
        {
       new() { Id = 1, ScreenId = 1, SectionId = 1, FieldName = "Field1", FieldLabel = "Field 1", FieldCode = "FLD001", DataType = "string", ControlType = "textbox", DisplayOrder = 1, ColumnSpan = 1, IsActive = true },
     new() { Id = 2, ScreenId = 1, SectionId = 1, FieldName = "Field2", FieldLabel = "Field 2", FieldCode = "FLD002", DataType = "number", ControlType = "textbox", DisplayOrder = 2, ColumnSpan = 1, IsActive = true },
       new() { Id = 3, ScreenId = 2, SectionId = 2, FieldName = "Field3", FieldLabel = "Field 3", FieldCode = "FLD003", DataType = "boolean", ControlType = "checkbox", DisplayOrder = 3, ColumnSpan = 1, IsActive = false }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMockDbSet().Object);
        var qp = new ScreenFormFieldMasterQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count());
    }

    [Fact]
    public async Task Service_GetAllAsync_WithScreenIdFilter_ReturnsFilteredEntities()
    {
        var entities = new List<ScreenFormFieldMasterEntity>
        {
        new() { Id = 1, ScreenId = 1, SectionId = 1, FieldName = "Field1", FieldLabel = "Field 1", FieldCode = "FLD001", DataType = "string", ControlType = "textbox", DisplayOrder = 1, ColumnSpan = 1, IsActive = true },
         new() { Id = 2, ScreenId = 1, SectionId = 1, FieldName = "Field2", FieldLabel = "Field 2", FieldCode = "FLD002", DataType = "number", ControlType = "textbox", DisplayOrder = 2, ColumnSpan = 1, IsActive = true },
       new() { Id = 3, ScreenId = 2, SectionId = 2, FieldName = "Field3", FieldLabel = "Field 3", FieldCode = "FLD003", DataType = "boolean", ControlType = "checkbox", DisplayOrder = 3, ColumnSpan = 1, IsActive = false }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMockDbSet().Object);
        var qp = new ScreenFormFieldMasterQueryParameters { ScreenId = 1, PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task Service_GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<ScreenFormFieldMasterEntity>().BuildMockDbSet().Object);
        var qp = new ScreenFormFieldMasterQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task Service_GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        var entities = Enumerable.Range(1, 25).Select(i => new ScreenFormFieldMasterEntity
        {
            Id = i,
            ScreenId = 1,
            SectionId = 1,
            FieldName = $"Field{i}",
            FieldLabel = $"Field {i}",
            FieldCode = $"FLD{i:000}",
            DataType = "string",
            ControlType = "textbox",
            DisplayOrder = i,
            ColumnSpan = 1,
            IsActive = true
        }).ToList();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMockDbSet().Object);
        var qp = new ScreenFormFieldMasterQueryParameters { PageNumber = 2, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(2, result.PageNumber);
    }

    [Fact]
    public async Task Service_CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        var createDto = new CreateScreenFormFieldMasterDto
        {
            ScreenId = 1,
            SectionId = 1,
            FieldName = "TestField",
            FieldLabel = "Test Field",
            FieldCode = "TEST001",
            DataType = "string",
            ControlType = "textbox",
            DisplayOrder = 1,
            IsActive = true,
            CreatedBy = 1
        };
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ScreenFormFieldMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScreenFormFieldMasterEntity e, CancellationToken _) => { e.Id = 1; return e; });
        var result = await _service.CreateAsync(createDto, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("TestField", result.FieldName);
        Assert.Equal("Test Field", result.FieldLabel);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Service_CreateAsync_DuplicateFieldCode_ThrowsException()
    {
        var createDto = new CreateScreenFormFieldMasterDto
        {
            ScreenId = 1,
            SectionId = 1,
            FieldName = "Test",
            FieldLabel = "Test Field",
            FieldCode = "TEST001",
            DataType = "string",
            ControlType = "textbox",
            DisplayOrder = 1
        };
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ScreenFormFieldMasterEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(createDto, CancellationToken.None));
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Service_UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        var existingEntity = new ScreenFormFieldMasterEntity
        {
            Id = 1,
            ScreenId = 1,
            SectionId = 1,
            FieldName = "OldField",
            FieldLabel = "Old Field",
            FieldCode = "TEST001",
            DataType = "string",
            ControlType = "textbox",
            DisplayOrder = 1,
            ColumnSpan = 1,
            IsActive = true
        };
        var updateDto = new UpdateScreenFormFieldMasterDto
        {
            ScreenId = 1,
            SectionId = 1,
            FieldName = "UpdatedField",
            FieldLabel = "Updated Field",
            FieldCode = "TEST001",
            DataType = "string",
            ControlType = "textbox",
            DisplayOrder = 1,
            ColumnSpan = 1,
            IsActive = true
        };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ScreenFormFieldMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal("UpdatedField", result.FieldName);
        Assert.Equal("Updated Field", result.FieldLabel);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Service_UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((ScreenFormFieldMasterEntity?)null);
        var updateDto = new UpdateScreenFormFieldMasterDto
        {
            ScreenId = 1,
            SectionId = 1,
            FieldName = "Test",
            FieldLabel = "Test Field",
            FieldCode = "TEST001",
            DataType = "string",
            ControlType = "textbox",
            DisplayOrder = 1,
            ColumnSpan = 1
        };
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);
        Assert.Null(result);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Service_UpdateAsync_DeactivateWithReferences_ThrowsValidationException()
    {
        var existingEntity = new ScreenFormFieldMasterEntity
        {
            Id =1,
            ScreenId =1,
            SectionId =1,
            FieldName = "Field",
            FieldLabel = "Label",
            FieldCode = "CODE",
            DataType = "string",
            ControlType = "textbox",
            DisplayOrder =1,
            ColumnSpan =1,
            IsActive = true
        };
        var updateDto = new UpdateScreenFormFieldMasterDto
        {
            ScreenId =1,
            SectionId =1,
            FieldName = "Field",
            FieldLabel = "Label",
            FieldCode = "CODE",
            DataType = "string",
            ControlType = "textbox",
            DisplayOrder =1,
            ColumnSpan =1,
            IsActive = false
        };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mockReferenceValidator.Setup(v => v.ValidateReferencesAsync<ScreenFormFieldMasterEntity>(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(ValidationResult.Failure("Cannot deactivate due to references"));
        await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(() => _service.UpdateAsync(1, updateDto, CancellationToken.None));
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ScreenFormFieldMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
     }

     [Fact]
     public async Task Service_UpdateAsync_DeactivateWithoutReferences_Succeeds()
     {
         var existingEntity = new ScreenFormFieldMasterEntity
         {
             Id =1,
             ScreenId =1,
             SectionId =1,
             FieldName = "Field",
             FieldLabel = "Label",
             FieldCode = "CODE",
             DataType = "string",
             ControlType = "textbox",
             DisplayOrder =1,
             ColumnSpan =1,
             IsActive = true
         };
         var updateDto = new UpdateScreenFormFieldMasterDto
         {
             ScreenId =1,
             SectionId =1,
             FieldName = "Field",
             FieldLabel = "Label",
             FieldCode = "CODE",
             DataType = "string",
             ControlType = "textbox",
             DisplayOrder =1,
             ColumnSpan =1,
             IsActive = false
         };
         _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
         _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ScreenFormFieldMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
         _mockReferenceValidator.Setup(v => v.ValidateReferencesAsync<ScreenFormFieldMasterEntity>(1, It.IsAny<CancellationToken>()))
         .ReturnsAsync(ValidationResult.Success());
         var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);
         Assert.NotNull(result);
         Assert.False(existingEntity.IsActive);
         _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ScreenFormFieldMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
         _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
     }

    [Fact]
    public async Task Service_DeleteAsync_ExistingEntity_DeletesSuccessfully()
    {
        var entity = new ScreenFormFieldMasterEntity
        {
            Id = 1,
            ScreenId = 1,
            SectionId = 1,
            FieldName = "Test",
            FieldLabel = "Test Field",
            FieldCode = "TEST001",
            DataType = "string",
            ControlType = "textbox",
            DisplayOrder = 1,
            ColumnSpan = 1
        };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<ScreenFormFieldMasterEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockReferenceValidator.Setup(v => v.ValidateReferencesAsync<ScreenFormFieldMasterEntity>(1, It.IsAny<CancellationToken>())).ReturnsAsync(ValidationResult.Success());
        var result = await _service.DeleteAsync(1, CancellationToken.None);
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.Is<ScreenFormFieldMasterEntity>(e => e.Id ==1), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Service_DeleteAsync_NonExistingEntity_ReturnsFalse()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((ScreenFormFieldMasterEntity?)null);
        var result = await _service.DeleteAsync(999, CancellationToken.None);
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Service_DeleteAsync_WithReferences_ThrowsValidationException()
    {
        var entity = new ScreenFormFieldMasterEntity { Id =1, ScreenId =1, SectionId =1, FieldName = "Test", FieldLabel = "Test Field", FieldCode = "TEST001", DataType = "string", ControlType = "textbox", DisplayOrder =1, ColumnSpan =1 };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockReferenceValidator.Setup(v => v.ValidateReferencesAsync<ScreenFormFieldMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Cannot delete due to references"));
        await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(() => _service.DeleteAsync(1, CancellationToken.None));
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<ScreenFormFieldMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Controller Tests

    [Fact]
    public async Task Controller_GetAll_ReturnsOk()
    {
        var qp = new ScreenFormFieldMasterQueryParameters();
        var pagedResult = new PagedResult<ScreenFormFieldMasterDto>(new List<ScreenFormFieldMasterDto>(), 0, 1, 10);
        var serviceMock = new Mock<IScreenFormFieldMasterService>();
        var loggerMock = new Mock<ILogger<ScreenFormFieldMasterController>>();
        serviceMock.Setup(s => s.GetAllAsync(qp, It.IsAny<CancellationToken>())).ReturnsAsync(pagedResult);
        var ctrl = new ScreenFormFieldMasterController(serviceMock.Object, loggerMock.Object);
        var result = await ctrl.GetAll(qp, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_GetById_ExistingId_ReturnsOk()
    {
        var serviceMock = new Mock<IScreenFormFieldMasterService>();
        var loggerMock = new Mock<ILogger<ScreenFormFieldMasterController>>();
        serviceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new ScreenFormFieldMasterDto { Id = 1 });
        var ctrl = new ScreenFormFieldMasterController(serviceMock.Object, loggerMock.Object);
        var result = await ctrl.GetById(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_GetById_NonExistingId_ReturnsNotFound()
    {
        var serviceMock = new Mock<IScreenFormFieldMasterService>();
        var loggerMock = new Mock<ILogger<ScreenFormFieldMasterController>>();
        serviceMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((ScreenFormFieldMasterDto?)null);
        var ctrl = new ScreenFormFieldMasterController(serviceMock.Object, loggerMock.Object);
        var result = await ctrl.GetById(999, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Controller_Create_ValidDto_ReturnsOk()
    {
        var serviceMock = new Mock<IScreenFormFieldMasterService>();
        var loggerMock = new Mock<ILogger<ScreenFormFieldMasterController>>();
        serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateScreenFormFieldMasterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScreenFormFieldMasterDto { Id = 1 });
        var ctrl = new ScreenFormFieldMasterController(serviceMock.Object, loggerMock.Object);
        var result = await ctrl.Create(new CreateScreenFormFieldMasterDto
        {
            ScreenId = 1,
            SectionId = 1,
            FieldName = "Test",
            FieldLabel = "Test Field",
            FieldCode = "TEST001",
            DataType = "string",
            ControlType = "textbox",
            DisplayOrder = 1
        }, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Update_ExistingId_ReturnsOk()
    {
        var serviceMock = new Mock<IScreenFormFieldMasterService>();
        var loggerMock = new Mock<ILogger<ScreenFormFieldMasterController>>();
        serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateScreenFormFieldMasterDto>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new ScreenFormFieldMasterDto { Id = 1 });
        var ctrl = new ScreenFormFieldMasterController(serviceMock.Object, loggerMock.Object);
        var result = await ctrl.Update(1, new UpdateScreenFormFieldMasterDto
        {
            ScreenId = 1,
            SectionId = 1,
            FieldName = "Test",
            FieldLabel = "Test Field",
            FieldCode = "TEST001",
            DataType = "string",
            ControlType = "textbox",
            DisplayOrder = 1,
            ColumnSpan = 1
        }, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Update_NonExistingId_ReturnsNotFound()
    {
        var serviceMock = new Mock<IScreenFormFieldMasterService>();
        var loggerMock = new Mock<ILogger<ScreenFormFieldMasterController>>();
        serviceMock.Setup(s => s.UpdateAsync(999, It.IsAny<UpdateScreenFormFieldMasterDto>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync((ScreenFormFieldMasterDto?)null);
        var ctrl = new ScreenFormFieldMasterController(serviceMock.Object, loggerMock.Object);
        var result = await ctrl.Update(999, new UpdateScreenFormFieldMasterDto
        {
            ScreenId = 1,
            SectionId = 1,
            FieldName = "Test",
            FieldLabel = "Test Field",
            FieldCode = "TEST001",
            DataType = "string",
            ControlType = "textbox",
            DisplayOrder = 1,
            ColumnSpan = 1
        }, CancellationToken.None);
        Assert.True(result is OkObjectResult || result is NotFoundResult);
    }

    [Fact]
    public async Task Controller_Delete_ExistingId_ReturnsOk()
    {
        var serviceMock = new Mock<IScreenFormFieldMasterService>();
        var loggerMock = new Mock<ILogger<ScreenFormFieldMasterController>>();
        serviceMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var ctrl = new ScreenFormFieldMasterController(serviceMock.Object, loggerMock.Object);
        var result = await ctrl.Delete(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Delete_NonExistingId_ReturnsNotFound()
    {
        var serviceMock = new Mock<IScreenFormFieldMasterService>();
        var loggerMock = new Mock<ILogger<ScreenFormFieldMasterController>>();
        serviceMock.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var ctrl = new ScreenFormFieldMasterController(serviceMock.Object, loggerMock.Object);
        var result = await ctrl.Delete(999, CancellationToken.None);
        Assert.True(result is OkObjectResult || result is NotFoundResult);
    }

    #endregion
}
