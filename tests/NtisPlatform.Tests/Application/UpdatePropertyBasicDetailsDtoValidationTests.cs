using System.ComponentModel.DataAnnotations;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Tests.Application;

/// <summary>
/// Comprehensive validation tests for UpdatePropertyBasicDetailsDto
/// Covers all validation attributes and edge cases
/// </summary>
public class UpdatePropertyBasicDetailsDtoValidationTests
{
    private static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_ValidData_PassesValidation()
    {
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 10,
            CategoryId = 1,
            PropertyTypeId = 2,
            PartitionNo = "A1",
            FlatOrShopNo = "101",
            PlotNo = "P123",
            SurveyNo = "S456",
            UPICId = "UPIC123",
            SubZoneNo = "SZ01"
        };

        var results = Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_WardIdZero_FailsValidation()
    {
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 0,
            TaxZoneId = 10
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("WardId must be greater than 0"));
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_TaxZoneIdZero_FailsValidation()
    {
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 0
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("TaxZoneId must be greater than 0"));
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_CategoryIdZero_FailsValidation()
    {
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 10,
            CategoryId = 0
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("CategoryId must be greater than 0"));
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_PropertyTypeIdZero_FailsValidation()
    {
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 10,
            PropertyTypeId = 0
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("PropertyTypeId must be greater than 0"));
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_PartitionNoTooLong_FailsValidation()
    {
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 10,
            PartitionNo = new string('A', 11) // Max 10
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("PartitionNo cannot exceed 10 characters"));
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_FlatOrShopNoTooLong_FailsValidation()
    {
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 10,
            FlatOrShopNo = new string('1', 51) // Max 50
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("FlatOrShopNo cannot exceed 50 characters"));
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_PlotNoTooLong_FailsValidation()
    {
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 10,
            PlotNo = new string('P', 21) // Max 20
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("PlotNo cannot exceed 20 characters"));
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_SurveyNoTooLong_FailsValidation()
    {
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 10,
            SurveyNo = new string('S', 31) // Max 30
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("SurveyNo cannot exceed 30 characters"));
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_UPICIdTooLong_FailsValidation()
    {
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 10,
            UPICId = new string('U', 31) // Max 30
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("UPICId cannot exceed 30 characters"));
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_SubZoneNoTooLong_FailsValidation()
    {
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 10,
            SubZoneNo = new string('Z', 21) // Max 20
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("SubZoneNo cannot exceed 20 characters"));
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_WingNoTooLong_FailsValidation()
    {
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 10,
            WingNo = new string('W', 51) // Max 50
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("WingNo cannot exceed 50 characters"));
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_WingNameTooLong_FailsValidation()
    {
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 10,
            WingName = new string('N', 101) // Max 100
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("WingName cannot exceed 100 characters"));
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_LatitudeTooLong_FailsValidation()
    {
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 10,
            Latitude = new string('L', 21) // Max 20
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Latitude cannot exceed 20 characters"));
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_LongitudeTooLong_FailsValidation()
    {
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 10,
            Longitude = new string('L', 21) // Max 20
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Longitude cannot exceed 20 characters"));
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_NoOfResidentialToiletsNegative_FailsValidation()
    {
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 10,
            NoOfResidentialToilets = -1
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("NoOfResidentialToilets cannot be negative"));
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_NoOfCommercialToiletsNegative_FailsValidation()
    {
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 10,
            NoOfCommercialToilets = -1
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("NoOfCommercialToilets cannot be negative"));
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_PlotAreaNegative_FailsValidation()
    {
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 10,
            PlotArea = -100.0
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("PlotArea cannot be negative"));
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_PlotAreaFtLengthNegative_FailsValidation()
    {
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 10,
            PlotAreaFtLength = -50.0
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("PlotAreaFtLength cannot be negative"));
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_PlotAreaFtWidthNegative_FailsValidation()
    {
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 10,
            PlotAreaFtWidth = -30.0
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("PlotAreaFtWidth cannot be negative"));
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_PlotAreaMtrLengthNegative_FailsValidation()
    {
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 10,
            PlotAreaMtrLength = -15.0
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("PlotAreaMtrLength cannot be negative"));
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_PlotAreaMtrWidthNegative_FailsValidation()
    {
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 10,
            PlotAreaMtrWidth = -9.0
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("PlotAreaMtrWidth cannot be negative"));
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_WingIdZero_FailsValidation()
    {
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 10,
            WingId = 0
        };

        var results = Validate(dto);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("WingId must be greater than 0"));
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_AllNumericFieldsPositive_PassesValidation()
    {
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 10,
            CategoryId = 1,
            PropertyTypeId = 2,
            WingId = 5,
            NoOfResidentialToilets = 2,
            NoOfCommercialToilets = 1,
            PlotArea = 1500.50,
            PlotAreaFtLength = 50.0,
            PlotAreaFtWidth = 30.0,
            PlotAreaMtrLength = 15.24,
            PlotAreaMtrWidth = 9.14
        };

        var results = Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_AllStringFieldsMaxLength_PassesValidation()
    {
        var dto = new UpdatePropertyBasicDetailsDto
        {
            WardId = 79,
            TaxZoneId = 10,
            PartitionNo = new string('A', 10),
            FlatOrShopNo = new string('1', 50),
            PlotNo = new string('P', 20),
            SurveyNo = new string('S', 30),
            UPICId = new string('U', 30),
            SubZoneNo = new string('Z', 20),
            WingNo = new string('W', 50),
            WingName = new string('N', 100)
        };

        var results = Validate(dto);
        Assert.Empty(results);
    }

    [Fact]
    public void UpdatePropertyBasicDetailsDto_DefaultConstructor_AllNullablesAreNull()
    {
        var dto = new UpdatePropertyBasicDetailsDto();

        Assert.Null(dto.CategoryId);
        Assert.Null(dto.PropertyTypeId);
        Assert.Null(dto.PartitionNo);
        Assert.Null(dto.FlatOrShopNo);
        Assert.Null(dto.PlotNo);
        Assert.Null(dto.SurveyNo);
        Assert.Null(dto.UPICId);
        Assert.Null(dto.SubZoneNo);
        Assert.Null(dto.WingNo);
        Assert.Null(dto.NoOfResidentialToilets);
        Assert.Null(dto.NoOfCommercialToilets);
        Assert.Null(dto.PlotArea);
        Assert.Null(dto.PlotAreaFtLength);
        Assert.Null(dto.PlotAreaFtWidth);
        Assert.Null(dto.PlotAreaMtrLength);
        Assert.Null(dto.PlotAreaMtrWidth);
        Assert.Null(dto.WingId);
        Assert.Null(dto.WingName);
    }
}
