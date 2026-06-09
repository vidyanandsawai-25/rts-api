namespace NtisPlatform.Core.Entities.Rules
{
    public class RulesFieldEntity : BaseEntity
    {
        public string FieldName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public string? DatabaseColumnName { get; set; }

        // Navigation property
        public virtual NtisPlatform.Core.Entities.Master.FieldConfigurationEntity? FieldConfiguration { get; set; }
    }
}
