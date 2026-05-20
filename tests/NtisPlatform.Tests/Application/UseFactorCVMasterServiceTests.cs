using AutoMapper;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Master.UseFactorCVMaster;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Application.DTOs.Bulk;

namespace NtisPlatform.Tests.Application;

public class UseFactorCVMasterServiceTests
{
    private readonly Mock<IRepository<UseFactorCVMasterEntity, int>> _mockRepository;
    private readonly Mock<IRepository<TypeOfUseEntity, int>> _mockTypeOfUseRepository;
    private readonly Mock<IRepository<SubTypeOfUseEntity, int>> _mockSubTypeOfUseRepository;
    private readonly Mock<IRepository<AssessmentYearRangeCVEntity, int>> _mockYearRangeCVRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly IMapper _mapper;
    private readonly UseFactorCVMasterService _service;

    public UseFactorCVMasterServiceTests()
    {
        _mockRepository = new Mock<IRepository<UseFactorCVMasterEntity, int>>();
        _mockTypeOfUseRepository = new Mock<IRepository<TypeOfUseEntity, int>>();
        _mockSubTypeOfUseRepository = new Mock<IRepository<SubTypeOfUseEntity, int>>();
        _mockYearRangeCVRepository = new Mock<IRepository<AssessmentYearRangeCVEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<UseFactorCVMasterMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _service = new UseFactorCVMasterService(
            _mockRepository.Object,
            _mockTypeOfUseRepository.Object,
            _mockSubTypeOfUseRepository.Object,
            _mockYearRangeCVRepository.Object,
            _mockUnitOfWork.Object,
            _mapper);
    }

    [Fact]
    public async Task GetAllAsync_YearRangeWithNoFactorRows_ReturnsAllPlaceholders()
    {
        var entities = new List<UseFactorCVMasterEntity>();
        var mockQuery = entities.BuildMockDbSet();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery.Object);

        var typeOfUses = new List<TypeOfUseEntity>
        {
            new TypeOfUseEntity { Id = 1, TypeOfUseCode = "A", Description = "Type A", Type = "T1", TypeOfUseGroupId = 1, IsActive = true },
            new TypeOfUseEntity { Id = 2, TypeOfUseCode = "B", Description = "Type B", Type = "T2", TypeOfUseGroupId = 2, IsActive = true }
        }.BuildMockDbSet();
        _mockTypeOfUseRepository.Setup(r => r.GetQueryable()).Returns(typeOfUses.Object);

        var subTypeOfUses = new List<SubTypeOfUseEntity>
        {
            new SubTypeOfUseEntity { Id = 1, TypeOfUseId = 1, Description = "Sub 1", IsActive = true },
            new SubTypeOfUseEntity { Id = 2, TypeOfUseId = 2, Description = "Sub 2", IsActive = true }
        }.BuildMockDbSet();
        _mockSubTypeOfUseRepository.Setup(r => r.GetQueryable()).Returns(subTypeOfUses.Object);

        var yearRanges = new List<AssessmentYearRangeCVEntity>
        {
            new AssessmentYearRangeCVEntity { Id = 1, FromYear = 2000, ToYear = 2020, IsActive = true }
        }.BuildMockDbSet();
        _mockYearRangeCVRepository.Setup(r => r.GetQueryable()).Returns(yearRanges.Object);

        var qp = new UseFactorCVMasterQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, x => Assert.Equal(0, x.Id));
    }

    [Fact]
    public async Task GetAllAsync_YearRangeWithPartialFactorRows_ReturnsRealAndPlaceholderRows()
    {
        var entities = new List<UseFactorCVMasterEntity>
        {
            new UseFactorCVMasterEntity { Id = 1, TypeOfUseId = 1, SubTypeOfUseId = 1, Factor = 1.0m, YearRangeCVId = 1, IsActive = true }
        };
        var mockQuery = entities.BuildMockDbSet();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery.Object);

        var typeOfUses = new List<TypeOfUseEntity>
        {
            new TypeOfUseEntity { Id = 1, TypeOfUseCode = "A", Description = "Type A", Type = "T1", TypeOfUseGroupId = 1, IsActive = true },
            new TypeOfUseEntity { Id = 2, TypeOfUseCode = "B", Description = "Type B", Type = "T2", TypeOfUseGroupId = 2, IsActive = true }
        }.BuildMockDbSet();
        _mockTypeOfUseRepository.Setup(r => r.GetQueryable()).Returns(typeOfUses.Object);

        var subTypeOfUses = new List<SubTypeOfUseEntity>
        {
            new SubTypeOfUseEntity { Id = 1, TypeOfUseId = 1, Description = "Sub 1", IsActive = true },
            new SubTypeOfUseEntity { Id = 2, TypeOfUseId = 2, Description = "Sub 2", IsActive = true }
        }.BuildMockDbSet();
        _mockSubTypeOfUseRepository.Setup(r => r.GetQueryable()).Returns(subTypeOfUses.Object);

        var yearRanges = new List<AssessmentYearRangeCVEntity>
        {
            new AssessmentYearRangeCVEntity { Id = 1, FromYear = 2000, ToYear = 2020, IsActive = true }
        }.BuildMockDbSet();
        _mockYearRangeCVRepository.Setup(r => r.GetQueryable()).Returns(yearRanges.Object);

        var qp = new UseFactorCVMasterQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, x => x.Id == 1 && x.TypeOfUseId == 1 && x.SubTypeOfUseId == 1);
    }

    [Fact]
    public async Task BulkCreateAsync_ValidItems_CreatesAll()
    {
        // Arrange
        var createDtos = new[]
        {
            new CreateUseFactorCVMasterDto { TypeOfUseId = 1, SubTypeOfUseId = 1, Factor = 1.1m, YearRangeCVId = 1, IsActive = true },
            new CreateUseFactorCVMasterDto { TypeOfUseId = 2, SubTypeOfUseId = 2, Factor = 2.2m, YearRangeCVId = 1, IsActive = true }
        };

        _mockRepository.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<UseFactorCVMasterEntity>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkCreateAsync(createDtos);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.SuccessCount);
        Assert.True(result.Errors == null || result.Errors.Count == 0);
    }

    [Fact]
    public async Task BulkUpdateAsync_ValidItems_UpdatesAll()
    {
        // Arrange
        var updateDtos = new[]
        {
            new BulkUpdateItem<int, UpdateUseFactorCVMasterDto>(1, new UpdateUseFactorCVMasterDto { TypeOfUseId = 1, SubTypeOfUseId = 1, Factor = 1.5m, YearRangeCVId = 1, IsActive = true }),
            new BulkUpdateItem<int, UpdateUseFactorCVMasterDto>(2, new UpdateUseFactorCVMasterDto { TypeOfUseId = 2, SubTypeOfUseId = 2, Factor = 2.5m, YearRangeCVId = 1, IsActive = true })
        };

        _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => new UseFactorCVMasterEntity { Id = id });

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<UseFactorCVMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.BulkUpdateAsync(updateDtos);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.SuccessCount);
        Assert.True(result.Errors == null || result.Errors.Count == 0);
    }
}
