using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Tests.Core.Entities;

/// <summary>
/// Unit tests for PropertyAssessmentEntity to ensure 100% code coverage
/// </summary>
public class PropertyAssessmentEntityTests
{
    [Fact]
    public void PropertyAssessmentEntity_AllProperties_GetSet_WorksCorrectly()
    {
        var now = DateTime.Now;
        var entity = new PropertyAssessmentEntity
        {
            Id = 1,
            PropertyId = 549357,
            OwnerTypeId = 2,
            AssessmentRemark = "Assessment remark",
            SurveyRemark = "Survey remark",
            FlatSystemRemark = "Flat system remark",
            CombPropRemark = "Combined property remark",
            AdharCardNo = "123456789012",
            RenterMobileNo = "8765432109",
            PrarupYadiPublishDate = now,
            AntimYadiPublishDate = now.AddDays(30),
            PropertyRegDate = now.AddDays(-365),
            ApplyTaxesFrom = 2023,
            PartOCDate = now.AddDays(-180),
            BHK = "3BHK",
            BlockNo = "B01",
            WingNo = "A",
            AlternativeEmailId = "alt@example.com",
            TotalBuiltupAreaSqFeet = 1500.0,
            TotalBuiltupAreaSqMeter = 139.35,
            Latitude = "18.5204",
            Longitude = "73.8567",
            NoOfResidentialToilets = 2,
            NoOfCommercialToilets = 1,
            MarkedForDeletion = false,
            MarkedForDeletionDate = null,
            IsActive = true,
            CreatedBy = 1,
            CreatedDate = now,
            UpdatedBy = 2,
            UpdatedDate = now
        };

        Assert.Equal(1, entity.Id);
        Assert.Equal(549357, entity.PropertyId);
        Assert.Equal(2, entity.OwnerTypeId);
        Assert.Equal("Assessment remark", entity.AssessmentRemark);
        Assert.Equal("Survey remark", entity.SurveyRemark);
        Assert.Equal("Flat system remark", entity.FlatSystemRemark);
        Assert.Equal("Combined property remark", entity.CombPropRemark);
        Assert.Equal("123456789012", entity.AdharCardNo);
        Assert.Equal("8765432109", entity.RenterMobileNo);
        Assert.Equal(now, entity.PrarupYadiPublishDate);
        Assert.Equal(now.AddDays(30), entity.AntimYadiPublishDate);
        Assert.Equal(now.AddDays(-365), entity.PropertyRegDate);
        Assert.Equal((short)2023, entity.ApplyTaxesFrom);
        Assert.Equal(now.AddDays(-180), entity.PartOCDate);
        Assert.Equal("3BHK", entity.BHK);
        Assert.Equal("B01", entity.BlockNo);
        Assert.Equal("A", entity.WingNo);
        Assert.Equal("alt@example.com", entity.AlternativeEmailId);
        Assert.Equal(1500.0, entity.TotalBuiltupAreaSqFeet);
        Assert.Equal(139.35, entity.TotalBuiltupAreaSqMeter);
        Assert.Equal("18.5204", entity.Latitude);
        Assert.Equal("73.8567", entity.Longitude);
        Assert.Equal(2, entity.NoOfResidentialToilets);
        Assert.Equal(1, entity.NoOfCommercialToilets);
        Assert.False(entity.MarkedForDeletion);
        Assert.Null(entity.MarkedForDeletionDate);
        Assert.True(entity.IsActive);
        Assert.Equal(1, entity.CreatedBy);
        Assert.Equal(2, entity.UpdatedBy);
    }

