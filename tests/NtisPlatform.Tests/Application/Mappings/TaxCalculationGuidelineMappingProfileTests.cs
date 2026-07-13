using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using NtisPlatform.Application.DTOs.Master.TaxCalculationGuideline;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Core.Entities.Master;
using Xunit;

namespace NtisPlatform.Tests.Application.Mappings;

public class TaxCalculationGuidelineMappingProfileTests
{
    private readonly IMapper _mapper;
    private readonly MapperConfiguration _configuration;

    public TaxCalculationGuidelineMappingProfileTests()
    {
        _configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TaxCalculationGuidelineMappingProfile>();
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
        var dto = new CreateTaxCalculationGuidelineDto
        {
            GuidelineCode = "GUIDE_001",
            GuidelineName = "Default Guideline",
            DatePriority1 = "RETROSPECTIVE",
            DatePriority2 = "ELECTRIC_BILL",
            DatePriority3 = "CC",
            DatePriority4 = "OC",
            IgnoreCCToOCIfWithinType = "MONTHS",
            ElectricBillDateRule = "NO_TAX",
            NoDateRule = "DEFAULT_RETROSPECTIVE",
            FloorCertificatePriority = "PROPERTY_OVERRIDES_FLOOR",
            ProrationMethod = "FULL_YEAR",
            TaxPersistenceMode = "PROPERTY_AGGREGATED",
            CreatedBy = 101
        };

        var entity = _mapper.Map<TaxCalculationGuidelineEntity>(dto);

        Assert.Equal(dto.GuidelineCode, entity.GuidelineCode);
        Assert.Equal(dto.GuidelineName, entity.GuidelineName);
        Assert.Equal(dto.CreatedBy, entity.CreatedBy);
        Assert.Equal(dto.TaxPersistenceMode, entity.TaxPersistenceMode);
    }
}
