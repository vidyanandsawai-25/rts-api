using NtisPlatform.Application.Attributes;
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
                Score = null, // force nullable.Value access
                Created = new DateTime(2024, 1, 1),
                Name = "X",
                City = "Y"
            };

            Assert.Throws<InvalidOperationException>(() => predicate(entityWithNullScore));
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
    }
}
