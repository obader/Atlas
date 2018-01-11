using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.Core.Logic.Entities
{
    public class TicketStatus:Entity
    {
        public string UserId { get; private set; }
        public long TicketId { get; private set; }

        public DateTime Date { get; }
        public ValueObjects.TicketStatusModel Status { get; private set; }

        public string ActionName { get; set; }

        public TicketStatus(ValueObjects.TicketStatusModel pStatus, string pUserId,DateTime? pDate, long pTicketId=-1, string pActionName = "")
        {
            UserId = pUserId;
            TicketId = pTicketId;
            Status = pStatus;
            Date = pDate ?? DateTime.UtcNow;
            ActionName = pActionName;
        }

    }
}
