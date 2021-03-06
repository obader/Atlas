//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Atlas.Core.Logic.DM
{
    using System;
    using System.Collections.Generic;
    
    public partial class TicketTransaction
    {
        public long TicketTransactionId { get; set; }
        public long TicketId { get; set; }
        public int BankId { get; set; }
        public string BankName { get; set; }
        public string BankTransactionId { get; set; }
        public string RequestId { get; set; }
        public string AccountNumber { get; set; }
        public string AccountType { get; set; }
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
        public Nullable<decimal> PaymentAmount { get; set; }
        public Nullable<int> PaymentCurrencyId { get; set; }
        public string SFM { get; set; }
        public string SourceChannel { get; set; }
        public System.DateTime TransactionDate { get; set; }
        public string TransactionStatus { get; set; }
        public byte[] RowVersion { get; set; }
    
        public virtual Ticket Ticket { get; set; }
    }
}
