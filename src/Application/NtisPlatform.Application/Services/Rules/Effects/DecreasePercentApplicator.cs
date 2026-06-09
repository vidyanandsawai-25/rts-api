namespace NtisPlatform.Application.Services.Rules.Effects
{
    /// <summary>Handles effectType "Decrease %" or "DecreasePercent" — reduces the base rate by the given percentage.</summary>
    public sealed class DecreasePercentApplicator : IRuleEffectApplicator
    {
        public bool CanHandle(string effectType) =>
            effectType.Contains("decrease", StringComparison.OrdinalIgnoreCase) &&
            (effectType.Contains("%") || effectType.Contains("percent", StringComparison.OrdinalIgnoreCase));

        /// <summary>Result = baseRate × (1 - effectValue / 100). E.g. 1000 × (1 - 40/100) = 600.</summary>
        public Task<decimal> Apply(decimal baseRate, decimal effectValue) =>
            Task.FromResult(baseRate * (1m - effectValue / 100m));
    }
}
