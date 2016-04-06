using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Tests.Brokerages.Bitfinex
{
    public class BitfinexTestsHelpers
    {

        public static Security GetSecurity()
        {
            return new Security(SecurityExchangeHours.AlwaysOpen(TimeZones.Utc), CreateConfig(), new Cash(CashBook.AccountCurrency, 1000, 1m), SymbolProperties.GetDefault(CashBook.AccountCurrency));
        }

        private static SubscriptionDataConfig CreateConfig()
        {
            return new SubscriptionDataConfig(typeof(TradeBar), "BTCUSD", Resolution.Minute, TimeZones.Utc, TimeZones.Utc, false, true, false);
        }


    }
}
