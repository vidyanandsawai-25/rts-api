using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Services.Property;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Enums;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Interfaces.Property;
using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Application.Services
{
    /// <summary>
    /// Tests for <see cref="PropertySearchService"/> - the per-feature service that owns the
    /// amount-filter validation, the query-parameter mapping / paged-result shaping and the
    /// dashboard delegation. Data access is mocked through <see cref="IPropertySearchRepository"/>.
    /// </summary>
    public class PropertySearchServiceTests
    {
        private readonly Mock<IPropertySearchRepository> _mockSearchRepository;
        private readonly PropertySearchService _service;

        public PropertySearchServiceTests()
        {
            _mockSearchRepository = new Mock<IPropertySearchRepository>();
            _service = new PropertySearchService(_mockSearchRepository.Object);
        }

        #region AmountFilterOperator validation

        [Fact]
        public async Task SearchPropertiesAsync_ShouldThrowValidationException_WhenAmountFilterOperatorProvidedWithoutAmountValue()
        {
            var queryParameters = new PropertySearchQueryParameters
            {
                AmountFilterOperator = "GreaterThan"
            };

            var ex = await Assert.ThrowsAsync<PropertyValidationException>(() =>
                _service.SearchPropertiesAsync(queryParameters, CancellationToken.None));

            Assert.Equal("AmountValue is required when AmountFilterOperator is 'GreaterThan'.", ex.Message);
        }

        [Fact]
        public async Task SearchPropertiesAsync_ShouldThrowValidationException_WhenBetweenWithoutAmountTo()
        {
            var queryParameters = new PropertySearchQueryParameters
            {
                AmountFilterOperator = "Between",
                AmountValue = 10000m
            };

            var ex = await Assert.ThrowsAsync<PropertyValidationException>(() =>
                _service.SearchPropertiesAsync(queryParameters, CancellationToken.None));

            Assert.Equal("AmountTo is required when AmountFilterOperator is Between.", ex.Message);
        }

        [Fact]
        public async Task SearchPropertiesAsync_ShouldThrowValidationException_WhenAmountValueGreaterThanAmountTo()
        {
            var queryParameters = new PropertySearchQueryParameters
            {
                AmountFilterOperator = "Between",
                AmountValue = 60000m,
                AmountTo = 30000m
            };

            var ex = await Assert.ThrowsAsync<PropertyValidationException>(() =>
                _service.SearchPropertiesAsync(queryParameters, CancellationToken.None));

            Assert.Equal("AmountValue cannot be greater than AmountTo.", ex.Message);
        }

        [Fact]
        public async Task SearchPropertiesAsync_ShouldThrowValidationException_WhenTopOperatorWithoutTopCount()
        {
            var queryParameters = new PropertySearchQueryParameters
            {
                AmountFilterOperator = "Top"
            };

            var ex = await Assert.ThrowsAsync<PropertyValidationException>(() =>
                _service.SearchPropertiesAsync(queryParameters, CancellationToken.None));

            Assert.Equal("TopCount must be a positive number when AmountFilterOperator is Top.", ex.Message);
        }

        [Fact]
        public async Task SearchPropertiesAsync_ShouldThrowValidationException_WhenTopCountIsZero()
        {
            var queryParameters = new PropertySearchQueryParameters
            {
                AmountFilterOperator = "Top",
                TopCount = 0
            };

            var ex = await Assert.ThrowsAsync<PropertyValidationException>(() =>
                _service.SearchPropertiesAsync(queryParameters, CancellationToken.None));

            Assert.Equal("TopCount must be a positive number when AmountFilterOperator is Top.", ex.Message);
        }

        [Fact]
        public async Task SearchPropertiesAsync_ShouldThrowValidationException_WhenInvalidOperator()
        {
            var queryParameters = new PropertySearchQueryParameters
            {
                AmountFilterOperator = "InvalidOperator",
                AmountValue = 10000m
            };

            var ex = await Assert.ThrowsAsync<PropertyValidationException>(() =>
                _service.SearchPropertiesAsync(queryParameters, CancellationToken.None));

            Assert.Contains("Invalid AmountFilterOperator value", ex.Message);
        }

        [Fact]
        public async Task SearchPropertiesAsync_ShouldThrowValidationException_WhenUnsupportedButDefinedOperator()
        {
            // Test for operators that exist in FilterOperator enum but are not supported for tax filtering
            var queryParameters = new PropertySearchQueryParameters
            {
                AmountFilterOperator = "Contains",  // Valid enum value but not supported for tax filtering
                AmountValue = 10000m
            };

            var ex = await Assert.ThrowsAsync<PropertyValidationException>(() =>
                _service.SearchPropertiesAsync(queryParameters, CancellationToken.None));

            Assert.Contains("Invalid AmountFilterOperator value", ex.Message);
            Assert.Contains("Valid values are: Equals, GreaterThan, LessThan, Between, Top", ex.Message);
        }

        #endregion

        #region SearchPropertiesAsync mapping / paging

        [Fact]
        public async Task SearchPropertiesAsync_WithValidParameters_ReturnsResults()
        {
            // Arrange
            var queryParameters = new PropertySearchQueryParameters
            {
                ZoneId = 1,
                WardId = 2,
                PageNumber = 1,
                PageSize = 10
            };

            var expectedTuple = (
                TotalCount: 2,
                Items: new List<PropertySearchResponseDto>
                {
                    new PropertySearchResponseDto { PropertyId = 1, PropertyNo = "001" },
                    new PropertySearchResponseDto { PropertyId = 2, PropertyNo = "002" }
                }
            );

            _mockSearchRepository
                .Setup(x => x.SearchPropertiesAsync(
                    It.IsAny<PropertySearchRequestDto>(),
                    queryParameters.PageNumber,
                    queryParameters.PageSize,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTuple);

            // Act
            var result = await _service.SearchPropertiesAsync(queryParameters, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(10, result.PageSize);
        }

        [Fact]
        public async Task SearchPropertiesAsync_WithPropertyNoFromOnly_PassesToRepository()
        {
            // Arrange
            var queryParameters = new PropertySearchQueryParameters
            {
                PropertyNoFrom = "050",
                PageNumber = 1,
                PageSize = 10
            };

            var expectedTuple = (
                TotalCount: 5,
                Items: new List<PropertySearchResponseDto>
                {
                    new PropertySearchResponseDto { PropertyId = 1, PropertyNo = "050" }
                }
            );

            _mockSearchRepository
                .Setup(x => x.SearchPropertiesAsync(
                    It.Is<PropertySearchRequestDto>(req => req.PropertyNoFrom == "050" && req.PropertyNoTo == null),
                    1,
                    10,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTuple);

            // Act
            var result = await _service.SearchPropertiesAsync(queryParameters, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.TotalCount);
            _mockSearchRepository.Verify(
                x => x.SearchPropertiesAsync(
                    It.Is<PropertySearchRequestDto>(req => req.PropertyNoFrom == "050" && req.PropertyNoTo == null),
                    1,
                    10,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchPropertiesAsync_WithPropertyNoToOnly_PassesToRepository()
        {
            // Arrange
            var queryParameters = new PropertySearchQueryParameters
            {
                PropertyNoTo = "100",
                PageNumber = 1,
                PageSize = 10
            };

            var expectedTuple = (
                TotalCount: 3,
                Items: new List<PropertySearchResponseDto>
                {
                    new PropertySearchResponseDto { PropertyId = 1, PropertyNo = "050" }
                }
            );

            _mockSearchRepository
                .Setup(x => x.SearchPropertiesAsync(
                    It.Is<PropertySearchRequestDto>(req => req.PropertyNoFrom == null && req.PropertyNoTo == "100"),
                    1,
                    10,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTuple);

            // Act
            var result = await _service.SearchPropertiesAsync(queryParameters, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.TotalCount);
            _mockSearchRepository.Verify(
                x => x.SearchPropertiesAsync(
                    It.Is<PropertySearchRequestDto>(req => req.PropertyNoFrom == null && req.PropertyNoTo == "100"),
                    1,
                    10,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchPropertiesAsync_WithPageSizeMinusOne_PassesToRepository()
        {
            // Arrange
            var queryParameters = new PropertySearchQueryParameters
            {
                PageNumber = 1,
                PageSize = -1
            };

            var expectedTuple = (
                TotalCount: 100,
                Items: Enumerable.Range(1, 100)
                    .Select(i => new PropertySearchResponseDto { PropertyId = i })
                    .ToList()
            );

            _mockSearchRepository
                .Setup(x => x.SearchPropertiesAsync(
                    It.IsAny<PropertySearchRequestDto>(),
                    1,
                    -1,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTuple);

            // Act
            var result = await _service.SearchPropertiesAsync(queryParameters, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100, result.TotalCount);
            Assert.Equal(100, result.Items.Count());
            _mockSearchRepository.Verify(
                x => x.SearchPropertiesAsync(
                    It.IsAny<PropertySearchRequestDto>(),
                    1,
                    -1,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SearchPropertiesAsync_WithOutOfRangePage_ReturnsEmptyResults()
        {
            // Arrange
            var queryParameters = new PropertySearchQueryParameters
            {
                PageNumber = 100,
                PageSize = 10
            };

            var expectedTuple = (
                TotalCount: 50,
                Items: new List<PropertySearchResponseDto>()
            );

            _mockSearchRepository
                .Setup(x => x.SearchPropertiesAsync(
                    It.IsAny<PropertySearchRequestDto>(),
                    100,
                    10,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTuple);

            // Act
            var result = await _service.SearchPropertiesAsync(queryParameters, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(50, result.TotalCount);
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task SearchPropertiesAsync_WithNoResults_ReturnsEmptyPagedResult()
        {
            // Arrange
            var queryParameters = new PropertySearchQueryParameters
            {
                ZoneId = 999,
                PageNumber = 1,
                PageSize = 10
            };

            var expectedTuple = (
                TotalCount: 0,
                Items: new List<PropertySearchResponseDto>()
            );

            _mockSearchRepository
                .Setup(x => x.SearchPropertiesAsync(
                    It.IsAny<PropertySearchRequestDto>(),
                    1,
                    10,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTuple);

            // Act
            var result = await _service.SearchPropertiesAsync(queryParameters, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task SearchPropertiesAsync_WithAllFilters_PassesAllToRepository()
        {
            // Arrange
            var queryParameters = new PropertySearchQueryParameters
            {
                ZoneId = 1,
                WardId = 2,
                PropertyNoFrom = "001",
                PropertyNoTo = "100",
                CategoryId = 3,
                PropertyTypeId = 4,
                TypeOfUseId = 5,
                OldPropertyNo = "OLD-001",
                UPICId = "UPIC-001",
                CSN = "CSN-001",
                SubZoneNo = "SUB-001",
                PlotNo = "PLOT-001",
                PropertyAssessmentStatusId = 6,
                MobileNo = "1234567890",
                OwnerName = "John Doe",
                OccupierName = "Jane Doe",
                FlatOrShopName = "Shop A",
                SocietyName = "Society ABC",
                Address = "123 Main St",
                PageNumber = 1,
                PageSize = 10
            };

            var expectedTuple = (
                TotalCount: 1,
                Items: new List<PropertySearchResponseDto>
                {
                    new PropertySearchResponseDto { PropertyId = 1 }
                }
            );

            _mockSearchRepository
                .Setup(x => x.SearchPropertiesAsync(
                    It.Is<PropertySearchRequestDto>(req =>
                        req.ZoneId == 1 &&
                        req.WardId == 2 &&
                        req.PropertyNoFrom == "001" &&
                        req.PropertyNoTo == "100" &&
                        req.CategoryId == 3 &&
                        req.PropertyTypeId == 4 &&
                        req.TypeOfUseId == 5 &&
                        req.OldPropertyNo == "OLD-001" &&
                        req.UPICId == "UPIC-001" &&
                        req.CSN == "CSN-001" &&
                        req.SubZoneNo == "SUB-001" &&
                        req.PlotNo == "PLOT-001" &&
                        req.PropertyAssessmentStatusId == 6 &&
                        req.MobileNo == "1234567890" &&
                        req.OwnerName == "John Doe" &&
                        req.OccupierName == "Jane Doe" &&
                        req.FlatOrShopName == "Shop A" &&
                        req.SocietyName == "Society ABC" &&
                        req.Address == "123 Main St"),
                    1,
                    10,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedTuple);

            // Act
            var result = await _service.SearchPropertiesAsync(queryParameters, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            _mockSearchRepository.Verify(
                x => x.SearchPropertiesAsync(
                    It.Is<PropertySearchRequestDto>(req =>
                        req.ZoneId == 1 &&
                        req.PropertyNoFrom == "001" &&
                        req.PropertyNoTo == "100"),
                    1,
                    10,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion

        #region GetPropertyDashboardStatsAsync

        [Fact]
        public async Task GetPropertyDashboardStatsAsync_ReturnsStats()
        {
            // Arrange
            var expectedStats = new PropertyDashboardStatsDto
            {
                RegisteredPropertyCount = 100,
                GeoSequencingPropertyCount = 100,
                SurveyPropertyCount = 0,
                DataProcessingPropertyCount = 0,
                QualityAnalysisPropertyCount = 0,
                AssessmentCompletedPropertyCount = 0
            };

            _mockSearchRepository
                .Setup(x => x.GetPropertyDashboardStatsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _service.GetPropertyDashboardStatsAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100, result.RegisteredPropertyCount);
            Assert.Equal(100, result.GeoSequencingPropertyCount);
            Assert.Equal(0, result.SurveyPropertyCount);
            Assert.Equal(0, result.DataProcessingPropertyCount);
            Assert.Equal(0, result.QualityAnalysisPropertyCount);
            Assert.Equal(0, result.AssessmentCompletedPropertyCount);
            _mockSearchRepository.Verify(
                x => x.GetPropertyDashboardStatsAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetPropertyDashboardStatsAsync_WithZeroCounts_ReturnsZeros()
        {
            // Arrange
            var expectedStats = new PropertyDashboardStatsDto
            {
                RegisteredPropertyCount = 0,
                GeoSequencingPropertyCount = 0,
                SurveyPropertyCount = 0,
                DataProcessingPropertyCount = 0,
                QualityAnalysisPropertyCount = 0,
                AssessmentCompletedPropertyCount = 0
            };

            _mockSearchRepository
                .Setup(x => x.GetPropertyDashboardStatsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _service.GetPropertyDashboardStatsAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.RegisteredPropertyCount);
            Assert.Equal(0, result.GeoSequencingPropertyCount);
            Assert.Equal(0, result.SurveyPropertyCount);
            Assert.Equal(0, result.DataProcessingPropertyCount);
            Assert.Equal(0, result.QualityAnalysisPropertyCount);
            Assert.Equal(0, result.AssessmentCompletedPropertyCount);
        }

        [Fact]
        public async Task GetPropertyDashboardStatsAsync_WithCancellationToken_PropagatesToken()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var expectedStats = new PropertyDashboardStatsDto();

            _mockSearchRepository
                .Setup(x => x.GetPropertyDashboardStatsAsync(token))
                .ReturnsAsync(expectedStats);

            // Act
            await _service.GetPropertyDashboardStatsAsync(token);

            // Assert
            _mockSearchRepository.Verify(
                x => x.GetPropertyDashboardStatsAsync(token),
                Times.Once);
        }

        #endregion

        #region GetScopeOptions Tests

        [Fact]
        public void GetScopeOptions_WithNullCategory_ReturnsAllScopeCategories()
        {
            // Act
            var result = _service.GetScopeOptions(category: null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.Count);

            // Verify mapping accuracy
            var allProps = result.Find(c => c.Id == (int)ScopeCategory.AllProperties);
            Assert.NotNull(allProps);
            Assert.Equal("AllProperties", allProps.Name);
            Assert.Equal("All Properties", allProps.DisplayName);
            Assert.Equal("Entire corporation", allProps.Description);
            Assert.Empty(allProps.Options);

            var buildingWise = result.Find(c => c.Id == (int)ScopeCategory.BuildingWise);
            Assert.NotNull(buildingWise);
            Assert.Equal("BuildingWise", buildingWise.Name);
            Assert.Equal("Building Wise", buildingWise.DisplayName);
            Assert.Equal("Building level", buildingWise.Description);
            Assert.Equal(new List<string> { "Zone", "Ward", "Property No" }, buildingWise.Options);
        }

        [Fact]
        public void GetScopeOptions_WithValidCategory_ReturnsOnlyThatCategory()
        {
            // Act
            var result = _service.GetScopeOptions(category: ScopeCategory.PropertyRange);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);

            var propertyRange = result[0];
            Assert.Equal((int)ScopeCategory.PropertyRange, propertyRange.Id);
            Assert.Equal("PropertyRange", propertyRange.Name);
            Assert.Equal("Property Range", propertyRange.DisplayName);
            Assert.Equal("From-to property range", propertyRange.Description);
            Assert.Equal(new List<string> { "Ward", "From Property", "To Property" }, propertyRange.Options);
        }

        #endregion
    }
}


