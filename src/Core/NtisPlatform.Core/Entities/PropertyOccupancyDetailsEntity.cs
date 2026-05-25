using System;
using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities
{
    public class PropertyOccupancyDetailsEntity : BaseEntity, IHardDeletable
    {
        public int PropertyDetailId { get; set; }
        public DateTime? OccupancyDate { get; set; }
        public string? OccupancyNumber { get; set; }
        public string? IssuedBy { get; set; }
        public string? Remarks { get; set; }
        public bool MarkedForDeletion { get; set; }
        public DateTime? MarkedForDeletionDate { get; set; }
        public virtual PropertyDetailsEntity? PropertyDetails { get; set; }
    }
}
