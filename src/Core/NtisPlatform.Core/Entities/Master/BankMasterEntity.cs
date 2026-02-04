namespace NtisPlatform.Core.Entities.Master
{
    public class BankMasterEntity : CommonBaseEntity
    {
        public int Id { get; set; }
        public string BankCode { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string IFSCCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
