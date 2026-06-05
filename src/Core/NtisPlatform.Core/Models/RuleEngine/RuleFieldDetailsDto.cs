namespace NtisPlatform.Core.Models.RuleEngine
{
    public class RuleFieldDetailsDto
    {
        public int RuleScopeId { get; set; }
        public int RulesFieldId { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string InputType { get; set; } = string.Empty;
        public bool HasApiSource { get; set; }
        public bool HasStaticValues { get; set; }
        public bool IsRequired { get; set; }
        public string ApiEndpoint { get; set; } = string.Empty;
        public string ApiMethod { get; set; } = string.Empty;
        public string ApiParameters { get; set; } = string.Empty;
        public string ApiResponseMapping { get; set; } = string.Empty;
        public string StaticValuesJson { get; set; } = string.Empty;
        public string DefaultValue { get; set; } = string.Empty;
        public string ValidationRegex { get; set; } = string.Empty;
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public int MinLength { get; set; }
        public int MaxLength { get; set; }
        public int? DisplayOrder { get; set; }
    }
}