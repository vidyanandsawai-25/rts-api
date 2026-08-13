namespace NtisPlatform.Application.Services.Rules.Effects
{
    /// <summary>Handles effectTypes like "Override", "Equal", "Equals", "Equal To", "Set", "Fixed", "=" — replaces/sets the base value equal to the effect value.</summary>
    public sealed class OverrideApplicator : IRuleEffectApplicator
    {
        private static readonly HashSet<string> SupportedAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            "override",
            "equal",
            "equals",
            "equal to",
            "equalto",
            "set",
            "fixed",
            "="
        };

        public bool CanHandle(string effectType)
        {
            if (string.IsNullOrWhiteSpace(effectType))
                return false;

            return SupportedAliases.Contains(effectType.Trim());
        }

        /// <summary>Result = effectValue (ignores baseRate entirely). E.g. fixed rate = 500.</summary>
        public Task<decimal> Apply(decimal baseRate, decimal effectValue) =>
            Task.FromResult(effectValue);

        public decimal GetApplyRate(decimal effectValue) => effectValue;
    }
}
