using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.DTOs.CapitalValue;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Core.Constants;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;



namespace NtisPlatform.Application.Services
{
    public class CapitalValueService : ICapitalValueService
    {
        private readonly IRepository<PropertyTaxCalculationCVResultsEntity, long> _cvRepository;
        private readonly IRepository<RateMasterForCVEntity, int> _rateRepository;
        private readonly IRepository<NatureFactorCVMasterEntity, int> _natureFactorRepository;
        private readonly IRepository<UseFactorCVMasterEntity, int> _useFactorRepository;
        private readonly IRepository<AgeFactorCVMasterEntity, int> _ageFactorRepository;
        private readonly IRepository<FloorFactorCVMasterEntity, int> _floorFactorRepository;
        private readonly IRepository<PropertyEntity, int> _propertyRepository;
        private readonly IRepository<PropertyDetailsEntity, int> _propertyDetailsRepository;
        private readonly IRepository<FlagMasterEntity, int> _flagRepository;
         private readonly IRepository<AssessmentYearRangeCVEntity, int> _assessmentYearRangeRepository;
        private readonly IRepository<TaxPercentageMasterCVEntity, int> _taxPercentageRepository;
        private readonly IRepository<TaxMasterEntity, int> _taxMasterRepository;
        private readonly IRepository<CSNDetailsEntity, int> _CSNDetailsRepository;
        private readonly IRepository<PolicyTaxDetailsCVEntity, int> _policyTaxDetailsCVRepository;
        private readonly IRepository<TransMastCVEntity, int> _transMastCVRepository;  
        private readonly IRepository<YearMasterEntity, int> _yearMasterRepository;  

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CapitalValueService> _logger;

        public CapitalValueService(
            IRepository<PropertyTaxCalculationCVResultsEntity, long> cvRepository,
            IRepository<RateMasterForCVEntity, int> rateRepository,
            IRepository<NatureFactorCVMasterEntity, int> natureRepository,
            IRepository<UseFactorCVMasterEntity, int> useRepository,
            IRepository<AgeFactorCVMasterEntity, int> ageRepository,
            IRepository<FloorFactorCVMasterEntity, int> floorFactorRepository,
            IRepository<PropertyEntity, int> propertyRepository,
            IRepository<PropertyDetailsEntity, int> propertyDetailsRepository,
            IRepository<FlagMasterEntity, int> flagRepository,
            IRepository<AssessmentYearRangeCVEntity, int> assessmentYearRangeRepository,
            IRepository<TaxPercentageMasterCVEntity, int> taxPercentageRepository,
            IRepository<TaxMasterEntity, int> taxMasterRepository,
            IRepository<CSNDetailsEntity, int> CSNDetailsRepository,
            IRepository<PolicyTaxDetailsCVEntity, int> policyTaxDetailsCVRepository,
            IRepository<TransMastCVEntity, int> transMastCVRepository,  
            IRepository<YearMasterEntity, int> yearMasterRepository,  
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<CapitalValueService> logger
            )
        {
            _cvRepository = cvRepository;
            _rateRepository = rateRepository;
            _natureFactorRepository = natureRepository;
            _useFactorRepository = useRepository;
            _ageFactorRepository = ageRepository;
            _floorFactorRepository = floorFactorRepository;
            _propertyRepository = propertyRepository;
            _propertyDetailsRepository = propertyDetailsRepository;
            _unitOfWork = unitOfWork;
            _flagRepository = flagRepository;
             _assessmentYearRangeRepository = assessmentYearRangeRepository;
            _taxPercentageRepository = taxPercentageRepository;
            _taxMasterRepository = taxMasterRepository;
            _CSNDetailsRepository = CSNDetailsRepository;
            _policyTaxDetailsCVRepository = policyTaxDetailsCVRepository;
            _transMastCVRepository = transMastCVRepository;  
            _yearMasterRepository = yearMasterRepository;  
            _mapper = mapper;
            _logger = logger;

        }



    
        private IQueryable<PropertyDetailsEntity> QueryPropertyDetailsWithIncludes()
            => _propertyDetailsRepository.GetQueryable()
                .Where(x => x.IsActive && !x.MarkedForDeletion)
                .Include(x => x.Floor)
                .Include(x => x.SubFloor)
                .Include(x => x.ConstructionType)
                .Include(x => x.TypeOfUse!)
                    .ThenInclude(x => x.TypeOfUseGroup)
                .Include(x => x.SubTypeOfUse);

         
        private IQueryable<PropertyTaxCalculationCVResultsEntity> QueryCVWithIncludes()
            => _cvRepository.GetQueryable()
                .Where(x => x.IsActive)
                .Include(x => x.TaxMaster)
                .Include(x => x.RateCVMaster)
                .Include(x => x.PropertyDetails!)
                    .ThenInclude(pd => pd.Floor)
                .Include(x => x.PropertyDetails!)
                    .ThenInclude(pd => pd.SubFloor)
                .Include(x => x.PropertyDetails!)
                    .ThenInclude(pd => pd.ConstructionType)
                .Include(x => x.PropertyDetails!)
                    .ThenInclude(pd => pd.TypeOfUse)
                .Include(x => x.PropertyDetails!)
                    .ThenInclude(pd => pd.SubTypeOfUse);

        private IQueryable<AssessmentYearRangeCVEntity> QueryActiveYearRanges()
            => _assessmentYearRangeRepository.GetQueryable()
                .Where(x => x.IsActive);

        private IQueryable<TaxPercentageMasterCVEntity> QueryActiveTaxPercentages()
        => _taxPercentageRepository.GetQueryable()
          .Where(x => x.IsActive)
          .Include(x => x.TaxMaster)
          .Where(x => x.TaxMaster != null && x.TaxMaster.IsActive);


