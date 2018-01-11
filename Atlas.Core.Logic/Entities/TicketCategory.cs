using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Atlas.Core.Logic.ValueObjects;

namespace Atlas.Core.Logic.Entities
{
    public class TicketCategory : Entity
    {
       public Property Value { get; private set; }

       public List<Resolution> Resolution { get; private set; }

        public List<Reason> Reason { get; private set; }

        public TicketCategory(long pId, string pCode, string pDescription, List<Resolution> pStatus, List<Reason> pReason)
        {
            Id = pId;
            Value = new Property(pCode,pDescription);
            Resolution = pStatus;
            Reason = pReason;
        }
    }
}
