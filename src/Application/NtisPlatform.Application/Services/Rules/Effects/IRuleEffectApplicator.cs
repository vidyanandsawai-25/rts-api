namespace NtisPlatform.Application.Services.Rules.Effects
{
    /// <summary>
    /// Strategy interface for applying a rule effect to a base value.
    /// Each implementation handles one effectType (e.g. "Decrease %", "Multiply").
    /// </summary>
    public interface IRuleEffectApplicator
    {
        /// <summary>Returns true if this applicator handles the given effectType string.</summary>
        bool CanHandle(string effectType);

        /// <summary>
        /// Applies the effect and returns the adjusted rate.
        /// </summary>
        /// <param name="baseRate">The original rate value (e.g. 1000.0).</param>
        /// <param name="effectValue">The magnitude from Context.value (e.g. 40 for 40%).</param>
        Task<decimal> Apply(decimal baseRate, decimal effectValue);
    }
}
