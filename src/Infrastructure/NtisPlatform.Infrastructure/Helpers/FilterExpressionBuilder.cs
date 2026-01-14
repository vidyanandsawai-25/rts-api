using System.Linq.Expressions;
using System.Reflection;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;

namespace NtisPlatform.Infrastructure.Helpers;

public static class FilterExpressionBuilder
{
    public static Expression<Func<TEntity, bool>>? BuildFilterExpression<TEntity, TQuery>(TQuery queryParameters)
        where TQuery : BaseQueryParameters
    {
        if (queryParameters == null)
            return null;

        var queryType = typeof(TQuery);
        var entityType = typeof(TEntity);
        var parameter = Expression.Parameter(entityType, "x");
        
        var expressions = new List<Expression>();
        var errors = new Dictionary<string, string>();

        // Get all properties with Filterable attribute
        var filterableProperties = queryType.GetProperties()
            .Where(p => p.GetCustomAttribute<FilterableAttribute>() != null)
            .ToList();

        foreach (var property in filterableProperties)
        {
            var value = property.GetValue(queryParameters);
            if (value == null)
                continue;

            var attribute = property.GetCustomAttribute<FilterableAttribute>()!;
            var entityPropertyName = attribute.EntityProperty ?? property.Name;

            // Handle special cases for range filters (Min/Max prefix)
            if (property.Name.StartsWith("Min") && attribute.Operator == FilterOperator.GreaterThanOrEqual)
            {
                entityPropertyName = attribute.EntityProperty ?? property.Name.Substring(3);
            }
            else if (property.Name.StartsWith("Max") && attribute.Operator == FilterOperator.LessThanOrEqual)
            {
                entityPropertyName = attribute.EntityProperty ?? property.Name.Substring(3);
            }
            else if (property.Name.EndsWith("After") && attribute.Operator == FilterOperator.GreaterThanOrEqual)
            {
                entityPropertyName = attribute.EntityProperty ?? property.Name.Replace("After", "");
            }
            else if (property.Name.EndsWith("Before") && attribute.Operator == FilterOperator.LessThanOrEqual)
            {
                entityPropertyName = attribute.EntityProperty ?? property.Name.Replace("Before", "");
            }

            var entityProperty = entityType.GetProperty(entityPropertyName);
            if (entityProperty == null)
            {
                errors.Add(property.Name, $"Property '{entityPropertyName}' not found on entity type '{entityType.Name}'");
                continue;
            }

            try
            {
                var expression = BuildComparisonExpression(parameter, entityProperty, value, attribute.Operator, property.Name);
                if (expression != null)
                {
                    expressions.Add(expression);
                }
            }
            catch (Exception ex)
            {
                errors.Add(property.Name, $"Invalid filter value: {ex.Message}");
            }
        }

        if (errors.Any())
        {
            throw new FilterValidationException("One or more filter validation errors occurred", errors);
        }

        if (!expressions.Any())
            return null;

        // Combine expressions with AND or OR logic
        var combinedExpression = queryParameters.FilterLogic == FilterLogic.And
            ? expressions.Aggregate((left, right) => Expression.AndAlso(left, right))
            : expressions.Aggregate((left, right) => Expression.OrElse(left, right));

        return Expression.Lambda<Func<TEntity, bool>>(combinedExpression, parameter);
    }

