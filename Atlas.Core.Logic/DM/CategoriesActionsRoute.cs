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
    
    public partial class CategoriesActionsRoute
    {
        public long CategoriesActionsRouteId { get; set; }
        public long CategoryId { get; set; }
        public long ActionsRouteId { get; set; }
        public byte[] RowVersion { get; set; }
        public Nullable<int> BankId { get; set; }
        public Nullable<long> ChannelId { get; set; }
    
        public virtual ActionsRoute ActionsRoute { get; set; }
    }
}
