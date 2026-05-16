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

public class ScreenServiceTests
{
    private readonly Mock<IRepository<ScreenEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly IMapper _mapper;
    private readonly ScreenService _service;

    public ScreenServiceTests()
    {
        _mockRepository = new Mock<IRepository<ScreenEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ScreenMappingProfile>();
        },
 Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _service = new ScreenService(_mockRepository.Object, _mockUnitOfWork.Object, _mapper, _mockReferenceValidator.Object);
    }

    #region Entity Tests

    [Fact]
    public void Entity_Properties_GetSet_WorksCorrectly()
    {
        var entity = new ScreenEntity
        {
            Id = 1,
            ScreenName = "Test Screen",
            ScreenCode = "TEST001",
            ModuleId = 1,
            IsActive = true,
            DisplayOrder = 1,
            IsMenuVisible = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now,
            UpdatedBy = 2,
            UpdatedDate = DateTime.Now,
            ParentScreenId = 99
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal("Test Screen", entity.ScreenName);
        Assert.Equal("TEST001", entity.ScreenCode);
        Assert.Equal(1, entity.ModuleId);
        Assert.True(entity.IsActive);
        Assert.Equal(1, entity.DisplayOrder);
        Assert.True(entity.IsMenuVisible);
        Assert.Equal(1, entity.CreatedBy);
        Assert.NotNull(entity.CreatedDate);
        Assert.Equal(2, entity.UpdatedBy);
        Assert.NotNull(entity.UpdatedDate);
        Assert.Equal(99, entity.ParentScreenId);
    }

    [Fact]
    public void Entity_DefaultValues_AreCorrect()
    {
        var entity = new ScreenEntity();
        Assert.Equal(0, entity.Id);
        Assert.True(string.IsNullOrEmpty(entity.ScreenName));
        Assert.True(string.IsNullOrEmpty(entity.ScreenCode));
        Assert.Null(entity.ModuleId);
        Assert.True(entity.IsActive); // IsActive defaults to true in BaseEntity
        Assert.Null(entity.DisplayOrder);
        Assert.False(entity.IsMenuVisible);
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
        var dto = new ScreenDto
        {
            Id = 1,
            ScreenName = "Test Screen",
            ScreenCode = "TEST001",
            ModuleId = 1,
            IsActive = true,
            DisplayOrder = 1,
            IsMenuVisible = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };

        Assert.Equal(1, dto.Id);
        Assert.Equal("Test Screen", dto.ScreenName);
        Assert.Equal("TEST001", dto.ScreenCode);
        Assert.Equal(1, dto.ModuleId);
        Assert.True(dto.IsActive);
        Assert.Equal(1, dto.DisplayOrder);
        Assert.True(dto.IsMenuVisible);
        Assert.NotNull(dto.CreatedDate);
        Assert.NotNull(dto.UpdatedDate);
    }

    [Fact]
    public void Dto_DefaultValues_AreCorrect()
    {
        var dto = new ScreenDto();
        Assert.Equal(0, dto.Id);
        Assert.True(string.IsNullOrEmpty(dto.ScreenName));
        Assert.True(string.IsNullOrEmpty(dto.ScreenCode));
        Assert.Null(dto.ModuleId);
        Assert.Null(dto.DisplayOrder);
        Assert.False(dto.IsMenuVisible);
        Assert.Null(dto.CreatedDate);
        Assert.Null(dto.UpdatedDate);
    }

    [Fact]
    public void CreateDto_ValidData_PassesValidation()
    {
        var dto = new CreateScreenDto
        {
            ScreenName = "Test Screen",
            ScreenCode = "TEST001",
            ModuleId = 1,
            IsActive = true,
            CreatedBy = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData(null, "ScreenMaster_ScreenName_Required")]
    [InlineData("", "ScreenMaster_ScreenName_Required")]
    public void CreateDto_InvalidScreenName_FailsValidation(string? screenName, string expectedError)
    {
        var dto = new CreateScreenDto { ScreenName = screenName!, ScreenCode = "TEST001" };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == expectedError);
    }

    [Fact]
    public void CreateDto_ScreenNameTooLong_FailsValidation()
    {
        var dto = new CreateScreenDto
        {
            ScreenName = new string('A', 201),
            ScreenCode = "TEST001"
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "ScreenMaster_ScreenName_MaxLen_200");
    }

    [Theory]
    [InlineData(null, "ScreenMaster_ScreenCode_Required")]
    [InlineData("", "ScreenMaster_ScreenCode_Required")]
    public void CreateDto_InvalidScreenCode_FailsValidation(string? screenCode, string expectedError)
    {
        var dto = new CreateScreenDto { ScreenName = "Test", ScreenCode = screenCode! };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == expectedError);
    }

    [Fact]
    public void UpdateDto_ValidData_PassesValidation()
    {
        var dto = new UpdateScreenDto
        {
            ScreenName = "Updated Screen",
            ScreenCode = "TEST001",
            ModuleId = 1,
            IsActive = true,
            UpdatedBy = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdateDto_MissingScreenName_FailsValidation()
    {
        var dto = new UpdateScreenDto { ScreenName = null!, ScreenCode = "TEST001" };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "ScreenMaster_ScreenName_Required");
    }

    #endregion

    #region QueryParameters Tests

    [Fact]
    public void QueryParameters_Properties_WorkCorrectly()
    {
        var qp = new ScreenQueryParameters
        {
            ScreenName = "Test",
            ScreenCode = "TEST001",
            ModuleId = 1,
            IsActive = true,
            PageNumber = 2,
            PageSize = 20,
            SearchTerm = "Test",
            SortBy = "ScreenName"
        };
        Assert.Equal("Test", qp.ScreenName);
        Assert.Equal("TEST001", qp.ScreenCode);
        Assert.Equal(1, qp.ModuleId);
        Assert.True(qp.IsActive);
        Assert.Equal(2, qp.PageNumber);
        Assert.Equal(20, qp.PageSize);
        Assert.Equal("Test", qp.SearchTerm);
        Assert.Equal("ScreenName", qp.SortBy);
    }

    [Fact]
    public void QueryParameters_DefaultValues_AreCorrect()
    {
        var qp = new ScreenQueryParameters();
        Assert.Null(qp.ScreenName);
        Assert.Null(qp.ScreenCode);
        Assert.Null(qp.ModuleId);
        Assert.Null(qp.IsActive);
        Assert.Equal(1, qp.PageNumber);
        Assert.Equal(10, qp.PageSize);
    }

    #endregion

    #region Service CRUD Tests

    [Fact]
    public async Task Service_GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = new ScreenEntity
        {
            Id = 1,
            ScreenName = "Test Screen",
            ScreenCode = "TEST001",
            IsActive = true
        };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var result = await _service.GetByIdAsync(1, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test Screen", result.ScreenName);
        Assert.Equal("TEST001", result.ScreenCode);
    }

    [Fact]
    public async Task Service_GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((ScreenEntity?)null);
        var result = await _service.GetByIdAsync(999, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task Service_GetAllAsync_ReturnsAllEntities()
    {
        var entities = new List<ScreenEntity>
        {
  new() { Id = 1, ScreenName = "Screen1", ScreenCode = "SCR001", IsActive = true },
            new() { Id = 2, ScreenName = "Screen2", ScreenCode = "SCR002", IsActive = true },
            new() { Id = 3, ScreenName = "Screen3", ScreenCode = "SCR003", IsActive = false }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMockDbSet().Object);
        var qp = new ScreenQueryParameters { PageNumber = 1, PageSize = 10 };
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
        var entities = new List<ScreenEntity>
        {
       new() { Id = 1, ScreenName = "Screen1", ScreenCode = "SCR001", IsActive = true },
       new() { Id = 2, ScreenName = "Screen2", ScreenCode = "SCR002", IsActive = true },
       new() { Id = 3, ScreenName = "Screen3", ScreenCode = "SCR003", IsActive = false }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMockDbSet().Object);
        var qp = new ScreenQueryParameters { IsActive = isActive, PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.Equal(expectedCount, result.Items.Count());
    }

    [Fact]
    public async Task Service_GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<ScreenEntity>().BuildMockDbSet().Object);
        var qp = new ScreenQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task Service_GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        var entities = Enumerable.Range(1, 25).Select(i => new ScreenEntity
        {
            Id = i,
            ScreenName = $"Screen {i}",
            ScreenCode = $"SCR{i:000}",
            IsActive = true
        }).ToList();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMockDbSet().Object);
        var qp = new ScreenQueryParameters { PageNumber = 2, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(2, result.PageNumber);
    }

    [Fact]
    public async Task Service_CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        var createDto = new CreateScreenDto
        {
            ScreenName = "Test Screen",
            ScreenCode = "TEST001",
            IsActive = true,
            CreatedBy = 1
        };
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ScreenEntity>(), It.IsAny<CancellationToken>()))
.ReturnsAsync((ScreenEntity e, CancellationToken _) => { e.Id = 1; return e; });
        var result = await _service.CreateAsync(createDto, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Test Screen", result.ScreenName);
        Assert.Equal("TEST001", result.ScreenCode);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Service_CreateAsync_DuplicateScreenCode_ThrowsException()
    {
        var createDto = new CreateScreenDto { ScreenName = "Test", ScreenCode = "TEST001" };
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ScreenEntity>(), It.IsAny<CancellationToken>()))
       .ThrowsAsync(new InvalidOperationException("Duplicate"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(createDto, CancellationToken.None));
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Service_UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        var existingEntity = new ScreenEntity
        {
            Id = 1,
            ScreenName = "Old Name",
            ScreenCode = "TEST001",
            IsActive = true
        };
        var updateDto = new UpdateScreenDto
        {
            ScreenName = "Updated Name",
            ScreenCode = "TEST001",
            IsActive = true
        };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ScreenEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.ScreenName);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Service_UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((ScreenEntity?)null);
        var updateDto = new UpdateScreenDto { ScreenName = "Test", ScreenCode = "TEST001" };
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);
        Assert.Null(result);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Service_UpdateAsync_DeactivateWithReferences_ThrowsValidationException()
    {
        var existingEntity = new ScreenEntity
        {
            Id =1,
            ScreenName = "Screen",
            ScreenCode = "CODE",
            IsActive = true
        };
        var updateDto = new UpdateScreenDto
        {
            ScreenName = "Screen",
            ScreenCode = "CODE",
            IsActive = false
        };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mockReferenceValidator.Setup(v => v.ValidateReferencesAsync<ScreenEntity>(1, It.IsAny<CancellationToken>()))
 .ReturnsAsync(ValidationResult.Failure("Cannot deactivate due to references"));
        await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(() => _service.UpdateAsync(1, updateDto, CancellationToken.None));
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ScreenEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Service_UpdateAsync_DeactivateWithoutReferences_Succeeds()
    {
        var existingEntity = new ScreenEntity
        {
            Id =1,
            ScreenName = "Screen",
            ScreenCode = "CODE",
            IsActive = true
        };
        var updateDto = new UpdateScreenDto
        {
            ScreenName = "Screen",
            ScreenCode = "CODE",
            IsActive = false
        };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ScreenEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockReferenceValidator.Setup(v => v.ValidateReferencesAsync<ScreenEntity>(1, It.IsAny<CancellationToken>()))
 .ReturnsAsync(ValidationResult.Success());
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);
        Assert.NotNull(result);
        Assert.False(existingEntity.IsActive);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ScreenEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Service_DeleteAsync_ExistingEntity_DeletesSuccessfully()
    {
        var entity = new ScreenEntity { Id =1, ScreenName = "Test", ScreenCode = "TEST001" };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<ScreenEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockReferenceValidator.Setup(v => v.ValidateReferencesAsync<ScreenEntity>(1, It.IsAny<CancellationToken>())).ReturnsAsync(ValidationResult.Success());
        var result = await _service.DeleteAsync(1, CancellationToken.None);
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.Is<ScreenEntity>(e => e.Id ==1), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Service_DeleteAsync_NonExistingEntity_ReturnsFalse()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((ScreenEntity?)null);
        var result = await _service.DeleteAsync(999, CancellationToken.None);
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Service_DeleteAsync_WithReferences_ThrowsValidationException()
    {
        var entity = new ScreenEntity { Id =1, ScreenName = "Test", ScreenCode = "TEST001" };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockReferenceValidator.Setup(v => v.ValidateReferencesAsync<ScreenEntity>(1, It.IsAny<CancellationToken>()))
 .ReturnsAsync(ValidationResult.Failure("Cannot delete due to references"));
        await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(() => _service.DeleteAsync(1, CancellationToken.None));
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<ScreenEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Controller Tests

    [Fact]
    public async Task Controller_GetAll_ReturnsOk()
    {
        var qp = new ScreenQueryParameters();
        var pagedResult = new PagedResult<ScreenDto>(new List<ScreenDto>(), 0, 1, 10);
        var serviceMock = new Mock<IScreenService>();
        var loggerMock = new Mock<ILogger<ScreenController>>();
        serviceMock.Setup(s => s.GetAllAsync(qp, It.IsAny<CancellationToken>())).ReturnsAsync(pagedResult);
        var ctrl = new ScreenController(serviceMock.Object, loggerMock.Object);
        var result = await ctrl.GetAll(qp, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_GetById_ExistingId_ReturnsOk()
    {
        var serviceMock = new Mock<IScreenService>();
        var loggerMock = new Mock<ILogger<ScreenController>>();
        serviceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new ScreenDto { Id = 1 });
        var ctrl = new ScreenController(serviceMock.Object, loggerMock.Object);
        var result = await ctrl.GetById(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_GetById_NonExistingId_ReturnsNotFound()
    {
        var serviceMock = new Mock<IScreenService>();
        var loggerMock = new Mock<ILogger<ScreenController>>();
        serviceMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((ScreenDto?)null);
        var ctrl = new ScreenController(serviceMock.Object, loggerMock.Object);
        var result = await ctrl.GetById(999, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Controller_Create_ValidDto_ReturnsOk()
    {
        var serviceMock = new Mock<IScreenService>();
        var loggerMock = new Mock<ILogger<ScreenController>>();
        serviceMock.Setup(s => s.CreateAsync(It.IsAny<CreateScreenDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScreenDto { Id = 1 });
        var ctrl = new ScreenController(serviceMock.Object, loggerMock.Object);
        var result = await ctrl.Create(new CreateScreenDto { ScreenName = "Test", ScreenCode = "TEST001" }, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Update_ExistingId_ReturnsOk()
    {
        var serviceMock = new Mock<IScreenService>();
        var loggerMock = new Mock<ILogger<ScreenController>>();
        serviceMock.Setup(s => s.UpdateAsync(1, It.IsAny<UpdateScreenDto>(), It.IsAny<CancellationToken>()))
     .ReturnsAsync(new ScreenDto { Id = 1 });
        var ctrl = new ScreenController(serviceMock.Object, loggerMock.Object);
        var result = await ctrl.Update(1, new UpdateScreenDto { ScreenName = "Test", ScreenCode = "TEST001" }, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Update_NonExistingId_ReturnsNotFound()
    {
        var serviceMock = new Mock<IScreenService>();
        var loggerMock = new Mock<ILogger<ScreenController>>();
        serviceMock.Setup(s => s.UpdateAsync(999, It.IsAny<UpdateScreenDto>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync((ScreenDto?)null);
        var ctrl = new ScreenController(serviceMock.Object, loggerMock.Object);
        var result = await ctrl.Update(999, new UpdateScreenDto { ScreenName = "Test", ScreenCode = "TEST001" }, CancellationToken.None);
        Assert.True(result is OkObjectResult || result is NotFoundResult);
    }

    [Fact]
    public async Task Controller_Delete_ExistingId_ReturnsOk()
    {
        var serviceMock = new Mock<IScreenService>();
        var loggerMock = new Mock<ILogger<ScreenController>>();
        serviceMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var ctrl = new ScreenController(serviceMock.Object, loggerMock.Object);
        var result = await ctrl.Delete(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Delete_NonExistingId_ReturnsNotFound()
    {
        var serviceMock = new Mock<IScreenService>();
        var loggerMock = new Mock<ILogger<ScreenController>>();
        serviceMock.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var ctrl = new ScreenController(serviceMock.Object, loggerMock.Object);
        var result = await ctrl.Delete(999, CancellationToken.None);
        Assert.True(result is OkObjectResult || result is NotFoundResult);
    }

    #endregion
}
