using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleEvidenceCondition;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Services.RetrospectiveTax;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.RetrospectiveTax;

public class RetrospectiveRuleEvidenceConditionServiceTests
{
    private readonly Mock<IRepository<RetrospectiveRuleEvidenceConditionEntity, int>> _mockRepository;
    private readonly Mock<IRepository<EvidenceTypeMasterEntity, int>> _mockEvidenceTypeRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly RetrospectiveRuleEvidenceConditionService _service;

    public RetrospectiveRuleEvidenceConditionServiceTests()
    {
        _mockRepository = new Mock<IRepository<RetrospectiveRuleEvidenceConditionEntity, int>>();
        _mockEvidenceTypeRepository = new Mock<IRepository<EvidenceTypeMasterEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new RetrospectiveRuleEvidenceConditionService(
            _mockRepository.Object,
            _mockEvidenceTypeRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    private static List<EvidenceTypeMasterEntity> BuildEvidenceTypes()
    {
        return new List<EvidenceTypeMasterEntity>
        {
            new() { Id = 3, EvidenceCode = "CD", EvidenceName = "Change Detection", DisplayOrder = 3, IsActive = true },
            new() { Id = 1, EvidenceCode = "OC", EvidenceName = "Occupancy Certificate", DisplayOrder = 1, IsActive = true },
            new() { Id = 2, EvidenceCode = "CC", EvidenceName = "Completion Certificate", DisplayOrder = 2, IsActive = true }
        };
    }

    #region Standard CRUD

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new RetrospectiveRuleEvidenceConditionEntity
        {
            Id = 1,
            RuleId = 10,
            EvidenceTypeId = 1,
            EvidenceState = "AVAILABLE",
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1,
            Rule = new RetrospectiveRuleMasterEntity { Id = 10, RuleCode = "R1", RuleName = "Rule 1" },
            EvidenceType = new EvidenceTypeMasterEntity { Id = 1, EvidenceCode = "OC", EvidenceName = "Occupancy Certificate" }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<RetrospectiveRuleEvidenceConditionDto>(It.IsAny<RetrospectiveRuleEvidenceConditionEntity>()))
            .Returns((RetrospectiveRuleEvidenceConditionEntity e) => new RetrospectiveRuleEvidenceConditionDto
            {
                Id = e.Id,
                RuleId = e.RuleId,
                EvidenceTypeId = e.EvidenceTypeId,
                EvidenceState = e.EvidenceState,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.RuleId);
        Assert.Equal(1, result.EvidenceTypeId);
        Assert.Equal("AVAILABLE", result.EvidenceState);
        Assert.NotNull(entity.Rule);
        Assert.NotNull(entity.EvidenceType);
        Assert.Equal("R1", entity.Rule!.RuleCode);
        Assert.Equal("OC", entity.EvidenceType!.EvidenceCode);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleEvidenceConditionEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<RetrospectiveRuleEvidenceConditionEntity>
        {
            new() { Id = 1, RuleId = 10, EvidenceTypeId = 1, EvidenceState = "AVAILABLE", IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now },
            new() { Id = 2, RuleId = 10, EvidenceTypeId = 2, EvidenceState = "UNAVAILABLE", IsActive = true, CreatedBy = 1, CreatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<RetrospectiveRuleEvidenceConditionEntity, RetrospectiveRuleEvidenceConditionDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new RetrospectiveRuleEvidenceConditionService(
            _mockRepository.Object,
            _mockEvidenceTypeRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new RetrospectiveRuleEvidenceConditionQueryParameters
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
        Assert.Contains(items, x => x.EvidenceTypeId == 1 && x.EvidenceState == "AVAILABLE");
        Assert.Contains(items, x => x.EvidenceTypeId == 2 && x.EvidenceState == "UNAVAILABLE");
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateRetrospectiveRuleEvidenceConditionDto
        {
            RuleId = 10,
            EvidenceTypeId = 1,
            EvidenceState = "AVAILABLE",
            CreatedBy = 1,
            IsActive = true
        };

        _mockMapper
            .Setup(m => m.Map<RetrospectiveRuleEvidenceConditionEntity>(It.IsAny<CreateRetrospectiveRuleEvidenceConditionDto>()))
            .Returns((CreateRetrospectiveRuleEvidenceConditionDto dto) => new RetrospectiveRuleEvidenceConditionEntity
            {
                Id = 1,
                RuleId = dto.RuleId,
                EvidenceTypeId = dto.EvidenceTypeId,
                EvidenceState = dto.EvidenceState,
                IsActive = true,
                CreatedDate = DateTime.Now,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RetrospectiveRuleEvidenceConditionEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleEvidenceConditionEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<RetrospectiveRuleEvidenceConditionDto>(It.IsAny<RetrospectiveRuleEvidenceConditionEntity>()))
            .Returns((RetrospectiveRuleEvidenceConditionEntity e) => new RetrospectiveRuleEvidenceConditionDto
            {
                Id = e.Id,
                RuleId = e.RuleId,
                EvidenceTypeId = e.EvidenceTypeId,
                EvidenceState = e.EvidenceState,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(10, result.RuleId);
        Assert.Equal(1, result.EvidenceTypeId);
        Assert.Equal("AVAILABLE", result.EvidenceState);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectiveRuleEvidenceConditionEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateRetrospectiveRuleEvidenceConditionDto
        {
            RuleId = 10,
            EvidenceTypeId = 1,
            EvidenceState = "UNAVAILABLE",
            IsActive = true,
            UpdatedBy = 2
        };

        var existingEntity = new RetrospectiveRuleEvidenceConditionEntity
        {
            Id = 1,
            RuleId = 10,
            EvidenceTypeId = 1,
            EvidenceState = "AVAILABLE",
            IsActive = true,
            CreatedDate = DateTime.Now,
            CreatedBy = 1
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleEvidenceConditionEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateRetrospectiveRuleEvidenceConditionDto>(), It.IsAny<RetrospectiveRuleEvidenceConditionEntity>()))
            .Callback((UpdateRetrospectiveRuleEvidenceConditionDto src, RetrospectiveRuleEvidenceConditionEntity dest) =>
            {
                dest.RuleId = src.RuleId;
                dest.EvidenceTypeId = src.EvidenceTypeId;
                dest.EvidenceState = src.EvidenceState;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy;
                dest.UpdatedDate = DateTime.Now;
            });

        _mockMapper
            .Setup(m => m.Map<RetrospectiveRuleEvidenceConditionDto>(It.IsAny<RetrospectiveRuleEvidenceConditionEntity>()))
            .Returns((RetrospectiveRuleEvidenceConditionEntity e) => new RetrospectiveRuleEvidenceConditionDto
            {
                Id = e.Id,
                RuleId = e.RuleId,
                EvidenceTypeId = e.EvidenceTypeId,
                EvidenceState = e.EvidenceState,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleEvidenceConditionEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal("UNAVAILABLE", existingEntity.EvidenceState);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateRetrospectiveRuleEvidenceConditionDto
        {
            RuleId = 10,
            EvidenceTypeId = 1,
            EvidenceState = "AVAILABLE",
            IsActive = true,
            UpdatedBy = 2
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleEvidenceConditionEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleEvidenceConditionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        // Arrange
        var idToDelete = 1;
        var existingEntity = new RetrospectiveRuleEvidenceConditionEntity
        {
            Id = 1,
            RuleId = 10,
            EvidenceTypeId = 1,
            EvidenceState = "AVAILABLE",
            IsActive = true
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<RetrospectiveRuleEvidenceConditionEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RetrospectiveRuleEvidenceConditionEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleEvidenceConditionEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RetrospectiveRuleEvidenceConditionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region GetEvidenceStateForRuleAsync

    [Fact]
    public async Task GetEvidenceStateForRuleAsync_ReturnsOneEntryPerEvidenceType_OrderedByDisplayOrder_WithSelectedState()
    {
        // Arrange
        const int ruleId = 10;

        var evidenceTypes = BuildEvidenceTypes(); // Ids 1(OC),2(CC),3(CD) with DisplayOrder 1,2,3 respectively
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(evidenceTypes.BuildMock());

        var conditions = new List<RetrospectiveRuleEvidenceConditionEntity>
        {
            new() { Id = 1, RuleId = ruleId, EvidenceTypeId = 1, EvidenceState = "AVAILABLE", IsActive = true },
            new() { Id = 2, RuleId = ruleId, EvidenceTypeId = 2, EvidenceState = "UNAVAILABLE", IsActive = true }
            // EvidenceTypeId = 3 has no condition row -> should be null
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(conditions.BuildMock());

        // Act
        var result = await _service.GetEvidenceStateForRuleAsync(ruleId, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);

        // Ordered by DisplayOrder: OC(1) -> CC(2) -> CD(3)
        Assert.Equal(1, result[0].EvidenceTypeId);
        Assert.Equal(2, result[1].EvidenceTypeId);
        Assert.Equal(3, result[2].EvidenceTypeId);

        Assert.Equal("AVAILABLE", result[0].SelectedState);
        Assert.Equal("UNAVAILABLE", result[1].SelectedState);
        Assert.Null(result[2].SelectedState);
    }

    [Fact]
    public async Task GetEvidenceStateForRuleAsync_IgnoresInactiveConditionRows()
    {
        // Arrange
        const int ruleId = 10;

        var evidenceTypes = BuildEvidenceTypes();
        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(evidenceTypes.BuildMock());

        var conditions = new List<RetrospectiveRuleEvidenceConditionEntity>
        {
            new() { Id = 1, RuleId = ruleId, EvidenceTypeId = 1, EvidenceState = "AVAILABLE", IsActive = false },
            new() { Id = 2, RuleId = 999, EvidenceTypeId = 2, EvidenceState = "AVAILABLE", IsActive = true } // different rule
        };
        _mockRepository.Setup(r => r.GetQueryable()).Returns(conditions.BuildMock());

        // Act
        var result = await _service.GetEvidenceStateForRuleAsync(ruleId, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, r => Assert.Null(r.SelectedState));
    }

    #endregion

    #region SetEvidenceStateForRuleAsync

    [Fact]
    public async Task SetEvidenceStateForRuleAsync_SameIdInBothLists_ThrowsValidationException_NoSideEffects()
    {
        // Arrange
        const int ruleId = 10;
        var request = new SetRetrospectiveRuleEvidenceConditionStateDto
        {
            AvailableEvidenceTypeIds = new List<int> { 1 },
            UnavailableEvidenceTypeIds = new List<int> { 1 },
            UpdatedBy = 99
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.SetEvidenceStateForRuleAsync(ruleId, request, CancellationToken.None));

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectiveRuleEvidenceConditionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleEvidenceConditionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetEvidenceStateForRuleAsync_NoExistingConditions_CreatesNewRowsForEachId()
    {
        // Arrange
        const int ruleId = 10;
        var request = new SetRetrospectiveRuleEvidenceConditionStateDto
        {
            AvailableEvidenceTypeIds = new List<int> { 1 },
            UnavailableEvidenceTypeIds = new List<int> { 2 },
            UpdatedBy = 99
        };

        // No existing condition rows for this rule
        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(new List<RetrospectiveRuleEvidenceConditionEntity>().BuildMock());

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RetrospectiveRuleEvidenceConditionEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleEvidenceConditionEntity e, CancellationToken _) => e);

        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(BuildEvidenceTypes().BuildMock());

        // Act
        var result = await _service.SetEvidenceStateForRuleAsync(ruleId, request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        _mockRepository.Verify(r => r.AddAsync(
            It.Is<RetrospectiveRuleEvidenceConditionEntity>(e =>
                e.RuleId == ruleId && e.EvidenceTypeId == 1 && e.EvidenceState == "AVAILABLE" && e.CreatedBy == 99 && e.IsActive),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockRepository.Verify(r => r.AddAsync(
            It.Is<RetrospectiveRuleEvidenceConditionEntity>(e =>
                e.RuleId == ruleId && e.EvidenceTypeId == 2 && e.EvidenceState == "UNAVAILABLE" && e.CreatedBy == 99 && e.IsActive),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectiveRuleEvidenceConditionEntity>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleEvidenceConditionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetEvidenceStateForRuleAsync_ExistingRowChangesState_UpdatesEvidenceStateAndUpdatedBy()
    {
        // Arrange
        const int ruleId = 10;
        var existing = new RetrospectiveRuleEvidenceConditionEntity
        {
            Id = 1,
            RuleId = ruleId,
            EvidenceTypeId = 1,
            EvidenceState = "AVAILABLE",
            IsActive = true
        };

        var request = new SetRetrospectiveRuleEvidenceConditionStateDto
        {
            AvailableEvidenceTypeIds = new List<int>(),
            UnavailableEvidenceTypeIds = new List<int> { 1 },
            UpdatedBy = 42
        };

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(new List<RetrospectiveRuleEvidenceConditionEntity> { existing }.BuildMock());

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleEvidenceConditionEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(BuildEvidenceTypes().BuildMock());

        // Act
        await _service.SetEvidenceStateForRuleAsync(ruleId, request, CancellationToken.None);

        // Assert
        Assert.Equal("UNAVAILABLE", existing.EvidenceState);
        Assert.Equal(42, existing.UpdatedBy);
        Assert.True(existing.IsActive);

        _mockRepository.Verify(r => r.UpdateAsync(
            It.Is<RetrospectiveRuleEvidenceConditionEntity>(e => e.Id == 1 && e.EvidenceState == "UNAVAILABLE" && e.UpdatedBy == 42),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectiveRuleEvidenceConditionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetEvidenceStateForRuleAsync_ExistingRowNotInEitherList_IsDeactivated()
    {
        // Arrange
        const int ruleId = 10;
        var existing = new RetrospectiveRuleEvidenceConditionEntity
        {
            Id = 1,
            RuleId = ruleId,
            EvidenceTypeId = 3,
            EvidenceState = "AVAILABLE",
            IsActive = true
        };

        var request = new SetRetrospectiveRuleEvidenceConditionStateDto
        {
            AvailableEvidenceTypeIds = new List<int> { 1 },
            UnavailableEvidenceTypeIds = new List<int> { 2 },
            UpdatedBy = 7
        };

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(new List<RetrospectiveRuleEvidenceConditionEntity> { existing }.BuildMock());

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RetrospectiveRuleEvidenceConditionEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleEvidenceConditionEntity e, CancellationToken _) => e);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleEvidenceConditionEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(BuildEvidenceTypes().BuildMock());

        // Act
        await _service.SetEvidenceStateForRuleAsync(ruleId, request, CancellationToken.None);

        // Assert
        Assert.False(existing.IsActive);
        Assert.Equal(7, existing.UpdatedBy);

        _mockRepository.Verify(r => r.UpdateAsync(
            It.Is<RetrospectiveRuleEvidenceConditionEntity>(e => e.Id == 1 && !e.IsActive && e.UpdatedBy == 7),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetEvidenceStateForRuleAsync_ExistingRowAlreadyCorrectState_DoesNotUpdateThatRow()
    {
        // Arrange
        const int ruleId = 10;
        var existing = new RetrospectiveRuleEvidenceConditionEntity
        {
            Id = 1,
            RuleId = ruleId,
            EvidenceTypeId = 1,
            EvidenceState = "AVAILABLE",
            IsActive = true
        };

        var request = new SetRetrospectiveRuleEvidenceConditionStateDto
        {
            AvailableEvidenceTypeIds = new List<int> { 1 },
            UnavailableEvidenceTypeIds = new List<int>(),
            UpdatedBy = 99
        };

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(new List<RetrospectiveRuleEvidenceConditionEntity> { existing }.BuildMock());

        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(BuildEvidenceTypes().BuildMock());

        // Act
        var result = await _service.SetEvidenceStateForRuleAsync(ruleId, request, CancellationToken.None);

        // Assert: no Add/Update calls at all — the only existing row already matches the desired state
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleEvidenceConditionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectiveRuleEvidenceConditionEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal("AVAILABLE", existing.EvidenceState);
        Assert.True(existing.IsActive);

        var oc = result.Single(r => r.EvidenceTypeId == 1);
        Assert.Equal("AVAILABLE", oc.SelectedState);
    }

    [Fact]
    public async Task SetEvidenceStateForRuleAsync_HappyPath_CallsSaveChangesOnce()
    {
        // Arrange
        const int ruleId = 10;
        var request = new SetRetrospectiveRuleEvidenceConditionStateDto
        {
            AvailableEvidenceTypeIds = new List<int> { 1 },
            UnavailableEvidenceTypeIds = new List<int> { 2 },
            UpdatedBy = 5
        };

        _mockRepository.Setup(r => r.GetQueryable())
            .Returns(new List<RetrospectiveRuleEvidenceConditionEntity>().BuildMock());

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RetrospectiveRuleEvidenceConditionEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleEvidenceConditionEntity e, CancellationToken _) => e);

        _mockEvidenceTypeRepository.Setup(r => r.GetQueryable()).Returns(BuildEvidenceTypes().BuildMock());

        // Act
        await _service.SetEvidenceStateForRuleAsync(ruleId, request, CancellationToken.None);

        // Assert
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
