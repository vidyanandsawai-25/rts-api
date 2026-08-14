namespace NtisPlatform.Core.Entities;

public class MergeDetailEntity : BaseEntity
{
    public int PropertyMapDetailId { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerNameEnglish { get; set; }
    public string? OccupierName { get; set; }
    public string? OccupierNameEnglish { get; set; }
    public string? MobileNo { get; set; }
    public string? Address { get; set; }
    public string? AddressEnglish { get; set; }
    public string? BuilderName { get; set; }
    public string? BuilderNameEnglish { get; set; }
    public string? FlatOrShopNo { get; set; }
    public string? FlatOrShopNoEnglish { get; set; }
    public string? FlatOrShopName { get; set; }
    public string? FlatOrShopNameEnglish { get; set; }
    public virtual PropertyMapDetailEntity PropertyMapDetail { get; set; }
}
