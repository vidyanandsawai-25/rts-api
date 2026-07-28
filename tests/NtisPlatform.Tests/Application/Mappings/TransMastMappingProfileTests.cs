using AutoMapper;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Mappings;
using NtisPlatform.Core.Entities;
using Xunit;

namespace NtisPlatform.Tests.Application.Mappings;

/// <summary>
/// Regression coverage for TransMastEntity/TransMastDto CalculationType/CalculationValue mapping.
/// Both sides now share these names (main's "result table changes" merge renamed the entity's
/// former RVorCV/RVorCVValue columns to match), so this locks in that the explicit ForMember
/// wiring keeps working as a same-named passthrough.
/// </summary>
public class TransMastMappingProfileTests
{
    private readonly IMapper _mapper;
    private readonly MapperConfiguration _configuration;

    public TransMastMappingProfileTests()
    {
        _configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TransMastMappingProfile>();
        }, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);

        _mapper = _configuration.CreateMapper();
    }

    [Fact]
    public void MappingProfile_Configuration_IsValid()
    {
        _configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void EntityToDto_MapsCalculationFields()
    {
        var entity = new TransMastEntity
        {
            Id = 1,
            PropertyId = 5,
            FinanceYearId = 10,
            TaxId = 2,
            CalculationType = "RV",
            CalculationValue = 12345.67m,
            TaxAmount = 500m
        };

        var dto = _mapper.Map<TransMastDto>(entity);

        Assert.Equal("RV", dto.CalculationType);
        Assert.Equal(12345.67m, dto.CalculationValue);
    }

    [Fact]
    public void CreateDtoToEntity_MapsCalculationFields()
    {
        var createDto = new CreateTransMastDto
        {
            PropertyId = 5,
            FinanceYearId = 10,
            TaxId = 2,
            CalculationType = "CV",
            CalculationValue = 999.99m,
            TaxAmount = 250m
        };

        var entity = _mapper.Map<TransMastEntity>(createDto);

        Assert.Equal("CV", entity.CalculationType);
        Assert.Equal(999.99m, entity.CalculationValue);
    }

    [Fact]
    public void UpdateDtoToEntity_MapsCalculationFields()
    {
        var updateDto = new UpdateTransMastDto
        {
            CalculationType = "RV",
            CalculationValue = 4321m,
            TaxAmount = 100m
        };

        var entity = new TransMastEntity
        {
            PropertyId = 5,
            FinanceYearId = 10,
            TaxId = 2,
            CalculationType = "CV",
            CalculationValue = 0m
        };

        _mapper.Map(updateDto, entity);

        Assert.Equal("RV", entity.CalculationType);
        Assert.Equal(4321m, entity.CalculationValue);
    }
}
