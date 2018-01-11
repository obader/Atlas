using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.Core.Logic.Entities
{
    public class Comment:Entity
    {

        public string CommentEntry { get; private set; }
        public string UserId { get; private set; }
        public DateTime Date { get; private set; }
        public long  TicketId { get; private set; }

        public Comment(long pTicketId, long pCommentId, string pUserId, string pComment,DateTime? pDateTime)
        {
            if (string.IsNullOrWhiteSpace(pComment))
                throw new ArgumentException("Invalid Comment value", nameof(pComment));
            TicketId = pTicketId;
            Id = pCommentId;
            UserId = pUserId;
            CommentEntry = pComment;
            Date = pDateTime ?? DateTime.UtcNow;
        }

    }
}
