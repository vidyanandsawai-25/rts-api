using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NtisPlatform.Application.Configuration;
using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.DTOs.CapitalValue;
using NtisPlatform.Application.Exceptions;
using NtisPlatform.Application.Interfaces.ICapitalValueService;
using NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService;
using NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService.Calculation;
using NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService.Data;
using NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService.Persistence;
using NtisPlatform.Application.Mappings; 
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Application.Services.CapitalValue.Utils;


namespace NtisPlatform.Application.Services.CapitalValue
{

    public class CapitalValueService : ICapitalValueService
    {
        private readonly IPropertyTaxCalculationCVResultsService _cvResultsService;
        private readonly IPolicyTaxDetailsService _policyTaxService;
        private readonly ITransMastService _transMastService;
        private readonly IPropertyDataLoader _propertyDataLoader;
        private readonly ICapitalValueMasterDataProvider _masterDataProvider;
        private readonly ICapitalValueCalculator _calculator;
        private readonly ICapitalValuePersistenceService _persistenceService;
        private readonly IUnitOfWork _unitOfWork;

        private readonly IMapper _mapper;
        private readonly CapitalValueOptions _options;
        private readonly ILogger<CapitalValueService> _logger;

        public CapitalValueService(
            IPropertyTaxCalculationCVResultsService cvResultsService,
            IPolicyTaxDetailsService policyTaxService,
            ITransMastService transMastService,
            IPropertyDataLoader propertyDataLoader,
            ICapitalValueMasterDataProvider masterDataProvider,
            ICapitalValueCalculator calculator,
            ICapitalValuePersistenceService persistenceService,
            IUnitOfWork unitOfWork,

            IMapper mapper,
            IOptions<CapitalValueOptions> options,
            ILogger<CapitalValueService> logger)
        {
            _cvResultsService = cvResultsService;
            _policyTaxService = policyTaxService;
            _transMastService = transMastService;
            _propertyDataLoader = propertyDataLoader;
            _masterDataProvider = masterDataProvider;
            _calculator = calculator;
            _persistenceService = persistenceService;
            _unitOfWork = unitOfWork;

            _mapper = mapper;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<List<CapitalValueDto>> GetAsync(int propertyId, CancellationToken cancellationToken = default)
        {
         
             // Step 1: Fetch all active PropertyDetails for this PropertyId
            var allActivePropertyDetails = await _propertyDataLoader.LoadPropertyDetailsAsync(propertyId, null, cancellationToken);
             if (!allActivePropertyDetails.Any())
             {
                 throw new PropertyDetailsNotFoundException(propertyId);
             }

            // Load property-level data needed for hash generation
            var property = await _propertyDataLoader.LoadPropertyAsync(propertyId, cancellationToken);
            var hasLift = await _propertyDataLoader.LoadLiftFlagAsync(propertyId, cancellationToken);
            var activePropertyDetailsIds = allActivePropertyDetails.Select(pd => pd.Id).ToList();

            // Step 2 & 2.5: Fetch CV results and detect changes using hash comparison
            var existingCVResults = new List<PropertyTaxCalculationCVResultsDto>();
            var PDIds = new List<int>();
            var hasAnyHashChanged = false;

            foreach (var propertyDetailsId in activePropertyDetailsIds)
            {
                var propertyDetail = allActivePropertyDetails.First(pd => pd.Id == propertyDetailsId);

                // Fetch CV results for this specific PropertyDetailsId
                var cvResultsForPropertyDetail = await _cvResultsService.GetByPropertyDetailsIdAsync(propertyDetailsId, cancellationToken);

                if (cvResultsForPropertyDetail.Any())
                {
                    // Step 2.5: Check if property details have changed using hash comparison
                    var existingHash = await _cvResultsService.GetCVInputHashAsync(propertyDetailsId, cancellationToken);
                    var currentHash =  CVInputHashGenerator.GenerateHash( propertyDetail, hasLift, property.MoujaId ?? 0, property.CSN ?? string.Empty);

                    if (existingHash != currentHash)
                    {
                          // Deactivate existing CV records - they will be recalculated
                        await _cvResultsService.DeactivateByPropertyDetailsIdAsync(propertyDetailsId, null, cancellationToken);
                        PDIds.Add(propertyDetailsId);
                        hasAnyHashChanged = true;
                    }
                    else
                    {
                        // No changes - use existing CV records
                        existingCVResults.AddRange(cvResultsForPropertyDetail);
                     }
                }
                else
                {
                    // No CV records found for this PropertyDetailsId - mark for calculation
                    PDIds.Add(propertyDetailsId);
                 }
            }

            // Step 2.6: If any PropertyDetails hash changed, soft delete PolicyTaxDetailsCV and TransMastCV
            // These are property-level aggregated records that need to be recreated with new totals
            if (hasAnyHashChanged)
            {
                await _policyTaxService.DeactivateByPropertyIdAsync(propertyId, null, cancellationToken);
                await _transMastService.DeactivateByPropertyIdAsync(propertyId, null, cancellationToken);
            }

            // Step 3: Auto-calculate for missing or changed PropertyDetailsIds if enabled
            if (PDIds.Any() && _options.AutoCalculateIfNotExists)
            {
                // Calculate CV for each PropertyDetailsId requiring calculation
                foreach (var propertyDetailsId in PDIds)
                {
                    await CreateAsync(new CreateCapitalValueDto { PropertyId = propertyId, PropertyDetailsId = propertyDetailsId }, cancellationToken);
                     // Fetch the newly created CV records
                    var newCVResults = await _cvResultsService.GetByPropertyDetailsIdAsync(propertyDetailsId, cancellationToken);
                    existingCVResults.AddRange(newCVResults);
                }

                // After calculating entries, recalculate and update aggregated totals
                // This ensures PolicyTaxDetailsCV and TransMastCV reflect the complete property totals
                await RecalculateAggregatedTotalsAsync(propertyId, cancellationToken);
            }

            // Step 4: Build DTOs - include blank DTOs for PropertyDetails with TypeOfUseGroupCVCode = "N"
            var result = BuildCapitalValueDtos(existingCVResults, allActivePropertyDetails, propertyId);
            return result;
        }

        /// <summary>
        /// Creates or updates capital value calculations for a property.
         /// </summary>
        public async Task<List<CapitalValueDto>> CreateAsync(CreateCapitalValueDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                // Step 1: Load all necessary data in as few calls as possible to optimize performance
                var property = await _propertyDataLoader.LoadPropertyAsync(dto.PropertyId, cancellationToken);
                var propertyDetailsList = await _propertyDataLoader.LoadPropertyDetailsAsync(dto.PropertyId, dto.PropertyDetailsId, cancellationToken);

                // Early check: Identify property details with TypeOfUseGroupCVCode = "N" to skip CV calculation
                var skippedCount = 0;
                var processableCount = 0;

                foreach (var pd in propertyDetailsList)
                {
                    var typeOfUseGroupCVCode = pd.TypeOfUse?.TypeOfUseGroupCV?.TypeOfUseGroupCVCode;
                    if (typeOfUseGroupCVCode == "N")
                    {
                        skippedCount++;
                    }
                    else
                    {
                        processableCount++;
                    }
                }

                // If all property details have TypeOfUseGroupCVCode = "N", create blank DTOs without loading master data
                if (processableCount == 0)
                {
                     // Use AutoMapper to create blank DTOs - cleaner and more maintainable
                    var blankResults = propertyDetailsList .Select(pd =>  CreateBlankCapitalValueDto(pd, dto.PropertyId, _mapper)) .ToList();
                     return blankResults;
                }

                // Load master data only if there are property details to process
                var masterData = await _masterDataProvider.LoadMasterDataAsync(property.MoujaId!.Value, property.CSN ?? string.Empty, cancellationToken);
                var hasLift = await _propertyDataLoader.LoadLiftFlagAsync(dto.PropertyId, cancellationToken);
                var financeYear = await _propertyDataLoader.LoadFinanceYearAsync(dto.FinanceYear, cancellationToken);

                // Step 2: Check for existing CV records to determine if we need to calculate or update, and to prepare for bulk insert (avoid duplicates)
                var existingCVs = await _cvResultsService.GetByPropertyIdAsync(dto.PropertyId, cancellationToken);
                var existingCVKeys = existingCVs.Select(x => (x.PropertyDetailsId, x.TaxId)).ToHashSet();
                Dictionary<int, PolicyTaxDetailsDto> existingPolicies = new();
                Dictionary<(int PropertyId, int FinanceYearId, int TaxId), TransMastDto> existingTransMast = new();

                if (dto.PropertyDetailsId == 0)
                {
                    var policies = await _policyTaxService.GetByPropertyIdAsync(dto.PropertyId, cancellationToken);
                    existingPolicies = policies.GroupBy(p => p.TaxId).ToDictionary(g => g.Key, g => g.First());
                    var transMasts = await _transMastService.GetByPropertyIdAsync(dto.PropertyId, cancellationToken);
                    existingTransMast = transMasts.GroupBy(t => (t.PropertyId, t.FinanceYearId, t.TaxId)).ToDictionary(g => g.Key, g => g.First());
                }

                // Step 3: Calculate CV for each PropertyDetails Logic Start
                var results = new List<CapitalValueDto>();
                var aggregatedTaxes = new Dictionary<int, (decimal TotalTax, decimal TotalCV)>();
                var cvResultsToCreate = new List<CreatePropertyTaxCalculationCVResultsDto>();

                foreach (var pd in propertyDetailsList)
                {
                    var typeOfUseGroupCVCode = pd.TypeOfUse?.TypeOfUseGroupCV?.TypeOfUseGroupCVCode;

                    // Validate navigation properties are loaded
                    if (pd.TypeOfUse == null)
                    {
                        throw new InvalidPropertyDataException("TypeOfUse", null, pd.Id);
                    }

                    if (pd.TypeOfUse.TypeOfUseGroupCV == null)
                    {
                        throw new TypeOfUseGroupNotFoundException(pd.Id, pd.TypeOfUseId);
                    }

                    // For PropertyDetails with TypeOfUseGroupCVCode = "N", create blank DTO with only descriptive information
                    if (typeOfUseGroupCVCode == "N")
                    {
                        _logger.LogDebug( "Creating blank CV DTO for PropertyDetailsId: {PropertyDetailsId} - TypeOfUseGroupCVCode is 'N'.", pd.Id);
                         // Use AutoMapper to create blank DTO - cleaner and more maintainable
                        var blankDto = CreateBlankCapitalValueDto(pd, dto.PropertyId, _mapper);
                        results.Add(blankDto);
                        continue; // Skip calculation and database insertion for this item
                    }

                     var calcResult = _calculator.Calculate(pd, masterData, hasLift, dto.PropertyId, property.MoujaId!.Value, property.CSN ?? string.Empty);

                    // Generate CV input hash for change detection
                    var cvInputHash = CVInputHashGenerator.GenerateHash( pd, hasLift, property.MoujaId ?? 0, property.CSN ?? string.Empty);

                    BuildCVResultEntities(calcResult.Result, calcResult.RateMaster, masterData.TaxTotalHead, existingCVKeys, cvResultsToCreate, dto, calcResult.FloorFactorEntity, calcResult.AgeFactorEntity, calcResult.NatureFactorEntity, calcResult.UseFactorEntity, cvInputHash);
                    results.Add(calcResult.Result);
                    AggregateTaxes(calcResult.Result, masterData.TaxTotalHead, aggregatedTaxes);
                }
                //Calculate CV for each PropertyDetails Logic End
                 // Step 4: Insert Data Into the PropertyTaxCalculationCVResults table using bulk insert for efficiency
                // Skip insertion if no CV results were calculated (all PropertyDetails had TypeOfUseGroupCVCode = "N")
                if (cvResultsToCreate.Any())
                {
                    await _persistenceService.PersistCVResultsAsync(cvResultsToCreate, cancellationToken);
                }
                else
                {
                    _logger.LogInformation( "No CV results to persist for PropertyId: {PropertyId} - all PropertyDetails had TypeOfUseGroupCVCode = 'N'",  dto.PropertyId);
                }

                // Step 5: Insert Data Into Policy and TransMast tables for aggregated reporting at property level, only if this is a full property calculation (PropertyDetailsId = 0)
                // Skip if no aggregated taxes (happens when all PropertyDetails had TypeOfUseGroupCVCode = "N")
                if (dto.PropertyDetailsId == 0 && aggregatedTaxes.Any())
                {
                    await _persistenceService.PersistAggregatedDataAsync(dto.PropertyId,financeYear,aggregatedTaxes, existingPolicies,existingTransMast,dto.PolicyCode ?? _options.DefaultPolicyCode,dto.PolicyDate ?? DateTime.Now,dto.PolicyYear ?? financeYear.Year,dto.PolicyReason,dto.CreatedBy ?? 0,cancellationToken);
                }

                 return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating CV for PropertyId: {PropertyId}", dto.PropertyId); 
                throw;
            }
        }
    

