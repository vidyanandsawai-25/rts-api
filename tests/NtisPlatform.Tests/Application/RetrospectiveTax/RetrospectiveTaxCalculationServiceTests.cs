using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.RetrospectiveTax.RetrospectiveTaxCalculation;
using NtisPlatform.Application.Interfaces.RetrospectiveTax;
using NtisPlatform.Application.Services.RetrospectiveTax;
using NtisPlatform.Core.Entities.RetrospectiveTax;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.RetrospectiveTax;

public class RetrospectiveTaxCalculationServiceTests
{
    private readonly Mock<IRepository<RetrospectiveTaxCalculationEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly RetrospectiveTaxCalculationService _service;

    public RetrospectiveTaxCalculationServiceTests()
    {
        _mockRepository = new Mock<IRepository<RetrospectiveTaxCalculationEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new RetrospectiveTaxCalculationService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new RetrospectiveTaxCalculationEntity
        {
            Id = 1,
            PropertyId = 100,
            CalculationMode = "PROPERTY",
            AssessmentDate = new DateTime(2024, 4, 1),
            BaseTaxAmount = 1000m,
            RetrospectiveTaxAmount = 200m,
            PenaltyAmount = 50m,
            TotalAmount = 1250m,
            CalculationStatus = "Calculated",
            CreatedBy = 1,
            CreatedDate = DateTime.Now
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<RetrospectiveTaxCalculationDto>(It.IsAny<RetrospectiveTaxCalculationEntity>()))
            .Returns((RetrospectiveTaxCalculationEntity e) => new RetrospectiveTaxCalculationDto
            {
                Id = e.Id,
                PropertyId = e.PropertyId,
                CalculationMode = e.CalculationMode,
                AssessmentDate = e.AssessmentDate,
                BaseTaxAmount = e.BaseTaxAmount,
                RetrospectiveTaxAmount = e.RetrospectiveTaxAmount,
                PenaltyAmount = e.PenaltyAmount,
                TotalAmount = e.TotalAmount,
                CalculationStatus = e.CalculationStatus,
                CreatedDate = e.CreatedDate
            });

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(100, result.PropertyId);
        Assert.Equal("PROPERTY", result.CalculationMode);
        Assert.Equal(1250m, result.TotalAmount);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveTaxCalculationEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<RetrospectiveTaxCalculationEntity>
        {
            new()
            {
                Id = 1, PropertyId = 100, CalculationMode = "PROPERTY",
                AssessmentDate = DateTime.Now, CalculationStatus = "Calculated", CreatedDate = DateTime.Now
            },
            new()
            {
                Id = 2, PropertyId = 101, CalculationMode = "FLOOR",
                AssessmentDate = DateTime.Now, CalculationStatus = "Calculated", CreatedDate = DateTime.Now
            }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<RetrospectiveTaxCalculationEntity, RetrospectiveTaxCalculationDto>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        IMapper mapper = mapperConfig.CreateMapper();

        var service = new RetrospectiveTaxCalculationService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var qp = new RetrospectiveTaxCalculationQueryParameters
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
        Assert.Contains(items, x => x.PropertyId == 100);
        Assert.Contains(items, x => x.PropertyId == 101);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateRetrospectiveTaxCalculationDto
        {
            PropertyId = 100,
            CalculationMode = "PROPERTY",
            FloorId = 5,
            AppliedRuleId = 10,
            AppliedTaxPolicyId = 20,
            AssessmentDate = DateTime.Now,
            PolicyStartDate = new DateTime(2020, 4, 1),
            LegalBoundaryDate = new DateTime(2014, 4, 1),
            RuleBoundaryDate = new DateTime(2018, 4, 1),
            ChargeableStartDate = new DateTime(2018, 4, 1),
            ChargeableEndDate = new DateTime(2024, 3, 31),
            BaseTaxAmount = 1000m,
            RetrospectiveTaxAmount = 200m,
            PenaltyAmount = 50m,
            TotalAmount = 1250m,
            AuthorizationStatus = "AUTHORIZED",
            CalculationStatus = "Calculated",
            Remarks = "Initial calculation",
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<RetrospectiveTaxCalculationEntity>(It.IsAny<CreateRetrospectiveTaxCalculationDto>()))
            .Returns((CreateRetrospectiveTaxCalculationDto dto) => new RetrospectiveTaxCalculationEntity
            {
                Id = 1,
                PropertyId = dto.PropertyId,
                CalculationMode = dto.CalculationMode,
                FloorId = dto.FloorId,
                AppliedRuleId = dto.AppliedRuleId,
                AppliedTaxPolicyId = dto.AppliedTaxPolicyId,
                AssessmentDate = dto.AssessmentDate,
                PolicyStartDate = dto.PolicyStartDate,
                LegalBoundaryDate = dto.LegalBoundaryDate,
                RuleBoundaryDate = dto.RuleBoundaryDate,
                ChargeableStartDate = dto.ChargeableStartDate,
                ChargeableEndDate = dto.ChargeableEndDate,
                BaseTaxAmount = dto.BaseTaxAmount,
                RetrospectiveTaxAmount = dto.RetrospectiveTaxAmount,
                PenaltyAmount = dto.PenaltyAmount,
                TotalAmount = dto.TotalAmount,
                AuthorizationStatus = dto.AuthorizationStatus,
                CalculationStatus = dto.CalculationStatus,
                Remarks = dto.Remarks,
                CreatedBy = dto.CreatedBy,
                CreatedDate = DateTime.Now,
                AppliedRule = new RetrospectiveRuleMasterEntity { Id = 10, RuleCode = "R1", RuleName = "Rule 1" },
                AppliedTaxPolicy = new RetrospectiveTaxPolicyEntity { Id = 20, TaxPolicyCode = "TP1", TaxPolicyName = "Policy 1" }
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RetrospectiveTaxCalculationEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveTaxCalculationEntity e, CancellationToken _) => e);

        _mockMapper
            .Setup(m => m.Map<RetrospectiveTaxCalculationDto>(It.IsAny<RetrospectiveTaxCalculationEntity>()))
            .Returns((RetrospectiveTaxCalculationEntity e) => new RetrospectiveTaxCalculationDto
            {
                Id = e.Id,
                PropertyId = e.PropertyId,
                CalculationMode = e.CalculationMode,
                AssessmentDate = e.AssessmentDate,
                BaseTaxAmount = e.BaseTaxAmount,
                RetrospectiveTaxAmount = e.RetrospectiveTaxAmount,
                PenaltyAmount = e.PenaltyAmount,
                TotalAmount = e.TotalAmount,
                CalculationStatus = e.CalculationStatus,
                CreatedDate = e.CreatedDate
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(100, result.PropertyId);
        Assert.Equal("PROPERTY", result.CalculationMode);
        Assert.Equal(1250m, result.TotalAmount);

        _mockRepository.Verify(r => r.AddAsync(
            It.Is<RetrospectiveTaxCalculationEntity>(e =>
                e.AppliedRule != null && e.AppliedRule.RuleCode == "R1" &&
                e.AppliedTaxPolicy != null && e.AppliedTaxPolicy.TaxPolicyCode == "TP1"),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var updateDto = new UpdateRetrospectiveTaxCalculationDto
        {
            PropertyId = 100,
            CalculationMode = "FLOOR",
            FloorId = 6,
            AppliedRuleId = 11,
            AppliedTaxPolicyId = 21,
            AssessmentDate = DateTime.Now,
            PolicyStartDate = new DateTime(2020, 4, 1),
            LegalBoundaryDate = new DateTime(2014, 4, 1),
            RuleBoundaryDate = new DateTime(2018, 4, 1),
            ChargeableStartDate = new DateTime(2018, 4, 1),
            ChargeableEndDate = new DateTime(2024, 3, 31),
            BaseTaxAmount = 1500m,
            RetrospectiveTaxAmount = 300m,
            PenaltyAmount = 75m,
            TotalAmount = 1875m,
            AuthorizationStatus = "UNDETERMINED",
            CalculationStatus = "ManualReview",
            Remarks = "Updated calculation",
            IsActive = true,
            UpdatedBy = 9
        };

        var existingEntity = new RetrospectiveTaxCalculationEntity
        {
            Id = 1,
            PropertyId = 100,
            CalculationMode = "PROPERTY",
            AssessmentDate = DateTime.Now,
            BaseTaxAmount = 1000m,
            RetrospectiveTaxAmount = 200m,
            PenaltyAmount = 50m,
            TotalAmount = 1250m,
            CalculationStatus = "Calculated",
            CreatedBy = 1,
            CreatedDate = DateTime.Now,
            AppliedRule = new RetrospectiveRuleMasterEntity { Id = 10, RuleCode = "R1", RuleName = "Rule 1" },
            AppliedTaxPolicy = new RetrospectiveTaxPolicyEntity { Id = 20, TaxPolicyCode = "TP1", TaxPolicyName = "Policy 1" }
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<RetrospectiveTaxCalculationEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(m => m.Map(It.IsAny<UpdateRetrospectiveTaxCalculationDto>(), It.IsAny<RetrospectiveTaxCalculationEntity>()))
            .Callback((UpdateRetrospectiveTaxCalculationDto src, RetrospectiveTaxCalculationEntity dest) =>
            {
                dest.PropertyId = src.PropertyId;
                dest.CalculationMode = src.CalculationMode;
                dest.FloorId = src.FloorId;
                dest.AppliedRuleId = src.AppliedRuleId;
                dest.AppliedTaxPolicyId = src.AppliedTaxPolicyId;
                dest.AssessmentDate = src.AssessmentDate;
                dest.PolicyStartDate = src.PolicyStartDate;
                dest.LegalBoundaryDate = src.LegalBoundaryDate;
                dest.RuleBoundaryDate = src.RuleBoundaryDate;
                dest.ChargeableStartDate = src.ChargeableStartDate;
                dest.ChargeableEndDate = src.ChargeableEndDate;
                dest.BaseTaxAmount = src.BaseTaxAmount;
                dest.RetrospectiveTaxAmount = src.RetrospectiveTaxAmount;
                dest.PenaltyAmount = src.PenaltyAmount;
                dest.TotalAmount = src.TotalAmount;
                dest.AuthorizationStatus = src.AuthorizationStatus;
                dest.CalculationStatus = src.CalculationStatus;
                dest.Remarks = src.Remarks;
                dest.IsActive = src.IsActive;
                dest.UpdatedBy = src.UpdatedBy;
                dest.UpdatedDate = DateTime.Now;
            });

        _mockMapper
            .Setup(m => m.Map<RetrospectiveTaxCalculationDto>(It.IsAny<RetrospectiveTaxCalculationEntity>()))
            .Returns((RetrospectiveTaxCalculationEntity e) => new RetrospectiveTaxCalculationDto
            {
                Id = e.Id,
                PropertyId = e.PropertyId,
                CalculationMode = e.CalculationMode,
                CalculationStatus = e.CalculationStatus,
                TotalAmount = e.TotalAmount,
                IsActive = e.IsActive,
                UpdatedDate = e.UpdatedDate
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveTaxCalculationEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal("FLOOR", existingEntity.CalculationMode);
        Assert.Equal("ManualReview", existingEntity.CalculationStatus);
        Assert.Equal(1875m, existingEntity.TotalAmount);
        Assert.Equal(6, existingEntity.FloorId);
        Assert.Equal(11, existingEntity.AppliedRuleId);
        Assert.Equal(21, existingEntity.AppliedTaxPolicyId);
        Assert.Equal("UNDETERMINED", existingEntity.AuthorizationStatus);
        Assert.Equal("Updated calculation", existingEntity.Remarks);
        Assert.NotNull(existingEntity.AppliedRule);
        Assert.NotNull(existingEntity.AppliedTaxPolicy);
        Assert.Equal("R1", existingEntity.AppliedRule!.RuleCode);
        Assert.Equal("TP1", existingEntity.AppliedTaxPolicy!.TaxPolicyCode);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateRetrospectiveTaxCalculationDto
        {
            PropertyId = 100,
            CalculationMode = "PROPERTY",
            AssessmentDate = DateTime.Now,
            CalculationStatus = "Calculated"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveTaxCalculationEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RetrospectiveTaxCalculationEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSaves_ReturnsTrue()
    {
        // Arrange
        var idToDelete = 1;
        var existingEntity = new RetrospectiveTaxCalculationEntity
        {
            Id = 1,
            PropertyId = 100,
            CalculationMode = "PROPERTY",
            AssessmentDate = DateTime.Now,
            CalculationStatus = "Calculated"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<RetrospectiveTaxCalculationEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RetrospectiveTaxCalculationEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse_DoesNotSave()
    {
        // Arrange
        var idToDelete = 999;

        _mockRepository
            .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RetrospectiveTaxCalculationEntity?)null);

        // Act
        var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RetrospectiveTaxCalculationEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
