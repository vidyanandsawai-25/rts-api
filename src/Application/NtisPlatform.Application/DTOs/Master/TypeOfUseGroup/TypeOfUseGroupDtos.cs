using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NtisPlatform.Application.DTOs
{
    public class TypeOfUseGroupDto : BaseDtos
    {
        public string TypeOfUseGroupID { get; set; } = "";
        public string GroupName { get; set; } = "";
        public string? GroupNameEnglish { get; set; }
        public string? GroupIcon { get; set; }
    }

    public class CreateTypeOfUseGroupDto : CreateBaseDtos
    {
        [Required(ErrorMessage = "TypeOfUseGroup_TypeOfUseGroupID_Required")]
        [StringLength(10, ErrorMessage = "TypeOfUseGroupID_MaxLen_10")]
        public string TypeOfUseGroupID { get; set; } = "";

        [Required(ErrorMessage = "TypeOfUseGroup_GroupName_Required")]
        [StringLength(50, ErrorMessage = "GroupName_MaxLen_50")]
        public string GroupName { get; set; } = "";

        [StringLength(50, ErrorMessage = "GroupNameEnglish_MaxLen_50")]
        public string? GroupNameEnglish { get; set; }

        [StringLength(20, ErrorMessage = "GroupIcon_MaxLen_20")]
        public string? GroupIcon { get; set; }

    }

    public class UpdateTypeOfUseGroupDto : UpdateBaseDtos
    {
        [Required(ErrorMessage = "TypeOfUseGroup_GroupName_Required")]
        [StringLength(50, ErrorMessage = "GroupName_MaxLen_50")]
        public string GroupName { get; set; } = "";

        [StringLength(50, ErrorMessage = "GroupNameEnglish_MaxLen_50")]
        public string? GroupNameEnglish { get; set; }

        [StringLength(20, ErrorMessage = "GroupIcon_MaxLen_20")]
        public string? GroupIcon { get; set; }
    }
}
