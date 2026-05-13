using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using NtisPlatform.Core.Entities;
namespace NtisPlatform.Core.Entities.Master
{
    [Table("FlagMaster", Schema = "PTIS")]
    public class FlagMasterEntity : BaseEntity
    {
     
        public int PropertyId { get; set; }
        public PropertyEntity PropertyMast { get; set; } = null!;
        public bool Lift { get; set; } //  USED IN CV

        // Add other properties as per your schema

    }
}
