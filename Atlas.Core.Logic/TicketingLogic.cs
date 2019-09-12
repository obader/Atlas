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
using Atlas.Core.Logic.DTOs;

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

        public string GetTicketByTransactionId(string pTransactionId)
        {
            string ticketId = string.Empty;

            try
            {
                using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
                {
                    var ticket =
                    (from ticketTransaction in ctx.TicketTransactions
                     where ticketTransaction.TransactionId == pTransactionId
                     && ticketTransaction.Ticket.TicketAudits.OrderByDescending(p => p.TicketAuditId).FirstOrDefault(c => c.TicketId == ticketTransaction.TicketId && c.TicketStatusId > 0)
                     .TicketStatusId != TicketStatusModel.ClosedTicketStatus.StatusId
                     && ticketTransaction.Ticket.TicketAudits.OrderByDescending(p => p.TicketAuditId).FirstOrDefault(c => c.TicketId == ticketTransaction.TicketId && c.TicketStatusId > 0)
                     .TicketStatusId != TicketStatusModel.ResolvedTicketStatus.StatusId
                     select ticketTransaction
                    ).ToList();

                    if (ticket != null && ticket.Count > 0)
                        ticketId = string.Join(",", ticket.FirstOrDefault().TicketId);
                }

                return ticketId;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public List<TicketCategory> GetTicketCategories(long channelId, int bankId)
        {
            try
            {
                using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
                {
                    var ticketCategories = new List<TicketCategory>();

                    //get all categories
                    var categories = ctx.Categories.AsNoTracking().Where(p => p.Enable == true).ToList();
                    for (int i = 0; i < categories.Count(); i++)
                    {
                        var category = categories[i];
                        //get category reasons
                        var reasons = category.Reasons
                            .Where(w =>
                           (bankId == 0 || (bankId > 0 && w.BankId == bankId)
                           && (channelId == 0 && w.ChannelId == null || (channelId > 0 && w.ChannelId == channelId))
                           )
                            ).ToList();
                        //get category ticketCategoriesActions
                        var ticketCategoriesActions = category.TicketCategoriesActions
                            .Where(w =>
                            (bankId == 0 || (bankId > 0 && w.BankId == bankId)
                            && (channelId == 0 && w.ChannelId == null || (channelId > 0 && w.ChannelId == channelId))
                            )
                            ).ToList();

                        //set return reasons
                        var lstReasons = new List<Reason>();
                        for (int j = 0; j < reasons.Count(); j++)
                        {
                            var reason = reasons[j];
                            lstReasons.Add(
                                new Reason(
                                    reason.ReasonsId,
                                    reason.Code,
                                    reason.Description
                                    )
                                );
                        }

                        //get ticketaction for each status
                        var resolutions = new List<Resolution>();
                        var statuses = ticketCategoriesActions.GroupBy(g => g.TicketStatusesId).Select(p => p.First()).ToList();
                        var actionsList = new List<Entities.Action>();
                        for (int k = 0; k < statuses.Count(); k++)
                        {
                            var status = statuses[k];
                            var ticketActions = ticketCategoriesActions.Where(w => w.TicketStatusesId == status.TicketStatus.TicketStatusId).Select(s => s.TicketAction).ToList();

                            for (int l = 0; l < ticketActions.Count(); l++)
                            {
                                var ticketAction = ticketActions[l];
                                actionsList.Add(new Entities.Action(ticketAction.TicketActionsId, ticketAction.Value, ticketAction.Description));
                            }
                            resolutions.Add(
                                new Resolution(
                                    status.TicketStatus.Value,
                                    status.TicketStatus.Description,
                                    actionsList
                                ));
                        }

                        ticketCategories.Add(new TicketCategory(category.CategoryId, category.Code, category.Description, resolutions, lstReasons));
                    }
                    return ticketCategories;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public MasterTicket AddNewTicket(Ticket ticket, TicketTransaction transaction, List<TicketExternalReferences> externalReferences, string comment, bool sendEmail, bool sendSMS,
            out List<string> ationRouteCodes, out List<Tuple<string, string, string, string>> actionNotificationCodes, out string channel, out string message, out bool isSuccess)
        {
            MasterTicket parentTicket = null;
            MasterTicket masterTicket = null;

            try
            {
                message = string.Empty;
                isSuccess = true;
                channel = string.Empty;

                if (string.IsNullOrEmpty(comment))
                {
                    message = "Error";
                    isSuccess = false;
                    channel = string.Empty;
                    ationRouteCodes = new List<string>();
                    actionNotificationCodes = new List<Tuple<string, string, string, string>>();
                    return masterTicket;
                }

                string category = string.Empty;
                string reason = string.Empty;
                ationRouteCodes = new List<string>();
                actionNotificationCodes = new List<Tuple<string, string, string, string>>();
                var createdStatus = TicketStatusModel.CreatedTicketStatus;

                List<TicketExternalReferences> lstTicketExternalReferences = new List<TicketExternalReferences>();
                List<TicketTransaction> lstTicketTransaction = new List<TicketTransaction>();
                using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
                {
                    long channelId = 0;

                    if (transaction != null)
                    {
                        var dbChannel = ctx.Channels.Where(p => p.ChannelCode == transaction.SourceChannel).FirstOrDefault();
                        if (dbChannel != null)
                        {
                            channelId = dbChannel.ChannelId;
                            channel = dbChannel.ChannelCode;
                        }
                    }

                    var createdTicket = ctx.Tickets.Add(
                    new DM.Ticket
                    {
                        Title = ticket.Title,
                        ApplicationId = ticket.ApplicationId,
                        CategoryId = ticket.CategoryId,
                        ReasonsId = ticket.ReasonsId,
                        CreatedBy = ticket.UserId,
                        Description = ticket.Title,
                        LastStatusChanged = DateTime.UtcNow,
                        TicketStatusId = createdStatus.StatusId,
                        CreationDate = ticket.CreationDate,
                        ModifedDate = ticket.ModifiedDate,
                        ProfileId = ticket.ProfileId,
                        CustomerId = ticket.CustomerId,
                        BankId = ticket.BankId,
                        PriorityId = ticket.PriorityId,
                        AssignedToDepartmentId = ticket.DepartmentId,
                        ChannelId = channelId
                    });

                    var cmt = ctx.Comments.Add(
                    new DM.Comment
                    {
                        Ticket = createdTicket,
                        UserId = ticket.UserId,
                        CommentValue = comment != null ? comment : string.Empty,
                        RecordDate = DateTime.UtcNow
                    });

                    if (ticket.ReasonsId > 0)
                    {
                        int reasonsId = Convert.ToInt32(ticket.ReasonsId);
                        var dbReason = ctx.Reasons.Where(p => p.ReasonsId == reasonsId).FirstOrDefault();
                        reason = dbReason.Description;
                    }

                    if (ticket.CategoryId > 0)
                    {
                        int categoryId = Convert.ToInt32(ticket.CategoryId);
                        var dbCategory = ctx.Categories.Where(p => p.CategoryId == categoryId).FirstOrDefault();
                        category = dbCategory.Code;

                        var catActionsRoutes = from c in ctx.CategoriesActionsRoutes
                                               where c.CategoryId == categoryId
                                               select c;

                        List<string> filtered = null;
                        filtered = catActionsRoutes.Where(w => w.BankId == createdTicket.BankId && w.ChannelId == createdTicket.ChannelId).Select(p => p.ActionsRoute.Code).ToList();
                        if (filtered.Count == 0)
                            filtered = catActionsRoutes.Where(w => w.BankId == createdTicket.BankId && w.ChannelId == null).Select(p => p.ActionsRoute.Code).ToList();
                        if (filtered.Count == 0)
                            filtered = catActionsRoutes.Where(w => w.BankId == null && w.BankId == createdTicket.ChannelId).Select(p => p.ActionsRoute.Code).ToList();
                        if (filtered.Count == 0)
                            filtered = catActionsRoutes.Where(w => w.BankId == null && w.BankId == null).Select(p => p.ActionsRoute.Code).ToList();

                        ationRouteCodes = filtered;

                        var actionsNotification = (from c in ctx.CategoriesActionsNotifications
                                                   where c.CategoryId == categoryId
                                                   && ((c.ActionsNotification.Code == ActionsNotification.ClaimAcknowledged && c.ActionsNotification.Type == 1)
                                                   || (sendEmail == true && c.ActionsNotification.Type == 1) || (sendSMS == true && c.ActionsNotification.Type == 2))
                                                   select new ActionNotificationDynamic()
                                                   {
                                                       Code = c.ActionsNotification.Code,
                                                       BankId = c.BankId,
                                                       ChannelId = c.ChannelId,
                                                       Channel = c.Channel != null ? c.Channel.ChannelDescription : string.Empty,
                                                   });

                        List<ActionNotificationDynamic> filtered2 = null;
                        filtered2 = actionsNotification.Where(w => w.BankId == createdTicket.BankId && w.ChannelId == createdTicket.ChannelId).ToList();
                        if (filtered2.Count == 0)
                            filtered2 = actionsNotification.Where(w => w.BankId == createdTicket.BankId && w.ChannelId == null).ToList();
                        if (filtered2.Count == 0)
                            filtered2 = actionsNotification.Where(w => w.BankId == null && w.BankId == createdTicket.ChannelId).ToList();
                        if (filtered2.Count == 0)
                            filtered2 = actionsNotification.Where(w => w.BankId == null && w.BankId == null).ToList();

                        for (int i = 0; i < filtered2.Count; i++)
                            actionNotificationCodes.Add(new Tuple<string, string, string, string>(filtered2[i].Code, filtered2[i].BankId.ToString(),
                                filtered2[i].ChannelId.ToString(), filtered2[i].Channel));
                    }

                    if (transaction != null)
                    {
                        var ticketTransactions = ctx.TicketTransactions.Add(
                        new DM.TicketTransaction
                        {
                            Ticket = createdTicket,
                            Amount = transaction.TotalAmount,
                            BankId = transaction.BankId,
                            BankName = transaction.BankName,
                            CurrencyCode = transaction.Currency,
                            CurrencyId = transaction.CurrencyId,
                            PaymentOptionId = transaction.PaymentOptionId,
                            PaymentOptionName = transaction.PaymentOptionName,
                            PaymentTypeId = transaction.PaymentTypeId,
                            PaymentTypeName = transaction.PaymentType,
                            ProviderId = transaction.ProviderId,
                            ProviderName = transaction.ProviderName,
                            TransactionId = transaction.PinPayTransactionId,
                            TransactionDate = transaction.TransactionDate,
                            TransactionStatus = transaction.StatusId,
                            BankTransactionId = transaction.BankTransactionId,
                            AccountNumber = transaction.AccountNumber,
                            AccountType = transaction.AccountType,
                            PaymentCurrencyId = transaction.PaymentCurrencyId,
                            SFM = transaction.SFM,
                            RequestId = transaction.RequestId,
                            PaymentAmount = transaction.PaymentAmount,
                            SourceChannel = transaction.SourceChannel
                        });

                        //ctx.SaveChanges();
                        lstTicketTransaction.Add(new TicketTransaction(transaction.TicketId, transaction.BankId, transaction.BankName, transaction.PinPayTransactionId, transaction.RequestId, transaction.ProviderId, transaction.ProviderName, transaction.PaymentTypeId, transaction.PaymentType, transaction.AccountType, transaction.AccountNumber, transaction.StatusId, transaction.TotalAmount, transaction.PaymentAmount, transaction.TransactionDate, transaction.CurrencyId, transaction.Currency, transaction.PaymentOptionId, transaction.PaymentOptionName, transaction.SourceChannel, transaction.SFM, transaction.BankTransactionId, transaction.PaymentCurrencyId));
                    }

                    var audit = ctx.TicketAudits.Add(new DM.TicketAudit
                    {
                        UserId = ticket.UserId,
                        Ticket = createdTicket,
                        TicketStatusId = createdStatus.StatusId,
                        ChangeDate = createdTicket.LastStatusChanged ?? DateTime.UtcNow,
                    });

                    var commentAudit = ctx.TicketAudits.Add(new DM.TicketAudit
                    {
                        UserId = ticket.UserId,
                        Ticket = createdTicket,
                        ChangeDate = createdTicket.LastStatusChanged ?? DateTime.UtcNow,
                        Comment = comment,
                        PriorityId = createdTicket.PriorityId,
                        DepartmentId = ticket.DepartmentId
                    });

                    if (externalReferences != null && externalReferences.Count > 0)
                    {
                        for (int i = 0; i < externalReferences.Count; i++)
                        {
                            var ticketExternalReferences = ctx.TicketExternalReferences.Add(
                            new DM.TicketExternalReference
                            {
                                Comments = externalReferences[i].Comments,
                                PayLoad = externalReferences[i].PayLoad,
                                RecordDate = DateTime.UtcNow,
                                TicketId = createdTicket.TicketId,
                                TypeCode = externalReferences[i].TypeCode
                            });
                            lstTicketExternalReferences.Add(
                                new TicketExternalReferences(externalReferences[i].Id, externalReferences[i].UserId, externalReferences[i].Date,
                                createdTicket.TicketId, externalReferences[i].Comments, externalReferences[i].TypeCode, externalReferences[i].PayLoad));
                        }
                    }

                    if (createdTicket.TicketParentId != null)
                        parentTicket = GetTicketParent(createdTicket.TicketParentId.Value, string.Empty);

                    ctx.SaveChanges();

                    masterTicket = new MasterTicket(ticket.UserId
                        , new Ticket(
                          createdTicket.TicketId
                        , createdTicket.TicketParentId != null ? createdTicket.TicketParentId.Value : 0
                        , ticket.UserId
                        , ticket.BankId
                        , createdTicket.ProfileId
                        , createdTicket.CustomerId
                        , createdTicket.Title
                        , createdTicket.Description
                        , ticket.ApplicationId
                        , ticket.CategoryId
                        , category
                        , ticket.ReasonsId
                        , reason
                        , TicketType.Profile
                        , ticket.PriorityId
                        , ticket.DepartmentId
                        , ticket.CreationDate
                        , createdTicket.ModifedDate)
                        , new List<Entities.TicketStatus> { new Entities.TicketStatus(createdStatus, audit.UserId, audit.ChangeDate, createdTicket.TicketId) }
                        , new List<Comment> { new Comment(createdTicket.TicketId, cmt.CommentId, ticket.UserId, comment, ticket.CreationDate) }
                        , lstTicketTransaction
                        , lstTicketExternalReferences
                        , parentTicket
                        );

                    return masterTicket;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
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

        public List<MasterTicket> GetTickets(string pUserId, DateTime? pFromDate, DateTime? pToDate, int pStatusId, int pCategoryId, int pProfileId, int pCustomerId, string pStatusesFilterOut, int pBankId)
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

        public List<MasterTicket> GetTicketsByCategoryId(string pUserId, int pCategoryId, int pBankId, int statusId, int page, int itemsPerPage,
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
                                        && (ticketId == 0 || (ticketId > 0 && (ticketTransaction.Ticket.TicketId == ticketId || ticketTransaction.Ticket.TicketParentId == ticketId)))
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
                              && (statusId == 0 || (statusId > 0 && ticket.TicketAudits.Where(p => p.TicketStatusId != null).FirstOrDefault() != null && ticket.TicketAudits.Where(p => p.TicketStatusId != null).OrderByDescending(p => p.ChangeDate).FirstOrDefault().TicketStatusId == statusId))
                              && (ticketId == 0 || (ticketId > 0 && (ticket.TicketId == ticketId || ticket.TicketParentId == ticketId)))
                              && (transactionId == 0 || (transactionId > 0 && ticket.TicketTransactions.Where(w => w.TransactionId == transactionId.ToString()).Any()))
                              && (profileId == 0 || (profileId > 0 && ticket.ProfileId == profileId))
                              select ticket
                              );
                    }

                    if (page != 0 && itemsPerPage != 0)
                        ticketsQuery = ticketsQuery.Skip((page - 1) * itemsPerPage).Take(itemsPerPage);

                    List<DM.Ticket> tickets = ticketsQuery.AsNoTracking().ToList();

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

                        mticket.HasIssue = ticket.HasIssue != null ? ticket.HasIssue.Value : false;
                        mticket.IssueDescription = ticket.TicketIssues.Count() != 0 ? ticket.TicketIssues.FirstOrDefault().IssueDescription : string.Empty;
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

        public int GetTicketsCountByCategoryId(string pUserId, int pCategoryId, int pBankId, int statusId, int page, int itemsPerPage,
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
                                        && (statusId == 0 || (statusId > 0 && ticketTransaction.Ticket.TicketAudits.Where(p => p.TicketStatusId != null).FirstOrDefault() != null && ticketTransaction.Ticket.TicketAudits.Where(p => p.TicketStatusId != null).OrderByDescending(p => p.ChangeDate).FirstOrDefault().TicketStatusId == statusId))
                                         && (ticketId == 0 || (ticketId > 0 && (ticketTransaction.Ticket.TicketId == ticketId || ticketTransaction.Ticket.TicketParentId == ticketId)))
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
                          && (ticketId == 0 || (ticketId > 0 && (ticket.TicketId == ticketId || ticket.TicketParentId == ticketId)))
                          && (transactionId == 0 || (transactionId > 0 && ticket.TicketTransactions.Where(w => w.TransactionId == transactionId.ToString()).Any()))
                          && (profileId == 0 || (profileId > 0 && ticket.ProfileId == profileId))
                          select ticket
                          );
                    }

                    return ticketsQuery.AsNoTracking().Count();
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
                if (ticket != null)
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

                    mticket.HasIssue = ticket.HasIssue != null ? ticket.HasIssue.Value : false;
                    mticket.IssueDescription = ticket.TicketIssues.Count() != 0 ? ticket.TicketIssues.FirstOrDefault().IssueDescription : string.Empty;
                }
            }
            return mticket;
        }

        public List<MasterTicket> GetTicketsByProfileId(string pUserId, int pProfiled, int pBankId)
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

        public List<MasterTicket> GetTicketsByCustomerId(string pUserId, int pCustomerd, int pBankId)
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

        public MasterTicket GetTicket(long pTicketId, string pUserId, int pBankId)
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

        public MasterTicket GetTicketById(string pUserId, long pTicketId)
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
                        statuses.Select(st => new Entities.TicketStatus(new TicketStatusModel
                        (st.TicketStatus.TicketStatusId, st.TicketStatus.Value, st.TicketStatus.Description),
                        st.UserId, st.ChangeDate, lTicket.TicketId, st.TicketAction != null ? st.TicketAction.Description : string.Empty)).ToList();

                    var externalReferencesList =
                         externalReferences.Select(st => new TicketExternalReferences(st.TicketExternalReferencesId, st.UserId, st.RecordDate, st.TicketId,
                         st.Comments, st.TypeCode, st.PayLoad)).ToList();

                    var lCommentList =
                        ctx.Comments.Where(p => p.TicketId == pTicketId).AsNoTracking().ToList();
                    var commentList = lCommentList.Select(cm => new Comment(lTicket.TicketId, cm.CommentId, cm.UserId, cm.CommentValue, cm.RecordDate))
                            .ToList();

                    var lTransactionsList =
                        ctx.TicketTransactions.Where(p => p.TicketId == pTicketId).AsNoTracking().ToList();
                    var transactionsList = lTransactionsList.Select(cm => new Entities.TicketTransaction(cm.TicketId, cm.BankId, cm.BankName, cm.TransactionId,
                        cm.RequestId, cm.ProviderId, cm.ProviderName, cm.PaymentTypeId, cm.PaymentTypeName, cm.AccountType, cm.AccountNumber,
                        cm.TransactionStatus, cm.Amount, cm.PaymentAmount, cm.TransactionDate, cm.CurrencyId, cm.CurrencyCode, cm.PaymentOptionId,
                        cm.PaymentOptionName, cm.SourceChannel, cm.SFM, cm.BankTransactionId, cm.PaymentCurrencyId ?? 0)).ToList();

                    if (statusList.Last() != null && statusList.Last().Status != null)
                    {
                        statusCode = statusList.Last().Status.StatusCode;
                    }

                    parentTicket = null;
                    if (lTicket.TicketParentId != null)
                        parentTicket = GetTicketParent(lTicket.TicketParentId.Value, string.Empty);

                    var mticket = new MasterTicket(pUserId,
                        new Ticket(lTicket.TicketId, lTicket.TicketParentId ?? 0, lTicket.CreatedBy, lTicket.BankId ?? 0, lTicket.ProfileId, lTicket.CustomerId, lTicket.Title,
                        lTicket.Description, lTicket.ApplicationId ?? 0, lTicket.CategoryId ?? 0, category, lTicket.ReasonsId ?? 0, reason, statusCode
                        , lTicket.PriorityId, lTicket.AssignedToDepartmentId, lTicket.CreationDate, lTicket.ModifedDate), statusList, commentList, transactionsList,
                        externalReferencesList, parentTicket);

                    if (parentTicket != null)
                    {
                        var dbParentTicket = ctx.Tickets.AsNoTracking().FirstOrDefault(p => p.TicketId == lTicket.TicketParentId);
                        mticket.HasIssue = dbParentTicket.HasIssue != null ? dbParentTicket.HasIssue.Value : false;
                        mticket.IssueDescription = dbParentTicket.TicketIssues.Count() != 0 ? dbParentTicket.TicketIssues.FirstOrDefault().IssueDescription : string.Empty;
                    }
                    else
                    {
                        mticket.HasIssue = lTicket.HasIssue != null ? lTicket.HasIssue.Value : false;
                        mticket.IssueDescription = lTicket.TicketIssues.Count() != 0 ? lTicket.TicketIssues.FirstOrDefault().IssueDescription : string.Empty;
                    }

                    //get channel code
                    if (lTicket.ChannelId != null)
                        mticket.Channel = ctx.Channels.Where(w => w.ChannelId == lTicket.ChannelId.Value).FirstOrDefault().ChannelCode;
                    return mticket;

                }
                catch (Exception ex)
                {
                    throw ex;
                }
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

        public MasterTicket UpdateTicketStatus(long pTicketid, int ticketCategoryActionId, string pUserId, string pComment, bool pIsSendEmail, bool pIsSendSMS,
            out List<string> actionRouteCodes, out List<Tuple<string, string, string, string>> actionNotificationCodes, out long profileId,
            out long customerId, out string channel, out bool isSuccess, out long ticketParentId)
        {
            MasterTicket masterTicket = null;
            int ticketStatusId = 0;
            int ticketCategoriesDestinationId = 0;
            int ticketActionsId = 0;

            try
            {
                isSuccess = true;
                channel = string.Empty;
                profileId = 0;
                customerId = 0;
                ticketParentId = 0;

                if (string.IsNullOrEmpty(pComment))
                {
                    isSuccess = false;
                    channel = string.Empty;
                    actionRouteCodes = new List<string>();
                    actionNotificationCodes = new List<Tuple<string, string, string, string>>();
                    return masterTicket;
                }

                actionRouteCodes = new List<string>();
                actionNotificationCodes = new List<Tuple<string, string, string, string>>();
                var createdStatus = TicketStatusModel.CreatedTicketStatus;

                using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
                {
                    var ticket = ctx.Tickets.FirstOrDefault(p => p.TicketId == pTicketid);
                    if (ticket == null)
                    {
                        isSuccess = false;
                        return masterTicket;
                    }

                    var dbChannel = ctx.Channels.Where(p => p.ChannelId == ticket.ChannelId).FirstOrDefault();
                    if (dbChannel != null)
                        channel = dbChannel.ChannelCode;

                    var ticketAudits = ctx.TicketAudits.OrderByDescending(p => p.ChangeDate).FirstOrDefault(p => p.TicketId == pTicketid && p.TicketStatusId != null);
                    if (ticketAudits == null)
                    {
                        isSuccess = false;
                        return masterTicket;
                    }

                    var ticketCategoryAction = ctx.TicketCategoriesActions
                        .Where(p => p.TicketCategoriesActionsId == ticketCategoryActionId
                        //&& p.TicketCategoriesId == ticket.CategoryId
                        //&& p.TicketStatusesId == ticketAudits.TicketStatusId
                        ).FirstOrDefault();

                    if (ticketCategoryAction != null)
                    {
                        ticketStatusId = Convert.ToInt32(ticketCategoryAction.TicketStatusesDestinationId);
                        ticketCategoriesDestinationId = ticketCategoryAction.TicketCategoriesDestinationId != null ? Convert.ToInt32(ticketCategoryAction.TicketCategoriesDestinationId) : 0;
                        ticketActionsId = Convert.ToInt32(ticketCategoryAction.TicketActionsId);
                        profileId = Convert.ToInt32(ticket.ProfileId);
                        customerId = Convert.ToInt32(ticket.CustomerId);
                    }
                    else
                    {
                        isSuccess = false;
                        return masterTicket;
                    }

                    if (ticketCategoryAction != null)
                    {
                        actionRouteCodes = ticketCategoryAction.TicketCategoriesActionsRoutes.Select(p => p.ActionsRoute.Code).ToList();

                        var actionsNotification = (from c in ctx.TicketCategoriesActionsNotifications
                                                   where c.TicketCategoriesActionsId == ticketCategoryAction.TicketCategoriesActionsId
                                                   && ((pIsSendEmail == true && c.ActionsNotification.Type == 1) || (pIsSendSMS == true && c.ActionsNotification.Type == 2))
                                                   select new ActionNotificationDynamic()
                                                   {
                                                       Code = c.ActionsNotification.Code,
                                                       BankId = c.TicketCategoriesAction.BankId,
                                                       ChannelId = c.TicketCategoriesAction.ChannelId,
                                                       Channel = c.TicketCategoriesAction.Channel != null ? c.TicketCategoriesAction.Channel.ChannelDescription : string.Empty,
                                                   });

                        List<ActionNotificationDynamic> filtered = null;
                        filtered = actionsNotification.Where(w => w.BankId == ticket.BankId && w.ChannelId == ticket.ChannelId).ToList();
                        if (filtered.Count == 0)
                            filtered = actionsNotification.Where(w => w.BankId == ticket.BankId && w.ChannelId == null).ToList();
                        if (filtered.Count == 0)
                            filtered = actionsNotification.Where(w => w.BankId == null && w.BankId == ticket.ChannelId).ToList();
                        if (filtered.Count == 0)
                            filtered = actionsNotification.Where(w => w.BankId == null && w.BankId == null).ToList();

                        for (int i = 0; i < filtered.Count; i++)
                            actionNotificationCodes.Add(new Tuple<string, string, string, string>(filtered[i].Code, filtered[i].BankId.ToString(), filtered[i].ChannelId.ToString(),
                                filtered[i].Channel));

                    }

                    ticket.LastStatusChanged = DateTime.UtcNow;
                    ticket.TicketStatusId = ticketStatusId;
                    ticket.ModifedDate = DateTime.UtcNow;
                    ticket.ModifiedBy = pUserId;

                    ticketParentId = ticket.TicketParentId != null ? ticket.TicketParentId.Value : 0;
                    if (ticket.TicketParentId != null && ticketStatusId == 4)
                        UpdateParentTicketStatus(ticket.TicketParentId.Value, ticketStatusId, pUserId, pComment, ticketActionsId, out ticketParentId);

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

                    if (ticketCategoriesDestinationId > 0)
                    {
                        var newTicket = ctx.Tickets.Add(new DM.Ticket
                        {
                            TicketParentId = pTicketid,
                            ApplicationId = ticket.ApplicationId,
                            CategoryId = ticketCategoriesDestinationId,
                            ProfileId = ticket.ProfileId,
                            CustomerId = ticket.CustomerId,
                            TicketStatusId = createdStatus.StatusId,
                            BankId = ticket.BankId,
                            ChannelId = ticket.ChannelId,
                            CreatedBy = pUserId,
                            LastStatusChanged = DateTime.UtcNow,
                            CreationDate = DateTime.UtcNow,
                        });

                        var etStatus = ctx.TicketAudits.Add(new DM.TicketAudit
                        {
                            UserId = pUserId,
                            Ticket = newTicket,
                            TicketStatusId = createdStatus.StatusId,
                            ChangeDate = ticket.LastStatusChanged ?? DateTime.UtcNow
                        });

                        var etComment = ctx.Comments.Add(new DM.Comment
                        {
                            Ticket = newTicket,
                            UserId = pUserId,
                            CommentValue = pComment != null ? pComment : string.Empty,
                            RecordDate = DateTime.UtcNow
                        });

                        var commentAudit = ctx.TicketAudits.Add(new DM.TicketAudit
                        {
                            UserId = pUserId,
                            Ticket = newTicket,
                            ChangeDate = ticket.LastStatusChanged ?? DateTime.UtcNow,
                            Comment = pComment,
                        });

                        var pTransaction = ctx.TicketTransactions.Where(p => p.TicketId == pTicketid).FirstOrDefault();

                        if (pTransaction != null)
                        {
                            var ticketTransactions = ctx.TicketTransactions.Add(new DM.TicketTransaction
                            {
                                Ticket = newTicket,
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

                    }
                    ctx.SaveChanges();
                    masterTicket = GetTicketById(pUserId, ticket.TicketId);

                    return masterTicket;
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void UpdateParentTicketStatus(long ticketId, long ticketStatusId, string pUserId, string pComment, long ticketActionsId, out long ticketParentId)
        {
            ticketParentId = 0;
            using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
            {
                var ticket = ctx.Tickets.FirstOrDefault(p => p.TicketId == ticketId);
                ticket.TicketStatusId = ticketStatusId;
                ticket.LastStatusChanged = DateTime.UtcNow;
                ticketParentId = ticketId;
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
                ctx.SaveChanges();

                if (ticket.TicketParentId != null)
                    UpdateParentTicketStatus(ticket.TicketParentId.Value, ticketStatusId, pUserId, pComment, ticketActionsId, out ticketParentId);
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


                    ticket.LastStatusChanged = DateTime.UtcNow;
                    ticket.TicketStatusId = 5;
                    if (ticket.TicketParentId != null)
                        CloseParentClaim(ticket.TicketParentId.Value, pUserId, pComment);

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
                    etData = GetTicketById(pUserId, ticket.TicketId);
                    return etData;
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void CloseParentClaim(long pTicketid, string pUserId, string pComment)
        {
            try
            {
                MasterTicket etData = null;
                var closedTicketStatus = ValueObjects.TicketStatusModel.ClosedTicketStatus;


                using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
                {
                    var ticket = ctx.Tickets.FirstOrDefault(p => p.TicketId == pTicketid);
                    ticket.LastStatusChanged = DateTime.UtcNow;
                    ticket.TicketStatusId = 5;


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

                    if (ticket.TicketParentId != null)
                        CloseParentClaim(ticket.TicketParentId.Value, pUserId, pComment);
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

        public List<Entities.Reason> GetReasonsByCategoryId(long pCategoryId, long pChannelId, int pBankId)
        {
            try
            {
                var lResult = new List<Reason>();
                using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
                {
                    var list = ctx.Reasons.AsNoTracking().ToList();

                    if (pCategoryId > 0 && list.Count > 0)
                        list = list.Where(p => p.CategoryId == pCategoryId && (pChannelId == 0 || (pChannelId > 0 && p.ChannelId == pChannelId)) && (pBankId == 0 || (pBankId > 0 && p.BankId == pBankId))).ToList();

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

        public List<TicketCategoriesActions> GetResolutionByTicketId(long ticketId)
        {
            try
            {
                List<TicketCategoriesActions> result = null;
                long ticketParentId = 0;

                using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
                {
                    var ticket = ctx.Tickets.FirstOrDefault(p => p.TicketId == ticketId);
                    if (ticket == null)
                        return result;

                    var ticketAudits = ctx.TicketAudits.OrderByDescending(p => p.ChangeDate).FirstOrDefault(p => p.TicketId == ticketId && p.TicketStatusId != null);
                    if (ticketAudits == null)
                        return result;

                    var ticketOld = ctx.TicketCategoriesActions.Where(p => p.TicketCategoriesId == ticket.CategoryId
                                    && p.TicketStatusesDestinationId == ticketAudits.TicketStatusId
                                    && p.TicketActionsId == ticketAudits.TicketActionsId).FirstOrDefault();

                    if (ticketOld != null)
                        ticketParentId = Convert.ToInt16(ticketOld.TicketCategoriesActionsId);

                    var ticketCategoriesActions = ctx.TicketCategoriesActions
                        .Where(p => p.TicketCategoriesId == ticket.CategoryId
                        && p.TicketStatusesId == ticketAudits.TicketStatusId
                        && (
                                (ticketParentId == 0 && p.TicketParentId == null)
                                || (ticketParentId > 0 && p.TicketParentId == ticketParentId))
                            ).ToList();

                    List<DM.TicketCategoriesAction> filtered = null;
                    filtered = ticketCategoriesActions.Where(w => w.BankId == ticket.BankId && w.ChannelId == ticket.ChannelId).ToList();
                    if (filtered.Count == 0)
                        filtered = ticketCategoriesActions.Where(w => w.BankId == ticket.BankId && w.ChannelId == null).ToList();
                    if (filtered.Count == 0)
                        filtered = ticketCategoriesActions.Where(w => w.BankId == null && w.BankId == ticket.ChannelId).ToList();
                    if (filtered.Count == 0)
                        filtered = ticketCategoriesActions.Where(w => w.BankId == null && w.BankId == null).ToList();
                    result = new List<TicketCategoriesActions>();
                    for (int i = 0; i < filtered.Count(); i++)
                    {
                        long ticketActionsId = filtered[i].TicketActionsId;
                        var ticketActions = ctx.TicketActions.Where(p => p.TicketActionsId == ticketActionsId).FirstOrDefault();
                        if (ticketActions != null)
                            result.Add(new TicketCategoriesActions(filtered[i].TicketCategoriesActionsId, ticketActions.Description));
                    }
                }
                return result;
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

        public int GetTransactionTicketsCountByCustomerId(string pUserId, int pCustomerId, int pProfileId, int pBankId,
           int page, int itemsPerPage)
        {
            var lTickets = new List<MasterTicket>();
            string category = string.Empty;
            string statusCode = string.Empty;
            string reason = string.Empty;

            using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
            {
                try
                {
                    IQueryable<DM.Ticket> ticketsQuery = from ticket in ctx.Tickets
                                                         where
                                                         (
                                                            (pBankId == 0 && ticket.ProfileId == pProfileId)
                                                            || (pBankId > 0 && ticket.BankId == pBankId && ticket.CustomerId == pCustomerId)
                                                         )
                                                         && ticket.TicketTransactions.Count() > 0
                                                         && ticket.TicketParentId.Equals(null)
                                                         orderby ticket.LastStatusChanged descending
                                                         select ticket;

                    return ticketsQuery.AsNoTracking().Count();
                }
                catch (Exception ex)
                {

                    throw ex;
                }

            }
        }

        public List<MasterTicket> GetTransactionTicketsByCustomerId(string pUserId, int pCustomerId, int pProfileId, int pBankId,
            int page, int itemsPerPage)
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
                    IQueryable<DM.Ticket> ticketsQuery = from ticket in ctx.Tickets
                                                         where
                                                         (
                                                            (pBankId == 0 && ticket.ProfileId == pProfileId)
                                                            || (pBankId > 0 && ticket.BankId == pBankId && ticket.CustomerId == pCustomerId)
                                                         )
                                                         && ticket.TicketTransactions.Count() > 0
                                                         && ticket.TicketParentId.Equals(null)
                                                         orderby ticket.LastStatusChanged descending
                                                         select ticket;

                    if (page != 0 && itemsPerPage != 0)
                        ticketsQuery = ticketsQuery.Skip((page - 1) * itemsPerPage).Take(itemsPerPage);

                    List<DM.Ticket> tickets = ticketsQuery.AsNoTracking().ToList();

                    foreach (var ticket in tickets)
                    {
                        var transaction = ctx.TicketTransactions.First();
                        //if (transaction != null)
                        //{
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
                            ticket.CreationDate, ticket.ModifedDate), statusList, commentList, new List<TicketTransaction> {
                                    new TicketTransaction(transaction.TicketId, transaction.BankId, transaction.BankName, transaction.TransactionId,transaction.RequestId,
                                    transaction.ProviderId, transaction.ProviderName, transaction.PaymentTypeId, transaction.PaymentTypeName, transaction.AccountType,
                                    transaction.AccountNumber, transaction.TransactionStatus, transaction.Amount,transaction.PaymentAmount, transaction.TransactionDate,
                                    transaction.CurrencyId, transaction.CurrencyCode, transaction.PaymentOptionId, transaction.PaymentOptionName,transaction.SourceChannel,
                                    transaction.SFM, transaction.BankTransactionId, transaction.PaymentCurrencyId ?? 0)
                            }, externalReferencesList, parentTicket);

                        if (mticket != null && mticket.Ticket != null && transaction != null && transaction.TransactionId != "")
                        {
                            mticket.Ticket.TransactionId = transaction.TransactionId;
                        }

                        lTickets.Add(mticket);
                        //}
                    }
                }
                catch (Exception ex)
                {

                    throw ex;
                }

            }
            return lTickets;
        }

        public List<Entities.Category> GetReportIssueCategories()
        {
            try
            {
                var lResult = new List<Category>();
                using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
                {
                    var list = ctx.Categories.AsNoTracking().Where(p => p.Enable == true && p.TicketType.Code == "0060").ToList();
                    lResult.AddRange(list.Select(application => new Category(application.CategoryId, application.Code, application.Description)));
                }
                return lResult;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<Entities.Reason> GetReportIssueReasons()
        {
            try
            {
                var lResult = new List<Reason>();
                using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
                {
                    var list = ctx.Reasons.AsNoTracking().Where(p => p.Category.Enable == true && p.Category.TicketType.Code == "0060").ToList();
                    lResult.AddRange(list.Select(application => new Reason(application.ReasonsId, application.Code, application.Description)));
                }
                return lResult;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public MasterTicket CreateIssueTicket(Ticket ticket, TicketTransaction pTransaction, string ticketIssueDescription,
            string comment, long reasonId, out List<string> pActionRouteCode, out List<Tuple<string, string, string, string>> pActionNotificationCode)
        {
            pActionNotificationCode = new List<Tuple<string, string, string, string>>();

            using (var ctx = DM.TicketingEntities.ConnectToSqlServer(_connectionInfo))
            {
                var categoryDescription = ctx.Categories.Where(w => w.CategoryId == ticket.CategoryId).FirstOrDefault().Description;
                long channelId = ctx.Channels.Where(w => w.ChannelCode.Contains(categoryDescription)).FirstOrDefault().ChannelId;
                ticket.ReasonsId = reasonId;
                long ticketStatus = ValueObjects.TicketStatusModel.CreatedTicketStatus.StatusId;
                var createdTicket = ctx.Tickets.Add(new DM.Ticket
                {
                    Title = ticket.Title,
                    ApplicationId = ticket.ApplicationId,
                    CategoryId = ticket.CategoryId,
                    ReasonsId = ticket.ReasonsId,
                    CreatedBy = ticket.UserId,
                    Description = ticket.Description,
                    LastStatusChanged = DateTime.UtcNow,
                    TicketStatusId = ticketStatus,
                    CreationDate = ticket.CreationDate,
                    ModifedDate = ticket.ModifiedDate,
                    ProfileId = ticket.ProfileId,
                    CustomerId = ticket.CustomerId,
                    BankId = ticket.BankId,
                    PriorityId = ticket.PriorityId,
                    AssignedToDepartmentId = ticket.DepartmentId,
                    ChannelId = channelId,
                    HasIssue = true
                });

                int categoryId = Convert.ToInt32(createdTicket.CategoryId);
                var etcategory = ctx.Categories.Where(p => p.CategoryId == categoryId).FirstOrDefault();
                string category = etcategory.Code;

                var catActionsRoutes = from c in ctx.CategoriesActionsRoutes
                                       where c.CategoryId == categoryId
                                       select c;

                List<string> filtered = null;
                filtered = catActionsRoutes.Where(w => w.BankId == ticket.BankId && w.ChannelId == createdTicket.ChannelId).Select(p => p.ActionsRoute.Code).ToList();
                if (filtered.Count == 0)
                    filtered = catActionsRoutes.Where(w => w.BankId == ticket.BankId && w.ChannelId == null).Select(p => p.ActionsRoute.Code).ToList();
                if (filtered.Count == 0)
                    filtered = catActionsRoutes.Where(w => w.BankId == null && w.BankId == createdTicket.ChannelId).Select(p => p.ActionsRoute.Code).ToList();
                if (filtered.Count == 0)
                    filtered = catActionsRoutes.Where(w => w.BankId == null && w.BankId == null).Select(p => p.ActionsRoute.Code).ToList();
                pActionRouteCode = filtered;


                //var actionsNotification = (from c in ctx.CategoriesActionsNotifications
                //                          where c.CategoryId == categoryId
                //                          //&& ((a.Code == ActionsNotification.ClaimAcknowledged && a.Type == 1) || (pIsSendEmail == true && a.Type == 1) || (pIsSendSMS == true && a.Type == 2))
                //                          select new ActionNotificationDynamic()
                //                          {
                //                              Code = c.ActionsNotification.Code,
                //                              BankId = c.BankId,
                //                              ChannelId = c.ChannelId,
                //                              Channel = c.Channel != null ? c.Channel.ChannelDescription : string.Empty,
                //                          });

                //List<ActionNotificationDynamic> filtered2 = null;
                //filtered2 = actionsNotification.Where(w => w.BankId == ticket.BankId && w.ChannelId == createdTicket.ChannelId).ToList();
                //if (filtered2.Count == 0)
                //    filtered2 = actionsNotification.Where(w => w.BankId == ticket.BankId && w.ChannelId == null).ToList();
                //if (filtered2.Count == 0)
                //    filtered2 = actionsNotification.Where(w => w.BankId == null && w.BankId == createdTicket.ChannelId).ToList();
                //if (filtered2.Count == 0)
                //    filtered2 = actionsNotification.Where(w => w.BankId == null && w.BankId == null).ToList();

                //for (int i = 0; i < filtered2.Count; i++)
                //    pActionNotificationCode.Add(new Tuple<string, string, string, string>(filtered2[i].Code, filtered2[i].BankId.ToString(),
                //        filtered2[i].ChannelId.ToString(), filtered2[i].Channel));


                int reasonsId = Convert.ToInt32(createdTicket.ReasonsId);
                var etReasons = ctx.Reasons.Where(p => p.ReasonsId == reasonsId).FirstOrDefault();
                string reason = etReasons.Description;

                var status = ctx.TicketAudits.Add(new DM.TicketAudit
                {
                    UserId = ticket.UserId,
                    Ticket = createdTicket,
                    TicketStatusId = ticketStatus,
                    ChangeDate = createdTicket.LastStatusChanged ?? DateTime.UtcNow

                });

                var createdComment = ctx.Comments.Add(new DM.Comment
                {
                    Ticket = createdTicket,
                    UserId = ticket.UserId,
                    CommentValue = comment,
                    RecordDate = DateTime.UtcNow
                });

                var commentAudit = ctx.TicketAudits.Add(new DM.TicketAudit
                {
                    UserId = ticket.UserId,
                    Ticket = createdTicket,
                    ChangeDate = createdTicket.LastStatusChanged ?? DateTime.UtcNow,
                    Comment = comment,
                    PriorityId = ticket.PriorityId,
                    DepartmentId = ticket.DepartmentId
                });

                List<TicketTransaction> lstTicketTransaction = null;
                if (pTransaction != null)
                {
                    lstTicketTransaction = new List<TicketTransaction>();
                    var ticketTransactions = ctx.TicketTransactions.Add(new DM.TicketTransaction
                    {
                        Ticket = createdTicket,
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
                    lstTicketTransaction.Add(new TicketTransaction(pTransaction.TicketId, pTransaction.BankId, pTransaction.BankName, pTransaction.PinPayTransactionId, pTransaction.RequestId,
                        pTransaction.ProviderId, pTransaction.ProviderName, pTransaction.PaymentTypeId, pTransaction.PaymentType, pTransaction.AccountType, pTransaction.AccountNumber, pTransaction.StatusId,
                        pTransaction.TotalAmount, pTransaction.PaymentAmount, pTransaction.TransactionDate, pTransaction.CurrencyId, pTransaction.Currency, pTransaction.PaymentOptionId,
                        pTransaction.PaymentOptionName, pTransaction.SourceChannel, pTransaction.SFM, pTransaction.BankTransactionId, pTransaction.PaymentCurrencyId));
                }

                //add issue
                DM.TicketIssue ticketIssue = new DM.TicketIssue()
                {
                    IssueDescription = ticketIssueDescription,
                    TicketId = createdTicket.TicketId
                };
                ctx.TicketIssues.Add(ticketIssue);

                ctx.SaveChanges();

                var mticket = new MasterTicket(
                    ticket.UserId
                    , new Ticket(
                      createdTicket.TicketId
                    , ticket.TicketParentId
                    , ticket.UserId
                    , ticket.BankId
                    , createdTicket.ProfileId
                    , ticket.CustomerId
                    , ticket.Title
                    , ticket.Description
                    , ticket.ApplicationId
                    , ticket.CategoryId
                    , category
                    , ticket.ReasonsId
                    , reason
                    , TicketType.Profile
                    , ticket.PriorityId
                    , ticket.DepartmentId
                    , createdTicket.CreationDate
                    , createdTicket.ModifedDate)
                    , new List<Entities.TicketStatus> { new Entities.TicketStatus(ValueObjects.TicketStatusModel.CreatedTicketStatus, status.UserId, status.ChangeDate, createdTicket.TicketId) }
                    , new List<Comment> { new Comment(createdTicket.TicketId, createdComment.CommentId, ticket.UserId, comment, ticket.CreationDate) }
                    , lstTicketTransaction
                    , null
                    , null
                    );

                return mticket;
            }
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
