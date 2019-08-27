using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.Core.Logic.DTOs
{
    public class ActionNotificationDynamic
    {
        public string Code { get; set; }
        public int? BankId { get; set; }
        public long? ChannelId { get; set; }
        public string Channel { get; set; }
    }
}
