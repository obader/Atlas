using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.Core.Logic.Entities
{
    public class IssueTicketDetails
    {
        public long ReasonId { get; set; }
        public string Description { get; set; }
        public string Comments { get; set; }
    }
}
