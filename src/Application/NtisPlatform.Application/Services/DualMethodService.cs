using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.DualMethod;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NtisPlatform.Application.Services
{
    /// <summary>
    /// Service for retrieving dual method tax data (CV, RV, and Old taxes)
    /// Follows clean architecture patterns with AutoMapper projections and proper query optimization
    /// </summary>
    public class DualMethodService : IDualMethodService
    {
        private readonly IRepository<TransMastCVEntity, long> _transCVRepository;
        private readonly IRepository<TransMastRVEntity, long> _transRVRepository;
        private readonly IRepository<TransMastOldEntity, int> _oldTaxRepository;
        private readonly IRepository<PropertyEntity, int> _propertyRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<DualMethodService> _logger;

        public DualMethodService(
            IRepository<TransMastCVEntity, long> transCVRepository,
            IRepository<TransMastRVEntity, long> transRVRepository,
            IRepository<TransMastOldEntity, int> oldTaxRepository,
            IRepository<PropertyEntity, int> propertyRepository,
            IMapper mapper,
            ILogger<DualMethodService> logger)
        {
            _transCVRepository = transCVRepository ?? throw new ArgumentNullException(nameof(transCVRepository));
            _transRVRepository = transRVRepository ?? throw new ArgumentNullException(nameof(transRVRepository));
            _oldTaxRepository = oldTaxRepository ?? throw new ArgumentNullException(nameof(oldTaxRepository));
            _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Query CV tax data with proper filters and eager loading
        /// Uses AutoMapper ProjectTo for optimized SQL generation
        /// </summary>
        private IQueryable<TransMastCVEntity> QueryCVTaxesWithIncludes(int propertyId)
        {
            return _transCVRepository.GetQueryable()
                .Where(x => x.PropertyId == propertyId
                    && x.IsActive
                    && !x.MarkedForDeletion
                    && x.TaxId != 0
                    && x.TaxMaster != null
                    && x.TaxMaster.IsActive)
                .Include(x => x.TaxMaster)
                .AsNoTracking();
        }

        /// <summary>
        /// Query RV tax data with proper filters and eager loading
        /// Uses AutoMapper ProjectTo for optimized SQL generation
        /// </summary>
        private IQueryable<TransMastRVEntity> QueryRVTaxesWithIncludes(int propertyId)
        {
            return _transRVRepository.GetQueryable()
                .Where(x => x.PropertyId == propertyId
                    && x.IsActive
                    && !x.MarkedForDeletion
                    && x.TaxId != 0
                    && x.TaxMaster != null
                    && x.TaxMaster.IsActive)
                .Include(x => x.TaxMaster)
                .AsNoTracking();
        }

        /// <summary>
        /// Query old tax data with proper filters and eager loading
        /// Uses AutoMapper ProjectTo for optimized SQL generation
        /// </summary>
        private IQueryable<TransMastOldEntity> QueryOldTaxesWithIncludes(int propertyMastOldId)
        {
            return _oldTaxRepository.GetQueryable()
                .Where(x => x.PropertyMastOldId == propertyMastOldId
                    && x.IsActive
                    && !x.MarkedForDeletion
                    && x.TaxId != 0
                    && x.TaxMaster != null
                    && x.TaxMaster.IsActive)
                .Include(x => x.TaxMaster)
                .AsNoTracking();
        }

        /// <summary>
        /// Retrieves all tax data (CV, RV, Old) for a property in a single operation
        /// Executes the underlying queries sequentially to avoid concurrent DbContext usage
        /// </summary>
        public async Task<DualMethodDto> GetRVCVTaxesAsync(int propertyId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting dual method tax retrieval for PropertyId={PropertyId}", propertyId);

            try
            {
                // Get PropertyMastOldId for fetching old tax data
                var propertyMastOldId = await GetPropertyMastOldIdAsync(propertyId, cancellationToken);

                if (propertyMastOldId.HasValue)
                {
                    _logger.LogDebug("Found PropertyMastOldId={PropertyMastOldId} for PropertyId={PropertyId}", 
                        propertyMastOldId.Value, propertyId);
                }
                else
                {
                    _logger.LogDebug("No PropertyMastOldId found for PropertyId={PropertyId}, old taxes will be empty", 
                        propertyId);
                }

                // Execute queries sequentially to avoid concurrent DbContext usage
                var cvTaxes = await GetCVTaxDataAsync(propertyId, cancellationToken);
                var rvTaxes = await GetRVTaxDataAsync(propertyId, cancellationToken);
                var oldTaxes = await GetOldTaxDataAsync(propertyMastOldId, cancellationToken);

                _logger.LogInformation(
                    "Retrieved tax data for PropertyId={PropertyId}: CV={CVCount} taxes, RV={RVCount} taxes, Old={OldCount} taxes",
                    propertyId, cvTaxes.Count, rvTaxes.Count, oldTaxes.Count);

                // Build and return aggregated result
                var result = new DualMethodDto
                {
                    CVTaxes = BuildTaxDictionary(cvTaxes),
                    RVTaxes = BuildTaxDictionary(rvTaxes),
                    OldTaxes = BuildTaxDictionary(oldTaxes)
                };

                _logger.LogInformation(
                    "Completed dual method tax retrieval for PropertyId={PropertyId}: CVTotal={CVTotal}, RVTotal={RVTotal}, OldTotal={OldTotal}",
                    propertyId, 
                    result.CVTaxes.GetValueOrDefault(TaxConstants.TaxTotalKey, 0m),
                    result.RVTaxes.GetValueOrDefault(TaxConstants.TaxTotalKey, 0m),
                    result.OldTaxes.GetValueOrDefault(TaxConstants.TaxTotalKey, 0m));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error retrieving dual method taxes for PropertyId={PropertyId}", propertyId);
                throw;
            }
        }

        #region Private Query Methods

        /// <summary>
        /// Retrieves PropertyMastOldId for a given property
        /// Returns null if property not found or doesn't have old data
        /// </summary>
        private async Task<int?> GetPropertyMastOldIdAsync(int propertyId, CancellationToken cancellationToken)
        {
            try
            {
                var property = await _propertyRepository.GetQueryable()
                    .Where(p => p.Id == propertyId && p.IsActive &&
                        !p.MarkedForDeletion)
                    .AsNoTracking()
                    .Select(p => new { p.PropertyMastOldId })
                    .FirstOrDefaultAsync(cancellationToken);

                return property?.PropertyMastOldId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error retrieving PropertyMastOldId for PropertyId={PropertyId}", propertyId);
                throw;
            }
        }

       
        /// <summary>
        /// Retrieves CV tax data and projects to DTO using AutoMapper
        /// </summary>
        private async Task<List<TaxDataDto>> GetCVTaxDataAsync(int propertyId, CancellationToken cancellationToken)
        {
            try
            {
                var result = await QueryCVTaxesWithIncludes(propertyId)
                    .ProjectTo<TaxDataDto>(_mapper.ConfigurationProvider)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Retrieved {Count} CV tax records for PropertyId={PropertyId}", 
                    result.Count, propertyId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error retrieving CV tax data for PropertyId={PropertyId}", propertyId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves RV tax data and projects to DTO using AutoMapper
        /// </summary>
        private async Task<List<TaxDataDto>> GetRVTaxDataAsync(int propertyId, CancellationToken cancellationToken)
        {
            try
            {
                var result = await QueryRVTaxesWithIncludes(propertyId)
                    .ProjectTo<TaxDataDto>(_mapper.ConfigurationProvider)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Retrieved {Count} RV tax records for PropertyId={PropertyId}", 
                    result.Count, propertyId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error retrieving RV tax data for PropertyId={PropertyId}", propertyId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves old tax data and projects to DTO using AutoMapper
        /// Returns empty list if propertyMastOldId is null
        /// </summary>
        private async Task<List<TaxDataDto>> GetOldTaxDataAsync(int? propertyMastOldId, CancellationToken cancellationToken)
        {
            if (!propertyMastOldId.HasValue)
            {
                return new List<TaxDataDto>();
            }

            try
            {
                var result = await QueryOldTaxesWithIncludes(propertyMastOldId.Value)
                    .ProjectTo<TaxDataDto>(_mapper.ConfigurationProvider)
                    .ToListAsync(cancellationToken);

                _logger.LogDebug("Retrieved {Count} old tax records for PropertyMastOldId={PropertyMastOldId}", 
                    result.Count, propertyMastOldId.Value);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error retrieving old tax data for PropertyMastOldId={PropertyMastOldId}", propertyMastOldId.Value);
                throw;
            }
        }

        #endregion

        #region Private Business Logic Methods

        /// <summary>
        /// Builds a tax dictionary from tax data with proper aggregation and naming
        /// Handles:
        /// - Grouping by TaxId and summing amounts
        /// - Duplicate tax name resolution
        /// - Reserved keyword collision avoidance
        /// - Rounding to whole numbers
        /// - Total calculation
        /// </summary>
        private static Dictionary<string, decimal> BuildTaxDictionary(List<TaxDataDto> taxDataList)
        {
            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            if (taxDataList == null || !taxDataList.Any())
            {
                result[TaxConstants.TaxTotalKey] = 0m;
                return result;
            }

            // Group by TaxId and TaxName, sum amounts for same tax
            // Exclude the database aggregate TaxTotal record from individual taxes by semantic name
            var groupedTaxes = taxDataList
                .Where(x => !string.Equals(x.TaxName?.Trim(),
                    TaxConstants.TaxTotalKey, StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => new { x.TaxId, x.TaxName })
                .Select(g => new TaxDataDto
                {
                    TaxId = g.Key.TaxId,
                    TaxName = g.Key.TaxName,
                    TaxAmount = g.Sum(x => x.TaxAmount)
                })
                .ToList();

            // Build dictionary with proper key handling
            foreach (var taxData in groupedTaxes)
            {
                var key = ResolveUniqueTaxKey(result, taxData);
                var roundedAmount = Math.Round(taxData.TaxAmount, 0, MidpointRounding.AwayFromZero);
                result[key] = roundedAmount;
            }

            // Add total
            result[TaxConstants.TaxTotalKey] = result.Values.Sum();

            return result;
        }

        /// <summary>
        /// Resolves a unique key for a tax entry, handling:
        /// - Blank/null names (generates "Tax_{TaxId}")
        /// - Reserved keyword collision (adds "_Total" suffix)
        /// - Duplicate names (adds "_{TaxId}" suffix)
        /// </summary>
        private static string ResolveUniqueTaxKey(Dictionary<string, decimal> existingKeys, TaxDataDto taxData)
        {
            // Generate base key
            var baseKey = string.IsNullOrWhiteSpace(taxData.TaxName)
                ? $"Tax_{taxData.TaxId}"
                : taxData.TaxName.Trim();

            // Handle reserved keyword collision
            if (string.Equals(baseKey, TaxConstants.TaxTotalKey, StringComparison.OrdinalIgnoreCase))
            {
                baseKey = $"Tax_{taxData.TaxId}_Total";
            }

            // Handle duplicate names with robust collision avoidance
            if (existingKeys.ContainsKey(baseKey))
            {
                var candidateKey = $"{baseKey}_{taxData.TaxId}";
                if (!existingKeys.ContainsKey(candidateKey))
                {
                    return candidateKey;
                }

                // If even the TaxId-suffixed key collides, use a numeric loop suffix
                int suffix = 1;
                string uniqueKey;
                do
                {
                    uniqueKey = $"{candidateKey}_{suffix++}";
                } while (existingKeys.ContainsKey(uniqueKey));

                return uniqueKey;
            }

            return baseKey;
        }

        #endregion

        /// <summary>
        /// Constants for tax-related operations
        /// </summary>
        private static class TaxConstants
        {
            public const string TaxTotalKey = "TaxTotal";
        }
    }
}
