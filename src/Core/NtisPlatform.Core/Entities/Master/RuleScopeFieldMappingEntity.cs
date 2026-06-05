namespace NtisPlatform.Core.Entities.Master
{
    public class RuleScopeFieldMappingEntity : BaseEntity
    {
        public int? RuleScopeId { get; set; }
        public int? RulesFieldId { get; set; }
        public int? DisplayOrder { get; set; }
    }
}