using System;
using System.Collections.Generic;
using System.Text;

namespace NtisPlatform.Core.Entities
{
    public class TypeOfUseGroupCVEntity : BaseEntity
    {
        public string TypeOfUseGroupCVCode { get; set; } = string.Empty;

        public string GroupName { get; set; } = string.Empty;

        public string GroupIcon { get; set; } = string.Empty;

        public bool IsFloorWiseRateApplicable { get; set; }

        public ICollection<TypeOfUseEntity> TypeOfUse { get; set; } = new List<TypeOfUseEntity>();

    }

}
