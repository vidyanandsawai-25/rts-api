namespace NtisPlatform.Application.DTOs.DualMethod
{
    /// <summary>
    /// DTO for tax data used in dual method calculations
    /// </summary>
    public class TaxDataDto
    {
        public int TaxId { get; set; }
        public string TaxName { get; set; } = string.Empty;
        public decimal TaxAmount { get; set; }
    }
}
