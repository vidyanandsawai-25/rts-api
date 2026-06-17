using System.Threading;
using System.Threading.Tasks;
using NtisPlatform.Application.DTOs.Rules.RuleExecution;
using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Entities.Master;

namespace NtisPlatform.Application.Interfaces.Rules
{
    /// <summary>
    /// Service for applying rule-based adjustments to base tax rates or calculated values.
    /// Encapsulates context building, execution via rules engine, and applicator handling.
    /// </summary>
    public interface IRuleApplierService
    {
        /// <summary>
        /// Builds input context and executes rules of the given category, sequentially applying matching effects to a base rate or value.
        /// </summary>
        /// <param name="context">The rule applier context containing all entities and computation parameters.</param>
        /// <param name="maxRetries">Maximum rule execution retries for transient faults</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The adjusted rate or value after applying rule effects</returns>
        Task<RuleApplicationResult> ApplyRulesAsync(
            RuleApplierContext context,
            int maxRetries = 3,
            CancellationToken cancellationToken = default);
    }
}
