using AutoMapper;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Rules;
using NtisPlatform.Application.Mappings.Rules;
using NtisPlatform.Application.Services.Rules;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Rules;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NtisPlatform.Tests.Application.Services
{
    public class PropertyRuleApplicationLogServiceTests
    {
        private readonly Mock<IRepository<PropertyRuleApplicationLogEntity, int>> _mockRepository;
        private readonly IMapper _mapper;
        private readonly PropertyRuleApplicationLogService _service;

        public PropertyRuleApplicationLogServiceTests()
        {
            _mockRepository = new Mock<IRepository<PropertyRuleApplicationLogEntity, int>>();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<PropertyRuleApplicationLogMappingProfile>();
            }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            _mapper = mapperConfig.CreateMapper();

            _service = new PropertyRuleApplicationLogService(_mockRepository.Object, _mapper);
        }

        [Fact]
        public async Task GetLogsAsync_FiltersOnlyActiveAndNotDeleted()
        {
            // Arrange
            var logs = new List<PropertyRuleApplicationLogEntity>
            {
                new() { Id = 1, PropertyId = 101, RuleName = "Rule A", IsActive = true, MarkedForDeletion = false },
                new() { Id = 2, PropertyId = 102, RuleName = "Rule B", IsActive = false, MarkedForDeletion = false }, // Inactive
                new() { Id = 3, PropertyId = 103, RuleName = "Rule C", IsActive = true, MarkedForDeletion = true } // Marked for deletion
            };

            var mockQueryable = MockQueryableExtensions.BuildMock(logs);
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

            var queryParams = new PropertyRuleApplicationLogQueryParameters { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _service.GetLogsAsync(queryParams, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(1, result.Items.First().Id);
        }

        [Fact]
        public async Task GetLogsAsync_SearchTermNumeric_MatchesPropertyIdOrDetailsId()
        {
            // Arrange
            var logs = new List<PropertyRuleApplicationLogEntity>
            {
                new() { Id = 1, PropertyId = 101, PropertyDetailsId = 201, RuleName = "Rule A", IsActive = true, MarkedForDeletion = false },
                new() { Id = 2, PropertyId = 102, PropertyDetailsId = 202, RuleName = "Rule B", IsActive = true, MarkedForDeletion = false }
            };

            var mockQueryable = MockQueryableExtensions.BuildMock(logs);
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

            var queryParams = new PropertyRuleApplicationLogQueryParameters
            {
                SearchTerm = "101",
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _service.GetLogsAsync(queryParams, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(101, result.Items.First().PropertyId);
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsLog()
        {
            // Arrange
            var logs = new List<PropertyRuleApplicationLogEntity>
            {
                new() { Id = 1, PropertyId = 101, RuleName = "Rule A", IsActive = true, MarkedForDeletion = false }
            };

            var mockQueryable = MockQueryableExtensions.BuildMock(logs);
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

            // Act
            var result = await _service.GetByIdAsync(1, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Rule A", result.RuleName);
        }

        [Fact]
        public async Task GetByIdAsync_WithDeletedOrInactive_ReturnsNull()
        {
            // Arrange
            var logs = new List<PropertyRuleApplicationLogEntity>
            {
                new() { Id = 1, PropertyId = 101, RuleName = "Rule A", IsActive = false, MarkedForDeletion = false },
                new() { Id = 2, PropertyId = 102, RuleName = "Rule B", IsActive = true, MarkedForDeletion = true }
            };

            var mockQueryable = MockQueryableExtensions.BuildMock(logs);
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQueryable);

            // Act
            var result1 = await _service.GetByIdAsync(1, CancellationToken.None);
            var result2 = await _service.GetByIdAsync(2, CancellationToken.None);

            // Assert
            Assert.Null(result1);
            Assert.Null(result2);
        }

        [Fact]
        public async Task GetLogsAsync_PopulatesRuleScopeDetails()
        {
            // Arrange
            var logs = new List<PropertyRuleApplicationLogEntity>
            {
                new()
                {
                    Id = 1,
                    RuleCode = "RULE_01",
                    RuleName = "Rule A",
                    IsActive = true,
                    MarkedForDeletion = false,
                    RuleScopeId = 10,
                    RuleScopeName = "Commercial"
                }
            };

            var mockLogsQueryable = MockQueryableExtensions.BuildMock(logs);
            _mockRepository.Setup(r => r.GetQueryable()).Returns(mockLogsQueryable);

            var queryParams = new PropertyRuleApplicationLogQueryParameters { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _service.GetLogsAsync(queryParams, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Items);
            var item = result.Items.First();
            Assert.Equal(10, item.RuleScopeId);
            Assert.Equal("Commercial", item.RuleScopeName);
        }
    }
}
