using AutoMapper;
using Moq;
using MockQueryable.Moq;
using NtisPlatform.Application.DTOs.PropertySocialDetails;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Property;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Unit tests for PropertySocialDetailsService
/// </summary>
public class PropertySocialDetailsServiceTests
{
    private readonly Mock<IRepository<PropertySocialDetailsEntity, int>> _mockRepository;
    private readonly Mock<IPropertySocialDetailsRepository> _mockSocialDetailsRepository;
    private readonly Mock<IRepository<DocumentBindingEntity, int>> _mockDocumentBindingRepository;
    private readonly Mock<IRepository<DocumentEntity, int>> _mockDocumentRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PropertySocialDetailsService _service;

    public PropertySocialDetailsServiceTests()
    {
        _mockRepository = new Mock<IRepository<PropertySocialDetailsEntity, int>>();
        _mockSocialDetailsRepository = new Mock<IPropertySocialDetailsRepository>();
        _mockDocumentBindingRepository = new Mock<IRepository<DocumentBindingEntity, int>>();
        _mockDocumentRepository = new Mock<IRepository<DocumentEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Set up empty queryables to prevent NullReferenceException during DTO enrichment
        var emptyBindings = new List<DocumentBindingEntity>().BuildMockDbSet().Object;
        _mockDocumentBindingRepository.Setup(r => r.GetQueryable()).Returns(emptyBindings);

        var emptyDocs = new List<DocumentEntity>().BuildMockDbSet().Object;
        _mockDocumentRepository.Setup(r => r.GetQueryable()).Returns(emptyDocs);

        // Also mock GetQueryable for SocialAttribute repository (social attributes loaded via _socialDetailsRepository)
        var emptyAttributes = new List<SocialAttributeEntity>().BuildMockDbSet().Object;
        _mockSocialDetailsRepository.Setup(r => r.GetActiveSocialAttributesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SocialAttributeEntity>());

        _service = new PropertySocialDetailsService(
            _mockRepository.Object,
            _mockSocialDetailsRepository.Object,
            _mockDocumentBindingRepository.Object,
            _mockDocumentRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object
        );
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsDto()
    {
        // Arrange
        var entityId = 1;
        var entity = new PropertySocialDetailsEntity
        {
            Id = entityId,
            PropertyId = 100,
            SocialAttributeId = 5,
            BitValue = true,
            IntValue = 10,
            DecimalValue = 100.50m,
            TextValue = "Test Value",
            DateValue = DateTime.Now,
            Remark = "Test Remark",
            IsActive = true
        };

        var dto = new PropertySocialDetailsDto
        {
            Id = entityId,
            PropertyId = 100,
            SocialAttributeId = 5,
            BitValue = true,
            IntValue = 10,
            DecimalValue = 100.50m,
            TextValue = "Test Value",
            DateValue = entity.DateValue,
            Remark = "Test Remark",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<PropertySocialDetailsDto>(It.IsAny<PropertySocialDetailsEntity>()))
            .Returns(dto);
        _mockSocialDetailsRepository.Setup(r => r.GetActiveSocialAttributesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SocialAttributeEntity>
            {
                new() { Id = 5, SocialAttributeCode = "HAS_FAMILY_PLANNING", IsActive = true, IsDiscountApplicable = false }
            });

        // Act
        var result = await _service.GetByIdAsync(entityId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Id, result.Id);
        Assert.Equal(dto.PropertyId, result.PropertyId);
        Assert.Equal(dto.SocialAttributeId, result.SocialAttributeId);
        Assert.Equal(dto.BitValue, result.BitValue);
        Assert.Equal(dto.TextValue, result.TextValue);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = 999;
        _mockRepository.Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertySocialDetailsEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(nonExistentId, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreatePropertySocialDetailsDto
        {
            PropertyId = 100,
            SocialAttributeId = 5,
            BitValue = true,
            IntValue = 10,
            DecimalValue = 50.25m,
            TextValue = "New Value",
            DateValue = DateTime.Now,
            Remark = "New Remark",
            IsActive = true
        };

        var entity = new PropertySocialDetailsEntity
        {
            Id = 1,
            PropertyId = createDto.PropertyId,
            SocialAttributeId = createDto.SocialAttributeId,
            BitValue = createDto.BitValue,
            IntValue = createDto.IntValue,
            DecimalValue = createDto.DecimalValue,
            TextValue = createDto.TextValue,
            DateValue = createDto.DateValue,
            Remark = createDto.Remark,
            IsActive = createDto.IsActive
        };

        var dto = new PropertySocialDetailsDto
        {
            Id = 1,
            PropertyId = createDto.PropertyId,
            SocialAttributeId = createDto.SocialAttributeId,
            BitValue = createDto.BitValue,
            IntValue = createDto.IntValue,
            DecimalValue = createDto.DecimalValue,
            TextValue = createDto.TextValue,
            DateValue = createDto.DateValue,
            Remark = createDto.Remark,
            IsActive = createDto.IsActive
        };

        _mockMapper.Setup(m => m.Map<PropertySocialDetailsEntity>(It.IsAny<CreatePropertySocialDetailsDto>()))
            .Returns(entity);
        _mockMapper.Setup(m => m.Map<PropertySocialDetailsDto>(It.IsAny<PropertySocialDetailsEntity>()))
            .Returns(dto);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<PropertySocialDetailsEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.PropertyId, result.PropertyId);
        Assert.Equal(dto.SocialAttributeId, result.SocialAttributeId);
        Assert.Equal(dto.TextValue, result.TextValue);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<PropertySocialDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNullableValues_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreatePropertySocialDetailsDto
        {
            PropertyId = 100,
            SocialAttributeId = 5,
            BitValue = null,
            IntValue = null,
            DecimalValue = null,
            TextValue = null,
            DateValue = null,
            Remark = null,
            IsActive = true
        };

        var entity = new PropertySocialDetailsEntity
        {
            Id = 1,
            PropertyId = createDto.PropertyId,
            SocialAttributeId = createDto.SocialAttributeId,
            IsActive = true
        };

        var dto = new PropertySocialDetailsDto
        {
            Id = 1,
            PropertyId = createDto.PropertyId,
            SocialAttributeId = createDto.SocialAttributeId,
            IsActive = true
        };

        _mockMapper.Setup(m => m.Map<PropertySocialDetailsEntity>(It.IsAny<CreatePropertySocialDetailsDto>()))
            .Returns(entity);
        _mockMapper.Setup(m => m.Map<PropertySocialDetailsDto>(It.IsAny<PropertySocialDetailsEntity>()))
            .Returns(dto);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<PropertySocialDetailsEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.PropertyId, result.PropertyId);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingEntity_ReturnsUpdatedDto()
    {
        // Arrange
        var entityId = 1;
        var updateDto = new UpdatePropertySocialDetailsDto
        {
            PropertyId = 100,
            SocialAttributeId = 5,
            BitValue = false,
            IntValue = 20,
            DecimalValue = 75.50m,
            TextValue = "Updated Value",
            DateValue = DateTime.Now.AddDays(1),
            Remark = "Updated Remark",
            IsActive = true
        };

        var existingEntity = new PropertySocialDetailsEntity
        {
            Id = entityId,
            PropertyId = 100,
            SocialAttributeId = 5,
            BitValue = true,
            IntValue = 10,
            TextValue = "Old Value",
            IsActive = true
        };

        var updatedDto = new PropertySocialDetailsDto
        {
            Id = entityId,
            PropertyId = updateDto.PropertyId,
            SocialAttributeId = updateDto.SocialAttributeId,
            BitValue = updateDto.BitValue,
            IntValue = updateDto.IntValue,
            DecimalValue = updateDto.DecimalValue,
            TextValue = updateDto.TextValue,
            DateValue = updateDto.DateValue,
            Remark = updateDto.Remark,
            IsActive = updateDto.IsActive
        };

        _mockRepository.Setup(r => r.GetByIdAsync(entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _mockMapper.Setup(m => m.Map(It.IsAny<UpdatePropertySocialDetailsDto>(), It.IsAny<PropertySocialDetailsEntity>()))
            .Returns(existingEntity);
        _mockMapper.Setup(m => m.Map<PropertySocialDetailsDto>(It.IsAny<PropertySocialDetailsEntity>()))
            .Returns(updatedDto);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<PropertySocialDetailsEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateAsync(entityId, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updatedDto.TextValue, result.TextValue);
        Assert.Equal(updatedDto.BitValue, result.BitValue);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PropertySocialDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = 999;
        var updateDto = new UpdatePropertySocialDetailsDto
        {
            PropertyId = 100,
            SocialAttributeId = 5
        };

        _mockRepository.Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertySocialDetailsEntity?)null);

        // Act
        var result = await _service.UpdateAsync(nonExistentId, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PropertySocialDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingEntity_ReturnsTrue()
    {
        // Arrange
        var entityId = 1;
        var entity = new PropertySocialDetailsEntity
        {
            Id = entityId,
            PropertyId = 100,
            SocialAttributeId = 5,
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(entityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<PropertySocialDetailsEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(entityId, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<PropertySocialDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentEntity_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = 999;

        _mockRepository.Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertySocialDetailsEntity?)null);

        // Act
        var result = await _service.DeleteAsync(nonExistentId, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<PropertySocialDetailsEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Discount Applicability Filtering Tests

    [Fact]
    public async Task GetPropertySocialInfoAsync_FiltersOutDiscountApplicableBranches()
    {
        // Arrange
        var allAttributes = new List<SocialAttributeEntity>
        {
            // Branch A: Parent allowed, child is discount applicable -> entire branch should be filtered out
            new() { Id = 1, SocialAttributeCode = "PARENT_A", IsDiscountApplicable = false, ParentAttributeId = null, IsActive = true },
            new() { Id = 2, SocialAttributeCode = "CHILD_A", IsDiscountApplicable = true, ParentAttributeId = 1, IsActive = true },
            
            // Branch B: Parent is discount applicable -> entire branch filtered out
            new() { Id = 3, SocialAttributeCode = "PARENT_B", IsDiscountApplicable = true, ParentAttributeId = null, IsActive = true },
            new() { Id = 4, SocialAttributeCode = "CHILD_B", IsDiscountApplicable = false, ParentAttributeId = 3, IsActive = true },

            // Branch C: Parent and child allowed -> entire branch preserved
            new() { Id = 5, SocialAttributeCode = "PARENT_C", IsDiscountApplicable = false, ParentAttributeId = null, IsActive = true },
            new() { Id = 6, SocialAttributeCode = "CHILD_C", IsDiscountApplicable = false, ParentAttributeId = 5, IsActive = true }
        };

        _mockSocialDetailsRepository.Setup(r => r.GetActiveSocialAttributesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allAttributes);
        _mockSocialDetailsRepository.Setup(r => r.GetActiveSocialDetailsByPropertyAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PropertySocialDetailsEntity>());

        // Act
        var result = await _service.GetPropertySocialInfoAsync(100, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.SocialAttributes);
        var activeParent = result.SocialAttributes[0];
        Assert.Equal(5, activeParent.Id);
        Assert.Single(activeParent.Children);
        Assert.Equal(6, activeParent.Children[0].Id);
    }

    [Fact]
    public async Task UpsertPropertySocialInfoAsync_WithDiscountApplicableAttribute_ThrowsPropertyValidationException()
    {
        // Arrange
        var allAttributes = new List<SocialAttributeEntity>
        {
            new() { Id = 1, SocialAttributeCode = "DISCOUNT_ATTR", IsDiscountApplicable = true, ParentAttributeId = null, IsActive = true }
        };

        _mockSocialDetailsRepository.Setup(r => r.GetActiveSocialAttributesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allAttributes);

        var upsertDto = new UpsertPropertySocialInfoDto
        {
            PropertyId = 100,
            UpdatedBy = 1,
            SocialAttributes = new List<PropertySocialInfoItemDto>
            {
                new() { SocialAttributeId = 1, BitValue = true }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.PropertyValidationException>(() =>
            _service.UpsertPropertySocialInfoAsync(upsertDto, CancellationToken.None));
    }

    #endregion
}
