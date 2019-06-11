﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class TicketingEntities : DbContext
    {
        public TicketingEntities()
            : base("name=TicketingEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<Application> Applications { get; set; }
        public virtual DbSet<Comment> Comments { get; set; }
        public virtual DbSet<TicketAudit> TicketAudits { get; set; }
        public virtual DbSet<TicketStatus> TicketStatuses { get; set; }
        public virtual DbSet<Department> Departments { get; set; }
        public virtual DbSet<Priority> Priorities { get; set; }
        public virtual DbSet<Reason> Reasons { get; set; }
        public virtual DbSet<Category> Categories { get; set; }
        public virtual DbSet<ActionsRoute> ActionsRoutes { get; set; }
        public virtual DbSet<CategoriesActionsRoute> CategoriesActionsRoutes { get; set; }
        public virtual DbSet<TicketAction> TicketActions { get; set; }
        public virtual DbSet<TicketCategoriesActionsRoute> TicketCategoriesActionsRoutes { get; set; }
        public virtual DbSet<TicketType> TicketTypes { get; set; }
        public virtual DbSet<ActionsNotification> ActionsNotifications { get; set; }
        public virtual DbSet<CategoriesActionsNotification> CategoriesActionsNotifications { get; set; }
        public virtual DbSet<TicketCategoriesActionsNotification> TicketCategoriesActionsNotifications { get; set; }
        public virtual DbSet<TicketCategoriesAction> TicketCategoriesActions { get; set; }
        public virtual DbSet<TicketExternalReference> TicketExternalReferences { get; set; }
        public virtual DbSet<TicketTransaction> TicketTransactions { get; set; }
        public virtual DbSet<Ticket> Tickets { get; set; }
    }
}
