using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NtisPlatform.Application.DTOs.PropertyTaxOperations;
using NtisPlatform.Application.Enums;
using NtisPlatform.Application.Interfaces;
using NtisPlatform.Application.Models;
using NtisPlatform.Application.Options;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;
using NtisPlatform.Core.Enums;
using Microsoft.Extensions.Configuration;
using NtisPlatform.Application.Interfaces.Master;
using NtisPlatform.Application.Interfaces.ICapitalValueService.ICapitalValueService;
using NtisPlatform.Application.DTOs.CapitalValue;

namespace NtisPlatform.Application.Services.PropertyTaxOperations;

/// <summary>
/// Implements the Property Tax Operations workflow. "Add Tax" reuses
/// <see cref="IRateableValueService.CalculateAndSaveAsync"/> per eligible property; execution is
/// synchronous for now but records a job + per-property results so the Audit &amp; Monitor screen
/// and runtime bar have real data.
/// </summary>
public class PropertyTaxOperationsService : IPropertyTaxOperationsService
{
    private readonly IRateableValueService _rateableValueService;
    private readonly ICapitalValueService _capitalValueService;
    private readonly IPolicyConfigurationService _policyConfigService;
    private readonly IRepository<PropertyEntity, int> _propertyRepo;
    private readonly IRepository<PropertyTaxJobEntity, int> _jobRepo;
    private readonly IRepository<PropertyTaxJobDetailEntity, int> _jobDetailRepo;
    private readonly IRepository<PropertyScreenLockEntity, int> _lockRepo;
    private readonly IRepository<PropertyDetailsEntity, int> _propertyDetailsRepo;
    private readonly IRepository<YearMasterEntity, int> _yearMasterRepo;
    private readonly IRepository<WardEntity, int> _wardRepo;
    private readonly IRepository<ZoneEntity, int> _zoneRepo;
    private readonly IRepository<PropertyTypeMasterEntity, int> _propertyTypeRepo;
    private readonly IRepository<PropertyAssessmentStatusEntity, int> _propertyAssessmentStatusRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PropertyTaxOperationsService> _logger;
    private readonly IUserScreenAccessService _screenAccessService;
    private readonly IConfiguration _configuration;

    private const string NetTaxPolicyCode = "NETTAX";

    public PropertyTaxOperationsService(
        IRateableValueService rateableValueService,
        ICapitalValueService capitalValueService,
        IPolicyConfigurationService policyConfigService,
        IRepository<PropertyEntity, int> propertyRepo,
        IRepository<PropertyTaxJobEntity, int> jobRepo,
        IRepository<PropertyTaxJobDetailEntity, int> jobDetailRepo,
        IRepository<PropertyScreenLockEntity, int> lockRepo,
        IRepository<PropertyDetailsEntity, int> propertyDetailsRepo,
        IRepository<YearMasterEntity, int> yearMasterRepo,
        IRepository<WardEntity, int> wardRepo,
        IRepository<ZoneEntity, int> zoneRepo,
        IRepository<PropertyTypeMasterEntity, int> propertyTypeRepo,
        IRepository<PropertyAssessmentStatusEntity, int> propertyAssessmentStatusRepo,
        IUnitOfWork unitOfWork,
        ILogger<PropertyTaxOperationsService> logger,
        IUserScreenAccessService screenAccessService,
        IConfiguration configuration)
    {
        _rateableValueService = rateableValueService;
        _capitalValueService = capitalValueService;
        _policyConfigService = policyConfigService;
        _propertyRepo = propertyRepo;
        _jobRepo = jobRepo;
        _jobDetailRepo = jobDetailRepo;
        _lockRepo = lockRepo;
        _propertyDetailsRepo = propertyDetailsRepo;
        _yearMasterRepo = yearMasterRepo;
        _wardRepo = wardRepo;
        _zoneRepo = zoneRepo;
        _propertyTypeRepo = propertyTypeRepo;
        _propertyAssessmentStatusRepo = propertyAssessmentStatusRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _screenAccessService = screenAccessService;
        _configuration = configuration;
    }

    // ---------------------------------------------------------------- Init

    public async Task<OperationsInitDto> GetInitAsync(int actingUserId, int? financeYearId = null, CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;

        var userScreens = (await _screenAccessService.GetUserScreensByUserIdAsync(actingUserId, cancellationToken)).ToList();
        
        bool canAddTax = userScreens.Any(s => s.ScreenCode == "PROP_TAX_ADD" && (s.HaveFullAccess || s.CanEdit));

        var allYears = await _yearMasterRepo.GetQueryable()
            .AsNoTracking()
            .OrderByDescending(y => y.Year)
            .ToListAsync(cancellationToken);

        var currentYearEntity = allYears.FirstOrDefault(y => y.IsActive)
            ?? allYears.FirstOrDefault(y => y.StartDate <= today && y.EndDate >= today)
            ?? allYears.FirstOrDefault();

        var selectedYearEntity = financeYearId.HasValue && financeYearId > 0
            ? allYears.FirstOrDefault(y => y.Id == financeYearId.Value)
            : currentYearEntity;

        int financeYear = selectedYearEntity?.Year ?? (today.Month >= 4 ? today.Year : today.Year - 1);

        var candidates = ActiveProperties();
        if (selectedYearEntity?.StartDate.HasValue == true && selectedYearEntity?.EndDate.HasValue == true)
        {
            candidates = candidates.Where(p => p.CreatedDate >= selectedYearEntity.StartDate.Value && p.CreatedDate <= selectedYearEntity.EndDate.Value);
        }

        int total = await candidates.CountAsync(cancellationToken);
        int eligible = await ApplyEligibility(candidates, financeYear).CountAsync(cancellationToken);
        int runningJobs = await _jobRepo.GetQueryable().AsNoTracking()
            .CountAsync(j => j.Status == nameof(JobStatus.InProgress) && !j.MarkedForDeletion, cancellationToken);

        return new OperationsInitDto
        {
            FinanceYears = allYears.Select(y => new FinanceYearOptionDto
            {
                Value = y.Id.ToString(),
                Label = !string.IsNullOrWhiteSpace(y.YearCode) ? y.YearCode : $"{y.Year}-{(y.Year + 1) % 100:D2}",
                IsActive = y.IsActive
            }).ToList(),
            Permissions = new OperationPermissionsDto
            {
                AddTax = canAddTax
            },
            Summary = new OperationsSummaryDto
            {
                TotalProperties = total,
                EligibleRecords = eligible,
                SkippedRecords = Math.Max(0, total - eligible),
                RunningJobs = runningJobs
            }
        };
    }

    public Task<ImportTemplateDto> GetImportTemplateAsync(CancellationToken cancellationToken = default)
    {
        var template = new ImportTemplateDto
        {
            Columns = new List<ImportTemplateColumnDto>
            {
                new() { Key = "Ward", Header = "Ward", DataType = "string", Required = true },
                new() { Key = "PropertyNoPartitionNo", Header = "PropertyNo-PartitionNo", DataType = "string", Required = true }
            },
            ScopeCategories = new List<ScopeCategoryOptionDto>
            {
                new() { Id = (int)ScopeCategory.ZoneNode, Name = ScopeCategory.ZoneNode.ToString(), Description = ScopeCategory.ZoneNode.GetDescription(), ScopeType = ScopeCategory.ZoneNode.GetScopeType(), RequiredColumns = ScopeCategory.ZoneNode.GetOptions() },
                new() { Id = (int)ScopeCategory.WardSector, Name = ScopeCategory.WardSector.ToString(), Description = ScopeCategory.WardSector.GetDescription(), ScopeType = ScopeCategory.WardSector.GetScopeType(), RequiredColumns = ScopeCategory.WardSector.GetOptions() },
                new() { Id = (int)ScopeCategory.BuildingWise, Name = ScopeCategory.BuildingWise.ToString(), Description = ScopeCategory.BuildingWise.GetDescription(), ScopeType = ScopeCategory.BuildingWise.GetScopeType(), RequiredColumns = ScopeCategory.BuildingWise.GetOptions() },
                new() { Id = (int)ScopeCategory.PropertyWise, Name = ScopeCategory.PropertyWise.ToString(), Description = ScopeCategory.PropertyWise.GetDescription(), ScopeType = ScopeCategory.PropertyWise.GetScopeType(), RequiredColumns = ScopeCategory.PropertyWise.GetOptions() }
            }
        };

        return Task.FromResult(template);
    }

    // ---------------------------------------------------------------- Eligible count

    public async Task<EligibleCountResponseDto> GetEligibleCountAsync(
        EligibleCountRequestDto request, int actingUserId, CancellationToken cancellationToken = default)
    {
        var yearEntity = await _yearMasterRepo.GetByIdAsync(request.FinanceYearId, cancellationToken);
        if (yearEntity is null) throw new ArgumentException("Invalid finance year ID.");
        int financeYear = yearEntity.Year;
        var scopeType = ParseScopeType(request.ScopeType);
        var (isPtAll, isAsAll) = await CheckAllSelectedFlagsAsync(request.Scope, cancellationToken);
        var candidates = BuildCandidateQuery(scopeType, request.Scope, yearEntity, isPtAll, isAsAll);

        int total = await candidates.CountAsync(cancellationToken);
        int eligible = await ApplyEligibility(candidates, financeYear).CountAsync(cancellationToken);

        return new EligibleCountResponseDto
        {
            Total = total,
            Eligible = eligible,
            Skipped = Math.Max(0, total - eligible)
        };
    }

    // ---------------------------------------------------------------- Preview

