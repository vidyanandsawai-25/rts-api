namespace NtisPlatform.Core.Entities.Master
{
    public class RulesFieldEntity : BaseEntity
    {
        public string FieldName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;

        // Navigation property
        public virtual FieldConfigurationEntity? FieldConfiguration { get; set; }
    }
}