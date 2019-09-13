using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations.Model;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.Core.Logic.Entities
{
    public class Ticket : Entity
    {
        public string UserId { get; private set; }
        public long ApplicationId { get; set; }

        public long TicketParentId { get; set; }
        public long CategoryId { get; set; }
        public string CategoryCode { get; set; }
        public long ReasonsId { get; set; }
        public string ReasonsName { get; set; }
        public string Status { get; set; }
        public int BankId { get; set; }
        public long? ProfileId { get; set; }
        public long? CustomerId { get; set; }
        public long? PriorityId { get; set; }
        public string TransactionId { get; set; }
        public long? DepartmentId { get; set; }
        public string Description { get; set; }
        public string Title { get; private set; }
        public DateTime CreationDate { get; private set; }
        public DateTime ModifiedDate { get; private set; }
        public Profile Profile { get; set; }
        public string BankName { get; set; }
        public string MobileNumber { get; set; }

        public Ticket(long pTicketId, long pTicketParentId, string pUserId, int pBankId, long? pProfileId, long? pCustomerId,
            string pTitle, string pDescription, long pApplicationId, long pCategoryId, string pCategoryCode, 
            long pReasonsId, string pReasonsName, string pStatus, long? pPriorityId,
            long? pDepartmentId, DateTime? pCreatedDate, DateTime? pLastChangeDate)
        {
            Id = pTicketId;
            TicketParentId = pTicketParentId;
            Title = pTitle;
            Description = pDescription;
            UserId = pUserId;
            ApplicationId = pApplicationId;
            CategoryId = pCategoryId;
            CreationDate = pCreatedDate ?? DateTime.UtcNow;
            ModifiedDate = pLastChangeDate ?? DateTime.UtcNow;
            BankId = pBankId;
            ProfileId = pProfileId;
            CustomerId = pCustomerId;
            DepartmentId = pDepartmentId;
            PriorityId = pPriorityId;
            CategoryCode = pCategoryCode;
            Status = pStatus;
            ReasonsId = pReasonsId;
            ReasonsName = pReasonsName;
        }
    }
}
