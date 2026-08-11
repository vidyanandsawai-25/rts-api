using AutoMapper;
using MockQueryable;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NtisPlatform.Api.Controllers.Master;
using NtisPlatform.Application.DTOs.Master;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Application.Interfaces.Master;

namespace NtisPlatform.Tests.Application;

public class AssetOrganizationMasterServiceTests
{
    private readonly Mock<IRepository<AssetOrganizationMasterEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly Mock<IHardDeleteCleanupService> _mockCleanupService;
    private readonly Mock<ILogger<AssetOrganizationMasterController>> _mockLogger;
    private readonly AssetOrganizationMasterService _service;

    public AssetOrganizationMasterServiceTests()
    {
        _mockRepository = new Mock<IRepository<AssetOrganizationMasterEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();
        _mockCleanupService = new Mock<IHardDeleteCleanupService>();
        _mockLogger = new Mock<ILogger<AssetOrganizationMasterController>>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new AssetOrganizationMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockReferenceValidator.Object);
    }

    private static AssetOrganizationMasterEntity CreateEntity(
        int id = 1,
        int authorityId = 10,
        string organizationCode = "ORG001",
        string organizationName = "Municipal Org",
        bool isActive = true)
    {
        return new AssetOrganizationMasterEntity
        {
            Id = id,
            AuthorityId = authorityId,
            OrganizationCode = organizationCode,
            OrganizationName = organizationName,
            IsActive = isActive,
            CreatedBy = 1,
            CreatedDate = DateTime.Now,
            MarkedForDeletion = false,
            MarkedForDeletionDate = null
        };
    }

    [Fact]
    public void TestAllProperties_ToEnsureCodeCoverage()
    {
        // Entity
        var entity = new AssetOrganizationMasterEntity
        {
            Id = 1,
            AuthorityId = 2,
            OrganizationCode = "A",
            OrganizationName = "B",
            IsActive = true,
            MarkedForDeletion = true,
            MarkedForDeletionDate = DateTime.Now
        };
        Assert.Equal(1, entity.Id);
        Assert.Equal(2, entity.AuthorityId);
        Assert.Equal("A", entity.OrganizationCode);
        Assert.Equal("B", entity.OrganizationName);
        Assert.True(entity.IsActive);
        Assert.True(entity.MarkedForDeletion);
        Assert.NotNull(entity.MarkedForDeletionDate);

        // DTOs
        var dto = new AssetOrganizationMasterDto
        {
            Id = 1,
            AuthorityId = 2,
            OrganizationCode = "A",
            OrganizationName = "B"
        };
        Assert.Equal(1, dto.Id);
        Assert.Equal(2, dto.AuthorityId);
        Assert.Equal("A", dto.OrganizationCode);
        Assert.Equal("B", dto.OrganizationName);

        var createDto = new CreateAssetOrganizationMasterDto
        {
            AuthorityId = 2,
            OrganizationCode = "A",
            OrganizationName = "B",
            CreatedBy = 2
        };
        Assert.Equal(2, createDto.AuthorityId);
        Assert.Equal("A", createDto.OrganizationCode);
        Assert.Equal("B", createDto.OrganizationName);
        Assert.Equal(2, createDto.CreatedBy);

        var updateDto = new UpdateAssetOrganizationMasterDto
        {
            AuthorityId = 2,
            OrganizationCode = "A",
            OrganizationName = "B",
            UpdatedBy = 3,
            IsActive = false
        };
        Assert.Equal(2, updateDto.AuthorityId);
        Assert.Equal("A", updateDto.OrganizationCode);
        Assert.Equal("B", updateDto.OrganizationName);
        Assert.Equal(3, updateDto.UpdatedBy);
        Assert.False(updateDto.IsActive);

        // Query parameters
        var qp = new AssetOrganizationMasterQueryParameters
        {
            AuthorityId = 2,
            OrganizationCode = "A",
            OrganizationName = "B",
            IsActive = true,
            PageNumber = 1,
            PageSize = 10
        };
        Assert.Equal(2, qp.AuthorityId);
        Assert.Equal("A", qp.OrganizationCode);
        Assert.Equal("B", qp.OrganizationName);
        Assert.True(qp.IsActive);
        Assert.Equal(1, qp.PageNumber);
        Assert.Equal(10, qp.PageSize);
    }

    [Fact]
    public void TestMappingProfile()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AssetOrganizationMasterMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = CreateEntity(1);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<AssetOrganizationMasterDto>(entity)).Returns(new AssetOrganizationMasterDto { Id = 1, OrganizationCode = "ORG001" });

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResult()
    {
        var entities = new List<AssetOrganizationMasterEntity> { CreateEntity(1), CreateEntity(2) };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMock());

        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<AssetOrganizationMasterMappingProfile>(), Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        var service = new AssetOrganizationMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapperConfig.CreateMapper(), _mockReferenceValidator.Object);

        var qp = new AssetOrganizationMasterQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await service.GetAllAsync(qp);

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_Succeeds()
    {
        var dto = new CreateAssetOrganizationMasterDto { AuthorityId = 10, OrganizationCode = "ORG001", OrganizationName = "Test" };
        var entity = CreateEntity(0, 10, "ORG001", "Test");

        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetOrganizationMasterEntity>().BuildMock());
        _mockMapper.Setup(m => m.Map<AssetOrganizationMasterEntity>(dto)).Returns(entity);
        _mockRepository.Setup(r => r.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<AssetOrganizationMasterDto>(entity)).Returns(new AssetOrganizationMasterDto { Id = 1 });

        var result = await _service.CreateAsync(dto);

        Assert.NotNull(result);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Duplicate_ThrowsValidationException()
    {
        var dto = new CreateAssetOrganizationMasterDto { AuthorityId = 10, OrganizationCode = "ORG001" };
        var existing = CreateEntity(1, 10, "ORG001");
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetOrganizationMasterEntity> { existing }.BuildMock());
        _mockMapper.Setup(m => m.Map<AssetOrganizationMasterEntity>(dto)).Returns(CreateEntity(0, 10, "ORG001"));

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task UpdateAsync_DeactivateWithReferences_ThrowsValidationException()
    {
        var dto = new UpdateAssetOrganizationMasterDto { AuthorityId = 10, OrganizationCode = "ORG001", IsActive = false };
        var existing = CreateEntity(1, isActive: true);

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockReferenceValidator.Setup(rv => rv.ValidateReferencesAsync<AssetOrganizationMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Referenced"));
        _mockMapper.Setup(m => m.Map(dto, existing)).Returns(existing).Callback<UpdateAssetOrganizationMasterDto, AssetOrganizationMasterEntity>((d, e) => e.IsActive = d.IsActive);

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, dto));
    }

    [Fact]
    public async Task DeleteAsync_WithReferences_ThrowsValidationException()
    {
        var existing = CreateEntity(1);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockReferenceValidator.Setup(rv => rv.ValidateReferencesAsync<AssetOrganizationMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Referenced"));

        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(1));
    }

    [Fact]
    public async Task DeleteAsync_NoReferences_Succeeds()
    {
        var existing = CreateEntity(1);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockReferenceValidator.Setup(rv => rv.ValidateReferencesAsync<AssetOrganizationMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());
        _mockRepository.Setup(r => r.DeleteAsync(existing, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(1);

        Assert.True(result);
    }

    [Fact]
    public async Task Controller_GetAll_ReturnsOk()
    {
        var mockService = new Mock<IAssetOrganizationMasterService>();
        mockService.Setup(s => s.GetAllAsync(It.IsAny<AssetOrganizationMasterQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AssetOrganizationMasterDto>(new List<AssetOrganizationMasterDto>(), 0, 1, 10));
        var controller = new AssetOrganizationMasterController(mockService.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, _mockLogger.Object);
        var qp = new AssetOrganizationMasterQueryParameters();

        var result = await controller.GetAll(qp, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_GetById_ReturnsOk()
    {
        var mockService = new Mock<IAssetOrganizationMasterService>();
        mockService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetOrganizationMasterDto { Id = 1 });
        var controller = new AssetOrganizationMasterController(mockService.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, _mockLogger.Object);

        var result = await controller.GetById(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Create_ReturnsOk()
    {
        var mockService = new Mock<IAssetOrganizationMasterService>();
        var dto = new CreateAssetOrganizationMasterDto { OrganizationCode = "ORG001" };
        mockService.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetOrganizationMasterDto { Id = 1 });
        var controller = new AssetOrganizationMasterController(mockService.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, _mockLogger.Object);

        var result = await controller.Create(dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Update_ReturnsOk()
    {
        var mockService = new Mock<IAssetOrganizationMasterService>();
        var dto = new UpdateAssetOrganizationMasterDto { OrganizationCode = "ORG001" };
        mockService.Setup(s => s.UpdateAsync(1, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetOrganizationMasterDto { Id = 1 });
        var controller = new AssetOrganizationMasterController(mockService.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, _mockLogger.Object);

        var result = await controller.Update(1, dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Delete_ReturnsOk()
    {
        var mockService = new Mock<IAssetOrganizationMasterService>();
        mockService.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = new AssetOrganizationMasterController(mockService.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, _mockLogger.Object);

        var result = await controller.Delete(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Purge_ReturnsOk()
    {
        var mockService = new Mock<IAssetOrganizationMasterService>();
        var controller = new AssetOrganizationMasterController(mockService.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, _mockLogger.Object);
        _mockCleanupService.Setup(c => c.ForceHardDeleteAsync<AssetOrganizationMasterEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.Purge(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }
}
