using NtisPlatform.Application.DTOs.Master.CertificateTaxGuideline;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs;

public class CertificateTaxGuidelineDtoTests
{
    [Fact]
    public void CreateDto_WithValidData_PassesValidation()
    {
        var dto = new CreateCertificateTaxGuidelineDto
        {
            GuidelineCode = "GUIDE_001",
            GuidelineName = "Default Guideline",
            DataType = "VARCHAR",
            GuidelineValue = "Some Value"
        };

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void CreateDto_WithInvalidDataType_FailsValidation()
    {
        var dto = new CreateCertificateTaxGuidelineDto
        {
            GuidelineCode = "GUIDE_001",
            GuidelineName = "Default Guideline",
            DataType = "INVALID"
        };

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateCertificateTaxGuidelineDto.DataType)));
    }

    [Fact]
    public void CreateDto_WithDateDataType_FailsValidation()
    {
        var dto = new CreateCertificateTaxGuidelineDto
        {
            GuidelineCode = "GUIDE_001",
            GuidelineName = "Default Guideline",
            DataType = "DATE"
        };

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateCertificateTaxGuidelineDto.DataType)));
    }

    [Fact]
    public void UpdateDto_WithDateDataType_FailsValidation()
    {
        var dto = new UpdateCertificateTaxGuidelineDto
        {
            GuidelineCode = "GUIDE_001",
            GuidelineName = "Default Guideline",
            DataType = "DATE"
        };

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(UpdateCertificateTaxGuidelineDto.DataType)));
    }
}
