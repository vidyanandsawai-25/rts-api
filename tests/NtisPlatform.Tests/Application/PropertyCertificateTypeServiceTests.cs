using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Master.PropertyCertificateType;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

public class PropertyCertificateTypeServiceTests
{
    private readonly Mock<IRepository<PropertyCertificateTypeMasterEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PropertyCertificateTypeService _service;

    public PropertyCertificateTypeServiceTests()
    {
        _mockRepository = new Mock<IRepository<PropertyCertificateTypeMasterEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockUnitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new PropertyCertificateTypeService(_mockRepository.Object, _mockUnitOfWork.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new PropertyCertificateTypeMasterEntity
        {
            Id = 1,
            CertificateTypeName = "Birth Certificate",
            CertificateTypeCode = "BIRTH_CERT",
            FieldCode = "FIELD001",
            SectionCode = "SECTION001",
            DocumentTypeCode = "DOC001",
            DisplayLabel = "Birth Certificate Label",
            DisplayOrder = 1,
            IsMandatory = true,
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<PropertyCertificateTypeDto>(It.IsAny<PropertyCertificateTypeMasterEntity>()))
            .Returns(new PropertyCertificateTypeDto
            {
                Id = 1,
                CertificateTypeName = "Birth Certificate",
                CertificateTypeCode = "BIRTH_CERT",
                FieldCode = "FIELD001",
                SectionCode = "SECTION001",
                DocumentTypeCode = "DOC001",
                DisplayLabel = "Birth Certificate Label",
                DisplayOrder = 1,
                IsMandatory = true,
                IsActive = true
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Birth Certificate", result.CertificateTypeName);
        Assert.Equal("BIRTH_CERT", result.CertificateTypeCode);
        Assert.Equal("FIELD001", result.FieldCode);
        Assert.Equal("SECTION001", result.SectionCode);
        Assert.Equal("DOC001", result.DocumentTypeCode);
        Assert.Equal("Birth Certificate Label", result.DisplayLabel);
        Assert.Equal(1, result.DisplayOrder);
        Assert.True(result.IsMandatory);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyCertificateTypeMasterEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(9999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<PropertyCertificateTypeMasterEntity>
        {
            new() { Id = 1, CertificateTypeName = "Certificate1", CertificateTypeCode = "CERT1", FieldCode = "F1", SectionCode = "S1", DocumentTypeCode = "D1", DisplayOrder = 1, IsMandatory = true, IsActive = true },
            new() { Id = 2, CertificateTypeName = "Certificate2", CertificateTypeCode = "CERT2", FieldCode = "F2", SectionCode = "S2", DocumentTypeCode = "D2", DisplayOrder = 2, IsMandatory = false, IsActive = true }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PropertyCertificateTypeMasterEntity, PropertyCertificateTypeDto>();
        });

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new PropertyCertificateTypeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new PropertyCertificateTypeQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And,
            SearchTerm = null!,
            SortBy = null!
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.CertificateTypeName == "Certificate1");
        Assert.Contains(items, x => x.CertificateTypeName == "Certificate2");
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreatePropertyCertificateTypeDto
        {
            CertificateTypeName = "New Certificate",
            CertificateTypeCode = "NEW_CERT",
            FieldCode = "FIELD_NEW",
            SectionCode = "SECTION_NEW",
            DocumentTypeCode = "DOC_NEW",
            DisplayLabel = "New Certificate Label",
            DisplayOrder = 3,
            IsMandatory = true,
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<PropertyCertificateTypeMasterEntity>(It.IsAny<CreatePropertyCertificateTypeDto>()))
            .Returns((CreatePropertyCertificateTypeDto dto) => new PropertyCertificateTypeMasterEntity
            {
                CertificateTypeName = dto.CertificateTypeName,
                CertificateTypeCode = dto.CertificateTypeCode,
                FieldCode = dto.FieldCode,
                SectionCode = dto.SectionCode,
                DocumentTypeCode = dto.DocumentTypeCode,
                DisplayLabel = dto.DisplayLabel,
                DisplayOrder = dto.DisplayOrder,
                IsMandatory = dto.IsMandatory,
                CreatedBy = dto.CreatedBy,
                CreatedDate = DateTime.Now,
                IsActive = true
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<PropertyCertificateTypeMasterEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyCertificateTypeMasterEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<PropertyCertificateTypeDto>(It.IsAny<PropertyCertificateTypeMasterEntity>()))
            .Returns((PropertyCertificateTypeMasterEntity e) => new PropertyCertificateTypeDto
            {
                CertificateTypeName = e.CertificateTypeName,
                CertificateTypeCode = e.CertificateTypeCode,
                FieldCode = e.FieldCode,
                SectionCode = e.SectionCode,
                DocumentTypeCode = e.DocumentTypeCode,
                DisplayLabel = e.DisplayLabel,
                DisplayOrder = e.DisplayOrder,
                IsMandatory = e.IsMandatory,
                IsActive = true
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Certificate", result.CertificateTypeName);
        Assert.Equal("NEW_CERT", result.CertificateTypeCode);
        Assert.Equal("FIELD_NEW", result.FieldCode);
        Assert.Equal("SECTION_NEW", result.SectionCode);
        Assert.Equal("DOC_NEW", result.DocumentTypeCode);
        Assert.Equal("New Certificate Label", result.DisplayLabel);
        Assert.Equal(3, result.DisplayOrder);
        Assert.True(result.IsMandatory);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<PropertyCertificateTypeMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdatePropertyCertificateTypeDto
        {
            CertificateTypeName = "Updated Certificate",
            CertificateTypeCode = "UPD_CERT",
            FieldCode = "FIELD_UPD",
            SectionCode = "SECTION_UPD",
            DocumentTypeCode = "DOC_UPD",
            DisplayLabel = "Updated Label",
            DisplayOrder = 5,
            IsMandatory = false,
            IsActive = true
        };

        var existingEntity = new PropertyCertificateTypeMasterEntity
        {
            Id = 1,
            CertificateTypeName = "Old Certificate",
            CertificateTypeCode = "OLD_CERT",
            FieldCode = "FIELD_OLD",
            SectionCode = "SECTION_OLD",
            DocumentTypeCode = "DOC_OLD",
            DisplayLabel = "Old Label",
            DisplayOrder = 1,
            IsMandatory = true,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<PropertyCertificateTypeMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdatePropertyCertificateTypeDto>(), It.IsAny<PropertyCertificateTypeMasterEntity>()))
            .Callback((UpdatePropertyCertificateTypeDto src, PropertyCertificateTypeMasterEntity dest) =>
            {
                dest.CertificateTypeName = src.CertificateTypeName;
                dest.CertificateTypeCode = src.CertificateTypeCode;
                dest.FieldCode = src.FieldCode;
                dest.SectionCode = src.SectionCode;
                dest.DocumentTypeCode = src.DocumentTypeCode;
                dest.DisplayLabel = src.DisplayLabel;
                dest.DisplayOrder = src.DisplayOrder;
                dest.IsMandatory = src.IsMandatory;
            });

        // Act
        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PropertyCertificateTypeMasterEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);

        Assert.Equal("Updated Certificate", existingEntity.CertificateTypeName);
        Assert.Equal("UPD_CERT", existingEntity.CertificateTypeCode);
        Assert.Equal("FIELD_UPD", existingEntity.FieldCode);
        Assert.Equal("SECTION_UPD", existingEntity.SectionCode);
        Assert.Equal("DOC_UPD", existingEntity.DocumentTypeCode);
        Assert.Equal("Updated Label", existingEntity.DisplayLabel);
        Assert.Equal(5, existingEntity.DisplayOrder);
        Assert.False(existingEntity.IsMandatory);
        Assert.True(existingEntity.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
    {
        // Arrange
        var updateDto = new UpdatePropertyCertificateTypeDto
        {
            CertificateTypeName = "Test",
            CertificateTypeCode = "TEST",
            FieldCode = "F1",
            SectionCode = "S1",
            DocumentTypeCode = "D1",
            DisplayOrder = 1,
            IsMandatory = true,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(9999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyCertificateTypeMasterEntity?)null);

        // Act
        await _service.UpdateAsync(9999, updateDto, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PropertyCertificateTypeMasterEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        int idToDelete = 9999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyCertificateTypeMasterEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);

        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        // Arrange
        int idToDelete = 1;

        var existingEntity = new PropertyCertificateTypeMasterEntity
        {
            Id = idToDelete,
            CertificateTypeName = "Certificate to Delete",
            CertificateTypeCode = "DEL_CERT",
            FieldCode = "F_DEL",
            SectionCode = "S_DEL",
            DocumentTypeCode = "D_DEL",
            DisplayOrder = 1,
            IsMandatory = true,
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(idToDelete, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);

        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
