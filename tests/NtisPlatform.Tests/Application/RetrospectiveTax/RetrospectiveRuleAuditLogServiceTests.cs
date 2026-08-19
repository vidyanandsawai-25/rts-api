using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveRuleAuditLog;
using NtisPlatform.Application.Services.RetrospectiveTax;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.RetrospectiveTax;

public class RetrospectiveRuleAuditLogServiceTests
{
    private readonly Mock<IRepository<RetrospectiveRuleAuditLogEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly RetrospectiveRuleAuditLogService _service;

    public RetrospectiveRuleAuditLogServiceTests()
    {
        _mockRepository = new Mock<IRepository<RetrospectiveRuleAuditLogEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new RetrospectiveRuleAuditLogService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var entity = new RetrospectiveRuleAuditLogEntity
        {
            Id = 1,
            RuleId = 5,
            ActionType = "PUBLISH",
            OldValue = "Draft",
            NewValue = "Active",
            Remarks = "Approved",
            CreatedBy = 1,
            CreatedDate = DateTime.Now,
            Rule = new RetrospectiveRuleMasterEntity { Id = 5, RuleCode = "R1", RuleName = "Rule 1" }
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<RetrospectiveRuleAuditLogDto>(It.IsAny<RetrospectiveRuleAuditLogEntity>()))
            .Returns((RetrospectiveRuleAuditLogEntity e) => new RetrospectiveRuleAuditLogDto
            {
                Id = e.Id,
                RuleId = e.RuleId,
                ActionType = e.ActionType,
                OldValue = e.OldValue,
                NewValue = e.NewValue,
                Remarks = e.Remarks,
                CreatedDate = e.CreatedDate
            });

        var result = await _service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(5, result.RuleId);
        Assert.Equal("PUBLISH", result.ActionType);
        Assert.Equal("Draft", result.OldValue);
        Assert.Equal("Active", result.NewValue);
        Assert.NotNull(entity.Rule);
        Assert.Equal("R1", entity.Rule!.RuleCode);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleAuditLogEntity?)null);

        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        var entities = new List<RetrospectiveRuleAuditLogEntity>
        {
            new() { Id = 1, RuleId = 5, ActionType = "CREATE", CreatedDate = DateTime.Now },
            new() { Id = 2, RuleId = 5, ActionType = "PUBLISH", CreatedDate = DateTime.Now }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<RetrospectiveRuleAuditLogEntity, RetrospectiveRuleAuditLogDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new RetrospectiveRuleAuditLogService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new RetrospectiveRuleAuditLogQueryParameters
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
        Assert.Contains(items, x => x.ActionType == "CREATE");
        Assert.Contains(items, x => x.ActionType == "PUBLISH");
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        var createDto = new CreateRetrospectiveRuleAuditLogDto
        {
            RuleId = 5,
            ActionType = "CREATE",
            OldValue = "Draft",
            NewValue = "Active",
            Remarks = "Initial creation",
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<RetrospectiveRuleAuditLogEntity>(It.IsAny<CreateRetrospectiveRuleAuditLogDto>()))
            .Returns((CreateRetrospectiveRuleAuditLogDto dto) => new RetrospectiveRuleAuditLogEntity
            {
                Id = 1,
                RuleId = dto.RuleId,
                ActionType = dto.ActionType,
                OldValue = dto.OldValue,
                NewValue = dto.NewValue,
                Remarks = dto.Remarks,
                CreatedBy = dto.CreatedBy,
                CreatedDate = DateTime.Now
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RetrospectiveRuleAuditLogEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleAuditLogEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<RetrospectiveRuleAuditLogDto>(It.IsAny<RetrospectiveRuleAuditLogEntity>()))
            .Returns((RetrospectiveRuleAuditLogEntity e) => new RetrospectiveRuleAuditLogDto
            {
                Id = e.Id,
                RuleId = e.RuleId,
                ActionType = e.ActionType
            });

        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(5, result.RuleId);
        Assert.Equal("CREATE", result.ActionType);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<RetrospectiveRuleAuditLogEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        var updateDto = new UpdateRetrospectiveRuleAuditLogDto
        {
            RuleId = 5,
            ActionType = "UPDATE",
            OldValue = "Draft",
            NewValue = "Active",
            Remarks = "Corrected",
            IsActive = true,
            UpdatedBy = 9
        };

        var existingEntity = new RetrospectiveRuleAuditLogEntity
        {
            Id = 1,
            RuleId = 5,
            ActionType = "CREATE",
            CreatedDate = DateTime.Now
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleAuditLogEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateRetrospectiveRuleAuditLogDto>(), It.IsAny<RetrospectiveRuleAuditLogEntity>()))
            .Callback((UpdateRetrospectiveRuleAuditLogDto src, RetrospectiveRuleAuditLogEntity dest) =>
            {
                dest.RuleId = src.RuleId;
                dest.ActionType = src.ActionType;
                dest.OldValue = src.OldValue;
                dest.NewValue = src.NewValue;
                dest.Remarks = src.Remarks;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy;
                dest.UpdatedDate = DateTime.Now;
            });

        _mockMapper
            .Setup(m => m.Map<RetrospectiveRuleAuditLogDto>(It.IsAny<RetrospectiveRuleAuditLogEntity>()))
            .Returns((RetrospectiveRuleAuditLogEntity e) => new RetrospectiveRuleAuditLogDto
            {
                Id = e.Id,
                ActionType = e.ActionType,
                Remarks = e.Remarks,
                IsActive = e.IsActive,
                UpdatedDate = e.UpdatedDate
            });

        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        Assert.NotNull(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleAuditLogEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal("UPDATE", existingEntity.ActionType);
        Assert.Equal("Corrected", existingEntity.Remarks);
        Assert.Equal(5, existingEntity.RuleId);
        Assert.Equal("Draft", existingEntity.OldValue);
        Assert.Equal("Active", existingEntity.NewValue);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        var updateDto = new UpdateRetrospectiveRuleAuditLogDto { RuleId = 5, ActionType = "UPDATE" };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleAuditLogEntity?)null);

        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveRuleAuditLogEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        var idToDelete = 1;
        var existingEntity = new RetrospectiveRuleAuditLogEntity { Id = 1, RuleId = 5, ActionType = "CREATE" };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<RetrospectiveRuleAuditLogEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RetrospectiveRuleAuditLogEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveRuleAuditLogEntity?)null);

        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RetrospectiveRuleAuditLogEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
