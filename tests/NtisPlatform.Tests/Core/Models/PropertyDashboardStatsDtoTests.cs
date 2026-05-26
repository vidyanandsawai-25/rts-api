using NtisPlatform.Core.Models;
using Xunit;

namespace NtisPlatform.Tests.Core.Models;

/// <summary>
/// Test class for PropertyDashboardStatsDto to achieve 100% line coverage
/// </summary>
public class PropertyDashboardStatsDtoTests
{
    [Fact]
    public void Constructor_CreatesInstance_WithDefaultValues()
    {
        // Act
        var dto = new PropertyDashboardStatsDto();

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(0, dto.RegisteredPropertyCount);
        Assert.Equal(0, dto.GeoSequencingPropertyCount);
        Assert.Equal(0, dto.SurveyPropertyCount);
        Assert.Equal(0, dto.DataProcessingPropertyCount);
        Assert.Equal(0, dto.QualityAnalysisPropertyCount);
        Assert.Equal(0, dto.AssessmentCompletedPropertyCount);
    }

    [Fact]
    public void RegisteredPropertyCount_CanBeSet_AndRetrieved()
    {
        // Arrange
        var dto = new PropertyDashboardStatsDto();

        // Act
        dto.RegisteredPropertyCount = 100;

        // Assert
        Assert.Equal(100, dto.RegisteredPropertyCount);
    }

    [Fact]
    public void GeoSequencingPropertyCount_CanBeSet_AndRetrieved()
    {
        // Arrange
        var dto = new PropertyDashboardStatsDto();

        // Act
        dto.GeoSequencingPropertyCount = 50;

        // Assert
        Assert.Equal(50, dto.GeoSequencingPropertyCount);
    }

    [Fact]
    public void SurveyPropertyCount_CanBeSet_AndRetrieved()
    {
        // Arrange
        var dto = new PropertyDashboardStatsDto();

        // Act
        dto.SurveyPropertyCount = 30;

        // Assert
        Assert.Equal(30, dto.SurveyPropertyCount);
    }

    [Fact]
    public void DataProcessingPropertyCount_CanBeSet_AndRetrieved()
    {
        // Arrange
        var dto = new PropertyDashboardStatsDto();

        // Act
        dto.DataProcessingPropertyCount = 25;

        // Assert
        Assert.Equal(25, dto.DataProcessingPropertyCount);
    }

    [Fact]
    public void QualityAnalysisPropertyCount_CanBeSet_AndRetrieved()
    {
        // Arrange
        var dto = new PropertyDashboardStatsDto();

        // Act
        dto.QualityAnalysisPropertyCount = 20;

        // Assert
        Assert.Equal(20, dto.QualityAnalysisPropertyCount);
    }

    [Fact]
    public void AssessmentCompletedPropertyCount_CanBeSet_AndRetrieved()
    {
        // Arrange
        var dto = new PropertyDashboardStatsDto();

        // Act
        dto.AssessmentCompletedPropertyCount = 15;

        // Assert
        Assert.Equal(15, dto.AssessmentCompletedPropertyCount);
    }

    [Fact]
    public void AllProperties_CanBeSet_Simultaneously()
    {
        // Arrange & Act
        var dto = new PropertyDashboardStatsDto
        {
            RegisteredPropertyCount = 100,
            GeoSequencingPropertyCount = 50,
            SurveyPropertyCount = 30,
            DataProcessingPropertyCount = 25,
            QualityAnalysisPropertyCount = 20,
            AssessmentCompletedPropertyCount = 15
        };

        // Assert
        Assert.Equal(100, dto.RegisteredPropertyCount);
        Assert.Equal(50, dto.GeoSequencingPropertyCount);
        Assert.Equal(30, dto.SurveyPropertyCount);
        Assert.Equal(25, dto.DataProcessingPropertyCount);
        Assert.Equal(20, dto.QualityAnalysisPropertyCount);
        Assert.Equal(15, dto.AssessmentCompletedPropertyCount);
    }

    [Fact]
    public void AllProperties_CanBeModified_AfterInitialization()
    {
        // Arrange
        var dto = new PropertyDashboardStatsDto
        {
            RegisteredPropertyCount = 100,
            GeoSequencingPropertyCount = 50
        };

        // Act
        dto.RegisteredPropertyCount = 200;
        dto.GeoSequencingPropertyCount = 150;
        dto.SurveyPropertyCount = 60;
        dto.DataProcessingPropertyCount = 50;
        dto.QualityAnalysisPropertyCount = 40;
        dto.AssessmentCompletedPropertyCount = 30;

        // Assert
        Assert.Equal(200, dto.RegisteredPropertyCount);
        Assert.Equal(150, dto.GeoSequencingPropertyCount);
        Assert.Equal(60, dto.SurveyPropertyCount);
        Assert.Equal(50, dto.DataProcessingPropertyCount);
        Assert.Equal(40, dto.QualityAnalysisPropertyCount);
        Assert.Equal(30, dto.AssessmentCompletedPropertyCount);
    }

