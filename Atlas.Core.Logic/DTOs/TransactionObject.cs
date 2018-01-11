using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Atlas.Core.Logic.DTOs
{
    public class TransactionObject
    {
        public long TicketTransactionId { get; set; }
        public long TicketId { get; set; }
        public int BankId { get; set; }
        public string BankName { get; set; }
        public string TransactionId { get; set; }
        public Nullable<int> ProviderId { get; set; }
        public string ProviderName { get; set; }
        public Nullable<int> PaymentTypeId { get; set; }
        public string PaymentTypeName { get; set; }
        public Nullable<int> PaymentOptionId { get; set; }
        public string PaymentOptionName { get; set; }
        public decimal Amount { get; set; }
        public int CurrencyId { get; set; }
        public string CurrencyCode { get; set; }
        public System.DateTime TransactionDate { get; set; }
        public string TransactionStatus { get; set; }
    }
}