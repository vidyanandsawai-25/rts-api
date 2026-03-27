using System;
using System.Collections.Generic;
using System.Text;


namespace NtisPlatform.Core.Entities.Master;

public class PaymentModeEntity : BaseEntity
{
    public int PaymentModeId { get; set; }
    public string? Code { get; set; } 
    public string? PaymentModeName { get; set; }
    public string? Type { get; set; }
    public string? Category { get; set; } 
    public string? Description { get; set; } 
    public string? ChargeType { get; set; } 
    public int? TransactionCharge { get; set; }
}

