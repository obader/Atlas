using PinPayObjects.BaseObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.Core.Logic
{
    public class DataAccess
    {
        protected DBConnectionInfo _dbConnectionInfo;
        public DataAccess(DBConnectionInfo pDbConnectionInfo)
        {
            _dbConnectionInfo = pDbConnectionInfo;
        }

        public List<DTOs.TicketObject> GetTickets(DateTime? FromDate,DateTime? ToDate,long? CategoryId,long? PriorityId,long? TicketStatusId,long? ApplicationId,long? BankId)
        {
            var result = new List<DTOs.TicketObject>();
            try
            {
                using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_dbConnectionInfo))
                {
                    var tickets = (from t in ctx.Tickets
                                   join c in ctx.Categories on t.CategoryId equals c.CategoryId
                                   join a in ctx.Applications on t.ApplicationId equals a.ApplicationId
                                   join d in ctx.Departments on t.AssignedToDepartmentId equals d.DepartmentId
                                   where (
                                   (FromDate == null || t.CreationDate >= FromDate)
                                   && (ToDate == null || t.CreationDate <= ToDate)
                                   && (CategoryId == null || t.CategoryId == CategoryId)
                                   && (TicketStatusId == null || t.TicketStatusId == TicketStatusId)
                                   && (ApplicationId == null || t.ApplicationId == ApplicationId)
                                   && (BankId == null || t.BankId == BankId)
                               )
                                   select (new DTOs.TicketObject
                                   {
                                       UserId = t.CreatedBy,
                                       ApplicationId = t.ApplicationId ?? 0,
                                       ApplicationName = a.ApplicationCode,
                                       CategoryId = t.CategoryId ?? 0,
                                       CategoryName = c.Description,
                                       BankId = t.BankId ?? 0,
                                       ProfileId = t.ProfileId,
                                       PriorityId = t.PriorityId ?? 0,
                                       DepartmentId = t.AssignedToDepartmentId,
                                       DepartmentName = d.Description,
                                       Description = t.Description,
                                       Title = t.Title,
                                       CreationDate = t.CreationDate,
                                       ModifiedDate = t.ModifedDate

                                   })).ToList();
                    return tickets;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}