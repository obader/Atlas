using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.Core.Logic.Entities
{
    public class TicketExternalReferences : Entity
    {
        public string UserId { get; private set; }
        public long TicketId { get; private set; }
        public string  TypeCode { get; }
        public string  Comments { get; private set; }
        public string PayLoad { get; set; }

        public DateTime Date { get; set; }

        public TicketExternalReferences(long pId, string pUserId,DateTime? pDate, long pTicketId=-1, string pComments = "",string pTypeCode = "", string pPayLoad ="")
        {
            Id = pId;
            UserId = pUserId;
            TicketId = pTicketId;
            Comments = pComments;
            Date = pDate ?? DateTime.UtcNow;
            TypeCode = pTypeCode;
            PayLoad = pPayLoad;
        }

    }
}