        public async Task<List<CapitalValueDto>> GetAsync( int propertyId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting capital value retrieval for PropertyId: {PropertyId}", propertyId);

            try
            {
                // Get all active PropertyDetailsIds for this property
                var allPdIds = await _propertyDetailsRepository.GetQueryable()
                    .Where(x => x.PropertyId == propertyId && !x.MarkedForDeletion && x.IsActive)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);

                if (!allPdIds.Any())
                {
                    _logger.LogWarning("Property with ID {PropertyId} not found or has no active property details", propertyId);
                    throw new InvalidOperationException(
                        $"Property with ID {propertyId} not found or has no active property details");
                }

                _logger.LogDebug("Found {Count} active property details for PropertyId: {PropertyId}", allPdIds.Count, propertyId);

                // Get existing CV records as (PropertyDetailsId, TaxId) combinations
                var existingCVCombinations = await _cvRepository.GetQueryable()
                    .Where(x => x.PropertyId == propertyId && x.IsActive)
                    .Select(x => new { x.PropertyDetailsId, x.TaxId })
                    .ToListAsync(cancellationToken);

                var existingPdIds = existingCVCombinations
                    .Select(x => x.PropertyDetailsId)
                    .Distinct()
                    .ToList();

                var missingPdIds = allPdIds
                    .Except(existingPdIds)
                    .ToList();

                if (missingPdIds.Any())
                {
                    _logger.LogInformation("Missing CV records for {Count} property details. Triggering CreateAsync for PropertyId: {PropertyId}", 
                        missingPdIds.Count, propertyId);

                    await CreateAsync(new CreateCapitalValueDto
                    {
                        PropertyId = propertyId
                    }, cancellationToken);
                }
                else
                {
                    var propertyDetails = await QueryPropertyDetailsWithIncludes()
                        .Where(x => x.PropertyId == propertyId)
                        .ToListAsync(cancellationToken);

                    if (propertyDetails.Any())
                    {
                        var yearRanges = await QueryActiveYearRanges()
                            .ToListAsync(cancellationToken);

                        var allTaxPercentages = await QueryActiveTaxPercentages()
                            .ToListAsync(cancellationToken);

                        var existingSet = existingCVCombinations
                            .Select(x => (x.PropertyDetailsId, x.TaxId))
                            .ToHashSet();

                        bool hasMissingTaxes = false;

                        foreach (var pd in propertyDetails)
                        {
                            if (!int.TryParse(pd.AssessmentYear, out int assessmentYear) || assessmentYear <= 0)
                                continue;

                            var yearRange = yearRanges.FirstOrDefault(x =>
                                assessmentYear >= x.FromYear &&
                                assessmentYear <= x.ToYear);

                            if (yearRange == null)
                                continue;

                            var expectedTaxIds = allTaxPercentages
                                .Where(x =>
                                    x.TypeOfUseId == pd.TypeOfUseId &&
                                    x.YearRangeCVId == yearRange.Id)
                                .Select(x => x.TaxId)
                                .Distinct()
                                .ToList();

                            if (expectedTaxIds.Any(taxId => !existingSet.Contains((pd.Id, taxId))))
                            {
                                hasMissingTaxes = true;
                                _logger.LogDebug("Missing tax records detected for PropertyDetailsId: {PropertyDetailsId}", pd.Id);
                                break;
                            }
                        }

                        if (hasMissingTaxes)
                        {
                            _logger.LogInformation("Missing tax records detected. Triggering CreateAsync for PropertyId: {PropertyId}", propertyId);

                            await CreateAsync(new CreateCapitalValueDto
                            {
                                PropertyId = propertyId
                            }, cancellationToken);
                        }
                    }
                }

                var data = await QueryCVWithIncludes()
                    .Where(x => x.PropertyId == propertyId)
                    .OrderBy(x => x.TaxId)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var result = data
                    .Where(cv => cv.PropertyDetails != null)
                    .GroupBy(cv => cv.PropertyDetailsId)
                    .Select(g =>
                    {
                        var firstCv = g.First();
                        var pd = firstCv.PropertyDetails!;

                        // Validate critical data integrity
                        if (!firstCv.CapitalValue.HasValue)
                        {
                            _logger.LogWarning("CapitalValue is null for PropertyDetailsId: {PropertyDetailsId}. This indicates incomplete calculation data.",
                                firstCv.PropertyDetailsId);
                        }

                        if (firstCv.RateCVMaster == null)
                        {
                            _logger.LogWarning("RateCVMaster is null for PropertyDetailsId: {PropertyDetailsId}. Rate information may be missing.",
                                firstCv.PropertyDetailsId);
                        }

                        var dto = _mapper.Map<CapitalValueDto>(pd);

                        dto.PropertyId = firstCv.PropertyId;
                        dto.CapitalValue = firstCv.CapitalValue ?? 0;
                        dto.FloorFactor = firstCv.FloorFactor;

                        // SDRR validation - ensure rate master exists
                        dto.SDRR = firstCv.RateCVMaster != null
                            ? (double)firstCv.RateCVMaster.RateAmount
                            : 0;

                        dto.UseFactor = firstCv.UseFactor;
                        dto.NTBFactor = firstCv.NTBFactor;
                        dto.AgeFactor = firstCv.AgeFactor;
                        dto.BaseValue = firstCv.BaseValue;

                        // Safely handle navigation properties with null coalescing
                        dto.FloorDescription = pd.Floor?.Description ?? string.Empty;
                        dto.ConstructionTypeDescription = pd.ConstructionType?.Description ?? string.Empty;
                        dto.TypeOfUseDescription = pd.TypeOfUse?.Description ?? string.Empty;
                        dto.SubTypeOfUseDescription = pd.SubTypeOfUse?.Description ?? string.Empty;
                        dto.SubFloorDescription = pd.SubFloor?.Description ?? string.Empty;

                        dto.Taxes = g
                            .Select(cv => _mapper.Map<TaxHeadDto>(cv))
                            .GroupBy(t => t.TaxId)
                            .Select(t =>
                            {
                                var tax = t.First();

                                var cvWithTax = g.First(cv => cv.TaxId == tax.TaxId);

                                // Validate TaxMaster exists
                                if (cvWithTax.TaxMaster == null)
                                {
                                    _logger.LogWarning("TaxMaster is null for TaxId: {TaxId}, PropertyDetailsId: {PropertyDetailsId}. Tax name will be empty.",
                                        tax.TaxId, firstCv.PropertyDetailsId);
                                }

                                tax.TaxName = cvWithTax.TaxMaster?.TaxName ?? string.Empty;

                                return tax;
                            })
                            .ToList();

                        return dto;
                    })
                    .ToList();

                _logger.LogInformation("Successfully retrieved {Count} capital value records for PropertyId: {PropertyId}", result.Count, propertyId);
                return result;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while retrieving capital value for PropertyId: {PropertyId}", propertyId);
                throw;
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Data format error for PropertyId: {PropertyId}", propertyId);
                throw new InvalidOperationException(
                    $"Data format error while retrieving capital value for property {propertyId}. Please check assessment year and other numeric fields.",
                    ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while processing capital value for PropertyId: {PropertyId}", propertyId);
                throw new InvalidOperationException(
                    $"Database error while processing capital value for property {propertyId}. The operation has been rolled back.",
                    ex);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex, "Operation cancelled for PropertyId: {PropertyId}", propertyId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving capital value for PropertyId: {PropertyId}", propertyId);
                throw new InvalidOperationException(
                    $"An unexpected error occurred while retrieving capital value for property {propertyId}. Please contact support if this issue persists.",
                    ex);
            }
        }