        //  <summary>
        // converts CapitalValueDto to a list of CreatePropertyTaxCalculationCVResultsDto for database insertion.
        // Now stores factor IDs instead of factor values.
        //  </summary>
        private void BuildCVResultEntities( CapitalValueDto cvDto,  RateMasterForCVEntity rateMaster,  TaxMasterEntity taxTotalHead,  HashSet<(int PropertyDetailsId, int TaxId)> existingCVKeys,  List<CreatePropertyTaxCalculationCVResultsDto> cvResultsToCreate,  CreateCapitalValueDto dto, FloorFactorCVMasterEntity? floorFactorEntity, AgeFactorCVMasterEntity? ageFactorEntity, NatureFactorCVMasterEntity? ntbFactorEntity, UseFactorCVMasterEntity? useFactorEntity, string cvInputHash)
        {

             // Build CV result entities for each tax
            foreach (var tax in cvDto.Taxes)
            {
                var key = (cvDto.PropertyDetailsId!.Value, tax.TaxId!.Value);

                if (!existingCVKeys.Contains(key))
                {
                    cvResultsToCreate.Add(new CreatePropertyTaxCalculationCVResultsDto
                    {
                        PropertyId = dto.PropertyId,
                        PropertyDetailsId = cvDto.PropertyDetailsId.Value,
                        TaxId = tax.TaxId.Value,
                        TaxPercentage = tax.Percentage!.Value,
                        TaxAmount = tax.Amount!.Value,
                        RateCVMasterId = rateMaster.Id,
                        BaseValue = cvDto.BaseValue!.Value,
                        CapitalValue = cvDto.CapitalValue!.Value,
                         // Store factor IDs instead of values
                        FloorFactorCVId = floorFactorEntity?.Id,
                        AgeFactorCVId = ageFactorEntity?.Id,
                        NatureFactorCVId = ntbFactorEntity?.Id,
                        UseFactorCVId = useFactorEntity?.Id,
                        CVInputHash = cvInputHash, // Store hash for change detection
                        CreatedBy = dto.CreatedBy,
                        IsActive=true,
                        CreatedDate= DateTime.Now
                    });
                    existingCVKeys.Add(key);
                }
            }

            // Add TaxTotal record
            var taxTotalKey = (cvDto.PropertyDetailsId!.Value, taxTotalHead.Id);
            if (!existingCVKeys.Contains(taxTotalKey))
            {
                decimal propertyDetailsTaxTotal = cvDto.Taxes.Sum(t => t.Amount!.Value);
                cvResultsToCreate.Add(new CreatePropertyTaxCalculationCVResultsDto
                {
                    PropertyId = dto.PropertyId,
                    PropertyDetailsId = cvDto.PropertyDetailsId.Value,
                    TaxId = taxTotalHead.Id,
                    TaxPercentage = 0,
                    TaxAmount = propertyDetailsTaxTotal,
                    RateCVMasterId = rateMaster.Id,
                    BaseValue = cvDto.BaseValue!.Value,
                    CapitalValue = cvDto.CapitalValue!.Value,

                    // Store factor IDs instead of values
                    FloorFactorCVId = floorFactorEntity?.Id,
                    AgeFactorCVId = ageFactorEntity?.Id,
                    NatureFactorCVId = ntbFactorEntity?.Id,
                    UseFactorCVId = useFactorEntity?.Id,
                    CVInputHash = cvInputHash, // Store hash for change detection
                    CreatedBy = dto.CreatedBy,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                });
                existingCVKeys.Add(taxTotalKey);
            }
        }

