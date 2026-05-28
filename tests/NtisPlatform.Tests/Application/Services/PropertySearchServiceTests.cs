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
                AmountFilterOperator = FilterOperator.GreaterThan
            };

            var ex = await Assert.ThrowsAsync<DataValidationException>(() =>
                _service.SearchPropertiesAsync(queryParameters, CancellationToken.None));

            Assert.Equal("AmountValue is required when AmountFilterOperator is provided.", ex.Message);
        }

        [Fact]
        public async Task SearchPropertiesAsync_ShouldThrowValidationException_WhenBetweenWithoutAmountTo()
        {
            var queryParameters = new PropertySearchQueryParameters
            {
                AmountFilterOperator = FilterOperator.Between,
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
                AmountFilterOperator = FilterOperator.Between,
                AmountValue = 60000m,
                AmountTo = 30000m
            };

            var ex = await Assert.ThrowsAsync<DataValidationException>(() =>
                _service.SearchPropertiesAsync(queryParameters, CancellationToken.None));

            Assert.Equal("AmountValue cannot be greater than AmountTo.", ex.Message);
        }
    }
}