        /// <summary>
        /// Creates capital value calculations for property. Orchestrates three main steps:
        /// 1. Load all master data (property, factors, rates, taxes, existing records)
        /// 2. Calculate CV and create records for each property detail
        /// 3. Update property-level aggregates (Policy and TransMast tables)
        /// </summary>
        public async Task<List<CapitalValueDto>> CreateAsync(CreateCapitalValueDto dto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting capital value creation for PropertyId: {PropertyId}, PropertyDetailsId: {PropertyDetailsId}, FinanceYear: {FinanceYear}",
                dto.PropertyId, dto.PropertyDetailsId, dto.FinanceYear);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            _logger.LogDebug("Transaction started for PropertyId: {PropertyId}", dto.PropertyId);

            try
            {
                // Step 1: Load all master data
                var masterData = await LoadMasterDataAsync(dto, cancellationToken);

                // Step 2: Calculate and create CV records
                var (resultList, aggregatedByTaxId) = await CalculateAndCreateCVRecordsAsync(dto, masterData, cancellationToken);

                // Step 3: Update property-level aggregates (only when calculating ALL property details)
                if (dto.PropertyDetailsId == CapitalValueConstants.PropertyDetails.AllPropertyDetails)
                {
                    await UpdatePropertyAggregatesAsync(dto, masterData, aggregatedByTaxId, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogDebug("Committing transaction for PropertyId: {PropertyId}", dto.PropertyId);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Successfully completed capital value creation for PropertyId: {PropertyId}. Created {Count} CV records",
                    dto.PropertyId, resultList.Count);

                return resultList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during capital value creation for PropertyId: {PropertyId}. Rolling back transaction", dto.PropertyId);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        /// <summary>
        /// Loads all master data required for CV calculations in one place:
        /// property info, lift flag, property details, finance year, year ranges,
        /// all factors, rate masters, tax configuration, and existing records
        /// </summary>
        private async Task<MasterCalculationData> LoadMasterDataAsync(CreateCapitalValueDto dto, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Loading master data for PropertyId: {PropertyId}", dto.PropertyId);

            // Property validation and basic info
            var property = await _propertyRepository.GetQueryable()
                .Where(x => x.Id == dto.PropertyId && !x.MarkedForDeletion && x.IsActive)
                .Select(x => new { x.MoujaId, x.CSN })
                .FirstOrDefaultAsync(cancellationToken);

            if (property == null)
            {
                _logger.LogWarning("Property not found for PropertyId: {PropertyId}", dto.PropertyId);
                throw new InvalidOperationException("Property not found");
            }

            // Validate critical fields for rate master lookup
            if (!property.MoujaId.HasValue)
            {
                _logger.LogError("Property MoujaId is null for PropertyId: {PropertyId}", dto.PropertyId);
                throw new InvalidOperationException($"Property {dto.PropertyId} has no MoujaId. MoujaId is required for rate calculation.");
            }

            if (string.IsNullOrWhiteSpace(property.CSN))
            {
                _logger.LogError("Property CSN is null or empty for PropertyId: {PropertyId}", dto.PropertyId);
                throw new InvalidOperationException($"Property {dto.PropertyId} has no CSN. CSN is required for rate calculation.");
            }

            _logger.LogDebug("Retrieved property MoujaId: {MoujaId}, CSN: {CSN} for PropertyId: {PropertyId}",
                property.MoujaId, property.CSN, dto.PropertyId);

            var hasLift = await _flagRepository.GetQueryable()
                .Where(x => x.PropertyId == dto.PropertyId && x.IsActive)
                .Select(x => x.Lift)
                .FirstOrDefaultAsync(cancellationToken);

            // Property details with navigation properties
            var propertyDetailsQuery = QueryPropertyDetailsWithIncludes()
                .Where(x => x.PropertyId == dto.PropertyId);

            if (dto.PropertyDetailsId != 0)
            {
                propertyDetailsQuery = propertyDetailsQuery.Where(x => x.Id == dto.PropertyDetailsId);
            }

            var propertyDetailsList = await propertyDetailsQuery.ToListAsync(cancellationToken);

            if (!propertyDetailsList.Any())
            {
                _logger.LogWarning("PropertyDetails not found for PropertyId: {PropertyId}, PropertyDetailsId: {PropertyDetailsId}",
                    dto.PropertyId, dto.PropertyDetailsId);
                throw new InvalidOperationException("PropertyDetails Not Found");
            }

            _logger.LogInformation("Processing {Count} property details for PropertyId: {PropertyId}",
                propertyDetailsList.Count, dto.PropertyId);

            // Finance year
            var financeYearQuery = _yearMasterRepository.GetQueryable().Where(x => x.IsActive);
            if (dto.FinanceYear.HasValue)
            {
                financeYearQuery = financeYearQuery.Where(x => x.Year == dto.FinanceYear.Value);
            }

            var financeYear = await financeYearQuery
                .Select(x => new { x.Id, x.Year })
                .FirstOrDefaultAsync(cancellationToken);

            if (financeYear == null)
            {
                var errorMessage = dto.FinanceYear.HasValue
                    ? $"Active finance year {dto.FinanceYear.Value} not found or is inactive."
                    : "No active finance year found in the system.";
                _logger.LogWarning("Finance year lookup failed for PropertyId: {PropertyId}. {ErrorMessage}",
                    dto.PropertyId, errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            _logger.LogDebug("Using FinanceYear: {FinanceYear} (Id: {FinanceYearId}) for PropertyId: {PropertyId}",
                financeYear.Year, financeYear.Id, dto.PropertyId);

            // Year ranges and all factors
            var yearRanges = await QueryActiveYearRanges().ToListAsync(cancellationToken);

            // Floor factors - safe dictionary creation with duplicate detection
            var floorFactorList = await _floorFactorRepository.GetQueryable()
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);

            var floorFactorDuplicates = floorFactorList
                .GroupBy(x => (x.FloorId, x.YearRangeCVId))
                .Where(g => g.Count() > 1)
                .ToList();

            if (floorFactorDuplicates.Any())
            {
                foreach (var dup in floorFactorDuplicates)
                {
                    _logger.LogWarning("Duplicate floor factor found: FloorId={FloorId}, YearRangeCVId={YearRangeCVId}, Count={Count}. Using first record.",
                        dup.Key.FloorId, dup.Key.YearRangeCVId, dup.Count());
                }
            }

            var floorFactorDict = floorFactorList
                .GroupBy(x => (x.FloorId, x.YearRangeCVId))
                .ToDictionary(g => g.Key, g => g.First());

            // Nature factors - safe dictionary creation with duplicate detection
            var natureFactorList = await _natureFactorRepository.GetQueryable()
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);

            var natureFactorDuplicates = natureFactorList
                .GroupBy(x => (x.ConstructionTypeId, x.YearRangeCVId))
                .Where(g => g.Count() > 1)
                .ToList();

            if (natureFactorDuplicates.Any())
            {
                foreach (var dup in natureFactorDuplicates)
                {
                    _logger.LogWarning("Duplicate nature factor found: ConstructionTypeId={ConstructionTypeId}, YearRangeCVId={YearRangeCVId}, Count={Count}. Using first record.",
                        dup.Key.ConstructionTypeId, dup.Key.YearRangeCVId, dup.Count());
                }
            }

            var natureFactors = natureFactorList
                .GroupBy(x => (x.ConstructionTypeId, x.YearRangeCVId))
                .ToDictionary(g => g.Key, g => (decimal?)g.First().Factor);

            // Use factors - safe dictionary creation with duplicate detection
            var useFactorList = await _useFactorRepository.GetQueryable()
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);

            var useFactorDuplicates = useFactorList
                .GroupBy(x => (x.TypeOfUseId, x.YearRangeCVId, x.SubTypeOfUseId))
                .Where(g => g.Count() > 1)
                .ToList();

            if (useFactorDuplicates.Any())
            {
                foreach (var dup in useFactorDuplicates)
                {
                    _logger.LogWarning("Duplicate use factor found: TypeOfUseId={TypeOfUseId}, YearRangeCVId={YearRangeCVId}, SubTypeOfUseId={SubTypeOfUseId}, Count={Count}. Using first record.",
                        dup.Key.TypeOfUseId, dup.Key.YearRangeCVId, dup.Key.SubTypeOfUseId, dup.Count());
                }
            }

            var useFactors = useFactorList
                .GroupBy(x => (x.TypeOfUseId, x.YearRangeCVId, x.SubTypeOfUseId))
                .ToDictionary(g => g.Key, g => (decimal?)g.First().Factor);

            var ageFactors = await _ageFactorRepository.GetQueryable()
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Loaded factors - Nature: {NatureCount}, Use: {UseCount}, Age: {AgeCount} for PropertyId: {PropertyId}",
                natureFactors.Count, useFactors.Count, ageFactors.Count, dto.PropertyId);

            // Rate masters
            var rateMasters = await (
                from csnDetail in _CSNDetailsRepository.GetQueryable()
                join rm in _rateRepository.GetQueryable().Where(x => x.IsActive)
                    on csnDetail.RateCVMasterId equals rm.Id
                where csnDetail.MoujaId == property.MoujaId && csnDetail.CSN == property.CSN
                select new
                {
                    RateMasterCVId = rm.Id,
                    rm.SubZoneId,
                    rm.TypeOfUseGroupId,
                    rm.FloorGroupId,
                    rm.AssessmentYearRangeId,
                    rm.RateAmount
                }
            ).ToListAsync(cancellationToken);

            if (!rateMasters.Any())
            {
                _logger.LogError("No active rate records found for PropertyId: {PropertyId}, MoujaId: {MoujaId}, CSN: {CSN}",
                    dto.PropertyId, property.MoujaId, property.CSN);
                throw new InvalidOperationException(
                    $"No active rate records found for MoujaId: {property.MoujaId}, CSN: {property.CSN}");
            }

            _logger.LogDebug("Found {Count} rate master records for PropertyId: {PropertyId}, MoujaId: {MoujaId}, CSN: {CSN}",
                rateMasters.Count, dto.PropertyId, property.MoujaId, property.CSN);

            // Tax configuration
            var taxTotalHead = await _taxMasterRepository.GetQueryable()
                .Where(x => x.IsActive && x.TaxName == CapitalValueConstants.Tax.TaxTotalName)
                .Select(x => new { x.Id, x.TaxName })
                .FirstOrDefaultAsync(cancellationToken);

            if (taxTotalHead == null)
                throw new InvalidOperationException($"{CapitalValueConstants.Tax.TaxTotalName} head not found in TaxMaster.");

            var taxData = await (
                from t1 in _taxMasterRepository.GetQueryable()
                    .Where(x => x.IsActive && x.Id != taxTotalHead.Id)
                join tp in _taxPercentageRepository.GetQueryable().Where(x => x.IsActive)
                    on t1.Id equals tp.TaxId
                select new
                {
                    t1.Id,
                    t1.TaxName,
                    tp.TypeOfUseId,
                    tp.YearRangeCVId,
                    tp.TaxPercentage
                }
            ).ToListAsync(cancellationToken);

            // Existing records
            var existingCVRecords = (await _cvRepository.GetQueryable()
                .Where(x => x.PropertyId == dto.PropertyId && x.IsActive)
                .Select(x => new { x.PropertyDetailsId, x.TaxId })
                .ToListAsync(cancellationToken))
                .Select(x => (x.PropertyDetailsId, x.TaxId))
                .ToHashSet();

            var existingPolicyRecords = await _policyTaxDetailsCVRepository.GetQueryable()
                .Where(x => x.PropertyId == dto.PropertyId && x.IsActive && !x.MarkedForDeletion)
                .ToListAsync(cancellationToken);

            var existingPolicyRecordDict = existingPolicyRecords
                .GroupBy(x => (x.PropertyId, x.TaxId))
                .ToDictionary(g => g.Key, g => g.First());

            var existingTransMastRecords = await _transMastCVRepository.GetQueryable()
                .Where(x => x.PropertyId == dto.PropertyId &&
                            x.FinanceYearId == financeYear.Id &&
                            !x.MarkedForDeletion)
                .ToListAsync(cancellationToken);

            var existingTransMastRecordDict = existingTransMastRecords
                .GroupBy(x => (x.PropertyId, x.FinanceYearId, x.TaxId))
                .ToDictionary(g => g.Key, g => g.First());

            _logger.LogDebug("Master data loading completed for PropertyId: {PropertyId}", dto.PropertyId);

            return new MasterCalculationData
            {
                Property = new PropertyLookupData
                {
                    MoujaId = property.MoujaId,
                    CSN = property.CSN
                },
                HasLift = hasLift,
                PropertyDetailsList = propertyDetailsList,
                FinanceYear = (financeYear.Id, financeYear.Year),
                YearRanges = yearRanges,
                FloorFactorDict = floorFactorDict,
                NatureFactors = natureFactors,
                UseFactors = useFactors,
                AgeFactors = ageFactors,
                RateMasters = rateMasters.Cast<dynamic>().ToList(),
                TaxData = taxData.Cast<dynamic>().ToList(),
                TaxTotalHead = (taxTotalHead.Id, taxTotalHead.TaxName),
                ExistingCVRecords = existingCVRecords,
                ExistingPolicyRecordDict = existingPolicyRecordDict,
                ExistingTransMastRecordDict = existingTransMastRecordDict
            };


        }

