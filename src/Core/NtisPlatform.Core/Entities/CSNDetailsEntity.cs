using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NtisPlatform.Core.Entities
{
    public class CSNDetailsEntity 
    {
        [Key]
        public int Id { get; set; }
        public int RateCVMasterId { get; set; }
        public int MoujaId { get; set; }
        public string? CSN { get; set; }
        //public int YearRangeCVId { get; set; }

    }
}
