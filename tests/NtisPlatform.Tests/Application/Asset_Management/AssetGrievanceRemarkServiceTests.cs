using AutoMapper;
using Moq;
using MockQueryable;
using NtisPlatform.Application.DTOs.Asset_Management;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services.Asset_Management;
using NtisPlatform.Core.Entities.Asset_Management;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Application.Mappings.Asset_Management;
using NtisPlatform.Application.Models;
using Xunit;

namespace NtisPlatform.Tests.Application.Asset_Management;

public class AssetGrievanceRemarkServiceTests
{
    private readonly Mock<IRepository<AssetGrievanceRemarkMasterEntity, int>> _mockRepository;
    private readonly Mock<IRepository<AssetGrievanceCategoryEntity, int>> _mockCategoryRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly AssetGrievanceRemarkService _service;

    public AssetGrievanceRemarkServiceTests()
    {
        _mockRepository = new Mock<IRepository<AssetGrievanceRemarkMasterEntity, int>>();
        _mockCategoryRepository = new Mock<IRepository<AssetGrievanceCategoryEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();

        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<AssetGrievanceRemarkMasterEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockUnitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new AssetGrievanceRemarkService(
            _mockRepository.Object,
            _mockCategoryRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockReferenceValidator.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = new AssetGrievanceRemarkMasterEntity
        {
            Id = 1,
            GrievanceCategoryId = 2,
            Remark = "Verification Required",
            Description = "Requires physical inspection",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<AssetGrievanceRemarkDto>(It.IsAny<AssetGrievanceRemarkMasterEntity>()))
            .Returns(new AssetGrievanceRemarkDto
            {
                Id = 1,
                GrievanceCategoryId = 2,
                Remark = "Verification Required",
                Description = "Requires physical inspection",
                IsActive = true
            });

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Verification Required", result.Remark);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetGrievanceRemarkMasterEntity?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        var entities = new List<AssetGrievanceRemarkMasterEntity>
        {
            new() { Id = 1, GrievanceCategoryId = 1, Remark = "Remark 1", IsActive = true },
            new() { Id = 2, GrievanceCategoryId = 1, Remark = "Remark 2", IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AssetGrievanceRemarkMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new AssetGrievanceRemarkService(_mockRepository.Object, _mockCategoryRepository.Object, _mockUnitOfWork.Object, mapper, _mockReferenceValidator.Object);

        var queryParams = new AssetGrievanceRemarkQueryParameters { PageNumber = 1, PageSize = 10 };

        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        var createDto = new CreateAssetGrievanceRemarkDto
        {
            GrievanceCategoryId = 1,
            Remark = "New Remark",
            Description = "Description text",
            CreatedBy = 10
        };

        Assert.Equal(10, createDto.CreatedBy);

        var categories = new List<AssetGrievanceCategoryEntity>
        {
            new() { Id = 1, CategoryName = "Cat 1", IsActive = true, MarkedForDeletion = false }
        };
        _mockCategoryRepository.Setup(c => c.GetQueryable()).Returns(categories.BuildMock());

        var existingList = new List<AssetGrievanceRemarkMasterEntity>();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(existingList.BuildMock());

        _mockMapper
            .Setup(m => m.Map<AssetGrievanceRemarkMasterEntity>(It.IsAny<CreateAssetGrievanceRemarkDto>()))
            .Returns((CreateAssetGrievanceRemarkDto dto) => new AssetGrievanceRemarkMasterEntity
            {
                GrievanceCategoryId = dto.GrievanceCategoryId,
                Remark = dto.Remark,
                Description = dto.Description,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<AssetGrievanceRemarkMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetGrievanceRemarkMasterEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<AssetGrievanceRemarkDto>(It.IsAny<AssetGrievanceRemarkMasterEntity>()))
            .Returns((AssetGrievanceRemarkMasterEntity e) => new AssetGrievanceRemarkDto
            {
                Id = e.Id,
                GrievanceCategoryId = e.GrievanceCategoryId,
                Remark = e.Remark,
                Description = e.Description,
                IsActive = e.IsActive
            });

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("New Remark", result.Remark);
    }

    [Fact]
    public async Task CreateAsync_InvalidCategory_ThrowsValidationException()
    {
        var createDto = new CreateAssetGrievanceRemarkDto
        {
            GrievanceCategoryId = 999,
            Remark = "New Remark"
        };

        var categories = new List<AssetGrievanceCategoryEntity>();
        _mockCategoryRepository.Setup(c => c.GetQueryable()).Returns(categories.BuildMock());

        _mockMapper
            .Setup(m => m.Map<AssetGrievanceRemarkMasterEntity>(It.IsAny<CreateAssetGrievanceRemarkDto>()))
            .Returns(new AssetGrievanceRemarkMasterEntity { GrievanceCategoryId = 999, Remark = "New Remark" });

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(createDto, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_DuplicateRemark_ThrowsValidationException()
    {
        var createDto = new CreateAssetGrievanceRemarkDto
        {
            GrievanceCategoryId = 1,
            Remark = "Duplicate Remark"
        };

        var categories = new List<AssetGrievanceCategoryEntity>
        {
            new() { Id = 1, CategoryName = "Cat 1", IsActive = true, MarkedForDeletion = false }
        };
        _mockCategoryRepository.Setup(c => c.GetQueryable()).Returns(categories.BuildMock());

        var existingList = new List<AssetGrievanceRemarkMasterEntity>
        {
            new() { Id = 1, GrievanceCategoryId = 1, Remark = "Duplicate Remark", MarkedForDeletion = false }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(existingList.BuildMock());

        _mockMapper
            .Setup(m => m.Map<AssetGrievanceRemarkMasterEntity>(It.IsAny<CreateAssetGrievanceRemarkDto>()))
            .Returns(new AssetGrievanceRemarkMasterEntity { GrievanceCategoryId = 1, Remark = "Duplicate Remark" });

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(createDto, CancellationToken.None));
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        var updateDto = new UpdateAssetGrievanceRemarkDto
        {
            GrievanceCategoryId = 1,
            Remark = "Updated Remark",
            UpdatedBy = 20
        };

        Assert.Equal(20, updateDto.UpdatedBy);

        var categories = new List<AssetGrievanceCategoryEntity>
        {
            new() { Id = 1, CategoryName = "Cat 1", IsActive = true, MarkedForDeletion = false }
        };
        _mockCategoryRepository.Setup(c => c.GetQueryable()).Returns(categories.BuildMock());

        var existingEntity = new AssetGrievanceRemarkMasterEntity
        {
            Id = 1,
            GrievanceCategoryId = 1,
            Remark = "Original Remark",
            IsActive = true
        };

        var existingList = new List<AssetGrievanceRemarkMasterEntity> { existingEntity };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(existingList.BuildMock());
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<AssetGrievanceRemarkMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateAssetGrievanceRemarkDto>(), It.IsAny<AssetGrievanceRemarkMasterEntity>()))
            .Callback((UpdateAssetGrievanceRemarkDto src, AssetGrievanceRemarkMasterEntity dest) =>
            {
                dest.Remark = src.Remark;
            });

        _mockMapper
            .Setup(m => m.Map<AssetGrievanceRemarkDto>(It.IsAny<AssetGrievanceRemarkMasterEntity>()))
            .Returns((AssetGrievanceRemarkMasterEntity e) => new AssetGrievanceRemarkDto
            {
                Id = e.Id,
                GrievanceCategoryId = e.GrievanceCategoryId,
                Remark = e.Remark,
                IsActive = e.IsActive
            });

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Updated Remark", result.Remark);
    }

    [Fact]
    public async Task UpdateAsync_DeactivationReferenceValidationFailure_ThrowsValidationException()
    {
        var updateDto = new UpdateAssetGrievanceRemarkDto
        {
            GrievanceCategoryId = 1,
            Remark = "Remark 1",
            IsActive = false
        };

        var existingEntity = new AssetGrievanceRemarkMasterEntity
        {
            Id = 1,
            GrievanceCategoryId = 1,
            Remark = "Remark 1",
            IsActive = true
        };

        var existingList = new List<AssetGrievanceRemarkMasterEntity> { existingEntity };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(existingList.BuildMock());
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);

        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<AssetGrievanceRemarkMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("RefErr", "Remark is in use"));

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateAssetGrievanceRemarkDto>(), It.IsAny<AssetGrievanceRemarkMasterEntity>()))
            .Callback((UpdateAssetGrievanceRemarkDto src, AssetGrievanceRemarkMasterEntity dest) =>
            {
                dest.IsActive = src.IsActive;
            });

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_InvalidCategory_ThrowsValidationException()
    {
        var updateDto = new UpdateAssetGrievanceRemarkDto
        {
            GrievanceCategoryId = 999,
            Remark = "Remark 1",
            IsActive = true
        };

        var existingEntity = new AssetGrievanceRemarkMasterEntity
        {
            Id = 1,
            GrievanceCategoryId = 1,
            Remark = "Remark 1",
            IsActive = true
        };

        var categories = new List<AssetGrievanceCategoryEntity>();
        _mockCategoryRepository.Setup(c => c.GetQueryable()).Returns(categories.BuildMock());

        var existingList = new List<AssetGrievanceRemarkMasterEntity> { existingEntity };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(existingList.BuildMock());
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateAssetGrievanceRemarkDto>(), It.IsAny<AssetGrievanceRemarkMasterEntity>()))
            .Callback((UpdateAssetGrievanceRemarkDto src, AssetGrievanceRemarkMasterEntity dest) =>
            {
                dest.GrievanceCategoryId = src.GrievanceCategoryId;
            });

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_DuplicateRemark_ThrowsValidationException()
    {
        var updateDto = new UpdateAssetGrievanceRemarkDto
        {
            GrievanceCategoryId = 1,
            Remark = "Duplicate Remark",
            IsActive = true
        };

        var categories = new List<AssetGrievanceCategoryEntity>
        {
            new() { Id = 1, CategoryName = "Cat 1", IsActive = true, MarkedForDeletion = false }
        };
        _mockCategoryRepository.Setup(c => c.GetQueryable()).Returns(categories.BuildMock());

        var existingEntity = new AssetGrievanceRemarkMasterEntity
        {
            Id = 1,
            GrievanceCategoryId = 1,
            Remark = "Original Remark",
            IsActive = true
        };

        var duplicateEntity = new AssetGrievanceRemarkMasterEntity
        {
            Id = 2,
            GrievanceCategoryId = 1,
            Remark = "Duplicate Remark",
            IsActive = true
        };

        var existingList = new List<AssetGrievanceRemarkMasterEntity> { existingEntity, duplicateEntity };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(existingList.BuildMock());
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateAssetGrievanceRemarkDto>(), It.IsAny<AssetGrievanceRemarkMasterEntity>()))
            .Callback((UpdateAssetGrievanceRemarkDto src, AssetGrievanceRemarkMasterEntity dest) =>
            {
                dest.Remark = src.Remark;
            });

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSavesSuccessfully()
    {
        var existingEntity = new AssetGrievanceRemarkMasterEntity { Id = 1, Remark = "Old Remark" };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<AssetGrievanceRemarkMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(1, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_ReferenceValidationFailure_ThrowsValidationException()
    {
        var existingEntity = new AssetGrievanceRemarkMasterEntity { Id = 1, Remark = "Remark In Use" };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<AssetGrievanceRemarkMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("RefErr", "Remark is in use"));

        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(1, CancellationToken.None));
    }

    #endregion

    #region Mapping and DTO Tests

    [Fact]
    public void AssetGrievanceRemark_Mapping_Configuration_IsValid()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AssetGrievanceRemarkMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var entity = new AssetGrievanceRemarkMasterEntity
        {
            Id = 1,
            GrievanceCategoryId = 2,
            GrievanceCategory = new AssetGrievanceCategoryEntity { Id = 2, CategoryName = "Sanitation" },
            Remark = "Verification Pending",
            Description = "Site verification by officer pending",
            IsActive = true,
            MarkedForDeletion = false
        };

        var dto = mapper.Map<AssetGrievanceRemarkDto>(entity);

        Assert.Equal(1, dto.Id);
        Assert.Equal(2, dto.GrievanceCategoryId);
        Assert.Equal("Sanitation", dto.GrievanceCategoryName);
        Assert.Equal("Verification Pending", dto.Remark);
        Assert.Equal("Site verification by officer pending", dto.Description);
        Assert.True(dto.IsActive);
        Assert.False(dto.MarkedForDeletion);
    }

    [Fact]
    public void CreateAssetGrievanceRemarkDto_To_Entity_Mapping_IsValid()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AssetGrievanceRemarkMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();

        var createDto = new CreateAssetGrievanceRemarkDto
        {
            GrievanceCategoryId = 3,
            Remark = "Action Required",
            Description = "Needs escalation to zonal head",
            CreatedBy = 15,
            IsActive = true
        };

        var entity = mapper.Map<AssetGrievanceRemarkMasterEntity>(createDto);

        Assert.Equal(3, entity.GrievanceCategoryId);
        Assert.Equal("Action Required", entity.Remark);
        Assert.Equal("Needs escalation to zonal head", entity.Description);
        Assert.Equal(15, entity.CreatedBy);
        Assert.True(entity.IsActive);
        Assert.False(entity.MarkedForDeletion);
    }

    [Fact]
    public void UpdateAssetGrievanceRemarkDto_To_Entity_Mapping_IsValid()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AssetGrievanceRemarkMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();

        var updateDto = new UpdateAssetGrievanceRemarkDto
        {
            GrievanceCategoryId = 5,
            Remark = "Updated Remark",
            Description = "Updated Description",
            IsActive = true,
            UpdatedBy = 30
        };

        Assert.Equal(5, updateDto.GrievanceCategoryId);
        Assert.Equal("Updated Remark", updateDto.Remark);
        Assert.Equal("Updated Description", updateDto.Description);
        Assert.True(updateDto.IsActive);
        Assert.Equal(30, updateDto.UpdatedBy);

        var entity = mapper.Map<AssetGrievanceRemarkMasterEntity>(updateDto);
        Assert.Equal(5, entity.GrievanceCategoryId);
        Assert.Equal("Updated Remark", entity.Remark);
        Assert.Equal("Updated Description", entity.Description);
        Assert.Equal(30, entity.UpdatedBy);
    }

    [Fact]
    public void AssetGrievanceRemarkDto_Properties_GetAndSet()
    {
        var now = DateTime.Now;
        var dto = new AssetGrievanceRemarkDto
        {
            Id = 200,
            GrievanceCategoryId = 5,
            GrievanceCategoryName = "Category Name",
            Remark = "Remark text",
            Description = "Desc text",
            IsActive = true,
            CreatedDate = now,
            UpdatedDate = now,
            MarkedForDeletion = false,
            MarkedForDeletionDate = null
        };

        Assert.Equal(200, dto.Id);
        Assert.Equal(5, dto.GrievanceCategoryId);
        Assert.Equal("Category Name", dto.GrievanceCategoryName);
        Assert.Equal("Remark text", dto.Remark);
        Assert.Equal("Desc text", dto.Description);
        Assert.True(dto.IsActive);
        Assert.Equal(now, dto.CreatedDate);
        Assert.Equal(now, dto.UpdatedDate);
        Assert.False(dto.MarkedForDeletion);
        Assert.Null(dto.MarkedForDeletionDate);
    }

    [Fact]
    public void AssetGrievanceRemarkQueryParameters_Properties_GetAndSet()
    {
        var qp = new AssetGrievanceRemarkQueryParameters
        {
            GrievanceCategoryId = 10,
            Remark = "Search Remark",
            Description = "Search Desc",
            IsActive = true,
            MarkedForDeletion = false,
            PageNumber = 2,
            PageSize = 15,
            SortBy = "Remark",
            SortOrder = "desc",
            SearchTerm = "Search"
        };

        Assert.Equal(10, qp.GrievanceCategoryId);
        Assert.Equal("Search Remark", qp.Remark);
        Assert.Equal("Search Desc", qp.Description);
        Assert.True(qp.IsActive);
        Assert.False(qp.MarkedForDeletion);
        Assert.Equal(2, qp.PageNumber);
        Assert.Equal(15, qp.PageSize);
        Assert.Equal("Remark", qp.SortBy);
        Assert.Equal("desc", qp.SortOrder);
        Assert.Equal("Search", qp.SearchTerm);
    }

    #endregion
}