        /// <summary>
        /// Builds CapitalValueDto from CV results retrieved from database.
        /// For PropertyDetails with TypeOfUseGroupCVCode = "N" (no CV results), creates blank DTOs.
        /// </summary>
        private List<CapitalValueDto> BuildCapitalValueDtos(List<PropertyTaxCalculationCVResultsDto> cvResults, List<PropertyDetailsEntity> allPropertyDetails, int propertyId)
        {
            var result = new List<CapitalValueDto>();
            var processedPropertyDetailsIds = new HashSet<int>();

            // Create a dictionary for O(1) lookup of PropertyDetails by Id
            var propertyDetailsLookup = allPropertyDetails.ToDictionary(pd => pd.Id);

            // Process PropertyDetails that have CV results
            foreach (var group in cvResults.GroupBy(cv => cv.PropertyDetailsId))
            {
                var firstCv = group.First();

                // Use the already-loaded allPropertyDetails instead of reloading from database
                if (!propertyDetailsLookup.TryGetValue(firstCv.PropertyDetailsId, out var pd))
                    continue;

                var dto = _mapper.Map<CapitalValueDto>(pd);
                dto.PropertyId = firstCv.PropertyId;
                dto.CapitalValue = firstCv.CapitalValue ?? 0;
                dto.FloorFactor = firstCv.FloorFactor;
                dto.UseFactor = firstCv.UseFactor ?? 1;
                dto.NTBFactor = firstCv.NTBFactor ?? 1;
                dto.AgeFactor = firstCv.AgeFactor ?? 1;
                dto.BaseValue = firstCv.BaseValue ?? 0;
                dto.FloorDescription = pd.Floor?.Description;
                dto.ConstructionTypeDescription = pd.ConstructionType?.Description;
                dto.TypeOfUseDescription = pd.TypeOfUse?.Description;
                dto.SubTypeOfUseDescription = pd.SubTypeOfUse?.Description;
                dto.SubFloorDescription = pd.SubFloor?.Description;

                dto.SDRR = pd.CarpetAreaSqMeter.HasValue && pd.CarpetAreaSqMeter.Value > 0 && firstCv.BaseValue.HasValue
                    ? firstCv.BaseValue.Value / pd.CarpetAreaSqMeter.Value
                    : 0;

                dto.Taxes = group.Select(cv => new TaxHeadDto
                {
                    TaxId = cv.TaxId,
                    TaxName = cv.TaxName,
                    Percentage = cv.TaxPercentage ?? 0,
                    Amount = cv.TaxAmount ?? 0
                }).ToList();

                result.Add(dto);
                processedPropertyDetailsIds.Add(pd.Id);
            }

            // Process PropertyDetails with no CV results (TypeOfUseGroupCVCode = "N")
            // Create blank DTOs for these
            foreach (var pd in allPropertyDetails.OrderBy(x => x.Id))
            {
                if (!processedPropertyDetailsIds.Contains(pd.Id))
                {
                    // Check if this is a code "N" property detail
                    var typeOfUseGroupCVCode = pd.TypeOfUse?.TypeOfUseGroupCV?.TypeOfUseGroupCVCode;

                    if (typeOfUseGroupCVCode == "N")
                    {
                        var blankDto = CreateBlankCapitalValueDto(pd, propertyId, _mapper);
                        result.Add(blankDto);
                        processedPropertyDetailsIds.Add(pd.Id);
                    }
                 }
            }

            // Ensure result is ordered by PropertyDetailsId
            return result.OrderBy(x => x.PropertyDetailsId).ToList();
        }