        /// <summary>
        /// Main calculation logic: loops through property details, validates years, finds year range and rate master,
        /// calculates all factors (NTB, Use, Age, Floor), computes capital value, retrieves applicable taxes,
        /// creates CV records for each tax, and aggregates totals for property-level updates
        /// </summary>
        private async Task<(List<CapitalValueDto> ResultList, Dictionary<int, (decimal TotalTaxAmount, decimal TotalCapitalValue)> AggregatedByTaxId)>

        CalculateAndCreateCVRecordsAsync(CreateCapitalValueDto dto, MasterCalculationData masterData, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Starting CV record calculation for PropertyId: {PropertyId}, processing {Count} property details",
                dto.PropertyId, masterData.PropertyDetailsList.Count);

            var resultList = new List<CapitalValueDto>();
            var aggregatedByTaxId = new Dictionary<int, (decimal TotalTaxAmount, decimal TotalCapitalValue)>();

            foreach (var pd in masterData.PropertyDetailsList)
            {
                _logger.LogDebug("Processing PropertyDetailsId: {PropertyDetailsId}, AssessmentYear: {AssessmentYear}, ConstructionYear: {ConstructionYear}",
                    pd.Id, pd.AssessmentYear, pd.ConstructionYear);

                // Validate and parse years
                if (!int.TryParse(pd.AssessmentYear, out int assessmentYear) || assessmentYear <= 0)
                {
                    _logger.LogError("Invalid assessment year for PropertyDetailsId: {PropertyDetailsId}, Value: {AssessmentYear}",
                        pd.Id, pd.AssessmentYear);
                    throw new InvalidOperationException(
                        $"Invalid or missing assessment year for PropertyDetails {pd.Id}: '{pd.AssessmentYear}'");
                }

                if (!int.TryParse(pd.ConstructionYear, out int constructionYear) || constructionYear <= 0)
                {
                    _logger.LogError("Invalid construction year for PropertyDetailsId: {PropertyDetailsId}, Value: {ConstructionYear}",
                        pd.Id, pd.ConstructionYear);
                    throw new InvalidOperationException(
                        $"Invalid or missing construction year for PropertyDetails {pd.Id}: '{pd.ConstructionYear}'");
                }

                int ageOfProperty = assessmentYear - constructionYear;
                if (ageOfProperty < 0)
                {
                    _logger.LogError("Invalid property age for PropertyDetailsId: {PropertyDetailsId}, Age: {Age} (AssessmentYear: {AssessmentYear}, ConstructionYear: {ConstructionYear})",
                        pd.Id, ageOfProperty, assessmentYear, constructionYear);
                    throw new InvalidOperationException($"Invalid property age for PropertyDetails {pd.Id}");
                }

                _logger.LogDebug("PropertyDetailsId: {PropertyDetailsId} - Age: {Age} years", pd.Id, ageOfProperty);

                // Find year range
                var yearRange = masterData.YearRanges.FirstOrDefault(x =>
                    assessmentYear >= x.FromYear && assessmentYear <= x.ToYear);

                if (yearRange == null)
                {
                    _logger.LogError("Year range not found for PropertyDetailsId: {PropertyDetailsId}, AssessmentYear: {AssessmentYear}",
                        pd.Id, assessmentYear);
                    throw new InvalidOperationException($"Year range not found for {assessmentYear}");
                }

                _logger.LogDebug("PropertyDetailsId: {PropertyDetailsId} - Using YearRange: {FromYear}-{ToYear} (Id: {YearRangeId})",
                    pd.Id, yearRange.FromYear, yearRange.ToYear, yearRange.Id);

                // Find rate master with floor-wise rate logic
                var typeOfUseGroupId = pd.TypeOfUse?.TypeOfUseGroupId;
                if (!typeOfUseGroupId.HasValue)
                {
                    _logger.LogError("TypeOfUseGroupId not found for PropertyDetailsId: {PropertyDetailsId}, TypeOfUseId: {TypeOfUseId}",
                        pd.Id, pd.TypeOfUseId);
                    throw new InvalidOperationException(
                        $"TypeOfUseGroupId not found for PropertyDetails {pd.Id}, TypeOfUseId {pd.TypeOfUseId}");
                }

                var typeOfUseGroup = pd.TypeOfUse?.TypeOfUseGroup;
                if (typeOfUseGroup == null)
                {
                    _logger.LogError("TypeOfUseGroup not loaded for PropertyDetailsId: {PropertyDetailsId}, TypeOfUseId: {TypeOfUseId}",
                        pd.Id, pd.TypeOfUseId);
                    throw new InvalidOperationException(
                        $"TypeOfUseGroup not loaded/found for PropertyDetails {pd.Id}, TypeOfUseId {pd.TypeOfUseId}");
                }

                bool isFloorWiseRateApplicable = typeOfUseGroup.IsFloorWiseRateApplicable;
                int? floorGroupId = null;

                if (isFloorWiseRateApplicable)
                {
                    floorGroupId = pd.Floor?.FloorGroupId;
                    if (!floorGroupId.HasValue)
                    {
                        _logger.LogError("FloorGroup not found for floor-wise rate PropertyDetailsId: {PropertyDetailsId}, FloorId: {FloorId}",
                            pd.Id, pd.FloorId);
                        throw new InvalidOperationException($"FloorGroup not found for floor-wise rate PropertyDetails {pd.Id}");
                    }
                }

                _logger.LogDebug("PropertyDetailsId: {PropertyDetailsId} - FloorWiseRate: {IsFloorWise}, FloorGroupId: {FloorGroupId}",
                    pd.Id, isFloorWiseRateApplicable, floorGroupId);

                var rateMaster = masterData.RateMasters.FirstOrDefault(x =>
                    x.AssessmentYearRangeId == yearRange.Id &&
                    x.TypeOfUseGroupId == typeOfUseGroupId.Value &&
                    (isFloorWiseRateApplicable ? x.FloorGroupId == floorGroupId : x.FloorGroupId == null));

                if (rateMaster == null)
                {
                    var floorMessage = isFloorWiseRateApplicable
                        ? $", FloorGroupId: {floorGroupId}"
                        : ", FloorGroupId: N/A (not floor-wise)";

                    // Use null-safe formatting for nullable fields
                    var moujaIdDisplay = masterData.Property.MoujaId?.ToString() ?? "NULL";
                    var csnDisplay = masterData.Property.CSN ?? "NULL";

                    _logger.LogError("Rate master not found for PropertyDetailsId: {PropertyDetailsId}. MoujaId: {MoujaId}, CSN: {CSN}, " +
                        "AssessmentYear: {AssessmentYear}, TypeOfUseGroupId: {TypeOfUseGroupId}{FloorMessage}",
                        pd.Id, moujaIdDisplay, csnDisplay, assessmentYear, typeOfUseGroupId.Value, floorMessage);

                    throw new InvalidOperationException(
                        $"Rate not found. Mouja: {moujaIdDisplay}, CSN: {csnDisplay}, " +
                        $"AssessmentYear: {assessmentYear}, TypeOfUseGroupId: {typeOfUseGroupId.Value}" + floorMessage);
                }

                decimal rate = rateMaster.RateAmount;
                int rateMasterCVId = (int)rateMaster.RateMasterCVId;

                _logger.LogDebug("PropertyDetailsId: {PropertyDetailsId} - Rate: {Rate}, RateMasterCVId: {RateMasterCVId}",
                    pd.Id, rate, rateMasterCVId);

                // Calculate all factors
                decimal ntb = masterData.NatureFactors.GetValueOrDefault((pd.ConstructionTypeId, yearRange.Id)) ?? 1;
                decimal use = pd.SubTypeOfUseId.HasValue ? masterData.UseFactors.GetValueOrDefault((pd.TypeOfUseId, yearRange.Id, pd.SubTypeOfUseId.Value)) ?? 1 : 1;

                var ageFactorEntity = masterData.AgeFactors.FirstOrDefault(x =>
                    x.ConstructionTypeId == pd.ConstructionTypeId &&
                    x.YearRangeCVId == yearRange.Id &&
                    ageOfProperty >= x.AgeFrom &&
                    ageOfProperty <= x.AgeTo);

                decimal age = ageFactorEntity?.Factor ?? 1;
                decimal floorFactor = 1;

                if (pd.FloorId != 0 &&
                    masterData.FloorFactorDict.TryGetValue((pd.FloorId, yearRange.Id), out var floorFactorEntity))
                {
                    floorFactor = masterData.HasLift ? floorFactorEntity.FactorWithLift : floorFactorEntity.FactorWithoutLift;
                }

                _logger.LogDebug("PropertyDetailsId: {PropertyDetailsId} - Factors calculated: NTB={NTB}, Use={Use}, Age={Age}, Floor={Floor}, HasLift={HasLift}",
                    pd.Id, ntb, use, age, floorFactor, masterData.HasLift);

                // Validate carpet area
                if (!pd.CarpetAreaSqMeter.HasValue || pd.CarpetAreaSqMeter.Value <= 0)
                {
                    _logger.LogError("Invalid or missing carpet area for PropertyDetailsId: {PropertyDetailsId}, Value: {CarpetArea}",
                        pd.Id, pd.CarpetAreaSqMeter);
                    throw new InvalidOperationException(
                        $"Invalid or missing carpet area for PropertyDetails {pd.Id}. Carpet area must be greater than 0.");
                }

                // Calculate capital value
                decimal carpetArea = (decimal)pd.CarpetAreaSqMeter.Value;
                var baseValue = rate * carpetArea;
                var capitalValue = baseValue * ntb * use * age * floorFactor;

                _logger.LogInformation("PropertyDetailsId: {PropertyDetailsId} - Calculated CV: {CapitalValue} (BaseValue: {BaseValue}, CarpetArea: {CarpetArea})",
                    pd.Id, capitalValue, baseValue, carpetArea);

                // Get applicable taxes
                var taxList = masterData.TaxData
                    .Where(x => x.TypeOfUseId == pd.TypeOfUseId && x.YearRangeCVId == yearRange.Id)
                    .OrderBy(x => x.Id)
                    .GroupBy(x => x.Id)
                    .Select(g => g.First())
                    .ToList();

                if (!taxList.Any())
                {
                    _logger.LogError("No tax percentages found for PropertyDetailsId: {PropertyDetailsId}, TypeOfUseId: {TypeOfUseId}, " +
                        "TypeOfUse: {TypeOfUse}, AssessmentYear: {AssessmentYear}, YearRangeId: {YearRangeId}",
                        pd.Id, pd.TypeOfUseId, pd.TypeOfUse?.Description, assessmentYear, yearRange.Id);

                    throw new InvalidOperationException(
                        $"Tax Percentage Not Found for TypeOfUse: {pd.TypeOfUse?.Description}, AssessmentYear: {assessmentYear}. " +
                        $"Taxes cannot be inserted into PropertyTaxCalculationCVResults table. " +
                        $"Please ensure tax percentage master data is configured for this TypeOfUse and Year Range.");
                }

                _logger.LogDebug("PropertyDetailsId: {PropertyDetailsId} - Found {TaxCount} applicable taxes", pd.Id, taxList.Count);

                // Build DTO
                var dtoItem = _mapper.Map<CapitalValueDto>(pd);
                dtoItem.PropertyId = dto.PropertyId;
                dtoItem.CapitalValue = capitalValue;
                dtoItem.BaseValue = (double)baseValue;
                dtoItem.FloorFactor = (double)floorFactor;
                dtoItem.SDRR = (double)rate;
                dtoItem.UseFactor = (double)use;
                dtoItem.NTBFactor = (double)ntb;
                dtoItem.AgeFactor = (double)age;
                dtoItem.Taxes = new List<TaxHeadDto>();
                dtoItem.FloorDescription = pd.Floor?.Description;
                dtoItem.ConstructionTypeDescription = pd.ConstructionType?.Description;
                dtoItem.TypeOfUseDescription = pd.TypeOfUse?.Description;
                dtoItem.SubTypeOfUseDescription = pd.SubTypeOfUse?.Description;
                dtoItem.SubFloorDescription = pd.SubFloor?.Description;

                decimal propertyDetailsTaxTotal = 0;

                // Create individual tax records
                foreach (var tax in taxList)
                {
                    int taxId = (int)tax.Id;
                    decimal taxPercentage = (decimal)tax.TaxPercentage;
                    string taxName = (string)tax.TaxName;
                    decimal taxAmount = Math.Round(capitalValue * (taxPercentage / 100), 2, MidpointRounding.AwayFromZero);
                    propertyDetailsTaxTotal += taxAmount;

                    dtoItem.Taxes.Add(new TaxHeadDto
                    {
                        TaxId = taxId,
                        TaxName = taxName,
                        Percentage = taxPercentage,
                        Amount = taxAmount
                    });

                    var recordKey = (pd.Id, taxId);

                    if (!masterData.ExistingCVRecords.Contains(recordKey))
                    {
                        _logger.LogDebug("Creating CV record for PropertyDetailsId: {PropertyDetailsId}, TaxId: {TaxId}, TaxAmount: {TaxAmount}",
                            pd.Id, taxId, taxAmount);

                        await _cvRepository.AddAsync(new PropertyTaxCalculationCVResultsEntity
                        {
                            PropertyId = dto.PropertyId,
                            PropertyDetailsId = pd.Id,
                            TaxId = taxId,
                            TaxPercentage = taxPercentage,
                            TaxAmount = taxAmount,
                            RateCVMasterId = rateMasterCVId,
                            BaseValue = (double)baseValue,
                            CapitalValue = capitalValue,
                            NTBFactor = (double)ntb,
                            UseFactor = (double)use,
                            AgeFactor = (double)age,
                            FloorFactor = (double)floorFactor,
                            CreatedBy = dto.CreatedBy,
                            CreatedDate = System.DateTime.UtcNow
                        }, cancellationToken);

                        masterData.ExistingCVRecords.Add(recordKey);
                    }
                    else
                    {
                        _logger.LogDebug("Skipping existing CV record for PropertyDetailsId: {PropertyDetailsId}, TaxId: {TaxId}",
                            pd.Id, taxId);
                    }

                    // Aggregate for property-level updates
                    if (aggregatedByTaxId.ContainsKey(taxId))
                    {
                        var existing = aggregatedByTaxId[taxId];
                        aggregatedByTaxId[taxId] = (
                            existing.TotalTaxAmount + taxAmount,
                            existing.TotalCapitalValue + capitalValue
                        );
                    }
                    else
                    {
                        aggregatedByTaxId[taxId] = (taxAmount, capitalValue);
                    }
                }

                // Create TaxTotal record
                dtoItem.Taxes.Add(new TaxHeadDto
                {
                    TaxId = masterData.TaxTotalHead.Id,
                    TaxName = masterData.TaxTotalHead.TaxName,
                    Percentage = 0,
                    Amount = propertyDetailsTaxTotal
                });

                var taxTotalRecordKey = (pd.Id, masterData.TaxTotalHead.Id);

                if (!masterData.ExistingCVRecords.Contains(taxTotalRecordKey))
                {
                    _logger.LogDebug("Creating TaxTotal CV record for PropertyDetailsId: {PropertyDetailsId}, TotalTax: {TotalTax}",
                        pd.Id, propertyDetailsTaxTotal);

                    await _cvRepository.AddAsync(new PropertyTaxCalculationCVResultsEntity
                    {
                        PropertyId = dto.PropertyId,
                        PropertyDetailsId = pd.Id,
                        TaxId = masterData.TaxTotalHead.Id,
                        TaxPercentage = 0,
                        TaxAmount = propertyDetailsTaxTotal,
                        RateCVMasterId = rateMasterCVId,
                        BaseValue = (double)baseValue,
                        CapitalValue = capitalValue,
                        NTBFactor = (double)ntb,
                        UseFactor = (double)use,
                        AgeFactor = (double)age,
                        FloorFactor = (double)floorFactor,
                        CreatedBy = dto.CreatedBy,
                        CreatedDate = System.DateTime.UtcNow
                    }, cancellationToken);

                    masterData.ExistingCVRecords.Add(taxTotalRecordKey);
                }

                // Aggregate TaxTotal
                if (aggregatedByTaxId.ContainsKey(masterData.TaxTotalHead.Id))
                {
                    var existing = aggregatedByTaxId[masterData.TaxTotalHead.Id];
                    aggregatedByTaxId[masterData.TaxTotalHead.Id] = (
                        existing.TotalTaxAmount + propertyDetailsTaxTotal,
                        existing.TotalCapitalValue + capitalValue
                    );
                }
                else
                {
                    aggregatedByTaxId[masterData.TaxTotalHead.Id] = (propertyDetailsTaxTotal, capitalValue);
                }

                resultList.Add(dtoItem);
            }

