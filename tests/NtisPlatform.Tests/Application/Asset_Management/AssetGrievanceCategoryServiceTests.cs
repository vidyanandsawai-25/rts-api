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

public class AssetGrievanceCategoryServiceTests
{
    private readonly Mock<IRepository<AssetGrievanceCategoryEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly AssetGrievanceCategoryService _service;

    public AssetGrievanceCategoryServiceTests()
    {
        _mockRepository = new Mock<IRepository<AssetGrievanceCategoryEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();

        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<AssetGrievanceCategoryEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()))
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

        _service = new AssetGrievanceCategoryService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockReferenceValidator.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = new AssetGrievanceCategoryEntity
        {
            Id = 1,
            CategoryName = "Service Quality Issue",
            ResolutionSlaDays = 3,
            Description = "Issues related to service quality",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<AssetGrievanceCategoryDto>(It.IsAny<AssetGrievanceCategoryEntity>()))
            .Returns(new AssetGrievanceCategoryDto
            {
                Id = 1,
                CategoryName = "Service Quality Issue",
                ResolutionSlaDays = 3,
                Description = "Issues related to service quality",
                IsActive = true
            });

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Service Quality Issue", result.CategoryName);
        Assert.Equal(3, result.ResolutionSlaDays);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetGrievanceCategoryEntity?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        var entities = new List<AssetGrievanceCategoryEntity>
        {
            new() { Id = 1, CategoryName = "Service Quality Issue", ResolutionSlaDays = 3, IsActive = true },
            new() { Id = 2, CategoryName = "Billing Issue", ResolutionSlaDays = 5, IsActive = true },
            new() { Id = 3, CategoryName = "Technical Support", ResolutionSlaDays = 7, IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AssetGrievanceCategoryMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();
        var service = new AssetGrievanceCategoryService(_mockRepository.Object, _mockUnitOfWork.Object, mapper, _mockReferenceValidator.Object);

        var queryParams = new AssetGrievanceCategoryQueryParameters { PageNumber = 1, PageSize = 10 };

        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        var createDto = new CreateAssetGrievanceCategoryDto
        {
            CategoryName = "Service Quality Issue",
            ResolutionSlaDays = 5,
            Description = "Issues related to service quality",
            CreatedBy = 10
        };

        Assert.Equal(10, createDto.CreatedBy);

        var existingList = new List<AssetGrievanceCategoryEntity>();
        var mockQuery = existingList.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        _mockMapper
            .Setup(m => m.Map<AssetGrievanceCategoryEntity>(It.IsAny<CreateAssetGrievanceCategoryDto>()))
            .Returns((CreateAssetGrievanceCategoryDto dto) => new AssetGrievanceCategoryEntity
            {
                CategoryName = dto.CategoryName,
                ResolutionSlaDays = dto.ResolutionSlaDays,
                Description = dto.Description,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<AssetGrievanceCategoryEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetGrievanceCategoryEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<AssetGrievanceCategoryDto>(It.IsAny<AssetGrievanceCategoryEntity>()))
            .Returns((AssetGrievanceCategoryEntity e) => new AssetGrievanceCategoryDto
            {
                Id = e.Id,
                CategoryName = e.CategoryName,
                ResolutionSlaDays = e.ResolutionSlaDays,
                Description = e.Description,
                IsActive = e.IsActive
            });

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Service Quality Issue", result.CategoryName);
    }

    [Fact]
    public async Task CreateAsync_DuplicateCategoryName_ThrowsValidationException()
    {
        var createDto = new CreateAssetGrievanceCategoryDto
        {
            CategoryName = "Existing Category"
        };

        var existingList = new List<AssetGrievanceCategoryEntity>
        {
            new() { Id = 1, CategoryName = "Existing Category", MarkedForDeletion = false }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(existingList.BuildMock());

        _mockMapper
            .Setup(m => m.Map<AssetGrievanceCategoryEntity>(It.IsAny<CreateAssetGrievanceCategoryDto>()))
            .Returns(new AssetGrievanceCategoryEntity { CategoryName = "Existing Category" });

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(createDto, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        var updateDto = new UpdateAssetGrievanceCategoryDto
        {
            CategoryName = "Service Quality Issue - Updated",
            ResolutionSlaDays = 2,
            UpdatedBy = 20
        };

        Assert.Equal(20, updateDto.UpdatedBy);

        var existingEntity = new AssetGrievanceCategoryEntity
        {
            Id = 1,
            CategoryName = "Service Quality Issue",
            ResolutionSlaDays = 5,
            IsActive = true
        };

        var existingList = new List<AssetGrievanceCategoryEntity> { existingEntity };
        var mockQuery = existingList.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<AssetGrievanceCategoryEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateAssetGrievanceCategoryDto>(), It.IsAny<AssetGrievanceCategoryEntity>()))
            .Callback((UpdateAssetGrievanceCategoryDto src, AssetGrievanceCategoryEntity dest) =>
            {
                dest.CategoryName = src.CategoryName;
                dest.ResolutionSlaDays = src.ResolutionSlaDays;
            });

        _mockMapper
            .Setup(m => m.Map<AssetGrievanceCategoryDto>(It.IsAny<AssetGrievanceCategoryEntity>()))
            .Returns((AssetGrievanceCategoryEntity e) => new AssetGrievanceCategoryDto
            {
                Id = e.Id,
                CategoryName = e.CategoryName,
                ResolutionSlaDays = e.ResolutionSlaDays,
                IsActive = e.IsActive
            });

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Service Quality Issue - Updated", result.CategoryName);
    }

    [Fact]
    public async Task UpdateAsync_DeactivationWithReferenceValidationFailure_ThrowsValidationException()
    {
        var updateDto = new UpdateAssetGrievanceCategoryDto
        {
            CategoryName = "Service Quality Issue",
            IsActive = false
        };

        var existingEntity = new AssetGrievanceCategoryEntity
        {
            Id = 1,
            CategoryName = "Service Quality Issue",
            IsActive = true
        };

        var existingList = new List<AssetGrievanceCategoryEntity> { existingEntity };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(existingList.BuildMock());
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);

        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<AssetGrievanceCategoryEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("ReferenceError", "Category is referenced by remarks"));

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateAssetGrievanceCategoryDto>(), It.IsAny<AssetGrievanceCategoryEntity>()))
            .Callback((UpdateAssetGrievanceCategoryDto src, AssetGrievanceCategoryEntity dest) =>
            {
                dest.IsActive = src.IsActive;
            });

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_DuplicateCategoryName_ThrowsValidationException()
    {
        var updateDto = new UpdateAssetGrievanceCategoryDto
        {
            CategoryName = "Duplicate Category Name",
            IsActive = true
        };

        var existingEntity = new AssetGrievanceCategoryEntity
        {
            Id = 1,
            CategoryName = "Original Category",
            IsActive = true
        };

        var otherEntity = new AssetGrievanceCategoryEntity
        {
            Id = 2,
            CategoryName = "Duplicate Category Name",
            IsActive = true
        };

        var existingList = new List<AssetGrievanceCategoryEntity> { existingEntity, otherEntity };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(existingList.BuildMock());
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateAssetGrievanceCategoryDto>(), It.IsAny<AssetGrievanceCategoryEntity>()))
            .Callback((UpdateAssetGrievanceCategoryDto src, AssetGrievanceCategoryEntity dest) =>
            {
                dest.CategoryName = src.CategoryName;
            });

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSavesSuccessfully()
    {
        var existingEntity = new AssetGrievanceCategoryEntity { Id = 1, CategoryName = "Old Category" };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<AssetGrievanceCategoryEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(1, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_ReferenceValidationFailure_ThrowsValidationException()
    {
        var existingEntity = new AssetGrievanceCategoryEntity { Id = 1, CategoryName = "Category With Remarks" };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<AssetGrievanceCategoryEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("ReferenceError", "Category has remarks"));

        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(1, CancellationToken.None));
    }

    #region Mapping and DTO Tests

    [Fact]
    public void AssetGrievanceCategory_Mapping_Configuration_IsValid()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AssetGrievanceCategoryMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var entity = new AssetGrievanceCategoryEntity
        {
            Id = 1,
            CategoryName = "Infrastructure Damage",
            Description = "Potholes and broken roads",
            ResolutionSlaDays = 5,
            IsActive = true,
            MarkedForDeletion = false
        };

        var dto = mapper.Map<AssetGrievanceCategoryDto>(entity);

        Assert.Equal(1, dto.Id);
        Assert.Equal("Infrastructure Damage", dto.CategoryName);
        Assert.Equal("Potholes and broken roads", dto.Description);
        Assert.Equal(5, dto.ResolutionSlaDays);
        Assert.True(dto.IsActive);
        Assert.False(dto.MarkedForDeletion);
    }

    [Fact]
    public void CreateAssetGrievanceCategoryDto_To_Entity_Mapping_IsValid()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AssetGrievanceCategoryMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();

        var createDto = new CreateAssetGrievanceCategoryDto
        {
            CategoryName = "Water Supply Issue",
            Description = "Low pressure water supply",
            ResolutionSlaDays = 2,
            CreatedBy = 10,
            IsActive = true
        };

        var entity = mapper.Map<AssetGrievanceCategoryEntity>(createDto);

        Assert.Equal("Water Supply Issue", entity.CategoryName);
        Assert.Equal("Low pressure water supply", entity.Description);
        Assert.Equal(2, entity.ResolutionSlaDays);
        Assert.Equal(10, entity.CreatedBy);
        Assert.True(entity.IsActive);
        Assert.False(entity.MarkedForDeletion);
    }

    [Fact]
    public void UpdateAssetGrievanceCategoryDto_To_Entity_Mapping_IsValid()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AssetGrievanceCategoryMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        IMapper mapper = mapperConfig.CreateMapper();

        var updateDto = new UpdateAssetGrievanceCategoryDto
        {
            CategoryName = "Updated Category",
            Description = "Updated Description",
            ResolutionSlaDays = 10,
            IsActive = true,
            UpdatedBy = 25
        };

        Assert.Equal("Updated Category", updateDto.CategoryName);
        Assert.Equal("Updated Description", updateDto.Description);
        Assert.Equal(10, updateDto.ResolutionSlaDays);
        Assert.True(updateDto.IsActive);
        Assert.Equal(25, updateDto.UpdatedBy);

        var entity = mapper.Map<AssetGrievanceCategoryEntity>(updateDto);
        Assert.Equal("Updated Category", entity.CategoryName);
        Assert.Equal("Updated Description", entity.Description);
        Assert.Equal(10, entity.ResolutionSlaDays);
        Assert.Equal(25, entity.UpdatedBy);
    }

    [Fact]
    public void AssetGrievanceCategoryDto_Properties_GetAndSet()
    {
        var now = DateTime.UtcNow;
        var dto = new AssetGrievanceCategoryDto
        {
            Id = 100,
            CategoryName = "Cat",
            Description = "Desc",
            ResolutionSlaDays = 7,
            IsActive = true,
            CreatedDate = now,
            UpdatedDate = now,
            MarkedForDeletion = false,
            MarkedForDeletionDate = null
        };

        Assert.Equal(100, dto.Id);
        Assert.Equal("Cat", dto.CategoryName);
        Assert.Equal("Desc", dto.Description);
        Assert.Equal(7, dto.ResolutionSlaDays);
        Assert.True(dto.IsActive);
        Assert.Equal(now, dto.CreatedDate);
        Assert.Equal(now, dto.UpdatedDate);
        Assert.False(dto.MarkedForDeletion);
        Assert.Null(dto.MarkedForDeletionDate);
    }

    [Fact]
    public void AssetGrievanceCategoryQueryParameters_Properties_GetAndSet()
    {
        var qp = new AssetGrievanceCategoryQueryParameters
        {
            CategoryName = "Search Cat",
            Description = "Search Desc",
            IsActive = true,
            MarkedForDeletion = false,
            PageNumber = 1,
            PageSize = 20,
            SortBy = "CategoryName",
            SortOrder = "asc",
            SearchTerm = "Search"
        };

        Assert.Equal("Search Cat", qp.CategoryName);
        Assert.Equal("Search Desc", qp.Description);
        Assert.True(qp.IsActive);
        Assert.False(qp.MarkedForDeletion);
        Assert.Equal(1, qp.PageNumber);
        Assert.Equal(20, qp.PageSize);
        Assert.Equal("CategoryName", qp.SortBy);
        Assert.Equal("asc", qp.SortOrder);
        Assert.Equal("Search", qp.SearchTerm);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(-100, false)]
    [InlineData(0, true)]
    [InlineData(7, true)]
    [InlineData(365, true)]
    public void CreateAssetGrievanceCategoryDto_ResolutionSlaDays_Validation(int days, bool isValid)
    {
        var dto = new CreateAssetGrievanceCategoryDto
        {
            CategoryName = "Category",
            ResolutionSlaDays = days
        };

        var ctx = new System.ComponentModel.DataAnnotations.ValidationContext(dto);
        var results = new System.Collections.Generic.List<System.ComponentModel.DataAnnotations.ValidationResult>();
        bool valid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(dto, ctx, results, true);

        Assert.Equal(isValid, valid);
        if (!isValid)
        {
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateAssetGrievanceCategoryDto.ResolutionSlaDays)));
        }
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(-100, false)]
    [InlineData(0, true)]
    [InlineData(7, true)]
    [InlineData(365, true)]
    public void UpdateAssetGrievanceCategoryDto_ResolutionSlaDays_Validation(int days, bool isValid)
    {
        var dto = new UpdateAssetGrievanceCategoryDto
        {
            CategoryName = "Category",
            ResolutionSlaDays = days
        };

        var ctx = new System.ComponentModel.DataAnnotations.ValidationContext(dto);
        var results = new System.Collections.Generic.List<System.ComponentModel.DataAnnotations.ValidationResult>();
        bool valid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(dto, ctx, results, true);

        Assert.Equal(isValid, valid);
        if (!isValid)
        {
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateAssetGrievanceCategoryDto.ResolutionSlaDays)));
        }
    }

    #endregion
}


