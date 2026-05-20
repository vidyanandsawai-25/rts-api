using AutoMapper;
using MockQueryable.Moq;
using Moq;
using NtisPlatform.Application.DTOs.Master.AgeFactorCVMaster;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Application.DTOs.Bulk;

namespace NtisPlatform.Tests.Application;

public class AgeFactorCVMasterServiceTests
{
    private readonly Mock<IRepository<AgeFactorCVMasterEntity, int>> _mockRepository;
    private readonly Mock<IRepository<ConstructionTypeEntity, int>> _mockConstructionTypeRepository;
    private readonly Mock<IRepository<AssessmentYearRangeCVEntity, int>> _mockYearRangeCVRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly IMapper _mapper;
    private readonly AgeFactorCVMasterService _service;

    public AgeFactorCVMasterServiceTests()
    {
        _mockRepository = new Mock<IRepository<AgeFactorCVMasterEntity, int>>();
        _mockConstructionTypeRepository = new Mock<IRepository<ConstructionTypeEntity, int>>();
        _mockYearRangeCVRepository = new Mock<IRepository<AssessmentYearRangeCVEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AgeFactorCVMasterMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _service = new AgeFactorCVMasterService(
            _mockRepository.Object,
            _mockConstructionTypeRepository.Object,
            _mockYearRangeCVRepository.Object,
            _mockUnitOfWork.Object,
            _mapper);
    }

