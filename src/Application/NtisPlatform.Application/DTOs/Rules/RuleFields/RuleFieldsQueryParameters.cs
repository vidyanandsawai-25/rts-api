using NtisPlatform.Application.DTOs.Queries;

namespace NtisPlatform.Application.DTOs.Rules.RuleFields
{
    public class RuleFieldsQueryParameters : BaseQueryParameters
    {
        public string? FieldName { get; set; }
        public string? FieldType { get; set; }
        public string? DataType { get; set; }
    }
}
