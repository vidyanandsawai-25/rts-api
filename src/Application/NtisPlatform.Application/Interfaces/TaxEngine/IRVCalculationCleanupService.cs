using System.Threading.Tasks;

namespace NtisPlatform.Application.Interfaces.TaxEngine
{
    public interface IRVCalculationCleanupService
    {
        Task DeactivateExistingRVCalculationsAsync(
            int propertyId,
            int financeYear,
            int? yearMasterId);
    }
}