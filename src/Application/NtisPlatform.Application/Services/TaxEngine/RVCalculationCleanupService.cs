using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NtisPlatform.Application.Interfaces.TaxEngine;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NtisPlatform.Application.Services.TaxEngine
{
    public class RVCalculationCleanupService : IRVCalculationCleanupService
    {
        private readonly IRepository<PropertyTaxCalculationRVResultsEntity, int> _taxResultsRepo;
        private readonly IRepository<PolicyTaxDetailsEntity, int> _policyTaxRepo;
        private readonly IRepository<TransMastRVEntity, int> _transmastRVRepo;
        private readonly ILogger<RVCalculationCleanupService> _logger;
        private readonly TimeProvider _timeProvider;

        public RVCalculationCleanupService(
            IRepository<PropertyTaxCalculationRVResultsEntity, int> taxResultsRepo,
            IRepository<PolicyTaxDetailsEntity, int> policyTaxRepo,
            IRepository<TransMastRVEntity, int> transmastRVRepo,
            ILogger<RVCalculationCleanupService> logger,
            TimeProvider timeProvider)
        {
            _taxResultsRepo = taxResultsRepo;
            _policyTaxRepo = policyTaxRepo;
            _transmastRVRepo = transmastRVRepo;
            _logger = logger;
            _timeProvider = timeProvider;
        }

        public async Task DeactivateExistingRVCalculationsAsync(
            int propertyId,
            int financeYear,
            int? yearMasterId)
        {
            var now = _timeProvider.GetLocalNow().DateTime;

            // Bulk UPDATE — replaces the previous N+1 loop of per-row UpdateAsync calls.
            int taxResultCount = await _taxResultsRepo.GetQueryable()
                .Where(x => x.PropertyId == propertyId && x.IsActive && !x.MarkedForDeletion)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.IsActive,              false)
                    .SetProperty(x => x.MarkedForDeletion,     true)
                    .SetProperty(x => x.MarkedForDeletionDate, now)
                    .SetProperty(x => x.UpdatedDate,           now));

            int policyCount = await _policyTaxRepo.GetQueryable()
                .Where(x => x.PropertyId == propertyId &&
                            x.PolicyYear == financeYear &&
                            x.PolicyCode == "NETTAX" &&
                            x.IsActive &&
                            !x.MarkedForDeletion)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.IsActive,              false)
                    .SetProperty(x => x.MarkedForDeletion,     true)
                    .SetProperty(x => x.MarkedForDeletionDate, now)
                    .SetProperty(x => x.UpdatedDate,           now));

            int transmastCount = 0;

            if (yearMasterId.HasValue)
            {
                transmastCount = await _transmastRVRepo.GetQueryable()
                    .Where(x => x.PropertyId == propertyId &&
                                x.FinanceYearId == yearMasterId.Value &&
                                x.IsActive &&
                                !x.MarkedForDeletion)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.IsActive,              false)
                        .SetProperty(x => x.MarkedForDeletion,     true)
                        .SetProperty(x => x.MarkedForDeletionDate, now)
                        .SetProperty(x => x.UpdatedDate,           now));
            }

            _logger.LogInformation(
                "Deactivated old RV calculation records for PropertyId={PropertyId}. " +
                "TaxResults={TaxResults}, PolicyRows={PolicyRows}, TransmastRows={TransmastRows}",
                propertyId, taxResultCount, policyCount, transmastCount);
        }
    }
}