    [Fact]
    public void PropertyAssessmentEntity_OptionalFields_CanBeNull()
    {
        var entity = new PropertyAssessmentEntity
        {
            Id = 1,
            PropertyId = 549357,
            IsActive = true,
            MarkedForDeletion = false
        };

        Assert.Null(entity.OwnerTypeId);
        Assert.Null(entity.AssessmentRemark);
        Assert.Null(entity.SurveyRemark);
        Assert.Null(entity.FlatSystemRemark);
        Assert.Null(entity.CombPropRemark);
        Assert.Null(entity.AdharCardNo);
        Assert.Null(entity.RenterMobileNo);
        Assert.Null(entity.PrarupYadiPublishDate);
        Assert.Null(entity.AntimYadiPublishDate);
        Assert.Null(entity.PropertyRegDate);
        Assert.Null(entity.ApplyTaxesFrom);
        Assert.Null(entity.PartOCDate);
        Assert.Null(entity.BHK);
        Assert.Null(entity.BlockNo);
        Assert.Null(entity.WingNo);
        Assert.Null(entity.AlternativeEmailId);
        Assert.Null(entity.TotalBuiltupAreaSqFeet);
        Assert.Null(entity.TotalBuiltupAreaSqMeter);
        Assert.Null(entity.Latitude);
        Assert.Null(entity.Longitude);
        Assert.Null(entity.NoOfResidentialToilets);
        Assert.Null(entity.NoOfCommercialToilets);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void PropertyAssessmentEntity_InheritsFromBaseEntity()
    {
        var entity = new PropertyAssessmentEntity();
        Assert.IsAssignableFrom<BaseEntity>(entity);
    }

    [Fact]
    public void PropertyAssessmentEntity_DefaultValues_SetCorrectly()
    {
        var entity = new PropertyAssessmentEntity();

        Assert.Equal(0, entity.Id);
        Assert.Equal(0, entity.PropertyId);
        Assert.False(entity.MarkedForDeletion);
        Assert.True(entity.IsActive);
        Assert.Null(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void PropertyAssessmentEntity_WingNo_GetSet_WorksCorrectly()
    {
        var entity = new PropertyAssessmentEntity
        {
            WingNo = "West Wing A"
        };

        Assert.Equal("West Wing A", entity.WingNo);
    }

    [Fact]
    public void PropertyAssessmentEntity_WingNo_CanBeNull()
    {
        var entity = new PropertyAssessmentEntity
        {
            WingNo = null
        };

        Assert.Null(entity.WingNo);
    }

    [Fact]
    public void PropertyAssessmentEntity_WingNo_CanBeEmptyString()
    {
        var entity = new PropertyAssessmentEntity
        {
            WingNo = string.Empty
        };

        Assert.Equal(string.Empty, entity.WingNo);
    }

    [Fact]
    public void PropertyAssessmentEntity_MarkedForDeletion_GetSet_WorksCorrectly()
    {
        var entity = new PropertyAssessmentEntity
        {
            MarkedForDeletion = true,
            MarkedForDeletionDate = DateTime.Now
        };

        Assert.True(entity.MarkedForDeletion);
        Assert.NotNull(entity.MarkedForDeletionDate);
    }

    [Fact]
    public void PropertyAssessmentEntity_MarkedForDeletionDate_GetSet_WorksCorrectly()
    {
        var now = DateTime.Now;
        var entity = new PropertyAssessmentEntity
        {
            MarkedForDeletion = true,
            MarkedForDeletionDate = now
        };

        Assert.Equal(now, entity.MarkedForDeletionDate);
    }

    [Fact]
    public void PropertyAssessmentEntity_AllNumericFields_GetSet_WorksCorrectly()
    {
        var entity = new PropertyAssessmentEntity
        {
            NoOfResidentialToilets = 5,
            NoOfCommercialToilets = 3,
            TotalBuiltupAreaSqFeet = 2500.75,
            TotalBuiltupAreaSqMeter = 232.26,
            ApplyTaxesFrom = 2024
        };

        Assert.Equal(5, entity.NoOfResidentialToilets);
        Assert.Equal(3, entity.NoOfCommercialToilets);
        Assert.Equal(2500.75, entity.TotalBuiltupAreaSqFeet);
        Assert.Equal(232.26, entity.TotalBuiltupAreaSqMeter);
        Assert.Equal((short)2024, entity.ApplyTaxesFrom);
    }

    [Fact]
    public void PropertyAssessmentEntity_AllStringFields_MaxLength_WorksCorrectly()
    {
        var entity = new PropertyAssessmentEntity
        {
            AssessmentRemark = new string('A', 400),
            SurveyRemark = new string('B', 400),
            FlatSystemRemark = new string('C', 400),
            CombPropRemark = new string('D', 400),
            AdharCardNo = new string('1', 12),
            RenterMobileNo = new string('2', 13),
            BHK = new string('4', 50),
            BlockNo = new string('5', 20),
            WingNo = new string('W', 50),
            AlternativeEmailId = new string('a', 100),
            Latitude = new string('7', 20),
            Longitude = new string('8', 20)
        };

        Assert.Equal(400, entity.AssessmentRemark!.Length);
        Assert.Equal(400, entity.SurveyRemark!.Length);
        Assert.Equal(400, entity.FlatSystemRemark!.Length);
        Assert.Equal(400, entity.CombPropRemark!.Length);
        Assert.Equal(12, entity.AdharCardNo!.Length);
        Assert.Equal(13, entity.RenterMobileNo!.Length);
        Assert.Equal(50, entity.BHK!.Length);
        Assert.Equal(20, entity.BlockNo!.Length);
        Assert.Equal(50, entity.WingNo!.Length);
        Assert.Equal(100, entity.AlternativeEmailId!.Length);
        Assert.Equal(20, entity.Latitude!.Length);
        Assert.Equal(20, entity.Longitude!.Length);
    }

    [Fact]
    public void PropertyAssessmentEntity_DateFields_GetSet_WorksCorrectly()
    {
        var date1 = new DateTime(2023, 1, 15);
        var date2 = new DateTime(2023, 2, 20);
        var date3 = new DateTime(2023, 3, 25);
        var date4 = new DateTime(2023, 4, 30);

        var entity = new PropertyAssessmentEntity
        {
            PrarupYadiPublishDate = date1,
            AntimYadiPublishDate = date2,
            PropertyRegDate = date3,
            PartOCDate = date4
        };

        Assert.Equal(date1, entity.PrarupYadiPublishDate);
        Assert.Equal(date2, entity.AntimYadiPublishDate);
        Assert.Equal(date3, entity.PropertyRegDate);
        Assert.Equal(date4, entity.PartOCDate);
    }

    [Fact]
    public void PropertyAssessmentEntity_NavigationProperty_PropertyMast_CanBeSet()
    {
        var assessmentEntity = new PropertyAssessmentEntity
        {
            Id = 1,
            PropertyId = 549357
        };

        var propertyEntity = new PropertyEntity
        {
            Id = 549357,
            TaxZoneId = 1,
            WardId = 5
        };

        assessmentEntity.PropertyMast = propertyEntity;

        Assert.NotNull(assessmentEntity.PropertyMast);
        Assert.Equal(549357, assessmentEntity.PropertyMast.Id);
        Assert.Equal(1, assessmentEntity.PropertyMast.TaxZoneId);
        Assert.Equal(5, assessmentEntity.PropertyMast.WardId);
    }

    [Fact]
    public void PropertyAssessmentEntity_NavigationProperty_PropertyMast_CanBeNull()
    {
        var assessmentEntity = new PropertyAssessmentEntity
        {
            Id = 1,
            PropertyId = 549357,
            PropertyMast = null
        };

        Assert.Null(assessmentEntity.PropertyMast);
    }
}
