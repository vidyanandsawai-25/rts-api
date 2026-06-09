using System.Threading;
using System.Threading.Tasks;
using NtisPlatform.Application.DTOs.Rules.RuleExecution;

namespace NtisPlatform.Application.Interfaces.Rules
{
    /// <summary>
    /// Service contract for querying, validating, and building the PropertyCalculationContext.
    /// Centrally manages property aggregates loading, year ranges resolving, and lift checks.
    /// </summary>
    public interface IPropertyContextLoaderService
    {
        /// <summary>
        /// Queries the database and constructs a complete, validated PropertyCalculationContext.
        /// Throws exceptions for missing property, invalid construction year, or unresolved year range.
        /// </summary>
        /// <param name="propertyId">Target property ID</param>
        /// <param name="financeYear">Target finance calculation year</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Fully loaded PropertyCalculationContext</returns>
        Task<PropertyCalculationContext> LoadPropertyContextAsync(
            int propertyId,
            int financeYear,
            CancellationToken cancellationToken = default);
    }
}
