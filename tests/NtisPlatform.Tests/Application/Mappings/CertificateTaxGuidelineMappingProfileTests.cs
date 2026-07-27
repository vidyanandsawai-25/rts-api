using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using NtisPlatform.Application.DTOs.Master.CertificateTaxGuideline;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Core.Entities.Master;
using Xunit;

namespace NtisPlatform.Tests.Application.Mappings;

public class CertificateTaxGuidelineMappingProfileTests
{
    private readonly IMapper _mapper;
    private readonly MapperConfiguration _configuration;

    public CertificateTaxGuidelineMappingProfileTests()
    {
        _configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<CertificateTaxGuidelineMappingProfile>();
        }, NullLoggerFactory.Instance);

        _mapper = _configuration.CreateMapper();
    }

    [Fact]
    public void Configuration_IsValid()
    {
        _configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_CreateDto_To_Entity_Works()
    {
        var dto = new CreateCertificateTaxGuidelineDto
        {
            GuidelineCode = "GUIDE_001",
            GuidelineName = "Default Guideline",
            GuidelineGroup = "General",
            DisplayOrder = 1,
            DataType = "VARCHAR",
            GuidelineValue = "Some Value",
            AllowedValues = "Some Value, Other Value",
            CreatedBy = 101
        };

        var entity = _mapper.Map<CertificateTaxGuidelineEntity>(dto);

        Assert.Equal(dto.GuidelineCode, entity.GuidelineCode);
        Assert.Equal(dto.GuidelineName, entity.GuidelineName);
        Assert.Equal(dto.GuidelineGroup, entity.GuidelineGroup);
        Assert.Equal(dto.DisplayOrder, entity.DisplayOrder);
        Assert.Equal(dto.DataType, entity.DataType);
        Assert.Equal(dto.GuidelineValue, entity.GuidelineValue);
        Assert.Equal(dto.AllowedValues, entity.AllowedValues);
        Assert.Equal(dto.CreatedBy, entity.CreatedBy);
    }
}
