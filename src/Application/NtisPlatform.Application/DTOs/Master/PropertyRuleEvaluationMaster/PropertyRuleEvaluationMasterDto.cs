using System.ComponentModel.DataAnnotations;

namespace NtisPlatform.Application.DTOs.Master.PropertyRuleEvaluationMaster
{
    public class PropertyRuleEvaluationMasterDto : BaseDtos
    {
        public int Id { get; set; }
        public string ParameterCode { get; set; } = string.Empty;
        public string ParameterName { get; set; } = string.Empty;
    }

    public class CreatePropertyRuleEvaluationMasterDto : CreateBaseDtos
    {
        [Required(ErrorMessage = "ParameterCode_Required")]
        [StringLength(100, ErrorMessage = "ParameterCode_MaxLen_100")]
        public string ParameterCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "ParameterName_Required")]
        [StringLength(200, ErrorMessage = "ParameterName_MaxLen_200")]
        public string ParameterName { get; set; } = string.Empty;
    }

    public class UpdatePropertyRuleEvaluationMasterDto : UpdateBaseDtos
    {
        [Required(ErrorMessage = "ParameterCode_Required")]
        [StringLength(100, ErrorMessage = "ParameterCode_MaxLen_100")]
        public string ParameterCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "ParameterName_Required")]
        [StringLength(200, ErrorMessage = "ParameterName_MaxLen_200")]
        public string ParameterName { get; set; } = string.Empty;
    }
}
