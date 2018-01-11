using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.Core.Logic.Entities
{
    public class Enums
    {
        public enum Currency
        {
            /// <summary>
            /// The usd
            /// </summary>
            USD = 840,
            /// <summary>
            /// The LBP
            /// </summary>
            LBP = 422,

            /// <summary>
            /// The AED
            /// </summary>
            AED = 784,

            /// <summary>
            /// The JPY
            /// </summary>
            JPY = 392,

            /// <summary>
            /// The EGP
            /// </summary>
            EGP = 818,

            /// <summary>
            /// The GBP
            /// </summary>
            GBP = 826,

            /// <summary>
            /// The EUR
            /// </summary>
            EUR = 978,

            /// <summary>
            /// The PPT
            /// </summary>
            PPT = 5000,

            /// <summary>
            /// The DPT
            /// </summary>
            DPT = 5001,


        }

        public class TicketType
        {
            public const string Profile = "0010";
            public const string Merchant = "0020";
            public const string Bank = "0030";
        }
    }
}
