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
        // Values & Dues validation
        var op = queryParameters.AmountFilterOperator;
        var from = queryParameters.AmountValue;
        var to = queryParameters.AmountTo;
        var topCount = queryParameters.TopCount;

        // Validate AmountFilterOperator if provided
        if (!string.IsNullOrWhiteSpace(op))
        {
            var opTrimmed = op.Trim();

            // Check if operator is valid for tax filtering
            if (!Enum.TryParse<FilterOperator>(opTrimmed, ignoreCase: true, out var parsedOp) ||
                !Enum.IsDefined(typeof(FilterOperator), parsedOp) ||
                (parsedOp != FilterOperator.Equals &&
                 parsedOp != FilterOperator.GreaterThan &&
                 parsedOp != FilterOperator.LessThan &&
                 parsedOp != FilterOperator.Between &&
                 parsedOp != FilterOperator.Top))
            {
                throw new PropertyValidationException($"Invalid AmountFilterOperator value: '{opTrimmed}'. Valid values are: Equals, GreaterThan, LessThan, Between, Top");
            }

            // Validate required fields based on operator
            if (parsedOp == FilterOperator.Top)
            {
                if (!topCount.HasValue || topCount.Value <= 0)
                    throw new PropertyValidationException("TopCount must be a positive number when AmountFilterOperator is Top.");
            }
            else
            {
                if (!from.HasValue)
                    throw new PropertyValidationException($"AmountValue is required when AmountFilterOperator is '{opTrimmed}'.");

                if (parsedOp == FilterOperator.Between)
                {
                    if (!to.HasValue)
                        throw new PropertyValidationException("AmountTo is required when AmountFilterOperator is Between.");

                    if (from.Value > to.Value)
                        throw new PropertyValidationException("AmountValue cannot be greater than AmountTo.");
                }
            }
        }

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
            ValuationTypeFilter = queryParameters.ValuationTypeFilter,
            RVorCV = queryParameters.RVorCV,
            AmountFilterOperator = queryParameters.AmountFilterOperator,
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

    public Task<List<PropertySearchResponseDto>> GetApartmentUnitListAsync(int propertyId, CancellationToken cancellationToken = default)
        => _repository.GetApartmentUnitListAsync(propertyId, cancellationToken);
}
