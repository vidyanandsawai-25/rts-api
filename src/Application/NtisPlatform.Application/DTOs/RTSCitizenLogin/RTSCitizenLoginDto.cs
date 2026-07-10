namespace NtisPlatform.Application.DTOs.CitizenLoginDetails;
    public class PropertyDto : BaseDtos
    {
        public int OwnerID { get; set; }
        public string? MobileNo { get; set; }
        public string? UnicdeAddress { get; set; }
        public string? NewZoneNo { get; set; }
        public string? NewWardNo { get; set; }
        public string? NewPropertyNo { get; set; }
        public string? NewPartitionNo { get; set; }
        public string? OldPropertyNo { get; set; }
        public string? PropertyDescription { get; set; }
        public string? OwnerFirstName { get; set; }
        public string? OccupierName { get; set; }
        public string? MarathiSocietyName { get; set; }
        public string? MarathiOwnerPatta { get; set; }
        public string? MarathiOwnerDukanFlatNo { get; set; }
    }

    public class CreatePropertyDto
    {
        public string? MobileNo { get; set; }
        public string? UnicdeAddress { get; set; }
        public string? NewZoneNo { get; set; }
        public string? NewWardNo { get; set; }
        public string? NewPropertyNo { get; set; }
        public string? NewPartitionNo { get; set; }
        public string? OldPropertyNo { get; set; }
        public string? PropertyDescription { get; set; }
        public string? OwnerFirstName { get; set; }
        public string? OccupierName { get; set; }
        public string? MarathiSocietyName { get; set; }
        public string? MarathiOwnerPatta { get; set; }
        public string? MarathiOwnerDukanFlatNo { get; set; }
    }

    public class UpdatePropertyDto
    {
        public int OwnerID { get; set; }
        public string? MobileNo { get; set; }
        public string? UnicdeAddress { get; set; }
        public string? NewZoneNo { get; set; }
        public string? NewWardNo { get; set; }
        public string? NewPropertyNo { get; set; }
        public string? NewPartitionNo { get; set; }
        public string? OldPropertyNo { get; set; }
        public string? PropertyDescription { get; set; }
        public string? OwnerFirstName { get; set; }
        public string? OccupierName { get; set; }
        public string? MarathiSocietyName { get; set; }
        public string? MarathiOwnerPatta { get; set; }
        public string? MarathiOwnerDukanFlatNo { get; set; }
    }
