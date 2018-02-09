using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.Core.Logic.Entities
{
   

    public class TicketTransaction : Entity
    {
        public long TicketId { get; private set; }

        public int BankId { get; private set; }

        public string BankName { get; private set; }

        public string PinPayTransactionId { get; private set; }

        public int? ProviderId{ get; private set; }

        public string ProviderName { get; private set; }

        public int? PaymentTypeId { get; private set; }

        public string PaymentType { get; private set; }

        public string AccountType { get; private set; }

        public string AccountNumber { get; private set; } 

        public decimal TotalAmount { get; private set; }

        public DateTime TransactionDate { get; private set; }

        public int CurrencyId { get; private set; }

        public int? PaymentCurrencyId { get; private set; }

        public string Currency { get; private set; }

        public string StatusId { get; private set; }

        public int? PaymentOptionId { get; private set; }

        public string PaymentOptionName { get; private set; }

        public string BankTransactionId { get; private set; }


        public string Status
        {
            get
            {
                if (StatusId == "0520")
                    return "Successful";
                else if (StatusId == "0920")
                    return "Failed";
                else if (StatusId == "0950")
                    return "Refunded";
                else if (StatusId == "0010")
                    return "Created";
                else if (StatusId == "0030")
                    return "indeterminate";
                return "";
            }
        }


     

        public TicketTransaction(long pTicketId,int pBankId, string pBankName, string pPinPayTransactionId, int? pProviderId, string pProviderName, int? pPaymentTypeId, string pPaymentType, string pAccountType, string pAccountNumber,
            string pStatusId, decimal pTotalAmount, DateTime pTransactionDate, int pCurrencyId,string pCurrencyCode, int? pPaymentOptionId, string pPaymentOptionName, string pBankTransactionId, int? pPaymentCurrencyId)
        {
            TicketId = pTicketId;
            BankId = pBankId;
            BankName = pBankName;
            PinPayTransactionId = pPinPayTransactionId;
            ProviderId = pProviderId;
            ProviderName = pProviderName;
            PaymentTypeId = pPaymentTypeId;
            PaymentType = pPaymentType;
            AccountType  = pAccountType;
            AccountNumber  = pAccountNumber;
            StatusId = pStatusId;
            TotalAmount  = pTotalAmount;
            TransactionDate  = pTransactionDate;
            CurrencyId  = pCurrencyId;
            PaymentOptionId = pPaymentOptionId;
            PaymentOptionName = pPaymentOptionName;
            Currency = pCurrencyCode;
            BankTransactionId = pBankTransactionId;
            PaymentCurrencyId = pPaymentCurrencyId;
        }

}
}
