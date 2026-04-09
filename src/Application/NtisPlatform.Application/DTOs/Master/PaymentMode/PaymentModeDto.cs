using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static System.Net.Mime.MediaTypeNames;


namespace NtisPlatform.Application.DTOs.Master.PaymentMode;

public class PaymentModeDto : BaseDtos
{
    public int Id { get; set; }

    [Required(ErrorMessage = "PaymentMode_Code_Required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "PaymentMode_Code_Length")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "PaymentMode_Name_Required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "PaymentMode_Name_Length")]
    public string PaymentModeName { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "PaymentMode_Type_MaxLength")]
    public string? Type { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "PaymentMode_Category_MaxLength")]
    public string Category { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "PaymentMode_Description_MaxLength")]
    public string Description { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "PaymentMode_ChargeType_MaxLength")]
    public string ChargeType { get; set; } = string.Empty;

    public int? TransactionCharge { get; set; }
}

public class CreatePaymentModeDto : CreateBaseDtos
{
    [Required(ErrorMessage = "PaymentMode_Code_Required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "PaymentMode_Code_Length")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "PaymentMode_Name_Required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "PaymentMode_Name_Length")]
    public string PaymentModeName { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "PaymentMode_Category_MaxLength")]
    public string Category { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "PaymentMode_Type_MaxLength")]
    public string? Type { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "PaymentMode_Description_MaxLength")]
    public string Description { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "PaymentMode_ChargeType_MaxLength")]
    public string ChargeType { get; set; } = string.Empty;

    public int? TransactionCharge { get; set; }
}

public class UpdatePaymentModeDto : UpdateBaseDtos
{
    [Required(ErrorMessage = "PaymentMode_Code_Required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "PaymentMode_Code_Length")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "PaymentMode_Name_Required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "PaymentMode_Name_Length")]
    public string PaymentModeName { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "PaymentMode_Category_MaxLength")]
    public string Category { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "PaymentMode_Type_MaxLength")]
    public string? Type { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "PaymentMode_Description_MaxLength")]
    public string Description { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "PaymentMode_ChargeType_MaxLength")]
    public string ChargeType { get; set; } = string.Empty;

    public int? TransactionCharge { get; set; }
}
