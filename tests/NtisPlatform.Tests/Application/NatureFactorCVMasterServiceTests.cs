using AutoMapper;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Master.NatureFactorCVMaster;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Application.DTOs.Bulk;

namespace NtisPlatform.Tests.Application;

public class NatureFactorCVMasterServiceTests
{
    private readonly Mock<IRepository<NatureFactorCVMasterEntity, int>> _mockRepository;
    private readonly Mock<IRepository<ConstructionTypeEntity, int>> _mockConstructionTypeRepository;
    private readonly Mock<IRepository<AssessmentYearRangeCVEntity, int>> _mockYearRangeCVRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly IMapper _mapper;
    private readonly NatureFactorCVMasterService _service;

    public NatureFactorCVMasterServiceTests()
    {
        _mockRepository = new Mock<IRepository<NatureFactorCVMasterEntity, int>>();
        _mockConstructionTypeRepository = new Mock<IRepository<ConstructionTypeEntity, int>>();
        _mockYearRangeCVRepository = new Mock<IRepository<AssessmentYearRangeCVEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<NatureFactorCVMasterMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _service = new NatureFactorCVMasterService(
            _mockRepository.Object,
            _mockConstructionTypeRepository.Object,
            _mockYearRangeCVRepository.Object,
            _mockUnitOfWork.Object,
            _mapper);
    }

    [Fact]
    public async Task GetAllAsync_YearRangeWithNoFactorRows_ReturnsAllPlaceholders()
    {
        var entities = new List<NatureFactorCVMasterEntity>();
        var mockQuery = entities.BuildMockDbSet();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery.Object);

        var constructionTypes = new List<ConstructionTypeEntity>
        {
            new ConstructionTypeEntity { Id = 1, ConstructionCode = "A", Description = "Type A", IsActive = true },
            new ConstructionTypeEntity { Id = 2, ConstructionCode = "B", Description = "Type B", IsActive = true }
        }.BuildMockDbSet();
        _mockConstructionTypeRepository.Setup(r => r.GetQueryable()).Returns(constructionTypes.Object);

        var yearRanges = new List<AssessmentYearRangeCVEntity>
        {
            new AssessmentYearRangeCVEntity { Id = 1, FromYear = 2000, ToYear = 2020, IsActive = true }
        }.BuildMockDbSet();
        _mockYearRangeCVRepository.Setup(r => r.GetQueryable()).Returns(yearRanges.Object);

        var qp = new NatureFactorCVMasterQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, x => Assert.Equal(0, x.Id));
    }

    [Fact]
    public async Task GetAllAsync_YearRangeWithPartialFactorRows_ReturnsRealAndPlaceholderRows()
    {
        var entities = new List<NatureFactorCVMasterEntity>
        {
            new NatureFactorCVMasterEntity { Id = 1, ConstructionTypeId = 1, Factor = 1.0m, YearRangeCVId = 1, IsActive = true }
        };
        var mockQuery = entities.BuildMockDbSet();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery.Object);

        var constructionTypes = new List<ConstructionTypeEntity>
        {
            new ConstructionTypeEntity { Id = 1, ConstructionCode = "A", Description = "Type A", IsActive = true },
            new ConstructionTypeEntity { Id = 2, ConstructionCode = "B", Description = "Type B", IsActive = true }
        }.BuildMockDbSet();
        _mockConstructionTypeRepository.Setup(r => r.GetQueryable()).Returns(constructionTypes.Object);

        var yearRanges = new List<AssessmentYearRangeCVEntity>
        {
            new AssessmentYearRangeCVEntity { Id = 1, FromYear = 2000, ToYear = 2020, IsActive = true }
        }.BuildMockDbSet();
        _mockYearRangeCVRepository.Setup(r => r.GetQueryable()).Returns(yearRanges.Object);

        var qp = new NatureFactorCVMasterQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, x => x.Id == 1 && x.ConstructionTypeId == 1);
    }

    [Fact]
    public async Task BulkCreateAsync_ValidItems_CreatesAll()
    {
        var createDtos = new[]
        {
            new CreateNatureFactorCVMasterDto { ConstructionTypeId = 1, Factor = 1.1m, YearRangeCVId = 1, IsActive = true },
            new CreateNatureFactorCVMasterDto { ConstructionTypeId = 2, Factor = 2.2m, YearRangeCVId = 1, IsActive = true }
        };

        _mockRepository.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<NatureFactorCVMasterEntity>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.BulkCreateAsync(createDtos);

        Assert.NotNull(result);
        Assert.Equal(2, result.SuccessCount);
        Assert.True(result.Errors == null || result.Errors.Count == 0);
    }

    [Fact]
    public async Task BulkUpdateAsync_ValidItems_UpdatesAll()
    {
        var updateDtos = new[]
        {
            new BulkUpdateItem<int, UpdateNatureFactorCVMasterDto>(1, new UpdateNatureFactorCVMasterDto { ConstructionTypeId = 1, Factor = 1.5m, YearRangeCVId = 1, IsActive = true }),
            new BulkUpdateItem<int, UpdateNatureFactorCVMasterDto>(2, new UpdateNatureFactorCVMasterDto { ConstructionTypeId = 2, Factor = 2.5m, YearRangeCVId = 1, IsActive = true })
        };

        _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => new NatureFactorCVMasterEntity { Id = id });

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<NatureFactorCVMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.BulkUpdateAsync(updateDtos);

        Assert.NotNull(result);
        Assert.Equal(2, result.SuccessCount);
        Assert.True(result.Errors == null || result.Errors.Count == 0);
    }
}
