using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Atlas.WCF.TicketsService.DTOs
{
    public class AuditObject
    {
        public long TicketAuditId { get; set; }
        public string UserId { get; set; }
        public long TicketId { get; set; }
        public long StatusId { get; set; }
        public string StatusName { get; set; } 
        public System.DateTime ChangeDate { get; set; }
        public string Comment { get; set; }
        public long PriorityId { get; set; }
        public string PriorityName { get; set; }
        public long? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
    }
}