    public async Task<OperationPreviewResponseDto> GetPreviewAsync(
        OperationPreviewRequestDto request, int actingUserId, CancellationToken cancellationToken = default)
    {
        var yearEntity = await _yearMasterRepo.GetByIdAsync(request.FinanceYearId, cancellationToken);
        if (yearEntity is null) throw new ArgumentException("Invalid finance year ID.");
        int financeYear = yearEntity.Year;
        var scopeType = ParseScopeType(request.ScopeType);
        var (isPtAll, isAsAll) = await CheckAllSelectedFlagsAsync(request.Scope, cancellationToken);
        var candidates = BuildCandidateQuery(scopeType, request.Scope, yearEntity, isPtAll, isAsAll);

        int total = await candidates.CountAsync(cancellationToken);

        int pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
        int pageSize = request.PageSize > 0 ? request.PageSize : 10;

        var wards = _wardRepo.GetQueryable().AsNoTracking();
        var zones = _zoneRepo.GetQueryable().AsNoTracking();

        var query = from p in candidates
                    join w in wards on p.WardId equals w.Id into wardJoin
                    from w in wardJoin.DefaultIfEmpty()
                    join z in zones on (w != null ? w.ZoneId : (int?)null) equals z.Id into zoneJoin
                    from z in zoneJoin.DefaultIfEmpty()
                    select new
                    {
                        p.Id,
                        p.PropertyNo,
                        p.PartitionNo,
                        p.OwnerName,
                        p.PropertyTypeId,
                        WardDescription = w != null ? w.Description : string.Empty,
                        ZoneDescription = z != null ? z.Description : string.Empty
                    };

        var page = await query
            .OrderBy(p => p.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var ids = page.Select(p => p.Id).ToList();
        var sets = await GetReasonSetsAsync(ids, financeYear, cancellationToken);

        // Batch-load detail fields for the page's "no details" properties (small set — page-sized)
        var noDetailPageIds = ids.Where(id => !sets.WithDetails.Contains(id) && !sets.Locked.Contains(id)).ToList();
        var detailFields = await GetPropertyDetailFieldsAsync(noDetailPageIds, cancellationToken);

        var records = new List<JobPropertyPreviewDto>(page.Count);
        foreach (var p in page)
        {
            var reason = ResolveSkipReason(p.Id, sets);
            string? skipReasonText = reason switch
            {
                SkipReason.PropertyLocked      => "Property is locked",
                SkipReason.PendingVerification => BuildDetailsSkipReason(detailFields.GetValueOrDefault(p.Id)),
                null                           => null,
                _                              => ToDisplayReason(reason.Value)
            };
            records.Add(new JobPropertyPreviewDto
            {
                PropertyId = p.Id,
                Zone = p.ZoneDescription,
                Ward = p.WardDescription,
                PropertyNo = p.PropertyNo ?? string.Empty,
                PartitionNo = p.PartitionNo ?? string.Empty,
                Owner = p.OwnerName ?? string.Empty,
                PropertyTypeId = p.PropertyTypeId,
                IsEligible = reason is null,
                SkipReason = skipReasonText
            });
        }

        var eligibleQuery = ApplyEligibility(candidates, financeYear);
        int eligibleCount = await eligibleQuery.CountAsync(cancellationToken);
        var breakdown = await BuildSkippedBreakdownAsync(candidates, financeYear, cancellationToken);

        var typeBreakdown = await eligibleQuery
            .GroupBy(p => p.PropertyTypeId)
            .Select(g => new PropertyTypeBreakdownDto
            {
                PropertyTypeId = g.Key ?? 0,
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        return new OperationPreviewResponseDto
        {
            TotalSelected = total,
            Eligible = eligibleCount,
            Skipped = Math.Max(0, total - eligibleCount),
            RequiresApproval = 0, // Add Tax requires no approval; only Remove operations do (future)
            Records = records,
            SkippedReasons = breakdown,
            EligibleBreakdown = typeBreakdown
        };
    }

    // ---------------------------------------------------------------- Execute (synchronous)

    public async Task<ExecuteOperationResponseDto> ExecuteAsync(
        ExecuteOperationRequestDto request, OperationContext context, CancellationToken cancellationToken = default)
    {
        if (request.FinanceYearId <= 0)
            throw new ArgumentException("FinanceYearId is required before execution.");

        var operation = ParseOperation(request.Operation);
        if (operation != JobOperation.AddTax)
            throw new InvalidOperationException("Only the Add Tax operation is supported.");

        var yearEntity = await _yearMasterRepo.GetByIdAsync(request.FinanceYearId, cancellationToken);
        if (yearEntity is null) throw new ArgumentException("Invalid finance year ID.");
        if (!yearEntity.IsActive)
            throw new InvalidOperationException("Tax calculation can only be performed for the current active finance year.");

        int financeYear = yearEntity.Year;
        var scopeType = ParseScopeType(request.ScopeType);
        ValidateScope(scopeType, request.Scope);

        var (isPtAll, isAsAll) = await CheckAllSelectedFlagsAsync(request.Scope, cancellationToken);
        var candidates = BuildCandidateQuery(scopeType, request.Scope, yearEntity, isPtAll, isAsAll);
        var eligibleQuery = ApplyEligibility(candidates, financeYear);

        int totalSelected = await candidates.CountAsync(cancellationToken);
        int eligibleCount = await eligibleQuery.CountAsync(cancellationToken);
        if (eligibleCount == 0)
            throw new InvalidOperationException("No eligible records for the selected scope.");

        var activeJobs = await _jobRepo.GetQueryable().AsNoTracking()
            .Include(j => j.FinanceYear)
            .Where(j => j.IsActive
                && !j.MarkedForDeletion
                && (j.Status == nameof(JobStatus.InProgress) || j.Status == nameof(JobStatus.Pending)))
            .ToListAsync(cancellationToken);

        if (activeJobs.Any())
        {
            // 1. Check InProgress jobs using the high-performance detail records query 
            var inProgressJobIds = activeJobs
                .Where(j => j.Status == nameof(JobStatus.InProgress))
                .Select(j => j.Id)
                .ToList();

            if (inProgressJobIds.Any())
            {
                var overlapExists = await _jobDetailRepo.GetQueryable()
                    .Where(d => inProgressJobIds.Contains(d.JobId) && !d.MarkedForDeletion)
                    .AnyAsync(d => eligibleQuery.Any(q => q.Id == d.PropertyId), cancellationToken);

                if (overlapExists)
                {
                    throw new InvalidOperationException("One or more selected properties overlap with an active running job. Please select different properties.");
                }
            }

            // 2. Check Pending jobs by falling back to scope evaluation (since Pending jobs do not have detail rows yet)
            var pendingJobs = activeJobs
                .Where(j => j.Status == nameof(JobStatus.Pending))
                .ToList();

            if (pendingJobs.Any())
            {
                foreach (var pendingJob in pendingJobs)
                {
                    var activeScopeType = ParseScopeType(pendingJob.ScopeType);
                    var activeScope = JsonSerializer.Deserialize<OperationScopeDto>(pendingJob.ScopeParamsJson ?? "{}", new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (activeScope != null)
                    {
                        var (activePtAll, activeAsAll) = await CheckAllSelectedFlagsAsync(activeScope, cancellationToken);
                        var activeCandidates = BuildCandidateQuery(activeScopeType, activeScope, pendingJob.FinanceYear, activePtAll, activeAsAll);
                        var activeEligible = ApplyEligibility(activeCandidates, pendingJob.FinanceYear.Year);
                        
                        var overlapExists = await activeEligible
                            .AnyAsync(p => eligibleQuery.Any(q => q.Id == p.Id), cancellationToken);
                        
                        if (overlapExists)
                        {
                            throw new InvalidOperationException($"One or more selected properties overlap with the pending job '{pendingJob.JobCode}' (started by {pendingJob.StartedByUserName}). Please select different properties.");
                        }
                    }
                }
            }
        }

        DateTime startTime = DateTime.Now;
        string status = nameof(JobStatus.Pending);

        if (request.Options?.IsScheduled == true)
        {
            status = nameof(JobStatus.Scheduled);
            if (request.Options.ScheduledDateTime.HasValue)
            {
                var scheduledTime = request.Options.ScheduledDateTime.Value.ToLocalTime();
                var today = DateTime.Today;

                if (scheduledTime <= DateTime.Now.AddMinutes(-5))
                {
                    throw new ArgumentException("Scheduled time must be in the future.");
                }

                if (scheduledTime.Date != today && 
                    scheduledTime.Date != today.AddDays(1) && 
                    scheduledTime.Date != today.AddDays(2))
                {
                    throw new ArgumentException("Scheduled time must be for today or tomorrow.");
                }

                startTime = scheduledTime;
            }
            else
            {
                var timeStr = _configuration.GetValue<string>("PropertyTaxJobRecovery:ScheduledTimeOfDay", "00:00:00");
                if (TimeSpan.TryParse(timeStr, out var timeOfDay))
                {
                    var today = DateTime.Today; // local time
                    var scheduledLocal = today.Date + timeOfDay;
                    if (scheduledLocal <= DateTime.Now)
                    {
                        scheduledLocal = scheduledLocal.AddDays(1);
                    }
                    startTime = scheduledLocal;
                }
                else
                {
                    var scheduledLocal = DateTime.Today.AddDays(1);
                    startTime = scheduledLocal;
                }
            }
        }

        var job = new PropertyTaxJobEntity
        {
            JobCode = $"TEMP-{Guid.NewGuid().ToString("N")[..20]}",
            Operation = operation.ToString(),
            FinanceYearId = request.FinanceYearId,
            ScopeType = scopeType.ToString(),
            ScopeParamsJson = JsonSerializer.Serialize(request.Scope),
            ScopeDescription = BuildScopeDescription(scopeType, request.Scope),
            StartedByUserId = context.ActingUserId,
            StartedByUserName = context.UserName,
            UserRole = context.UserRole,
            StartTime = startTime,
            Status = status,
            Remarks = status == nameof(JobStatus.Scheduled)
                ? "job is scheduled, not yet started"
                : "Records are still pending to be processed",
            RecordsSelected = eligibleCount,
            RecordsProcessed = 0,
            SuccessCount = 0,
            SkippedCount = 0,
            CreatedBy = context.ActingUserId,
            CreatedDate = DateTime.Now,
            IsActive = true
        };
        await _jobRepo.AddAsync(job, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Update JobCode using the generated job.Id (Identity column)
        job.JobCode = FormatJobCode(operation, financeYear, job.Id);
        job.UpdatedBy = context.ActingUserId;
        job.UpdatedDate = DateTime.Now;
        await _jobRepo.UpdateAsync(job, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ExecuteOperationResponseDto
        {
            JobId = job.JobCode,
            Status = job.Status,
            Summary = new JobSummaryDto
            {
                Total = job.RecordsSelected,
                Processed = 0,
                Success = 0,
                Failed = 0,
                Skipped = job.SkippedCount
            }
        };
    }

    public async Task ProcessJobAsync(int jobId, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepo.GetQueryable()
            .Include(j => j.FinanceYear)
            .FirstOrDefaultAsync(j => j.Id == jobId && !j.MarkedForDeletion, cancellationToken);
        if (job is null)
        {
            _logger.LogError("ProcessJobAsync: Job ID {JobId} not found.", jobId);
            return;
        }

        _ = ParseOperation(job.Operation);
        var scopeType = ParseScopeType(job.ScopeType);
        var scope = JsonSerializer.Deserialize<OperationScopeDto>(job.ScopeParamsJson ?? "{}", new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

        var (isPtAll, isAsAll) = await CheckAllSelectedFlagsAsync(scope, cancellationToken);
        var candidates = BuildCandidateQuery(scopeType, scope, job.FinanceYear, isPtAll, isAsAll);
        var eligibleQuery = ApplyEligibility(candidates, job.FinanceYear.Year);

        var targets = await eligibleQuery
            .OrderBy(p => p.Id)
            .Select(p => new { p.Id, p.PropertyNo, p.OwnerName })
            .ToListAsync(cancellationToken);

        if (targets.Count == 0)
        {
            job.Status = nameof(JobStatus.Completed);
            job.Remarks = "Completed with 0 eligible records at processing time.";
            job.RecordsProcessed = job.RecordsSelected;
            job.SuccessCount = 0;
            job.FailedCount = 0;
            job.SkippedCount = job.RecordsSelected;
            job.CompleteTime = DateTime.Now;
            job.DurationMs = (long)(job.CompleteTime.Value - job.StartTime).TotalMilliseconds;
            job.UpdatedDate = DateTime.Now;
            await _jobRepo.UpdateAsync(job, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var existingDetails = await _jobDetailRepo.GetQueryable()
            .Where(d => d.JobId == job.Id && !d.MarkedForDeletion)
            .ToListAsync(cancellationToken);

        List<PropertyTaxJobDetailEntity> details;

        if (!existingDetails.Any())
        {
            details = targets.Select(t => new PropertyTaxJobDetailEntity
            {
                JobId = job.Id,
                PropertyId = t.Id,
                PropertyNo = t.PropertyNo,
                Status = nameof(JobDetailStatus.Pending),
                CreatedBy = job.StartedByUserId,
                CreatedDate = DateTime.Now,
                IsActive = true
            }).ToList();

            await _jobDetailRepo.AddRangeAsync(details, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        else
        {
            details = existingDetails;
        }

        var calculationMethod = await _policyConfigService.GetPolicyValueAsync(
            "TaxCalculationMethod", "RV", cancellationToken);

        int batchSize = _configuration.GetValue<int>("PropertyTaxJobProcessor:BatchSize", 50);
        if (batchSize <= 0) batchSize = 50;
        int totalBatches = (int)Math.Ceiling((double)details.Count / batchSize);
        if (totalBatches == 0) totalBatches = 1;

        int success = details.Count(d => d.Status == nameof(JobDetailStatus.Added));
        int failed = details.Count(d => d.Status == nameof(JobDetailStatus.Failed));
        int skipped = details.Count(d => d.Status == nameof(JobDetailStatus.Skipped));

        int processedCount = success + failed + skipped;
        int initialBatch = (processedCount / batchSize) + 1;
        if (initialBatch > totalBatches) initialBatch = totalBatches;

        job.Remarks = $"Batch {initialBatch} of {totalBatches} processing";
        await _jobRepo.UpdateAsync(job, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        int processedSinceLastSave = 0;

        for (int i = 0; i < details.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var detail = details[i];

            if (detail.Status == nameof(JobDetailStatus.Added) || detail.Status == nameof(JobDetailStatus.Skipped))
            {
                continue;
            }

            detail.ExecutionStartTime = DateTime.Now;
            
            try
            {
                if (string.Equals(calculationMethod, "CV", StringComparison.OrdinalIgnoreCase))
                {
                    var cvResults = await _capitalValueService.CreateAsync(new CreateCapitalValueDto
                    {
                        PropertyId = detail.PropertyId,
                        FinanceYear = job.FinanceYear.Year,
                        CreatedBy = job.StartedByUserId
                    }, cancellationToken);

                    var totalTax = cvResults.SelectMany(r => r.Taxes).Sum(t => t.Amount ?? 0);
                    var taxHeads = cvResults.SelectMany(r => r.Taxes)
                        .Select(t => t.TaxName)
                        .Where(name => !string.IsNullOrEmpty(name))
                        .Distinct();

                    detail.Status = nameof(JobDetailStatus.Added);
                    detail.Amount = totalTax;
                    detail.TaxHead = taxHeads.Any() ? string.Join(", ", taxHeads) : NetTaxPolicyCode;
                    detail.Message = "Capital Value Tax added successfully";
                }
                else
                {
                    var rv = await _rateableValueService.CalculateAndSaveAsync(detail.PropertyId);
                    detail.Status = nameof(JobDetailStatus.Added);

                    // Prefer TaxTotal if present to avoid double-counting (TaxTotal is sum of individual taxes)
                    if (rv.Policy?.Taxes != null && rv.Policy.Taxes.TryGetValue("TaxTotal", out var taxTotal))
                    {
                        detail.Amount = taxTotal;
                    }
                    else
                    {
                        detail.Amount = rv.Policy?.Taxes?.Values.Sum() ?? 0m;
                    }

                    detail.TaxHead = rv.Policy?.Taxes.Count > 0
                        ? string.Join(", ", rv.Policy.Taxes.Keys)
                        : NetTaxPolicyCode;
                    detail.Message = "Rateable Value Tax added successfully";
                }
            }
            catch (Exception ex)
            {
                detail.Status = nameof(JobDetailStatus.Failed);
                detail.Message = Truncate(ex.Message, 2000);
                _logger.LogWarning(ex,
                    "Add Tax failed for PropertyId={PropertyId}, JobCode={JobCode}", detail.PropertyId, job.JobCode);
            }

            detail.ExecutionEndTime = DateTime.Now;
            detail.UpdatedBy = job.StartedByUserId;
            detail.UpdatedDate = DateTime.Now;
            await _jobDetailRepo.UpdateAsync(detail, cancellationToken);

            job.SuccessCount = details.Count(d => d.Status == nameof(JobDetailStatus.Added));
            job.FailedCount = details.Count(d => d.Status == nameof(JobDetailStatus.Failed));
            job.SkippedCount = details.Count(d => d.Status == nameof(JobDetailStatus.Skipped));
            job.RecordsProcessed = job.SuccessCount + job.FailedCount + job.SkippedCount;

            int currentBatch = (i / batchSize) + 1;
            if (currentBatch > totalBatches) currentBatch = totalBatches;
            job.Remarks = $"Batch {currentBatch} of {totalBatches} processing";

            job.UpdatedDate = DateTime.Now;
            await _jobRepo.UpdateAsync(job, cancellationToken);
            
            processedSinceLastSave++;
            if (processedSinceLastSave >= batchSize || i == details.Count - 1)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                processedSinceLastSave = 0;
            }
        }

        job.Status = nameof(JobStatus.Completed);
        job.Remarks = "All records processed";
        job.CompleteTime = DateTime.Now;
        job.DurationMs = (long)(job.CompleteTime.Value - job.StartTime).TotalMilliseconds;
        job.SuccessCount = details.Count(d => d.Status == nameof(JobDetailStatus.Added));
        job.FailedCount = details.Count(d => d.Status == nameof(JobDetailStatus.Failed));
        job.SkippedCount = details.Count(d => d.Status == nameof(JobDetailStatus.Skipped));
        job.RecordsProcessed = job.SuccessCount + job.FailedCount + job.SkippedCount;
        job.UpdatedDate = DateTime.Now;
        await _jobRepo.UpdateAsync(job, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // ---------------------------------------------------------------- Job status / properties

    public async Task<JobStatusDto> GetJobStatusAsync(
        int jobId, int actingUserId, CancellationToken cancellationToken = default)
    {
        var job = await GetJobByIdAsync(jobId, cancellationToken);
        int total = job.RecordsSelected;
        int processed = job.RecordsProcessed;
        return new JobStatusDto
        {
            JobId = job.JobCode,
            Status = job.Status,
            Total = total,
            Processed = processed,
            Success = job.SuccessCount,
            Failed = job.FailedCount,
            Pending = Math.Max(0, total - processed),
            Percentage = total > 0 ? (int)Math.Round(processed * 100.0 / total) : 100
        };
    }

    private IQueryable<JobPropertyResultDto> GetJobPropertiesQuery(int jobId)
    {
        var wards = _wardRepo.GetQueryable().AsNoTracking();
        var zones = _zoneRepo.GetQueryable().AsNoTracking();

        return from d in _jobDetailRepo.GetQueryable().AsNoTracking()
               join p in _propertyRepo.GetQueryable().AsNoTracking() on d.PropertyId equals p.Id
               join w in wards on p.WardId equals w.Id into wardJoin
               from w in wardJoin.DefaultIfEmpty()
               join z in zones on (w != null ? w.ZoneId : (int?)null) equals z.Id into zoneJoin
               from z in zoneJoin.DefaultIfEmpty()
               where d.JobId == jobId && !d.MarkedForDeletion
               orderby d.Id
               select new JobPropertyResultDto
               {
                   Zone = z != null ? z.Description : string.Empty,
                   Ward = w != null ? w.Description : string.Empty,
                   WardNo = w != null ? w.WardNo : string.Empty,
                   UPICID = p.UPICId ?? string.Empty,
                   PropertyNo = d.PropertyNo ?? string.Empty,
                   PartitionNo = p.PartitionNo ?? string.Empty,
                   Owner = p.OwnerName ?? string.Empty,
                   TaxHead = d.TaxHead ?? string.Empty,
                   Amount = d.Amount ?? 0m,
                   Status = d.Status,
                   Message = d.Message ?? string.Empty
               };
    }

    public async Task<PagedResult<JobPropertyResultDto>> GetJobPropertiesAsync(
        int jobId, JobPropertiesQueryParameters queryParams, int actingUserId, CancellationToken cancellationToken = default)
    {
        var job = await GetJobByIdAsync(jobId, cancellationToken);
        var query = GetJobPropertiesQuery(job.Id);

        int totalCount = await query.CountAsync(cancellationToken);
        var pageSize = queryParams.PageSize == -1 ? totalCount == 0 ? 1 : totalCount : queryParams.PageSize;

        var items = await query
            .Skip((queryParams.PageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<JobPropertyResultDto>(items, totalCount, queryParams.PageNumber, pageSize);
    }

    // ---------------------------------------------------------------- Audit

    public async Task<PagedResult<JobAuditDto>> GetAuditListAsync(
        OperationAuditQueryParameters query, int actingUserId, CancellationToken cancellationToken = default)
    {
        var q = _jobRepo.GetQueryable().AsNoTracking().Where(j => !j.MarkedForDeletion);

        if (!string.IsNullOrWhiteSpace(query.Operation))
            q = q.Where(j => j.Operation == query.Operation);
        if (query.FinanceYearId.HasValue)
            q = q.Where(j => j.FinanceYearId == query.FinanceYearId.Value);
        if (!string.IsNullOrWhiteSpace(query.Status))
            q = q.Where(j => j.Status == query.Status);
        if (!string.IsNullOrWhiteSpace(query.JobCode))
            q = q.Where(j => j.JobCode == query.JobCode);

        if (query.StartTime.HasValue)
        {
            var date = query.StartTime.Value.Date;
            q = q.Where(j => j.StartTime >= date && j.StartTime < date.AddDays(1));
        }
        if (query.CompleteTime.HasValue)
        {
            var date = query.CompleteTime.Value.Date;
            q = q.Where(j => j.CompleteTime >= date && j.CompleteTime < date.AddDays(1));
        }
        if (query.CreatedDate.HasValue)
        {
            var date = query.CreatedDate.Value.Date;
            q = q.Where(j => j.CreatedDate >= date && j.CreatedDate < date.AddDays(1));
        }

        int totalCount = await q.CountAsync(cancellationToken);

        var pageSize = query.PageSize == -1 ? totalCount == 0 ? 1 : totalCount : query.PageSize;
        var items = await q
            .OrderByDescending(j => j.StartTime)
            .Skip((query.PageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new JobAuditDto
            {
                Id = j.Id,
                JobId = j.JobCode,
                DateTime = j.StartTime,
                Operation = j.Operation,
                DoneBy = j.StartedByUserName ?? j.StartedByUserId.ToString(),
                Scope = j.ScopeDescription ?? j.ScopeType,
                StartTime = j.StartTime,
                CompleteTime = j.CompleteTime,
                Duration = FormatDuration(j.DurationMs),
                Records = $"{j.SuccessCount} / {j.RecordsSelected}",
                Status = j.Status,
                Remarks = j.Remarks
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<JobAuditDto>(items, totalCount, query.PageNumber, pageSize);
    }

    public async Task<JobAuditDetailDto> GetAuditDetailAsync(
        int jobId, JobPropertiesQueryParameters queryParams, int actingUserId, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepo.GetQueryable().AsNoTracking()
            .Include(j => j.FinanceYear)
            .FirstOrDefaultAsync(j => j.Id == jobId && !j.MarkedForDeletion, cancellationToken);
        if (job is null)
            throw new KeyNotFoundException($"Job ID '{jobId}' was not found.");

        var query = GetJobPropertiesQuery(job.Id);

        int totalCount = await query.CountAsync(cancellationToken);
        var pageSize = queryParams.PageSize == -1 ? totalCount == 0 ? 1 : totalCount : queryParams.PageSize;

        var properties = await query
            .Skip((queryParams.PageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var pagedProperties = new PagedResult<JobPropertyResultDto>(properties, totalCount, queryParams.PageNumber, pageSize);

        return new JobAuditDetailDto
        {
            JobId = job.JobCode,
            Operation = job.Operation,
            FinanceYear = job.FinanceYear?.YearCode ?? job.FinanceYear?.Year.ToString() ?? string.Empty,
            StartedBy = job.StartedByUserName ?? job.StartedByUserId.ToString(),
            UserRole = job.UserRole,
            StartTime = job.StartTime,
            CompleteTime = job.CompleteTime,
            Duration = FormatDuration(job.DurationMs),
            Summary = new ProcessingSummaryDto
            {
                TotalSelected = job.RecordsSelected,
                SuccessfullyAdded = job.SuccessCount,
                SkippedRecords = job.SkippedCount,
                Failed = job.FailedCount
            },
            Properties = pagedProperties
        };
    }

    // ---------------------------------------------------------------- Scope & eligibility

    private IQueryable<PropertyEntity> ActiveProperties() =>
        _propertyRepo.GetQueryable().AsNoTracking().Where(p => p.IsActive && !p.MarkedForDeletion);

    /// <summary>
    /// Resolves the effective list of zone IDs to filter by. If the frontend sent explicit
    /// <see cref="OperationScopeDto.ZoneIds"/> those are used directly; otherwise we attempt
    /// a name-based match against <see cref="ZoneEntity.ZoneNo"/> and
    /// <see cref="ZoneEntity.Description"/> using the fallback
    /// <see cref="OperationScopeDto.ZoneNames"/> list populated by the Excel importer when
    /// numeric ZoneIds could not be resolved on the client.
    /// </summary>
    private IReadOnlyCollection<int> ResolveEffectiveZoneIds(OperationScopeDto scope)
    {
        if (scope.ZoneIds is { Count: > 0 }) return scope.ZoneIds;
        if (scope.ZoneNames is { Count: > 0 })
        {
            var candidates = scope.ZoneNames
                .Where(z => !string.IsNullOrWhiteSpace(z))
                .Select(z => z.Trim())
                .ToList();
            if (candidates.Count == 0) return Array.Empty<int>();

            var normalized = candidates
                .Select(z => z.ToLowerInvariant())
                .ToHashSet(StringComparer.Ordinal);

            var stripped = new HashSet<string>(
                normalized.Select(z => new string(z.Where(c => !char.IsWhiteSpace(c) && c != '-' && c != '_').ToArray())),
                StringComparer.Ordinal);

            var zoneIds = _zoneRepo.GetQueryable()
                .AsNoTracking()
                .Select(z => new
                {
                    z.Id,
                    ZoneNoLower = (z.ZoneNo ?? "").ToLower(),
                    DescLower = (z.Description ?? "").ToLower()
                })
                .AsEnumerable()
                .Where(z =>
                    normalized.Contains(z.ZoneNoLower) ||
                    normalized.Contains(z.DescLower) ||
                    stripped.Contains(new string(z.ZoneNoLower.Where(c => !char.IsWhiteSpace(c) && c != '-' && c != '_').ToArray())) ||
                    stripped.Contains(new string(z.DescLower.Where(c => !char.IsWhiteSpace(c) && c != '-' && c != '_').ToArray())))
                .Select(z => z.Id)
                .Distinct()
                .ToList();
            return zoneIds;
        }
        return Array.Empty<int>();
    }

    /// <summary>
    /// Resolves the effective list of ward IDs to filter by. If the frontend sent explicit
    /// <see cref="OperationScopeDto.WardIds"/> those are used directly; otherwise we attempt
    /// a name-based match against <see cref="WardEntity.WardNo"/> and
    /// <see cref="WardEntity.Description"/> using the fallback
    /// <see cref="OperationScopeDto.WardNames"/> list populated by the Excel importer when
    /// numeric WardIds could not be resolved on the client.
    /// </summary>
    private IReadOnlyCollection<int> ResolveEffectiveWardIds(OperationScopeDto scope)
    {
        if (scope.WardIds is { Count: > 0 }) return scope.WardIds;
        if (scope.WardNames is { Count: > 0 })
        {
            var candidates = scope.WardNames
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Select(w => w.Trim())
                .ToList();
            if (candidates.Count == 0) return Array.Empty<int>();

            var normalized = candidates
                .Select(w => w.ToLowerInvariant())
                .ToHashSet(StringComparer.Ordinal);

            var stripped = new HashSet<string>(
                normalized.Select(w => new string(w.Where(c => !char.IsWhiteSpace(c) && c != '-' && c != '_').ToArray())),
                StringComparer.Ordinal);

            var wardIds = _wardRepo.GetQueryable()
                .AsNoTracking()
                .Select(w => new
                {
                    w.Id,
                    WardNoLower = (w.WardNo ?? "").ToLower(),
                    DescLower = (w.Description ?? "").ToLower()
                })
                .AsEnumerable()
                .Where(w =>
                    normalized.Contains(w.WardNoLower) ||
                    normalized.Contains(w.DescLower) ||
                    stripped.Contains(new string(w.WardNoLower.Where(c => !char.IsWhiteSpace(c) && c != '-' && c != '_').ToArray())) ||
                    stripped.Contains(new string(w.DescLower.Where(c => !char.IsWhiteSpace(c) && c != '-' && c != '_').ToArray())))
                .Select(w => w.Id)
                .Distinct()
                .ToList();
            return wardIds;
        }
        return Array.Empty<int>();
    }

    private async Task<(bool isPropertyTypeAllSelected, bool isAssessmentStatusAllSelected)> CheckAllSelectedFlagsAsync(
        OperationScopeDto scope, CancellationToken cancellationToken)
    {
        bool isPropertyTypeAllSelected = false;
        bool isAssessmentStatusAllSelected = false;

        if (scope.PropertyTypeIds is { Count: > 0 } ptids)
        {
            var activePtIds = await _propertyTypeRepo.GetQueryable()
                .Where(pt => pt.IsActive)
                .Select(pt => pt.Id)
                .ToListAsync(cancellationToken);
            isPropertyTypeAllSelected = activePtIds.Count > 0 && activePtIds.All(id => ptids.Contains(id));
        }

        if (scope.AssessmentStatusIds is { Count: > 0 } asids)
        {
            var activeAsIds = await _propertyAssessmentStatusRepo.GetQueryable()
                .Where(status => status.IsActive)
                .Select(status => status.Id)
                .ToListAsync(cancellationToken);
            isAssessmentStatusAllSelected = activeAsIds.Count > 0 && activeAsIds.All(id => asids.Contains(id));
        }

        return (isPropertyTypeAllSelected, isAssessmentStatusAllSelected);
    }

    private IQueryable<PropertyEntity> BuildCandidateQuery(
        JobScopeType scopeType,
        OperationScopeDto scope,
        YearMasterEntity? year = null,
        bool isPropertyTypeAllSelected = false,
        bool isAssessmentStatusAllSelected = false)
    {
        var q = ActiveProperties();

        if (year?.StartDate.HasValue == true && year?.EndDate.HasValue == true)
        {
            q = q.Where(p => p.CreatedDate >= year.StartDate.Value && p.CreatedDate <= year.EndDate.Value);
        }

        switch (scopeType)
        {     

            case JobScopeType.Zone:
                {
                    var effectiveZoneIds = ResolveEffectiveZoneIds(scope);
                    if (effectiveZoneIds.Count > 0)
                        q = q.Where(p => effectiveZoneIds.Contains(p.Ward!.ZoneId));
                    if (scope.PropertyTypeIds is { Count: > 0 } ptids)
                    {
                        if (isPropertyTypeAllSelected)
                            q = q.Where(p => !p.PropertyTypeId.HasValue || ptids.Contains(p.PropertyTypeId.Value));
                        else
                            q = q.Where(p => p.PropertyTypeId.HasValue && ptids.Contains(p.PropertyTypeId.Value));
                    }
                    if (scope.AssessmentStatusIds is { Count: > 0 } asids)
                    {
                        if (isAssessmentStatusAllSelected)
                            q = q.Where(p => !p.PropertyAssessmentStatusId.HasValue || asids.Contains(p.PropertyAssessmentStatusId.Value));
                        else
                            q = q.Where(p => p.PropertyAssessmentStatusId.HasValue && asids.Contains(p.PropertyAssessmentStatusId.Value));
                    }
                    break;
                }

            case JobScopeType.Ward:
                {
                    var effectiveWardIds = ResolveEffectiveWardIds(scope);
                    if (effectiveWardIds.Count > 0)
                        q = q.Where(p => effectiveWardIds.Contains(p.WardId));
                    var effectiveZoneIds = ResolveEffectiveZoneIds(scope);
                    if (effectiveZoneIds.Count > 0)
                        q = q.Where(p => effectiveZoneIds.Contains(p.Ward!.ZoneId));
                    if (scope.PropertyTypeIds is { Count: > 0 } ptids2)
                    {
                        if (isPropertyTypeAllSelected)
                            q = q.Where(p => !p.PropertyTypeId.HasValue || ptids2.Contains(p.PropertyTypeId.Value));
                        else
                            q = q.Where(p => p.PropertyTypeId.HasValue && ptids2.Contains(p.PropertyTypeId.Value));
                    }
                    if (scope.AssessmentStatusIds is { Count: > 0 } asids2)
                    {
                        if (isAssessmentStatusAllSelected)
                            q = q.Where(p => !p.PropertyAssessmentStatusId.HasValue || asids2.Contains(p.PropertyAssessmentStatusId.Value));
                        else
                            q = q.Where(p => p.PropertyAssessmentStatusId.HasValue && asids2.Contains(p.PropertyAssessmentStatusId.Value));
                    }
                    break;
                }

            case JobScopeType.Building:
                {
                    var effectiveZoneIds = ResolveEffectiveZoneIds(scope);
                    var effectiveWardIds = ResolveEffectiveWardIds(scope);
                    var hasBuildings = scope.Building is { Count: > 0 };

                    // -----------------------------------------------------------------
                    // SAFETY NET FOR EXCEL UPLOAD: If the user uploaded a building-list
                    // scope but NEITHER zone IDs/names NOR ward IDs/names could be
                    // resolved (e.g. frontend ward lookup failed), then do NOT apply the
                    // (PropertyNo, PartitionNo) building filter across ALL wards — that
                    // would match identical (PropNo, PartNo) combos in unrelated wards
                    // and produce hundreds of unintended records (see: 12 Excel rows
                    // yielding 133 records). Instead, return an empty result set so the
                    // counts clearly signal that the scope is invalid.
                    // -----------------------------------------------------------------
                    if (hasBuildings && effectiveZoneIds.Count == 0 && effectiveWardIds.Count == 0
                        && (scope.WardNames is { Count: > 0 } || scope.ZoneNames is { Count: > 0 })
                        && !scope.Building!.Any(b => b.Contains(':')))
                    {
                        // The user DID specify ward/zone names but they didn't match any
                        // master records → treat as "no valid scope" rather than matching
                        // the whole system.
                        return q.Take(0);
                    }

                    if (hasBuildings)
                    {
                        var buildings = scope.Building!;
                        if (buildings.Any(b => b.Contains(':')))
                        {
                            var parameter = System.Linq.Expressions.Expression.Parameter(typeof(PropertyEntity), "p");
                            System.Linq.Expressions.Expression? body = null;

                            // Cache ward lookups to avoid redundant DB queries in the loop
                            var wardCache = new Dictionary<string, int>();

                            foreach (var b in buildings)
                            {
                                if (b.Contains(':'))
                                {
                                    var parts = b.Split(':');
                                    var wardKey = parts[0]?.Trim();
                                    var propVal = parts.Length > 1 ? parts[1]?.Trim() : string.Empty;

                                    if (string.IsNullOrEmpty(wardKey) || string.IsNullOrEmpty(propVal))
                                        continue;

                                    int? wardId = null;
                                    if (int.TryParse(wardKey, out var parsedWardId))
                                    {
                                        wardId = parsedWardId;
                                    }
                                    else
                                    {
                                        if (wardCache.TryGetValue(wardKey, out var cachedId))
                                        {
                                            wardId = cachedId;
                                        }
                                        else
                                        {
                                            var wardEntity = _wardRepo.GetQueryable()
                                                .FirstOrDefault(w => w.IsActive && 
                                                    (w.WardNo == wardKey || w.Description == wardKey));
                                            if (wardEntity != null)
                                            {
                                                wardId = wardEntity.Id;
                                                wardCache[wardKey] = wardEntity.Id;
                                            }
                                        }
                                    }

                                    if (wardId.HasValue)
                                    {
                                        string propNo = propVal;
                                        string partNo = string.Empty;
                                        if (propVal.Contains('-'))
                                        {
                                            var subParts = propVal.Split('-');
                                            propNo = subParts[0]?.Trim() ?? string.Empty;
                                            partNo = subParts.Length > 1 ? subParts[1]?.Trim() ?? string.Empty : string.Empty;
                                        }

                                        var wardEqual = System.Linq.Expressions.Expression.Equal(
                                            System.Linq.Expressions.Expression.Property(parameter, nameof(PropertyEntity.WardId)),
                                            System.Linq.Expressions.Expression.Constant(wardId.Value)
                                        );
                                        var propEqual = System.Linq.Expressions.Expression.Equal(
                                            System.Linq.Expressions.Expression.Property(parameter, nameof(PropertyEntity.PropertyNo)),
                                            System.Linq.Expressions.Expression.Constant(propNo)
                                        );

                                        System.Linq.Expressions.Expression rowExpr = System.Linq.Expressions.Expression.AndAlso(wardEqual, propEqual);

                                        if (!string.IsNullOrEmpty(partNo))
                                        {
                                            var partEqual = System.Linq.Expressions.Expression.Equal(
                                                System.Linq.Expressions.Expression.Property(parameter, nameof(PropertyEntity.PartitionNo)),
                                                System.Linq.Expressions.Expression.Constant(partNo)
                                            );
                                            rowExpr = System.Linq.Expressions.Expression.AndAlso(rowExpr, partEqual);
                                        }
                                        else
                                        {
                                            var partProp = System.Linq.Expressions.Expression.Property(parameter, nameof(PropertyEntity.PartitionNo));
                                            var partNull = System.Linq.Expressions.Expression.Equal(partProp, System.Linq.Expressions.Expression.Constant(null, typeof(string)));
                                            var partEmpty = System.Linq.Expressions.Expression.Equal(partProp, System.Linq.Expressions.Expression.Constant(string.Empty));
                                            var partNullOrEmpty = System.Linq.Expressions.Expression.OrElse(partNull, partEmpty);
                                            rowExpr = System.Linq.Expressions.Expression.AndAlso(rowExpr, partNullOrEmpty);
                                        }

                                        body = body == null ? rowExpr : System.Linq.Expressions.Expression.OrElse(body, rowExpr);
                                    }
                                }
                            }

                            if (body != null)
                            {
                                var lambda = System.Linq.Expressions.Expression.Lambda<Func<PropertyEntity, bool>>(body, parameter);
                                q = q.Where(lambda);
                            }
                            else
                            {
                                return q.Take(0);
                            }
                        }
                        else
                        {
                            if (effectiveZoneIds.Count > 0)
                                q = q.Where(p => effectiveZoneIds.Contains(p.Ward!.ZoneId));
                            if (effectiveWardIds.Count > 0)
                                q = q.Where(p => effectiveWardIds.Contains(p.WardId));

                            if (buildings.Any(b => b.Contains('-')))
                            {
                                var pairs = buildings
                                    .Select(b =>
                                    {
                                        var parts = b.Split('-');
                                        return (PropNo: parts[0]?.Trim(), PartNo: parts.Length > 1 ? parts[1]?.Trim() : string.Empty);
                                    })
                                    .Where(pair => !string.IsNullOrEmpty(pair.PropNo))
                                    .ToList();

                                var parameter = System.Linq.Expressions.Expression.Parameter(typeof(PropertyEntity), "p");
                                System.Linq.Expressions.Expression? body = null;

                                foreach (var pair in pairs)
                                {
                                    var propEqual = System.Linq.Expressions.Expression.Equal(
                                        System.Linq.Expressions.Expression.Property(parameter, nameof(PropertyEntity.PropertyNo)),
                                        System.Linq.Expressions.Expression.Constant(pair.PropNo)
                                    );
                                    var partEqual = System.Linq.Expressions.Expression.Equal(
                                        System.Linq.Expressions.Expression.Property(parameter, nameof(PropertyEntity.PartitionNo)),
                                        System.Linq.Expressions.Expression.Constant(pair.PartNo)
                                    );
                                    var andExpr = System.Linq.Expressions.Expression.AndAlso(propEqual, partEqual);

                                    body = body == null ? andExpr : System.Linq.Expressions.Expression.OrElse(body, andExpr);
                                }

                                if (body != null)
                                {
                                    var lambda = System.Linq.Expressions.Expression.Lambda<Func<PropertyEntity, bool>>(body, parameter);
                                    q = q.Where(lambda);
                                }
                            }
                            else
                            {
                                q = q.Where(p => buildings.Contains(p.PropertyNo ?? string.Empty));
                                if (scope.PartitionNos is { Count: > 0 } partNos3)
                                {
                                    q = q.Where(p => partNos3.Contains(p.PartitionNo ?? string.Empty));
                                }
                            }
                        }
                    }
                    else
                    {
                        if (effectiveZoneIds.Count > 0)
                            q = q.Where(p => effectiveZoneIds.Contains(p.Ward!.ZoneId));
                        if (effectiveWardIds.Count > 0)
                            q = q.Where(p => effectiveWardIds.Contains(p.WardId));
                    }
                    break;
                }

            case JobScopeType.Property:
                {
                    if (scope.PropertyIds is { Count: > 0 } pids)
                    {
                        q = q.Where(p => pids.Contains(p.Id));
                    }
                    if (scope.UpicIds is { Count: > 0 } upics)
                    {
                        q = q.Where(p => p.UPICId != null && upics.Contains(p.UPICId));
                    }
                    if (scope.MobileNumbers is { Count: > 0 } mobiles)
                    {
                        q = q.Where(p => p.MobileNo != null && mobiles.Contains(p.MobileNo));
                    }
                    if (!string.IsNullOrWhiteSpace(scope.SearchText))
                    {
                        var s = scope.SearchText.Trim();
                        q = q.Where(p =>
                            p.MobileNo == s ||
                            p.UPICId == s);
                    }
                    break;
                }

            case JobScopeType.Range:
                var rangeQ = ActiveProperties();
                if (year?.StartDate.HasValue == true && year?.EndDate.HasValue == true)
                {
                    rangeQ = rangeQ.Where(p => p.CreatedDate >= year.StartDate.Value && p.CreatedDate <= year.EndDate.Value);
                }

                if (scope.ZoneIds is { Count: > 0 } zids4) rangeQ = rangeQ.Where(p => zids4.Contains(p.Ward!.ZoneId));
                if (scope.WardIds is { Count: > 0 } wIds4) rangeQ = rangeQ.Where(p => wIds4.Contains(p.WardId));
                if (scope.PartitionNos is { Count: > 0 } partNos4) rangeQ = rangeQ.Where(p => partNos4.Contains(p.PartitionNo ?? string.Empty));
                if (scope.PropertyTypeIds is { Count: > 0 } ptids4)
                {
                    if (isPropertyTypeAllSelected)
                        rangeQ = rangeQ.Where(p => !p.PropertyTypeId.HasValue || ptids4.Contains(p.PropertyTypeId.Value));
                    else
                        rangeQ = rangeQ.Where(p => p.PropertyTypeId.HasValue && ptids4.Contains(p.PropertyTypeId.Value));
                }
                if (scope.AssessmentStatusIds is { Count: > 0 } asids4)
                {
                    if (isAssessmentStatusAllSelected)
                        rangeQ = rangeQ.Where(p => !p.PropertyAssessmentStatusId.HasValue || asids4.Contains(p.PropertyAssessmentStatusId.Value));
                    else
                        rangeQ = rangeQ.Where(p => p.PropertyAssessmentStatusId.HasValue && asids4.Contains(p.PropertyAssessmentStatusId.Value));
                }

                var propertiesInWard = rangeQ
                    .Select(p => new { p.Id, p.PropertyNo, p.PartitionNo })
                    .ToList();

                var matchedIds = new List<int>();
                foreach (var p in propertiesInWard)
                {
                    var computed = string.IsNullOrEmpty(p.PartitionNo) 
                        ? p.PropertyNo 
                        : p.PropertyNo + "-" + p.PartitionNo;

                    bool matchesFrom = string.IsNullOrWhiteSpace(scope.FromPropertyNo) || 
                        NtisPlatform.Application.Utilities.NaturalStringComparer.Instance.Compare(computed, scope.FromPropertyNo) >= 0;

                    bool matchesTo = string.IsNullOrWhiteSpace(scope.ToPropertyNo) || 
                        NtisPlatform.Application.Utilities.NaturalStringComparer.Instance.Compare(computed, scope.ToPropertyNo) <= 0;

                    if (matchesFrom && matchesTo)
                    {
                        matchedIds.Add(p.Id);
                    }
                }

                q = q.Where(p => matchedIds.Contains(p.Id));
                break;
        }

        return q;
    }

    private IQueryable<PropertyEntity> ApplyEligibility(IQueryable<PropertyEntity> candidates, int financeYear)
    {
        // Note: financeYear is currently unused but kept for future eligibility checks (e.g. AlreadyProcessed check).
        var locked = LockedPropertyIds();
        var withDetails = PropertiesWithDetails();

        return candidates
            .Where(p => !locked.Contains(p.Id))
            .Where(p => withDetails.Contains(p.Id));
    }

    private IQueryable<int> LockedPropertyIds() =>
        _lockRepo.GetQueryable().AsNoTracking()
            .Where(l => l.IsLocked && l.IsActive && !l.MarkedForDeletion)
            .Select(l => l.PropertyId);

    private IQueryable<int> PropertiesWithDetails() =>
        _propertyDetailsRepo.GetQueryable().AsNoTracking()
            .Where(d => d.IsActive)
            .Select(d => d.PropertyId);

    private async Task<List<SkippedReasonDto>> BuildSkippedBreakdownAsync(
        IQueryable<PropertyEntity> candidates, int financeYear, CancellationToken ct)
    {
        var locked = LockedPropertyIds();
        var withDetails = PropertiesWithDetails();

        int lockedCount = await candidates.CountAsync(p => locked.Contains(p.Id), ct);
        int pendingCount = await candidates.CountAsync(p => !withDetails.Contains(p.Id), ct);

        return new List<SkippedReasonDto>
        {
            new() { Reason = ToLocalizationKey(SkipReason.PropertyLocked), Count = lockedCount },
            new() { Reason = ToLocalizationKey(SkipReason.PendingVerification), Count = pendingCount }
        };
    }

    private async Task<ReasonSets> GetReasonSetsAsync(List<int> ids, int financeYear, CancellationToken ct)
    {
        if (ids.Count == 0) return new ReasonSets(new(), new());

        var locked = (await _lockRepo.GetQueryable().AsNoTracking()
            .Where(l => l.IsLocked && l.IsActive && !l.MarkedForDeletion && ids.Contains(l.PropertyId))
            .Select(l => l.PropertyId).Distinct().ToListAsync(ct)).ToHashSet();

        var withDetails = (await _propertyDetailsRepo.GetQueryable().AsNoTracking()
            .Where(d => d.IsActive && ids.Contains(d.PropertyId))
            .Select(d => d.PropertyId).Distinct().ToListAsync(ct)).ToHashSet();

        return new ReasonSets(locked, withDetails);
    }

    private static SkipReason? ResolveSkipReason(int propertyId, ReasonSets sets)
    {
        if (sets.Locked.Contains(propertyId)) return SkipReason.PropertyLocked;
        if (!sets.WithDetails.Contains(propertyId)) return SkipReason.PendingVerification;
        return null;
    }

    // ---------------------------------------------------------------- Helpers

    private async Task<PropertyTaxJobEntity> GetJobByIdAsync(int jobId, CancellationToken ct)
    {
        var job = await _jobRepo.GetQueryable().AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId && !j.MarkedForDeletion, ct);
        if (job is null)
            throw new KeyNotFoundException($"Job ID '{jobId}' was not found.");
        return job;
    }

    private static string FormatJobCode(JobOperation operation, int financeYear, int jobId)
    {
        return $"JOB-{OperationPrefix(operation)}-{financeYear}-{jobId:D4}";
    }

    private static string OperationPrefix(JobOperation operation) => operation switch
    {
        JobOperation.AddTax => "ADD",
        JobOperation.QuarterlyAdd => "QADD",
        JobOperation.RemoveTax => "REM",
        JobOperation.QuarterlyRemove => "QREM",
        _ => "OP"
    };

    private static void ValidateScope(JobScopeType scopeType, OperationScopeDto scope)
    {
        switch (scopeType)
        {
            case JobScopeType.Zone when (scope.ZoneIds is null || scope.ZoneIds.Count == 0)
                                       && (scope.ZoneNames is null || scope.ZoneNames.Count == 0):
                throw new ArgumentException("At least one zone is required for the selected scope.");
            case JobScopeType.Ward when (scope.WardIds is null || scope.WardIds.Count == 0)
                                       && (scope.WardNames is null || scope.WardNames.Count == 0):
                throw new ArgumentException("At least one ward is required for the selected scope.");
            case JobScopeType.Building when ((scope.ZoneIds is null || scope.ZoneIds.Count == 0)
                                             && (scope.ZoneNames is null || scope.ZoneNames.Count == 0))
                                         || (scope.WardIds is null || scope.WardIds.Count == 0)
                                             && (scope.WardNames is null || scope.WardNames.Count == 0)
                                         || scope.Building is null || scope.Building.Count == 0:
                throw new ArgumentException("Zone, ward and building are required for the selected scope.");
            case JobScopeType.Property when (scope.PropertyIds is null || scope.PropertyIds.Count == 0)
                                            && (scope.UpicIds is null || scope.UpicIds.Count == 0)
                                            && (scope.MobileNumbers is null || scope.MobileNumbers.Count == 0)
                                            && string.IsNullOrWhiteSpace(scope.SearchText):
                throw new ArgumentException("Select at least one property for the selected scope.");
            case JobScopeType.Range when string.IsNullOrWhiteSpace(scope.FromPropertyNo)
                                         || string.IsNullOrWhiteSpace(scope.ToPropertyNo):
                throw new ArgumentException("Both From and To property numbers are required for the selected scope.");
        }
    }

    private static string BuildScopeDescription(JobScopeType scopeType, OperationScopeDto scope)
    {
        var category = scopeType switch
        {
            JobScopeType.Zone => ScopeCategory.ZoneNode,
            JobScopeType.Ward => ScopeCategory.WardSector,
            JobScopeType.Building => ScopeCategory.BuildingWise,
            JobScopeType.Property => ScopeCategory.PropertyWise,
            JobScopeType.Range => ScopeCategory.PropertyRange,
            _ => throw new ArgumentException($"Unknown scope type '{scopeType}'.")
        };
        return category.GetDescription();
    }

    private static JobScopeType ParseScopeType(string value) =>
        Enum.TryParse<JobScopeType>(value, ignoreCase: true, out var parsed)
            ? parsed
            : throw new ArgumentException($"Unknown scope type '{value}'.");

    private static JobOperation ParseOperation(string value) =>
        Enum.TryParse<JobOperation>(value, ignoreCase: true, out var parsed)
            ? parsed
            : throw new ArgumentException($"Unknown operation '{value}'.");

    private static string FormatDuration(long? durationMs)
    {
        if (durationMs is null) return string.Empty;
        var span = TimeSpan.FromMilliseconds(durationMs.Value);
        return span.TotalMinutes >= 1
            ? $"{(int)span.TotalMinutes}m {span.Seconds}s"
            : $"{span.Seconds}s";
    }

    private static string ToLocalizationKey(SkipReason reason) => reason switch
    {
        SkipReason.AlreadyProcessed => "Skip_AlreadyProcessed",
        SkipReason.PropertyLocked => "Skip_PropertyLocked",
        SkipReason.PendingVerification => "Skip_PropertyDetailsMissing",
        SkipReason.InvalidScope => "Skip_InvalidScope",
        SkipReason.PermissionRestricted => "Skip_PermissionRestricted",
        SkipReason.ApprovalRequired => "Skip_ApprovalRequired",
        _ => reason.ToString()
    };

    private static string Truncate(string value, int max) =>
        string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];

    private sealed record ReasonSets(HashSet<int> Locked, HashSet<int> WithDetails);
    private static readonly string DefaultDetailsMissingReason =
        "Property details not found: Floor type, Construction type, Carpet area, Built-up area, Type of use, Assessment year missing";

    public async Task WritePreviewExportCsvToStreamAsync(
        Stream outputStream,
        OperationPreviewRequestDto request,
        string downloadType,
        CancellationToken cancellationToken = default)
    {
        var yearEntity = await _yearMasterRepo.GetByIdAsync(request.FinanceYearId, cancellationToken);
        if (yearEntity is null) throw new ArgumentException("Invalid finance year ID.");
        int financeYear = yearEntity.Year;

        var scopeType = ParseScopeType(request.ScopeType);
        var (isPtAll, isAsAll) = await CheckAllSelectedFlagsAsync(request.Scope, cancellationToken);
        var candidates = BuildCandidateQuery(scopeType, request.Scope, yearEntity, isPtAll, isAsAll);

        var locks = _lockRepo.GetQueryable().AsNoTracking();
        var details = _propertyDetailsRepo.GetQueryable().AsNoTracking();

        // Apply eligibility filter based on downloadType
        if (string.Equals(downloadType, "eligible", StringComparison.OrdinalIgnoreCase))
        {
            candidates = candidates.Where(p =>
                !locks.Any(l => l.PropertyId == p.Id && l.IsLocked && l.IsActive && !l.MarkedForDeletion) &&
                details.Any(d => d.PropertyId == p.Id && d.IsActive)
            );
        }
        else if (string.Equals(downloadType, "skipped", StringComparison.OrdinalIgnoreCase))
        {
            candidates = candidates.Where(p =>
                locks.Any(l => l.PropertyId == p.Id && l.IsLocked && l.IsActive && !l.MarkedForDeletion) ||
                !details.Any(d => d.PropertyId == p.Id && d.IsActive)
            );
        }
        // else "all" — no extra filter, use candidates as-is

        var wards = _wardRepo.GetQueryable().AsNoTracking();
        var zones = _zoneRepo.GetQueryable().AsNoTracking();

        var query = from p in candidates
                    join w in wards on p.WardId equals w.Id into wardJoin
                    from w in wardJoin.DefaultIfEmpty()
                    join z in zones on (w != null ? w.ZoneId : (int?)null) equals z.Id into zoneJoin
                    from z in zoneJoin.DefaultIfEmpty()
                    select new
                    {
                        PropertyId = p.Id,
                        Zone = z != null ? z.Description : string.Empty,
                        Ward = w != null ? w.Description : string.Empty,
                        PropertyNo = p.PropertyNo ?? string.Empty,
                        PartitionNo = p.PartitionNo ?? string.Empty,
                        OwnerName = p.OwnerName ?? string.Empty,
                        IsLocked = locks.Any(l => l.PropertyId == p.Id && l.IsLocked && l.IsActive && !l.MarkedForDeletion),
                        HasDetails = details.Any(d => d.PropertyId == p.Id && d.IsActive)
                    };

        await using var writer = new StreamWriter(outputStream, new System.Text.UTF8Encoding(true), bufferSize: 1024, leaveOpen: true);

        await writer.WriteLineAsync("Zone,Ward,Property No,Partition No,Owner,Status,Skip Reason");

        int count = 0;
        await foreach (var item in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            bool isEligible = !item.IsLocked && item.HasDetails;
            string status = isEligible ? "Eligible" : "Skipped";
            string skipReason = isEligible ? "-"
                : item.IsLocked ? "Property is locked"
                : DefaultDetailsMissingReason;

            string zone = EscapeCsvField(item.Zone);
            string ward = EscapeCsvField(item.Ward);
            string propNo = EscapeCsvField(item.PropertyNo);
            string partNo = EscapeCsvField(item.PartitionNo);
            string owner = EscapeCsvField(item.OwnerName);
            string reason = EscapeCsvField(skipReason);

            await writer.WriteLineAsync($"{zone},{ward},{propNo},{partNo},{owner},{status},{reason}");

            count++;
            if (count % 500 == 0)
            {
                await writer.FlushAsync(cancellationToken);
            }
        }
    }

    public async Task WritePropertiesCsvToStreamAsync(
        Stream outputStream,
        string statusFilter,
        int? financeYearId = null,
        CancellationToken cancellationToken = default)
    {
        var candidates = _propertyRepo.GetQueryable().AsNoTracking()
            .Where(p => p.IsActive && !p.MarkedForDeletion);

        var resolvedYearId = financeYearId;
        if (!resolvedYearId.HasValue || resolvedYearId <= 0)
        {
            var today = DateTime.Today;
            var activeYear = await _yearMasterRepo.GetQueryable().AsNoTracking()
                .FirstOrDefaultAsync(y => y.IsActive, cancellationToken)
                ?? await _yearMasterRepo.GetQueryable().AsNoTracking()
                    .FirstOrDefaultAsync(y => y.StartDate <= today && y.EndDate >= today, cancellationToken)
                    ?? await _yearMasterRepo.GetQueryable().AsNoTracking()
                        .FirstOrDefaultAsync(cancellationToken);
            
            resolvedYearId = activeYear?.Id;
        }

        if (resolvedYearId.HasValue && resolvedYearId > 0)
        {
            var yearEntity = await _yearMasterRepo.GetByIdAsync(resolvedYearId.Value, cancellationToken);
            if (yearEntity?.StartDate.HasValue == true && yearEntity?.EndDate.HasValue == true)
            {
                candidates = candidates.Where(p => p.CreatedDate >= yearEntity.StartDate.Value && p.CreatedDate <= yearEntity.EndDate.Value);
            }
        }

        var locks = _lockRepo.GetQueryable().AsNoTracking();
        var details = _propertyDetailsRepo.GetQueryable().AsNoTracking();

        // 1. Filter directly in SQL
        if (string.Equals(statusFilter, "eligible", StringComparison.OrdinalIgnoreCase))
        {
            candidates = candidates.Where(p => 
                !locks.Any(l => l.PropertyId == p.Id && l.IsLocked && l.IsActive && !l.MarkedForDeletion) &&
                details.Any(d => d.PropertyId == p.Id && d.IsActive)
            );
        }
        else if (string.Equals(statusFilter, "skipped", StringComparison.OrdinalIgnoreCase))
        {
            candidates = candidates.Where(p => 
                locks.Any(l => l.PropertyId == p.Id && l.IsLocked && l.IsActive && !l.MarkedForDeletion) ||
                !details.Any(d => d.PropertyId == p.Id && d.IsActive)
            );
        }

        var wards = _wardRepo.GetQueryable().AsNoTracking();
        var zones = _zoneRepo.GetQueryable().AsNoTracking();

        // 2. Fetch list using EF Core's ToListAsync with explicit Left Joins to ensure no rows are lost
        var query = from p in candidates
                    join w in wards on p.WardId equals w.Id into wardJoin
                    from w in wardJoin.DefaultIfEmpty()
                    join z in zones on (w != null ? w.ZoneId : (int?)null) equals z.Id into zoneJoin
                    from z in zoneJoin.DefaultIfEmpty()
                    select new
                    {
                        PropertyId = p.Id,
                        Zone = z != null ? z.Description : string.Empty,
                        Ward = w != null ? w.Description : string.Empty,
                        PropertyNo = p.PropertyNo ?? string.Empty,
                        PartitionNo = p.PartitionNo ?? string.Empty,
                        OwnerName = p.OwnerName ?? string.Empty,
                        IsLocked = locks.Any(l => l.PropertyId == p.Id && l.IsLocked && l.IsActive && !l.MarkedForDeletion),
                        HasDetails = details.Any(d => d.PropertyId == p.Id && d.IsActive)
                    };

        await using var writer = new StreamWriter(outputStream, new System.Text.UTF8Encoding(true), bufferSize: 1024, leaveOpen: true);

        // Write headers
        await writer.WriteLineAsync("Zone,Ward,Property No,Partition No,Owner,Status,Skip Reason");

        int count = 0;
        await foreach (var item in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            bool isEligible = !item.IsLocked && item.HasDetails;
            string status = isEligible ? "Eligible" : "Skipped";
            string skipReason = isEligible ? "-"
                : item.IsLocked ? "Property is locked"
                : DefaultDetailsMissingReason;

            // Escape fields for CSV format safety & formula injection protection
            var zone = EscapeCsvField(item.Zone);
            var ward = EscapeCsvField(item.Ward);
            var propNo = EscapeCsvField(item.PropertyNo);
            var partNo = EscapeCsvField(item.PartitionNo);
            var owner = EscapeCsvField(item.OwnerName);
            var reason = EscapeCsvField(skipReason);

            await writer.WriteLineAsync($"{zone},{ward},{propNo},{partNo},{owner},{status},{reason}");

            count++;
            if (count % 500 == 0)
            {
                await writer.FlushAsync(cancellationToken);
            }
        }
    }

    // ---------------------------------------------------------------- Detail-level skip reason helpers

    private sealed record PropertyDetailFields(
        int? FloorId, int? ConstructionTypeId,
        double? CarpetAreaSqMeter, double? CarpetAreaSqFeet,
        double? BuiltupAreaSqMeter, double? BuiltupAreaSqFeet,
        int? TypeOfUseId, string? AssessmentYear);

    /// <summary>
    /// Builds a human-readable skip reason that lists exactly which required PropertyDetails
    /// fields are absent. When <paramref name="fields"/> is null the property has no active
    /// details record at all, so every required field is reported as missing.
    /// </summary>
    private static string BuildDetailsSkipReason(PropertyDetailFields? fields)
    {
        var missing = new List<string>();

        if (fields is null || !fields.FloorId.HasValue)                                     missing.Add("Floor type");
        if (fields is null || !fields.ConstructionTypeId.HasValue)                          missing.Add("Construction type");
        if (fields is null || (!fields.CarpetAreaSqMeter.HasValue && !fields.CarpetAreaSqFeet.HasValue))   missing.Add("Carpet area");
        if (fields is null || (!fields.BuiltupAreaSqMeter.HasValue && !fields.BuiltupAreaSqFeet.HasValue)) missing.Add("Built-up area");
        if (fields is null || !fields.TypeOfUseId.HasValue || fields.TypeOfUseId <= 0)      missing.Add("Type of use");
        if (fields is null || string.IsNullOrWhiteSpace(fields.AssessmentYear))             missing.Add("Assessment year");

        return missing.Count > 0
            ? $"Property details not found: {string.Join(", ", missing)} missing"
            : "Property details complete";
    }

    /// <summary>
    /// Batch-loads the required detail fields for a set of property IDs in a single SQL query.
    /// Returns null for any ID that has no active PropertyDetails record.
    /// </summary>
    private async Task<Dictionary<int, PropertyDetailFields>> GetPropertyDetailFieldsAsync(
        IList<int> propertyIds, CancellationToken cancellationToken)
    {
        if (propertyIds.Count == 0) return new Dictionary<int, PropertyDetailFields>();

        var rows = await _propertyDetailsRepo.GetQueryable().AsNoTracking()
            .Where(d => d.IsActive && propertyIds.Contains(d.PropertyId))
            .GroupBy(d => d.PropertyId)
            .Select(g => new
            {
                PropertyId       = g.Key,
                FloorId          = g.Min(d => d.FloorId),
                ConstructionType = g.Min(d => d.ConstructionTypeId),
                CarpetSqm        = g.Min(d => d.CarpetAreaSqMeter),
                CarpetSqft       = g.Min(d => d.CarpetAreaSqFeet),
                BuiltupSqm       = g.Min(d => d.BuiltupAreaSqMeter),
                BuiltupSqft      = g.Min(d => d.BuiltupAreaSqFeet),
                TypeOfUse        = g.Min(d => d.TypeOfUseId),
                AssessmentYear   = g.Min(d => d.AssessmentYear)
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            r => r.PropertyId,
            r => new PropertyDetailFields(
                r.FloorId,
                r.ConstructionType,
                r.CarpetSqm,
                r.CarpetSqft,
                r.BuiltupSqm,
                r.BuiltupSqft,
                r.TypeOfUse,
                r.AssessmentYear));
    }

    /// <summary>
    /// Returns a plain-English display message for a skip reason (locked case only).
    /// Details-missing cases use <see cref="BuildDetailsSkipReason"/> for field-level specifics.
    /// </summary>
    private static string ToDisplayReason(SkipReason reason) => reason switch
    {
        SkipReason.PropertyLocked => "Property is locked",
        _                         => reason.ToString()
    };

    private static string EscapeCsvField(string? field)
    {
        if (string.IsNullOrEmpty(field)) return string.Empty;
        
        // Neutralize formula injection
        char first = field[0];
        if (first == '=' || first == '+' || first == '-' || first == '@')
        {
            field = "'" + field;
        }
        
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}