    private static Expression? BuildComparisonExpression(
        ParameterExpression parameter,
        PropertyInfo entityProperty,
        object value,
        FilterOperator operatorType,
        string queryPropertyName)
    {
        var propertyAccess = Expression.Property(parameter, entityProperty);
        var propertyType = entityProperty.PropertyType;
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        // Convert value to target type
        object? convertedValue;
        try
        {
            if (value.GetType() != underlyingType)
            {
                convertedValue = Convert.ChangeType(value, underlyingType);
            }
            else
            {
                convertedValue = value;
            }
        }
        catch
        {
            throw new Exception($"Cannot convert value '{value}' to type '{underlyingType.Name}'");
        }

        var constantValue = Expression.Constant(convertedValue, underlyingType);

        // Handle nullable types
        Expression propertyExpression = propertyAccess;
        if (Nullable.GetUnderlyingType(propertyType) != null)
        {
            propertyExpression = Expression.Property(propertyAccess, "Value");
        }

        // String operations (case-insensitive)
        if (underlyingType == typeof(string))
        {
            return BuildStringExpression(propertyAccess, convertedValue?.ToString() ?? "", operatorType);
        }

        // Numeric/DateTime comparisons
        return operatorType switch
        {
            FilterOperator.Equals => Expression.Equal(propertyExpression, constantValue),
            FilterOperator.GreaterThan => Expression.GreaterThan(propertyExpression, constantValue),
            FilterOperator.LessThan => Expression.LessThan(propertyExpression, constantValue),
            FilterOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(propertyExpression, constantValue),
            FilterOperator.LessThanOrEqual => Expression.LessThanOrEqual(propertyExpression, constantValue),
            _ => throw new Exception($"Operator '{operatorType}' not supported for type '{underlyingType.Name}'")
        };
    }

    private static Expression BuildStringExpression(Expression propertyAccess, string value, FilterOperator operatorType)
    {
        // Convert to lowercase for case-insensitive comparison
        var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)!;
        var propertyLower = Expression.Call(propertyAccess, toLowerMethod);
        var valueLower = Expression.Constant(value.ToLower());

        return operatorType switch
        {
            FilterOperator.Equals => Expression.Equal(propertyLower, valueLower),
            FilterOperator.Contains => Expression.Call(propertyLower, typeof(string).GetMethod("Contains", new[] { typeof(string) })!, valueLower),
            FilterOperator.StartsWith => Expression.Call(propertyLower, typeof(string).GetMethod("StartsWith", new[] { typeof(string) })!, valueLower),
            FilterOperator.EndsWith => Expression.Call(propertyLower, typeof(string).GetMethod("EndsWith", new[] { typeof(string) })!, valueLower),
            _ => throw new Exception($"Operator '{operatorType}' not supported for string type")
        };
    }

    public static Expression<Func<TEntity, bool>>? BuildSearchExpression<TEntity, TQuery>(TQuery queryParameters)
        where TQuery : BaseQueryParameters
    {
        if (queryParameters == null || string.IsNullOrWhiteSpace(queryParameters.SearchTerm))
            return null;

        var queryType = typeof(TQuery);
        var entityType = typeof(TEntity);
        var parameter = Expression.Parameter(entityType, "x");
        
        // Get all properties with Searchable attribute
        var searchableProperties = queryType.GetProperties()
            .Where(p => p.GetCustomAttribute<SearchableAttribute>() != null)
            .ToList();

        if (!searchableProperties.Any())
            return null;

        var searchExpressions = new List<Expression>();
        var searchTermLower = queryParameters.SearchTerm.ToLower();
        var searchTermConstant = Expression.Constant(searchTermLower);

        foreach (var property in searchableProperties)
        {
            var attribute = property.GetCustomAttribute<SearchableAttribute>()!;
            var entityPropertyName = attribute.EntityProperty ?? property.Name;
            var entityProperty = entityType.GetProperty(entityPropertyName);

            if (entityProperty == null || entityProperty.PropertyType != typeof(string))
                continue;

            var propertyAccess = Expression.Property(parameter, entityProperty);
            var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)!;
            var propertyLower = Expression.Call(propertyAccess, toLowerMethod);
            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
            var containsCall = Expression.Call(propertyLower, containsMethod, searchTermConstant);

            searchExpressions.Add(containsCall);
        }

        if (!searchExpressions.Any())
            return null;

        // Combine all search expressions with OR
        var combinedExpression = searchExpressions.Aggregate((left, right) => Expression.OrElse(left, right));
        return Expression.Lambda<Func<TEntity, bool>>(combinedExpression, parameter);
    }

    public static string[] GetSortableFields<TQuery>() where TQuery : BaseQueryParameters
    {
        var queryType = typeof(TQuery);
        return queryType.GetProperties()
            .Where(p => p.GetCustomAttribute<SortableAttribute>() != null)
            .Select(p =>
            {
                var attribute = p.GetCustomAttribute<SortableAttribute>()!;
                return attribute.EntityProperty ?? p.Name;
            })
            .ToArray();
    }
}
