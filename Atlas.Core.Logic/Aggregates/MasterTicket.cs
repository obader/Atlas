using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atlas.Core.Logic.DM;

namespace Atlas.Core.Logic.Aggregates
{
    public class MasterTicket
    {
        public Entities.Ticket Ticket { get; private set; }
        public List<Entities.TicketStatus> Statuses { get; private set; }

        public List<Entities.TicketExternalReferences> ExternalReferences { get; private set; }
        public List<Entities.Comment> Comments { get; private set; }

        public List<Entities.TicketTransaction> Transactions { get; set; }
        public DateTime LastModifieDate { get; private set; }
        public string ModifiedByUserId { get; private set; }
        public MasterTicket ParentTicket { get; set; }
        public bool HasIssue { get; set; }
        public string IssueDescription { get; set; }

        public MasterTicket(string pUserId, Entities.Ticket pTicket, List<Entities.TicketStatus> pStatuses,List<Entities.Comment> pComments, List<Entities.TicketTransaction> pTransactions, List<Entities.TicketExternalReferences> pExternalReferences,
            MasterTicket pParentTicket)
        {
            Ticket = pTicket;
            Statuses = pStatuses;
            ModifiedByUserId = pUserId;
            Comments = pComments;
            Transactions = pTransactions;
            ExternalReferences = pExternalReferences;
            ParentTicket = pParentTicket;
        }

        public ValueObjects.TicketStatusModel GetCurrentStatus()
        {
            return Statuses?.OrderByDescending(p => p.Date).FirstOrDefault()?.Status;
        }
        public void ChangeStatus(ValueObjects.TicketStatusModel pStatus)
        {
            var newStatus = new Entities.TicketStatus(pStatus,ModifiedByUserId, null, Ticket.Id);
            Statuses.Add(newStatus);
            AddStatus(newStatus);
            LastModifieDate = DateTime.UtcNow;
        }

        public void UpdateDescription(string pDescription)
        {
            Ticket.Description = pDescription;
            LastModifieDate = DateTime.UtcNow;
            UpdateDesc(pDescription);
        }

        public void UpdateCategory(int pCategoryId)
        {
            Ticket.CategoryId = pCategoryId;
            LastModifieDate = DateTime.UtcNow;
            UpdateCat(pCategoryId);
        }

        public void UpdateProfileId(long pProfileId)
        {
            Ticket.ProfileId = pProfileId;
            LastModifieDate = DateTime.UtcNow;
            UpdateProfile(pProfileId);
        }

        public void UpdateCustomerId(long pCustomerId)
        {
            Ticket.CustomerId = pCustomerId;
            LastModifieDate = DateTime.UtcNow;
            UpdateCustomer(pCustomerId);
        }
        private void UpdateDesc(string pDescription)
        {
            using (var ctx = new DM.TicketingEntities())
            {
                var ticket = ctx.Tickets.FirstOrDefault(p => p.TicketId == Ticket.Id);
                if(ticket == null)
                    return;
                ticket.Description = pDescription;
                ticket.ModifedDate = DateTime.UtcNow;
                ticket.ModifiedBy = ModifiedByUserId;
                ctx.TicketAudits.Add(new DM.TicketAudit
                {

                    Comment = "New Description: "+pDescription,
                    ChangeDate = DateTime.UtcNow,
                    TicketId = Ticket.Id,
                    UserId = ModifiedByUserId
                });
                ctx.SaveChanges();
            }
        }
        private void UpdateCat(int pCategoryId)
        {
            using (var ctx = new DM.TicketingEntities())
            {
                var ticket = ctx.Tickets.FirstOrDefault(p => p.TicketId == Ticket.Id);
                if (ticket == null)
                    return;
                ticket.CategoryId = pCategoryId;
                ticket.ModifedDate = DateTime.UtcNow;
                ticket.ModifiedBy = ModifiedByUserId;
                ctx.TicketAudits.Add(new DM.TicketAudit
                {

                    Comment = "New Category: " + pCategoryId,
                    ChangeDate = DateTime.UtcNow,
                    TicketId = Ticket.Id,
                    UserId = ModifiedByUserId
                });
                ctx.SaveChanges();
            }
        }

        private void UpdateProfile(long pProfileId)
        {
            using (var ctx = new DM.TicketingEntities())
            {
                var ticket = ctx.Tickets.FirstOrDefault(p => p.TicketId == Ticket.Id);
                if (ticket == null)
                    return;
                ticket.ProfileId = pProfileId;
                ticket.ModifedDate = DateTime.UtcNow;
                ticket.ModifiedBy = ModifiedByUserId;
                ctx.TicketAudits.Add(new DM.TicketAudit
                {

                    Comment = "New Profile: " + pProfileId,
                    ChangeDate = DateTime.UtcNow,
                    TicketId = Ticket.Id,
                    UserId = ModifiedByUserId
                });
                ctx.SaveChanges();
            }
        }

        private void UpdateCustomer(long pCustomerId)
        {
            using (var ctx = new DM.TicketingEntities())
            {
                var ticket = ctx.Tickets.FirstOrDefault(p => p.TicketId == Ticket.Id);
                if (ticket == null)
                    return;
                ticket.CustomerId = pCustomerId;
                ticket.ModifedDate = DateTime.UtcNow;
                ticket.ModifiedBy = ModifiedByUserId;
                ctx.TicketAudits.Add(new DM.TicketAudit
                {

                    Comment = "New Customer: " + pCustomerId,
                    ChangeDate = DateTime.UtcNow,
                    TicketId = Ticket.Id,
                    UserId = ModifiedByUserId
                });
                ctx.SaveChanges();
            }
        }

        public void AddComments(string pComment)
        {
            var lComment = new Entities.Comment(Ticket.Id, -1, ModifiedByUserId, pComment, null);
            Comments.Add(lComment);
            AddComment(lComment);
            LastModifieDate = DateTime.UtcNow;
        }

        private void AddStatus(Entities.TicketStatus pNewStatus)
        {
            using (var ctx = new DM.TicketingEntities())
            {
                var ticket = ctx.Tickets.FirstOrDefault(p => p.TicketId == pNewStatus.TicketId);
                if (ticket == null)
                    return;
                ticket.TicketStatusId = pNewStatus.Status.StatusId;
                ticket.LastStatusChanged = pNewStatus.Date;

                ctx.TicketAudits.Add(new DM.TicketAudit
                {
                    TicketStatusId = pNewStatus.Status.StatusId,
                    ChangeDate = pNewStatus.Date,
                    TicketId = Ticket.Id,
                    UserId = pNewStatus.UserId
                });
                ctx.SaveChanges();
            }
        }

        private void AddComment(Entities.Comment pNewComment)
        {
            using (var ctx = new DM.TicketingEntities())
            {
                ctx.Comments.Add(new Comment
                {
                    TicketId = Ticket.Id,
                    CommentValue = pNewComment.CommentEntry,
                    RecordDate = pNewComment.Date,
                    UserId = pNewComment.UserId
                });

                ctx.TicketAudits.Add(new DM.TicketAudit
                {
                    Comment = pNewComment.CommentEntry,
                    ChangeDate = pNewComment.Date,
                    TicketId = Ticket.Id,
                    UserId = pNewComment.UserId
                });


                ctx.SaveChanges();
            }
        }
    }
}