    [Fact]
    public async Task GetAllAsync_YearRangeWithNoFactorRows_ReturnsAllPlaceholders()
    {
        var entities = new List<AgeFactorCVMasterEntity>();
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

        var qp = new AgeFactorCVMasterQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, x => Assert.Equal(0, x.Id));
        Assert.All(result.Items, x => Assert.Equal(5, x.AgeTo));
    }

    [Fact]
    public async Task GetAllAsync_FilterByAgeFrom_ReturnsOnlyMatchingRows()
    {
        var entities = new List<AgeFactorCVMasterEntity>
        {
            new AgeFactorCVMasterEntity { Id = 1, ConstructionTypeId = 1, AgeFrom = 0, AgeTo = 5, Factor = 1.0m, YearRangeCVId = 1, IsActive = true },
            new AgeFactorCVMasterEntity { Id = 2, ConstructionTypeId = 1, AgeFrom = 10, AgeTo = 15, Factor = 2.0m, YearRangeCVId = 1, IsActive = true }
        };
        var mockQuery = entities.BuildMockDbSet();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery.Object);

        var constructionTypes = new List<ConstructionTypeEntity>
        {
            new ConstructionTypeEntity { Id = 1, ConstructionCode = "A", Description = "Type A", IsActive = true }
        }.BuildMockDbSet();
        _mockConstructionTypeRepository.Setup(r => r.GetQueryable()).Returns(constructionTypes.Object);

        var yearRanges = new List<AssessmentYearRangeCVEntity>
        {
            new AssessmentYearRangeCVEntity { Id = 1, FromYear = 2000, ToYear = 2020, IsActive = true }
        }.BuildMockDbSet();
        _mockYearRangeCVRepository.Setup(r => r.GetQueryable()).Returns(yearRanges.Object);

        var qp = new AgeFactorCVMasterQueryParameters { AgeFrom = 5, PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.NotNull(result);
        Assert.All(result.Items, x => Assert.True(x.AgeFrom >= 5));
    }

    [Fact]
    public async Task GetAllAsync_FilterByAgeTo_ReturnsOnlyMatchingRows()
    {
        var entities = new List<AgeFactorCVMasterEntity>
        {
            new AgeFactorCVMasterEntity { Id = 1, ConstructionTypeId = 1, AgeFrom = 0, AgeTo = 5, Factor = 1.0m, YearRangeCVId = 1, IsActive = true },
            new AgeFactorCVMasterEntity { Id = 2, ConstructionTypeId = 1, AgeFrom = 10, AgeTo = 15, Factor = 2.0m, YearRangeCVId = 1, IsActive = true }
        };
        var mockQuery = entities.BuildMockDbSet();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery.Object);

        var constructionTypes = new List<ConstructionTypeEntity>
        {
            new ConstructionTypeEntity { Id = 1, ConstructionCode = "A", Description = "Type A", IsActive = true }
        }.BuildMockDbSet();
        _mockConstructionTypeRepository.Setup(r => r.GetQueryable()).Returns(constructionTypes.Object);

        var yearRanges = new List<AssessmentYearRangeCVEntity>
        {
            new AssessmentYearRangeCVEntity { Id = 1, FromYear = 2000, ToYear = 2020, IsActive = true }
        }.BuildMockDbSet();
        _mockYearRangeCVRepository.Setup(r => r.GetQueryable()).Returns(yearRanges.Object);

        var qp = new AgeFactorCVMasterQueryParameters { AgeTo = 10, PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.NotNull(result);
        Assert.All(result.Items, x => Assert.True(x.AgeTo <= 10));
    }

    [Fact]
    public async Task GetAllAsync_FilterByAgeFromAndAgeTo_ReturnsOnlyMatchingRows()
    {
        var entities = new List<AgeFactorCVMasterEntity>
        {
            new AgeFactorCVMasterEntity { Id = 1, ConstructionTypeId = 1, AgeFrom = 0, AgeTo = 5, Factor = 1.0m, YearRangeCVId = 1, IsActive = true },
            new AgeFactorCVMasterEntity { Id = 2, ConstructionTypeId = 1, AgeFrom = 10, AgeTo = 15, Factor = 2.0m, YearRangeCVId = 1, IsActive = true },
            new AgeFactorCVMasterEntity { Id = 3, ConstructionTypeId = 1, AgeFrom = 20, AgeTo = 25, Factor = 3.0m, YearRangeCVId = 1, IsActive = true }
        };
        var mockQuery = entities.BuildMockDbSet();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery.Object);

        var constructionTypes = new List<ConstructionTypeEntity>
        {
            new ConstructionTypeEntity { Id = 1, ConstructionCode = "A", Description = "Type A" }
        }.BuildMockDbSet();
        _mockConstructionTypeRepository.Setup(r => r.GetQueryable()).Returns(constructionTypes.Object);

        var yearRanges = new List<AssessmentYearRangeCVEntity>
        {
            new AssessmentYearRangeCVEntity { Id = 1, FromYear = 2000, ToYear = 2020 }
        }.BuildMockDbSet();
        _mockYearRangeCVRepository.Setup(r => r.GetQueryable()).Returns(yearRanges.Object);

        var qp = new AgeFactorCVMasterQueryParameters { AgeFrom = 5, AgeTo = 20, PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.NotNull(result);
        Assert.All(result.Items, x => Assert.True(x.AgeFrom >= 5 && x.AgeTo <= 20));
    }
    [Fact]
    public async Task GetAllAsync_YearRangeWithPartialFactorRows_ReturnsRealAndPlaceholderRows()
    {
        var entities = new List<AgeFactorCVMasterEntity>
        {
            new AgeFactorCVMasterEntity { Id = 1, ConstructionTypeId = 1, AgeFrom = 0, AgeTo = 5, Factor = 1.0m, YearRangeCVId = 1, IsActive = true }
        };
        var mockQuery = entities.BuildMockDbSet();
        _mockRepository.Setup(r => r.GetQueryable()).Returns(mockQuery.Object);

        var constructionTypes = new List<ConstructionTypeEntity>
        {
            new ConstructionTypeEntity { Id = 1, ConstructionCode = "A", Description = "Type A" },
            new ConstructionTypeEntity { Id = 2, ConstructionCode = "B", Description = "Type B" }
        }.BuildMockDbSet();
        _mockConstructionTypeRepository.Setup(r => r.GetQueryable()).Returns(constructionTypes.Object);

        var yearRanges = new List<AssessmentYearRangeCVEntity>
        {
            new AssessmentYearRangeCVEntity { Id = 1, FromYear = 2000, ToYear = 2020 }
        }.BuildMockDbSet();
        _mockYearRangeCVRepository.Setup(r => r.GetQueryable()).Returns(yearRanges.Object);

        var qp = new AgeFactorCVMasterQueryParameters { PageNumber = 1, PageSize = 10 };
        var result = await _service.GetAllAsync(qp, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, x => x.Id == 1 && x.ConstructionTypeId == 1);
        // Assert placeholder row AgeTo is 5
        Assert.Contains(result.Items, x => x.Id == 0 && x.AgeTo == 5);
    }

    [Fact]
    public async Task BulkCreateAsync_ValidItems_CreatesAll()
    {
        var createDtos = new[]
        {
            new CreateAgeFactorCVMasterDto { ConstructionTypeId = 1, AgeFrom = 0, AgeTo = 5, Factor = 1.1m, YearRangeCVId = 1, IsActive = true },
            new CreateAgeFactorCVMasterDto { ConstructionTypeId = 2, AgeFrom = 6, AgeTo = 10, Factor = 2.2m, YearRangeCVId = 1, IsActive = true }
        };

        _mockRepository.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<AgeFactorCVMasterEntity>>(), It.IsAny<CancellationToken>()))
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
            new BulkUpdateItem<int, UpdateAgeFactorCVMasterDto>(1, new UpdateAgeFactorCVMasterDto { ConstructionTypeId = 1, AgeFrom = 0, AgeTo = 5, Factor = 1.5m, YearRangeCVId = 1, IsActive = true }),
            new BulkUpdateItem<int, UpdateAgeFactorCVMasterDto>(2, new UpdateAgeFactorCVMasterDto { ConstructionTypeId = 2, AgeFrom = 6, AgeTo = 10, Factor = 2.5m, YearRangeCVId = 1, IsActive = true })
        };

        _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => new AgeFactorCVMasterEntity { Id = id });

        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<AgeFactorCVMasterEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.BulkUpdateAsync(updateDtos);

        Assert.NotNull(result);
        Assert.Equal(2, result.SuccessCount);
        Assert.True(result.Errors == null || result.Errors.Count == 0);
    }
}
