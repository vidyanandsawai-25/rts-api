using NtisPlatform.Core.Entities;
using NtisPlatform.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace NtisPlatform.Core.Entities.Master
{
    public class FlagMasterEntity : BaseEntity, IHardDeletable
    {
     
        public int PropertyId { get; set; }

        //public PropertyEntity PropertyMast { get; set; } = null!;
        public virtual PropertyEntity? PropertyMast { get; set; }

        public bool Lift { get; set; } //  USED IN CV
		
        /// <summary>
        /// Indicates whether the entity is marked for deletion
        /// </summary>

        public bool MarkedForDeletion { get; set; } = false;

        /// <summary>
        /// Date when marked for deletion
        /// </summary>
        public DateTime? MarkedForDeletionDate { get; set; }
        // Add other properties as per your schema

    }
}
