using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application
{
    public class RuleServiceTests
    {
        private readonly Mock<IRepository<RuleEntity, int>> _mockRepository;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly RuleService _service;

        public RuleServiceTests()
        {
            _mockRepository = new Mock<IRepository<RuleEntity, int>>();
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

            _service = new RuleService(
                _mockRepository.Object,
                _mockUnitOfWork.Object,
                _mockMapper.Object);
        }

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsDto()
        {
            // Arrange
            var entity = new RuleEntity
            {
                Id = 1,
                RuleCode = "RULE001",
                Category = "Tax",
                DisplayName = "Tax Calculation Rule",
                Description = "Rule for calculating property tax",
                DataType = "Decimal",
                DefaultValue = "0.05",
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now
            };

            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            _mockMapper.Setup(m => m.Map<RuleDto>(It.IsAny<RuleEntity>()))
                .Returns(new RuleDto
                {
                    Id = 1,
                    RuleCode = "RULE001",
                    Category = "Tax",
                    DisplayName = "Tax Calculation Rule",
                    Description = "Rule for calculating property tax",
                    DataType = "Decimal",
                    DefaultValue = "0.05",
                    IsActive = true,
                    CreatedDate = entity.CreatedDate
                });

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("RULE001", result.RuleCode);
            Assert.Equal("Tax", result.Category);
            Assert.Equal("Tax Calculation Rule", result.DisplayName);
            Assert.Equal("Rule for calculating property tax", result.Description);
            Assert.Equal("Decimal", result.DataType);
            Assert.Equal("0.05", result.DefaultValue);
            Assert.True(result.IsActive);

            _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
            _mockMapper.Verify(m => m.Map<RuleDto>(entity), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((RuleEntity?)null);

            // Act
            var result = await _service.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
            _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
            _mockMapper.Verify(m => m.Map<RuleDto>(It.IsAny<RuleEntity>()), Times.Never);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public async Task GetByIdAsync_InvalidId_ReturnsNull(int invalidId)
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(invalidId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((RuleEntity?)null);

            // Act
            var result = await _service.GetByIdAsync(invalidId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_ReturnsAllEntities()
        {
            // Arrange
            var entities = new List<RuleEntity>
            {
                new()
                {
                    Id = 1,
                    RuleCode = "RULE001",
                    Category = "Tax",
                    DisplayName = "Tax Rule",
                    Description = "Tax calculation",
                    DataType = "Decimal",
                    DefaultValue = "0.05",
                    IsActive = true
                },
                new()
                {
                    Id = 2,
                    RuleCode = "RULE002",
                    Category = "Penalty",
                    DisplayName = "Penalty Rule",
                    Description = "Penalty calculation",
                    DataType = "Percentage",
                    DefaultValue = "2.0",
                    IsActive = true
                },
                new()
                {
                    Id = 3,
                    RuleCode = "RULE003",
                    Category = "Discount",
                    DisplayName = "Discount Rule",
                    Description = "Discount calculation",
                    DataType = "Percentage",
                    DefaultValue = "5.0",
                    IsActive = true
                }
            };

            var mockQuery = entities.BuildMock();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<RuleEntity, RuleDto>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

            mapperConfig.AssertConfigurationIsValid();
            IMapper mapper = mapperConfig.CreateMapper();

            var service = new RuleService(
                _mockRepository.Object,
                _mockUnitOfWork.Object,
                mapper);

            var queryParams = new RuleQueryParameters
            {
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await service.GetAllAsync(queryParams, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);

            var items = result.Items.ToList();
            Assert.Equal(3, items.Count);
            Assert.Contains(items, x => x.RuleCode == "RULE001" && x.Category == "Tax");
            Assert.Contains(items, x => x.RuleCode == "RULE002" && x.Category == "Penalty");
            Assert.Contains(items, x => x.RuleCode == "RULE003" && x.Category == "Discount");
        }

        [Fact]
        public async Task GetAllAsync_WithCategoryFilter_ReturnsFilteredEntities()
        {
            // Arrange
            var entities = new List<RuleEntity>
            {
                new() { Id = 1, RuleCode = "RULE001", Category = "Tax", DisplayName = "Tax Rule 1", DataType = "Decimal", DefaultValue = "0.05", IsActive = true },
                new() { Id = 2, RuleCode = "RULE002", Category = "Penalty", DisplayName = "Penalty Rule", DataType = "Percentage", DefaultValue = "2.0", IsActive = true },
                new() { Id = 3, RuleCode = "RULE003", Category = "Tax", DisplayName = "Tax Rule 2", DataType = "Decimal", DefaultValue = "0.10", IsActive = true }
            };

            var mockQuery = entities.BuildMock();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<RuleEntity, RuleDto>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

            IMapper mapper = mapperConfig.CreateMapper();
            var service = new RuleService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

            var queryParams = new RuleQueryParameters
            {
                PageNumber = 1,
                PageSize = 10,
                Category = "Tax"
            };

            // Act
            var result = await service.GetAllAsync(queryParams, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.TotalCount >= 1);
            Assert.All(result.Items, item =>
                Assert.Contains("Tax", item.Category, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task GetAllAsync_WithRuleCodeFilter_ReturnsFilteredEntities()
        {
            // Arrange
            var entities = new List<RuleEntity>
            {
                new() { Id = 1, RuleCode = "RULE001", Category = "Tax", DisplayName = "Tax Rule", DataType = "Decimal", IsActive = true },
                new() { Id = 2, RuleCode = "RULE002", Category = "Penalty", DisplayName = "Penalty Rule", DataType = "Percentage", IsActive = true },
                new() { Id = 3, RuleCode = "RULE003", Category = "Discount", DisplayName = "Discount Rule", DataType = "Percentage", IsActive = true }
            };

            var mockQuery = entities.BuildMock();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<RuleEntity, RuleDto>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

            IMapper mapper = mapperConfig.CreateMapper();
            var service = new RuleService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

            var queryParams = new RuleQueryParameters
            {
                PageNumber = 1,
                PageSize = 10,
                RuleCode = "RULE001"
            };

            // Act
            var result = await service.GetAllAsync(queryParams, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.TotalCount >= 1);
            Assert.All(result.Items, item =>
                Assert.Contains("RULE001", item.RuleCode, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
        {
            // Arrange
            var entities = new List<RuleEntity>();
            var mockQuery = entities.BuildMock();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<RuleEntity, RuleDto>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

            IMapper mapper = mapperConfig.CreateMapper();
            var service = new RuleService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

            var queryParams = new RuleQueryParameters
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

        [Fact]
        public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var entities = Enumerable.Range(1, 25)
                .Select(i => new RuleEntity
                {
                    Id = i,
                    RuleCode = $"RULE{i:000}",
                    Category = $"Category {i % 3}",
                    DisplayName = $"Rule {i}",
                    Description = $"Description {i}",
                    DataType = "String",
                    DefaultValue = $"Value{i}",
                    IsActive = true
                })
                .ToList();

            var mockQuery = entities.BuildMock();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<RuleEntity, RuleDto>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

            IMapper mapper = mapperConfig.CreateMapper();
            var service = new RuleService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

            var queryParams = new RuleQueryParameters
            {
                PageNumber = 2,
                PageSize = 10
            };

            // Act
            var result = await service.GetAllAsync(queryParams, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(25, result.TotalCount);
            Assert.Equal(10, result.Items.Count());
            Assert.Equal(2, result.PageNumber);
        }

        [Fact]
        public async Task GetAllAsync_WithSearchTerm_ReturnsMatchingEntities()
        {
            // Arrange
            var entities = new List<RuleEntity>
    {
        new() { Id = 1, RuleCode = "RULE001", Category = "Tax", DisplayName = "Tax Calculation", DataType = "Decimal", IsActive = true },
        new() { Id = 2, RuleCode = "RULE002", Category = "Penalty", DisplayName = "Penalty Rate", DataType = "Percentage", IsActive = true },
        new() { Id = 3, RuleCode = "RULE003", Category = "Discount", DisplayName = "Discount Rate", DataType = "Percentage", IsActive = true }
    };

            var mockQuery = entities.BuildMock();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<RuleEntity, RuleDto>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

            IMapper mapper = mapperConfig.CreateMapper();
            var service = new RuleService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

            var queryParams = new RuleQueryParameters
            {
                PageNumber = 1,
                PageSize = 10,
                SearchTerm = "Penalty"
            };

            // Act
            var result = await service.GetAllAsync(queryParams, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);

            var items = result.Items.ToList();
            Assert.Single(items);

            Assert.All(items, item =>
                Assert.True(
                    (!string.IsNullOrWhiteSpace(item.RuleCode) &&
                     item.RuleCode.Contains("Penalty", StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(item.Category) &&
                     item.Category.Contains("Penalty", StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(item.DisplayName) &&
                     item.DisplayName.Contains("Penalty", StringComparison.OrdinalIgnoreCase)),
                    $"Returned item does not match search term 'Penalty'. RuleCode='{item.RuleCode}', Category='{item.Category}', DisplayName='{item.DisplayName}'"));
        }

        [Fact]
        public async Task GetAllAsync_OnlyActiveRules_ReturnsActiveEntities()
        {
            // Arrange
            var entities = new List<RuleEntity>
            {
                new() { Id = 1, RuleCode = "RULE001", Category = "Tax", DisplayName = "Active Tax Rule", DataType = "Decimal", IsActive = true },
                new() { Id = 2, RuleCode = "RULE002", Category = "Penalty", DisplayName = "Inactive Penalty Rule", DataType = "Percentage", IsActive = false },
                new() { Id = 3, RuleCode = "RULE003", Category = "Discount", DisplayName = "Active Discount Rule", DataType = "Percentage", IsActive = true }
            };

            var mockQuery = entities.BuildMock();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<RuleEntity, RuleDto>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

            IMapper mapper = mapperConfig.CreateMapper();
            var service = new RuleService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

            var queryParams = new RuleQueryParameters
            {
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await service.GetAllAsync(queryParams, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
        {
            // Arrange
            var createDto = new CreateRuleDto
            {
                RuleCode = "RULE001",
                Category = "Tax",
                DisplayName = "New Tax Rule",
                Description = "Description for new tax rule",
                DataType = "Decimal",
                DefaultValue = "0.05",
                IsActive = true,
                CreatedBy = 1
            };

            _mockMapper
                .Setup(m => m.Map<RuleEntity>(It.IsAny<CreateRuleDto>()))
                .Returns((CreateRuleDto dto) => new RuleEntity
                {
                    RuleCode = dto.RuleCode,
                    Category = dto.Category,
                    DisplayName = dto.DisplayName,
                    Description = dto.Description,
                    DataType = dto.DataType,
                    DefaultValue = dto.DefaultValue,
                    IsActive = dto.IsActive,
                    CreatedBy = dto.CreatedBy
                });

            _mockRepository
                .Setup(r => r.AddAsync(It.IsAny<RuleEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((RuleEntity e, CancellationToken _) =>
                {
                    e.Id = 1;
                    e.CreatedDate = DateTime.Now;
                    return e;
                });

            _mockMapper
                .Setup(m => m.Map<RuleDto>(It.IsAny<RuleEntity>()))
                .Returns((RuleEntity e) => new RuleDto
                {
                    Id = e.Id,
                    RuleCode = e.RuleCode,
                    Category = e.Category,
                    DisplayName = e.DisplayName,
                    Description = e.Description,
                    DataType = e.DataType,
                    DefaultValue = e.DefaultValue,
                    IsActive = e.IsActive,
                    CreatedDate = e.CreatedDate
                });

            // Act
            var result = await _service.CreateAsync(createDto, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("RULE001", result.RuleCode);
            Assert.Equal("Tax", result.Category);
            Assert.Equal("New Tax Rule", result.DisplayName);
            Assert.Equal("Description for new tax rule", result.Description);
            Assert.Equal("Decimal", result.DataType);
            Assert.Equal("0.05", result.DefaultValue);
            Assert.True(result.IsActive);

            _mockRepository.Verify(r => r.AddAsync(
                It.Is<RuleEntity>(e => e.Category == "Tax" && e.DisplayName == "New Tax Rule" && e.IsActive),
                It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WithMinimalData_ReturnsCreatedDto()
        {
            // Arrange
            var createDto = new CreateRuleDto
            {
                RuleCode = "RULE002",
                Category = "Simple",
                DisplayName = "Simple Rule",
                DataType = "String",
                IsActive = true,
                CreatedBy = 1
            };

            _mockMapper
                .Setup(m => m.Map<RuleEntity>(It.IsAny<CreateRuleDto>()))
                .Returns(new RuleEntity
                {
                    RuleCode = "RULE002",
                    Category = "Simple",
                    DisplayName = "Simple Rule",
                    DataType = "String",
                    IsActive = true,
                    CreatedBy = 1
                });

            _mockRepository
                .Setup(r => r.AddAsync(It.IsAny<RuleEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((RuleEntity e, CancellationToken _) =>
                {
                    e.Id = 2;
                    e.CreatedDate = DateTime.Now;
                    return e;
                });

            _mockMapper
                .Setup(m => m.Map<RuleDto>(It.IsAny<RuleEntity>()))
                .Returns(new RuleDto
                {
                    Id = 2,
                    RuleCode = "RULE002",
                    Category = "Simple",
                    DisplayName = "Simple Rule",
                    DataType = "String",
                    IsActive = true
                });

            // Act
            var result = await _service.CreateAsync(createDto, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Id);
            Assert.Equal("Simple", result.Category);
            Assert.Equal("Simple Rule", result.DisplayName);
            Assert.True(result.IsActive);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_InactiveRule_CreatesSuccessfully()
        {
            // Arrange
            var createDto = new CreateRuleDto
            {
                RuleCode = "RULE003",
                Category = "Inactive",
                DisplayName = "Inactive Rule",
                DataType = "String",
                IsActive = false,
                CreatedBy = 1
            };

            _mockMapper
                .Setup(m => m.Map<RuleEntity>(It.IsAny<CreateRuleDto>()))
                .Returns(new RuleEntity
                {
                    RuleCode = "RULE003",
                    Category = "Inactive",
                    DisplayName = "Inactive Rule",
                    DataType = "String",
                    IsActive = false,
                    CreatedBy = 1
                });

            _mockRepository
                .Setup(r => r.AddAsync(It.IsAny<RuleEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((RuleEntity e, CancellationToken _) =>
                {
                    e.Id = 3;
                    e.CreatedDate = DateTime.Now;
                    return e;
                });

            _mockMapper
                .Setup(m => m.Map<RuleDto>(It.IsAny<RuleEntity>()))
                .Returns(new RuleDto
                {
                    Id = 3,
                    RuleCode = "RULE003",
                    Category = "Inactive",
                    DisplayName = "Inactive Rule",
                    DataType = "String",
                    IsActive = false
                });

            // Act
            var result = await _service.CreateAsync(createDto, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsActive);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
        {
            // Arrange
            var updateDto = new UpdateRuleDto
            {
                RuleCode = "RULE001",
                Category = "Tax",
                DisplayName = "Updated Tax Rule",
                Description = "Updated description",
                DataType = "Decimal",
                DefaultValue = "0.10",
                IsActive = true,
                UpdatedBy = 1
            };

            var existingEntity = new RuleEntity
            {
                Id = 1,
                RuleCode = "RULE001",
                Category = "Tax",
                DisplayName = "Old Tax Rule",
                Description = "Old description",
                DataType = "Decimal",
                DefaultValue = "0.05",
                IsActive = true,
                CreatedBy = 1,
                CreatedDate = DateTime.Now.AddDays(-1)
            };

            _mockRepository
                .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEntity);

            _mockRepository
                .Setup(r => r.UpdateAsync(It.IsAny<RuleEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockMapper
                .Setup(m => m.Map(It.IsAny<UpdateRuleDto>(), It.IsAny<RuleEntity>()))
                .Callback((UpdateRuleDto src, RuleEntity dest) =>
                {
                    dest.RuleCode = src.RuleCode;
                    dest.Category = src.Category;
                    dest.DisplayName = src.DisplayName;
                    dest.Description = src.Description;
                    dest.DataType = src.DataType;
                    dest.DefaultValue = src.DefaultValue;
                    dest.IsActive = src.IsActive;
                    dest.UpdatedBy = src.UpdatedBy;
                    dest.UpdatedDate = DateTime.Now;
                });

            _mockMapper
                .Setup(m => m.Map<RuleDto>(It.IsAny<RuleEntity>()))
                .Returns((RuleEntity e) => new RuleDto
                {
                    Id = e.Id,
                    RuleCode = e.RuleCode,
                    Category = e.Category,
                    DisplayName = e.DisplayName,
                    Description = e.Description,
                    DataType = e.DataType,
                    DefaultValue = e.DefaultValue,
                    IsActive = e.IsActive,
                    UpdatedDate = e.UpdatedDate
                });

            // Act
            var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Tax Rule", result.DisplayName);
            Assert.Equal("Updated description", result.Description);
            Assert.Equal("0.10", result.DefaultValue);
            Assert.True(result.IsActive);

            _mockRepository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
            _mockRepository.Verify(r => r.UpdateAsync(existingEntity, It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingEntity_ReturnsNull()
        {
            // Arrange
            var updateDto = new UpdateRuleDto
            {
                RuleCode = "TEST001",
                Category = "Test",
                DisplayName = "Test Rule",
                DataType = "String",
                IsActive = true
            };

            _mockRepository
                .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((RuleEntity?)null);

            // Act
            var result = await _service.UpdateAsync(999, updateDto, CancellationToken.None);

            // Assert
            Assert.Null(result);
            _mockRepository.Verify(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RuleEntity>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_DeactivateRule_UpdatesSuccessfully()
        {
            // Arrange
            var updateDto = new UpdateRuleDto
            {
                RuleCode = "RULE001",
                Category = "Tax",
                DisplayName = "Tax Rule",
                DataType = "Decimal",
                IsActive = false,
                UpdatedBy = 1
            };

            var existingEntity = new RuleEntity
            {
                Id = 1,
                RuleCode = "RULE001",
                Category = "Tax",
                DisplayName = "Tax Rule",
                DataType = "Decimal",
                IsActive = true
            };

            _mockRepository
                .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEntity);

            _mockRepository
                .Setup(r => r.UpdateAsync(It.IsAny<RuleEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockMapper
                .Setup(m => m.Map(It.IsAny<UpdateRuleDto>(), It.IsAny<RuleEntity>()))
                .Callback((UpdateRuleDto src, RuleEntity dest) =>
                {
                    dest.IsActive = src.IsActive;
                    dest.UpdatedBy = src.UpdatedBy;
                });

            _mockMapper
                .Setup(m => m.Map<RuleDto>(It.IsAny<RuleEntity>()))
                .Returns((RuleEntity e) => new RuleDto
                {
                    Id = e.Id,
                    IsActive = e.IsActive
                });

            // Act
            var result = await _service.UpdateAsync(1, updateDto, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsActive);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_ExistingEntity_DeletesAndSavesSuccessfully()
        {
            // Arrange
            var idToDelete = 1;

            var existingEntity = new RuleEntity
            {
                Id = idToDelete,
                RuleCode = "RULE001",
                Category = "Tax",
                DisplayName = "Tax Rule",
                IsActive = true
            };

            _mockRepository
                .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEntity);

            _mockRepository
                .Setup(r => r.DeleteAsync(It.IsAny<RuleEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
            _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<RuleEntity>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingEntity_ReturnsFalse()
        {
            // Arrange
            var idToDelete = 999;

            _mockRepository
                .Setup(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()))
                .ReturnsAsync((RuleEntity?)null);

            // Act
            var result = await _service.DeleteAsync(idToDelete, CancellationToken.None);

            // Assert
            Assert.False(result);
            _mockRepository.Verify(r => r.GetByIdAsync(idToDelete, It.IsAny<CancellationToken>()), Times.Once);
            _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Transaction Tests

        [Fact]
        public async Task CreateAsync_VerifiesNoExplicitTransactionUsed()
        {
            // Arrange
            var createDto = new CreateRuleDto
            {
                RuleCode = "TEST001",
                Category = "Test",
                DisplayName = "Test Rule",
                DataType = "String",
                IsActive = true,
                CreatedBy = 1
            };

            _mockMapper
                .Setup(m => m.Map<RuleEntity>(It.IsAny<CreateRuleDto>()))
                .Returns(new RuleEntity());

            _mockRepository
                .Setup(r => r.AddAsync(It.IsAny<RuleEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RuleEntity { Id = 1 });

            _mockMapper
                .Setup(m => m.Map<RuleDto>(It.IsAny<RuleEntity>()))
                .Returns(new RuleDto());

            // Act
            await _service.CreateAsync(createDto, CancellationToken.None);

            // Assert
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_VerifiesNoExplicitTransactionUsed()
        {
            // Arrange
            var existingEntity = new RuleEntity
            {
                Id = 1,
                RuleCode = "RULE001",
                Category = "Tax",
                DisplayName = "Tax Rule",
                IsActive = true
            };

            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingEntity);

            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<RuleEntity>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockMapper.Setup(m => m.Map<RuleDto>(It.IsAny<RuleEntity>()))
                .Returns(new RuleDto());

            var updateDto = new UpdateRuleDto
            {
                RuleCode = "RULE001",
                Category = "Tax",
                DisplayName = "Updated Rule",
                DataType = "Decimal",
                IsActive = true
            };

            // Act
            await _service.UpdateAsync(1, updateDto, CancellationToken.None);

            // Assert
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region Mapper Tests

        [Fact]
        public async Task GetByIdAsync_VerifiesMapperCalledOnce()
        {
            // Arrange
            var entity = new RuleEntity
            {
                Id = 1,
                RuleCode = "RULE001",
                Category = "Tax",
                IsActive = true
            };

            _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(entity);

            _mockMapper.Setup(m => m.Map<RuleDto>(It.IsAny<RuleEntity>()))
                .Returns(new RuleDto());

            // Act
            await _service.GetByIdAsync(1);

            // Assert
            _mockMapper.Verify(m => m.Map<RuleDto>(entity), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_VerifiesMapperCalledTwice()
        {
            // Arrange
            var createDto = new CreateRuleDto
            {
                RuleCode = "RULE001",
                Category = "Tax",
                DisplayName = "Tax Rule",
                DataType = "Decimal",
                IsActive = true,
                CreatedBy = 1
            };

            _mockMapper.Setup(m => m.Map<RuleEntity>(It.IsAny<CreateRuleDto>()))
                .Returns(new RuleEntity());

            _mockRepository.Setup(r => r.AddAsync(It.IsAny<RuleEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RuleEntity { Id = 1 });

            _mockMapper.Setup(m => m.Map<RuleDto>(It.IsAny<RuleEntity>()))
                .Returns(new RuleDto());

            // Act
            await _service.CreateAsync(createDto, CancellationToken.None);

            // Assert
            _mockMapper.Verify(m => m.Map<RuleEntity>(createDto), Times.Once);
            _mockMapper.Verify(m => m.Map<RuleDto>(It.IsAny<RuleEntity>()), Times.Once);
        }

        #endregion

        #region Business Logic Tests

        [Fact]
        public async Task GetAllAsync_WithMultipleFilters_ReturnsFilteredResults()
        {
            // Arrange
            var entities = new List<RuleEntity>
            {
                new() { Id = 1, RuleCode = "RULE001", Category = "Tax", DisplayName = "Tax Rule 1", DataType = "Decimal", IsActive = true },
                new() { Id = 2, RuleCode = "RULE002", Category = "Tax", DisplayName = "Tax Rule 2", DataType = "Percentage", IsActive = true },
                new() { Id = 3, RuleCode = "RULE003", Category = "Penalty", DisplayName = "Penalty Rule", DataType = "Decimal", IsActive = true }
            };

            var mockQuery = entities.BuildMock();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<RuleEntity, RuleDto>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

            IMapper mapper = mapperConfig.CreateMapper();
            var service = new RuleService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

            var queryParams = new RuleQueryParameters
            {
                PageNumber = 1,
                PageSize = 10,
                Category = "Tax"
            };

            // Act
            var result = await service.GetAllAsync(queryParams, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.TotalCount >= 1);
            Assert.All(result.Items, item =>
            {
                Assert.Contains("Tax", item.Category);
            });
        }

        [Fact]
        public async Task CreateAsync_WithAllFields_CreatesSuccessfully()
        {
            // Arrange
            var createDto = new CreateRuleDto
            {
                RuleCode = "RULE001",
                Category = "Tax",
                DisplayName = "Complete Tax Rule",
                Description = "A comprehensive tax calculation rule",
                DataType = "Decimal",
                DefaultValue = "0.075",
                IsActive = true,
                CreatedBy = 1
            };

            _mockMapper
                .Setup(m => m.Map<RuleEntity>(It.IsAny<CreateRuleDto>()))
                .Returns((CreateRuleDto dto) => new RuleEntity
                {
                    RuleCode = dto.RuleCode,
                    Category = dto.Category,
                    DisplayName = dto.DisplayName,
                    Description = dto.Description,
                    DataType = dto.DataType,
                    DefaultValue = dto.DefaultValue,
                    IsActive = dto.IsActive,
                    CreatedBy = dto.CreatedBy
                });

            _mockRepository
                .Setup(r => r.AddAsync(It.IsAny<RuleEntity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((RuleEntity e, CancellationToken _) =>
                {
                    e.Id = 1;
                    e.CreatedDate = DateTime.Now;
                    return e;
                });

            _mockMapper
                .Setup(m => m.Map<RuleDto>(It.IsAny<RuleEntity>()))
                .Returns((RuleEntity e) => new RuleDto
                {
                    Id = e.Id,
                    RuleCode = e.RuleCode,
                    Category = e.Category,
                    DisplayName = e.DisplayName,
                    Description = e.Description,
                    DataType = e.DataType,
                    DefaultValue = e.DefaultValue,
                    IsActive = e.IsActive,
                    CreatedDate = e.CreatedDate
                });

            // Act
            var result = await _service.CreateAsync(createDto, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Complete Tax Rule", result.DisplayName);
            Assert.Equal("A comprehensive tax calculation rule", result.Description);
            Assert.Equal("Decimal", result.DataType);
            Assert.Equal("0.075", result.DefaultValue);
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task GetAllAsync_SortByCategory_ReturnsSortedResults()
        {
            // Arrange
            var entities = new List<RuleEntity>
    {
        new() { Id = 1, RuleCode = "RULE001", Category = "Tax", DisplayName = "Tax Rule", DataType = "Decimal", IsActive = true },
        new() { Id = 2, RuleCode = "RULE002", Category = "Discount", DisplayName = "Discount Rule", DataType = "Percentage", IsActive = true },
        new() { Id = 3, RuleCode = "RULE003", Category = "Penalty", DisplayName = "Penalty Rule", DataType = "Decimal", IsActive = true }
    };

            var mockQuery = entities.BuildMock();
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<RuleEntity, RuleDto>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

            IMapper mapper = mapperConfig.CreateMapper();
            var service = new RuleService(_mockRepository.Object, _mockUnitOfWork.Object, mapper);

            var queryParams = new RuleQueryParameters
            {
                PageNumber = 1,
                PageSize = 10,
                SortBy = "Category",
                SortOrder = "asc"
            };

            // Act
            var result = await service.GetAllAsync(queryParams, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);

            var items = result.Items.ToList();
            Assert.Equal(3, items.Count);

            var categories = items.Select(x => x.Category).ToList();
            var expectedOrder = categories.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();

            Assert.Equal(expectedOrder, categories);
            Assert.Equal(new[] { "Discount", "Penalty", "Tax" }, categories);
        }

        #endregion
    }
}