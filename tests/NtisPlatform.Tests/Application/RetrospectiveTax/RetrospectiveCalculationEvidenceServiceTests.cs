using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveCalculationEvidence;
using NtisPlatform.Application.Services.RetrospectiveTax;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.RetrospectiveTax;

public class RetrospectiveCalculationEvidenceServiceTests
{
    private readonly Mock<IRepository<RetrospectiveCalculationEvidenceEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly RetrospectiveCalculationEvidenceService _service;

    public RetrospectiveCalculationEvidenceServiceTests()
    {
        _mockRepository = new Mock<IRepository<RetrospectiveCalculationEvidenceEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new RetrospectiveCalculationEvidenceService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = new RetrospectiveCalculationEvidenceEntity
        {
            Id = 1,
            CalculationId = 10,
            EvidenceTypeId = 1,
            EvidenceDate = new DateTime(2024, 4, 1),
            IsAvailable = true,
            SourceReference = "CERT-001",
            CreatedDate = DateTime.Now,
            Calculation = new RetrospectiveTaxCalculationEntity { Id = 10, PropertyId = 100, CalculationMode = "PROPERTY" },
            EvidenceType = new EvidenceTypeMasterEntity { Id = 1, EvidenceCode = "OC", EvidenceName = "Occupancy Certificate" }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<RetrospectiveCalculationEvidenceDto>(It.IsAny<RetrospectiveCalculationEvidenceEntity>()))
            .Returns((RetrospectiveCalculationEvidenceEntity e) => new RetrospectiveCalculationEvidenceDto
            {
                Id = e.Id,
                CalculationId = e.CalculationId,
                EvidenceTypeId = e.EvidenceTypeId,
                EvidenceDate = e.EvidenceDate,
                IsAvailable = e.IsAvailable,
                SourceReference = e.SourceReference,
                CreatedDate = e.CreatedDate
            });

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.CalculationId);
        Assert.Equal(1, result.EvidenceTypeId);
        Assert.True(result.IsAvailable);
        Assert.Equal("CERT-001", result.SourceReference);
        Assert.NotNull(entity.Calculation);
        Assert.NotNull(entity.EvidenceType);
        Assert.Equal("PROPERTY", entity.Calculation!.CalculationMode);
        Assert.Equal("OC", entity.EvidenceType!.EvidenceCode);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveCalculationEvidenceEntity?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        var entities = new List<RetrospectiveCalculationEvidenceEntity>
        {
            new() { Id = 1, CalculationId = 10, EvidenceTypeId = 1, IsAvailable = true, CreatedDate = DateTime.Now },
            new() { Id = 2, CalculationId = 10, EvidenceTypeId = 2, IsAvailable = false, CreatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<RetrospectiveCalculationEvidenceEntity, RetrospectiveCalculationEvidenceDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new RetrospectiveCalculationEvidenceService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new RetrospectiveCalculationEvidenceQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And
        };

        var result = await service.GetAllAsync(qp, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.EvidenceTypeId == 1);
        Assert.Contains(items, x => x.EvidenceTypeId == 2);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        var createDto = new CreateRetrospectiveCalculationEvidenceDto
        {
            CalculationId = 10,
            EvidenceTypeId = 1,
            EvidenceDate = DateTime.Now,
            IsAvailable = true,
            SourceReference = "CERT-001",
            IsActive = true,
            CreatedBy = 3
        };

        _mockMapper
            .Setup(m => m.Map<RetrospectiveCalculationEvidenceEntity>(It.IsAny<CreateRetrospectiveCalculationEvidenceDto>()))
            .Returns((CreateRetrospectiveCalculationEvidenceDto dto) => new RetrospectiveCalculationEvidenceEntity
            {
                Id = 1,
                CalculationId = dto.CalculationId,
                EvidenceTypeId = dto.EvidenceTypeId,
                EvidenceDate = dto.EvidenceDate,
                IsAvailable = dto.IsAvailable,
                SourceReference = dto.SourceReference,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy,
                CreatedDate = DateTime.Now
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RetrospectiveCalculationEvidenceEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveCalculationEvidenceEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<RetrospectiveCalculationEvidenceDto>(It.IsAny<RetrospectiveCalculationEvidenceEntity>()))
            .Returns((RetrospectiveCalculationEvidenceEntity e) => new RetrospectiveCalculationEvidenceDto
            {
                Id = e.Id,
                CalculationId = e.CalculationId,
                EvidenceTypeId = e.EvidenceTypeId,
                IsAvailable = e.IsAvailable,
                SourceReference = e.SourceReference
            });

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.CalculationId);
        Assert.True(result.IsAvailable);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectiveCalculationEvidenceEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        var updateDto = new UpdateRetrospectiveCalculationEvidenceDto
        {
            CalculationId = 10,
            EvidenceTypeId = 2,
            EvidenceDate = new DateTime(2024, 6, 1),
            IsAvailable = false,
            SourceReference = "UPDATED",
            IsActive = true,
            UpdatedBy = 9
        };

        var existingEntity = new RetrospectiveCalculationEvidenceEntity
        {
            Id = 1,
            CalculationId = 10,
            EvidenceTypeId = 1,
            IsAvailable = true,
            SourceReference = "CERT-001",
            CreatedDate = DateTime.Now
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RetrospectiveCalculationEvidenceEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateRetrospectiveCalculationEvidenceDto>(), It.IsAny<RetrospectiveCalculationEvidenceEntity>()))
            .Callback((UpdateRetrospectiveCalculationEvidenceDto src, RetrospectiveCalculationEvidenceEntity dest) =>
            {
                dest.CalculationId = src.CalculationId;
                dest.EvidenceTypeId = src.EvidenceTypeId;
                dest.EvidenceDate = src.EvidenceDate;
                dest.IsAvailable = src.IsAvailable;
                dest.SourceReference = src.SourceReference;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy;
                dest.UpdatedDate = DateTime.Now;
            });

        _mockMapper
            .Setup(m => m.Map<RetrospectiveCalculationEvidenceDto>(It.IsAny<RetrospectiveCalculationEvidenceEntity>()))
            .Returns((RetrospectiveCalculationEvidenceEntity e) => new RetrospectiveCalculationEvidenceDto
            {
                Id = e.Id,
                EvidenceTypeId = e.EvidenceTypeId,
                IsAvailable = e.IsAvailable,
                SourceReference = e.SourceReference,
                IsActive = e.IsActive,
                UpdatedDate = e.UpdatedDate
            });

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveCalculationEvidenceEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(2, existingEntity.EvidenceTypeId);
        Assert.False(existingEntity.IsAvailable);
        Assert.Equal("UPDATED", existingEntity.SourceReference);
        Assert.Equal(10, existingEntity.CalculationId);
        Assert.Equal(new DateTime(2024, 6, 1), existingEntity.EvidenceDate);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        var updateDto = new UpdateRetrospectiveCalculationEvidenceDto { CalculationId = 10, EvidenceTypeId = 1, IsAvailable = true };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveCalculationEvidenceEntity?)null);

        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveCalculationEvidenceEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        var idToDelete = 1;
        var existingEntity = new RetrospectiveCalculationEvidenceEntity { Id = 1, CalculationId = 10, EvidenceTypeId = 1 };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<RetrospectiveCalculationEvidenceEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RetrospectiveCalculationEvidenceEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveCalculationEvidenceEntity?)null);

        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RetrospectiveCalculationEvidenceEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
