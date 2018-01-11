using PinPayObjects.BaseObjects.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Atlas.WCF.TicketsService.Requests
{
    public class GetTicketsRequest:BaseRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public long? CategoryId { get; set; }
        public long? PriorityId { get; set; }
        public long? TicketStatusId { get; set; }
        public long? ApplicationId { get; set; }
        public long? BankId { get; set; }
    }
}