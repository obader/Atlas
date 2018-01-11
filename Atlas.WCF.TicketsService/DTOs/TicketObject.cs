using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Atlas.WCF.TicketsService.DTOs
{
    public class TicketObject
    {
        public string UserId { get; private set; }
        public long ApplicationId { get; set; }
        public string ApplicationName { get; set; }
        public long CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int BankId { get; set; }
        public long? ProfileId { get; set; }
        public long PriorityId { get; set; }
        public string PriorityName { get; set; }
        public long? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string Description { get; set; }
        public string Title { get; private set; }
        public DateTime CreationDate { get; private set; }
        public DateTime ModifiedDate { get; private set; }
    }
}