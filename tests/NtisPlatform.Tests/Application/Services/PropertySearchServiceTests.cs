using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

using DataValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace NtisPlatform.Tests.Application.Services
{
    public class PropertySearchServiceTests
    {
        private readonly PropertyService _service;

        public PropertySearchServiceTests()
        {
            var repositoryMock = new Mock<IRepository<PropertyEntity, int>>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var mapperMock = new Mock<IMapper>();
            var propertyRepositoryMock = new Mock<IPropertyRepository>();
            var loggerMock = new Mock<ILogger<PropertyService>>();

            var featureFlags = Options.Create(new FeatureFlagsOptions());

            _service = new PropertyService(
                repositoryMock.Object,
                unitOfWorkMock.Object,
                mapperMock.Object,
                propertyRepositoryMock.Object,
                loggerMock.Object,
                featureFlags);
        }

        [Fact]
        public async Task SearchPropertiesAsync_ShouldThrowValidationException_WhenAmountFilterOperatorProvidedWithoutAmountValue()
        {
            var queryParameters = new PropertySearchQueryParameters
            {
                AmountFilterOperator = "GreaterThan"
            };

            var ex = await Assert.ThrowsAsync<DataValidationException>(() =>
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

            var ex = await Assert.ThrowsAsync<DataValidationException>(() =>
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

            var ex = await Assert.ThrowsAsync<DataValidationException>(() =>
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

            var ex = await Assert.ThrowsAsync<DataValidationException>(() =>
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

            var ex = await Assert.ThrowsAsync<DataValidationException>(() =>
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

            var ex = await Assert.ThrowsAsync<DataValidationException>(() =>
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

            var ex = await Assert.ThrowsAsync<DataValidationException>(() =>
                _service.SearchPropertiesAsync(queryParameters, CancellationToken.None));

            Assert.Contains("Invalid AmountFilterOperator value", ex.Message);
            Assert.Contains("Valid values are: Equals, GreaterThan, LessThan, Between, Top", ex.Message);
        }
    }
}