            _logger.LogInformation("CV record calculation completed for PropertyId: {PropertyId}. Created {ResultCount} DTO records, {TaxAggregateCount} tax aggregates",
                dto.PropertyId, resultList.Count, aggregatedByTaxId.Count);

            return (resultList, aggregatedByTaxId);
        }

        /// <summary>
        /// Updates property-level aggregate tables: PolicyTaxDetailsCV and TransMastCV.
        /// For each tax, either updates existing records or creates new ones with aggregated capital value and tax amount.
        /// Only called when calculating ALL property details (dto.PropertyDetailsId == CapitalValueConstants.PropertyDetails.AllPropertyDetails)
        /// </summary>
        private async Task UpdatePropertyAggregatesAsync( CreateCapitalValueDto dto, MasterCalculationData masterData, Dictionary<int, (decimal TotalTaxAmount, decimal TotalCapitalValue)> aggregatedByTaxId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating property aggregates for PropertyId: {PropertyId}, {TaxCount} tax types",
                dto.PropertyId, aggregatedByTaxId.Count);

            foreach (var item in aggregatedByTaxId)
            {
                int taxId = item.Key;
                decimal totalTaxAmount = item.Value.TotalTaxAmount;
                decimal totalCapitalValue = item.Value.TotalCapitalValue;

                _logger.LogDebug("Processing aggregate for PropertyId: {PropertyId}, TaxId: {TaxId}, TotalTaxAmount: {TotalTaxAmount}, TotalCapitalValue: {TotalCapitalValue}",
                    dto.PropertyId, taxId, totalTaxAmount, totalCapitalValue);

                // Update or create PolicyTaxDetailsCV record
                var policyRecordKey = (dto.PropertyId, taxId);

                if (masterData.ExistingPolicyRecordDict.TryGetValue(policyRecordKey, out var existingPolicyEntity))
                {
                    _logger.LogDebug("Updating existing PolicyTaxDetailsCV record for PropertyId: {PropertyId}, TaxId: {TaxId}",
                        dto.PropertyId, taxId);

                    existingPolicyEntity.PolicyCode = dto.PolicyCode ?? CapitalValueConstants.Policy.DefaultPolicyCode;
                    existingPolicyEntity.PolicyDate = dto.PolicyDate ?? System.DateTime.UtcNow;
                    existingPolicyEntity.PolicyYear = dto.PolicyYear ?? masterData.FinanceYear.Year;
                    existingPolicyEntity.PolicyReason = dto.PolicyReason;
                    existingPolicyEntity.PolicyRVorCVvalue = totalCapitalValue;
                    existingPolicyEntity.TaxAmount = totalTaxAmount;
                    existingPolicyEntity.UpdatedBy = dto.CreatedBy;
                    existingPolicyEntity.UpdatedDate = System.DateTime.UtcNow;

                    await _policyTaxDetailsCVRepository.UpdateAsync(existingPolicyEntity, cancellationToken);
                }
                else
                {
                    _logger.LogDebug("Creating new PolicyTaxDetailsCV record for PropertyId: {PropertyId}, TaxId: {TaxId}",
                        dto.PropertyId, taxId);

                    var policyTaxEntity = new PolicyTaxDetailsCVEntity
                    {
                        PropertyId = dto.PropertyId,
                        PolicyCode = dto.PolicyCode ?? CapitalValueConstants.Policy.DefaultPolicyCode,
                        PolicyDate = dto.PolicyDate ?? System.DateTime.UtcNow,
                        PolicyYear = dto.PolicyYear ?? masterData.FinanceYear.Year,
                        PolicyReason = dto.PolicyReason,
                        PolicyRVorCVvalue = totalCapitalValue,
                        TaxId = taxId,
                        TaxAmount = totalTaxAmount,
                        IsActive = true,
                        CreatedBy = dto.CreatedBy,
                        CreatedDate = System.DateTime.UtcNow
                    };

                    await _policyTaxDetailsCVRepository.AddAsync(policyTaxEntity, cancellationToken);
                    masterData.ExistingPolicyRecordDict.Add(policyRecordKey, policyTaxEntity);
                }

                // Update or create TransMastCV record
                var transMastRecordKey = (dto.PropertyId, masterData.FinanceYear.Id, taxId);

                if (masterData.ExistingTransMastRecordDict.TryGetValue(transMastRecordKey, out var existingTransMastEntity))
                {
                    _logger.LogDebug("Updating existing TransMastCV record for PropertyId: {PropertyId}, FinanceYearId: {FinanceYearId}, TaxId: {TaxId}",
                        dto.PropertyId, masterData.FinanceYear.Id, taxId);

                    existingTransMastEntity.CapitalValue = totalCapitalValue;
                    existingTransMastEntity.TaxAmount = totalTaxAmount;
                    existingTransMastEntity.IsActive = true;
                    existingTransMastEntity.UpdatedBy = dto.CreatedBy;
                    existingTransMastEntity.UpdatedDate = System.DateTime.UtcNow;

                    await _transMastCVRepository.UpdateAsync(existingTransMastEntity, cancellationToken);
                }
                else
                {
                    _logger.LogDebug("Creating new TransMastCV record for PropertyId: {PropertyId}, FinanceYearId: {FinanceYearId}, TaxId: {TaxId}",
                        dto.PropertyId, masterData.FinanceYear.Id, taxId);

                    var transMastEntity = new TransMastCVEntity
                    {
                        PropertyId = dto.PropertyId,
                        FinanceYearId = masterData.FinanceYear.Id,
                        CapitalValue = totalCapitalValue,
                        TaxId = taxId,
                        TaxAmount = totalTaxAmount,
                        IsActive = true,
                        CreatedBy = dto.CreatedBy,
                        CreatedDate = System.DateTime.UtcNow
                    };

                    await _transMastCVRepository.AddAsync(transMastEntity, cancellationToken);
                    masterData.ExistingTransMastRecordDict.Add(transMastRecordKey, transMastEntity);
                }
            }

