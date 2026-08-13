using AutoMapper;
using Moq;
using MockQueryable.Moq;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Application.Mappings;
using System.ComponentModel.DataAnnotations;
using NtisPlatform.Application.Models;

namespace NtisPlatform.Tests.Application;
public class InventoryItemCategoryServiceTests
{
    private readonly Mock<IRepository<InventoryItemCategoryEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly IMapper _mapper;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly Mock<IRepository<AssetCategoryEntity, int>> _mockAssetCategoryRepository;
    private readonly InventoryItemCategoryService _service;

    public InventoryItemCategoryServiceTests()
    {
        _mockRepository = new Mock<IRepository<InventoryItemCategoryEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();
        _mockReferenceValidator
        .Setup(x => x.ValidateReferencesAsync<InventoryItemCategoryEntity>(It.IsAny<int>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Success());
        _mockAssetCategoryRepository = new Mock<IRepository<AssetCategoryEntity, int>>();
        _mockAssetCategoryRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetCategoryEntity>().BuildMockDbSet().Object);
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<InventoryItemCategoryMappingProfile>();
        },
        Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _service = new InventoryItemCategoryService(_mockRepository.Object, _mockUnitOfWork.Object, _mapper, _mockReferenceValidator.Object, _mockAssetCategoryRepository.Object);
    }

    #region Entity Tests

    [Fact]
    public void Entity_Properties_GetSet_WorksCorrectly()
    {
        var entity = new InventoryItemCategoryEntity
        {
            Id = 1,
            TypeCode = "CAT001",
            TypeName = "Electronics",
            DisplayOrder = 1,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = DateTime.Now,
            UpdatedBy = 2,
            UpdatedDate = DateTime.Now
        };
        Assert.Equal(1, entity.Id);
        Assert.Equal("CAT001", entity.TypeCode);
        Assert.Equal("Electronics", entity.TypeName);
        Assert.Equal(1, entity.DisplayOrder);
        Assert.True(entity.IsActive);
        Assert.Equal(1, entity.CreatedBy);
        Assert.NotNull(entity.CreatedDate);
        Assert.Equal(2, entity.UpdatedBy);
        Assert.NotNull(entity.UpdatedDate);
    }

    [Fact]
    public void Entity_DefaultValues_AreCorrect()
    {
        var entity = new InventoryItemCategoryEntity();
        Assert.Equal(0, entity.Id);
        Assert.Equal(string.Empty, entity.TypeCode);  // NOT NULL varchar(20) in the live DB
        Assert.Equal(string.Empty, entity.TypeName);  // Changed from Assert.Null to match entity default
        Assert.Null(entity.DisplayOrder);  // int NULL in the live DB, no default constraint
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
        var dto = new InventoryItemCategoryDto
        {
            Id = 1,
            TypeCode = "CAT001",
            TypeName = "Electronics",
            DisplayOrder = 1,
            IsActive = true,
            CreatedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };
        Assert.Equal(1, dto.Id);
        Assert.Equal("CAT001", dto.TypeCode);
        Assert.Equal("Electronics", dto.TypeName);
        Assert.Equal(1, dto.DisplayOrder);
        Assert.True(dto.IsActive);
        Assert.NotNull(dto.CreatedDate);
        Assert.NotNull(dto.UpdatedDate);
    }

    [Fact]
    public void CreateDto_ValidData_PassesValidation()
    {
        var dto = new CreateInventoryItemCategoryDto
        {
            AssetCategoryId = 1,
            TypeCode = "CAT001",
            TypeName = "Electronics",
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
    [InlineData(null, "InventoryItemCategory_TypeName_Required")]
    [InlineData("", "InventoryItemCategory_TypeName_Required")]
    public void CreateDto_InvalidTypeName_FailsValidation(string? typeName, string expectedError)
    {
        var dto = new CreateInventoryItemCategoryDto { TypeCode = "CAT001", TypeName = typeName!, DisplayOrder = 1 };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == expectedError);
    }

    [Fact]
    public void CreateDto_TypeNameTooLong_FailsValidation()
    {
        var dto = new CreateInventoryItemCategoryDto
        {
            TypeCode = "CAT001",
            TypeName = new string('A', 101),
            DisplayOrder = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "InventoryItemCategory_TypeName_MaxLen_100");
    }

    [Fact]
    public void CreateDto_DescriptionTooLong_FailsValidation()
    {
        // Live DB column AMS.InventoryItemCategoryMaster.Description is nvarchar(500) NULL.
        var dto = new CreateInventoryItemCategoryDto
        {
            AssetCategoryId = 1,
            TypeCode = "CAT001",
            TypeName = "Electronics",
            DisplayOrder = 1,
            Description = new string('A', 501)
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "InventoryItemCategory_Description_MaxLen_500");
    }

    // Live DB column AMS.InventoryItemCategoryMaster.AssetCategoryId is int NOT NULL with
    // FK_InventoryItemCategoryMaster_AssetCategory -- this was previously missing from the entity
    // and DTOs entirely, which meant every Create request failed with a raw NOT NULL violation
    // swallowed into a generic 500.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateDto_InvalidAssetCategoryId_FailsValidation(int assetCategoryId)
    {
        var dto = new CreateInventoryItemCategoryDto { AssetCategoryId = assetCategoryId, TypeCode = "CAT001", TypeName = "Electronics", DisplayOrder = 1 };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "InventoryItemCategory_AssetCategoryId_InvalidRange");
    }

    [Fact]
    public void CreateDto_MissingDisplayOrder_FailsValidation()
    {
        var dto = new CreateInventoryItemCategoryDto { TypeCode = "CAT001", TypeName = "Electronics", DisplayOrder = null };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "InventoryItemCategory_DisplayOrder_Required");
    }

    // Live DB column AMS.InventoryItemCategoryMaster.TypeCode is varchar(20) NOT NULL -- these
    // guard against the exact failure class originally reported (a request that omits/overflows
    // TypeCode passing DTO validation and then blowing up with a raw DB error at CreateAsync).
    [Theory]
    [InlineData(null, "InventoryItemCategory_TypeCode_Required")]
    [InlineData("", "InventoryItemCategory_TypeCode_Required")]
    public void CreateDto_MissingTypeCode_FailsValidation(string? typeCode, string expectedError)
    {
        var dto = new CreateInventoryItemCategoryDto { TypeCode = typeCode!, TypeName = "Electronics", DisplayOrder = 1 };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == expectedError);
    }

    [Fact]
    public void CreateDto_TypeCodeTooLong_FailsValidation()
    {
        var dto = new CreateInventoryItemCategoryDto
        {
            TypeCode = new string('A', 21),
            TypeName = "Electronics",
            DisplayOrder = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "InventoryItemCategory_TypeCode_MaxLen_20");
    }

    [Theory]
    [InlineData("इलेक्ट्रॉ")] // TypeCode column is varchar(20) (ASCII-only, non-unicode) -- Devanagari must be rejected
    [InlineData("CAT 001")]   // spaces aren't valid in a code
    [InlineData("CAT#001")]   // '#' isn't valid in a code
    public void CreateDto_NonAsciiOrInvalidCharsInTypeCode_FailsValidation(string typeCode)
    {
        var dto = new CreateInventoryItemCategoryDto { TypeCode = typeCode, TypeName = "Electronics", DisplayOrder = 1 };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "InventoryItemCategory_TypeCode_Invalid");
    }

    [Theory]
    [InlineData("CAT-001")]
    [InlineData("CAT_001")]
    [InlineData("CAT001")]
    public void CreateDto_ValidAsciiTypeCode_PassesValidation(string typeCode)
    {
        var dto = new CreateInventoryItemCategoryDto { AssetCategoryId = 1, TypeCode = typeCode, TypeName = "Electronics", DisplayOrder = 1 };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdateDto_ValidData_PassesValidation()
    {
        var dto = new UpdateInventoryItemCategoryDto
        {
            AssetCategoryId = 1,
            TypeCode = "CAT001",
            TypeName = "Electronics Updated",
            DisplayOrder = 2,
            IsActive = true,
            UpdatedBy = 1
        };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdateDto_MissingTypeName_FailsValidation()
    {
        var dto = new UpdateInventoryItemCategoryDto { TypeCode = "CAT001", TypeName = null, DisplayOrder = 1 };
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        Assert.False(isValid);
        Assert.Contains(results, v => v.ErrorMessage == "InventoryItemCategory_TypeName_Required");
    }

    #endregion

    #region QueryParameters Tests

    [Fact]
    public void QueryParameters_Properties_WorkCorrectly()
    {
        var qp = new InventoryItemCategoryQueryParameters
        {
            IsActive = true,
            PageNumber = 2,
            PageSize = 20,
            SearchTerm = "Test",
            SortBy = "TypeName"
        };
        Assert.True(qp.IsActive);
        Assert.Equal(2, qp.PageNumber);
        Assert.Equal(20, qp.PageSize);
        Assert.Equal("Test", qp.SearchTerm);
        Assert.Equal("TypeName", qp.SortBy);
    }

    [Fact]
    public void QueryParameters_DefaultValues_AreCorrect()
    {
        var qp = new InventoryItemCategoryQueryParameters();
        Assert.Null(qp.IsActive);
        Assert.Equal(1, qp.PageNumber);
        Assert.Equal(10, qp.PageSize);
    }

    #endregion

    #region Service CRUD Tests

    [Fact]
    public async Task Service_GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = new InventoryItemCategoryEntity
        {
            Id = 1,
            TypeCode = "CAT001",
            TypeName = "Electronics",
            DisplayOrder = 1,
            IsActive = true
        };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var result = await _service.GetByIdAsync(1, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("CAT001", result.TypeCode);
        Assert.Equal("Electronics", result.TypeName);
    }

    [Fact]
    public async Task Service_GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((InventoryItemCategoryEntity?)null);
        var result = await _service.GetByIdAsync(999, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task Service_GetAllAsync_ReturnsAllEntities()
    {
        var entities = new List<InventoryItemCategoryEntity>
        {
            new() { Id = 1, TypeCode = "CAT001", TypeName = "Electronics", DisplayOrder = 1, IsActive = true },
            new() { Id = 2, TypeCode = "CAT002", TypeName = "Furniture", DisplayOrder = 2, IsActive = true },
            new() { Id = 3, TypeCode = "CAT003", TypeName = "Office", DisplayOrder = 3, IsActive = false }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMockDbSet().Object);
        var qp = new InventoryItemCategoryQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count());
    }

    [Theory]
    [InlineData(null, 3)]      // No IsActive filter, returns all 3 items
    [InlineData(true, 2)]  // IsActive=true, returns 2 active items
    [InlineData(false, 1)]      // IsActive=false, returns 1 inactive item
    public async Task Service_GetAllAsync_WithFilters_ReturnsFilteredEntities(bool? isActive, int expectedCount)
    {
        var entities = new List<InventoryItemCategoryEntity>
     {
       new() { Id = 1, TypeCode = "CAT001", TypeName = "Electronics", DisplayOrder = 1, IsActive = true },
            new() { Id = 2, TypeCode = "CAT001", TypeName = "Electronics Old", DisplayOrder = 2, IsActive = false },
            new() { Id = 3, TypeCode = "CAT002", TypeName = "Furniture", DisplayOrder = 3, IsActive = true }
     };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMockDbSet().Object);
        var qp = new InventoryItemCategoryQueryParameters { IsActive = isActive, PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.Equal(expectedCount, result.Items.Count());
    }

    // AssetCategoryId is a required FK (AMS.InventoryItemCategoryMaster.AssetCategoryId ->
    // AMS.AssetCategoryMaster.Id) -- GetAll must resolve and expose the referenced category's
    // display name (AssetCategoryName) via a join, not just the raw FK id.
    [Fact]
    public async Task Service_GetAllAsync_PopulatesAssetCategoryName_FromReferencedAssetCategory()
    {
        var entities = new List<InventoryItemCategoryEntity>
        {
            new() { Id = 1, AssetCategoryId = 5, TypeCode = "CAT001", TypeName = "Electronics", DisplayOrder = 1, IsActive = true },
            new() { Id = 2, AssetCategoryId = 999, TypeCode = "CAT002", TypeName = "Furniture", DisplayOrder = 2, IsActive = true }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMockDbSet().Object);

        var assetCategories = new List<AssetCategoryEntity>
        {
            new() { Id = 5, CategoryCode = "ELEC", CategoryName = "Electronics Category" }
        };
        _mockAssetCategoryRepository.Setup(r => r.GetQueryable()).Returns(assetCategories.BuildMockDbSet().Object);

        var qp = new InventoryItemCategoryQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);

        var mapped = result.Items.ToDictionary(x => x.Id);
        Assert.Equal("Electronics Category", mapped[1].AssetCategoryName);
        Assert.Null(mapped[2].AssetCategoryName); // AssetCategoryId 999 doesn't resolve to any AssetCategoryEntity
    }

    [Fact]
    public async Task Service_GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<InventoryItemCategoryEntity>().BuildMockDbSet().Object);
        var qp = new InventoryItemCategoryQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task Service_GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        var entities = Enumerable.Range(1, 25).Select(i => new InventoryItemCategoryEntity
        {
            Id = i,
            TypeCode = $"CAT{i:000}",
            TypeName = $"Category {i}",
            DisplayOrder = i,
            IsActive = true
        }).ToList();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMockDbSet().Object);
        var qp = new InventoryItemCategoryQueryParameters { PageNumber = 2, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(10, result.Items.Count());
        Assert.Equal(2, result.PageNumber);
    }

    [Fact]
    public async Task Service_CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        var createDto = new CreateInventoryItemCategoryDto
        {
            TypeCode = "CAT001",
            TypeName = "Electronics",
            DisplayOrder = 1,
            IsActive = true,
            CreatedBy = 1
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<InventoryItemCategoryEntity>().BuildMockDbSet().Object);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<InventoryItemCategoryEntity>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((InventoryItemCategoryEntity e, CancellationToken _) => { e.Id = 1; return e; });
        var result = await _service.CreateAsync(createDto, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("CAT001", result.TypeCode);
        Assert.Equal("Electronics", result.TypeName);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("MISC", "Miscellaneous", 10, true)]
    [InlineData("OLD001", "Obsolete", 99, false)]
    public async Task Service_CreateAsync_EdgeCases_CreatesSuccessfully(string typeCode, string typeName, int displayOrder, bool isActive)
    {
        var createDto = new CreateInventoryItemCategoryDto
        {
            TypeCode = typeCode,
            TypeName = typeName,
            DisplayOrder = displayOrder,
            IsActive = isActive
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<InventoryItemCategoryEntity>().BuildMockDbSet().Object);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<InventoryItemCategoryEntity>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((InventoryItemCategoryEntity e, CancellationToken _) => { e.Id = 1; return e; });
        var result = await _service.CreateAsync(createDto, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(typeCode, result.TypeCode);
        Assert.Equal(typeName, result.TypeName);
        Assert.Equal(isActive, result.IsActive);
    }

    [Fact]
    public async Task Service_CreateAsync_RepositoryThrowsOnRaceConditionDuplicate_PropagatesException()
    {
        // Simulates a DB-level unique-constraint violation on a concurrent insert that slips past
        // the in-application CheckDuplicateAsync check (e.g. two requests racing on the same TypeCode).
        var createDto = new CreateInventoryItemCategoryDto { TypeCode = "CAT001", TypeName = "Electronics", DisplayOrder = 1 };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<InventoryItemCategoryEntity>().BuildMockDbSet().Object);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<InventoryItemCategoryEntity>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new InvalidOperationException("Duplicate"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(createDto, CancellationToken.None));
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Service_UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        var existingEntity = new InventoryItemCategoryEntity
        {
            Id = 1,
            TypeCode = "CAT001",
            TypeName = "Electronics",
            DisplayOrder = 1,
            IsActive = true
        };
        var updateDto = new UpdateInventoryItemCategoryDto
        {
            TypeCode = "CAT001",
            TypeName = "Electronics Updated",
            DisplayOrder = 2,
            IsActive = true
        };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<InventoryItemCategoryEntity> { existingEntity }.BuildMockDbSet().Object);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<InventoryItemCategoryEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal("Electronics Updated", result.TypeName);
        Assert.Equal(2, result.DisplayOrder);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Service_UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((InventoryItemCategoryEntity?)null);
        var updateDto = new UpdateInventoryItemCategoryDto { TypeName = "Test", DisplayOrder = 1 };
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);
        Assert.Null(result);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Service_UpdateAsync_Deactivation_CallsReferenceValidation()
    {
        var entity = new InventoryItemCategoryEntity { Id = 1, IsActive = true };
        var updateDto = new UpdateInventoryItemCategoryDto { IsActive = false, TypeName = "Test", DisplayOrder = 1 };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<InventoryItemCategoryEntity> { entity }.BuildMockDbSet().Object);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<InventoryItemCategoryEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        _mockReferenceValidator.Verify(
            x => x.ValidateReferencesAsync<InventoryItemCategoryEntity>(1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Service_DeleteAsync_CallsReferenceValidation()
    {
        var entity = new InventoryItemCategoryEntity { Id = 1, TypeCode = "CAT001" };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<InventoryItemCategoryEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _service.DeleteAsync(1, CancellationToken.None);

        _mockReferenceValidator.Verify(
            x => x.ValidateReferencesAsync<InventoryItemCategoryEntity>(1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Service_DeleteAsync_ReferenceValidationFails_ThrowsException()
    {
        var entity = new InventoryItemCategoryEntity { Id = 1, TypeCode = "CAT001" };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockReferenceValidator
        .Setup(x => x.ValidateReferencesAsync<InventoryItemCategoryEntity>(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Failure("Test error"));

        await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(() => _service.DeleteAsync(1, CancellationToken.None));
    }

    [Fact]
    public async Task Service_DeleteAsync_ExistingEntity_DeletesSuccessfully()
    {
        var entity = new InventoryItemCategoryEntity { Id = 1, TypeCode = "CAT001" };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<InventoryItemCategoryEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var result = await _service.DeleteAsync(1, CancellationToken.None);
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.Is<InventoryItemCategoryEntity>(e => e.Id == 1), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Service_DeleteAsync_NonExistingEntity_ReturnsFalse()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((InventoryItemCategoryEntity?)null);
        var result = await _service.DeleteAsync(999, CancellationToken.None);
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Duplicate Validation Tests

    [Fact]
    public async Task Service_CreateAsync_DuplicateTypeName_ThrowsValidationException()
    {
        var existing = new List<InventoryItemCategoryEntity>
        {
            new() { Id = 1, TypeCode = "CAT001", TypeName = "Electronics" }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(existing.BuildMockDbSet().Object);

        var createDto = new CreateInventoryItemCategoryDto { TypeCode = "CAT999", TypeName = "Electronics", DisplayOrder = 1 };

        var ex = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.CreateAsync(createDto, CancellationToken.None));
        Assert.Contains("InventoryItemCategory_TypeName_Duplicate", ex.Errors.Values);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Service_CreateAsync_DuplicateTypeCode_ThrowsValidationException()
    {
        var existing = new List<InventoryItemCategoryEntity>
        {
            new() { Id = 1, TypeCode = "CAT001", TypeName = "Electronics" }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(existing.BuildMockDbSet().Object);

        var createDto = new CreateInventoryItemCategoryDto { TypeCode = "CAT001", TypeName = "Different Name", DisplayOrder = 1 };

        var ex = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.CreateAsync(createDto, CancellationToken.None));
        Assert.Contains("InventoryItemCategory_TypeCode_Duplicate", ex.Errors.Values);
    }

    [Fact]
    public async Task Service_CreateAsync_TypeCodeStillHeldByMarkedForDeletionRow_ThrowsValidationException()
    {
        // UQ_InventoryItemCategoryMaster_TypeCode is a plain (unfiltered) unique constraint in the
        // live DB -- a row that's only MarkedForDeletion (pending nightly HardDeleteCleanupService
        // purge) still physically occupies its TypeCode, so the app-level check must catch this
        // before the DB does, rather than pretending the code is free for reuse.
        var existing = new List<InventoryItemCategoryEntity>
        {
            new() { Id = 1, TypeCode = "CAT001", TypeName = "Old Electronics", MarkedForDeletion = true }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(existing.BuildMockDbSet().Object);

        var createDto = new CreateInventoryItemCategoryDto { TypeCode = "CAT001", TypeName = "New Electronics", DisplayOrder = 1 };

        var ex = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.CreateAsync(createDto, CancellationToken.None));
        Assert.Contains("InventoryItemCategory_TypeCode_Duplicate", ex.Errors.Values);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Service_CreateAsync_TypeNameStillHeldByMarkedForDeletionRow_ThrowsValidationException()
    {
        var existing = new List<InventoryItemCategoryEntity>
        {
            new() { Id = 1, TypeCode = "CAT001", TypeName = "Electronics", MarkedForDeletion = true }
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(existing.BuildMockDbSet().Object);

        var createDto = new CreateInventoryItemCategoryDto { TypeCode = "CAT999", TypeName = "Electronics", DisplayOrder = 1 };

        var ex = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.CreateAsync(createDto, CancellationToken.None));
        Assert.Contains("InventoryItemCategory_TypeName_Duplicate", ex.Errors.Values);
    }

    [Fact]
    public async Task Service_UpdateAsync_RenameToOwnCurrentName_Succeeds()
    {
        var existingEntity = new InventoryItemCategoryEntity { Id = 1, TypeCode = "CAT001", TypeName = "Electronics", IsActive = true };
        var all = new List<InventoryItemCategoryEntity> { existingEntity };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(all.BuildMockDbSet().Object);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<InventoryItemCategoryEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var updateDto = new UpdateInventoryItemCategoryDto { TypeCode = "CAT001", TypeName = "Electronics", DisplayOrder = 1, IsActive = true };
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Service_UpdateAsync_RenameToAnotherRowsName_ThrowsValidationException()
    {
        var existingEntity = new InventoryItemCategoryEntity { Id = 1, TypeCode = "CAT001", TypeName = "Electronics", IsActive = true };
        var other = new InventoryItemCategoryEntity { Id = 2, TypeCode = "CAT002", TypeName = "Furniture", IsActive = true };
        var all = new List<InventoryItemCategoryEntity> { existingEntity, other };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEntity);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(all.BuildMockDbSet().Object);

        var updateDto = new UpdateInventoryItemCategoryDto { TypeCode = "CAT001", TypeName = "Furniture", DisplayOrder = 1, IsActive = true };

        var ex = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.UpdateAsync(1, updateDto, CancellationToken.None));
        Assert.Contains("InventoryItemCategory_TypeName_Duplicate", ex.Errors.Values);
    }

    #endregion

    #region Deactivation Reference Validation Tests

    [Fact]
    public async Task Service_UpdateAsync_DeactivationReferenced_ThrowsValidationException()
    {
        var entity = new InventoryItemCategoryEntity { Id = 1, TypeCode = "CAT001", TypeName = "Electronics", IsActive = true };
        var all = new List<InventoryItemCategoryEntity> { entity };
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockRepository.Setup(r => r.GetQueryable()).Returns(all.BuildMockDbSet().Object);
        _mockReferenceValidator
            .Setup(x => x.ValidateReferencesAsync<InventoryItemCategoryEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(NtisPlatform.Application.Models.ValidationResult.Failure("Referenced by Inventory Batch"));

        var updateDto = new UpdateInventoryItemCategoryDto { TypeCode = "CAT001", TypeName = "Electronics", DisplayOrder = 1, IsActive = false };

        await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(
            () => _service.UpdateAsync(1, updateDto, CancellationToken.None));
    }

    #endregion
}
