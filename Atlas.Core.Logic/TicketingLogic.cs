using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Atlas.Core.Logic.Aggregates;
using Atlas.Core.Logic.Entities;
using Comment = Atlas.Core.Logic.Entities.Comment;
using Ticket = Atlas.Core.Logic.Entities.Ticket;
using TicketTransaction = Atlas.Core.Logic.Entities.TicketTransaction;
using PinPayObjects.BaseObjects;
using System.Data;
using static Atlas.Core.Logic.Entities.Enums;
using Atlas.Core.Logic.ValueObjects;

namespace Atlas.Core.Logic
{
    public class TicketingLogic
    {
        protected DBConnectionInfo _connectionInfo;
        public TicketingLogic(DBConnectionInfo pConnectionInfo)
        {
            _connectionInfo = pConnectionInfo;
        }

        public TicketingLogic(string host, string catalog, string user, string pass, bool winAuth)
        {
            _connectionInfo = new DBConnectionInfo(host, catalog, user, pass, winAuth);
        }

        public bool CheckDb()
        {
            bool lResult = false;
            try
            {
                using (var dbContext = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
                {
                    dbContext.Database.Connection.Open();
                    if (dbContext.Database.Connection.State == ConnectionState.Open)
                    {
                        lResult = true;
                        dbContext.Database.Connection.Close();
                    }
                }
            }
            catch (Exception)
            {
                lResult = false;
            }
            return lResult;
        }

        public List<MasterTicket> GeTickets(string pUserId, DateTime? pFromDate, DateTime? pToDate, int pStatusId, int pCategoryId, int pProfileId, int pCustomerId, string pStatusesFilterOut, int pBankId)
        {
            var lTickets = new List<MasterTicket>();
            string category = string.Empty;
            string statusCode = string.Empty;
            string reason = string.Empty;
            MasterTicket parentTicket = null;

            using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
            {
                try
                {
                    // filter out unwanted statuses
                    string[] _pFilter = pStatusesFilterOut != null ? pStatusesFilterOut.Split(',') : new string[] { };
                    List<long?> pFilter = new List<long?>();
                    foreach (string f in _pFilter) pFilter.Insert(0, Convert.ToInt64(f));

                    var statuses =
                    ctx.TicketAudits.Where(
                            p => (pStatusId == 0 || p.TicketStatusId == pStatusId) && (pFromDate == null || p.ChangeDate >= pFromDate) && (p.ChangeDate <= pToDate || pToDate == null)
                            && (pFilter.Count == 0 || !pFilter.Contains(p.TicketStatusId) && p.TicketStatusId != null)
                        )
                              .ToList();

                    if (statuses.Count == 0)
                        return lTickets;

                    var statusesIds = ctx.TicketStatuses.Where(p => _pFilter.Contains(p.Value)).Select(q => q.TicketStatusId).ToList();


                    var ticketIds = statuses.Select(q => q.TicketId).ToList();
                    if (ticketIds.Count == 0)
                        return lTickets;

                    var tickets =
                        ctx.Tickets.Where(p => (pBankId == 0 || (pBankId > 0 && p.BankId == pBankId)) && (pCategoryId == 0 || pCategoryId == p.CategoryId) && ticketIds.Contains(p.TicketId) && (pCustomerId == 0 || pCustomerId == p.CustomerId) && !statusesIds.Contains(p.TicketStatusId)).Distinct()
                            .AsNoTracking()
                            .ToList();

                    if (tickets.Count == 0)
                        return lTickets;

                    foreach (var ticket in tickets)
                    {

                        if (ticket.CategoryId != null && ticket.CategoryId > 0)
                        {
                            int categoryId = Convert.ToInt32(ticket.CategoryId);
                            var etcategory = ctx.Categories.Where(p => p.CategoryId == categoryId).FirstOrDefault();
                            category = etcategory.Code;
                        }

                        if (ticket.ReasonsId != null && ticket.ReasonsId > 0)
                        {
                            int reasonsId = Convert.ToInt32(ticket.ReasonsId);
                            var etReasons = ctx.Reasons.Where(p => p.ReasonsId == reasonsId).FirstOrDefault();
                            reason = etReasons.Description;
                        }
                        var sts =
                            ctx.TicketAudits.Where(p => p.TicketId == ticket.TicketId && p.TicketStatusId != null)
                                .ToList();


                        var etr =
                          ctx.TicketExternalReferences.Where(p => p.TicketId == ticket.TicketId)
                              .ToList();

                        var statusList =
                            sts.Select(st => new Entities.TicketStatus(new ValueObjects.TicketStatusModel(st.TicketStatus.TicketStatusId, st.TicketStatus.Value, st.TicketStatus.Description), st.UserId, null, ticket.TicketId, st.TicketAction != null ? st.TicketAction.Description : "")).ToList();


                        var externalReferencesList =
                         etr.Select(st => new Entities.TicketExternalReferences(st.TicketExternalReferencesId, st.UserId, st.RecordDate, st.TicketId, st.Comments, st.TypeCode, st.PayLoad)).ToList();


                        var lCommentList =
                            ctx.Comments.Where(p => p.TicketId == ticket.TicketId).AsNoTracking().ToList();
                        var commentList = lCommentList.Select(cm => new Comment(ticket.TicketId, cm.CommentId, cm.UserId, cm.CommentValue, cm.RecordDate))
                                .ToList();

                        statusCode = statusList.LastOrDefault()?.Status?.StatusCode;

                        parentTicket = null;
                        if (ticket.TicketParentId != null)
                            parentTicket = GetTicketParent(ticket.TicketParentId.Value, pUserId);

                        var mticket = new MasterTicket(pUserId,
                            new Ticket(ticket.TicketId, ticket.TicketParentId ?? 0, ticket.CreatedBy, ticket.BankId ?? 0, ticket.ProfileId, ticket.CustomerId, ticket.Title, ticket.Description, ticket.ApplicationId ?? 0, ticket.CategoryId ?? 0, category, ticket.ReasonsId ?? 0, reason, statusCode, ticket.PriorityId, ticket.AssignedToDepartmentId,
                                ticket.CreationDate, ticket.ModifedDate), statusList, commentList, null, externalReferencesList, parentTicket);
                        lTickets.Add(mticket);
                    }
                }
                catch (Exception ex)
                {

                    throw ex;
                }

            }
            return lTickets;
        }

        public List<MasterTicket> GeTicketsByCategoryId(string pUserId, int pCategoryId, int pBankId, int statusId, int page, int itemsPerPage,
            long ticketId, long transactionId, long profileId)
        {
            var lTickets = new List<MasterTicket>();
            string category = string.Empty;
            string statusCode = string.Empty;
            string reason = string.Empty;
            MasterTicket parentTicket = null;
            IQueryable<DM.Ticket> ticketsQuery = null;

            using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
            {
                try
                {
                    
                    if (transactionId > 0)
                    {
                        ticketsQuery = (from ticketTransaction in ctx.TicketTransactions
                                        orderby ticketTransaction.Ticket.LastStatusChanged descending
                                        where ticketTransaction.Ticket.CategoryId == pCategoryId
                                        && (pBankId == 0 || (pBankId > 0 && ticketTransaction.Ticket.BankId == pBankId))
                                        && (statusId == 0 || (statusId > 0 && ticketTransaction.Ticket.TicketAudits.Where(p => p.TicketStatusId != null).FirstOrDefault() != null && ticketTransaction.Ticket.TicketAudits.Where(p => p.TicketStatusId != null).OrderByDescending(p => p.ChangeDate).FirstOrDefault().TicketStatusId == statusId))
                                        && (ticketId == 0 || (ticketId > 0 && ticketTransaction.Ticket.TicketId == ticketId))
                                        && (transactionId == 0 || (transactionId > 0 && ticketTransaction.TransactionId == transactionId.ToString()))
                                        && (profileId == 0 || (profileId > 0 && ticketTransaction.Ticket.ProfileId == profileId))
                                        select ticketTransaction.Ticket
                              );
                    }
                    else
                    {
                        ticketsQuery =
                             (from ticket in ctx.Tickets
                              orderby ticket.LastStatusChanged descending
                              where ticket.CategoryId == pCategoryId
                              && (pBankId == 0 || (pBankId > 0 && ticket.BankId == pBankId))
                              && (statusId == 0 || (statusId > 0 && ticket.TicketAudits.Where(p => p.TicketStatusId != null).FirstOrDefault()!= null && ticket.TicketAudits.Where(p => p.TicketStatusId != null).OrderByDescending(p=>p.ChangeDate).FirstOrDefault().TicketStatusId == statusId))
                              && (ticketId == 0 || (ticketId > 0 && ticket.TicketId == ticketId))
                              && (transactionId == 0 || (transactionId > 0 && ticket.TicketTransactions.Where(w => w.TransactionId == transactionId.ToString()).Any()))
                              && (profileId == 0 || (profileId > 0 && ticket.ProfileId == profileId))
                              select ticket
                              );
                    }

                    if (page != 0 && itemsPerPage != 0)
                        ticketsQuery = ticketsQuery.Skip((page - 1) * itemsPerPage).Take(itemsPerPage);


                    List<DM.Ticket> tickets = ticketsQuery.ToList();

                    foreach (var ticket in tickets)
                    {
                        if (ticket.CategoryId != null && ticket.CategoryId > 0)
                        {
                            int categoryId = Convert.ToInt32(ticket.CategoryId);
                            var etcategory = ctx.Categories.Where(p => p.CategoryId == categoryId).FirstOrDefault();
                            category = etcategory.Code;
                        }

                        if (ticket.ReasonsId != null && ticket.ReasonsId > 0)
                        {
                            int reasonsId = Convert.ToInt32(ticket.ReasonsId);
                            var etReasons = ctx.Reasons.Where(p => p.ReasonsId == reasonsId).FirstOrDefault();
                            if (etReasons != null)
                                reason = etReasons.Description;
                        }

                        var transaction =
                          ctx.TicketTransactions.Where(p => p.TicketId == ticket.TicketId)
                              .FirstOrDefault();

                        var sts =
                            ctx.TicketAudits.Where(p => p.TicketId == ticket.TicketId && p.TicketStatusId != null)
                                .ToList();

                        var etr =
                         ctx.TicketExternalReferences.Where(p => p.TicketId == ticket.TicketId)
                             .ToList();

                        var statusList =
                            sts.Select(st => new Entities.TicketStatus(new ValueObjects.TicketStatusModel(st.TicketStatus.TicketStatusId, st.TicketStatus.Value, st.TicketStatus.Description), st.UserId, null, ticket.TicketId, st.TicketAction != null ? st.TicketAction.Description : "")).ToList();

                        var externalReferencesList =
                             etr.Select(st => new Entities.TicketExternalReferences(st.TicketExternalReferencesId, st.UserId, st.RecordDate, st.TicketId, st.Comments, st.TypeCode, st.PayLoad)).ToList();

                        var lCommentList =
                            ctx.Comments.Where(p => p.TicketId == ticket.TicketId).AsNoTracking().ToList();

                        var commentList = lCommentList.Select(cm => new Comment(ticket.TicketId, cm.CommentId, cm.UserId, cm.CommentValue, cm.RecordDate))
                                .ToList();

                        statusCode = statusList.LastOrDefault()?.Status?.StatusCode;

                        parentTicket = null;
                        if (ticket.TicketParentId != null)
                            parentTicket = GetTicketParent(ticket.TicketParentId.Value, pUserId);


                        MasterTicket mticket = null;

                        if (transaction != null)
                        {
                            mticket = new MasterTicket(pUserId,
                                      new Ticket(ticket.TicketId, ticket.TicketParentId ?? 0, ticket.CreatedBy, ticket.BankId ?? 0, ticket.ProfileId, ticket.CustomerId, ticket.Title, ticket.Description, ticket.ApplicationId ?? 0, ticket.CategoryId ?? 0, category, ticket.ReasonsId ?? 0, reason, statusCode, ticket.PriorityId, ticket.AssignedToDepartmentId,
                                                ticket.CreationDate, ticket.ModifedDate), statusList, commentList, new List<Entities.TicketTransaction> {
                                               new Entities.TicketTransaction(transaction.TicketId, transaction.BankId, transaction.BankName, transaction.TransactionId,transaction.RequestId, transaction.ProviderId, transaction.ProviderName, transaction.PaymentTypeId, transaction.PaymentTypeName, transaction.AccountType, transaction.AccountNumber, transaction.TransactionStatus, transaction.Amount,transaction.PaymentAmount, transaction.TransactionDate, transaction.CurrencyId, transaction.CurrencyCode, transaction.PaymentOptionId, transaction.PaymentOptionName,transaction.SourceChannel, transaction.SFM, transaction.BankTransactionId, transaction.PaymentCurrencyId ?? 0)
                                              }, externalReferencesList, parentTicket);
                        }
                        else
                        {
                            mticket = new MasterTicket(pUserId,
                                      new Ticket(ticket.TicketId, ticket.TicketParentId ?? 0, ticket.CreatedBy, ticket.BankId ?? 0, ticket.ProfileId, ticket.CustomerId, ticket.Title, ticket.Description, ticket.ApplicationId ?? 0, ticket.CategoryId ?? 0, category, ticket.ReasonsId ?? 0, reason, statusCode, ticket.PriorityId, ticket.AssignedToDepartmentId,
                                      ticket.CreationDate, ticket.ModifedDate), statusList, commentList, null, externalReferencesList, parentTicket);
                        }

                        if (mticket != null && mticket.Ticket != null && transaction != null && transaction.TransactionId != "")
                        {
                            mticket.Ticket.TransactionId = transaction.TransactionId;
                        }

                        lTickets.Add(mticket);
                    }
                }
                catch (Exception ex)
                {

                    throw ex;
                }
            }
            return lTickets;
        }

        public int GeTicketsCountByCategoryId(string pUserId, int pCategoryId, int pBankId, int statusId, int page, int itemsPerPage,
            long ticketId, long transactionId, long profileId)
        {
            var lTickets = new List<MasterTicket>();
            string category = string.Empty;
            string statusCode = string.Empty;
            string reason = string.Empty;
            IQueryable<DM.Ticket> ticketsQuery = null;

            using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
            {
                try
                {
                    if (transactionId > 0)
                    {
                        ticketsQuery = (from ticketTransaction in ctx.TicketTransactions
                                        where ticketTransaction.Ticket.CategoryId == pCategoryId
                                        && (pBankId == 0 || (pBankId > 0 && ticketTransaction.Ticket.BankId == pBankId))
                                        && (statusId == 0 || (statusId > 0 && ticketTransaction.Ticket.TicketAudits.Where(p=>p.TicketStatusId!= null).FirstOrDefault() != null && ticketTransaction.Ticket.TicketAudits.Where(p => p.TicketStatusId != null).OrderByDescending(p => p.ChangeDate).FirstOrDefault().TicketStatusId == statusId))
                                        && (ticketId == 0 || (ticketId > 0 && ticketTransaction.Ticket.TicketId == ticketId))
                                        && (transactionId == 0 || (transactionId > 0 && ticketTransaction.TransactionId == transactionId.ToString()))
                                        && (profileId == 0 || (profileId > 0 && ticketTransaction.Ticket.ProfileId == profileId))
                                        select ticketTransaction.Ticket
                              );
                    }
                    else
                    {
                        ticketsQuery =
                         (from ticket in ctx.Tickets
                          orderby ticket.CreationDate descending
                          where ticket.CategoryId == pCategoryId
                          && (pBankId == 0 || (pBankId > 0 && ticket.BankId == pBankId))
                          && (statusId == 0 || (statusId > 0 && ticket.TicketAudits.Where(p => p.TicketStatusId != null).FirstOrDefault() != null && ticket.TicketAudits.Where(p => p.TicketStatusId != null).OrderByDescending(p => p.ChangeDate).FirstOrDefault().TicketStatusId == statusId))
                          && (ticketId == 0 || (ticketId > 0 && ticket.TicketId == ticketId))
                          && (transactionId == 0 || (transactionId > 0 && ticket.TicketTransactions.Where(w => w.TransactionId == transactionId.ToString()).Any()))
                          && (profileId == 0 || (profileId > 0 && ticket.ProfileId == profileId))
                          select ticket
                          );
                    }

                    return ticketsQuery.Count();
                }
                catch (Exception ex)
                {

                    throw ex;
                }
            }
        }


        private MasterTicket GetTicketParent(long ticketId, string pUserId)
        {
            string category = string.Empty;
            string reason = string.Empty;
            string statusCode = string.Empty;
            MasterTicket parentTicket = null;
            MasterTicket mticket = null;

            using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
            {
                var ticket =
                    ctx.Tickets.Where(p => p.TicketId == ticketId)
                        .AsNoTracking()
                        .FirstOrDefault();

                if (ticket.CategoryId != null && ticket.CategoryId > 0)
                {
                    int categoryId = Convert.ToInt32(ticket.CategoryId);
                    var etcategory = ctx.Categories.Where(p => p.CategoryId == categoryId).FirstOrDefault();
                    category = etcategory.Code;
                }

                if (ticket.ReasonsId != null && ticket.ReasonsId > 0)
                {
                    int reasonsId = Convert.ToInt32(ticket.ReasonsId);
                    var etReasons = ctx.Reasons.Where(p => p.ReasonsId == reasonsId).FirstOrDefault();
                    if (etReasons != null)
                        reason = etReasons.Description;
                }

                var transaction =
                  ctx.TicketTransactions.Where(p => p.TicketId == ticket.TicketId)
                      .FirstOrDefault();

                var sts =
                    ctx.TicketAudits.Where(p => p.TicketId == ticket.TicketId && p.TicketStatusId != null)
                        .ToList();

                var etr =
                 ctx.TicketExternalReferences.Where(p => p.TicketId == ticket.TicketId)
                     .ToList();

                var statusList =
                    sts.Select(st => new Entities.TicketStatus(new ValueObjects.TicketStatusModel(st.TicketStatus.TicketStatusId, st.TicketStatus.Value, st.TicketStatus.Description), st.UserId, null, ticket.TicketId, st.TicketAction != null ? st.TicketAction.Description : "")).ToList();

                var externalReferencesList =
                     etr.Select(st => new Entities.TicketExternalReferences(st.TicketExternalReferencesId, st.UserId, st.RecordDate, st.TicketId, st.Comments, st.TypeCode, st.PayLoad)).ToList();

                var lCommentList =
                    ctx.Comments.Where(p => p.TicketId == ticket.TicketId).AsNoTracking().ToList();

                var commentList = lCommentList.Select(cm => new Comment(ticket.TicketId, cm.CommentId, cm.UserId, cm.CommentValue, cm.RecordDate))
                        .ToList();

                statusCode = statusList.LastOrDefault()?.Status?.StatusCode;

                parentTicket = null;
                if (ticket.TicketParentId != null)
                    parentTicket = GetTicketParent(ticket.TicketParentId.Value, pUserId);

                if (transaction != null)
                {
                    mticket = new MasterTicket(pUserId,
                              new Ticket(ticket.TicketId, ticket.TicketParentId ?? 0, ticket.CreatedBy, ticket.BankId ?? 0, ticket.ProfileId, ticket.CustomerId, ticket.Title, ticket.Description, ticket.ApplicationId ?? 0, ticket.CategoryId ?? 0, category, ticket.ReasonsId ?? 0, reason, statusCode, ticket.PriorityId, ticket.AssignedToDepartmentId,
                                        ticket.CreationDate, ticket.ModifedDate), statusList, commentList, new List<Entities.TicketTransaction> {
                                               new Entities.TicketTransaction(transaction.TicketId, transaction.BankId, transaction.BankName, transaction.TransactionId,transaction.RequestId, transaction.ProviderId, transaction.ProviderName, transaction.PaymentTypeId, transaction.PaymentTypeName, transaction.AccountType, transaction.AccountNumber, transaction.TransactionStatus, transaction.Amount,transaction.PaymentAmount, transaction.TransactionDate, transaction.CurrencyId, transaction.CurrencyCode, transaction.PaymentOptionId, transaction.PaymentOptionName,transaction.SourceChannel, transaction.SFM, transaction.BankTransactionId, transaction.PaymentCurrencyId ?? 0)
                                      }, externalReferencesList, parentTicket);
                }
                else
                {
                    mticket = new MasterTicket(pUserId,
                              new Ticket(ticket.TicketId, ticket.TicketParentId ?? 0, ticket.CreatedBy, ticket.BankId ?? 0, ticket.ProfileId, ticket.CustomerId, ticket.Title, ticket.Description, ticket.ApplicationId ?? 0, ticket.CategoryId ?? 0, category, ticket.ReasonsId ?? 0, reason, statusCode, ticket.PriorityId, ticket.AssignedToDepartmentId,
                              ticket.CreationDate, ticket.ModifedDate), statusList, commentList, null, externalReferencesList, parentTicket);
                }

                if (mticket != null && mticket.Ticket != null && transaction != null && transaction.TransactionId != "")
                {
                    mticket.Ticket.TransactionId = transaction.TransactionId;
                }
            }
            return mticket;
        }

        public List<MasterTicket> GeTicketsByProfileId(string pUserId, int pProfiled, int pBankId)
        {
            var lTickets = new List<MasterTicket>();
            string category = string.Empty;
            string statusCode = string.Empty;
            string reason = string.Empty;
            MasterTicket parentTicket = null;

            using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
            {
                try
                {
                    var tickets =
                        ctx.Tickets.Where(p => p.ProfileId == pProfiled && (pBankId == 0 || (pBankId > 0 && p.BankId == pBankId))).Distinct()
                            .AsNoTracking()
                            .ToList();

                    if (tickets.Count == 0)
                        return lTickets;

                    foreach (var ticket in tickets)
                    {
                        if (ticket.CategoryId != null && ticket.CategoryId > 0)
                        {
                            int categoryId = Convert.ToInt32(ticket.CategoryId);
                            var etcategory = ctx.Categories.Where(p => p.CategoryId == categoryId).FirstOrDefault();
                            category = etcategory.Code;
                        }

                        if (ticket.ReasonsId != null && ticket.ReasonsId > 0)
                        {
                            int reasonsId = Convert.ToInt32(ticket.ReasonsId);
                            var etReasons = ctx.Reasons.Where(p => p.ReasonsId == reasonsId).FirstOrDefault();
                            reason = etReasons.Description;
                        }

                        var transaction = ctx.TicketTransactions.FirstOrDefault(p => p.TicketId == ticket.TicketId);

                        var sts =
                            ctx.TicketAudits.Where(p => p.TicketId == ticket.TicketId && p.TicketStatusId != null)
                                .ToList();

                        var etr =
                         ctx.TicketExternalReferences.Where(p => p.TicketId == ticket.TicketId)
                             .ToList();


                        var statusList =
                            sts.Select(st => new Entities.TicketStatus(new ValueObjects.TicketStatusModel(st.TicketStatus.TicketStatusId, st.TicketStatus.Value, st.TicketStatus.Description), st.UserId, null, ticket.TicketId, st.TicketAction != null ? st.TicketAction.Description : "")).ToList();

                        var externalReferencesList =
                             etr.Select(st => new Entities.TicketExternalReferences(st.TicketExternalReferencesId, st.UserId, st.RecordDate, st.TicketId, st.Comments, st.TypeCode, st.PayLoad)).ToList();


                        var lCommentList =
                            ctx.Comments.Where(p => p.TicketId == ticket.TicketId).AsNoTracking().ToList();

                        var commentList = lCommentList.Select(cm => new Comment(ticket.TicketId, cm.CommentId, cm.UserId, cm.CommentValue, cm.RecordDate))
                                .ToList();

                        if (statusList.Last() != null && statusList.Last().Status != null)
                        {
                            statusCode = statusList.Last().Status.StatusCode;
                        }

                        parentTicket = null;
                        if (ticket.TicketParentId != null)
                            parentTicket = GetTicketParent(ticket.TicketParentId.Value, pUserId);

                        MasterTicket mticket = null;

                        if (transaction != null)
                        {
                            mticket = new MasterTicket(pUserId,
                            new Ticket(ticket.TicketId, ticket.TicketParentId ?? 0, ticket.CreatedBy, ticket.BankId ?? 0, ticket.ProfileId, ticket.CustomerId, ticket.Title, ticket.Description, ticket.ApplicationId ?? 0, ticket.CategoryId ?? 0, category, ticket.ReasonsId ?? 0, reason, statusCode, ticket.PriorityId, ticket.AssignedToDepartmentId,
                                ticket.CreationDate, ticket.ModifedDate), statusList, commentList, new List<Entities.TicketTransaction> {
                                    new Entities.TicketTransaction(transaction.TicketId, transaction.BankId, transaction.BankName, transaction.TransactionId,transaction.RequestId, transaction.ProviderId, transaction.ProviderName, transaction.PaymentTypeId, transaction.PaymentTypeName, transaction.AccountType, transaction.AccountNumber, transaction.TransactionStatus, transaction.Amount,transaction.PaymentAmount, transaction.TransactionDate, transaction.CurrencyId, transaction.CurrencyCode, transaction.PaymentOptionId, transaction.PaymentOptionName,transaction.SourceChannel, transaction.SFM, transaction.BankTransactionId, transaction.PaymentCurrencyId ?? 0)
                                }, externalReferencesList, parentTicket);
                        }
                        else
                        {
                            mticket = new MasterTicket(pUserId,
                            new Ticket(ticket.TicketId, ticket.TicketParentId ?? 0, ticket.CreatedBy, ticket.BankId ?? 0, ticket.ProfileId, ticket.CustomerId, ticket.Title, ticket.Description, ticket.ApplicationId ?? 0, ticket.CategoryId ?? 0, category, ticket.ReasonsId ?? 0, reason, statusCode, ticket.PriorityId, ticket.AssignedToDepartmentId,
                                ticket.CreationDate, ticket.ModifedDate), statusList, commentList, null, externalReferencesList, parentTicket);
                        }



                        if (mticket != null && mticket.Ticket != null && transaction != null && transaction.TransactionId != "")
                        {
                            mticket.Ticket.TransactionId = transaction.TransactionId;
                        }



                        lTickets.Add(mticket);
                    }
                }
                catch (Exception ex)
                {

                    throw ex;
                }

            }
            return lTickets;
        }

        public List<MasterTicket> GeTicketsByCustomerId(string pUserId, int pCustomerd, int pBankId)
        {
            var lTickets = new List<MasterTicket>();
            string category = string.Empty;
            string statusCode = string.Empty;
            string reason = string.Empty;
            MasterTicket parentTicket = null;

            using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
            {
                try
                {
                    var tickets =
                        ctx.Tickets.Where(p => p.CustomerId == pCustomerd && (pBankId == 0 || (pBankId > 0 && p.BankId == pBankId))).Distinct()
                            .AsNoTracking()
                            .ToList();

                    if (tickets.Count == 0)
                        return lTickets;

                    foreach (var ticket in tickets)
                    {
                        if (ticket.CategoryId != null && ticket.CategoryId > 0)
                        {
                            int categoryId = Convert.ToInt32(ticket.CategoryId);
                            var etcategory = ctx.Categories.Where(p => p.CategoryId == categoryId).FirstOrDefault();
                            category = etcategory.Code;
                        }

                        if (ticket.ReasonsId != null && ticket.ReasonsId > 0)
                        {
                            int reasonsId = Convert.ToInt32(ticket.ReasonsId);
                            var etReasons = ctx.Reasons.Where(p => p.ReasonsId == reasonsId).FirstOrDefault();
                            reason = etReasons.Description;
                        }

                        var transaction = ctx.TicketTransactions.FirstOrDefault(p => p.TicketId == ticket.TicketId);

                        var sts =
                            ctx.TicketAudits.Where(p => p.TicketId == ticket.TicketId && p.TicketStatusId != null)
                                .ToList();

                        var etr =
                         ctx.TicketExternalReferences.Where(p => p.TicketId == ticket.TicketId)
                             .ToList();


                        var statusList =
                            sts.Select(st => new Entities.TicketStatus(new ValueObjects.TicketStatusModel(st.TicketStatus.TicketStatusId, st.TicketStatus.Value, st.TicketStatus.Description), st.UserId, null, ticket.TicketId, st.TicketAction != null ? st.TicketAction.Description : "")).ToList();

                        var externalReferencesList =
                             etr.Select(st => new Entities.TicketExternalReferences(st.TicketExternalReferencesId, st.UserId, st.RecordDate, st.TicketId, st.Comments, st.TypeCode, st.PayLoad)).ToList();


                        var lCommentList =
                            ctx.Comments.Where(p => p.TicketId == ticket.TicketId).AsNoTracking().ToList();

                        var commentList = lCommentList.Select(cm => new Comment(ticket.TicketId, cm.CommentId, cm.UserId, cm.CommentValue, cm.RecordDate))
                                .ToList();

                        if (statusList.Last() != null && statusList.Last().Status != null)
                        {
                            statusCode = statusList.Last().Status.StatusCode;
                        }

                        parentTicket = null;
                        if (ticket.TicketParentId != null)
                            parentTicket = GetTicketParent(ticket.TicketParentId.Value, pUserId);

                        MasterTicket mticket = null;

                        if (transaction != null)
                        {
                            mticket = new MasterTicket(pUserId,
                            new Ticket(ticket.TicketId, ticket.TicketParentId ?? 0, ticket.CreatedBy, ticket.BankId ?? 0, ticket.ProfileId, ticket.CustomerId, ticket.Title, ticket.Description, ticket.ApplicationId ?? 0, ticket.CategoryId ?? 0, category, ticket.ReasonsId ?? 0, reason, statusCode, ticket.PriorityId, ticket.AssignedToDepartmentId,
                                ticket.CreationDate, ticket.ModifedDate), statusList, commentList, new List<Entities.TicketTransaction> {
                                    new Entities.TicketTransaction(transaction.TicketId, transaction.BankId, transaction.BankName, transaction.TransactionId,transaction.RequestId, transaction.ProviderId, transaction.ProviderName, transaction.PaymentTypeId, transaction.PaymentTypeName, transaction.AccountType, transaction.AccountNumber, transaction.TransactionStatus, transaction.Amount,transaction.PaymentAmount, transaction.TransactionDate, transaction.CurrencyId, transaction.CurrencyCode, transaction.PaymentOptionId, transaction.PaymentOptionName,transaction.SourceChannel, transaction.SFM, transaction.BankTransactionId, transaction.PaymentCurrencyId ?? 0)
                                }, externalReferencesList, parentTicket);
                        }
                        else
                        {
                            mticket = new MasterTicket(pUserId,
                            new Ticket(ticket.TicketId, ticket.TicketParentId ?? 0, ticket.CreatedBy, ticket.BankId ?? 0, ticket.ProfileId, ticket.CustomerId, ticket.Title, ticket.Description, ticket.ApplicationId ?? 0, ticket.CategoryId ?? 0, category, ticket.ReasonsId ?? 0, reason, statusCode, ticket.PriorityId, ticket.AssignedToDepartmentId,
                                ticket.CreationDate, ticket.ModifedDate), statusList, commentList, null, externalReferencesList, parentTicket);
                        }



                        if (mticket != null && mticket.Ticket != null && transaction != null && transaction.TransactionId != "")
                        {
                            mticket.Ticket.TransactionId = transaction.TransactionId;
                        }



                        lTickets.Add(mticket);
                    }
                }
                catch (Exception ex)
                {

                    throw ex;
                }

            }
            return lTickets;
        }

        public MasterTicket GeTicket(long pTicketId, string pUserId, int pBankId)
        {
            MasterTicket parentTicket = null;

            using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
            {
                string category = string.Empty;
                string statusCode = string.Empty;
                string reason = string.Empty;

                try
                {
                    var lTicket = ctx.Tickets.AsNoTracking().FirstOrDefault(p => p.TicketId == pTicketId && (pBankId == 0 || (p.BankId > 0 && p.BankId == pBankId)));
                    if (lTicket == null)
                        return null;

                    if (lTicket.CategoryId != null && lTicket.CategoryId > 0)
                    {
                        int categoryId = Convert.ToInt32(lTicket.CategoryId);
                        var etcategory = ctx.Categories.Where(p => p.CategoryId == categoryId).FirstOrDefault();
                        category = etcategory.Code;
                    }

                    if (lTicket.ReasonsId != null && lTicket.ReasonsId > 0)
                    {
                        int reasonsId = Convert.ToInt32(lTicket.ReasonsId);
                        var etReasons = ctx.Reasons.Where(p => p.ReasonsId == reasonsId).FirstOrDefault();
                        reason = etReasons.Description;
                    }


                    var externalReferences =
                     ctx.TicketExternalReferences.Where(p => p.TicketId == pTicketId)
                         .ToList();


                    var statuses = ctx.TicketAudits.Where(p => p.TicketId == pTicketId && p.TicketStatusId != null).ToList();

                    if (statuses.Count == 0)
                        return null;

                    var statusList =
                        statuses.Select(st => new Entities.TicketStatus(new ValueObjects.TicketStatusModel(st.TicketStatus.TicketStatusId, st.TicketStatus.Value, st.TicketStatus.Description), st.UserId, null, lTicket.TicketId, st.TicketAction != null ? st.TicketAction.Description : "")).ToList();

                    if (statusList.Last() != null && statusList.Last().Status != null)
                    {
                        statusCode = statusList.Last().Status.StatusCode;
                    }


                    var externalReferencesList =
                           externalReferences.Select(st => new Entities.TicketExternalReferences(st.TicketExternalReferencesId, st.UserId, st.RecordDate, st.TicketId, st.Comments, st.TypeCode, st.PayLoad)).ToList();



                    var lCommentList =
                        ctx.Comments.Where(p => p.TicketId == pTicketId).AsNoTracking().ToList();
                    var commentList = lCommentList.Select(cm => new Entities.Comment(lTicket.TicketId, cm.CommentId, cm.UserId, cm.CommentValue, cm.RecordDate))
                            .ToList();

                    parentTicket = null;
                    if (lTicket.TicketParentId != null)
                        parentTicket = GetTicketParent(lTicket.TicketParentId.Value, pUserId);

                    var mticket = new MasterTicket(pUserId,
                        new Ticket(lTicket.TicketId, lTicket.TicketParentId ?? 0, lTicket.CreatedBy, lTicket.BankId ?? 0, lTicket.ProfileId, lTicket.CustomerId, lTicket.Title, lTicket.Description, lTicket.ApplicationId ?? 0, lTicket.CategoryId ?? 0, category, lTicket.ReasonsId ?? 0, reason, statusCode
                        , lTicket.PriorityId, lTicket.AssignedToDepartmentId, lTicket.CreationDate, lTicket.ModifedDate), statusList, commentList, null, externalReferencesList, parentTicket);
                    return mticket;

                }
                catch (Exception ex)
                {

                    throw ex;
                }

            }
        }

        public MasterTicket GeTicketById(string pUserId, long pTicketId)
        {
            using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
            {
                string category = string.Empty;
                string statusCode = string.Empty;
                string reason = string.Empty;
                MasterTicket parentTicket = null;

                try
                {
                    var lTicket = ctx.Tickets.AsNoTracking().FirstOrDefault(p => p.TicketId == pTicketId);
                    if (lTicket == null)
                        return null;

                    if (lTicket.CategoryId != null && lTicket.CategoryId > 0)
                    {
                        int categoryId = Convert.ToInt32(lTicket.CategoryId);
                        var etcategory = ctx.Categories.Where(p => p.CategoryId == categoryId).FirstOrDefault();
                        category = etcategory.Code;
                    }

                    if (lTicket.ReasonsId != null && lTicket.ReasonsId > 0)
                    {
                        int reasonsId = Convert.ToInt32(lTicket.ReasonsId);
                        var etReasons = ctx.Reasons.Where(p => p.ReasonsId == reasonsId).FirstOrDefault();
                        reason = etReasons.Description;
                    }


                    var externalReferences = ctx.TicketExternalReferences.Where(p => p.TicketId == pTicketId).ToList();

                    var statuses = ctx.TicketAudits.Where(p => p.TicketId == pTicketId && p.TicketStatusId != null).ToList();

                    if (statuses.Count == 0)
                        return null;

                    var statusList =
                        statuses.Select(st => new Entities.TicketStatus(new ValueObjects.TicketStatusModel(st.TicketStatus.TicketStatusId, st.TicketStatus.Value, st.TicketStatus.Description), st.UserId, st.ChangeDate, lTicket.TicketId, st.TicketAction != null ? st.TicketAction.Description : "")).ToList();

                    var externalReferencesList =
                         externalReferences.Select(st => new Entities.TicketExternalReferences(st.TicketExternalReferencesId, st.UserId, st.RecordDate, st.TicketId, st.Comments, st.TypeCode, st.PayLoad)).ToList();



                    var lCommentList =
                        ctx.Comments.Where(p => p.TicketId == pTicketId).AsNoTracking().ToList();
                    var commentList = lCommentList.Select(cm => new Entities.Comment(lTicket.TicketId, cm.CommentId, cm.UserId, cm.CommentValue, cm.RecordDate))
                            .ToList();

                    var lTransactionsList =
                        ctx.TicketTransactions.Where(p => p.TicketId == pTicketId).AsNoTracking().ToList();
                    var transactionsList = lTransactionsList.Select(cm => new Entities.TicketTransaction(cm.TicketId, cm.BankId, cm.BankName, cm.TransactionId, cm.RequestId, cm.ProviderId, cm.ProviderName, cm.PaymentTypeId, cm.PaymentTypeName, cm.AccountType, cm.AccountNumber, cm.TransactionStatus, cm.Amount, cm.PaymentAmount, cm.TransactionDate, cm.CurrencyId, cm.CurrencyCode, cm.PaymentOptionId, cm.PaymentOptionName, cm.SourceChannel, cm.SFM, cm.BankTransactionId, cm.PaymentCurrencyId ?? 0)).ToList();


                    if (statusList.Last() != null && statusList.Last().Status != null)
                    {
                        statusCode = statusList.Last().Status.StatusCode;
                    }

                    parentTicket = null;
                    if (lTicket.TicketParentId != null)
                        parentTicket = GetTicketParent(lTicket.TicketParentId.Value, string.Empty);

                    var mticket = new MasterTicket(pUserId,
                        new Ticket(lTicket.TicketId, lTicket.TicketParentId ?? 0, lTicket.CreatedBy, lTicket.BankId ?? 0, lTicket.ProfileId, lTicket.CustomerId, lTicket.Title, lTicket.Description, lTicket.ApplicationId ?? 0, lTicket.CategoryId ?? 0, category, lTicket.ReasonsId ?? 0, reason, statusCode
                        , lTicket.PriorityId, lTicket.AssignedToDepartmentId, lTicket.CreationDate, lTicket.ModifedDate), statusList, commentList, transactionsList, externalReferencesList, parentTicket);
                    return mticket;

                }
                catch (Exception ex)
                {

                    throw ex;
                }

            }
        }

        public MasterTicket AddNewTicket(Ticket pTicket, Atlas.Core.Logic.Entities.TicketTransaction pTransaction, List<Atlas.Core.Logic.Entities.TicketExternalReferences> pExternalReferences, string pComment, bool pIsSendEmail, bool pIsSendSMS, out List<string> pActionRouteCode, out List<string> pActionNotificationCode, out string message, out bool isSuccess)
        {
            message = string.Empty;
            isSuccess = true;
            MasterTicket parentTicket = null;
            MasterTicket tTicket = null;
            try
            {

                if (pComment == null || pComment == "")
                {
                    message = "Error";
                    isSuccess = false;
                    pActionRouteCode = new List<string>();
                    pActionNotificationCode = new List<string>();
                    return tTicket;
                }



                string category = string.Empty;
                string reason = string.Empty;
                pActionRouteCode = new List<string>();
                pActionNotificationCode = new List<string>();
                var createdStatus = ValueObjects.TicketStatusModel.CreatedTicketStatus;
                List<TicketExternalReferences> lstTicketExternalReferences = new List<TicketExternalReferences>();
                List<TicketTransaction> lstTicketTransaction = new List<TicketTransaction>();
                using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
                {
                    var ticket = ctx.Tickets.Add(new DM.Ticket
                    {
                        Title = pTicket.Title,
                        ApplicationId = pTicket.ApplicationId,
                        CategoryId = pTicket.CategoryId,
                        ReasonsId = pTicket.ReasonsId,
                        CreatedBy = pTicket.UserId,
                        Description = pTicket.Description,
                        LastStatusChanged = DateTime.UtcNow,
                        TicketStatusId = createdStatus.StatusId,
                        CreationDate = pTicket.CreationDate,
                        ModifedDate = pTicket.ModifiedDate,
                        ProfileId = pTicket.ProfileId,
                        CustomerId = pTicket.CustomerId,
                        BankId = pTicket.BankId,
                        PriorityId = pTicket.PriorityId,
                        AssignedToDepartmentId = pTicket.DepartmentId
                    });

                    if (pTicket.CategoryId > 0)
                    {
                        int categoryId = Convert.ToInt32(pTicket.CategoryId);
                        var etcategory = ctx.Categories.Where(p => p.CategoryId == categoryId).FirstOrDefault();
                        category = etcategory.Code;

                        var ationsRoutes = from c in ctx.CategoriesActionsRoutes
                                           join a in ctx.ActionsRoutes on c.ActionsRouteId equals a.ActionsRouteId
                                           where c.CategoryId == categoryId
                                           select a;


                        pActionRouteCode = ationsRoutes.ToList().Select(p => p.Code).ToList();



                        var ationsNotification = from c in ctx.CategoriesActionsNotifications
                                                 join a in ctx.ActionsNotifications on c.ActionsNotificationId equals a.ActionsNotificationId
                                                 where c.CategoryId == categoryId && ((a.Code == ActionsNotification.ClaimAcknowledged && a.Type == 1) || (pIsSendEmail == true && a.Type == 1) || (pIsSendSMS == true && a.Type == 2))
                                                 select a;


                        pActionNotificationCode = ationsNotification.ToList().Select(p => p.Code).ToList();

                    }

                    if (pTicket.ReasonsId > 0)
                    {
                        int reasonsId = Convert.ToInt32(pTicket.ReasonsId);
                        var etReasons = ctx.Reasons.Where(p => p.ReasonsId == reasonsId).FirstOrDefault();
                        reason = etReasons.Description;
                    }

                    var status = ctx.TicketAudits.Add(new DM.TicketAudit
                    {
                        UserId = pTicket.UserId,
                        Ticket = ticket,
                        TicketStatusId = createdStatus.StatusId,
                        ChangeDate = ticket.LastStatusChanged ?? DateTime.UtcNow

                    });

                    var comment = ctx.Comments.Add(new DM.Comment
                    {
                        Ticket = ticket,
                        UserId = pTicket.UserId,
                        CommentValue = pComment != null ? pComment : string.Empty,
                        RecordDate = DateTime.UtcNow
                    });


                    if (pTransaction != null)
                    {
                        var ticketTransactions = ctx.TicketTransactions.Add(new DM.TicketTransaction
                        {
                            Ticket = ticket,
                            Amount = pTransaction.TotalAmount,
                            BankId = pTransaction.BankId,
                            BankName = pTransaction.BankName,
                            CurrencyCode = pTransaction.Currency,
                            CurrencyId = pTransaction.CurrencyId,
                            PaymentOptionId = pTransaction.PaymentOptionId,
                            PaymentOptionName = pTransaction.PaymentOptionName,
                            PaymentTypeId = pTransaction.PaymentTypeId,
                            PaymentTypeName = pTransaction.PaymentType,
                            ProviderId = pTransaction.ProviderId,
                            ProviderName = pTransaction.ProviderName,
                            TransactionId = pTransaction.PinPayTransactionId,
                            TransactionDate = pTransaction.TransactionDate,
                            TransactionStatus = pTransaction.StatusId,
                            BankTransactionId = pTransaction.BankTransactionId,
                            AccountNumber = pTransaction.AccountNumber,
                            AccountType = pTransaction.AccountType,
                            PaymentCurrencyId = pTransaction.PaymentCurrencyId,
                            SFM = pTransaction.SFM,
                            RequestId = pTransaction.RequestId,
                            PaymentAmount = pTransaction.PaymentAmount,
                            SourceChannel = pTransaction.SourceChannel

                        });
                        ctx.SaveChanges();
                        lstTicketTransaction.Add(new TicketTransaction(pTransaction.TicketId, pTransaction.BankId, pTransaction.BankName, pTransaction.PinPayTransactionId, pTransaction.RequestId, pTransaction.ProviderId, pTransaction.ProviderName, pTransaction.PaymentTypeId, pTransaction.PaymentType, pTransaction.AccountType, pTransaction.AccountNumber, pTransaction.StatusId, pTransaction.TotalAmount, pTransaction.PaymentAmount, pTransaction.TransactionDate, pTransaction.CurrencyId, pTransaction.Currency, pTransaction.PaymentOptionId, pTransaction.PaymentOptionName, pTransaction.SourceChannel, pTransaction.SFM, pTransaction.BankTransactionId, pTransaction.PaymentCurrencyId));
                    }


                    var commentAudit = ctx.TicketAudits.Add(new DM.TicketAudit
                    {
                        UserId = pTicket.UserId,
                        Ticket = ticket,
                        ChangeDate = ticket.LastStatusChanged ?? DateTime.UtcNow,
                        Comment = pComment,
                        PriorityId = pTicket.PriorityId,
                        DepartmentId = pTicket.DepartmentId
                    });

                    ctx.SaveChanges();

                    if (pExternalReferences != null && pExternalReferences.Count > 0)
                    {
                        for (int i = 0; i < pExternalReferences.Count; i++)
                        {

                            var ticketExternalReferences = ctx.TicketExternalReferences.Add(new DM.TicketExternalReference
                            {
                                Comments = pExternalReferences[i].Comments,
                                PayLoad = pExternalReferences[i].PayLoad,
                                RecordDate = DateTime.UtcNow,
                                TicketId = ticket.TicketId,
                                TypeCode = pExternalReferences[i].TypeCode
                            });
                            ctx.SaveChanges();
                            lstTicketExternalReferences.Add(new TicketExternalReferences(pExternalReferences[i].Id, pExternalReferences[i].UserId, pExternalReferences[i].Date, ticket.TicketId, pExternalReferences[i].Comments, pExternalReferences[i].TypeCode, pExternalReferences[i].PayLoad));
                        }
                    }

                    if (ticket.TicketParentId != null)
                        parentTicket = GetTicketParent(ticket.TicketParentId.Value, string.Empty);

                    var mticket = new MasterTicket(pTicket.UserId
                        , new Ticket(
                          ticket.TicketId
                        , pTicket.TicketParentId
                        , pTicket.UserId
                        , pTicket.BankId
                        , pTicket.ProfileId
                        , pTicket.CustomerId
                        , pTicket.Title
                        , pTicket.Description
                        , pTicket.ApplicationId
                        , pTicket.CategoryId
                        , category
                        , pTicket.ReasonsId
                        , reason
                        , TicketType.Profile
                        , pTicket.PriorityId
                        , pTicket.DepartmentId
                        , ticket.CreationDate
                        , ticket.ModifedDate)
                        , new List<Entities.TicketStatus> { new Entities.TicketStatus(createdStatus, status.UserId, status.ChangeDate, ticket.TicketId) }
                        , new List<Comment> { new Comment(ticket.TicketId, comment.CommentId, pTicket.UserId, pComment, ticket.CreationDate) }
                        , lstTicketTransaction
                        , lstTicketExternalReferences
                        , parentTicket
                        );

                    return mticket;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void UpdateTicket(Ticket pTicket, string pComment)
        {
            try
            {
                var createdStatus = ValueObjects.TicketStatusModel.CreatedTicketStatus;
                using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
                {
                    var ticket = ctx.Tickets.FirstOrDefault(p => p.TicketId == pTicket.Id);
                    if (ticket == null)
                        return;

                    if (pTicket.PriorityId != ticket.PriorityId || pTicket.DepartmentId != ticket.AssignedToDepartmentId || pTicket.Description != ticket.Description)
                    {
                        var status = ctx.TicketAudits.Add(new DM.TicketAudit
                        {
                            UserId = pTicket.UserId,
                            Ticket = ticket,
                            PriorityId = pTicket.PriorityId,
                            DepartmentId = pTicket.DepartmentId,
                            ChangeDate = ticket.LastStatusChanged ?? DateTime.UtcNow
                        });
                        ticket.PriorityId = pTicket.PriorityId;
                        ticket.AssignedToDepartmentId = pTicket.DepartmentId;
                        ticket.ModifedDate = DateTime.UtcNow;
                        ticket.ModifiedBy = pTicket.UserId;
                        ticket.Description = pTicket.Description;
                        ctx.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;

            }
        }

        public MasterTicket UpdateTicketStatus(long pTicketid, string pticketCategoryActionsId, string pUserId, string pComment, bool pIsSendEmail, bool pIsSendSMS, out List<string> pActionRouteCode, out List<string> pActionNotificationCode, out long pProfileId, out long pCustomerId, out bool iSuccess)
        {
            try
            {
                iSuccess = true;
                long ticketStatusId = 0;
                long ticketCategoriesDestinationId = 0;
                long ticketCategoryActionsId = Convert.ToInt32(pticketCategoryActionsId);
                long ticketActionsId = 0;
                MasterTicket etData = null;


                if (pComment == null || pComment == "")
                {
                    iSuccess = false;
                    pActionRouteCode = new List<string>();
                    pActionNotificationCode = new List<string>();
                    pProfileId = 0;
                    pCustomerId = 0;
                    return etData;
                }

                pActionRouteCode = new List<string>();
                pActionNotificationCode = new List<string>();
                pProfileId = 0;
                pCustomerId = 0;

                var createdStatus = ValueObjects.TicketStatusModel.CreatedTicketStatus;
                using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
                {
                    var ticket = ctx.Tickets.FirstOrDefault(p => p.TicketId == pTicketid);
                    if (ticket == null)
                        if (ticket == null)
                        {
                            iSuccess = false;
                            return etData;
                        }


                    var ticketAudits = ctx.TicketAudits.OrderByDescending(p => p.ChangeDate).FirstOrDefault(p => p.TicketId == pTicketid && p.TicketStatusId != null);
                    if (ticketAudits == null)
                        if (ticket == null)
                        {
                            iSuccess = false;
                            return etData;
                        }

                    var ticketActions = ctx.TicketCategoriesActions.Where(p => p.TicketCategoriesActionsId == ticketCategoryActionsId && p.TicketCategoriesId == ticket.CategoryId && p.TicketStatusesId == ticketAudits.TicketStatusId).FirstOrDefault();


                    if (ticketActions != null)
                    {
                        ticketStatusId = Convert.ToInt32(ticketActions.TicketStatusesDestinationId);
                        ticketCategoriesDestinationId = ticketActions.TicketCategoriesDestinationId != null ? Convert.ToInt32(ticketActions.TicketCategoriesDestinationId) : 0;
                        ticketActionsId = ticketActions.TicketActionsId;
                        pProfileId = Convert.ToInt32(ticket.ProfileId);
                        pCustomerId = Convert.ToInt32(ticket.CustomerId);
                    }

                    else
                    {
                        iSuccess = false;
                        return etData;
                    }

                    if (ticketActions != null && ticketActions.TicketCategoriesActionsId > 0)
                    {
                        var ationsRoutes = from c in ctx.TicketCategoriesActionsRoutes
                                           join a in ctx.ActionsRoutes on c.ActionsRouteId equals a.ActionsRouteId
                                           where c.TicketCategoriesActionsId == ticketActions.TicketCategoriesActionsId
                                           select a;


                        pActionRouteCode = ationsRoutes.ToList().Select(p => p.Code).ToList();


                        var ationsNotification = from c in ctx.TicketCategoriesActionsNotifications
                                                 join a in ctx.ActionsNotifications on c.ActionsNotificationId equals a.ActionsNotificationId
                                                 where c.TicketCategoriesActionsId == ticketActions.TicketCategoriesActionsId && ((pIsSendEmail == true && a.Type == 1) || (pIsSendSMS == true && a.Type == 2))
                                                 select a;


                        pActionNotificationCode = ationsNotification.ToList().Select(p => p.Code).ToList();


                    }

                    ticket.LastStatusChanged = DateTime.UtcNow;
                    ticket.TicketStatusId = ticketStatusId;
                    if (ticket.TicketParentId != null && ticketStatusId == 4)
                        UpdateParentTicketStatus(ticket.TicketParentId.Value, ticketStatusId, pUserId, pComment, ticketActionsId);

                    var status = ctx.TicketAudits.Add(new DM.TicketAudit
                    {
                        UserId = pUserId,
                        Ticket = ticket,
                        ChangeDate = ticket.LastStatusChanged ?? DateTime.UtcNow,
                        Comment = pComment,
                        TicketActionsId = ticketActionsId,
                        TicketStatusId = ticketStatusId
                    });

                    var comment = ctx.Comments.Add(new DM.Comment
                    {
                        Ticket = ticket,
                        UserId = pUserId,
                        CommentValue = pComment,
                        RecordDate = DateTime.UtcNow
                    });

                    ticket.ModifedDate = DateTime.UtcNow;
                    ticket.ModifiedBy = pUserId;
                    ticket.Description = "";
                    ctx.SaveChanges();

                    if (ticketCategoriesDestinationId > 0)
                    {
                        var etTicket = ctx.Tickets.Add(new DM.Ticket
                        {
                            TicketParentId = pTicketid,
                            ApplicationId = ticket.ApplicationId,
                            CategoryId = ticketCategoriesDestinationId,
                            ProfileId = ticket.ProfileId,
                            CustomerId = ticket.CustomerId,
                            TicketStatusId = createdStatus.StatusId,
                            BankId = ticket.BankId,
                            CreatedBy = pUserId,
                            LastStatusChanged = DateTime.UtcNow,
                            CreationDate = DateTime.UtcNow,
                        });

                        var etStatus = ctx.TicketAudits.Add(new DM.TicketAudit
                        {
                            UserId = pUserId,
                            Ticket = etTicket,
                            TicketStatusId = createdStatus.StatusId,
                            ChangeDate = ticket.LastStatusChanged ?? DateTime.UtcNow

                        });

                        var etComment = ctx.Comments.Add(new DM.Comment
                        {
                            Ticket = etTicket,
                            UserId = pUserId,
                            CommentValue = pComment != null ? pComment : string.Empty,
                            RecordDate = DateTime.UtcNow
                        });

                        var commentAudit = ctx.TicketAudits.Add(new DM.TicketAudit
                        {
                            UserId = pUserId,
                            Ticket = etTicket,
                            ChangeDate = ticket.LastStatusChanged ?? DateTime.UtcNow,
                            Comment = pComment,
                        });


                        var pTransaction = ctx.TicketTransactions.Where(p => p.TicketId == pTicketid).FirstOrDefault();

                        if (pTransaction != null)
                        {
                            var ticketTransactions = ctx.TicketTransactions.Add(new DM.TicketTransaction
                            {
                                Ticket = etTicket,
                                Amount = pTransaction.Amount,
                                BankId = pTransaction.BankId,
                                BankName = pTransaction.BankName,
                                CurrencyCode = pTransaction.CurrencyCode,
                                CurrencyId = pTransaction.CurrencyId,
                                PaymentOptionId = pTransaction.PaymentOptionId,
                                PaymentOptionName = pTransaction.PaymentOptionName,
                                PaymentTypeId = pTransaction.PaymentTypeId,
                                PaymentTypeName = pTransaction.PaymentTypeName,
                                ProviderId = pTransaction.ProviderId,
                                ProviderName = pTransaction.ProviderName,
                                TransactionId = pTransaction.TransactionId,
                                TransactionDate = pTransaction.TransactionDate,
                                TransactionStatus = pTransaction.TransactionStatus,
                                BankTransactionId = pTransaction.BankTransactionId,
                                AccountNumber = pTransaction.AccountNumber,
                                AccountType = pTransaction.AccountType,
                                PaymentCurrencyId = pTransaction.PaymentCurrencyId,
                                SFM = pTransaction.SFM,
                                RequestId = pTransaction.RequestId,
                                PaymentAmount = pTransaction.PaymentAmount,
                                SourceChannel = pTransaction.SourceChannel

                            });

                        }

                        ctx.SaveChanges();
                    }

                    etData = GeTicketById(pUserId, ticket.TicketId);

                    return etData;
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void UpdateParentTicketStatus(long ticketId, long ticketStatusId, string pUserId, string pComment, long ticketActionsId)
        {
            using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
            {
                var ticket = ctx.Tickets.FirstOrDefault(p => p.TicketId == ticketId);
                ticket.TicketStatusId = ticketStatusId;
                ticket.LastStatusChanged = DateTime.UtcNow;
                var status = ctx.TicketAudits.Add(new DM.TicketAudit
                {
                    UserId = pUserId,
                    Ticket = ticket,
                    ChangeDate = ticket.LastStatusChanged ?? DateTime.UtcNow,
                    Comment = pComment,
                    TicketActionsId = ticketActionsId,
                    TicketStatusId = ticketStatusId
                });

                var comment = ctx.Comments.Add(new DM.Comment
                {
                    Ticket = ticket,
                    UserId = pUserId,
                    CommentValue = pComment,
                    RecordDate = DateTime.UtcNow
                });
                ticket.ModifedDate = DateTime.UtcNow;
                ticket.ModifiedBy = pUserId;
                ticket.Description = "";
                ctx.SaveChanges();

                if (ticket.TicketParentId != null)
                    UpdateParentTicketStatus(ticket.TicketParentId.Value, ticketStatusId, pUserId, pComment, ticketActionsId);
            }
        }

        public MasterTicket CloseClaim(long pTicketid, string pUserId, string pComment, out bool iSuccess)
        {
            try
            {
                iSuccess = false;
                MasterTicket etData = null;
                var closedTicketStatus = ValueObjects.TicketStatusModel.ClosedTicketStatus;


                if (pComment == null || pComment == "")
                {
                    iSuccess = false;

                    return etData;
                }


                using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
                {
                    var ticket = ctx.Tickets.FirstOrDefault(p => p.TicketId == pTicketid);
                    if (ticket == null)
                    {
                        iSuccess = false;
                        return etData;
                    }



                    var ticketAudits = ctx.TicketAudits.OrderByDescending(p => p.ChangeDate).FirstOrDefault(p => p.TicketId == pTicketid && p.TicketStatusId != null);
                    if (ticketAudits == null)
                    {
                        iSuccess = false;
                        return etData;
                    }


                    var status = ctx.TicketAudits.Add(new DM.TicketAudit
                    {
                        UserId = pUserId,
                        Ticket = ticket,
                        ChangeDate = ticket.LastStatusChanged ?? DateTime.UtcNow,
                        Comment = pComment,
                        TicketActionsId = 7,
                        TicketStatusId = 5
                    });

                    var comment = ctx.Comments.Add(new DM.Comment
                    {
                        Ticket = ticket,
                        UserId = pUserId,
                        CommentValue = pComment,
                        RecordDate = DateTime.UtcNow
                    });

                    ticket.ModifedDate = DateTime.UtcNow;
                    ticket.ModifiedBy = pUserId;
                    ticket.Description = "";
                    ctx.SaveChanges();
                    iSuccess = true;
                    etData = GeTicketById(pUserId, ticket.TicketId);
                    return etData;
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<Entities.Application> GetApplications()
        {
            try
            {
                var lResult = new List<Application>();
                using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
                {
                    var list = ctx.Applications.AsNoTracking().ToList();
                    lResult.AddRange(list.Select(application => new Application(application.ApplicationId, application.ApplicationCode, application.ApplicationDescription)));
                }
                return lResult;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<Entities.Department> GetDepartments()
        {
            try
            {
                var lResult = new List<Department>();
                using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
                {
                    var list = ctx.Departments.AsNoTracking().ToList();
                    lResult.AddRange(list.Select(department => new Department(department.DepartmentId, department.Code, department.Description)));
                }
                return lResult;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<Entities.Priority> GetPriorities()
        {
            try
            {
                var lResult = new List<Priority>();
                using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
                {
                    var list = ctx.Priorities.AsNoTracking().ToList();
                    lResult.AddRange(list.Select(priority => new Priority(priority.PriorityId, priority.Code, priority.Description)));
                }
                return lResult;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public List<Entities.Reason> GetReasonsByCategoryId(long pCategoryId)
        {
            try
            {
                var lResult = new List<Reason>();
                using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
                {
                    var list = ctx.Reasons.AsNoTracking().ToList();

                    if (pCategoryId > 0 && list.Count > 0)
                        list = list.Where(p => p.CategoryId == pCategoryId).ToList();

                    lResult.AddRange(list.Select(priority => new Reason(priority.ReasonsId, priority.Code, priority.Description)));
                }
                return lResult;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public List<Entities.Category> GetCategories()
        {
            try
            {
                var lResult = new List<Category>();
                using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
                {
                    var list = ctx.Categories.AsNoTracking().Where(p => p.Enable == true).ToList();
                    lResult.AddRange(list.Select(application => new Category(application.CategoryId, application.Code, application.Description)));
                }
                return lResult;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<Entities.TicketCategory> GetTicketCategories()
        {
            try
            {
                var lResult = new List<TicketCategory>();
                using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
                {

                    var list = ctx.Categories.AsNoTracking().Where(p => p.Enable == true).ToList();
                    for (int i = 0; i < list.Count(); i++)
                    {
                        var lResultResolution = new List<Resolution>();
                        var lResultReason = new List<Reason>();

                        long idCategories = list[i].CategoryId;
                        var listCategories = ctx.TicketCategoriesActions.Where(p => p.TicketCategoriesId == idCategories).ToList();
                        var listReasons = ctx.Reasons.Where(p => p.CategoryId == idCategories).ToList();


                        List<long> listStatus = listCategories.Where(p => p.TicketCategoriesId == idCategories).Select(p => p.TicketStatusesId).Distinct().ToList();
                        for (int j = 0; j < listStatus.Count(); j++)
                        {
                            var lResultActions = new List<Entities.Action>();
                            long idStatus = listStatus[j];
                            List<long> listActions = listCategories.Where(p => p.TicketCategoriesId == idCategories && p.TicketStatusesId == idStatus).Select(p => p.TicketCategoriesActionsId).Distinct().ToList();

                            for (int t = 0; t < listActions.Count(); t++)
                            {
                                long idActions = listActions[t];
                                var etActions = ctx.TicketActions.Where(p => p.TicketActionsId == idActions).FirstOrDefault();
                                if (etActions != null)
                                {
                                    lResultActions.Add(new Entities.Action(etActions.TicketActionsId, etActions.Value, etActions.Description));
                                }
                            }
                            var etStatus = ctx.TicketStatuses.Where(p => p.TicketStatusId == idStatus).FirstOrDefault();
                            if (etStatus != null)
                            {
                                lResultResolution.Add(new Resolution(etStatus.Value, etStatus.Description, lResultActions));
                            }

                        }


                        for (int l = 0; l < listReasons.Count(); l++)
                        {
                            lResultReason.Add(new Reason(listReasons[l].ReasonsId, listReasons[l].Code, listReasons[l].Description));

                        }


                        var etCategory = ctx.Categories.Where(p => p.CategoryId == idCategories).FirstOrDefault();
                        if (etCategory != null)
                        {
                            lResult.Add(new TicketCategory(etCategory.CategoryId, etCategory.Code, etCategory.Description, lResultResolution, lResultReason));
                        }

                    }
                }
                return lResult;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<ValueObjects.TicketStatusModel> GetStatuses()
        {
            try
            {
                var lResult = new List<ValueObjects.TicketStatusModel>();
                using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
                {
                    var list = ctx.TicketStatuses.AsNoTracking().ToList();
                    lResult.AddRange(list.Select(status => new ValueObjects.TicketStatusModel(status.TicketStatusId, status.Value, status.Description)));
                }
                return lResult;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public List<TicketCategoriesActions> GetResolutionByTicketId(long pTicketId)
        {
            try
            {
                var lResult = new List<TicketCategoriesActions>();
                long ticketParentId = 0;
                using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
                {
                    var ticket = ctx.Tickets.FirstOrDefault(p => p.TicketId == pTicketId);
                    if (ticket == null)
                        return lResult;



                    var ticketAudits = ctx.TicketAudits.OrderByDescending(p => p.ChangeDate).FirstOrDefault(p => p.TicketId == pTicketId && p.TicketStatusId != null);
                    if (ticketAudits == null)
                        return lResult;

                    var ticketOld = ctx.TicketCategoriesActions.Where(p => p.TicketCategoriesId == ticket.CategoryId && p.TicketStatusesDestinationId == ticketAudits.TicketStatusId && p.TicketActionsId == ticketAudits.TicketActionsId).FirstOrDefault();

                    if (ticketOld != null)
                    {
                        ticketParentId = Convert.ToInt16(ticketOld.TicketCategoriesActionsId);
                    }

                    var ticketCategoriesActions = ctx.TicketCategoriesActions.Where(p => p.TicketCategoriesId == ticket.CategoryId && p.TicketStatusesId == ticketAudits.TicketStatusId && ((ticketParentId == 0 && p.TicketParentId == null) || (ticketParentId > 0 && p.TicketParentId == ticketParentId))).ToList();



                    for (int i = 0; i < ticketCategoriesActions.Count(); i++)
                    {
                        long ticketActionsId = ticketCategoriesActions[i].TicketActionsId;
                        var ticketActions = ctx.TicketActions.Where(p => p.TicketActionsId == ticketActionsId).FirstOrDefault();
                        if (ticketActions != null)
                            lResult.Add(new Entities.TicketCategoriesActions(ticketCategoriesActions[i].TicketCategoriesActionsId, ticketActions.Description));
                    }

                }
                return lResult;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public bool HasTransaction(long pCategoryId)
        {
            try
            {
                using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
                {
                    var category = ctx.Categories.Where(p => p.CategoryId == pCategoryId).FirstOrDefault();

                    if (category != null && category.HasTransaction == true)

                        return true;
                    else
                        return false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public string  GetTicketByTransactionId(int pTransactionId)
        {
            string ticktId = string.Empty;

            try
            {
                using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
                {

                    var ticket =
                    (from tr in ctx.TicketTransactions
                     let tk = ctx.TicketAudits.OrderByDescending(p=>p.TicketAuditId).FirstOrDefault(c =>c.TicketId == tr.TicketId &&  c.TicketStatusId >0)                 
                     where tr.TransactionId == pTransactionId.ToString() && tk.TicketStatusId != TicketStatusModel.ClosedTicketStatus.StatusId && tk.TicketStatusId != TicketStatusModel.ResolvedTicketStatus.StatusId
                     select tk
                    ).ToList();

                    if (ticket != null)                  
                        ticktId = string.Join(",", ticket);                 
                }

                return ticktId;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public List<MasterTicket> GetTransactionTicketsByCustomerId(string pUserId, int pCustomerId, int pProfileId, int pBankId)
        {
            var lTickets = new List<MasterTicket>();
            string category = string.Empty;
            string statusCode = string.Empty;
            string reason = string.Empty;
            MasterTicket parentTicket = null;

            using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
            {
                try
                {
                    var tickets =
                       ctx.Tickets.Where(p => (pBankId == 0 && p.ProfileId == pProfileId) || (pBankId > 0 && p.BankId == pBankId && p.CustomerId == pCustomerId)).Distinct()
                            .AsNoTracking()
                            .ToList();

                    if (tickets.Count == 0)
                        return lTickets;

                    foreach (var ticket in tickets)
                    {
                        var transaction = ctx.TicketTransactions.FirstOrDefault(p => p.TicketId == ticket.TicketId);
                        if (transaction != null)
                        {
                            if (ticket.CategoryId != null && ticket.CategoryId > 0)
                            {
                                int categoryId = Convert.ToInt32(ticket.CategoryId);
                                var etcategory = ctx.Categories.Where(p => p.CategoryId == categoryId).FirstOrDefault();
                                category = etcategory.Code;
                            }

                            if (ticket.ReasonsId != null && ticket.ReasonsId > 0)
                            {
                                int reasonsId = Convert.ToInt32(ticket.ReasonsId);
                                var etReasons = ctx.Reasons.Where(p => p.ReasonsId == reasonsId).FirstOrDefault();
                                reason = etReasons.Description;
                            }


                            var sts =
                            ctx.TicketAudits.Where(p => p.TicketId == ticket.TicketId && p.TicketStatusId != null)
                                .ToList();

                            var etr =
                             ctx.TicketExternalReferences.Where(p => p.TicketId == ticket.TicketId)
                                 .ToList();


                            var statusList =
                                sts.Select(st => new Entities.TicketStatus(new ValueObjects.TicketStatusModel(st.TicketStatus.TicketStatusId, st.TicketStatus.Value, st.TicketStatus.Description), st.UserId, null, ticket.TicketId, st.TicketAction != null ? st.TicketAction.Description : "")).ToList();

                            var externalReferencesList =
                                 etr.Select(st => new Entities.TicketExternalReferences(st.TicketExternalReferencesId, st.UserId, st.RecordDate, st.TicketId, st.Comments, st.TypeCode, st.PayLoad)).ToList();


                            var lCommentList =
                                ctx.Comments.Where(p => p.TicketId == ticket.TicketId).AsNoTracking().ToList();

                            var commentList = lCommentList.Select(cm => new Comment(ticket.TicketId, cm.CommentId, cm.UserId, cm.CommentValue, cm.RecordDate))
                                    .ToList();

                            if (statusList.Last() != null && statusList.Last().Status != null)
                            {
                                statusCode = statusList.Last().Status.StatusCode;
                            }

                            parentTicket = null;
                            if (ticket.TicketParentId != null)
                                parentTicket = GetTicketParent(ticket.TicketParentId.Value, pUserId);

                            MasterTicket mticket = null;

                            mticket = new MasterTicket(pUserId,
                            new Ticket(ticket.TicketId, ticket.TicketParentId ?? 0, ticket.CreatedBy, ticket.BankId ?? 0, ticket.ProfileId, ticket.CustomerId, ticket.Title, ticket.Description, ticket.ApplicationId ?? 0, ticket.CategoryId ?? 0, category, ticket.ReasonsId ?? 0, reason, statusCode, ticket.PriorityId, ticket.AssignedToDepartmentId,
                                ticket.CreationDate, ticket.ModifedDate), statusList, commentList, new List<Entities.TicketTransaction> {
                                    new Entities.TicketTransaction(transaction.TicketId, transaction.BankId, transaction.BankName, transaction.TransactionId,transaction.RequestId, transaction.ProviderId, transaction.ProviderName, transaction.PaymentTypeId, transaction.PaymentTypeName, transaction.AccountType, transaction.AccountNumber, transaction.TransactionStatus, transaction.Amount,transaction.PaymentAmount, transaction.TransactionDate, transaction.CurrencyId, transaction.CurrencyCode, transaction.PaymentOptionId, transaction.PaymentOptionName,transaction.SourceChannel, transaction.SFM, transaction.BankTransactionId, transaction.PaymentCurrencyId ?? 0)
                                }, externalReferencesList, parentTicket);

                            if (mticket != null && mticket.Ticket != null && transaction != null && transaction.TransactionId != "")
                            {
                                mticket.Ticket.TransactionId = transaction.TransactionId;
                            }

                            lTickets.Add(mticket);
                        }
                    }
                }
                catch (Exception ex)
                {

                    throw ex;
                }

            }
            return lTickets;
        }
    }

    public class TicketActions
    {
        public const string RequestRenderService = "0001";
        public const string RequestRefund = "0002";
        public const string ForceClose = "0003";
        public const string ApproveRequest = "0004";
        public const string RejectRequest = "0005";
        public const string ApprovedRefund = "0006";
        public const string RejectRefund = "0007";
        public const string CloseClaim = "0008";
    }

    public class ActionsNotification
    {
        public const string ClaimAcknowledged = "Claim_Acknowledged_EMAIL";

    }
}
