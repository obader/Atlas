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
    
    public partial class Category
    {
        public long CategoryId { get; set; }
        public string Code { get; set; }
        public bool Enable { get; set; }
        public bool HasTransaction { get; set; }
        public string Description { get; set; }
        public byte[] RowVersion { get; set; }
        public long TicketTypeId { get; set; }
    
        public virtual TicketType TicketType { get; set; }
    }
}
