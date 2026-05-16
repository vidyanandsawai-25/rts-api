using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Infrastructure.Data;
using NtisPlatform.Infrastructure.Services;
using Xunit;

namespace NtisPlatform.Tests.Infrastructure.Services;

/// <summary>
/// Comprehensive tests for ReferenceValidationConfiguration to achieve 100% line and branch coverage
/// </summary>
public class ReferenceValidationConfigurationTests
{
    #region ForEntity Tests

    [Fact]
    public void ForEntity_ReturnsReferenceValidatorBuilder()
    {
        // Arrange
        var config = new ReferenceValidationConfiguration();

        // Act
        var builder = config.ForEntity<AssessmentYearRangeCVEntity>();

        // Assert
        Assert.NotNull(builder);
        Assert.IsType<ReferenceValidatorBuilder<AssessmentYearRangeCVEntity>>(builder);
    }

    [Fact]
    public void ForEntity_MultipleEntities_ReturnsCorrectBuilders()
    {
        // Arrange
        var config = new ReferenceValidationConfiguration();

        // Act
        var builder1 = config.ForEntity<AssessmentYearRangeCVEntity>();
        var builder2 = config.ForEntity<SubFloorEntity>();

        // Assert
        Assert.NotNull(builder1);
        Assert.NotNull(builder2);
        Assert.IsType<ReferenceValidatorBuilder<AssessmentYearRangeCVEntity>>(builder1);
        Assert.IsType<ReferenceValidatorBuilder<SubFloorEntity>>(builder2);
    }

    #endregion

    #region CheckReferences Tests

    [Fact]
    public void CheckReferences_WithSingleReference_AddsCheck()
    {
        // Arrange
        var config = new ReferenceValidationConfiguration();
        var builder = config.ForEntity<AssessmentYearRangeCVEntity>();

        // Act
        builder.CheckReferences(
            ("Age Factor CV Master", (ctx, id) => ctx.AgeFactorCVMasters.Where(a => a.YearRangeCVId == id).Cast<object>())
        );

        var result = builder.Build();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Age Factor CV Master", result[0].TableName);
    }

    [Fact]
    public void CheckReferences_WithMultipleReferences_AddsAllChecks()
    {
        // Arrange
        var config = new ReferenceValidationConfiguration();
        var builder = config.ForEntity<AssessmentYearRangeCVEntity>();

        // Act
        builder.CheckReferences(
            ("Age Factor CV Master", (ctx, id) => ctx.AgeFactorCVMasters.Where(a => a.YearRangeCVId == id).Cast<object>()),
            ("Floor Factor CV Master", (ctx, id) => ctx.FloorFactorCVMasters.Where(f => f.YearRangeCVId == id).Cast<object>()),
            ("Nature Factor CV Master", (ctx, id) => ctx.NatureFactorCVMasters.Where(n => n.YearRangeCVId == id).Cast<object>())
        );

        var result = builder.Build();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("Age Factor CV Master", result[0].TableName);
        Assert.Equal("Floor Factor CV Master", result[1].TableName);
        Assert.Equal("Nature Factor CV Master", result[2].TableName);
    }

    [Fact]
    public void CheckReferences_Chaining_AddsChecksIncrementally()
    {
        // Arrange
        var config = new ReferenceValidationConfiguration();
        var builder = config.ForEntity<SubFloorEntity>();

        // Act
        builder.CheckReferences(
            ("Property Details", (ctx, id) => ctx.PropertyDetails.Where(p => p.SubFloorId == id).Cast<object>())
        ).CheckReferences(
            ("Property Details Reassessment", (ctx, id) => ctx.PropertyDetailsReassessment.Where(r => r.SubFloorId == id).Cast<object>())
        );

        var result = builder.Build();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Property Details", result[0].TableName);
        Assert.Equal("Property Details Reassessment", result[1].TableName);
    }

    #endregion

    #region Build Tests

    [Fact]
    public void Build_WithSingleEntity_ReturnsDictionary()
    {
        // Arrange
        var config = new ReferenceValidationConfiguration();
        config.ForEntity<AssessmentYearRangeCVEntity>()
            .CheckReferences(
                ("Age Factor CV Master", (ctx, id) => ctx.AgeFactorCVMasters.Where(a => a.YearRangeCVId == id).Cast<object>())
            );

        // Act
        var result = config.Build();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.True(result.ContainsKey(typeof(AssessmentYearRangeCVEntity)));
        Assert.Single(result[typeof(AssessmentYearRangeCVEntity)]);
    }

    [Fact]
    public void Build_WithMultipleEntities_ReturnsDictionaryWithAllEntries()
    {
        // Arrange
        var config = new ReferenceValidationConfiguration();
        config.ForEntity<AssessmentYearRangeCVEntity>()
            .CheckReferences(
                ("Age Factor CV Master", (ctx, id) => ctx.AgeFactorCVMasters.Where(a => a.YearRangeCVId == id).Cast<object>())
            );
        config.ForEntity<SubFloorEntity>()
            .CheckReferences(
                ("Property Details", (ctx, id) => ctx.PropertyDetails.Where(p => p.SubFloorId == id).Cast<object>())
            );
        config.ForEntity<ConstructionTypeEntity>()
            .CheckReferences(
                ("Rates", (ctx, id) => ctx.RateEntity.Where(r => r.ConstructionTypeId == id).Cast<object>())
            );

        // Act
        var result = config.Build();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.True(result.ContainsKey(typeof(AssessmentYearRangeCVEntity)));
        Assert.True(result.ContainsKey(typeof(SubFloorEntity)));
        Assert.True(result.ContainsKey(typeof(ConstructionTypeEntity)));
    }

