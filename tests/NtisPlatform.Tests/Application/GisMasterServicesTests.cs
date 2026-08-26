using AutoMapper;
using Moq;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.GIS;
using NtisPlatform.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class GisMasterServicesTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;

    public GisMasterServicesTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();

        _mockUnitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    #region GisCorporationConfigService Tests

    [Fact]
    public async Task GisCorporationConfigService_GetById_ReturnsDto()
    {
        var mockRepo = new Mock<IRepository<GisCorporationConfigEntity, int>>();
        var entity = new GisCorporationConfigEntity { Id = 1, UlbId = 1, DefaultCenterLat = 19.2184m, DefaultCenterLng = 72.9781m };
        var dto = new GisCorporationConfigDto { Id = 1, UlbId = 1, DefaultCenterLat = 19.2184m, DefaultCenterLng = 72.9781m };

        mockRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<GisCorporationConfigDto>(entity)).Returns(dto);

        var service = new GisCorporationConfigService(mockRepo.Object, _mockUnitOfWork.Object, _mockMapper.Object);
        var result = await service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result!.UlbId);
    }

    #endregion

    #region GisDepartmentUserAccessService Tests

    [Fact]
    public async Task GisDepartmentUserAccessService_Create_ReturnsCreatedDto()
    {
        var mockRepo = new Mock<IRepository<GisDepartmentUserAccessEntity, int>>();
        var createDto = new CreateGisDepartmentUserAccessDto { UserId = 1, DepartmentId = 1, UlbId = 1, ZoneId = 17, CanView = true };
        var entity = new GisDepartmentUserAccessEntity { Id = 1, UserId = 1, DepartmentId = 1, UlbId = 1, ZoneId = 17, CanView = true };
        var resultDto = new GisDepartmentUserAccessDto { Id = 1, UserId = 1, DepartmentId = 1, UlbId = 1, ZoneId = 17, CanView = true };

        _mockMapper.Setup(m => m.Map<GisDepartmentUserAccessEntity>(createDto)).Returns(entity);
        _mockMapper.Setup(m => m.Map<GisDepartmentUserAccessDto>(entity)).Returns(resultDto);

        var service = new GisDepartmentUserAccessService(mockRepo.Object, _mockUnitOfWork.Object, _mockMapper.Object);
        var result = await service.CreateAsync(createDto);

        Assert.NotNull(result);
        Assert.Equal(1, result.UserId);
        mockRepo.Verify(r => r.AddAsync(It.IsAny<GisDepartmentUserAccessEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GisLayerMasterService Tests

    [Fact]
    public async Task GisLayerMasterService_GetById_ReturnsDto()
    {
        var mockRepo = new Mock<IRepository<GisLayerMasterEntity, int>>();
        var entity = new GisLayerMasterEntity { Id = 1, LayerCode = "TAX_BOUNDARY", LayerName = "Tax Boundary Layer" };
        var dto = new GisLayerMasterDto { Id = 1, LayerCode = "TAX_BOUNDARY", LayerName = "Tax Boundary Layer" };

        mockRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<GisLayerMasterDto>(entity)).Returns(dto);

        var service = new GisLayerMasterService(mockRepo.Object, _mockUnitOfWork.Object, _mockMapper.Object);
        var result = await service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("TAX_BOUNDARY", result!.LayerCode);
    }

    #endregion

    #region GisKpiMasterService Tests

    [Fact]
    public async Task GisKpiMasterService_Create_ReturnsCreatedDto()
    {
        var mockRepo = new Mock<IRepository<GisKpiMasterEntity, int>>();
        var createDto = new CreateGisKpiMasterDto { KpiCode = "TOTAL_PROPERTIES", DefaultTitle = "Total Properties" };
        var entity = new GisKpiMasterEntity { Id = 1, KpiCode = "TOTAL_PROPERTIES", DefaultTitle = "Total Properties" };
        var resultDto = new GisKpiMasterDto { Id = 1, KpiCode = "TOTAL_PROPERTIES", DefaultTitle = "Total Properties" };

        _mockMapper.Setup(m => m.Map<GisKpiMasterEntity>(createDto)).Returns(entity);
        _mockMapper.Setup(m => m.Map<GisKpiMasterDto>(entity)).Returns(resultDto);

        var service = new GisKpiMasterService(mockRepo.Object, _mockUnitOfWork.Object, _mockMapper.Object);
        var result = await service.CreateAsync(createDto);

        Assert.NotNull(result);
        Assert.Equal("TOTAL_PROPERTIES", result.KpiCode);
    }

    #endregion

    #region GisDepartmentKpiMappingService Tests

    [Fact]
    public async Task GisDepartmentKpiMappingService_Create_ReturnsCreatedDto()
    {
        var mockRepo = new Mock<IRepository<GisDepartmentKpiMappingEntity, int>>();
        var createDto = new CreateGisDepartmentKpiMappingDto { DepartmentId = 1, KpiMasterId = 1, CustomTitle = "Tax Properties" };
        var entity = new GisDepartmentKpiMappingEntity { Id = 1, DepartmentId = 1, KpiMasterId = 1, CustomTitle = "Tax Properties" };
        var resultDto = new GisDepartmentKpiMappingDto { Id = 1, DepartmentId = 1, KpiMasterId = 1, CustomTitle = "Tax Properties" };

        _mockMapper.Setup(m => m.Map<GisDepartmentKpiMappingEntity>(createDto)).Returns(entity);
        _mockMapper.Setup(m => m.Map<GisDepartmentKpiMappingDto>(entity)).Returns(resultDto);

        var service = new GisDepartmentKpiMappingService(mockRepo.Object, _mockUnitOfWork.Object, _mockMapper.Object);
        var result = await service.CreateAsync(createDto);

        Assert.NotNull(result);
        Assert.Equal("Tax Properties", result.CustomTitle);
    }

    #endregion

    #region GisFilterMasterService Tests

    [Fact]
    public async Task GisFilterMasterService_Create_ReturnsCreatedDto()
    {
        var mockRepo = new Mock<IRepository<GisFilterMasterEntity, int>>();
        var createDto = new CreateGisFilterMasterDto { FilterKey = "zoneId", FilterLabel = "Select Zone", ControlType = "DROPDOWN" };
        var entity = new GisFilterMasterEntity { Id = 1, FilterKey = "zoneId", FilterLabel = "Select Zone", ControlType = "DROPDOWN" };
        var resultDto = new GisFilterMasterDto { Id = 1, FilterKey = "zoneId", FilterLabel = "Select Zone", ControlType = "DROPDOWN" };

        _mockMapper.Setup(m => m.Map<GisFilterMasterEntity>(createDto)).Returns(entity);
        _mockMapper.Setup(m => m.Map<GisFilterMasterDto>(entity)).Returns(resultDto);

        var service = new GisFilterMasterService(mockRepo.Object, _mockUnitOfWork.Object, _mockMapper.Object);
        var result = await service.CreateAsync(createDto);

        Assert.NotNull(result);
        Assert.Equal("zoneId", result.FilterKey);
    }

    #endregion

    #region GisDepartmentFilterMappingService Tests

    [Fact]
    public async Task GisDepartmentFilterMappingService_Create_ReturnsCreatedDto()
    {
        var mockRepo = new Mock<IRepository<GisDepartmentFilterMappingEntity, int>>();
        var createDto = new CreateGisDepartmentFilterMappingDto { DepartmentId = 1, FilterMasterId = 1, CustomLabel = "Select Tax Zone" };
        var entity = new GisDepartmentFilterMappingEntity { Id = 1, DepartmentId = 1, FilterMasterId = 1, CustomLabel = "Select Tax Zone" };
        var resultDto = new GisDepartmentFilterMappingDto { Id = 1, DepartmentId = 1, FilterMasterId = 1, CustomLabel = "Select Tax Zone" };

        _mockMapper.Setup(m => m.Map<GisDepartmentFilterMappingEntity>(createDto)).Returns(entity);
        _mockMapper.Setup(m => m.Map<GisDepartmentFilterMappingDto>(entity)).Returns(resultDto);

        var service = new GisDepartmentFilterMappingService(mockRepo.Object, _mockUnitOfWork.Object, _mockMapper.Object);
        var result = await service.CreateAsync(createDto);

        Assert.NotNull(result);
        Assert.Equal("Select Tax Zone", result.CustomLabel);
    }

    #endregion

    #region GisUploadHistoryService Tests

    [Fact]
    public async Task GisUploadHistoryService_GetById_ReturnsDto()
    {
        var mockRepo = new Mock<IRepository<GisUploadHistoryEntity, int>>();
        var entity = new GisUploadHistoryEntity { Id = 1, FileName = "Thane_Wards.geojson", RecordCount = 150 };
        var dto = new GisUploadHistoryDto { Id = 1, FileName = "Thane_Wards.geojson", RecordCount = 150 };

        mockRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _mockMapper.Setup(m => m.Map<GisUploadHistoryDto>(entity)).Returns(dto);

        var service = new GisUploadHistoryService(mockRepo.Object, _mockUnitOfWork.Object, _mockMapper.Object);
        var result = await service.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Thane_Wards.geojson", result!.FileName);
    }

    #endregion
}
