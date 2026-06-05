namespace NtisPlatform.Application.Services.RuleEngine.Effects
{
    /// <summary>Handles effectType "Override" — replaces the base rate with a fixed value.</summary>
    public sealed class OverrideApplicator : IRuleEffectApplicator
    {
        public bool CanHandle(string effectType) =>
            effectType.Contains("override", StringComparison.OrdinalIgnoreCase);

        /// <summary>Result = effectValue (ignores baseRate entirely). E.g. fixed rate = 500.</summary>
        public Task<decimal> Apply(decimal baseRate, decimal effectValue) =>
            Task.FromResult(effectValue);
    }
}
