using AutoMapper;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.PropertyChangeCategory;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

public class PropertyChangeCategoryServiceTests
{
    private readonly Mock<IRepository<PropertyMapDetailEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<PropertyChangeCategoryService>> _mockLogger;
    private readonly Mock<IRepository<PropertyEntity, int>> _mockPropertyRepository;
    private readonly Mock<IRepository<PropertyCategoryEntity, int>> _mockCategoryRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly PropertyChangeCategoryService _service;

    public PropertyChangeCategoryServiceTests()
    {
        _mockRepository = new Mock<IRepository<PropertyMapDetailEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<PropertyChangeCategoryService>>();
        _mockPropertyRepository = new Mock<IRepository<PropertyEntity, int>>();
        _mockCategoryRepository = new Mock<IRepository<PropertyCategoryEntity, int>>();
        _mockMapper = new Mock<IMapper>();

        _service = new PropertyChangeCategoryService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object,
            _mockPropertyRepository.Object,
            _mockCategoryRepository.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task UpdateAsync_PropertyNotFound_ThrowsValidationException()
    {
        // Arrange
        var dto = new UpdatePropertyChangeCategoryDto { PropertyId = 10, CategoryId = 2 };
        _mockPropertyRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyEntity>().BuildMock());
        _mockCategoryRepository.Setup(r => r.GetQueryable()).Returns(new List<PropertyCategoryEntity>().BuildMock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NtisPlatform.Application.Exceptions.ValidationException>(() => _service.UpdateAsync(10, dto, CancellationToken.None));
        Assert.Contains("Property not found", ex.Message);
    }
}
