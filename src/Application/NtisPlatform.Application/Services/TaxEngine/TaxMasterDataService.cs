using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.TaxEngine
{
    /// <summary>
    /// EF Core-backed implementation of <see cref="ITaxMasterDataService"/>.
    /// </summary>
    public class TaxMasterDataService : ITaxMasterDataService
    {

        private readonly IRepository<TypeOfUseEntity, int> _typeOfUseRepo;
        private readonly IRepository<PropertyCategoryEntity, int> _propertyCategoryRepo;
        private readonly IRepository<SubTypeOfUseEntity, int> _subTypeOfUseRepo;
        private readonly IRepository<FloorEntity, int> _floorRepo;
        private readonly IRepository<SubFloorEntity, int> _subFloorRepo;
        private readonly IRepository<ConstructionTypeEntity, int> _constructionTypeRepo;
        private readonly IRepository<RateEntity, int> _rateRepo;
        private readonly IRepository<RateSectionEntity, int> _rateSectionRepo;
        private readonly IRepository<RateSectionDetailsEntity, int> _rateSectionDetailsRepo;
        private readonly IRepository<DepreciationMasterEntity, int> _depreciationRepo;
        private readonly IRepository<AssessmentYearRangeEntity, int> _yearRangeRepo;
        private readonly IRepository<TaxMasterEntity, int> _taxMasterRepo;
        private readonly IRepository<TaxPercentageMasterRVEntity, int> _taxPercentageRepo;
        private readonly IRepository<EducationTaxMasterEntity, int> _educationTaxRepo;
        private readonly IRepository<EmploymentTaxMasterEntity, int> _employmentTaxRepo;

        public TaxMasterDataService(
            IRepository<TypeOfUseEntity, int> typeOfUseRepo,
            IRepository<PropertyCategoryEntity, int> propertyCategoryRepo,
            IRepository<SubTypeOfUseEntity, int> subTypeOfUseRepo,
            IRepository<FloorEntity, int> floorRepo,
            IRepository<SubFloorEntity, int> subFloorRepo,
            IRepository<ConstructionTypeEntity, int> constructionTypeRepo,
            IRepository<RateEntity, int> rateRepo,
            IRepository<RateSectionEntity, int> rateSectionRepo,
            IRepository<RateSectionDetailsEntity, int> rateSectionDetailsRepo,
            IRepository<DepreciationMasterEntity, int> depreciationRepo,
            IRepository<AssessmentYearRangeEntity, int> yearRangeRepo,
            IRepository<TaxMasterEntity, int> taxMasterRepo,
            IRepository<TaxPercentageMasterRVEntity, int> taxPercentageRepo,
            IRepository<EducationTaxMasterEntity, int> educationTaxRepo,
            IRepository<EmploymentTaxMasterEntity, int> employmentTaxRepo)
        {
            _typeOfUseRepo = typeOfUseRepo;
            _propertyCategoryRepo = propertyCategoryRepo;
            _subTypeOfUseRepo = subTypeOfUseRepo;
            _floorRepo = floorRepo;
            _subFloorRepo = subFloorRepo;
            _constructionTypeRepo = constructionTypeRepo;
            _rateRepo = rateRepo;
            _rateSectionRepo = rateSectionRepo;
            _rateSectionDetailsRepo = rateSectionDetailsRepo;
            _depreciationRepo = depreciationRepo;
            _yearRangeRepo = yearRangeRepo;
            _taxMasterRepo = taxMasterRepo;
            _taxPercentageRepo = taxPercentageRepo;
            _educationTaxRepo = educationTaxRepo;
            _employmentTaxRepo = employmentTaxRepo;
        }

        // ── helpers ──────────────────────────────────────────────────────────────

        private Task<List<T>> GetOrCacheAsync<T>(string key, System.Func<Task<List<T>>> factory)
            => factory();

        // ── ITaxMasterDataService ─────────────────────────────────────────────────

        public virtual Task<List<TypeOfUseEntity>> GetActiveTypeOfUsesAsync() =>
            GetOrCacheAsync("tmd:TypeOfUses",
                () => _typeOfUseRepo.GetQueryable()
                          .AsNoTracking()
                          .Include(x => x.TypeOfUseCategory)   // needed for TypeOfUseCategoryCode-based Plot/OpenPlot rule
                          .Where(x => x.IsActive)
                          .ToListAsync());

        public virtual Task<List<PropertyCategoryEntity>> GetActivePropertyCategoriesAsync() =>
            GetOrCacheAsync("tmd:PropertyCategories",
                () => _propertyCategoryRepo.GetQueryable().AsNoTracking().Where(x => x.IsActive).ToListAsync());

        public virtual Task<List<SubTypeOfUseEntity>> GetActiveSubTypeOfUsesAsync() =>
            GetOrCacheAsync("tmd:SubTypeOfUses",
                () => _subTypeOfUseRepo.GetQueryable().AsNoTracking().Where(x => x.IsActive).ToListAsync());

        public virtual Task<List<FloorEntity>> GetActiveFloorsAsync() =>
            GetOrCacheAsync("tmd:Floors",
                () => _floorRepo.GetQueryable().AsNoTracking().Where(x => x.IsActive).ToListAsync());

        public virtual Task<List<SubFloorEntity>> GetActiveSubFloorsAsync() =>
            GetOrCacheAsync("tmd:SubFloors",
                () => _subFloorRepo.GetQueryable().AsNoTracking().Where(x => x.IsActive).ToListAsync());

        public virtual Task<List<ConstructionTypeEntity>> GetActiveConstructionTypesAsync() =>
            GetOrCacheAsync("tmd:ConstructionTypes",
                () => _constructionTypeRepo.GetQueryable().AsNoTracking().Where(x => x.IsActive).ToListAsync());

        public virtual async Task<int> GetRateSectionIdForWardAsync(int? wardId)
        {
            if (!wardId.HasValue) return 0;

            var sectionDetail = await _rateSectionDetailsRepo.GetQueryable()
                .AsNoTracking()
                .Where(x => x.WardId == wardId.Value && x.IsActive)
                .FirstOrDefaultAsync();

            return sectionDetail?.RateSectionId ?? 0;
        }

        public virtual Task<List<RateEntity>> GetRatesForSectionAsync(int rateSectionId) =>
            GetOrCacheAsync($"tmd:Rates:{rateSectionId}",
                () => _rateRepo.GetQueryable().AsNoTracking()
                    .Where(x => x.RateSectionId == rateSectionId && x.IsActive)
                    .ToListAsync());

        public virtual Task<List<DepreciationMasterEntity>> GetActiveDepreciationsAsync() =>
            GetOrCacheAsync("tmd:Depreciations",
                () => _depreciationRepo.GetQueryable().AsNoTracking().Where(x => x.IsActive).ToListAsync());

        public virtual Task<List<AssessmentYearRangeEntity>> GetActiveYearRangesAsync() =>
            GetOrCacheAsync("tmd:YearRanges",
                () => _yearRangeRepo.GetQueryable().AsNoTracking().Where(x => x.IsActive).ToListAsync());

        public virtual Task<List<TaxMasterEntity>> GetActiveTaxesAsync() =>
            GetOrCacheAsync("tmd:Taxes",
                () => _taxMasterRepo.GetQueryable()
                          .AsNoTracking()
                          .Include(t => t.TaxCategoryMaster)   // needed for CategoryCode-based classification
                          .Where(x => x.IsActive)
                          .ToListAsync());

        public virtual Task<List<TaxPercentageMasterRVEntity>> GetActiveTaxPercentagesAsync() =>
            GetOrCacheAsync("tmd:TaxPercentages",
                () => _taxPercentageRepo.GetQueryable().AsNoTracking().Where(x => x.IsActive).ToListAsync());

        public virtual Task<List<EducationTaxMasterEntity>> GetActiveEducationTaxSlabsAsync() =>
            GetOrCacheAsync("tmd:EducationSlabs",
                () => _educationTaxRepo.GetQueryable().AsNoTracking().Where(x => x.IsActive).ToListAsync());

        public virtual Task<List<EmploymentTaxMasterEntity>> GetActiveEmploymentTaxSlabsAsync() =>
            GetOrCacheAsync("tmd:EmploymentSlabs",
                () => _employmentTaxRepo.GetQueryable().AsNoTracking().Where(x => x.IsActive).ToListAsync());
    }
}