        /// <summary>
        /// Aggregates tax amounts across property details for property-level reporting.
        /// </summary>
        private void AggregateTaxes(CapitalValueDto cvDto, TaxMasterEntity taxTotalHead, Dictionary<int, (decimal, decimal)> aggregated)
        {
            foreach (var tax in cvDto.Taxes)
            {
                if (aggregated.ContainsKey(tax.TaxId!.Value))
                {
                    var existing = aggregated[tax.TaxId.Value];
                    aggregated[tax.TaxId.Value] = (existing.Item1 + tax.Amount!.Value, existing.Item2 + cvDto.CapitalValue!.Value);
                }
                else
                {
                    aggregated[tax.TaxId.Value] = (tax.Amount!.Value, cvDto.CapitalValue!.Value);
                }
            }

            decimal propertyDetailsTaxTotal = cvDto.Taxes.Sum(t => t.Amount!.Value);

            if (aggregated.ContainsKey(taxTotalHead.Id))
            {
                var existing = aggregated[taxTotalHead.Id];
                aggregated[taxTotalHead.Id] = (existing.Item1 + propertyDetailsTaxTotal, existing.Item2 + cvDto.CapitalValue!.Value);
            }
            else
            {
                aggregated[taxTotalHead.Id] = (propertyDetailsTaxTotal, cvDto.CapitalValue!.Value);
            }
        }

