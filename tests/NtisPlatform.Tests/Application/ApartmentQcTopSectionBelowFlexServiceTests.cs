using MockQueryable;
using Moq;
using NtisPlatform.Application.DTOs.Property.ApartmentQcTopSection;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using Xunit;

namespace NtisPlatform.Tests.Application;

public class ApartmentQcTopSectionBelowFlexServiceTests
{
    private readonly Mock<IApartmentQcTopSectionRepository> _topSectionRepository = new();
    private readonly Mock<IApartmentQcTopSectionBelowFlexRepository> _repository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly ApartmentQcTopSectionBelowFlexService _service;

    public ApartmentQcTopSectionBelowFlexServiceTests()
    {
        _service = new ApartmentQcTopSectionBelowFlexService(_topSectionRepository.Object, _repository.Object, _userRepository.Object);
    }

    [Fact]
    public async Task GetBelowFlexAsync_PropertyNotFound_ReturnsNull()
    {
        _topSectionRepository.Setup(r => r.GetPropertyAsync(It.IsAny<ApartmentQcTopSectionQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PropertyTopSectionRawData?)null);

        var result = await _service.GetBelowFlexAsync(new ApartmentQcTopSectionQueryParameters { PropertyId = 999 });

        Assert.Null(result);
        _repository.Verify(r => r.GetWorkflowStagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetBelowFlexAsync_ResolvesCreatedByAndUpdatedByNames()
    {
        _topSectionRepository.Setup(r => r.GetPropertyAsync(It.IsAny<ApartmentQcTopSectionQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyTopSectionRawData { Id = 5 });

        _repository.Setup(r => r.GetWorkflowStagesAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkflowStageStatusDto>
            {
                new() { StageId = 1, StageName = "GIS Verification", IsCompleted = true, CreatedBy = 10, UpdatedBy = 11 }
            });
        _repository.Setup(r => r.GetCertificateTypesAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CertificateTypeStatusDto>
            {
                new() { CertificateTypeId = 1, CertificateTypeCode = "CC", CertificateTypeName = "Completion Certificate", IsIssued = true, CreatedBy = 10 }
            });

        var users = new List<UserEntity>
        {
            new() { Id = 10, FirstName = "Amit", LastName = "Shah" },
            new() { Id = 11, FirstName = "Priya", MiddleName = "R", LastName = "Nair" }
        };
        _userRepository.Setup(r => r.GetQueryable()).Returns(users.BuildMock());

        var result = await _service.GetBelowFlexAsync(new ApartmentQcTopSectionQueryParameters { PropertyId = 5 });

        Assert.NotNull(result);
        Assert.Equal(5, result!.PropertyId);

        var stage = Assert.Single(result.WorkflowStages);
        Assert.Equal("Amit Shah", stage.CreatedByName);
        Assert.Equal("Priya R Nair", stage.UpdatedByName);

        var cert = Assert.Single(result.CertificateTypes);
        Assert.Equal("Amit Shah", cert.CreatedByName);
        Assert.Null(cert.UpdatedByName);
    }

    [Fact]
    public async Task GetBelowFlexAsync_NoAuditIds_SkipsUserLookup()
    {
        _topSectionRepository.Setup(r => r.GetPropertyAsync(It.IsAny<ApartmentQcTopSectionQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyTopSectionRawData { Id = 8 });
        _repository.Setup(r => r.GetWorkflowStagesAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkflowStageStatusDto> { new() { StageId = 1, StageName = "Assessment", IsCompleted = false } });
        _repository.Setup(r => r.GetCertificateTypesAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CertificateTypeStatusDto>());

        var result = await _service.GetBelowFlexAsync(new ApartmentQcTopSectionQueryParameters { PropertyId = 8 });

        Assert.NotNull(result);
        _userRepository.Verify(r => r.GetQueryable(), Times.Never);
    }
}
