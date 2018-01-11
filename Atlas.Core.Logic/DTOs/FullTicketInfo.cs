using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Atlas.Core.Logic.DTOs
{
    public class FullTicketInfo
    {
        public FullTicketInfo()
        {
            Comments = new List<CommentObject>();
            Transactions = new List<TransactionObject>();
            Audits = new List<AuditObject>();
        }
        public TicketObject Ticket { get; set; }
        public List<CommentObject> Comments { get; set; }
        public List<TransactionObject> Transactions { get; set; }
        public List<AuditObject> Audits { get; set; }
    }
}