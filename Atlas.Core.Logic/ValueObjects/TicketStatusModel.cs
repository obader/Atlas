using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.Core.Logic.ValueObjects
{
    public class TicketStatusModel:ValueObject<TicketStatusModel>
    {
        public static TicketStatusModel CreatedTicketStatus      = new TicketStatusModel(1,"0010","Created");
        public static TicketStatusModel PendingTicketStatus      = new TicketStatusModel(2, "0020", "Pending");
        public static TicketStatusModel ProcessingTicketStatus   = new TicketStatusModel(3, "0030", "Processing");
        public static TicketStatusModel ResolvedTicketStatus     = new TicketStatusModel(4, "0040", "Resolved");
        public static TicketStatusModel ClosedTicketStatus       = new TicketStatusModel(5, "0050", "Closed");

        public long     StatusId    { get; private set; }
        public string   StatusCode { get; private set; }
        public string StatusDescription { get; private set; }

        public TicketStatusModel(long pStatusId, string pStatusCode, string pStatusDescription)
        {
            StatusId = pStatusId;
            StatusCode = pStatusCode;
            StatusDescription = pStatusDescription;
        }

        public TicketStatusModel UpdateStatus(long pStatusId, string pStatusCode, string pStatusDescription)
        {
            if(pStatusId == StatusId)
                throw new InvalidOperationException("Can't change the status the same status");

            return new TicketStatusModel(pStatusId, pStatusCode, pStatusDescription);
        }

        public static TicketStatusModel GetStatus(long pStatusId)
        {
            switch (pStatusId)
            {
                case 1:
                    return CreatedTicketStatus;
                case 2:
                    return PendingTicketStatus;
                case 3:
                    return ProcessingTicketStatus;
                case 4:
                    return ResolvedTicketStatus;
                case 5:
                    return ClosedTicketStatus;
            }

            return CreatedTicketStatus;
        }
    }
}
