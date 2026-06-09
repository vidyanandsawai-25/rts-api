namespace NtisPlatform.Application.Services.Rules.Effects
{
    /// <summary>Handles effectType "Exemption" — zeroes out the base rate (fully exempt).</summary>
    public sealed class ExemptionApplicator : IRuleEffectApplicator
    {
        public bool CanHandle(string effectType) =>
            effectType.Contains("exempt", StringComparison.OrdinalIgnoreCase);

        /// <summary>Result = 0 (fully exempt, regardless of baseRate or effectValue).</summary>
        public Task<decimal> Apply(decimal baseRate, decimal effectValue) => Task.FromResult(0m);
    }
}