    [Fact]
    public void Build_WithNoEntities_ReturnsEmptyDictionary()
    {
        // Arrange
        var config = new ReferenceValidationConfiguration();

        // Act
        var result = config.Build();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Build_WithEntityButNoReferences_ReturnsEmptyList()
    {
        // Arrange
        var config = new ReferenceValidationConfiguration();
        config.ForEntity<AssessmentYearRangeCVEntity>();

        // Act
        var result = config.Build();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Empty(result[typeof(AssessmentYearRangeCVEntity)]);
    }

    #endregion

    #region IReferenceValidatorBuilder Interface Tests

    [Fact]
    public void IReferenceValidatorBuilder_Build_ReturnsSameAsConcreteBuild()
    {
        // Arrange
        var builder = new ReferenceValidatorBuilder<AssessmentYearRangeCVEntity>();
        builder.CheckReferences(
            ("Age Factor CV Master", (ctx, id) => ctx.AgeFactorCVMasters.Where(a => a.YearRangeCVId == id).Cast<object>())
        );

        IReferenceValidatorBuilder interfaceBuilder = builder;

        // Act
        var concreteResult = builder.Build();
        var interfaceResult = interfaceBuilder.Build();

        // Assert
        Assert.NotNull(concreteResult);
        Assert.NotNull(interfaceResult);
        Assert.Equal(concreteResult.Count, interfaceResult.Count);
        Assert.Equal(concreteResult[0].TableName, interfaceResult[0].TableName);
    }

    #endregion

    #region Complex Configuration Tests

    [Fact]
    public void Configuration_WithComplexReferences_BuildsCorrectly()
    {
        // Arrange
        var config = new ReferenceValidationConfiguration();

        config.ForEntity<TypeOfUseEntity>()
            .CheckReferences(
                ("Parking Type Master", (ctx, id) => ctx.ParkingTypeMaster.Where(d => d.TypeOfUseId == id).Cast<object>()),
                ("Property Description And TypeOfUseValidation", (ctx, id) => ctx.PropertyDescriptionAndTypeOfUseValidations.Where(d => d.TypeOfUseId == id).Cast<object>()),
                ("Property Details", (ctx, id) => ctx.PropertyDetails.Where(d => d.TypeOfUseId == id).Cast<object>()),
                ("Property Details Reassessment", (ctx, id) => ctx.PropertyDetailsReassessment.Where(d => d.TypeOfUseId == id).Cast<object>()),
                ("SubType Of Use Master", (ctx, id) => ctx.SubTypeOfUse.Where(d => d.TypeOfUseId == id).Cast<object>()),
                ("Tax PercentageMaster CV", (ctx, id) => ctx.TaxPercentageMasterCVs.Where(d => d.TypeOfUseId == id).Cast<object>()),
                ("Tax PercentageMaster RV", (ctx, id) => ctx.TaxPercentageMasterRVs.Where(d => d.TypeOfUseId == id).Cast<object>()),
                ("Use Factor CV Master", (ctx, id) => ctx.UseFactorCVMaster.Where(d => d.TypeOfUseId == id).Cast<object>())
            );

        // Act
        var result = config.Build();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.True(result.ContainsKey(typeof(TypeOfUseEntity)));
        Assert.Equal(8, result[typeof(TypeOfUseEntity)].Count);
    }

    [Fact]
    public void Configuration_WithOverridingEntity_UsesLatestConfiguration()
    {
        // Arrange
        var config = new ReferenceValidationConfiguration();

        config.ForEntity<SubFloorEntity>()
            .CheckReferences(
                ("Property Details", (ctx, id) => ctx.PropertyDetails.Where(p => p.SubFloorId == id).Cast<object>())
            );

        // Override with new configuration
        config.ForEntity<SubFloorEntity>()
            .CheckReferences(
                ("Property Details Reassessment", (ctx, id) => ctx.PropertyDetailsReassessment.Where(r => r.SubFloorId == id).Cast<object>())
            );

        // Act
        var result = config.Build();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Single(result[typeof(SubFloorEntity)]);
        Assert.Equal("Property Details Reassessment", result[typeof(SubFloorEntity)][0].TableName);
    }

    #endregion

    #region ReferenceValidatorBuilder Direct Tests

    [Fact]
    public void ReferenceValidatorBuilder_Build_WithNoChecks_ReturnsEmptyList()
    {
        // Arrange
        var builder = new ReferenceValidatorBuilder<AssessmentYearRangeCVEntity>();

        // Act
        var result = builder.Build();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ReferenceValidatorBuilder_CheckReferences_ReturnsBuilderForChaining()
    {
        // Arrange
        var builder = new ReferenceValidatorBuilder<ConstructionTypeEntity>();

        // Act
        var returnedBuilder = builder.CheckReferences(
            ("Rates", (ctx, id) => ctx.RateEntity.Where(r => r.ConstructionTypeId == id).Cast<object>())
        );

        // Assert
        Assert.NotNull(returnedBuilder);
        Assert.Same(builder, returnedBuilder);
    }

    #endregion
}
