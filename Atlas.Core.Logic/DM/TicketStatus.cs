//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Atlas.Core.Logic.DM
{
    using System;
    using System.Collections.Generic;
    
    public partial class TicketStatus
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public TicketStatus()
        {
            this.TicketAudits = new HashSet<TicketAudit>();
        }
    
        public long TicketStatusId { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public byte[] RowVersion { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<TicketAudit> TicketAudits { get; set; }
    }
}
