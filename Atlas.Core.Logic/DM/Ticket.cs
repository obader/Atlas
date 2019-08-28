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
    
    public partial class Ticket
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Ticket()
        {
            this.Comments = new HashSet<Comment>();
            this.TicketAudits = new HashSet<TicketAudit>();
            this.TicketExternalReferences = new HashSet<TicketExternalReference>();
            this.TicketTransactions = new HashSet<TicketTransaction>();
            this.TicketIssues = new HashSet<TicketIssue>();
        }
    
        public long TicketId { get; set; }
        public Nullable<long> TicketParentId { get; set; }
        public Nullable<long> ApplicationId { get; set; }
        public Nullable<long> CategoryId { get; set; }
        public Nullable<long> PriorityId { get; set; }
        public Nullable<long> AssignedToDepartmentId { get; set; }
        public Nullable<long> ReasonsId { get; set; }
        public long TicketStatusId { get; set; }
        public string Title { get; set; }
        public Nullable<int> BankId { get; set; }
        public Nullable<long> CustomerId { get; set; }
        public Nullable<long> ProfileId { get; set; }
        public string Description { get; set; }
        public string CreatedBy { get; set; }
        public System.DateTime CreationDate { get; set; }
        public string ModifiedBy { get; set; }
        public Nullable<System.DateTime> ModifedDate { get; set; }
        public Nullable<System.DateTime> LastStatusChanged { get; set; }
        public byte[] RowVersion { get; set; }
        public Nullable<long> ChannelId { get; set; }
        public Nullable<bool> HasIssue { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Comment> Comments { get; set; }
        public virtual Department Department { get; set; }
        public virtual Priority Priority { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<TicketAudit> TicketAudits { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<TicketExternalReference> TicketExternalReferences { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<TicketTransaction> TicketTransactions { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<TicketIssue> TicketIssues { get; set; }
    }
}
