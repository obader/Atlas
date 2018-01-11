using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Atlas.Core.Logic.DTOs
{
    public class TicketObject
    {
        public string UserId { get;  set; }
        public long ApplicationId { get; set; }
        public string ApplicationName { get; set; }
        public long CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int BankId { get; set; }
        public long? ProfileId { get; set; }
        public long PriorityId { get; set; }
        
        public long? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string Description { get; set; }
        public string Title { get;  set; }
        public DateTime CreationDate { get;  set; }
        public DateTime? ModifiedDate { get;  set; }
    }
}