using AutoMapper;
using FluentAssertions;
using Moq;
using NtisPlatform.Application.DTOs.CommonDetails;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class BulkUpdateFieldConfigServiceTests
{
    private readonly Mock<IRepository<BulkUpdateFieldConfigEntity, int>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly BulkUpdateFieldConfigService _service;

    public BulkUpdateFieldConfigServiceTests()
    {
        _repositoryMock = new Mock<IRepository<BulkUpdateFieldConfigEntity, int>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _service = new BulkUpdateFieldConfigService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsBulkUpdateFieldConfigDto()
    {
        // Arrange
        var id = 1;
        var entity = new BulkUpdateFieldConfigEntity
        {
            Id = id,
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType",
            DisplayName = "Property Type",
            DisplayNameMarathi = "मालमत्ता प्रकार",
            ControlType = "Dropdown",
            DataType = "String",
            Placeholder = "Select Property Type",
            IsRequired = true,
            MaxLength = 100,
            ValidationRegex = null,
            DefaultValue = null,
            SequenceNo = 1,
            IsActive = true,
            IsReadonly = false,
            BindApi = "/api/PropertyType",
            CreatedDate = DateTime.Now
        };
        var expectedDto = new BulkUpdateFieldConfigDto
        {
            Id = id,
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType",
            DisplayName = "Property Type",
            DisplayNameMarathi = "मालमत्ता प्रकार",
            ControlType = "Dropdown",
            DataType = "String",
            Placeholder = "Select Property Type",
            IsRequired = true,
            MaxLength = 100,
            ValidationRegex = null,
            DefaultValue = null,
            SequenceNo = 1,
            IsActive = true,
            IsReadonly = false,
            BindApi = "/api/PropertyType"
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _mapperMock.Setup(m => m.Map<BulkUpdateFieldConfigDto>(entity))
            .Returns(expectedDto);

        // Act
        var result = await _service.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedDto);
        _repositoryMock.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var id = 999;
        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BulkUpdateFieldConfigEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _repositoryMock.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedBulkUpdateFieldConfigDto()
    {
        // Arrange
        var createDto = new CreateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = "Ward",
            DisplayName = "Ward",
            DisplayNameMarathi = "प्रभाग",
            ControlType = "Dropdown",
            DataType = "Integer",
            Placeholder = "Select Ward",
            IsRequired = true,
            MaxLength = null,
            ValidationRegex = null,
            DefaultValue = null,
            SequenceNo = 2,
            IsReadonly = false,
            BindApi = "/api/Ward",
            CreatedBy = 1
        };
        var entity = new BulkUpdateFieldConfigEntity
        {
            Id = 0,
            BulkUpdateMasterId = 1,
            FieldName = "Ward",
            DisplayName = "Ward",
            DisplayNameMarathi = "प्रभाग",
            ControlType = "Dropdown",
            DataType = "Integer",
            Placeholder = "Select Ward",
            IsRequired = true,
            MaxLength = null,
            ValidationRegex = null,
            DefaultValue = null,
            SequenceNo = 2,
            IsReadonly = false,
            BindApi = "/api/Ward",
            CreatedBy = 1
        };
        var savedEntity = new BulkUpdateFieldConfigEntity
        {
            Id = 2,
            BulkUpdateMasterId = 1,
            FieldName = "Ward",
            DisplayName = "Ward",
            DisplayNameMarathi = "प्रभाग",
            ControlType = "Dropdown",
            DataType = "Integer",
            Placeholder = "Select Ward",
            IsRequired = true,
            MaxLength = null,
            ValidationRegex = null,
            DefaultValue = null,
            SequenceNo = 2,
            IsActive = true,
            IsReadonly = false,
            BindApi = "/api/Ward",
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };
        var expectedDto = new BulkUpdateFieldConfigDto
        {
            Id = 2,
            BulkUpdateMasterId = 1,
            FieldName = "Ward",
            DisplayName = "Ward",
            DisplayNameMarathi = "प्रभाग",
            ControlType = "Dropdown",
            DataType = "Integer",
            Placeholder = "Select Ward",
            IsRequired = true,
            MaxLength = null,
            ValidationRegex = null,
            DefaultValue = null,
            SequenceNo = 2,
            IsActive = true,
            IsReadonly = false,
            BindApi = "/api/Ward"
        };

        _mapperMock.Setup(m => m.Map<BulkUpdateFieldConfigEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<BulkUpdateFieldConfigEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedEntity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<BulkUpdateFieldConfigDto>(It.IsAny<BulkUpdateFieldConfigEntity>()))
            .Returns(expectedDto);

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(2);
        result.FieldName.Should().Be("Ward");
        result.DisplayName.Should().Be("Ward");
        result.ControlType.Should().Be("Dropdown");
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<BulkUpdateFieldConfigEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateFieldName_ThrowsException()
    {
        // Arrange
        var createDto = new CreateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType", // Already exists
            DisplayName = "Property Type",
            ControlType = "Dropdown",
            DataType = "String",
            SequenceNo = 1,
            CreatedBy = 1
        };
        var entity = new BulkUpdateFieldConfigEntity { FieldName = "PropertyType" };

        _mapperMock.Setup(m => m.Map<BulkUpdateFieldConfigEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<BulkUpdateFieldConfigEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate FieldName for the same BulkUpdateMasterId"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateAsync(createDto, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WithRequiredFieldsOnly_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = "Status",
            DisplayName = "Status",
            DisplayNameMarathi = string.Empty,
            ControlType = "Checkbox",
            DataType = "Boolean",
            SequenceNo = 3,
            CreatedBy = 1
        };
        var entity = new BulkUpdateFieldConfigEntity
        {
            BulkUpdateMasterId = 1,
            FieldName = "Status",
            DisplayName = "Status",
            ControlType = "Checkbox",
            DataType = "Boolean",
            SequenceNo = 3,
            CreatedBy = 1
        };
        var savedEntity = new BulkUpdateFieldConfigEntity
        {
            Id = 3,
            BulkUpdateMasterId = 1,
            FieldName = "Status",
            DisplayName = "Status",
            ControlType = "Checkbox",
            DataType = "Boolean",
            SequenceNo = 3,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };
        var expectedDto = new BulkUpdateFieldConfigDto
        {
            Id = 3,
            BulkUpdateMasterId = 1,
            FieldName = "Status",
            DisplayName = "Status",
            ControlType = "Checkbox",
            DataType = "Boolean",
            SequenceNo = 3,
            IsActive = true
        };

        _mapperMock.Setup(m => m.Map<BulkUpdateFieldConfigEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<BulkUpdateFieldConfigEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedEntity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<BulkUpdateFieldConfigDto>(It.IsAny<BulkUpdateFieldConfigEntity>()))
            .Returns(expectedDto);

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(3);
        result.FieldName.Should().Be("Status");
        result.ControlType.Should().Be("Checkbox");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidIdAndDto_ReturnsUpdatedBulkUpdateFieldConfigDto()
    {
        // Arrange
        var id = 1;
        var updateDto = new UpdateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType",
            DisplayName = "Property Type (Updated)",
            DisplayNameMarathi = "मालमत्ता प्रकार (अद्ययावत)",
            ControlType = "Dropdown",
            DataType = "String",
            Placeholder = "Please Select Property Type",
            IsRequired = true,
            MaxLength = 150,
            ValidationRegex = null,
            DefaultValue = null,
            SequenceNo = 1,
            IsReadonly = false,
            BindApi = "/api/PropertyType/GetAll",
            UpdatedBy = 1
        };
        var existingEntity = new BulkUpdateFieldConfigEntity
        {
            Id = id,
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType",
            DisplayName = "Property Type",
            ControlType = "Dropdown",
            DataType = "String",
            SequenceNo = 1,
            IsActive = true
        };
        var updatedEntity = new BulkUpdateFieldConfigEntity
        {
            Id = id,
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType",
            DisplayName = "Property Type (Updated)",
            DisplayNameMarathi = "मालमत्ता प्रकार (अद्ययावत)",
            ControlType = "Dropdown",
            DataType = "String",
            Placeholder = "Please Select Property Type",
            IsRequired = true,
            MaxLength = 150,
            SequenceNo = 1,
            IsActive = true,
            IsReadonly = false,
            BindApi = "/api/PropertyType/GetAll",
            UpdatedBy = 1,
            UpdatedDate = DateTime.Now
        };
        var expectedDto = new BulkUpdateFieldConfigDto
        {
            Id = id,
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType",
            DisplayName = "Property Type (Updated)",
            DisplayNameMarathi = "मालमत्ता प्रकार (अद्ययावत)",
            ControlType = "Dropdown",
            DataType = "String",
            Placeholder = "Please Select Property Type",
            IsRequired = true,
            MaxLength = 150,
            SequenceNo = 1,
            IsActive = true,
            IsReadonly = false,
            BindApi = "/api/PropertyType/GetAll"
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _mapperMock.Setup(m => m.Map(updateDto, existingEntity))
            .Returns(updatedEntity);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<BulkUpdateFieldConfigEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<BulkUpdateFieldConfigDto>(It.IsAny<BulkUpdateFieldConfigEntity>()))
            .Returns(expectedDto);

        // Act
        var result = await _service.UpdateAsync(id, updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.DisplayName.Should().Be("Property Type (Updated)");
        result.Placeholder.Should().Be("Please Select Property Type");
        result.MaxLength.Should().Be(150);
        result.BindApi.Should().Be("/api/PropertyType/GetAll");
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<BulkUpdateFieldConfigEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var id = 999;
        var updateDto = new UpdateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = "NonExistent",
            DisplayName = "Non Existent",
            ControlType = "TextBox",
            DataType = "String",
            SequenceNo = 1,
            UpdatedBy = 1
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BulkUpdateFieldConfigEntity?)null);

        // Act
        var result = await _service.UpdateAsync(id, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<BulkUpdateFieldConfigEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ChangingControlType_UpdatesSuccessfully()
    {
        // Arrange
        var id = 1;
        var updateDto = new UpdateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = "Description",
            DisplayName = "Description",
            DisplayNameMarathi = "वर्णन",
            ControlType = "TextArea", // Changed from TextBox
            DataType = "String",
            MaxLength = 500,
            SequenceNo = 4,
            UpdatedBy = 1
        };
        var existingEntity = new BulkUpdateFieldConfigEntity
        {
            Id = id,
            FieldName = "Description",
            ControlType = "TextBox",
            DataType = "String",
            SequenceNo = 4,
            IsActive = true
        };
        var updatedEntity = new BulkUpdateFieldConfigEntity
        {
            Id = id,
            FieldName = "Description",
            ControlType = "TextArea",
            DataType = "String",
            MaxLength = 500,
            SequenceNo = 4,
            IsActive = true,
            UpdatedBy = 1
        };
        var expectedDto = new BulkUpdateFieldConfigDto
        {
            Id = id,
            FieldName = "Description",
            ControlType = "TextArea",
            DataType = "String",
            MaxLength = 500,
            SequenceNo = 4,
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _mapperMock.Setup(m => m.Map(updateDto, existingEntity))
            .Returns(updatedEntity);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<BulkUpdateFieldConfigEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<BulkUpdateFieldConfigDto>(It.IsAny<BulkUpdateFieldConfigEntity>()))
            .Returns(expectedDto);

        // Act
        var result = await _service.UpdateAsync(id, updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ControlType.Should().Be("TextArea");
        result.MaxLength.Should().Be(500);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ReturnsTrueAndSoftDeletes()
    {
        // Arrange
        var id = 1;
        var entity = new BulkUpdateFieldConfigEntity
        {
            Id = id,
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType",
            DisplayName = "Property Type",
            ControlType = "Dropdown",
            DataType = "String",
            SequenceNo = 1,
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _repositoryMock.Setup(r => r.DeleteAsync(It.IsAny<BulkUpdateFieldConfigEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.DeleteAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<BulkUpdateFieldConfigEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var id = 999;

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BulkUpdateFieldConfigEntity?)null);

        // Act
        var result = await _service.DeleteAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<BulkUpdateFieldConfigEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Edge Cases and Validation Tests

    [Fact]
    public async Task CreateAsync_WithMaxLengthValidation_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = "Email",
            DisplayName = "Email Address",
            DisplayNameMarathi = "ईमेल पत्ता",
            ControlType = "TextBox",
            DataType = "String",
            Placeholder = "Enter email",
            IsRequired = true,
            MaxLength = 255,
            ValidationRegex = @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$",
            SequenceNo = 5,
            CreatedBy = 1
        };
        var entity = new BulkUpdateFieldConfigEntity
        {
            BulkUpdateMasterId = 1,
            FieldName = "Email",
            DisplayName = "Email Address",
            ControlType = "TextBox",
            DataType = "String",
            MaxLength = 255,
            ValidationRegex = @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$",
            SequenceNo = 5,
            CreatedBy = 1
        };
        var savedEntity = new BulkUpdateFieldConfigEntity
        {
            Id = 5,
            BulkUpdateMasterId = 1,
            FieldName = "Email",
            DisplayName = "Email Address",
            ControlType = "TextBox",
            DataType = "String",
            MaxLength = 255,
            ValidationRegex = @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$",
            SequenceNo = 5,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };
        var expectedDto = new BulkUpdateFieldConfigDto
        {
            Id = 5,
            BulkUpdateMasterId = 1,
            FieldName = "Email",
            DisplayName = "Email Address",
            ControlType = "TextBox",
            DataType = "String",
            MaxLength = 255,
            ValidationRegex = @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$",
            SequenceNo = 5,
            IsActive = true
        };

        _mapperMock.Setup(m => m.Map<BulkUpdateFieldConfigEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<BulkUpdateFieldConfigEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedEntity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<BulkUpdateFieldConfigDto>(It.IsAny<BulkUpdateFieldConfigEntity>()))
            .Returns(expectedDto);

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ValidationRegex.Should().NotBeNull();
        result.MaxLength.Should().Be(255);
    }

    [Fact]
    public async Task CreateAsync_WithReadonlyField_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = "PropertyId",
            DisplayName = "Property ID",
            DisplayNameMarathi = "मालमत्ता आयडी",
            ControlType = "TextBox",
            DataType = "String",
            IsRequired = false,
            IsReadonly = true, // Readonly field
            SequenceNo = 6,
            CreatedBy = 1
        };
        var entity = new BulkUpdateFieldConfigEntity
        {
            BulkUpdateMasterId = 1,
            FieldName = "PropertyId",
            DisplayName = "Property ID",
            ControlType = "TextBox",
            DataType = "String",
            IsReadonly = true,
            SequenceNo = 6,
            CreatedBy = 1
        };
        var savedEntity = new BulkUpdateFieldConfigEntity
        {
            Id = 6,
            BulkUpdateMasterId = 1,
            FieldName = "PropertyId",
            DisplayName = "Property ID",
            ControlType = "TextBox",
            DataType = "String",
            IsReadonly = true,
            SequenceNo = 6,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };
        var expectedDto = new BulkUpdateFieldConfigDto
        {
            Id = 6,
            BulkUpdateMasterId = 1,
            FieldName = "PropertyId",
            DisplayName = "Property ID",
            ControlType = "TextBox",
            DataType = "String",
            IsReadonly = true,
            SequenceNo = 6,
            IsActive = true
        };

        _mapperMock.Setup(m => m.Map<BulkUpdateFieldConfigEntity>(createDto))
            .Returns(entity);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<BulkUpdateFieldConfigEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedEntity);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<BulkUpdateFieldConfigDto>(It.IsAny<BulkUpdateFieldConfigEntity>()))
            .Returns(expectedDto);

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsReadonly.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ChangeSequenceNo_UpdatesSuccessfully()
    {
        // Arrange
        var id = 1;
        var updateDto = new UpdateBulkUpdateFieldConfigDto
        {
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType",
            DisplayName = "Property Type",
            ControlType = "Dropdown",
            DataType = "String",
            SequenceNo = 10, // Changed from 1 to 10
            UpdatedBy = 1
        };
        var existingEntity = new BulkUpdateFieldConfigEntity
        {
            Id = id,
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType",
            SequenceNo = 1,
            IsActive = true
        };
        var updatedEntity = new BulkUpdateFieldConfigEntity
        {
            Id = id,
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType",
            SequenceNo = 10,
            IsActive = true,
            UpdatedBy = 1
        };
        var expectedDto = new BulkUpdateFieldConfigDto
        {
            Id = id,
            BulkUpdateMasterId = 1,
            FieldName = "PropertyType",
            SequenceNo = 10,
            IsActive = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);
        _mapperMock.Setup(m => m.Map(updateDto, existingEntity))
            .Returns(updatedEntity);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<BulkUpdateFieldConfigEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mapperMock.Setup(m => m.Map<BulkUpdateFieldConfigDto>(It.IsAny<BulkUpdateFieldConfigEntity>()))
            .Returns(expectedDto);

        // Act
        var result = await _service.UpdateAsync(id, updateDto, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.SequenceNo.Should().Be(10);
    }

    #endregion
}
