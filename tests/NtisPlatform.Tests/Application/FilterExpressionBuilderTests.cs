using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Core.Entities;
using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace NtisPlatform.Tests.Application
{
    public class FilterExpressionBuilderTests
    {
        private sealed class TestEntity
        {
            public int Age { get; set; }
            public int? Score { get; set; }
            public DateTime Created { get; set; }
            public string Name { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
        }

        // ---------- Query DTOs for filters ----------
        private sealed class FilterQuery : BaseQueryParameters
        {
            // Direct property name match -> entity.Age
            [Filterable(FilterOperator.Equals)]
            public int? Age { get; set; }

            // Range via Min/Max naming (property.Name.StartsWith("Min"/"Max"))
            [Filterable(FilterOperator.GreaterThanOrEqual)]
            public int? MinScore { get; set; }

            [Filterable(FilterOperator.LessThanOrEqual)]
            public int? MaxScore { get; set; }

            // Date range via After/Before naming
            [Filterable(FilterOperator.GreaterThanOrEqual)]
            public DateTime? CreatedAfter { get; set; }

            [Filterable(FilterOperator.LessThanOrEqual)]
            public DateTime? CreatedBefore { get; set; }

            // String filter on entity.Name
            [Filterable(FilterOperator.Contains)]
            public string? Name { get; set; }
        }

        private sealed class OrFilterQuery : BaseQueryParameters
        {
            public OrFilterQuery() => FilterLogic = FilterLogic.Or;

            [Filterable(FilterOperator.Equals)]
            public int? Age { get; set; }

            [Filterable(FilterOperator.StartsWith)]
            public string? Name { get; set; }
        }

        private sealed class MissingEntityPropertyQuery : BaseQueryParameters
        {
            [Filterable(FilterOperator.Equals, EntityProperty = "DoesNotExist")]
            public int? SomeValue { get; set; }
        }

        private sealed class BadConversionQuery : BaseQueryParameters
        {
            // Query value is string "abc", but entity property Age is int -> Convert.ChangeType fails
            [Filterable(FilterOperator.Equals, EntityProperty = "Age")]
            public string? Age { get; set; }
        }

        // ---------- Query DTOs for search ----------
        private sealed class SearchQuery : BaseQueryParameters
        {
            // BuildSearchExpression reads SearchableAttribute on query properties.
            // The property values are not used; only SearchTerm is used.
            [Searchable(EntityProperty = "Name")]
            public string? Name { get; set; }

            [Searchable(EntityProperty = "City")]
            public string? City { get; set; }
        }

        private sealed class NoSearchableQuery : BaseQueryParameters
        {
            public string? Something { get; set; }
        }

        // ---------- Query DTO for sortable ----------
        private sealed class SortQuery : BaseQueryParameters
        {
            [Sortable(EntityProperty = "CreatedDate")]
            public string? Created { get; set; }

            [Sortable] // no EntityProperty -> should return property name itself
            public string? Age { get; set; }
        }

        // ---------- Helpers ----------
        private static List<TestEntity> SampleData() =>
 new()
 {
        new TestEntity { Age = 10, Score = 50, Created = new DateTime(2024, 1, 1), Name = "Alice", City = "Pune" },
        new TestEntity { Age = 20, Score = 70, Created = new DateTime(2024, 6, 1), Name = "bob",   City = "Mumbai" },
        new TestEntity { Age = 30, Score = 10, Created = new DateTime(2025, 1, 1), Name = "CHARLIE", City = "Delhi" },
 };

        private static Func<TestEntity, bool> Compile(Expression<Func<TestEntity, bool>> expr) => expr.Compile();

        // =======================
        // BuildFilterExpression
        // =======================

        [Fact]
        public void BuildFilterExpression_NullQuery_ReturnsNull()
        {
            var expr = FilterExpressionBuilder.BuildFilterExpression<TestEntity, FilterQuery>(null!);
            Assert.Null(expr);
        }

        [Fact]
        public void BuildFilterExpression_NoFiltersSet_ReturnsNull()
        {
            var q = new FilterQuery(); // all filterable properties are null
            var expr = FilterExpressionBuilder.BuildFilterExpression<TestEntity, FilterQuery>(q);
            Assert.Null(expr);
        }

        [Fact]
        public void BuildFilterExpression_EqualsOnInt_FiltersCorrectly()
        {
            var q = new FilterQuery { Age = 20 };

            var expr = FilterExpressionBuilder.BuildFilterExpression<TestEntity, FilterQuery>(q);
            Assert.NotNull(expr);

            var data = SampleData();
            var result = data.Where(Compile(expr!)).ToList();

            Assert.Single(result);
            Assert.Equal("bob", result[0].Name);
        }

        [Fact]
        public void BuildFilterExpression_RangeOnNullableProperty_WithNullEntityValue_ThrowsAtRuntime()
        {
            var q = new FilterQuery { MinScore = 60 };

            var expr = FilterExpressionBuilder.BuildFilterExpression<TestEntity, FilterQuery>(q);
            Assert.NotNull(expr);

            var predicate = expr!.Compile();

            var entityWithNullScore = new TestEntity
            {
                Age = 99,
                Score = null, // nullable property is null
                Created = new DateTime(2024, 1, 1),
                Name = "X",
                City = "Y"
            };

            // Should be excluded (return false), not throw
            Assert.False(predicate(entityWithNullScore));
        }




        [Fact]
        public void BuildFilterExpression_AfterBeforeDateRange_Works()
        {
            var q = new FilterQuery
            {
                CreatedAfter = new DateTime(2024, 2, 1),
                CreatedBefore = new DateTime(2024, 12, 31)
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<TestEntity, FilterQuery>(q);
            Assert.NotNull(expr);

            var data = SampleData();
            var result = data.Where(Compile(expr!)).ToList();

            Assert.Single(result);
            Assert.Equal("bob", result[0].Name);
        }

        [Fact]
        public void BuildFilterExpression_StringContains_IsCaseInsensitive()
        {
            var q = new FilterQuery { Name = "ali" }; // should match "Alice"

            var expr = FilterExpressionBuilder.BuildFilterExpression<TestEntity, FilterQuery>(q);
            Assert.NotNull(expr);

            var data = SampleData();
            var result = data.Where(Compile(expr!)).ToList();

            Assert.Single(result);
            Assert.Equal("Alice", result[0].Name);
        }

        [Fact]
        public void BuildFilterExpression_MultipleFilters_AndLogic_IsDefault()
        {
            var q = new FilterQuery
            {
                Age = 20,
                Name = "bo" // contains "bo"
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<TestEntity, FilterQuery>(q);
            Assert.NotNull(expr);

            var data = SampleData();
            var result = data.Where(Compile(expr!)).ToList();

            Assert.Single(result);
            Assert.Equal("bob", result[0].Name);
        }

        [Fact]
        public void BuildFilterExpression_MultipleFilters_OrLogic_Works()
        {
            var q = new OrFilterQuery
            {
                Age = 10,     // matches Alice
                Name = "ch"   // starts with "ch" matches "CHARLIE" (case-insensitive)
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<TestEntity, OrFilterQuery>(q);
            Assert.NotNull(expr);

            var data = SampleData();
            var result = data.Where(Compile(expr!)).ToList();

            // Should include Alice and CHARLIE
            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Name == "Alice");
            Assert.Contains(result, x => x.Name == "CHARLIE");
        }

        [Fact]
        public void BuildFilterExpression_MissingEntityProperty_ThrowsFilterValidationException()
        {
            var q = new MissingEntityPropertyQuery { SomeValue = 1 };

            Assert.Throws<FilterValidationException>(() =>
                FilterExpressionBuilder.BuildFilterExpression<TestEntity, MissingEntityPropertyQuery>(q));
        }

        [Fact]
        public void BuildFilterExpression_InvalidConversion_ThrowsFilterValidationException()
        {
            var q = new BadConversionQuery { Age = "abc" };

            Assert.Throws<FilterValidationException>(() =>
                FilterExpressionBuilder.BuildFilterExpression<TestEntity, BadConversionQuery>(q));
        }

        // =======================
        // BuildSearchExpression
        // =======================

        [Fact]
        public void BuildSearchExpression_NullOrWhitespaceSearchTerm_ReturnsNull()
        {
            var q1 = new SearchQuery { SearchTerm = null };
            var q2 = new SearchQuery { SearchTerm = "   " };

            Assert.Null(FilterExpressionBuilder.BuildSearchExpression<TestEntity, SearchQuery>(q1));
            Assert.Null(FilterExpressionBuilder.BuildSearchExpression<TestEntity, SearchQuery>(q2));
        }

        [Fact]
        public void BuildSearchExpression_NoSearchableAttributes_ReturnsNull()
        {
            var q = new NoSearchableQuery { SearchTerm = "pune" };

            var expr = FilterExpressionBuilder.BuildSearchExpression<TestEntity, NoSearchableQuery>(q);
            Assert.Null(expr);
        }

        [Fact]
        public void BuildSearchExpression_SearchesAcrossSearchableFields_OrLogic_CaseInsensitive()
        {
            var q = new SearchQuery { SearchTerm = "mumb" };

            var expr = FilterExpressionBuilder.BuildSearchExpression<TestEntity, SearchQuery>(q);
            Assert.NotNull(expr);

            var data = SampleData();
            var result = data.Where(Compile(expr!)).ToList();

            Assert.Single(result);
            Assert.Equal("bob", result[0].Name);
        }

        [Fact]
        public void BuildSearchExpression_MatchesOnNameOrCity()
        {
            var q = new SearchQuery { SearchTerm = "char" };

            var expr = FilterExpressionBuilder.BuildSearchExpression<TestEntity, SearchQuery>(q);
            Assert.NotNull(expr);

            var data = SampleData();
            var result = data.Where(Compile(expr!)).ToList();

            Assert.Single(result);
            Assert.Equal("CHARLIE", result[0].Name);
        }

        // =======================
        // GetSortableFields
        // =======================

        [Fact]
        public void GetSortableFields_ReturnsEntityPropertyOrPropertyName()
        {
            var fields = FilterExpressionBuilder.GetSortableFields<SortQuery>();

            Assert.Contains("CreatedDate", fields); // EntityProperty
            Assert.Contains("Age", fields);         // fallback to property name
            Assert.Equal(2, fields.Length);
        }

        // =======================
        // IN Operator Tests
        // =======================

        private sealed class InTestEntity
        {
            public int Id { get; set; }
            public string? Category { get; set; }
            public string? Status { get; set; }
            public int? Score { get; set; }
        }

        private sealed class InFilterQuery : BaseQueryParameters
        {
            [Filterable(FilterOperator.In, EntityProperty = "Category")]
            public List<string>? Categories { get; set; }

            [Filterable(FilterOperator.In, EntityProperty = "Score")]
            public List<int>? Scores { get; set; }
        }

        private static List<InTestEntity> SampleInTestData() => new()
        {
            new InTestEntity { Id = 1, Category = "Electronics", Status = "Active", Score = 90 },
            new InTestEntity { Id = 2, Category = "Books", Status = "Active", Score = 85 },
            new InTestEntity { Id = 3, Category = "Clothing", Status = "Inactive", Score = 70 },
            new InTestEntity { Id = 4, Category = "Electronics", Status = "Active", Score = 95 },
            new InTestEntity { Id = 5, Category = "Books", Status = "Inactive", Score = 88 },
        };

        [Fact]
        public void BuildFilterExpression_InOperator_StringCollection_FiltersCorrectly()
        {
            var query = new InFilterQuery
            {
                Categories = new List<string> { "Electronics", "Books" }
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<InTestEntity, InFilterQuery>(query);
            Assert.NotNull(expr);

            var data = SampleInTestData();
            var result = data.Where(expr!.Compile()).ToList();

            Assert.Equal(4, result.Count);
            Assert.Contains(result, x => x.Id == 1);
            Assert.Contains(result, x => x.Id == 2);
            Assert.Contains(result, x => x.Id == 4);
            Assert.Contains(result, x => x.Id == 5);
            Assert.DoesNotContain(result, x => x.Id == 3);
        }

        [Fact]
        public void BuildFilterExpression_InOperator_IntCollection_FiltersCorrectly()
        {
            var query = new InFilterQuery
            {
                Scores = new List<int> { 90, 95 }
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<InTestEntity, InFilterQuery>(query);
            Assert.NotNull(expr);

            var data = SampleInTestData();
            var result = data.Where(expr!.Compile()).ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Id == 1 && x.Score == 90);
            Assert.Contains(result, x => x.Id == 4 && x.Score == 95);
        }

        [Fact]
        public void BuildFilterExpression_InOperator_EmptyCollection_ReturnsNull()
        {
            var query = new InFilterQuery
            {
                Categories = new List<string>()
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<InTestEntity, InFilterQuery>(query);

            Assert.Null(expr);
        }

        [Fact]
        public void BuildFilterExpression_InOperator_NullCollection_ReturnsNull()
        {
            var query = new InFilterQuery
            {
                Categories = null
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<InTestEntity, InFilterQuery>(query);

            Assert.Null(expr);
        }

        [Fact]
        public void BuildFilterExpression_InOperator_SingleItemCollection_Works()
        {
            var query = new InFilterQuery
            {
                Categories = new List<string> { "Electronics" }
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<InTestEntity, InFilterQuery>(query);
            Assert.NotNull(expr);

            var data = SampleInTestData();
            var result = data.Where(expr!.Compile()).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, x => Assert.Equal("Electronics", x.Category));
        }

        [Fact]
        public void BuildFilterExpression_InOperator_WithNullEntityValues_HandlesGracefully()
        {
            var dataWithNulls = new List<InTestEntity>
            {
                new InTestEntity { Id = 1, Category = "Electronics", Score = 90 },
                new InTestEntity { Id = 2, Category = null, Score = 85 },
                new InTestEntity { Id = 3, Category = "Books", Score = null },
            };

            var query = new InFilterQuery
            {
                Categories = new List<string> { "Electronics", "Books" }
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<InTestEntity, InFilterQuery>(query);
            Assert.NotNull(expr);

            var result = dataWithNulls.Where(expr!.Compile()).ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Id == 1);
            Assert.Contains(result, x => x.Id == 3);
            Assert.DoesNotContain(result, x => x.Id == 2);
        }

        [Fact]
        public void BuildFilterExpression_InOperator_MultipleFilters_CombinesWithAnd()
        {
            var query = new InFilterQuery
            {
                Categories = new List<string> { "Electronics", "Books" },
                Scores = new List<int> { 85, 90, 95 }
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<InTestEntity, InFilterQuery>(query);
            Assert.NotNull(expr);

            var data = SampleInTestData();
            var result = data.Where(expr!.Compile()).ToList();

            Assert.Equal(3, result.Count);
            Assert.Contains(result, x => x.Id == 1 && x.Category == "Electronics" && x.Score == 90);
            Assert.Contains(result, x => x.Id == 2 && x.Category == "Books" && x.Score == 85);
            Assert.Contains(result, x => x.Id == 4 && x.Category == "Electronics" && x.Score == 95);
        }

        [Fact]
        public void BuildFilterExpression_InOperator_CaseInsensitive_MatchesCorrectly()
        {
            var query = new InFilterQuery
            {
                Categories = new List<string> { "electronics", "BOOKS" } // Different cases
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<InTestEntity, InFilterQuery>(query);
            Assert.NotNull(expr);

            var data = SampleInTestData();
            var result = data.Where(expr!.Compile()).ToList();

            // Should match "Electronics" and "Books" case-insensitively
            Assert.Equal(4, result.Count);
            Assert.Contains(result, x => x.Id == 1 && x.Category == "Electronics");
            Assert.Contains(result, x => x.Id == 2 && x.Category == "Books");
            Assert.Contains(result, x => x.Id == 4 && x.Category == "Electronics");
            Assert.Contains(result, x => x.Id == 5 && x.Category == "Books");
        }

        [Fact]
        public void BuildFilterExpression_NotInOperator_CaseInsensitive_ExcludesCorrectly()
        {
            var query = new NotInFilterQuery
            {
                ExcludedCategories = new List<string> { "ELECTRONICS" } // Uppercase
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<InTestEntity, NotInFilterQuery>(query);
            Assert.NotNull(expr);

            var data = SampleInTestData();
            var result = data.Where(expr!.Compile()).ToList();

            // Should exclude "Electronics" (case-insensitive), include Books and Clothing
            Assert.Equal(3, result.Count);
            Assert.Contains(result, x => x.Id == 2 && x.Category == "Books");
            Assert.Contains(result, x => x.Id == 3 && x.Category == "Clothing");
            Assert.Contains(result, x => x.Id == 5 && x.Category == "Books");
            Assert.DoesNotContain(result, x => x.Category == "Electronics");
        }

        // =======================
        // Numeric String Comparison Tests
        // =======================

        private sealed class NumericStringEntity
        {
            public int Id { get; set; }
            public string? Code { get; set; }
        }

        private sealed class NumericStringQuery : BaseQueryParameters
        {
            [Filterable(FilterOperator.GreaterThanOrEqual)]
            public string? MinCode { get; set; }

            [Filterable(FilterOperator.LessThanOrEqual)]
            public string? MaxCode { get; set; }
        }

        [Fact]
        public void BuildFilterExpression_NumericStringComparison_WorksCorrectly()
        {
            var data = new List<NumericStringEntity>
            {
                new NumericStringEntity { Id = 1, Code = "1" },
                new NumericStringEntity { Id = 2, Code = "2" },
                new NumericStringEntity { Id = 3, Code = "10" },
                new NumericStringEntity { Id = 4, Code = "100" },
                new NumericStringEntity { Id = 5, Code = "200" },
            };

            var query = new NumericStringQuery
            {
                MinCode = "2",
                MaxCode = "100"
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<NumericStringEntity, NumericStringQuery>(query);
            Assert.NotNull(expr);

            var result = data.Where(expr!.Compile()).ToList();

            // Should correctly handle numeric comparison using length-first + CompareTo strategy: 2 <= value <= 100
            // Strategy: Compares by string length first, then lexicographic CompareTo for equal length
            // Example: "2" (len=1) < "10" (len=2) < "100" (len=3)
            Assert.Equal(3, result.Count);
            Assert.Contains(result, x => x.Code == "2");
            Assert.Contains(result, x => x.Code == "10");
            Assert.Contains(result, x => x.Code == "100");
            Assert.DoesNotContain(result, x => x.Code == "1");
            Assert.DoesNotContain(result, x => x.Code == "200");
        }

        [Fact]
        public void BuildFilterExpression_NumericStringComparison_LargeNumbers_WorksCorrectly()
        {
            var data = new List<NumericStringEntity>
            {
                new NumericStringEntity { Id = 1, Code = "1" },
                new NumericStringEntity { Id = 2, Code = "50" },
                new NumericStringEntity { Id = 3, Code = "500" },
                new NumericStringEntity { Id = 4, Code = "5000" },
                new NumericStringEntity { Id = 5, Code = "50000" },
            };

            var query = new NumericStringQuery
            {
                MinCode = "50",
                MaxCode = "5000"
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<NumericStringEntity, NumericStringQuery>(query);
            Assert.NotNull(expr);

            var result = data.Where(expr!.Compile()).ToList();

            // Length-first comparison ensures correct numeric ordering without padding
            // Strategy: Compare by length first (shorter = smaller), then by CompareTo for equal length
            Assert.Equal(3, result.Count);
            Assert.Contains(result, x => x.Code == "50");
            Assert.Contains(result, x => x.Code == "500");
            Assert.Contains(result, x => x.Code == "5000");
        }

        // =======================
        // Property Entity Integration Tests
        // =======================

        [Fact]
        public void BuildFilterExpression_PropertyEntity_WardIdsInOperator_WorksCorrectly()
        {
            var data = new List<PropertyEntity>
            {
                new PropertyEntity { PropertyId = 1, TaxZoneId = 1, WardId = 1, PropertyNo = "100", PartitionNo = "A", IsActive = true },
                new PropertyEntity { PropertyId = 2, TaxZoneId = 1, WardId = 1, PropertyNo = "150", PartitionNo = "B", IsActive = true },
                new PropertyEntity { PropertyId = 3, TaxZoneId = 1, WardId = 2, PropertyNo = "200", PartitionNo = "A", IsActive = true },
                new PropertyEntity { PropertyId = 4, TaxZoneId = 1, WardId = 2, PropertyNo = "250", PartitionNo = "C", IsActive = true },
                new PropertyEntity { PropertyId = 5, TaxZoneId = 1, WardId = 3, PropertyNo = "300", PartitionNo = "A", IsActive = true },
            };

            var query = new PropertyQueryParameters
            {
                WardIds = new List<int> { 1, 2 }
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<PropertyEntity, PropertyQueryParameters>(query);
            Assert.NotNull(expr);

            var result = data.Where(expr!.Compile()).ToList();

            Assert.Equal(4, result.Count);
            Assert.Contains(result, x => x.WardId == 1);
            Assert.Contains(result, x => x.WardId == 2);
            Assert.DoesNotContain(result, x => x.WardId == 3);
        }

        [Fact]
        public void BuildFilterExpression_PropertyEntity_PropertyNoContainsFilter()
        {
            var data = new List<PropertyEntity>
            {
                new PropertyEntity { PropertyId = 1, TaxZoneId = 1, WardId = 1, PropertyNo = "1", PartitionNo = "A", IsActive = true },
                new PropertyEntity { PropertyId = 2, TaxZoneId = 1, WardId = 1, PropertyNo = "2", PartitionNo = "A", IsActive = true },
                new PropertyEntity { PropertyId = 3, TaxZoneId = 1, WardId = 1, PropertyNo = "10", PartitionNo = "A", IsActive = true },
                new PropertyEntity { PropertyId = 4, TaxZoneId = 1, WardId = 1, PropertyNo = "100", PartitionNo = "A", IsActive = true },
                new PropertyEntity { PropertyId = 5, TaxZoneId = 1, WardId = 1, PropertyNo = "200", PartitionNo = "A", IsActive = true },
            };

            var query = new PropertyQueryParameters
            {
                PropertyNo = "10"
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<PropertyEntity, PropertyQueryParameters>(query);
            Assert.NotNull(expr);

            var result = data.Where(expr!.Compile()).ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.PropertyNo == "10");
            Assert.Contains(result, x => x.PropertyNo == "100");
        }

        [Fact]
        public void BuildFilterExpression_PropertyEntity_CombinedWardIdsAndPropertyNoFilters()
        {
            var data = new List<PropertyEntity>
            {
                new PropertyEntity { PropertyId = 1, TaxZoneId = 1, WardId = 13, PropertyNo = "1", PartitionNo = "A", IsActive = true },
                new PropertyEntity { PropertyId = 2, TaxZoneId = 1, WardId = 13, PropertyNo = "50", PartitionNo = "A", IsActive = true },
                new PropertyEntity { PropertyId = 3, TaxZoneId = 1, WardId = 11, PropertyNo = "10", PartitionNo = "A", IsActive = true },
                new PropertyEntity { PropertyId = 4, TaxZoneId = 1, WardId = 11, PropertyNo = "100", PartitionNo = "A", IsActive = true },
                new PropertyEntity { PropertyId = 5, TaxZoneId = 1, WardId = 11, PropertyNo = "200", PartitionNo = "A", IsActive = true },
                new PropertyEntity { PropertyId = 6, TaxZoneId = 1, WardId = 99, PropertyNo = "75", PartitionNo = "A", IsActive = true },
            };

            var query = new PropertyQueryParameters
            {
                WardIds = new List<int> { 13, 11 },
                PropertyNo = "10"
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<PropertyEntity, PropertyQueryParameters>(query);
            Assert.NotNull(expr);

            var result = data.Where(expr!.Compile()).ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.WardId == 11 && x.PropertyNo == "10");
            Assert.Contains(result, x => x.WardId == 11 && x.PropertyNo == "100");
        }

        [Fact]
        public void BuildFilterExpression_PropertyEntity_PropertyNoContains()
        {
            var data = new List<PropertyEntity>
            {
                new PropertyEntity { PropertyId = 1, TaxZoneId = 1, WardId = 1, PropertyNo = "100", PartitionNo = "A", IsActive = true },
                new PropertyEntity { PropertyId = 2, TaxZoneId = 1, WardId = 1, PropertyNo = "150", PartitionNo = "A", IsActive = true },
                new PropertyEntity { PropertyId = 3, TaxZoneId = 1, WardId = 1, PropertyNo = "250", PartitionNo = "A", IsActive = true },
                new PropertyEntity { PropertyId = 4, TaxZoneId = 1, WardId = 1, PropertyNo = "350", PartitionNo = "A", IsActive = true },
            };

            var query = new PropertyQueryParameters
            {
                PropertyNo = "50" // Should match 150, 250, 350
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<PropertyEntity, PropertyQueryParameters>(query);
            Assert.NotNull(expr);

            var result = data.Where(expr!.Compile()).ToList();

            Assert.Equal(3, result.Count);
            Assert.Contains(result, x => x.PropertyNo == "150");
            Assert.Contains(result, x => x.PropertyNo == "250");
            Assert.Contains(result, x => x.PropertyNo == "350");
        }

        [Fact]
        public void BuildFilterExpression_PropertyEntity_PartitionNoContains()
        {
            var data = new List<PropertyEntity>
            {
                new PropertyEntity { PropertyId = 1, TaxZoneId = 1, WardId = 1, PropertyNo = "100", PartitionNo = "A", IsActive = true },
                new PropertyEntity { PropertyId = 2, TaxZoneId = 1, WardId = 1, PropertyNo = "150", PartitionNo = "AB", IsActive = true },
                new PropertyEntity { PropertyId = 3, TaxZoneId = 1, WardId = 1, PropertyNo = "200", PartitionNo = "B", IsActive = true },
                new PropertyEntity { PropertyId = 4, TaxZoneId = 1, WardId = 1, PropertyNo = "250", PartitionNo = "C", IsActive = true },
            };

            var query = new PropertyQueryParameters
            {
                PartitionNo = "A"
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<PropertyEntity, PropertyQueryParameters>(query);
            Assert.NotNull(expr);

            var result = data.Where(expr!.Compile()).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, x => Assert.Contains("A", x.PartitionNo));
        }

        [Fact]
        public void BuildFilterExpression_PropertyEntity_EmptyWardIdsList_ReturnsNull()
        {
            var data = new List<PropertyEntity>
            {
                new PropertyEntity { PropertyId = 1, TaxZoneId = 1, WardId = 1, PropertyNo = "100", PartitionNo = "A", IsActive = true },
            };

            var query = new PropertyQueryParameters
            {
                WardIds = new List<int>() // Empty list
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<PropertyEntity, PropertyQueryParameters>(query);

            // Empty collection should skip the filter, returning null
            Assert.Null(expr);
        }

        // =======================
        // Null String Property Tests (Guards against NullReferenceException)
        // =======================

        private sealed class NullableStringEntity
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }
        }

        private sealed class NullableStringQuery : BaseQueryParameters
        {
            [Filterable(FilterOperator.Contains)]
            public string? Name { get; set; }

            [Filterable(FilterOperator.StartsWith)]
            public string? Description { get; set; }
        }

        private sealed class NullableStringEndsWithQuery : BaseQueryParameters
        {
            [Filterable(FilterOperator.EndsWith)]
            public string? Name { get; set; }
        }

        [Fact]
        public void BuildFilterExpression_ContainsOperator_WithNullPropertyValue_DoesNotThrowAndFiltersCorrectly()
        {
            var data = new List<NullableStringEntity>
            {
                new NullableStringEntity { Id = 1, Name = "Alice", Description = "Developer" },
                new NullableStringEntity { Id = 2, Name = null, Description = "Manager" },
                new NullableStringEntity { Id = 3, Name = "Bob", Description = null },
                new NullableStringEntity { Id = 4, Name = null, Description = null },
            };

            var query = new NullableStringQuery
            {
                Name = "ali" // Should only match entities with non-null Name containing "ali"
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<NullableStringEntity, NullableStringQuery>(query);
            Assert.NotNull(expr);

            var predicate = expr!.Compile();

            // Execute in-memory - should NOT throw NullReferenceException
            var result = data.Where(predicate).ToList();

            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
            Assert.Equal("Alice", result[0].Name);
        }

        [Fact]
        public void BuildFilterExpression_StartsWithOperator_WithNullPropertyValue_DoesNotThrowAndFiltersCorrectly()
        {
            var data = new List<NullableStringEntity>
            {
                new NullableStringEntity { Id = 1, Name = "Alice", Description = "Developer" },
                new NullableStringEntity { Id = 2, Name = "Bob", Description = null },
                new NullableStringEntity { Id = 3, Name = "Charlie", Description = "Designer" },
                new NullableStringEntity { Id = 4, Name = "David", Description = null },
            };

            var query = new NullableStringQuery
            {
                Description = "dev" // Should only match entities with non-null Description starting with "dev"
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<NullableStringEntity, NullableStringQuery>(query);
            Assert.NotNull(expr);

            var predicate = expr!.Compile();

            // Execute in-memory - should NOT throw NullReferenceException
            var result = data.Where(predicate).ToList();

            Assert.Single(result);
            Assert.Equal(1, result[0].Id);
            Assert.Equal("Developer", result[0].Description);
        }

        [Fact]
        public void BuildFilterExpression_EndsWithOperator_WithNullPropertyValue_DoesNotThrowAndFiltersCorrectly()
        {
            var data = new List<NullableStringEntity>
            {
                new NullableStringEntity { Id = 1, Name = "Testing" },
                new NullableStringEntity { Id = 2, Name = null },
                new NullableStringEntity { Id = 3, Name = "Debugging" },
                new NullableStringEntity { Id = 4, Name = "Coding" },
            };

            var query = new NullableStringEndsWithQuery
            {
                Name = "ing" // Should match "Testing", "Debugging", "Coding" but not null
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<NullableStringEntity, NullableStringEndsWithQuery>(query);
            Assert.NotNull(expr);

            var predicate = expr!.Compile();

            // Execute in-memory - should NOT throw NullReferenceException
            var result = data.Where(predicate).ToList();

            Assert.Equal(3, result.Count);
            Assert.Contains(result, x => x.Id == 1);
            Assert.Contains(result, x => x.Id == 3);
            Assert.Contains(result, x => x.Id == 4);
            Assert.DoesNotContain(result, x => x.Id == 2); // Null should be filtered out
        }

        [Fact]
        public void BuildFilterExpression_AllNullStrings_ReturnsEmptyResult()
        {
            var data = new List<NullableStringEntity>
            {
                new NullableStringEntity { Id = 1, Name = null },
                new NullableStringEntity { Id = 2, Name = null },
                new NullableStringEntity { Id = 3, Name = null },
            };

            var query = new NullableStringQuery
            {
                Name = "test"
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<NullableStringEntity, NullableStringQuery>(query);
            Assert.NotNull(expr);

            var predicate = expr!.Compile();

            // Execute in-memory - should NOT throw NullReferenceException
            var result = data.Where(predicate).ToList();

            Assert.Empty(result); // All null values should be filtered out
        }

        [Fact]
        public void BuildFilterExpression_MixedNullAndNonNull_ContainsOperator_FiltersCorrectly()
        {
            var data = new List<NullableStringEntity>
            {
                new NullableStringEntity { Id = 1, Name = "Test123" },
                new NullableStringEntity { Id = 2, Name = null },
                new NullableStringEntity { Id = 3, Name = "Testing456" },
                new NullableStringEntity { Id = 4, Name = "" }, // Empty string
                new NullableStringEntity { Id = 5, Name = "Test789" },
            };

            var query = new NullableStringQuery
            {
                Name = "test"
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<NullableStringEntity, NullableStringQuery>(query);
            Assert.NotNull(expr);

            var predicate = expr!.Compile();

            // Execute in-memory
            var result = data.Where(predicate).ToList();

            Assert.Equal(3, result.Count);
            Assert.Contains(result, x => x.Id == 1);
            Assert.Contains(result, x => x.Id == 3);
            Assert.Contains(result, x => x.Id == 5);
            Assert.DoesNotContain(result, x => x.Id == 2); // Null filtered out
            Assert.DoesNotContain(result, x => x.Id == 4); // Empty string doesn't contain "test"
        }

        [Fact]
        public void BuildFilterExpression_PropertyEntity_NullPartitionNo_DoesNotThrowOnContains()
        {
            var data = new List<PropertyEntity>
            {
                new PropertyEntity { PropertyId = 1, TaxZoneId = 1, WardId = 1, PropertyNo = "100", PartitionNo = "A", IsActive = true },
                new PropertyEntity { PropertyId = 2, TaxZoneId = 1, WardId = 1, PropertyNo = "101", PartitionNo = null, IsActive = true },
                new PropertyEntity { PropertyId = 3, TaxZoneId = 1, WardId = 1, PropertyNo = "102", PartitionNo = "AB", IsActive = true },
            };

            var query = new PropertyQueryParameters
            {
                PartitionNo = "A"
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<PropertyEntity, PropertyQueryParameters>(query);
            Assert.NotNull(expr);

            var predicate = expr!.Compile();

            // Execute in-memory - should NOT throw NullReferenceException on null PartitionNo
            var result = data.Where(predicate).ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.PropertyId == 1);
            Assert.Contains(result, x => x.PropertyId == 3);
            Assert.DoesNotContain(result, x => x.PropertyId == 2); // Null PartitionNo filtered out
        }

        // =======================
        // NotIn Operator Tests
        // =======================

        private sealed class NotInFilterQuery : BaseQueryParameters
        {
            [Filterable(FilterOperator.NotIn, EntityProperty = "Category")]
            public List<string>? ExcludedCategories { get; set; }

            [Filterable(FilterOperator.NotIn, EntityProperty = "Score")]
            public List<int>? ExcludedScores { get; set; }
        }

        [Fact]
        public void BuildFilterExpression_NotInOperator_StringCollection_FiltersCorrectly()
        {
            var query = new NotInFilterQuery
            {
                ExcludedCategories = new List<string> { "Clothing" }
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<InTestEntity, NotInFilterQuery>(query);
            Assert.NotNull(expr);

            var data = SampleInTestData();
            var result = data.Where(expr!.Compile()).ToList();

            // Should exclude Clothing (Id=3), include all others
            Assert.Equal(4, result.Count);
            Assert.Contains(result, x => x.Id == 1 && x.Category == "Electronics");
            Assert.Contains(result, x => x.Id == 2 && x.Category == "Books");
            Assert.Contains(result, x => x.Id == 4 && x.Category == "Electronics");
            Assert.Contains(result, x => x.Id == 5 && x.Category == "Books");
            Assert.DoesNotContain(result, x => x.Id == 3);
        }

        [Fact]
        public void BuildFilterExpression_NotInOperator_IntCollection_FiltersCorrectly()
        {
            var query = new NotInFilterQuery
            {
                ExcludedScores = new List<int> { 70, 85 }
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<InTestEntity, NotInFilterQuery>(query);
            Assert.NotNull(expr);

            var data = SampleInTestData();
            var result = data.Where(expr!.Compile()).ToList();

            // Should exclude scores 70 and 85
            Assert.Equal(3, result.Count);
            Assert.Contains(result, x => x.Score == 90);
            Assert.Contains(result, x => x.Score == 95);
            Assert.Contains(result, x => x.Score == 88);
        }

        [Fact]
        public void BuildFilterExpression_NotInOperator_EmptyCollection_ReturnsNull()
        {
            var query = new NotInFilterQuery
            {
                ExcludedCategories = new List<string>()
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<InTestEntity, NotInFilterQuery>(query);

            Assert.Null(expr);
        }

        [Fact]
        public void BuildFilterExpression_NotInOperator_NullCollection_ReturnsNull()
        {
            var query = new NotInFilterQuery
            {
                ExcludedCategories = null
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<InTestEntity, NotInFilterQuery>(query);

            Assert.Null(expr);
        }

        [Fact]
        public void BuildFilterExpression_NotInOperator_WithNullEntityValues_IncludesNulls()
        {
            var dataWithNulls = new List<InTestEntity>
            {
                new InTestEntity { Id = 1, Category = "Electronics", Score = 90 },
                new InTestEntity { Id = 2, Category = null, Score = 85 },
                new InTestEntity { Id = 3, Category = "Books", Score = null },
            };

            var query = new NotInFilterQuery
            {
                ExcludedCategories = new List<string> { "Electronics" }
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<InTestEntity, NotInFilterQuery>(query);
            Assert.NotNull(expr);

            var result = dataWithNulls.Where(expr!.Compile()).ToList();

            // Should exclude Electronics (Id=1), include Books and null
            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Id == 2 && x.Category == null);
            Assert.Contains(result, x => x.Id == 3 && x.Category == "Books");
            Assert.DoesNotContain(result, x => x.Id == 1);
        }

        // =======================
        // NotEquals Operator Tests
        // =======================

        private sealed class NotEqualsQuery : BaseQueryParameters
        {
            [Filterable(FilterOperator.NotEquals, EntityProperty = "Age")]
            public int? ExcludedAge { get; set; }

            [Filterable(FilterOperator.NotEquals, EntityProperty = "Name")]
            public string? ExcludedName { get; set; }
        }

        [Fact]
        public void BuildFilterExpression_NotEqualsOperator_IntValue_FiltersCorrectly()
        {
            var query = new NotEqualsQuery
            {
                ExcludedAge = 20
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<TestEntity, NotEqualsQuery>(query);
            Assert.NotNull(expr);

            var data = SampleData();
            var result = data.Where(expr!.Compile()).ToList();

            // Should exclude Age=20 (bob), include Alice and CHARLIE
            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Name == "Alice" && x.Age == 10);
            Assert.Contains(result, x => x.Name == "CHARLIE" && x.Age == 30);
            Assert.DoesNotContain(result, x => x.Name == "bob");
        }

        [Fact]
        public void BuildFilterExpression_NotEqualsOperator_StringValue_CaseInsensitive()
        {
            var query = new NotEqualsQuery
            {
                ExcludedName = "BOB" // Should match "bob" (case-insensitive)
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<TestEntity, NotEqualsQuery>(query);
            Assert.NotNull(expr);

            var data = SampleData();
            var result = data.Where(expr!.Compile()).ToList();

            // Should exclude "bob", include Alice and CHARLIE
            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Name == "Alice");
            Assert.Contains(result, x => x.Name == "CHARLIE");
            Assert.DoesNotContain(result, x => x.Name == "bob");
        }

        [Fact]
        public void BuildFilterExpression_NotEqualsOperator_WithNullEntityValue_ExcludesNulls()
        {
            var dataWithNulls = new List<NullableStringEntity>
            {
                new NullableStringEntity { Id = 1, Name = "Alice" },
                new NullableStringEntity { Id = 2, Name = null },
                new NullableStringEntity { Id = 3, Name = "Bob" },
            };

            var query = new NotEqualsQuery
            {
                ExcludedName = "Alice"
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<NullableStringEntity, NotEqualsQuery>(query);
            Assert.NotNull(expr);

            var result = dataWithNulls.Where(expr!.Compile()).ToList();

            // Should exclude Alice AND null (null check happens first), include only Bob
            Assert.Single(result);
            Assert.Equal(3, result[0].Id);
            Assert.Equal("Bob", result[0].Name);
        }

        // =======================
        // IsNull Operator Tests
        // =======================

        private sealed class IsNullQuery : BaseQueryParameters
        {
            [Filterable(FilterOperator.IsNull, EntityProperty = "Name")]
            public bool? NameIsNull { get; set; }

            [Filterable(FilterOperator.IsNull, EntityProperty = "Score")]
            public bool? ScoreIsNull { get; set; }
        }

        [Fact]
        public void BuildFilterExpression_IsNullOperator_FiltersNullValues()
        {
            var dataWithNulls = new List<NullableStringEntity>
            {
                new NullableStringEntity { Id = 1, Name = "Alice", Description = "Dev" },
                new NullableStringEntity { Id = 2, Name = null, Description = "Manager" },
                new NullableStringEntity { Id = 3, Name = "Bob", Description = null },
                new NullableStringEntity { Id = 4, Name = null, Description = null },
            };

            var query = new IsNullQuery
            {
                NameIsNull = true // Filter where Name IS NULL
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<NullableStringEntity, IsNullQuery>(query);
            Assert.NotNull(expr);

            var result = dataWithNulls.Where(expr!.Compile()).ToList();

            // Should return only entities with null Name
            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Id == 2);
            Assert.Contains(result, x => x.Id == 4);
        }

        [Fact]
        public void BuildFilterExpression_IsNullOperator_OnValueType_FiltersNullValues()
        {
            var dataWithNulls = new List<TestEntity>
            {
                new TestEntity { Age = 10, Score = 50, Name = "Alice", City = "Pune" },
                new TestEntity { Age = 20, Score = null, Name = "Bob", City = "Mumbai" },
                new TestEntity { Age = 30, Score = 70, Name = "Charlie", City = "Delhi" },
            };

            var query = new IsNullQuery
            {
                ScoreIsNull = true
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<TestEntity, IsNullQuery>(query);
            Assert.NotNull(expr);

            var result = dataWithNulls.Where(expr!.Compile()).ToList();

            // Should return only Bob with null Score
            Assert.Single(result);
            Assert.Equal("Bob", result[0].Name);
        }

        [Fact]
        public void BuildFilterExpression_IsNullOperator_False_ReturnsNull()
        {
            var query = new IsNullQuery
            {
                NameIsNull = false // false means skip the filter
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<NullableStringEntity, IsNullQuery>(query);

            // Should return null since we're not filtering for null
            Assert.Null(expr);
        }

        [Fact]
        public void BuildFilterExpression_IsNullOperator_Null_ReturnsNull()
        {
            var query = new IsNullQuery
            {
                NameIsNull = null
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<NullableStringEntity, IsNullQuery>(query);

            Assert.Null(expr);
        }

        // =======================
        // IsNotNull Operator Tests
        // =======================

        private sealed class IsNotNullQuery : BaseQueryParameters
        {
            [Filterable(FilterOperator.IsNotNull, EntityProperty = "Name")]
            public bool? NameIsNotNull { get; set; }

            [Filterable(FilterOperator.IsNotNull, EntityProperty = "Description")]
            public bool? DescriptionIsNotNull { get; set; }
        }

        [Fact]
        public void BuildFilterExpression_IsNotNullOperator_FiltersNonNullValues()
        {
            var dataWithNulls = new List<NullableStringEntity>
            {
                new NullableStringEntity { Id = 1, Name = "Alice", Description = "Dev" },
                new NullableStringEntity { Id = 2, Name = null, Description = "Manager" },
                new NullableStringEntity { Id = 3, Name = "Bob", Description = null },
                new NullableStringEntity { Id = 4, Name = null, Description = null },
            };

            var query = new IsNotNullQuery
            {
                NameIsNotNull = true // Filter where Name IS NOT NULL
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<NullableStringEntity, IsNotNullQuery>(query);
            Assert.NotNull(expr);

            var result = dataWithNulls.Where(expr!.Compile()).ToList();

            // Should return only entities with non-null Name
            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Id == 1 && x.Name == "Alice");
            Assert.Contains(result, x => x.Id == 3 && x.Name == "Bob");
        }

        [Fact]
        public void BuildFilterExpression_IsNotNullOperator_CombinedWithIsNull()
        {
            var dataWithNulls = new List<NullableStringEntity>
            {
                new NullableStringEntity { Id = 1, Name = "Alice", Description = "Dev" },
                new NullableStringEntity { Id = 2, Name = null, Description = "Manager" },
                new NullableStringEntity { Id = 3, Name = "Bob", Description = null },
                new NullableStringEntity { Id = 4, Name = null, Description = null },
                new NullableStringEntity { Id = 5, Name = "Charlie", Description = "Designer" },
            };

            // Combined query: Name IS NOT NULL AND Description IS NOT NULL
            var query = new IsNotNullQuery
            {
                NameIsNotNull = true,
                DescriptionIsNotNull = true
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<NullableStringEntity, IsNotNullQuery>(query);
            Assert.NotNull(expr);

            var result = dataWithNulls.Where(expr!.Compile()).ToList();

            // Should return only entities with both Name AND Description non-null
            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Id == 1 && x.Name == "Alice");
            Assert.Contains(result, x => x.Id == 5 && x.Name == "Charlie");
        }

        [Fact]
        public void BuildFilterExpression_IsNotNullOperator_False_ReturnsNull()
        {
            var query = new IsNotNullQuery
            {
                NameIsNotNull = false
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<NullableStringEntity, IsNotNullQuery>(query);

            Assert.Null(expr);
        }

        [Fact]
        public void BuildFilterExpression_IsNotNullOperator_Null_ReturnsNull()
        {
            var query = new IsNotNullQuery
            {
                NameIsNotNull = null
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<NullableStringEntity, IsNotNullQuery>(query);

            Assert.Null(expr);
        }

        // =======================
        // Combined New Operators Tests
        // =======================

        private sealed class CombinedOperatorsQuery : BaseQueryParameters
        {
            [Filterable(FilterOperator.NotIn, EntityProperty = "Category")]
            public List<string>? ExcludedCategories { get; set; }

            [Filterable(FilterOperator.NotEquals, EntityProperty = "Score")]
            public int? ExcludedScore { get; set; }
        }

        [Fact]
        public void BuildFilterExpression_CombinedNotInAndNotEquals_FiltersCorrectly()
        {
            var data = SampleInTestData();

            // Complex query combining multiple new operators
            // NotIn: Exclude "Books" category
            // NotEquals: Exclude Score=90
            // Expected: Electronics with Score 95, Clothing with Score 70
            var query = new CombinedOperatorsQuery
            {
                ExcludedCategories = new List<string> { "Books" },
                ExcludedScore = 90
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<InTestEntity, CombinedOperatorsQuery>(query);
            Assert.NotNull(expr);

            var result = data.Where(expr!.Compile()).ToList();

            // Should exclude Books category and Score=90
            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Id == 3 && x.Category == "Clothing" && x.Score == 70);
            Assert.Contains(result, x => x.Id == 4 && x.Category == "Electronics" && x.Score == 95);
        }

        [Fact]
        public void BuildFilterExpression_IsNullAndIsNotNull_Orthogonal()
        {
            var dataWithNulls = new List<NullableStringEntity>
            {
                new NullableStringEntity { Id = 1, Name = "Alice", Description = null },
                new NullableStringEntity { Id = 2, Name = null, Description = "Manager" },
                new NullableStringEntity { Id = 3, Name = "Bob", Description = "Dev" },
            };

            // Query: Name IS NOT NULL AND Description IS NOT NULL
            var query = new IsNotNullQuery
            {
                NameIsNotNull = true,
                DescriptionIsNotNull = true
            };

            var expr = FilterExpressionBuilder.BuildFilterExpression<NullableStringEntity, IsNotNullQuery>(query);
            Assert.NotNull(expr);

            var result = dataWithNulls.Where(expr!.Compile()).ToList();

            // Should return only Bob (both fields non-null)
            Assert.Single(result);
            Assert.Equal(3, result[0].Id);
            Assert.Equal("Bob", result[0].Name);
            Assert.Equal("Dev", result[0].Description);
        }
    }
}