        /// <summary>
        /// Recalculates and updates totals in PolicyTaxDetailsCV and TransMastCV.
        /// Called after backfilling missing PropertyDetailsId entries to ensure totals are correct.
        /// </summary>
        private async Task RecalculateAggregatedTotalsAsync(int propertyId, CancellationToken cancellationToken)
        {
            try
            {
                // Load all CV results for the property
                var allCVResults = await _cvResultsService.GetByPropertyIdAsync(propertyId, cancellationToken);

                if (!allCVResults.Any())
                {
                     return;
                }

                // Load master data for TaxTotalHead
                var property = await _propertyDataLoader.LoadPropertyAsync(propertyId, cancellationToken);
                var masterData = await _masterDataProvider.LoadMasterDataAsync( property.MoujaId!.Value, property.CSN ?? string.Empty, cancellationToken);

                var financeYear = await _propertyDataLoader.LoadFinanceYearAsync(null, cancellationToken);

                // Group CV results by PropertyDetailsId and Calculated
                var aggregatedTaxes = new Dictionary<int, (decimal TotalTax, decimal TotalCV)>();

                foreach (var group in allCVResults.GroupBy(cv => cv.PropertyDetailsId))
                {
                    // Sum up taxes for this PropertyDetailsId (excluding TaxTotal)
                    var propertyDetailTaxes = group.Where(cv => cv.TaxId != masterData.TaxTotalHead.Id);
                    var propertyDetailTotal = group.FirstOrDefault(cv => cv.TaxId == masterData.TaxTotalHead.Id);

                    if (propertyDetailTotal != null)
                    {
                        // Calculate each individual tax
                        foreach (var cvResult in propertyDetailTaxes)
                        {
                            var taxAmount = cvResult.TaxAmount ?? 0;
                            var capitalValue = cvResult.CapitalValue ?? 0;

                            if (aggregatedTaxes.ContainsKey(cvResult.TaxId))
                            {
                                var existing = aggregatedTaxes[cvResult.TaxId];
                                aggregatedTaxes[cvResult.TaxId] = (existing.TotalTax + taxAmount, existing.TotalCV + capitalValue);
                            }
                            else
                            {
                                aggregatedTaxes[cvResult.TaxId] = (taxAmount, capitalValue);
                            }
                        }

                        // Calculate totals for TaxTotal 
                        var totalTaxAmount = propertyDetailTotal.TaxAmount ?? 0;
                        var totalCapitalValue = propertyDetailTotal.CapitalValue ?? 0;

                        if (aggregatedTaxes.ContainsKey(masterData.TaxTotalHead.Id))
                        {
                            var existing = aggregatedTaxes[masterData.TaxTotalHead.Id];
                            aggregatedTaxes[masterData.TaxTotalHead.Id] = (existing.TotalTax + totalTaxAmount, existing.TotalCV + totalCapitalValue);
                        }
                        else
                        {
                            aggregatedTaxes[masterData.TaxTotalHead.Id] = (totalTaxAmount, totalCapitalValue);
                        }
                    }
                }

                // Load existing PolicyTaxDetailsCV and TransMastCV for update
                var existingPolicies = await _policyTaxService.GetByPropertyIdAsync(propertyId, cancellationToken);
                var existingPoliciesDict = existingPolicies .GroupBy(p => p.TaxId) .ToDictionary(g => g.Key, g => g.First());
                var existingTransMast = await _transMastService.GetByPropertyIdAsync(propertyId, cancellationToken);
                var existingTransMastDict = existingTransMast.ToDictionary(t => (t.PropertyId, t.FinanceYearId, t.TaxId), t => t);

                // Update PolicyTaxDetailsCV and TransMastCV with new aggregated totals
                await _persistenceService.PersistAggregatedDataAsync( propertyId, financeYear, aggregatedTaxes, existingPoliciesDict, existingTransMastDict, _options.DefaultPolicyCode, DateTime.Now, financeYear.Year, "", 0, cancellationToken);
 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error recalculating aggregated totals for PropertyId: {PropertyId}",
                    propertyId);
                throw;
            }
        }

        /// <summary>
        /// Creates a blank CapitalValueDto from PropertyDetailsEntity.
        /// Used for PropertyDetails with TypeOfUseGroupCVCode = "N" where no calculation is performed.
        /// All calculation fields are set to 0, only descriptive information is populated.
        /// </summary>
        public static CapitalValueDto CreateBlankCapitalValueDto(PropertyDetailsEntity propertyDetails, int propertyId, IMapper mapper)
        {
            var dto = mapper.Map<CapitalValueDto>(propertyDetails);
            dto.PropertyId = propertyId;
            dto.CapitalValue = 0;
            dto.BaseValue = 0;
            dto.FloorFactor = 0;
            dto.SDRR = 0;
            dto.UseFactor = 0;
            dto.NTBFactor = 0;
            dto.AgeFactor = 0;
            dto.YearRangeCVId = null;
            dto.Taxes = new List<TaxHeadDto>();

            return dto;
        }


    }
}
