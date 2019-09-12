namespace Atlas.Core.Logic.Entities
{
    public class IssueTicketTransaction
    {
        public int Id { get; set; }
        public string BankTransactionId { get; set; }
        public IssueTicketMerchant Merchant { get; set; }
        public string TransactionStatus { get; set; }
        public string TransactionDate { get; set; }
        public IssueTicketService Service { get; set; }
        public decimal ProductAmount { get; set; }
        public IssueTicketAccountType AccountType { get; set; }
        public IssueTicketPaymentOption PaymentOption { get; set; }
        public decimal PaymentAmount { get; set; }
        public IssueTicketCurrency Currency { get; set; }
    }   
}