    [Fact]
    public void Properties_CanBeSetToZero()
    {
        // Arrange
        var dto = new PropertyDashboardStatsDto
        {
            RegisteredPropertyCount = 100,
            GeoSequencingPropertyCount = 50,
            SurveyPropertyCount = 30,
            DataProcessingPropertyCount = 25,
            QualityAnalysisPropertyCount = 20,
            AssessmentCompletedPropertyCount = 15
        };

        // Act
        dto.RegisteredPropertyCount = 0;
        dto.GeoSequencingPropertyCount = 0;
        dto.SurveyPropertyCount = 0;
        dto.DataProcessingPropertyCount = 0;
        dto.QualityAnalysisPropertyCount = 0;
        dto.AssessmentCompletedPropertyCount = 0;

        // Assert
        Assert.Equal(0, dto.RegisteredPropertyCount);
        Assert.Equal(0, dto.GeoSequencingPropertyCount);
        Assert.Equal(0, dto.SurveyPropertyCount);
        Assert.Equal(0, dto.DataProcessingPropertyCount);
        Assert.Equal(0, dto.QualityAnalysisPropertyCount);
        Assert.Equal(0, dto.AssessmentCompletedPropertyCount);
    }

    [Fact]
    public void Properties_CanHandleLargeValues()
    {
        // Arrange & Act
        var dto = new PropertyDashboardStatsDto
        {
            RegisteredPropertyCount = int.MaxValue,
            GeoSequencingPropertyCount = int.MaxValue - 1,
            SurveyPropertyCount = int.MaxValue - 2,
            DataProcessingPropertyCount = int.MaxValue - 3,
            QualityAnalysisPropertyCount = int.MaxValue - 4,
            AssessmentCompletedPropertyCount = int.MaxValue - 5
        };

        // Assert
        Assert.Equal(int.MaxValue, dto.RegisteredPropertyCount);
        Assert.Equal(int.MaxValue - 1, dto.GeoSequencingPropertyCount);
        Assert.Equal(int.MaxValue - 2, dto.SurveyPropertyCount);
        Assert.Equal(int.MaxValue - 3, dto.DataProcessingPropertyCount);
        Assert.Equal(int.MaxValue - 4, dto.QualityAnalysisPropertyCount);
        Assert.Equal(int.MaxValue - 5, dto.AssessmentCompletedPropertyCount);
    }

    [Fact]
    public void Properties_CanHandleNegativeValues()
    {
        // Arrange & Act
        var dto = new PropertyDashboardStatsDto
        {
            RegisteredPropertyCount = -1,
            GeoSequencingPropertyCount = -2,
            SurveyPropertyCount = -3,
            DataProcessingPropertyCount = -4,
            QualityAnalysisPropertyCount = -5,
            AssessmentCompletedPropertyCount = -6
        };

        // Assert
        Assert.Equal(-1, dto.RegisteredPropertyCount);
        Assert.Equal(-2, dto.GeoSequencingPropertyCount);
        Assert.Equal(-3, dto.SurveyPropertyCount);
        Assert.Equal(-4, dto.DataProcessingPropertyCount);
        Assert.Equal(-5, dto.QualityAnalysisPropertyCount);
        Assert.Equal(-6, dto.AssessmentCompletedPropertyCount);
    }

    [Fact]
    public void TotalPropertyCount_CanBeCalculated()
    {
        // Arrange
        var dto = new PropertyDashboardStatsDto
        {
            RegisteredPropertyCount = 100,
            GeoSequencingPropertyCount = 50,
            SurveyPropertyCount = 30,
            DataProcessingPropertyCount = 25,
            QualityAnalysisPropertyCount = 20,
            AssessmentCompletedPropertyCount = 15
        };

        // Act
        var total = dto.RegisteredPropertyCount + dto.GeoSequencingPropertyCount +
                   dto.SurveyPropertyCount + dto.DataProcessingPropertyCount +
                   dto.QualityAnalysisPropertyCount + dto.AssessmentCompletedPropertyCount;

        // Assert
        Assert.Equal(240, total);
    }

    [Fact]
    public void RegisteredPropertyCount_AndGeoSequencingPropertyCount_CanBeSame()
    {
        // Arrange & Act
        var dto = new PropertyDashboardStatsDto
        {
            RegisteredPropertyCount = 100,
            GeoSequencingPropertyCount = 100
        };

        // Assert
        Assert.Equal(dto.RegisteredPropertyCount, dto.GeoSequencingPropertyCount);
    }
}
