using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Atlas.Core.Logic.DTOs
{
    public class CommentObject
    {
        public long CommentId { get; set; }
        public long TicketId { get; set; }
        public string CommentValue { get; set; }
        public string UserId { get; set; }
        public System.DateTime RecordDate { get; set; }
    }
}