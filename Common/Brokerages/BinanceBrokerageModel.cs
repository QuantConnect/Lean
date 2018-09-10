using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides Binance specific properties
    /// </summary>
    public class BinanceBrokerageModel : DefaultBrokerageModel
    {
        public BinanceBrokerageModel(AccountType accountType = AccountType.Cash) : base(accountType)
        {
            if (accountType == AccountType.Margin)
            {
                throw new Exception("The Binance brokerage does not currently support Margin trading.");
            }
        }
    }
}
