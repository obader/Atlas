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
    
    public partial class TicketIssue
    {
        public int Id { get; set; }
        public long TicketId { get; set; }
        public string IssueDescription { get; set; }
        public byte[] RowVersion { get; set; }
    
        public virtual Ticket Ticket { get; set; }
    }
}
