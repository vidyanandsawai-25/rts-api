using AutoMapper;
using Moq;
using NtisPlatform.Application.DTOs.Master.SocialAttributeMaster;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Application;

public class SocialAttributeServiceTests
{
    private readonly Mock<IRepository<SocialAttributeEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly SocialAttributeService _service;

    public SocialAttributeServiceTests()
    {
        _mockRepository = new Mock<IRepository<SocialAttributeEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new SocialAttributeService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object
            );
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var entity = new SocialAttributeEntity
        {
            Id = 1,
            SocialAttributeCode = "CODE1",
            SocialAttributeName = "Test Attribute",
            DataType = "string",
            Unit = "unit",
            DisplayOrder = 1,
            ParentAttributeId = null,
            IsRequiredWhenParentTrue = false,
            IsDiscountApplicable = false
        };

        var dto = new SocialAttributeDto
        {
            Id = 1,
            SocialAttributeCode = "CODE1",
            SocialAttributeName = "Test Attribute",
            DataType = "string",
            Unit = "unit",
            DisplayOrder = 1,
            ParentAttributeId = null,
            IsRequiredWhenParentTrue = false,
            IsDiscountApplicable = false
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<SocialAttributeDto>(It.IsAny<SocialAttributeEntity>()))
            .Returns(dto);

        // Act
        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dto.Id, result.Id);
        Assert.Equal(dto.SocialAttributeCode, result.SocialAttributeCode);
    }
}