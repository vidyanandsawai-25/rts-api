namespace NtisPlatform.Application.Services.RuleEngine.Effects
{
    /// <summary>Handles effectType "Multiply" — multiplies the base rate by a factor.</summary>
    public sealed class MultiplyApplicator : IRuleEffectApplicator
    {
        public bool CanHandle(string effectType) =>
            effectType.Contains("multiply", StringComparison.OrdinalIgnoreCase);

        /// <summary>Result = baseRate × effectValue. E.g. 1000 × 2.5 = 2500.</summary>
        public Task<decimal> Apply(decimal baseRate, decimal effectValue) =>
            Task.FromResult(baseRate * effectValue);
    }
}