            _logger.LogInformation("Property aggregates update completed for PropertyId: {PropertyId}", dto.PropertyId);
        }

        #region Helper Class

        /// <summary>
        /// Container for property lookup data required for rate master queries
        /// </summary>
        private class PropertyLookupData
        {
            public int? MoujaId { get; set; }
            public string? CSN { get; set; }
        }

        /// <summary>
        /// Container for all master data required for capital value calculations
        /// </summary>
        private class MasterCalculationData
        {
            public PropertyLookupData Property { get; set; } = null!;
            public bool HasLift { get; set; }
            public List<PropertyDetailsEntity> PropertyDetailsList { get; set; } = null!;
            public (int Id, int Year) FinanceYear { get; set; }
            public List<AssessmentYearRangeCVEntity> YearRanges { get; set; } = null!;
            public Dictionary<(int FloorId, int YearRangeCVId), FloorFactorCVMasterEntity> FloorFactorDict { get; set; } = null!;
            public Dictionary<(int ConstructionTypeId, int YearRangeCVId), decimal?> NatureFactors { get; set; } = null!;
            public Dictionary<(int TypeOfUseId, int YearRangeCVId, int SubTypeOfUseId), decimal?> UseFactors { get; set; } = null!;
            public List<AgeFactorCVMasterEntity> AgeFactors { get; set; } = null!;
            public List<dynamic> RateMasters { get; set; } = null!;
            public List<dynamic> TaxData { get; set; } = null!;
            public (int Id, string TaxName) TaxTotalHead { get; set; }
            public HashSet<(int PropertyDetailsId, int TaxId)> ExistingCVRecords { get; set; } = null!;
            public Dictionary<(int PropertyId, int TaxId), PolicyTaxDetailsCVEntity> ExistingPolicyRecordDict { get; set; } = null!;
            public Dictionary<(int PropertyId, int FinanceYearId, int TaxId), TransMastCVEntity> ExistingTransMastRecordDict { get; set; } = null!;


        }

        #endregion
   
    }

}
