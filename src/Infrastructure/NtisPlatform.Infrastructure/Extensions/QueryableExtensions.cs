using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.DTOs.Queries;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Models;
using NtisPlatform.Infrastructure.Helpers;

namespace NtisPlatform.Infrastructure.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<TEntity> ApplyFilters<TEntity, TQuery>(
        this IQueryable<TEntity> query,
        TQuery queryParameters)
        where TQuery : BaseQueryParameters
    {
        var filterExpression = FilterExpressionBuilder.BuildFilterExpression<TEntity, TQuery>(queryParameters);
        
        if (filterExpression != null)
        {
            query = query.Where(filterExpression);
        }

        return query;
    }

    public static IQueryable<TEntity> ApplySearch<TEntity, TQuery>(
        this IQueryable<TEntity> query,
        TQuery queryParameters)
        where TQuery : BaseQueryParameters
    {
        var searchExpression = FilterExpressionBuilder.BuildSearchExpression<TEntity, TQuery>(queryParameters);
        
        if (searchExpression != null)
        {
            query = query.Where(searchExpression);
        }

        return query;
    }

    public static IQueryable<TEntity> ApplySort<TEntity, TQuery>(
        this IQueryable<TEntity> query,
        TQuery queryParameters)
        where TQuery : BaseQueryParameters
    {
        if (string.IsNullOrWhiteSpace(queryParameters.SortBy))
            return query;

        var sortableFields = FilterExpressionBuilder.GetSortableFields<TQuery>();
        
        if (!sortableFields.Contains(queryParameters.SortBy, StringComparer.OrdinalIgnoreCase))
        {
            throw new FilterValidationException("SortBy", $"Field '{queryParameters.SortBy}' is not sortable. Allowed fields: {string.Join(", ", sortableFields)}");
        }

        var entityType = typeof(TEntity);
        var property = entityType.GetProperty(queryParameters.SortBy, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        
        if (property == null)
        {
            throw new FilterValidationException("SortBy", $"Property '{queryParameters.SortBy}' not found on entity type '{entityType.Name}'");
        }

        var parameter = Expression.Parameter(entityType, "x");
        var propertyAccess = Expression.Property(parameter, property);
        var lambda = Expression.Lambda(propertyAccess, parameter);

        var methodName = queryParameters.SortOrder?.ToLower() == "desc" ? "OrderByDescending" : "OrderBy";
        var orderByMethod = typeof(Queryable).GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(entityType, property.PropertyType);

        return (IQueryable<TEntity>)orderByMethod.Invoke(null, new object[] { query, lambda })!;
    }

    public static async Task<PagedResult<TEntity>> ToPagedResultAsync<TEntity>(
        this IQueryable<TEntity> query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TEntity>(items, totalCount, pageNumber, pageSize);
    }

    public static async Task<PagedResult<TEntity>> ApplyPaginationAsync<TEntity, TQuery>(
        this IQueryable<TEntity> query,
        TQuery queryParameters,
        CancellationToken cancellationToken = default)
        where TQuery : BaseQueryParameters
    {
        return await query.ToPagedResultAsync(
            queryParameters.PageNumber,
            queryParameters.PageSize,
            cancellationToken);
    }
}
