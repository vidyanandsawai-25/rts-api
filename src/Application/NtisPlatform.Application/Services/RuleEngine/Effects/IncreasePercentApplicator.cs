namespace NtisPlatform.Application.Services.RuleEngine.Effects
{
    /// <summary>Handles effectType "Increase %" or "IncreasePercent" — increases the base rate by the given percentage.</summary>
    public sealed class IncreasePercentApplicator : IRuleEffectApplicator
    {
        public bool CanHandle(string effectType) =>
            effectType.Contains("increase", StringComparison.OrdinalIgnoreCase) &&
            (effectType.Contains("%") || effectType.Contains("percent", StringComparison.OrdinalIgnoreCase));

        /// <summary>Result = baseRate × (1 + effectValue / 100). E.g. 1000 × (1 + 20/100) = 1200.</summary>
        public Task<decimal> Apply(decimal baseRate, decimal effectValue) =>
            Task.FromResult(baseRate * (1m + effectValue / 100m));
    }
}
