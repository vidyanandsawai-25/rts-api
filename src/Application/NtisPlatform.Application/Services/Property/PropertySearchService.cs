using System;
using System.Collections.Generic;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Core.Enums;
using NtisPlatform.Core.Interfaces.Property;
using NtisPlatform.Core.Models;

namespace NtisPlatform.Application.Services.Property;

/// <summary>
/// Use-case service for the Property Search screen. Owns the amount-filter validation rules and
/// the query-parameter -&gt; request mapping / paged-result shaping (including the PageSize == -1
/// "return all" normalization); the actual querying is delegated to
/// <see cref="IPropertySearchRepository"/>.
/// </summary>
public class PropertySearchService : IPropertySearchService
{
    private readonly IPropertySearchRepository _repository;

    public PropertySearchService(IPropertySearchRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<PropertySearchResponseDto>> SearchPropertiesAsync(PropertySearchQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        // Validate Values & Dues filters
        ValidateValuesAndDuesFilters(queryParameters);

        // Map query parameters to repository request DTO
        var searchRequest = new PropertySearchRequestDto
        {
            DashboardFilter = queryParameters.DashboardFilter,
            PropertyProcessFilter = queryParameters.PropertyProcessFilter,
            PropertyTypeId = queryParameters.PropertyTypeId,
            TypeOfUseId = queryParameters.TypeOfUseId,
            ZoneId = queryParameters.ZoneId,
            WardId = queryParameters.WardId,
            CategoryId = queryParameters.CategoryId,
            PropertyNoFrom = queryParameters.PropertyNoFrom,
            PropertyNoTo = queryParameters.PropertyNoTo,
            OldPropertyNo = queryParameters.OldPropertyNo,
            UPICId = queryParameters.UPICId,
            CSN = queryParameters.CSN,
            SubZoneNo = queryParameters.SubZoneNo,
            PlotNo = queryParameters.PlotNo,
            PropertyAssessmentStatusId = queryParameters.PropertyAssessmentStatusId,
            WorkflowStageId = queryParameters.WorkflowStageId,
            PropertyDescriptionId = queryParameters.PropertyDescriptionId,
            MobileNo = queryParameters.MobileNo,
            OwnerName = queryParameters.OwnerName,
            OccupierName = queryParameters.OccupierName,
            FlatOrShopName = queryParameters.FlatOrShopName,
            SocietyName = queryParameters.SocietyName,
            Address = queryParameters.Address,
            // Values & Dues filters
            ValuationMethod = queryParameters.ValuationMethod,
            FilterType = queryParameters.FilterType,
            AmountValue = queryParameters.AmountValue,
            AmountTo = queryParameters.AmountTo,
            TopCount = queryParameters.TopCount
        };

        var (totalCount, items) = await _repository.SearchPropertiesAsync(
            searchRequest,
            queryParameters.PageNumber,
            queryParameters.PageSize,
            cancellationToken);

        var pageNumber = queryParameters.PageNumber;
        var pageSize = queryParameters.PageSize;

        if (queryParameters.PageSize == -1)
        {
            pageNumber = 1;
            pageSize = Math.Max(1, totalCount);
        }

        return new PagedResult<PropertySearchResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public Task<PropertyDashboardStatsDto> GetPropertyDashboardStatsAsync(CancellationToken cancellationToken = default)
        => _repository.GetPropertyDashboardStatsAsync(cancellationToken);

    public Task<MainCardsResponseDto> GetMainCardsAsync(
        PropertySearchRequestDto? searchRequest = null,
        CancellationToken cancellationToken = default)
        => _repository.GetMainCardsAsync(searchRequest, cancellationToken);

    public Task<List<WorkflowStageCardDto>> GetWorkflowCardsAsync(
        PropertySearchRequestDto? searchRequest = null,
        CancellationToken cancellationToken = default)
        => _repository.GetWorkflowCardsAsync(searchRequest, cancellationToken);

    public List<ScopeCategoryDto> GetScopeOptions(ScopeCategory? category)
    {
        var categories = category.HasValue
            ? new[] { category.Value }
            : Enum.GetValues<ScopeCategory>();

        return categories.Select(c => new ScopeCategoryDto
        {
            Id = (int)c,
            Name = c.ToString(),
            DisplayName = c.GetDisplayName(),
            Description = c.GetDescription(),
            Options = c.GetOptions()
        }).ToList();
    }

    public async Task<ApartmentUnitListResponseDto> GetApartmentUnitListAsync(int propertyId, PropertySearchRequestDto? searchRequest = null, CancellationToken cancellationToken = default)
    {
        if (searchRequest != null)
        {
            ValidateValuesAndDuesFilters(new PropertySearchQueryParameters
            {
                ValuationMethod = searchRequest.ValuationMethod,
                FilterType = searchRequest.FilterType,
                AmountValue = searchRequest.AmountValue,
                AmountTo = searchRequest.AmountTo,
                TopCount = searchRequest.TopCount
            });
        }

        return await _repository.GetApartmentUnitListAsync(propertyId, searchRequest, cancellationToken);
    }

    /// <summary>
    /// Validates Values & Dues filter parameters (ValuationMethod/FilterType/AmountValue/AmountTo/TopCount).
    /// Throws PropertyValidationException if invalid combinations are detected.
    /// </summary>
    private void ValidateValuesAndDuesFilters(PropertySearchQueryParameters queryParameters)
    {
        var valuationMethod = queryParameters.ValuationMethod?.Trim();
        var filterType = queryParameters.FilterType?.Trim();

        // If ValuationMethod is provided without FilterType, that's invalid
        if (!string.IsNullOrWhiteSpace(valuationMethod) && string.IsNullOrWhiteSpace(filterType))
        {
            throw new PropertyValidationException("FilterType is required when ValuationMethod is provided.");
        }

        // If FilterType is provided, validate it and its required parameters
        if (!string.IsNullOrWhiteSpace(filterType))
        {
            if (string.IsNullOrWhiteSpace(valuationMethod))
            {
                throw new PropertyValidationException("ValuationMethod is required when FilterType is provided.");
            }

            // Validate ValuationMethod - only RV and CV are allowed from PolicyConfiguration
            var validValuationMethods = new[] { "RV", "CV" };
            if (!validValuationMethods.Any(v => v.Equals(valuationMethod, StringComparison.OrdinalIgnoreCase)))
            {
                throw new PropertyValidationException($"Invalid ValuationMethod: '{valuationMethod}'. Valid values from PolicyConfiguration are: RV, CV");
            }

            // Validate FilterType value
            var validFilterTypes = new[] { "Exact Value", "More Than", "Less Than", "Between", "Top" };
            if (!validFilterTypes.Any(f => f.Equals(filterType, StringComparison.OrdinalIgnoreCase)))
            {
                throw new PropertyValidationException($"Invalid FilterType: '{filterType}'. Valid values are: Exact Value, More Than, Less Than, Between, Top");
            }

            // Validate parameters based on FilterType
            if (filterType.Equals("Top", StringComparison.OrdinalIgnoreCase))
            {
                if (!queryParameters.TopCount.HasValue || queryParameters.TopCount.Value <= 0)
                {
                    throw new PropertyValidationException("TopCount must be a positive integer when FilterType is 'Top'.");
                }
            }
            else
            {
                if (!queryParameters.AmountValue.HasValue)
                {
                    throw new PropertyValidationException($"AmountValue is required when FilterType is '{filterType}'.");
                }

                if (filterType.Equals("Between", StringComparison.OrdinalIgnoreCase))
                {
                    if (!queryParameters.AmountTo.HasValue)
                    {
                        throw new PropertyValidationException("AmountTo is required when FilterType is 'Between'.");
                    }
                    if (queryParameters.AmountValue.Value > queryParameters.AmountTo.Value)
                    {
                        throw new PropertyValidationException("AmountValue cannot be greater than AmountTo.");
                    }
                }
            }
        }
    }
}
