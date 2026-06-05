namespace NtisPlatform.Core.Entities.Master
{
    public class RuleEffectTypeEntity : BaseEntity
    {
        public string EffectType { get; set; } = string.Empty;

        // Navigation property - one-to-one relationship with EffectTypeConfiguration
        public virtual EffectTypeConfigurationEntity? EffectTypeConfiguration { get; set; }
    }
}