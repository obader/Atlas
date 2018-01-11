using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasMngr.Classes
{
    public static class GlobalData
    {
        public static List<Bank> GetBanks()
        {
            var lBanks = new List<Bank>
            {
                new Bank {BankId = 1, BankName = "Audi", CountryId = 422},
                new Bank {BankId = 2, BankName = "Med", CountryId = 422},
                new Bank {BankId = 3, BankName = "FransaBank", CountryId = 422},
                new Bank {BankId = 4, BankName = "DIB", CountryId = 761},
                new Bank {BankId = 9999, BankName = "Audi", CountryId = 9999}
            };
            return lBanks;
        }
    }
}
