using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Application.Services.TaxEngine
{
    /// <summary>
    /// Service for retrieving master data required for tax calculations
    /// </summary>
    public class TaxMasterDataService
    {
        private readonly IRepository<TypeOfUseEntity, int> _typeOfUseRepo;
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

        public virtual Task<List<TypeOfUseEntity>> GetActiveTypeOfUsesAsync() =>
            _typeOfUseRepo.GetQueryable().Where(x => x.IsActive).ToListAsync();

        public virtual Task<List<SubTypeOfUseEntity>> GetActiveSubTypeOfUsesAsync() =>
            _subTypeOfUseRepo.GetQueryable().Where(x => x.IsActive).ToListAsync();

        public virtual Task<List<FloorEntity>> GetActiveFloorsAsync() =>
            _floorRepo.GetQueryable().Where(x => x.IsActive).ToListAsync();

        public virtual Task<List<SubFloorEntity>> GetActiveSubFloorsAsync() =>
            _subFloorRepo.GetQueryable().Where(x => x.IsActive).ToListAsync();

        public virtual Task<List<ConstructionTypeEntity>> GetActiveConstructionTypesAsync() =>
            _constructionTypeRepo.GetQueryable().Where(x => x.IsActive).ToListAsync();

        public virtual async Task<int> GetRateSectionIdForWardAsync(int? wardId)
        {
            if (!wardId.HasValue)
                return 0;

            var sectionDetail = await _rateSectionDetailsRepo.GetQueryable()
                .Where(x => x.WardId == wardId.Value && x.IsActive)
                .FirstOrDefaultAsync();

            return sectionDetail?.RateSectionId ?? 0;
        }

        public virtual Task<List<RateEntity>> GetRatesForSectionAsync(int rateSectionId) =>
            _rateRepo.GetQueryable()
                .Where(x => x.RateSectionId == rateSectionId && x.IsActive)
                .ToListAsync();

        public virtual Task<List<DepreciationMasterEntity>> GetActiveDepreciationsAsync() =>
            _depreciationRepo.GetQueryable().Where(x => x.IsActive).ToListAsync();

        public virtual Task<List<AssessmentYearRangeEntity>> GetActiveYearRangesAsync() =>
            _yearRangeRepo.GetQueryable().Where(x => x.IsActive).ToListAsync();

        public virtual Task<List<TaxMasterEntity>> GetActiveTaxesAsync() =>
            _taxMasterRepo.GetQueryable().Where(x => x.IsActive).ToListAsync();

        public virtual Task<List<TaxPercentageMasterRVEntity>> GetActiveTaxPercentagesAsync() =>
            _taxPercentageRepo.GetQueryable().Where(x => x.IsActive).ToListAsync();

        public virtual Task<List<EducationTaxMasterEntity>> GetActiveEducationTaxSlabsAsync() =>
            _educationTaxRepo.GetQueryable().Where(x => x.IsActive).ToListAsync();

        public virtual Task<List<EmploymentTaxMasterEntity>> GetActiveEmploymentTaxSlabsAsync() =>
            _employmentTaxRepo.GetQueryable().Where(x => x.IsActive).ToListAsync();
    }
}
