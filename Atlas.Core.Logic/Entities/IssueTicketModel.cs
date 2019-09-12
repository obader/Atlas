using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.Core.Logic.Entities
{
    public class IssueTicketModel
    {
        public string CategoryId { get; set; }
        public IssueTicketDetails Details { get; set; }
        public IssueTicketSubscriber Subscriber { get; set; }
        public IssueTicketTransaction Transaction { get; set; }
    }
}
