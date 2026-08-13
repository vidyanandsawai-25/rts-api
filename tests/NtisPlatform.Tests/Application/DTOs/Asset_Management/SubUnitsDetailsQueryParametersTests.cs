using System.Linq;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Asset_Management.SubUnitsDetails;
using NtisPlatform.Application.DTOs.Queries;
using Xunit;

namespace NtisPlatform.Tests.Application.DTOs.Asset_Management;

/// <summary>
/// Tests for SubUnitsDetailsQueryParameters.
///
/// Unlike sibling query-parameter classes in this folder (e.g. AssetRoomWiseMinusDataQueryParameters,
/// BulkUpdateMasterQueryParameters), none of this class's properties currently carry
/// [Filterable]/[Sortable]/[Searchable]. The characterization test below locks in that current
/// state - see the final summary for why this is flagged as a DTO design gap rather than fixed here
/// (fixing it would mean adding attributes to production code, which is out of scope for this test pass).
/// </summary>
public class SubUnitsDetailsQueryParametersTests
{
    [Fact]
    public void QueryParameters_InheritsFromBaseQueryParameters()
    {
        var queryParameters = new SubUnitsDetailsQueryParameters();

        Assert.IsAssignableFrom<BaseQueryParameters>(queryParameters);
    }

    [Fact]
    public void QueryParameters_Defaults_AllFilterPropertiesAreNull()
    {
        var queryParameters = new SubUnitsDetailsQueryParameters();

        Assert.Null(queryParameters.AssetId);
        Assert.Null(queryParameters.FloorId);
        Assert.Null(queryParameters.SubFloorId);
        Assert.Null(queryParameters.ConstructionTypeId);
        Assert.Null(queryParameters.TypeOfUseId);
        Assert.Null(queryParameters.SubTypeOfUseId);
        Assert.Null(queryParameters.ConstructionYear);
        Assert.Null(queryParameters.AssessmentYear);
        Assert.Null(queryParameters.MarkedForDeletion);
    }

    [Fact]
    public void QueryParameters_AllPropertiesGetAndSetCorrectly()
    {
        var queryParameters = new SubUnitsDetailsQueryParameters
        {
            AssetId = 1,
            FloorId = 2,
            SubFloorId = 3,
            ConstructionTypeId = 4,
            TypeOfUseId = 5,
            SubTypeOfUseId = 6,
            ConstructionYear = "2020",
            AssessmentYear = "2021",
            MarkedForDeletion = false
        };

        Assert.Equal(1, queryParameters.AssetId);
        Assert.Equal(2, queryParameters.FloorId);
        Assert.Equal(3, queryParameters.SubFloorId);
        Assert.Equal(4, queryParameters.ConstructionTypeId);
        Assert.Equal(5, queryParameters.TypeOfUseId);
        Assert.Equal(6, queryParameters.SubTypeOfUseId);
        Assert.Equal("2020", queryParameters.ConstructionYear);
        Assert.Equal("2021", queryParameters.AssessmentYear);
        Assert.False(queryParameters.MarkedForDeletion);
    }

    [Fact]
    public void QueryParameters_MarkedForDeletion_CanBeSetTrue()
    {
        var queryParameters = new SubUnitsDetailsQueryParameters { MarkedForDeletion = true };

        Assert.True(queryParameters.MarkedForDeletion);
    }

    [Fact]
    public void QueryParameters_InheritedPaging_DefaultsMatchBaseQueryParameters()
    {
        var queryParameters = new SubUnitsDetailsQueryParameters();

        Assert.Equal(1, queryParameters.PageNumber);
        Assert.Equal(10, queryParameters.PageSize);
        Assert.Equal("asc", queryParameters.SortOrder);
        Assert.Null(queryParameters.SortBy);
        Assert.Null(queryParameters.SearchTerm);
    }

    [Fact]
    public void QueryParameters_NoPropertiesHaveFilterableSortableOrSearchableAttributes_CurrentlyUnfilterable()
    {
        // Characterization test: as of writing, SubUnitsDetailsQueryParameters declares no
        // [Filterable]/[Sortable]/[Searchable] attributes at all, despite exposing properties
        // (AssetId, FloorId, ConstructionTypeId, etc.) that look like standard filter fields.
        // FilterExpressionBuilder / ApplySort / ApplySearch will silently ignore all of them,
        // and any `?SortBy=AssetId` request will 400 as "not sortable". If this is unintentional,
        // production code needs [Filterable]/[Sortable] (and [Searchable] where relevant) added.
        var properties = typeof(SubUnitsDetailsQueryParameters).GetProperties();

        var anyFilterable = properties.Any(p => p.GetCustomAttributes(typeof(FilterableAttribute), false).Any());
        var anySortable = properties.Any(p => p.GetCustomAttributes(typeof(SortableAttribute), false).Any());
        var anySearchable = properties.Any(p => p.GetCustomAttributes(typeof(SearchableAttribute), false).Any());

        Assert.False(anyFilterable);
        Assert.False(anySortable);
        Assert.False(anySearchable);
    }
}
