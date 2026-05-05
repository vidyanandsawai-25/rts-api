using System.Linq.Expressions;
using System.Reflection;
using NtisPlatform.Application.Attributes;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Exceptions;

namespace NtisPlatform.Application.Extensions;

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
            var attribute = property.GetCustomAttribute<FilterableAttribute>()!;

            // Skip null values EXCEPT for IsNull/IsNotNull operators which work on boolean flags
            if (value == null && attribute.Operator != FilterOperator.IsNull && attribute.Operator != FilterOperator.IsNotNull)
                continue;

            // For IsNull/IsNotNull, check if the boolean flag is true
            if (attribute.Operator == FilterOperator.IsNull || attribute.Operator == FilterOperator.IsNotNull)
            {
                // If value is null or false, skip this filter
                if (value == null || (value is bool boolValue && !boolValue))
                    continue;
            }

            // Skip empty collections for IN/NOT IN
            if (value is System.Collections.IEnumerable enumerable &&
                value is not string &&
                attribute.Operator != FilterOperator.IsNull &&
                attribute.Operator != FilterOperator.IsNotNull &&
                !enumerable.Cast<object>().Any())
            {
                continue;
            }

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
                // For IsNull/IsNotNull, we don't need the actual value, just pass a dummy
                var valueToPass = (attribute.Operator == FilterOperator.IsNull || attribute.Operator == FilterOperator.IsNotNull)
                    ? new object()
                    : value!;

                var expression = BuildComparisonExpression(parameter, entityProperty, valueToPass, attribute.Operator, property.Name);
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

        // Handle IsNull/IsNotNull operators (no value needed)
        if (operatorType == FilterOperator.IsNull || operatorType == FilterOperator.IsNotNull)
        {
            return BuildNullCheckExpression(propertyAccess, propertyType, operatorType);
        }

        // Handle IN/NOT IN operators for collections
        if (operatorType == FilterOperator.In || operatorType == FilterOperator.NotIn)
        {
            return BuildInExpression(propertyAccess, value, entityProperty, operatorType);
        }

        // String operations
        if (underlyingType == typeof(string))
        {
            return BuildStringExpression(propertyAccess, value?.ToString() ?? "", operatorType);
        }

        // For non-string types, convert value to target type
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
        if (Nullable.GetUnderlyingType(propertyType) != null)
        {
            // propertyAccess.HasValue && propertyAccess.Value <op> constantValue
            var hasValue = Expression.Property(propertyAccess, "HasValue");
            var valueExpr = Expression.Property(propertyAccess, "Value");
            Expression comparison = operatorType switch
            {
                FilterOperator.Equals => Expression.Equal(valueExpr, constantValue),
                FilterOperator.NotEquals => Expression.NotEqual(valueExpr, constantValue),
                FilterOperator.GreaterThan => Expression.GreaterThan(valueExpr, constantValue),
                FilterOperator.LessThan => Expression.LessThan(valueExpr, constantValue),
                FilterOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(valueExpr, constantValue),
                FilterOperator.LessThanOrEqual => Expression.LessThanOrEqual(valueExpr, constantValue),
                _ => throw new Exception($"Operator '{operatorType}' not supported for type '{underlyingType.Name}'")
            };
            return Expression.AndAlso(hasValue, comparison);
        }

        // Non-nullable types
        Expression propertyExpression = propertyAccess;
        return operatorType switch
        {
            FilterOperator.Equals => Expression.Equal(propertyExpression, constantValue),
            FilterOperator.NotEquals => Expression.NotEqual(propertyExpression, constantValue),
            FilterOperator.GreaterThan => Expression.GreaterThan(propertyExpression, constantValue),
            FilterOperator.LessThan => Expression.LessThan(propertyExpression, constantValue),
            FilterOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(propertyExpression, constantValue),
            FilterOperator.LessThanOrEqual => Expression.LessThanOrEqual(propertyExpression, constantValue),
            _ => throw new Exception($"Operator '{operatorType}' not supported for type '{underlyingType.Name}'")
        };
    }

    private static Expression BuildStringExpression(Expression propertyAccess, string value, FilterOperator operatorType)
    {
        if (operatorType == FilterOperator.Contains ||
            operatorType == FilterOperator.StartsWith ||
            operatorType == FilterOperator.EndsWith)
        {
            var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)!;

            var propertyNotNull = Expression.NotEqual(
                propertyAccess,
                Expression.Constant(null, typeof(string)));

            var propertyLower = Expression.Call(propertyAccess, toLowerMethod);
            var valueLower = Expression.Constant(value.ToLower());

            Expression textOperation = operatorType switch
            {
                FilterOperator.Contains => Expression.Call(
                    propertyLower,
                    typeof(string).GetMethod("Contains", new[] { typeof(string) })!,
                    valueLower),

                FilterOperator.StartsWith => Expression.Call(
                    propertyLower,
                    typeof(string).GetMethod("StartsWith", new[] { typeof(string) })!,
                    valueLower),

                FilterOperator.EndsWith => Expression.Call(
                    propertyLower,
                    typeof(string).GetMethod("EndsWith", new[] { typeof(string) })!,
                    valueLower),

                _ => throw new Exception($"Unexpected operator in string text operations")
            };

            // property != null && property.ToLower().<op>(value.ToLower())
            return Expression.AndAlso(propertyNotNull, textOperation);
        }

        if (operatorType == FilterOperator.GreaterThan ||
            operatorType == FilterOperator.LessThan ||
            operatorType == FilterOperator.GreaterThanOrEqual ||
            operatorType == FilterOperator.LessThanOrEqual)
        {
            if (long.TryParse(value, out _))
            {
                var numericPropertyNotNull = Expression.NotEqual(
                    propertyAccess,
                    Expression.Constant(null, typeof(string)));

                var valueLength = value.Length;
                var valueLengthConstant = Expression.Constant(valueLength);
                var valueConstant = Expression.Constant(value);

                var lengthProperty = typeof(string).GetProperty("Length")!;
                var propertyLength = Expression.Property(propertyAccess, lengthProperty);

                var compareToMethod = typeof(string).GetMethod("CompareTo", new[] { typeof(string) })!;
                var compareToCall = Expression.Call(propertyAccess, compareToMethod, valueConstant);
                var zero = Expression.Constant(0);

                Expression lengthComparison, stringComparison, numericRangeComparison;

                switch (operatorType)
                {
                    case FilterOperator.GreaterThan:
                        lengthComparison = Expression.GreaterThan(propertyLength, valueLengthConstant);
                        stringComparison = Expression.GreaterThan(compareToCall, zero);
                        numericRangeComparison = Expression.OrElse(
                            lengthComparison,
                            Expression.AndAlso(
                                Expression.Equal(propertyLength, valueLengthConstant),
                                stringComparison
                            )
                        );
                        break;

                    case FilterOperator.LessThan:
                        lengthComparison = Expression.LessThan(propertyLength, valueLengthConstant);
                        stringComparison = Expression.LessThan(compareToCall, zero);
                        numericRangeComparison = Expression.OrElse(
                            lengthComparison,
                            Expression.AndAlso(
                                Expression.Equal(propertyLength, valueLengthConstant),
                                stringComparison
                            )
                        );
                        break;

                    case FilterOperator.GreaterThanOrEqual:
                        lengthComparison = Expression.GreaterThan(propertyLength, valueLengthConstant);
                        stringComparison = Expression.GreaterThanOrEqual(compareToCall, zero);
                        numericRangeComparison = Expression.OrElse(
                            lengthComparison,
                            Expression.AndAlso(
                                Expression.Equal(propertyLength, valueLengthConstant),
                                stringComparison
                            )
                        );
                        break;

                    case FilterOperator.LessThanOrEqual:
                        lengthComparison = Expression.LessThan(propertyLength, valueLengthConstant);
                        stringComparison = Expression.LessThanOrEqual(compareToCall, zero);
                        numericRangeComparison = Expression.OrElse(
                            lengthComparison,
                            Expression.AndAlso(
                                Expression.Equal(propertyLength, valueLengthConstant),
                                stringComparison
                            )
                        );
                        break;

                    default:
                        throw new Exception($"Unexpected operator");
                }

                return Expression.AndAlso(numericPropertyNotNull, numericRangeComparison);
            }
        }

        var fallbackToLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)!;

        var fallbackPropertyNotNull = Expression.NotEqual(
            propertyAccess,
            Expression.Constant(null, typeof(string)));

        var fallbackPropertyLower = Expression.Call(propertyAccess, fallbackToLowerMethod);
        var fallbackValueLower = Expression.Constant(value.ToLower());

        Expression fallbackComparison;
        if (operatorType == FilterOperator.Equals)
        {
            fallbackComparison = Expression.Equal(fallbackPropertyLower, fallbackValueLower);
        }
        else if (operatorType == FilterOperator.NotEquals)
        {
            fallbackComparison = Expression.NotEqual(fallbackPropertyLower, fallbackValueLower);
        }
        else
        {
            var compareToMethod = typeof(string).GetMethod("CompareTo", new[] { typeof(string) })!;
            var compareToCall = Expression.Call(fallbackPropertyLower, compareToMethod, fallbackValueLower);
            var zero = Expression.Constant(0);

            fallbackComparison = operatorType switch
            {
                FilterOperator.GreaterThan => Expression.GreaterThan(compareToCall, zero),
                FilterOperator.LessThan => Expression.LessThan(compareToCall, zero),
                FilterOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(compareToCall, zero),
                FilterOperator.LessThanOrEqual => Expression.LessThanOrEqual(compareToCall, zero),
                _ => throw new Exception($"Operator '{operatorType}' not supported for string type")
            };
        }

        return Expression.AndAlso(fallbackPropertyNotNull, fallbackComparison);
    }

    private static Expression BuildInExpression(Expression propertyAccess, object collectionValue, PropertyInfo entityProperty, FilterOperator operatorType)
    {
        var valueType = collectionValue.GetType();

        // Validate that the value is a collection (but not a string)
        if (!typeof(System.Collections.IEnumerable).IsAssignableFrom(valueType) || valueType == typeof(string))
        {
            throw new Exception($"IN/NOT IN operator requires a collection type (List, Array, IEnumerable), got {valueType.Name}");
        }

        // Check for empty collection - return appropriate constant expression
        var enumerable = (System.Collections.IEnumerable)collectionValue;
        var hasElements = enumerable.Cast<object>().Any();
        if (!hasElements)
        {
            // Empty collection: IN returns false, NOT IN returns true
            return Expression.Constant(operatorType == FilterOperator.NotIn);
        }

        // Get the element type of the collection
        var elementType = valueType.IsGenericType
            ? valueType.GetGenericArguments()[0]
            : valueType.GetElementType() ?? typeof(object);

        var propertyType = entityProperty.PropertyType;
        var underlyingPropertyType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        // Type validation: collection element type must be compatible with property type
        var targetType = underlyingPropertyType;
        var sourceElementType = Nullable.GetUnderlyingType(elementType) ?? elementType;
        var targetElementType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (sourceElementType != targetElementType && !targetElementType.IsAssignableFrom(sourceElementType))
        {
            // Check if types can be converted (e.g., int to long, string to enum)
            var canConvert = CanConvertTypes(sourceElementType, targetElementType);
            if (!canConvert)
            {
                throw new Exception($"IN/NOT IN filter type mismatch: collection contains '{elementType.Name}' but property '{entityProperty.Name}' is '{targetType.Name}'. Ensure the collection element type matches the entity property type.");
            }
        }

        Expression? nullCheckExpression = null;
        Expression propertyToCheck = propertyAccess;

        // Handle nullable types - need to check HasValue before accessing Value
        if (Nullable.GetUnderlyingType(propertyType) != null)
        {
            var hasValueProperty = Expression.Property(propertyAccess, "HasValue");
            nullCheckExpression = hasValueProperty;
            // Only access .Value when we know HasValue is true (will be used in AndAlso)
            propertyToCheck = Expression.Property(propertyAccess, "Value");
        }
        else if (propertyType.IsClass)
        {
            // For reference types (including string), add null check
            nullCheckExpression = Expression.NotEqual(propertyAccess, Expression.Constant(null, propertyType));
        }

        Expression containsCall;

        // For string collections, use case-insensitive comparison
        if (elementType == typeof(string) && underlyingPropertyType == typeof(string))
        {
            // Convert collection to lowercase for case-insensitive comparison
            var stringEnumerable = enumerable.Cast<string>();
            var lowerCaseCollection = stringEnumerable
                .Where(s => s != null)
                .Select(s => s.ToLower())
                .ToList();

            // If after filtering nulls, collection is empty
            if (lowerCaseCollection.Count == 0)
            {
                return Expression.Constant(operatorType == FilterOperator.NotIn);
            }

            var lowerCollectionConstant = Expression.Constant(lowerCaseCollection);

            // Convert property to lowercase for case-insensitive comparison
            var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)!;
            var propertyLower = Expression.Call(propertyToCheck, toLowerMethod);

            // Use Enumerable.Contains with case-insensitive comparison
            var containsMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(string));

            containsCall = Expression.Call(null, containsMethod, lowerCollectionConstant, propertyLower);
        }
        else
        {
            // For non-string types, convert collection elements to target type if needed
            object convertedCollection = collectionValue;
            var collectionElementType = elementType;

            if (sourceElementType != targetElementType)
            {
                // Convert collection elements to target type
                convertedCollection = ConvertCollectionElements(enumerable, targetElementType);
                collectionElementType = targetElementType;
            }

            // Use standard Contains for non-string types
            var containsMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                .MakeGenericMethod(collectionElementType);

            var collectionConstant = Expression.Constant(convertedCollection);
            containsCall = Expression.Call(null, containsMethod, collectionConstant, propertyToCheck);
        }

        // Apply NOT for NotIn operator
        if (operatorType == FilterOperator.NotIn)
        {
            containsCall = Expression.Not(containsCall);
        }

        // Combine with null check if applicable
        if (nullCheckExpression != null)
        {
            if (operatorType == FilterOperator.NotIn)
            {
                // For NOT IN: null values should return true (not in the list)
                // !HasValue || !Contains(value)
                var invertedNullCheck = Expression.Not(nullCheckExpression);
                return Expression.OrElse(invertedNullCheck, containsCall);
            }
            else
            {
                // For IN: HasValue && Contains(value)
                return Expression.AndAlso(nullCheckExpression, containsCall);
            }
        }

        return containsCall;
    }

    /// <summary>
    /// Checks if source type can be converted to target type
    /// </summary>
    private static bool CanConvertTypes(Type sourceType, Type targetType)
    {
        if (sourceType == targetType)
            return true;

        // Handle numeric conversions
        var numericTypes = new[] { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
                                   typeof(int), typeof(uint), typeof(long), typeof(ulong),
                                   typeof(float), typeof(double), typeof(decimal) };

        if (numericTypes.Contains(sourceType) && numericTypes.Contains(targetType))
            return true;

        // Handle string to enum conversion
        if (sourceType == typeof(string) && targetType.IsEnum)
            return true;

        // Handle enum to string conversion
        if (sourceType.IsEnum && targetType == typeof(string))
            return true;

        // Handle enum underlying type conversions
        if (sourceType.IsEnum && numericTypes.Contains(targetType))
            return true;
        if (targetType.IsEnum && numericTypes.Contains(sourceType))
            return true;

        return false;
    }

    /// <summary>
    /// Converts collection elements to the target type
    /// </summary>
    private static object ConvertCollectionElements(System.Collections.IEnumerable source, Type targetType)
    {
        var listType = typeof(List<>).MakeGenericType(targetType);
        var list = (System.Collections.IList)Activator.CreateInstance(listType)!;

        foreach (var item in source)
        {
            if (item == null)
                continue;

            try
            {
                object convertedItem;
                if (targetType.IsEnum && item is string strValue)
                {
                    convertedItem = Enum.Parse(targetType, strValue, ignoreCase: true);
                }
                else if (targetType.IsEnum)
                {
                    convertedItem = Enum.ToObject(targetType, item);
                }
                else
                {
                    convertedItem = Convert.ChangeType(item, targetType);
                }
                list.Add(convertedItem);
            }
            catch
            {
                // Skip items that can't be converted
            }
        }

        return list;
    }

    /// <summary>
    /// Builds expression for IsNull and IsNotNull operators.
    /// Throws a descriptive exception for non-nullable value types.
    /// </summary>
    private static Expression BuildNullCheckExpression(Expression propertyAccess, Type propertyType, FilterOperator operatorType)
    {
        var underlyingType = Nullable.GetUnderlyingType(propertyType);

        // Handle Nullable<T> types (e.g., int?, DateTime?)
        if (underlyingType != null)
        {
            var hasValueProperty = Expression.Property(propertyAccess, "HasValue");
            return operatorType == FilterOperator.IsNull
                ? Expression.Not(hasValueProperty)  // !HasValue means IsNull
                : hasValueProperty;                  // HasValue means IsNotNull
        }

        // Handle reference types (classes, strings, etc.)
        if (propertyType.IsClass)
        {
            var nullConstant = Expression.Constant(null, propertyType);
            return operatorType == FilterOperator.IsNull
                ? Expression.Equal(propertyAccess, nullConstant)
                : Expression.NotEqual(propertyAccess, nullConstant);
        }

        // Non-nullable value types (int, double, bool, DateTime, enums, structs, etc.)
        // These can never be null, so IsNull/IsNotNull operators don't make sense
        throw new Exception(
            $"Cannot use '{operatorType}' operator on non-nullable value type '{propertyType.Name}'. " +
            $"The property is of type '{propertyType.Name}' which cannot be null. " +
            $"Consider using a nullable type '{propertyType.Name}?' in your entity if null checks are required, " +
            $"or remove the IsNull/IsNotNull filter for this property.");
    }

    public static Expression<Func<TEntity, bool>>? BuildSearchExpression<TEntity, TQuery>(TQuery queryParameters)
        where TQuery : BaseQueryParameters
    {
        if (queryParameters == null || string.IsNullOrWhiteSpace(queryParameters.SearchTerm))
            return null;

        var queryType = typeof(TQuery);
        var entityType = typeof(TEntity);
        var parameter = Expression.Parameter(entityType, "x");

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


    // --- Conversion/Parsing Utilities for PropertyNo and Range Logic ---

    /// <summary>
    /// Normalizes a string by trimming whitespace and converting empty/whitespace strings to null.
    /// </summary>
    /// <param name="s">The string to normalize.</param>
    /// <returns>The trimmed string, or null if the input is null, empty, or whitespace.</returns>
    /// <example>
    /// <code>
    /// Norm("  hello  ") // Returns "hello"
    /// Norm("   ")       // Returns null
    /// Norm(null)        // Returns null
    /// </code>
    /// </example>
    public static string? Norm(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>
    /// Parses a comma-separated value (CSV) string into a distinct list of trimmed, non-empty strings.
    /// </summary>
    /// <param name="s">The CSV string to parse (e.g., "A1, A2, A3").</param>
    /// <returns>A list of distinct, trimmed values (case-insensitive comparison).</returns>
    /// <example>
    /// <code>
    /// Csv("A1,A2,A3")      // Returns ["A1", "A2", "A3"]
    /// Csv("A1, A1, a1")    // Returns ["A1"] (duplicates removed)
    /// Csv("  A1 ,  , A2 ") // Returns ["A1", "A2"] (empty values excluded)
    /// </code>
    /// </example>
    public static List<string> Csv(string? s) =>
        (s ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

    /// <summary>
    /// Attempts to split a string into an alphabetic prefix and numeric suffix (e.g., "A123" → "A" and 123).
    /// </summary>
    /// <param name="s">The string to split.</param>
    /// <param name="pref">The alphabetic prefix (output parameter).</param>
    /// <param name="num">The numeric suffix (output parameter).</param>
    /// <returns>
    /// <c>true</c> if the string contains both a letter prefix and numeric suffix; otherwise, <c>false</c>.
    /// </returns>
    /// <example>
    /// <code>
    /// SplitAlphaNum("A123", out var prefix, out var number) // Returns true, prefix="A", number=123
    /// SplitAlphaNum("ABC", out var prefix, out var number)  // Returns false (no numeric part)
    /// SplitAlphaNum("123", out var prefix, out var number)  // Returns false (no letter prefix)
    /// </code>
    /// </example>
    public static bool SplitAlphaNum(string s, out string pref, out int num)
    {
        s = s.Trim();
        int i = 0; while (i < s.Length && char.IsLetter(s[i])) i++;
        pref = ""; num = 0;
        return i > 0 && i < s.Length && int.TryParse(s[i..], out num) && (pref = s[..i]).Length > 0;
    }

    /// <summary>
    /// Determines if a property number falls within the specified range using intelligent range matching.
    /// </summary>
    /// <param name="pn">The property number to test.</param>
    /// <param name="from">The start of the range.</param>
    /// <param name="to">The end of the range.</param>
    /// <returns><c>true</c> if <paramref name="pn"/> falls within the range; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <para>This method supports three range types:</para>
    /// <list type="number">
    ///   <item><b>Numeric range:</b> "1" to "100" matches numeric values 1-100.</item>
    ///   <item><b>Alphanumeric range (same prefix):</b> "A1" to "A100" matches A1, A2, ..., A100 (numeric comparison).</item>
    ///   <item><b>Lexicographic range:</b> Fallback for mixed formats using case-insensitive string comparison.</item>
    /// </list>
    /// <para>The range is automatically normalized (swaps from/to if reversed).</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// MatchRange("50", "1", "100")     // Returns true (numeric range)
    /// MatchRange("A10", "A1", "A100")  // Returns true (alphanumeric range)
    /// MatchRange("A10", "A20", "A100") // Returns false
    /// MatchRange("ABC", "A", "Z")      // Returns true (lexicographic)
    /// </code>
    /// </example>
    public static bool MatchRange(string pn, string from, string to)
    {
        pn = pn.Trim();
        if (pn.Length == 0) return false;

        // normalize order (ignore-case)
        if (string.Compare(from, to, StringComparison.OrdinalIgnoreCase) > 0) (from, to) = (to, from);

        // numeric range: 1..10
        if (int.TryParse(from, out var fInt) && int.TryParse(to, out var tInt))
            return int.TryParse(pn, out var pInt) && pInt >= fInt && pInt <= tInt;

        // same-prefix alphanum range: A1..A10
        if (SplitAlphaNum(from, out var fPref, out var fNum) &&
            SplitAlphaNum(to, out var tPref, out var tNum) &&
            string.Equals(fPref, tPref, StringComparison.OrdinalIgnoreCase) &&
            SplitAlphaNum(pn, out var pPref, out var pNum) &&
            string.Equals(pPref, fPref, StringComparison.OrdinalIgnoreCase))
        {
            var lo = Math.Min(fNum, tNum);
            var hi = Math.Max(fNum, tNum);
            return pNum >= lo && pNum <= hi;
        }

        // fallback: lexicographic (ignore-case)
        return string.Compare(pn, from, StringComparison.OrdinalIgnoreCase) >= 0 &&
               string.Compare(pn, to, StringComparison.OrdinalIgnoreCase) <= 0;
    }

    /// <summary>
    /// Returns a sort key for mixed numeric/alphanumeric values: numeric values sort before text, and are sorted numerically.
    /// </summary>
    /// <param name="value">The value to generate a sort key for.</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    ///   <item><b>kind:</b> 0 for numeric values, 1 for text values.</item>
    ///   <item><b>num:</b> The numeric value (or <see cref="long.MaxValue"/> for text).</item>
    ///   <item><b>text:</b> The original trimmed string value.</item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// Use this for natural sorting where "10" comes after "2" instead of before it.
    /// </remarks>
    /// <example>
    /// <code>
    /// var items = new[] { "10", "2", "A", "1" };
    /// var sorted = items.OrderBy(x => SortKey(x));
    /// // Result: ["1", "2", "10", "A"]
    /// </code>
    /// </example>
    public static (int kind, long num, string text) SortKey(string? value)
    {
        var v = (value ?? "").Trim();
        bool allDigits = v.Length > 0 && v.All(char.IsDigit);

        if (allDigits && long.TryParse(v, out var n))
            return (0, n, v);

        return (1, long.MaxValue, v);
    }
}
