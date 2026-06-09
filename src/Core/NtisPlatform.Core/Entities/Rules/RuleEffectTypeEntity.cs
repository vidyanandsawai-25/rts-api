namespace NtisPlatform.Core.Entities.Rules
{
    public class RuleEffectTypeEntity : BaseEntity
    {
        public string EffectType { get; set; } = string.Empty;

        // Navigation property - one-to-one relationship with EffectTypeConfiguration
        public virtual NtisPlatform.Core.Entities.Master.EffectTypeConfigurationEntity? EffectTypeConfiguration { get; set; }
    }
}
