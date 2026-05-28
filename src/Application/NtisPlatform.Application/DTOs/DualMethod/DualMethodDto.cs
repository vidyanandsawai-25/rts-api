using NtisPlatform.Application.DTOs.CapitalValue;
using System;
using System.Collections.Generic;
using System.Text;

namespace NtisPlatform.Application.DTOs.DualMethod
{
    public class DualMethodDto
    {
        public Dictionary<string, decimal> OldTaxes { get; set; } = new();
        public Dictionary<string, decimal> RVTaxes { get; set; } = new();
        public Dictionary<string, decimal> CVTaxes { get; set; } = new();

    }
    public class TaxSumDto
    {
        public int TaxId { get; set; }
        public decimal Amount { get; set; }
    }
}
