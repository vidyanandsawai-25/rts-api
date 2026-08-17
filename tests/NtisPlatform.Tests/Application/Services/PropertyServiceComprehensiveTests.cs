using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Options;
using NtisPlatform.Application.Services;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Application.Services;

/// <summary>
/// Comprehensive tests for PropertyService to achieve 100% code coverage
/// </summary>
public class PropertyServiceComprehensiveTests
{
    private readonly Mock<IRepository<PropertyEntity, int>> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IPropertyRepository> _mockPropertyRepository;
    private readonly PropertyService _service;
    private readonly Mock<ILogger<PropertyService>> _mockLogger;
    private readonly Mock<IOptions<FeatureFlagsOptions>> _mockFeatureFlags;

    public PropertyServiceComprehensiveTests()
    {
        _mockRepository = new Mock<IRepository<PropertyEntity, int>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockPropertyRepository = new Mock<IPropertyRepository>();
        _mockLogger = new Mock<ILogger<PropertyService>>();
        _mockFeatureFlags = new Mock<IOptions<FeatureFlagsOptions>>();

        // Setup feature flag to allow property deletion without payment validation
        _mockFeatureFlags.Setup(f => f.Value).Returns(new FeatureFlagsOptions
        {
            AllowPropertyDeletionWithoutPaymentValidation = true
        });

        _service = new PropertyService(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockPropertyRepository.Object,
            _mockLogger.Object,
            _mockFeatureFlags.Object, new Mock<IRepository<NtisPlatform.Core.Entities.WardEntity, int>>().Object, new Mock<IRepository<NtisPlatform.Core.Entities.PropertyCategoryEntity, int>>().Object, new Mock<IRepository<NtisPlatform.Core.Entities.SocietyDetailsEntity, int>>().Object, new Mock<IRepository<NtisPlatform.Core.Entities.PropertyDetailsEntity, int>>().Object, new Mock<IRepository<NtisPlatform.Core.Entities.RoomWiseSubmissionDetailsEntity, int>>().Object, new Mock<IRepository<NtisPlatform.Core.Entities.PropertyAssessmentEntity, int>>().Object, new Mock<IRepository<GlobalSurveyWardAllocationEntity, int>>().Object, new Mock<IRepository<OldWardMasterEntity, int>>().Object, new Mock<IRepository<PropertyMapMasterEntity, int>>().Object, new Mock<IRepository<PropertyMapDetailEntity, int>>().Object, new Mock<IRepository<WingEntity, int>>().Object, new Mock<IRepository<UserEntity, int>>().Object, new Mock<IRepository<PropertyMastOldEntity, int>>().Object, new Mock<IRepository<PropertyTypeMasterEntity, int>>().Object,
            new Mock<IRepository<CommunicationDetailsEntity, int>>().Object,
            new Mock<IRepository<PropertyPhotoEntity, int>>().Object,
            new Mock<IRepository<DocumentBindingEntity, int>>().Object,
            new Mock<IRepository<DocumentEntity, int>>().Object,
            new Mock<IRepository<PropertyPhotoTypeEntity, int>>().Object,
            new Mock<IRepository<OwnerTypeMasterEntity, int>>().Object,
            new Mock<IRepository<WingEntity, int>>().Object,
            new Mock<NtisPlatform.Application.Interfaces.Rules.IPropertyRuleApplicationLogService>().Object);
    }


    // Society, KYC and Basic Details flows now live with their per-tab services
    // (PropertySocietyService / PropertyKycService / PropertyBasicDetailsService and their dedicated tests).
}



