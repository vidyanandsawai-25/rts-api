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

public class AssetAuthorityMasterServiceTests
{
    private readonly Mock<IRepository<AssetAuthorityMasterEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly Mock<IHardDeleteCleanupService> _mockCleanupService;
    private readonly Mock<ILogger<AssetAuthorityMasterController>> _mockLogger;
    private readonly AssetAuthorityMasterService _service;

    public AssetAuthorityMasterServiceTests()
    {
        _mockRepository = new Mock<IRepository<AssetAuthorityMasterEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();
        _mockCleanupService = new Mock<IHardDeleteCleanupService>();
        _mockLogger = new Mock<ILogger<AssetAuthorityMasterController>>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new AssetAuthorityMasterService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockReferenceValidator.Object);
    }

    private static AssetAuthorityMasterEntity CreateEntity(
        int id = 1,
        string authorityCode = "AUTH001",
        string authorityName = "State Authority",
        bool isActive = true)
    {
        return new AssetAuthorityMasterEntity
        {
            Id = id,
            AuthorityCode = authorityCode,
            AuthorityName = authorityName,
            State = "MH",
            IsActive = isActive,
            CreatedBy = 1,
            CreatedDate = DateTime.UtcNow,
            MarkedForDeletion = false,
            MarkedForDeletionDate = null
        };
    }

    [Fact]
    public void TestAllProperties_ToEnsureCodeCoverage()
    {
        // Entity
        var entity = new AssetAuthorityMasterEntity
        {
            Id = 1,
            AuthorityCode = "A",
            AuthorityName = "B",
            State = "C",
            IsActive = true,
            MarkedForDeletion = true,
            MarkedForDeletionDate = DateTime.UtcNow
        };
        Assert.Equal(1, entity.Id);
        Assert.Equal("A", entity.AuthorityCode);
        Assert.Equal("B", entity.AuthorityName);
        Assert.Equal("C", entity.State);
        Assert.True(entity.IsActive);
        Assert.True(entity.MarkedForDeletion);
        Assert.NotNull(entity.MarkedForDeletionDate);

        // DTOs
        var dto = new AssetAuthorityMasterDto
        {
            Id = 1,
            AuthorityCode = "A",
            AuthorityName = "B",
            State = "C"
        };
        Assert.Equal(1, dto.Id);
        Assert.Equal("A", dto.AuthorityCode);
        Assert.Equal("B", dto.AuthorityName);
        Assert.Equal("C", dto.State);

        var createDto = new CreateAssetAuthorityMasterDto
        {
            AuthorityCode = "A",
            AuthorityName = "B",
            State = "C",
            CreatedBy = 2
        };
        Assert.Equal("A", createDto.AuthorityCode);
        Assert.Equal("B", createDto.AuthorityName);
        Assert.Equal("C", createDto.State);
        Assert.Equal(2, createDto.CreatedBy);

        var updateDto = new UpdateAssetAuthorityMasterDto
        {
            AuthorityCode = "A",
            AuthorityName = "B",
            State = "C",
            UpdatedBy = 3,
            IsActive = false
        };
        Assert.Equal("A", updateDto.AuthorityCode);
        Assert.Equal("B", updateDto.AuthorityName);
        Assert.Equal("C", updateDto.State);
        Assert.Equal(3, updateDto.UpdatedBy);
        Assert.False(updateDto.IsActive);

        // Query parameters
        var qp = new AssetAuthorityMasterQueryParameters
        {
            AuthorityCode = "A",
            AuthorityName = "B",
            State = "C",
            IsActive = true,
            PageNumber = 1,
            PageSize = 10
        };
        Assert.Equal("A", qp.AuthorityCode);
        Assert.Equal("B", qp.AuthorityName);
        Assert.Equal("C", qp.State);
        Assert.True(qp.IsActive);
        Assert.Equal(1, qp.PageNumber);
        Assert.Equal(10, qp.PageSize);
    }

    [Fact]
    public void TestMappingProfile()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AssetAuthorityMasterMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = CreateEntity(1);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<AssetAuthorityMasterDto>(entity)).Returns(new AssetAuthorityMasterDto { Id = 1, AuthorityCode = "AUTH001" });

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResult()
    {
        var entities = new List<AssetAuthorityMasterEntity> { CreateEntity(1), CreateEntity(2) };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(entities.BuildMock());

        var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile<AssetAuthorityMasterMappingProfile>(), Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        var service = new AssetAuthorityMasterService(_mockRepository.Object, _mockUnitOfWork.Object, mapperConfig.CreateMapper(), _mockReferenceValidator.Object);

        var qp = new AssetAuthorityMasterQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await service.GetAllAsync(qp);

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_Succeeds()
    {
        var dto = new CreateAssetAuthorityMasterDto { AuthorityCode = "AUTH001", AuthorityName = "Test" };
        var entity = CreateEntity(0, "AUTH001", "Test");

        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetAuthorityMasterEntity>().BuildMock());
        _mockMapper.Setup(m => m.Map<AssetAuthorityMasterEntity>(dto)).Returns(entity);
        _mockRepository.Setup(r => r.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<AssetAuthorityMasterDto>(entity)).Returns(new AssetAuthorityMasterDto { Id = 1 });

        var result = await _service.CreateAsync(dto);

        Assert.NotNull(result);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Duplicate_ThrowsValidationException()
    {
        var dto = new CreateAssetAuthorityMasterDto { AuthorityCode = "AUTH001" };
        var existing = CreateEntity(1, "AUTH001");
        _mockRepository.Setup(r => r.GetQueryable()).Returns(new List<AssetAuthorityMasterEntity> { existing }.BuildMock());
        _mockMapper.Setup(m => m.Map<AssetAuthorityMasterEntity>(dto)).Returns(CreateEntity(0, "AUTH001"));

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task UpdateAsync_DeactivateWithReferences_ThrowsValidationException()
    {
        var dto = new UpdateAssetAuthorityMasterDto { AuthorityCode = "AUTH001", IsActive = false };
        var existing = CreateEntity(1, isActive: true);

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockReferenceValidator.Setup(rv => rv.ValidateReferencesAsync<AssetAuthorityMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Referenced"));
        _mockMapper.Setup(m => m.Map(dto, existing)).Returns(existing).Callback<UpdateAssetAuthorityMasterDto, AssetAuthorityMasterEntity>((d, e) => e.IsActive = d.IsActive);

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(1, dto));
    }

    [Fact]
    public async Task DeleteAsync_WithReferences_ThrowsValidationException()
    {
        var existing = CreateEntity(1);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockReferenceValidator.Setup(rv => rv.ValidateReferencesAsync<AssetAuthorityMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Failure("Referenced"));

        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(1));
    }

    [Fact]
    public async Task DeleteAsync_NoReferences_Succeeds()
    {
        var existing = CreateEntity(1);
        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _mockReferenceValidator.Setup(rv => rv.ValidateReferencesAsync<AssetAuthorityMasterEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());
        _mockRepository.Setup(r => r.DeleteAsync(existing, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(1);

        Assert.True(result);
    }

    [Fact]
    public async Task Controller_GetAll_ReturnsOk()
    {
        var mockService = new Mock<IAssetAuthorityMasterService>();
        mockService.Setup(s => s.GetAllAsync(It.IsAny<AssetAuthorityMasterQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AssetAuthorityMasterDto>(new List<AssetAuthorityMasterDto>(), 0, 1, 10));
        var controller = new AssetAuthorityMasterController(mockService.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, _mockLogger.Object);
        var qp = new AssetAuthorityMasterQueryParameters();

        var result = await controller.GetAll(qp, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_GetById_ReturnsOk()
    {
        var mockService = new Mock<IAssetAuthorityMasterService>();
        mockService.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetAuthorityMasterDto { Id = 1 });
        var controller = new AssetAuthorityMasterController(mockService.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, _mockLogger.Object);

        var result = await controller.GetById(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Create_ReturnsOk()
    {
        var mockService = new Mock<IAssetAuthorityMasterService>();
        var dto = new CreateAssetAuthorityMasterDto { AuthorityCode = "AUTH001" };
        mockService.Setup(s => s.CreateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetAuthorityMasterDto { Id = 1 });
        var controller = new AssetAuthorityMasterController(mockService.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, _mockLogger.Object);

        var result = await controller.Create(dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Update_ReturnsOk()
    {
        var mockService = new Mock<IAssetAuthorityMasterService>();
        var dto = new UpdateAssetAuthorityMasterDto { AuthorityCode = "AUTH001" };
        mockService.Setup(s => s.UpdateAsync(1, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetAuthorityMasterDto { Id = 1 });
        var controller = new AssetAuthorityMasterController(mockService.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, _mockLogger.Object);

        var result = await controller.Update(1, dto, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Delete_ReturnsOk()
    {
        var mockService = new Mock<IAssetAuthorityMasterService>();
        mockService.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = new AssetAuthorityMasterController(mockService.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, _mockLogger.Object);

        var result = await controller.Delete(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Controller_Purge_ReturnsOk()
    {
        var mockService = new Mock<IAssetAuthorityMasterService>();
        var controller = new AssetAuthorityMasterController(mockService.Object, _mockCleanupService.Object, _mockReferenceValidator.Object, _mockLogger.Object);
        _mockCleanupService.Setup(c => c.ForceHardDeleteAsync<AssetAuthorityMasterEntity, int>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.Purge(1, CancellationToken.None);
        Assert.IsType<OkObjectResult>(result);
    }
}
