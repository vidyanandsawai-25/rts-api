using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Master.RuleOperatorMaster;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Rules;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Comprehensive unit tests for RuleOperatorService
/// Tests business logic, data operations, and service layer functionality
/// </summary>
public class RuleOperatorServiceTests
{
    private readonly Mock<IRepository<RuleOperatorEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly RuleOperatorService _service;

    public RuleOperatorServiceTests()
    {
        _mockRepository = new Mock<IRepository<RuleOperatorEntity, int>>();
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

        _service = new RuleOperatorService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object);
    }

    /// <summary>
    /// Creates a real IMapper using the production RuleOperatorMappingProfile.
    /// This ensures tests validate the actual mapping configuration.
    /// </summary>
    private static IMapper CreateRealMapper()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<RuleOperatorMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        // Assert configuration is valid - catches mapping errors at test time
        mapperConfig.AssertConfigurationIsValid();
        return mapperConfig.CreateMapper();
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        var entities = new List<RuleOperatorEntity>
        {
            new()
            {
                Id = 1,
                Operator = "=",
                OperatorDescription = "Equals",
                IsActive = true
            },
            new()
            {
                Id = 2,
                Operator = ">",
                OperatorDescription = "Greater Than",
                IsActive = true
            },
            new()
            {
                Id = 3,
                Operator = "<",
                OperatorDescription = "Less Than",
                IsActive = true
            }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        // Use real mapper with production profile
        var mapper = CreateRealMapper();

        var service = new RuleOperatorService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var queryParams = new RuleOperatorQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count());
        _mockRepository.Verify(r => r.GetQueryable(), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithActiveFilter_ReturnsOnlyActiveEntities()
    {
        // Arrange
        var entities = new List<RuleOperatorEntity>
        {
            new()
            {
                Id = 1,
                Operator = "=",
                OperatorDescription = "Equals",
                IsActive = true
            },
            new()
            {
                Id = 2,
                Operator = "!=",
                OperatorDescription = "Not Equals",
                IsActive = false
            },
            new()
            {
                Id = 3,
                Operator = ">",
                OperatorDescription = "Greater Than",
                IsActive = true
            }
        };

        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        // Use real mapper with production profile
        var mapper = CreateRealMapper();

        var service = new RuleOperatorService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var queryParams = new RuleOperatorQueryParameters
        {
            PageNumber = 1,
            PageSize = 10,
            IsActive = true
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.True(item.IsActive));
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var entities = new List<RuleOperatorEntity>();
        var mockQuery = entities.BuildMock();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

        // Use real mapper with production profile
        var mapper = CreateRealMapper();

        var service = new RuleOperatorService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            mapper);

        var queryParams = new RuleOperatorQueryParameters
        {
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await service.GetAllAsync(queryParams, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingEntity_ReturnsDto()
    {
        // Arrange
        var entity = new RuleOperatorEntity
        {
            Id = 1,
            Operator = "=",
            OperatorDescription = "Equals",
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockMapper.Setup(m => m.Map<RuleOperatorDto>(It.IsAny<RuleOperatorEntity>()))
            .Returns(new RuleOperatorDto
            {
                Id = 1,
                Operator = "=",
                OperatorDescription = "Equals",
                IsActive = true,
                CreatedDate = entity.CreatedDate
            });

        // Act
        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("=", result.Operator);
        Assert.Equal("Equals", result.OperatorDescription);
        Assert.True(result.IsActive);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleOperatorEntity?)null);

        // Act
        var result = await _service.GetByIdAsync(999, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var createDto = new CreateRuleOperatorDto
        {
            Operator = ">=",
            OperatorDescription = "Greater Than or Equal",
            IsActive = true,
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<RuleOperatorEntity>(It.IsAny<CreateRuleOperatorDto>()))
            .Returns((CreateRuleOperatorDto dto) => new RuleOperatorEntity
            {
                Operator = dto.Operator,
                OperatorDescription = dto.OperatorDescription,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RuleOperatorEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleOperatorEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                e.CreatedDate = DateTime.Now;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<RuleOperatorDto>(It.IsAny<RuleOperatorEntity>()))
            .Returns((RuleOperatorEntity e) => new RuleOperatorDto
            {
                Id = e.Id,
                Operator = e.Operator,
                OperatorDescription = e.OperatorDescription,
                IsActive = e.IsActive,
                CreatedDate = e.CreatedDate
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(">=", result.Operator);
        Assert.Equal("Greater Than or Equal", result.OperatorDescription);
        Assert.True(result.IsActive);

        _mockRepository.Verify(r => r.AddAsync(
            It.Is<RuleOperatorEntity>(e =>
                e.Operator == ">=" &&
                e.OperatorDescription == "Greater Than or Equal" &&
                e.IsActive),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithMinimalData_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateRuleOperatorDto
        {
            Operator = "LIKE",
            OperatorDescription = "Pattern Match",
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<RuleOperatorEntity>(It.IsAny<CreateRuleOperatorDto>()))
            .Returns((CreateRuleOperatorDto dto) => new RuleOperatorEntity
            {
                Operator = dto.Operator,
                OperatorDescription = dto.OperatorDescription,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RuleOperatorEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleOperatorEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<RuleOperatorDto>(It.IsAny<RuleOperatorEntity>()))
            .Returns(new RuleOperatorDto
            {
                Id = 1,
                Operator = "LIKE",
                OperatorDescription = "Pattern Match"
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("LIKE", result.Operator);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InactiveOperator_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateRuleOperatorDto
        {
            Operator = "DEPRECATED",
            OperatorDescription = "Old Operator",
            IsActive = false,
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<RuleOperatorEntity>(It.IsAny<CreateRuleOperatorDto>()))
            .Returns(new RuleOperatorEntity
            {
                Operator = "DEPRECATED",
                OperatorDescription = "Old Operator",
                IsActive = false,
                CreatedBy = 1
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RuleOperatorEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleOperatorEntity e, CancellationToken _) =>
            {
                e.Id = 3;
                e.CreatedDate = DateTime.Now;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<RuleOperatorDto>(It.IsAny<RuleOperatorEntity>()))
            .Returns(new RuleOperatorDto
            {
                Id = 3,
                Operator = "DEPRECATED",
                OperatorDescription = "Old Operator",
                IsActive = false
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_VerifiesMapperCalledTwice()
    {
        // Arrange
        var createDto = new CreateRuleOperatorDto
        {
            Operator = "=",
            OperatorDescription = "Equals",
            IsActive = true,
            CreatedBy = 1
        };

        _mockMapper.Setup(m => m.Map<RuleOperatorEntity>(It.IsAny<CreateRuleOperatorDto>()))
            .Returns(new RuleOperatorEntity());

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<RuleOperatorEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RuleOperatorEntity { Id = 1 });

        _mockMapper.Setup(m => m.Map<RuleOperatorDto>(It.IsAny<RuleOperatorEntity>()))
            .Returns(new RuleOperatorDto());

        // Act
        await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        _mockMapper.Verify(m => m.Map<RuleOperatorEntity>(It.IsAny<CreateRuleOperatorDto>()), Times.Once);
        _mockMapper.Verify(m => m.Map<RuleOperatorDto>(It.IsAny<RuleOperatorEntity>()), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var existingEntity = new RuleOperatorEntity
        {
            Id = 1,
            Operator = "=",
            OperatorDescription = "Equals",
            IsActive = true,
            CreatedDate = DateTime.Now.AddDays(-10)
        };

        var updateDto = new UpdateRuleOperatorDto
        {
            Operator = "==",
            OperatorDescription = "Equals (Updated)",
            IsActive = true,
            UpdatedBy = 1
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockMapper.Setup(m => m.Map(It.IsAny<UpdateRuleOperatorDto>(), It.IsAny<RuleOperatorEntity>()))
            .Callback<UpdateRuleOperatorDto, RuleOperatorEntity>((dto, entity) =>
            {
                entity.Operator = dto.Operator;
                entity.OperatorDescription = dto.OperatorDescription;
                entity.IsActive = dto.IsActive;
                entity.UpdatedBy = dto.UpdatedBy;
            })
            .Returns(existingEntity);

        _mockMapper.Setup(m => m.Map<RuleOperatorDto>(It.IsAny<RuleOperatorEntity>()))
            .Returns(new RuleOperatorDto
            {
                Id = 1,
                Operator = "==",
                OperatorDescription = "Equals (Updated)",
                IsActive = true
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("==", result.Operator);
        Assert.Equal("Equals (Updated)", result.OperatorDescription);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var updateDto = new UpdateRuleOperatorDto
        {
            Operator = "=",
            OperatorDescription = "Equals",
            UpdatedBy = 1
        };

        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleOperatorEntity?)null);

        // Act
        var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DeactivateOperator_UpdatesSuccessfully()
    {
        // Arrange
        var existingEntity = new RuleOperatorEntity
        {
            Id = 1,
            Operator = "OLD",
            OperatorDescription = "Old Operator",
            IsActive = true
        };

        var updateDto = new UpdateRuleOperatorDto
        {
            Operator = "OLD",
            OperatorDescription = "Old Operator (Deprecated)",
            IsActive = false,
            UpdatedBy = 1
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEntity);

        _mockMapper.Setup(m => m.Map(It.IsAny<UpdateRuleOperatorDto>(), It.IsAny<RuleOperatorEntity>()))
            .Callback<UpdateRuleOperatorDto, RuleOperatorEntity>((dto, entity) =>
            {
                entity.Operator = dto.Operator;
                entity.OperatorDescription = dto.OperatorDescription;
                entity.IsActive = dto.IsActive;
            })
            .Returns(existingEntity);

        _mockMapper.Setup(m => m.Map<RuleOperatorDto>(It.IsAny<RuleOperatorEntity>()))
            .Returns(new RuleOperatorDto
            {
                Id = 1,
                Operator = "OLD",
                OperatorDescription = "Old Operator (Deprecated)",
                IsActive = false
            });

        // Act
        var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsActive);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingEntity_DeletesAndSavesSuccessfully()
    {
        // Arrange
        var entity = new RuleOperatorEntity
        {
            Id = 1,
            Operator = "=",
            OperatorDescription = "Equals",
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        _mockRepository.Setup(r => r.DeleteAsync(It.IsAny<RuleOperatorEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteAsync(1, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.Is<RuleOperatorEntity>(e => e.Id == 1), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleOperatorEntity?)null);

        // Act
        var result = await _service.DeleteAsync(999, CancellationToken.None);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RuleOperatorEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Additional Edge Case Tests

    [Fact]
    public async Task CreateAsync_WithAllFields_CreatesSuccessfully()
    {
        // Arrange
        var createDto = new CreateRuleOperatorDto
        {
            Operator = "BETWEEN",
            OperatorDescription = "Between two values",
            IsActive = true,
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<RuleOperatorEntity>(It.IsAny<CreateRuleOperatorDto>()))
            .Returns((CreateRuleOperatorDto dto) => new RuleOperatorEntity
            {
                Operator = dto.Operator,
                OperatorDescription = dto.OperatorDescription,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy
            });

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RuleOperatorEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuleOperatorEntity e, CancellationToken _) =>
            {
                e.Id = 1;
                e.CreatedDate = DateTime.Now;
                return e;
            });

        _mockMapper
            .Setup(m => m.Map<RuleOperatorDto>(It.IsAny<RuleOperatorEntity>()))
            .Returns((RuleOperatorEntity e) => new RuleOperatorDto
            {
                Id = e.Id,
                Operator = e.Operator,
                OperatorDescription = e.OperatorDescription,
                IsActive = e.IsActive
            });

        // Act
        var result = await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("BETWEEN", result.Operator);
        Assert.Equal("Between two values", result.OperatorDescription);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_VerifiesNoExplicitTransactionUsed()
    {
        // Arrange
        var createDto = new CreateRuleOperatorDto
        {
            Operator = "TEST",
            OperatorDescription = "Test Operator",
            IsActive = true,
            CreatedBy = 1
        };

        _mockMapper
            .Setup(m => m.Map<RuleOperatorEntity>(It.IsAny<CreateRuleOperatorDto>()))
            .Returns(new RuleOperatorEntity());

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<RuleOperatorEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RuleOperatorEntity { Id = 1 });

        _mockMapper
            .Setup(m => m.Map<RuleOperatorDto>(It.IsAny<RuleOperatorEntity>()))
            .Returns(new RuleOperatorDto());

        // Act
        await _service.CreateAsync(createDto, CancellationToken.None);

        // Assert
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
