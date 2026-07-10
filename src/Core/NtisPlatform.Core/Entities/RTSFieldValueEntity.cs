using NtisPlatform.Core.Interfaces;

namespace NtisPlatform.Core.Entities;

    public class RTSFieldValueEntity:BaseEntity, IHardDeletable
    {
        public int ApplicationId { get; set; }
        public int FieldDefinitionId { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string? TextValue { get; set; }
        public decimal? NumberValue { get; set; }
        public DateTime? DateValue { get; set; }
        public bool? BooleanValue { get; set; }
        public Guid? DocumentGuid { get; set; }
        public bool MarkedForDeletion { get; set; }
        public DateTime? MarkedForDeletionDate { get; set; }
        public virtual RTSApplicationDetailsEntity? Application { get; set; } 
    }

