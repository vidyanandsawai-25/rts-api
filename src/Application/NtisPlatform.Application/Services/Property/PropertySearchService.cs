using System;
using System.Collections.Generic;
using NtisPlatform.Application.DTOs.Property;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Interfaces.Property;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Extensions;
using NtisPlatform.Core.Constants;
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
            ScopeType = c.GetScopeType(),
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

    public async Task<PagedResult<PropertySearchByCategoryResponseDto>> SearchByCategoryAsync(PropertySearchByCategoryQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        ValidateSearchByCategoryParameters(queryParameters);

        var searchRequest = MapToSearchByCategoryRequest(queryParameters);

        var (totalCount, items) = await _repository.SearchByCategoryAsync(
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

        return new PagedResult<PropertySearchByCategoryResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<List<int>> ResolvePropertyIdsByCategoryAsync(PropertySearchByCategoryQueryParameters queryParameters, CancellationToken cancellationToken = default)
    {
        ValidateSearchByCategoryParameters(queryParameters);

        var searchRequest = MapToSearchByCategoryRequest(queryParameters);

        return await _repository.GetPropertyIdsByCategoryAsync(searchRequest, cancellationToken);
    }

    private const int MaxSuggestionResults = 100;

    public Task<List<PropertySuggestionDto>> GetPropertySuggestionsAsync(
        int wardId, string? propertyNo, string? partitionNo, int maxResults = 20, CancellationToken cancellationToken = default)
    {
        if (wardId <= 0)
            throw new PropertyValidationException("WardId is required for property suggestions.");

        var clampedMaxResults = Math.Clamp(maxResults <= 0 ? 20 : maxResults, 1, MaxSuggestionResults);

        return _repository.GetPropertySuggestionsAsync(wardId, propertyNo, partitionNo, clampedMaxResults, cancellationToken);
    }

    private static PropertySearchByCategoryRequestDto MapToSearchByCategoryRequest(PropertySearchByCategoryQueryParameters queryParameters)
    {
        return new PropertySearchByCategoryRequestDto
        {
            SearchCategory = queryParameters.SearchCategory,
            ZoneId = queryParameters.ZoneId,
            WardId = queryParameters.WardId,
            PropertyNo = queryParameters.PropertyNo,
            PartitionNo = queryParameters.PartitionNo,
            PropertyFrom = queryParameters.PropertyFrom,
            PropertyTo = queryParameters.PropertyTo,
            PartType = queryParameters.PartType,
            PropertyCategoryName = queryParameters.PropertyCategoryName,
            PropertyAssessmentStatusId = queryParameters.PropertyAssessmentStatusId,
            IsWing = queryParameters.IsWing,
            SearchTerm = queryParameters.SearchTerm
        };
    }

    /// <summary>
    /// Validates that the fields required by the selected SearchCategory are present and
    /// well-formed. Throws PropertyValidationException on failure.
    /// </summary>
    private static void ValidateSearchByCategoryParameters(PropertySearchByCategoryQueryParameters queryParameters)
    {
        if (!Enum.IsDefined(typeof(PropertySearchCategory), queryParameters.SearchCategory))
            throw new PropertyValidationException(PropertyConstants.SearchByCategory.ErrorMessages.InvalidSearchCategory);

        switch (queryParameters.SearchCategory)
        {
            case PropertySearchCategory.ZoneWise:
                if (!queryParameters.ZoneId.HasValue || queryParameters.ZoneId.Value <= 0)
                    throw new PropertyValidationException(PropertyConstants.SearchByCategory.ErrorMessages.ZoneIdRequired);
                break;

            case PropertySearchCategory.WardWise:
                if (!queryParameters.WardId.HasValue || queryParameters.WardId.Value <= 0)
                    throw new PropertyValidationException(PropertyConstants.SearchByCategory.ErrorMessages.WardIdRequired);
                break;

            case PropertySearchCategory.BuildingWise:
                if (!queryParameters.WardId.HasValue || queryParameters.WardId.Value <= 0)
                    throw new PropertyValidationException(PropertyConstants.SearchByCategory.ErrorMessages.WardIdRequired);
                if (string.IsNullOrWhiteSpace(queryParameters.PropertyNo))
                    throw new PropertyValidationException(PropertyConstants.SearchByCategory.ErrorMessages.PropertyNoRequired);
                break;

            case PropertySearchCategory.FromToProperty:
                if (!queryParameters.WardId.HasValue || queryParameters.WardId.Value <= 0)
                    throw new PropertyValidationException(PropertyConstants.SearchByCategory.ErrorMessages.WardIdRequired);

                if (string.IsNullOrWhiteSpace(queryParameters.PropertyFrom))
                    throw new PropertyValidationException(PropertyConstants.SearchByCategory.ErrorMessages.PropertyFromRequired);

                if (!TryParseLeadingPropertyNo(queryParameters.PropertyFrom).HasValue)
                    throw new PropertyValidationException(PropertyConstants.SearchByCategory.ErrorMessages.InvalidPropertyFromFormat);

                if (!string.IsNullOrWhiteSpace(queryParameters.PropertyTo) && !TryParseLeadingPropertyNo(queryParameters.PropertyTo).HasValue)
                    throw new PropertyValidationException(PropertyConstants.SearchByCategory.ErrorMessages.InvalidPropertyToFormat);

                break;
        }

        if (!string.IsNullOrWhiteSpace(queryParameters.PropertyAssessmentStatusId))
        {
            foreach (var token in FilterExpressionBuilder.Csv(queryParameters.PropertyAssessmentStatusId))
            {
                if (!int.TryParse(token, out _))
                    throw new PropertyValidationException(PropertyConstants.SearchByCategory.ErrorMessages.InvalidPropertyAssessmentStatusIdFormat);
            }
        }
    }

    /// <summary>
    /// Parses the numeric property-number segment from a "PropertyNo[-PartitionNo]" token
    /// (e.g. "1-A9" -> 1), mirroring the leading TRY_CONVERT(INT, ...) validation in the source query.
    /// </summary>
    private static int? TryParseLeadingPropertyNo(string token)
    {
        var dashIndex = token.IndexOf('-');
        var propertyPart = dashIndex < 0 ? token : token.Substring(0, dashIndex);
        return int.TryParse(propertyPart, out var parsed) ? parsed : null;
    }

    /// <summary>
    /// Validates Values and Dues filter parameters (ValuationMethod/FilterType/AmountValue/AmountTo/TopCount).
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

    public async Task<PagedResult<PropertySearchResponseDto>> UnifiedSearchPropertiesAsync(
        string query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new PagedResult<PropertySearchResponseDto>
            {
                Items = new List<PropertySearchResponseDto>(),
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        var (totalCount, items) = await _repository.UnifiedSearchPropertiesAsync(
            query.Trim(),
            pageNumber,
            pageSize,
            cancellationToken);

        return new PagedResult<PropertySearchResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
