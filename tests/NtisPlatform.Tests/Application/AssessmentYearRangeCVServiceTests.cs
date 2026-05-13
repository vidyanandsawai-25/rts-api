using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using ValidationResult = NtisPlatform.Application.Models.ValidationResult;

namespace NtisPlatform.Tests.Application;

public class AssessmentYearRangeCVServiceTests
{
    private readonly Mock<IRepository<AssessmentYearRangeCVEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IReferenceValidationService> _mockReferenceValidator;
    private readonly AssessmentYearRangeCVService _service;

    public AssessmentYearRangeCVServiceTests()
    {
        _mockRepository = new Mock<IRepository<AssessmentYearRangeCVEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockReferenceValidator = new Mock<IReferenceValidationService>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new AssessmentYearRangeCVService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockReferenceValidator.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = new AssessmentYearRangeCVEntity
        {

            Id = 1,
            FromYear = 2000,
            ToYear = 2020,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 1
        }; 

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<AssessmentYearRangeCVDto>(It.IsAny<AssessmentYearRangeCVEntity>()))
            .Returns(new AssessmentYearRangeCVDto
            {

                Id = 1,
                FromYear = entity.FromYear,
                ToYear = entity.ToYear,
                IsActive = true,
                CreatedDate = entity.CreatedDate ?? DateTime.Now,
                UpdatedDate = entity.UpdatedDate
            });

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);

        Assert.Equal(1, result.Id);
        Assert.Equal(2000, result.FromYear);
        Assert.Equal(2020, result.ToYear);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssessmentYearRangeCVEntity?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        var entities = new List<AssessmentYearRangeCVEntity>
        {

            new() { Id = 1, FromYear = 2000, ToYear = 2020, IsActive = true, CreatedDate = DateTime.Now },
            new() { Id = 2, FromYear = 2021, ToYear = 2030, IsActive = false, CreatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<AssessmentYearRangeCVEntity, AssessmentYearRangeCVDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new AssessmentYearRangeCVService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper,
            _mockReferenceValidator.Object);

        var qp = new AssessmentYearRangeCVQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
        };

        var result = await service.GetAllAsync(qp, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.Id == 1);
        Assert.Contains(items, x => x.Id == 2);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Setup GetQueryable to return empty list for overlap validation
        var existingRanges = new List<AssessmentYearRangeCVEntity>();
        var mockQuery = existingRanges.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var createDto = new CreateAssessmentYearRangeCVDto
        {
            FromYear = 2000,
            ToYear = 2020,
            IsActive = true,
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<AssessmentYearRangeCVEntity>(It.IsAny<CreateAssessmentYearRangeCVDto>()))
            .Returns((CreateAssessmentYearRangeCVDto dto) => new AssessmentYearRangeCVEntity
            {

                Id = 1,
                FromYear = dto.FromYear,
                ToYear = dto.ToYear,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy ?? 0,
                CreatedDate = DateTime.Now
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<AssessmentYearRangeCVEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssessmentYearRangeCVEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<AssessmentYearRangeCVDto>(It.IsAny<AssessmentYearRangeCVEntity>()))
            .Returns((AssessmentYearRangeCVEntity e) => new AssessmentYearRangeCVDto
            {

                Id = e.Id,
                FromYear = e.FromYear,
                ToYear = e.ToYear,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate ?? DateTime.Now,
                UpdatedDate = e.UpdatedDate
            });

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);

        Assert.Equal(1, result.Id);
        Assert.Equal(2000, result.FromYear);
        Assert.Equal(2020, result.ToYear);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<AssessmentYearRangeCVEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        var updateDto = new UpdateAssessmentYearRangeCVDto
        {
            FromYear = 2010,
            ToYear = 2025,
            IsActive = false,
            UpdatedBy = 2
        };

        var existingEntity = new AssessmentYearRangeCVEntity
        {

            Id = 1,
            FromYear = 2000,
            ToYear = 2020,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1,
            UpdatedDate = DateTime.Now,
            UpdatedBy = 1
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        // Setup reference validation for deactivation
        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<AssessmentYearRangeCVEntity>(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        // Setup GetQueryable to return only the entity being updated (no other overlapping ranges)
        var existingRanges = new List<AssessmentYearRangeCVEntity> { existingEntity };
        var mockQuery = existingRanges.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<AssessmentYearRangeCVEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateAssessmentYearRangeCVDto>(), It.IsAny<AssessmentYearRangeCVEntity>()))
            .Callback((UpdateAssessmentYearRangeCVDto src, AssessmentYearRangeCVEntity dest) =>
            {
                dest.FromYear = src.FromYear;
                dest.ToYear = src.ToYear;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy ?? 0;
                dest.UpdatedDate = DateTime.Now;
            });

        await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<AssessmentYearRangeCVEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(2010, existingEntity.FromYear);
        Assert.Equal(2025, existingEntity.ToYear);
        Assert.False(existingEntity.IsActive);
        Assert.Equal(2, existingEntity.UpdatedBy);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_DoesNotUpdate()
    {
        var updateDto = new UpdateAssessmentYearRangeCVDto
        {
            FromYear = 2010,
            ToYear = 2025,
            IsActive = false,
            UpdatedBy = 2
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssessmentYearRangeCVEntity?)null);

        await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<AssessmentYearRangeCVEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssessmentYearRangeCVEntity?)null);

        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        Assert.False(result);

        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<AssessmentYearRangeCVEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        var idToDelete = 1;

        var existingEntity = new AssessmentYearRangeCVEntity
        {

            Id = idToDelete,
            FromYear = 2000,
            ToYear = 2020,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockReferenceValidator
            .Setup(r => r.ValidateReferencesAsync<AssessmentYearRangeCVEntity>(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<AssessmentYearRangeCVEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        Assert.True(result);

        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<AssessmentYearRangeCVEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_OverlappingRange_ThrowsValidationException()
    {
        var existingRanges = new List<AssessmentYearRangeCVEntity>
        {
            new() { Id = 1, FromYear = 2000, ToYear = 2020 }
        };
        var mockQuery = existingRanges.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var createDto = new CreateAssessmentYearRangeCVDto { FromYear = 2010, ToYear = 2025, IsActive = true };
        _mockMapper.Setup(m => m.Map<AssessmentYearRangeCVEntity>(It.IsAny<CreateAssessmentYearRangeCVDto>()))
            .Returns(new AssessmentYearRangeCVEntity { FromYear = createDto.FromYear, ToYear = createDto.ToYear });

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(createDto, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_NonOverlappingRange_Succeeds()
    {
        var existingRanges = new List<AssessmentYearRangeCVEntity>
        {
            new() { Id = 1, FromYear = 2000, ToYear = 2020 }
        };
        var mockQuery = existingRanges.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var createDto = new CreateAssessmentYearRangeCVDto { FromYear = 2021, ToYear = 2025, IsActive = true, CreatedBy = 1 };
        _mockMapper.Setup(m => m.Map<AssessmentYearRangeCVEntity>(It.IsAny<CreateAssessmentYearRangeCVDto>()))
            .Returns(new AssessmentYearRangeCVEntity { Id = 2, FromYear = createDto.FromYear, ToYear = createDto.ToYear, IsActive = true });

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<AssessmentYearRangeCVEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssessmentYearRangeCVEntity e, CancellationToken _) => e);

        _mockMapper.Setup(m => m.Map<AssessmentYearRangeCVDto>(It.IsAny<AssessmentYearRangeCVEntity>()))
            .Returns(new AssessmentYearRangeCVDto { Id = 2, FromYear = 2021, ToYear = 2025, IsActive = true });

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2021, result.FromYear);
        Assert.Equal(2025, result.ToYear);
    }
}
