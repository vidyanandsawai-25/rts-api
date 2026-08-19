using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleDateCondition;
using NtisPlatform.Application.Services.RetrospectiveTax;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.RetrospectiveTax;

public class RetrospectiveRuleDateConditionServiceTests
{
    private readonly Mock<IRepository<RetrospectiveRuleDateConditionEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly RetrospectiveRuleDateConditionService _service;

    public RetrospectiveRuleDateConditionServiceTests()
    {
        _mockRepository = new Mock<IRepository<RetrospectiveRuleDateConditionEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new RetrospectiveRuleDateConditionService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new RetrospectiveRuleDateConditionEntity
        {
            Id = 1,
            RuleId = 10,
            ComparatorCode = "ELECTRICITY_BEFORE_CC",
            LeftEvidenceTypeId = 2,
            RightEvidenceTypeId = 3,
            CompareOperator = "BEFORE",
            CompareDate = new DateTime(2020, 1, 1),
            CompareDateTo = new DateTime(2020, 6, 1),
            CompareYears = 5,
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1,
            UpdatedDate = null,
            UpdatedBy = null,
            Rule = new RetrospectiveRuleMasterEntity { Id = 10, RuleCode = "R1", RuleName = "Rule 1" },
            LeftEvidenceType = new EvidenceTypeMasterEntity { Id = 2, EvidenceCode = "OC", EvidenceName = "Occupancy Certificate" },
            RightEvidenceType = new EvidenceTypeMasterEntity { Id = 3, EvidenceCode = "CC", EvidenceName = "Completion Certificate" }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<RetrospectiveRuleDateConditionDto>(It.IsAny<RetrospectiveRuleDateConditionEntity>()))
            .Returns((RetrospectiveRuleDateConditionEntity e) => new RetrospectiveRuleDateConditionDto
            {
                Id = e.Id,
                RuleId = e.RuleId,
                ComparatorCode = e.ComparatorCode,
                LeftEvidenceTypeId = e.LeftEvidenceTypeId,
                RightEvidenceTypeId = e.RightEvidenceTypeId,
                CompareOperator = e.CompareOperator,
                CompareDate = e.CompareDate,
                CompareDateTo = e.CompareDateTo,
                CompareYears = e.CompareYears,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate,
                UpdatedDate = e.UpdatedDate
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.RuleId);
        Assert.Equal("ELECTRICITY_BEFORE_CC", result.ComparatorCode);
        Assert.True(result.IsActive);
        Assert.Equal(5, result.CompareYears);
        Assert.NotNull(entity.Rule);
        Assert.NotNull(entity.LeftEvidenceType);
        Assert.NotNull(entity.RightEvidenceType);
        Assert.Equal("R1", entity.Rule!.RuleCode);
        Assert.Equal("OC", entity.LeftEvidenceType!.EvidenceCode);
        Assert.Equal("CC", entity.RightEvidenceType!.EvidenceCode);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleDateConditionEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<RetrospectiveRuleDateConditionEntity>
        {
            new() { Id = 1, RuleId = 10, ComparatorCode = "NONE", IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now },
            new() { Id = 2, RuleId = 11, ComparatorCode = "ELECTRICITY_AFTER_CC", IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<RetrospectiveRuleDateConditionEntity, RetrospectiveRuleDateConditionDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new RetrospectiveRuleDateConditionService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new RetrospectiveRuleDateConditionQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            FilterLogic = NtisPlatform.Application.Enums.FilterLogic.And
        };

        // Act
        var result = await service.GetAllAsync(qp, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);

        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, x => x.ComparatorCode == "NONE");
        Assert.Contains(items, x => x.ComparatorCode == "ELECTRICITY_AFTER_CC");
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateRetrospectiveRuleDateConditionDto
        {
            RuleId = 10,
            ComparatorCode = "ELECTRICITY_BEFORE_CUTOFF",
            LeftEvidenceTypeId = 2,
            RightEvidenceTypeId = 3,
            CompareOperator = "BEFORE",
            CompareDate = new DateTime(2020, 1, 1),
            CompareDateTo = new DateTime(2020, 6, 1),
            CompareYears = 5,
            CreatedBy = 1,
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<RetrospectiveRuleDateConditionEntity>(It.IsAny<CreateRetrospectiveRuleDateConditionDto>()))
            .Returns((CreateRetrospectiveRuleDateConditionDto dto) => new RetrospectiveRuleDateConditionEntity
            {
                Id = 1,
                RuleId = dto.RuleId,
                ComparatorCode = dto.ComparatorCode,
                LeftEvidenceTypeId = dto.LeftEvidenceTypeId,
                RightEvidenceTypeId = dto.RightEvidenceTypeId,
                CompareOperator = dto.CompareOperator,
                CompareDate = dto.CompareDate,
                CompareDateTo = dto.CompareDateTo,
                CompareYears = dto.CompareYears,
                IsActive = true,
                CreatedDate = DateTime.Now,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RetrospectiveRuleDateConditionEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleDateConditionEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<RetrospectiveRuleDateConditionDto>(It.IsAny<RetrospectiveRuleDateConditionEntity>()))
            .Returns((RetrospectiveRuleDateConditionEntity e) => new RetrospectiveRuleDateConditionDto
            {
                Id = e.Id,
                RuleId = e.RuleId,
                ComparatorCode = e.ComparatorCode,
                CompareDate = e.CompareDate,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.RuleId);
        Assert.Equal("ELECTRICITY_BEFORE_CUTOFF", result.ComparatorCode);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectiveRuleDateConditionEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateRetrospectiveRuleDateConditionDto
        {
            RuleId = 10,
            ComparatorCode = "OC_OLDER_THAN_ALLOWED_PERIOD",
            LeftEvidenceTypeId = 2,
            RightEvidenceTypeId = 3,
            CompareOperator = "OLDER_THAN_YEARS",
            CompareDate = new DateTime(2021, 1, 1),
            CompareDateTo = new DateTime(2021, 6, 1),
            CompareYears = 5,
            IsActive = true,
            UpdatedBy = 2
        };

        var existingEntity = new RetrospectiveRuleDateConditionEntity
        {
            Id = 1,
            RuleId = 10,
            ComparatorCode = "NONE",
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleDateConditionEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateRetrospectiveRuleDateConditionDto>(), It.IsAny<RetrospectiveRuleDateConditionEntity>()))
            .Callback((UpdateRetrospectiveRuleDateConditionDto src, RetrospectiveRuleDateConditionEntity dest) =>
            {
                dest.RuleId = src.RuleId;
                dest.ComparatorCode = src.ComparatorCode;
                dest.LeftEvidenceTypeId = src.LeftEvidenceTypeId;
                dest.RightEvidenceTypeId = src.RightEvidenceTypeId;
                dest.CompareOperator = src.CompareOperator;
                dest.CompareDate = src.CompareDate;
                dest.CompareDateTo = src.CompareDateTo;
                dest.CompareYears = src.CompareYears;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy;
                dest.UpdatedDate = DateTime.Now;
            });

        _mockMapper
            .Setup(m => m.Map<RetrospectiveRuleDateConditionDto>(It.IsAny<RetrospectiveRuleDateConditionEntity>()))
            .Returns((RetrospectiveRuleDateConditionEntity e) => new RetrospectiveRuleDateConditionDto
            {
                Id = e.Id,
                RuleId = e.RuleId,
                ComparatorCode = e.ComparatorCode,
                CompareYears = e.CompareYears,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleDateConditionEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal("OC_OLDER_THAN_ALLOWED_PERIOD", existingEntity.ComparatorCode);
        Assert.Equal(5, existingEntity.CompareYears);
        Assert.Equal(2, existingEntity.LeftEvidenceTypeId);
        Assert.Equal(3, existingEntity.RightEvidenceTypeId);
        Assert.Equal("OLDER_THAN_YEARS", existingEntity.CompareOperator);
        Assert.Equal(new DateTime(2021, 1, 1), existingEntity.CompareDate);
        Assert.Equal(new DateTime(2021, 6, 1), existingEntity.CompareDateTo);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateRetrospectiveRuleDateConditionDto
        {
            RuleId = 10,
            ComparatorCode = "NONE",
            IsActive = true,
            UpdatedBy = 2
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleDateConditionEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleDateConditionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        // Arrange
        var idToDelete = 1;
        var existingEntity = new RetrospectiveRuleDateConditionEntity
        {
            Id = 1,
            RuleId = 10,
            ComparatorCode = "NONE",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<RetrospectiveRuleDateConditionEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RetrospectiveRuleDateConditionEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleDateConditionEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RetrospectiveRuleDateConditionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
